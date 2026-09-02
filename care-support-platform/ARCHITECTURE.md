# CareBridge — Architecture

Same proven stack and layering as the LMS project, so patterns, tooling, and muscle memory carry over directly. Deviations from the LMS are called out explicitly.

## 1. Tech Stack

| Layer | Technology |
|---|---|
| Backend runtime | **.NET 8 LTS** (`net8.0`) |
| Web framework | ASP.NET Core 8 Web API |
| ORM | EF Core 8.0.x |
| Database | SQL Server 2022 (LocalDB `MSSQLLocalDB` for dev) |
| Auth | JWT + refresh tokens; BCrypt password hashing; TOTP 2FA (doctors/admins) |
| Validation | FluentValidation 11.x |
| Mapping | AutoMapper 12.x |
| App pattern | CQRS via MediatR 12.x |
| Background jobs | **Hangfire** (reminder scheduling, notification dispatch, exports) — *new vs LMS* |
| Real-time | SignalR (message delivery, notification badge) — *new vs LMS* |
| Payments | Stripe.net (subscriptions + webhooks) |
| Logging | Serilog (structured, correlation IDs) |
| API docs | Swashbuckle at `/swagger` |
| Backend tests | xUnit + Moq |
| Frontend | Angular 20, standalone components, Signals, typed Reactive Forms, Tailwind CSS |
| Frontend tests | Jasmine + Karma; Cypress E2E |

## 2. Solution Layout

```
src/
├── CareBridge.Domain/           # Entities, enums, domain events. No project references.
├── CareBridge.Application/      # DTOs, MediatR handlers, validators, service interfaces. → Domain
├── CareBridge.Infrastructure/   # DbContext, repositories, migrations, Stripe, storage,
│                                # notification channels, Hangfire jobs. → Application + Domain
└── CareBridge.WebAPI/           # Controllers, SignalR hubs, middleware, Program.cs. → Application + Infrastructure
tests/
└── CareBridge.Tests/            # xUnit + Moq

carebridge-angular/src/app/
├── core/        # auth/JWT/error interceptors, guards, SignalR service, notification service
├── shared/      # UI kit: charts, badges, empty states, confirm dialogs, date/timezone pipes
├── auth/        # login, register, verify, reset, 2FA
├── patient/     # 18 features (PAT-*)
├── doctor/      # 14 features (DOC-*)
└── admin/       # 10 features (ADM-*)
```

Reference rule (same as LMS): Domain ← Application ← Infrastructure; WebAPI composes. DTOs at the boundary, never EF entities from controllers.

## 3. Domain Model (ERD)

```
User (Id, Email, PasswordHash, Role, Status, TwoFactorSecret?)
 ├─ PatientProfile (UserId, DOB, PrimaryConsultantName/Contact, Conditions, Allergies, Timezone)
 ├─ DoctorProfile  (UserId, LicenseNo, Specialty, VerificationStatus, MaxCapacity)
 └─ RefreshToken   (UserId, Token, ExpiresAt, RevokedAt?)

DoctorPatientAssignment (DoctorId, PatientId, AssignedAt, EndedAt?, EndedReason?)

CarePlan        (PatientId, Version, DiagnosisNote, ConsultantInstructions, ConfirmedAt, SupersededById?)
 └─ Medication  (CarePlanId, Name, Dose, Form, Route, Notes)
     └─ MedicationSchedule (MedicationId, TimesJson, WithFood, StartDate, EndDate)
         └─ DoseSlot      (ScheduleId, ScheduledAtUtc, LocalDate, Status: Pending|Taken|TakenLate|Skipped, LoggedAt?, SkipReason?)

DailyCheckIn (PatientId, LocalDate, Feeling1to5, Pain0to10, SleepHours, Appetite, Mood, Note)
SymptomLog   (PatientId, SymptomCatalogId, Severity, OnsetAt, Note, PhotoDocumentId?)
VitalReading (PatientId, Type, Value, Unit, MeasuredAt, Source: Manual|Device)

Observation  (DoctorId, PatientId, Text, Visibility: CareTeam|SharedWithPatient, CreatedAt, AddendumToId?)
GuidanceNote (DoctorId, PatientId, TemplateId?, Text, PatientAckAt?)
FollowUp     (DoctorId, PatientId, DueAt, Recurrence?, Status: Proposed|Confirmed|Completed|Missed, OutcomeNote?)
Escalation   (DoctorId, PatientId, Reason, Urgency, RaisedAt, AcknowledgedAt?, ResolvedAt?, ResolutionNote?)

Conversation (PatientId, DoctorId) ── Message (ConversationId, SenderId, Body, DocumentId?, SentAt, ReadAt?)

Document     (OwnerPatientId, UploaderId, Kind, StoragePath, ContentType, Size, VirusScanStatus)
Notification (UserId, Type, PayloadJson, Channels, ScheduledFor?, SentAt?, ReadAt?)

SubscriptionPlan (Name, PriceCents, Interval, Active)
PatientSubscription (PatientId, PlanId, StripeSubscriptionId, Status, CurrentPeriodEnd)

AuditLog     (ActorUserId, Action, EntityType, EntityId, PatientId?, Ip, At, DetailsJson)  # append-only
SymptomCatalog / VitalTypeCatalog / GuidanceTemplate / NotificationTemplate               # ADM-10
```

Design notes:
- **Care plans are versioned, not edited** — a recovery record must be trustworthy. Same for `Observation` (addendum pattern) and `Message` (immutable).
- **DoseSlot is materialized** by a nightly + on-schedule-change Hangfire job from `MedicationSchedule`, in the patient's timezone. Reminders and adherence both read slots — one source of truth.
- **Every patient-scoped query goes through the assignment check**: a doctor resolves patients only via active `DoctorPatientAssignment`. Enforced centrally (see §5), not per-handler.

## 4. Key Mechanisms

### 4.1 Notification engine (PLT-02)
- `Notifications` table is the queue of record; Hangfire recurring job dispatches due rows to channel providers (in-app via SignalR, email via SMTP provider, push later).
- Retry with backoff; quiet-hours and digest logic applied at dispatch time so settings changes take effect immediately.

### 4.2 Attention queue (DOC-04)
- Rule evaluators run on write (missed dose threshold, severe symptom, out-of-range vital, missed check-in) via MediatR domain-event notifications — no polling.
- Produces `AttentionItem` rows (PatientId, RuleCode, SourceEntity, CreatedAt, DismissedAt?, DismissNote?).
- Thresholds come from ADM-06 settings, cached with invalidation.

### 4.3 Audit trail (PLT-03)
- ASP.NET Core middleware + a MediatR pipeline behavior write `AuditLog` rows for every authenticated request touching patient data (route ⇒ action mapping), plus explicit entries for sensitive reads (document download, export).
- Table is append-only: no update/delete API, and the DB user for the app has no UPDATE/DELETE grant on it.

### 4.4 Payments (PLT-05)
- Stripe Checkout for subscribe; webhook endpoint (signature-verified) updates `PatientSubscription`.
- Authorization policy `ActiveSubscription` gates doctor-interaction features; tracking features stay available in grace period.

## 5. Security & Authorization

- **Policies:** `PatientOnly`, `DoctorVerified`, `AdminOnly`, `ActiveSubscription`, plus resource handlers:
  - `PatientResourceHandler` — patient can access only `me`-scoped data.
  - `AssignedDoctorHandler` — doctor must hold an active assignment to the target patient (single implementation used by every `/doctor/patients/{id}/*` endpoint).
- **JWT:** 60-min access tokens; refresh tokens rotated on use, revoked on deactivation (ADM-01) and password change.
- **PHI at rest:** SQL Server TDE for the database; column-level encryption for `DiagnosisNote`, `Observation.Text`, `Message.Body`.
- **Files:** private object storage; time-limited signed URLs only; upload virus-scan hook before visibility.
- **Rate limiting** (ASP.NET Core rate limiter): tight on `/auth/*`; anomaly alert on bulk patient reads (PLT-06).
- **Secrets:** user-secrets in dev, environment/key vault in prod. Never in the repo.

## 6. Frontend Architecture Notes

- Standalone components + Signals; `inject()`; typed Reactive Forms; `@if`/`@for` with `track`.
- Route guards per role; lazy-loaded feature areas (`patient/`, `doctor/`, `admin/`).
- `core/realtime.service.ts` wraps SignalR: message receipts + notification badge as signals.
- Charts (adherence, vitals, symptom trends): ng2-charts (Chart.js) — small, sufficient for MVP.
- Timezone rule: API stores UTC + patient-local date where relevant (`DoseSlot.LocalDate`, `DailyCheckIn.LocalDate`); UI always renders in the patient's profile timezone.

## 7. Environments & Deployment

- Same workflow as LMS: LocalDB + `ng serve` for dev; optional `docker-compose.yml` (SQL Server, Redis, API, Angular dev server); EF migrations auto-apply in `Development`.
- Production posture (week 9–10): managed SQL, blob storage for documents, HTTPS-terminating reverse proxy, Hangfire dashboard behind admin auth, Serilog → centralized sink, backups + point-in-time restore verified before launch (patient data!).
