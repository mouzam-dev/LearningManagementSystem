# CareBridge — Business Requirements Document (BRD)

> **Working name:** CareBridge (placeholder — rename freely).
> **Product:** A secure UI + API healthcare support platform connecting patients with **supporting doctors** for continuous, day-to-day recovery care alongside — never instead of — the patient's primary consultant.
> **Version:** 0.1 (initial draft) · **Date:** 2026-09-02

---

## 1. Business Concept & Product Vision

The platform provides continuous, personalized medical support alongside a patient's existing primary consultant or family doctor. It **does not replace** the primary doctor, diagnosis, or prescribed treatment; it adds an extra layer of care focused on the patient's day-to-day recovery.

After a patient visits their primary consultant and receives a diagnosis, prescription, or medication plan, the patient uses the platform to track medications, symptoms, vital health information, and daily progress. A dedicated **supporting doctor** monitors this information, follows the patient's recovery journey, provides timely guidance, encourages medication adherence, and identifies situations that may require the patient to consult or return to their primary doctor.

**Revenue model (initial):** subscription or per-patient care-support fee. Positioning: a trusted **"second layer of continuous care"** that helps patients stay on track with their existing treatment — while keeping the primary consultant at the center of medical treatment.

### 1.1 What the product is NOT (hard boundaries)

These boundaries shape features, UI copy, and legal posture:

- **No diagnosis.** Supporting doctors record observations and guidance; the UI never presents their input as a diagnosis.
- **No prescribing.** Prescriptions/medication plans are *recorded* on the platform as issued by the primary consultant; the platform never originates them.
- **No treatment changes.** Any concern that implies a change of treatment produces a recommendation to **consult the primary doctor** (an "escalation"), not a new instruction.
- **Not an emergency service.** Prominent messaging: "If this is an emergency, call your local emergency number." No feature may imply real-time emergency response.

## 2. Actors & Roles

| Role | Description |
|---|---|
| **Patient** | Tracks medications, symptoms, vitals, daily check-ins; communicates with their supporting doctor; manages reminders and subscription. |
| **Supporting Doctor** | Reviews assigned patients' history and daily data, records observations, schedules follow-ups, flags concerns, recommends return-to-consultant escalations. |
| **Admin** | Manages accounts, doctor verification, doctor–patient assignments, subscription plans, platform settings, and audit/compliance reporting. |

A user account has exactly one role. Doctor accounts require admin verification (license details) before activation.

## 3. Modules & Feature Catalog

48 features across 4 modules. IDs are stable — reference them in commits, branches, and test names (`PAT-05`, `DOC-03`, …).

### 3.1 Patient Module (18 features)

| ID | Feature | Description | Acceptance criteria (summary) |
|---|---|---|---|
| PAT-01 | Registration & profile | Register with email/password; profile: demographics, primary consultant info, known conditions, allergies. | Email verified; profile editable; consultant info shown to supporting doctor. |
| PAT-02 | Care plan intake | Record the primary consultant's diagnosis note, prescription(s), and instructions (free text + optional file upload). | Care plan visible to patient + assigned doctor; immutable once confirmed, amendable via new version. |
| PAT-03 | Medication list | Add medications from the recorded prescription: name, dose, form, route, notes. | CRUD before confirmation; changes after confirmation are versioned. |
| PAT-04 | Medication schedule | Per-medication schedule: times/day, with-food flags, start/end dates. | Schedule generates daily dose slots; timezone-aware. |
| PAT-05 | Medication reminders | Push/email reminders per dose slot; snooze; quiet hours. | Reminder fires within ±5 min of slot; snooze re-fires once. |
| PAT-06 | Dose logging (adherence) | Mark each slot taken / skipped (with reason) / taken late. | Adherence % computed daily and over care-plan window. |
| PAT-07 | Daily check-in | One-tap daily wellness entry: overall feeling (1–5), pain (0–10), sleep, appetite, mood, free-text note. | ≤60 seconds to complete; one per day, editable same-day. |
| PAT-08 | Symptom tracking | Log symptoms from a structured list + severity + onset + free text; attach photos. | Symptom timeline renders per symptom; severity trend chart. |
| PAT-09 | Vitals tracking | Manual entry of BP, heart rate, temperature, weight, glucose, SpO₂ (extensible types). | Unit validation and physiological range warnings; charts per vital. |
| PAT-10 | Recovery progress view | Dashboard: adherence %, check-in streak, symptom trend, next follow-up, doctor's latest guidance. | Loads < 2s; empty states for new patients. |
| PAT-11 | Secure messaging | Asynchronous chat with assigned supporting doctor; attachments (images, PDFs ≤10 MB). | Delivery/read state; messages immutable; retention per policy. |
| PAT-12 | Follow-up visibility | See upcoming/past follow-ups scheduled by the doctor; confirm or request reschedule. | State machine: proposed → confirmed → completed/missed. |
| PAT-13 | Escalation notice | When doctor flags "consult your primary doctor", patient sees a prominent, acknowledgeable notice. | Requires explicit acknowledgement; acknowledgement timestamp stored. |
| PAT-14 | Notifications center | In-app list of all notifications (reminders, messages, follow-ups, escalations) with read state. | Unread badge; mark-all-read. |
| PAT-15 | Documents | Upload/view care documents (lab reports, discharge summaries). | Virus-scan hook; only patient + assigned doctor can view. |
| PAT-16 | Subscription management | View plan, subscribe, cancel; payment via provider (Stripe first). | Access to doctor features gated on active subscription; grace period configurable. |
| PAT-17 | Data export | Export own data (care plans, logs, messages) as JSON/PDF. | Complete export delivered ≤ 24h; audit-logged. |
| PAT-18 | Consent & privacy controls | Explicit consent at onboarding for data sharing with supporting doctor; revocable. | Revoking consent unassigns doctor and freezes sharing; recorded in audit log. |

### 3.2 Supporting Doctor Module (14 features)

| ID | Feature | Description | Acceptance criteria (summary) |
|---|---|---|---|
| DOC-01 | Doctor onboarding | Register with credentials (license no., specialty); pending until admin verification. | Unverified doctors cannot access patient data. |
| DOC-02 | Patient roster | List of assigned patients with status chips (stable / needs attention / escalated). | Sorted by attention priority; paginated; search. |
| DOC-03 | Patient detail dashboard | Full patient view: care plan, adherence, check-ins, symptoms, vitals charts, documents, message thread. | All widgets lazy-loaded; date-range filter. |
| DOC-04 | Attention queue | Auto-surfaced items: missed doses (configurable threshold), severe symptoms, out-of-range vitals, missed check-ins. | Queue item links to source data; dismiss-with-note. |
| DOC-05 | Observations | Record timestamped clinical observations against a patient (private-to-care-team or shared-with-patient). | Immutable once saved; amend via addendum. |
| DOC-06 | Guidance notes | Send structured guidance to patient (adherence encouragement, lifestyle notes) — explicitly *not* treatment changes. | Template library; patient ack optional. |
| DOC-07 | Follow-up scheduling | Schedule recurring or one-off follow-ups (message-based or call note); outcomes recorded. | Calendar view; missed follow-ups resurface in attention queue. |
| DOC-08 | Escalation ("back to consultant") | Flag patient with reason + urgency; generates PAT-13 notice and optional summary PDF the patient can take to their consultant. | Escalation lifecycle: raised → acknowledged → resolved (with resolution note). |
| DOC-09 | Secure messaging | Doctor side of PAT-11, with canned responses and per-patient threads. | Same guarantees as PAT-11. |
| DOC-10 | Adherence analytics | Per-patient and roster-level adherence trends. | Roster view: adherence distribution; export CSV. |
| DOC-11 | Care summary generator | Compose a recovery summary (adherence, symptom course, observations) for the primary consultant, exportable as PDF. | Patient must consent per PAT-18 before sharing externally. |
| DOC-12 | Availability & workload | Set availability, max patient capacity; visible to admin for assignment decisions. | Assignment blocked past capacity. |
| DOC-13 | Doctor notifications | Alerts for attention-queue items, new messages, missed follow-ups. | Digest mode (hourly/daily) configurable. |
| DOC-14 | Activity log view | Doctor's own activity trail on each patient (what was viewed/recorded, when). | Read-only; mirrors audit log subset. |

### 3.3 Admin Module (10 features)

| ID | Feature | Description | Acceptance criteria (summary) |
|---|---|---|---|
| ADM-01 | User management | Search, view, activate/deactivate patients and doctors. | Deactivation revokes tokens ≤ 5 min. |
| ADM-02 | Doctor verification | Review license details/documents; approve or reject with reason. | Approval flips DOC account to active; notification sent. |
| ADM-03 | Doctor–patient assignment | Assign/reassign supporting doctor to patient, respecting capacity (DOC-12) and consent (PAT-18). | Reassignment transfers thread visibility per policy; both parties notified. |
| ADM-04 | Subscription plans | CRUD plans (price, billing period, features); manage promo codes. | Plan changes never retroactively bill; audit-logged. |
| ADM-05 | Billing overview | Subscription status per patient, failed payments, churn view. | Reconciles with payment provider webhooks. |
| ADM-06 | Platform settings | Reminder windows, escalation urgency levels, attention-queue thresholds, retention periods. | Settings versioned; effective-from timestamps. |
| ADM-07 | Audit log explorer | Search the immutable audit trail by user, patient, action, date. | Export CSV; access to explorer itself is audit-logged. |
| ADM-08 | Compliance reporting | Data-access reports, consent status report, export/delete request tracking (GDPR/HIPAA-style DSRs). | DSR workflow with due-date tracking. |
| ADM-09 | Operational dashboard | Active patients, adherence platform-wide, escalation counts, message volume, doctor workload. | Refresh ≤ 15 min lag. |
| ADM-10 | Content management | Manage symptom catalog, medication form/route lists, guidance templates, notification templates. | Catalog changes versioned; no breaking of historical logs. |

### 3.4 Platform / Cross-cutting (6 features)

| ID | Feature | Description |
|---|---|---|
| PLT-01 | Authentication & authorization | JWT auth, refresh tokens, role-based policies, account lockout, password reset, optional 2FA (TOTP) for doctors/admins. |
| PLT-02 | Notification engine | Unified engine behind PAT-05/PAT-14/DOC-13: channels (in-app, email, push), templates, scheduling, retry, quiet hours. |
| PLT-03 | Audit trail | Append-only record of every read/write of patient data: who, what, when, from where. Backs DOC-14, ADM-07, ADM-08. |
| PLT-04 | File storage service | Encrypted-at-rest object storage abstraction for documents/photos; signed, time-limited download URLs. |
| PLT-05 | Payment integration | Stripe subscriptions + webhooks (invoice paid/failed, subscription updated/cancelled) driving PAT-16/ADM-05. |
| PLT-06 | API activity records | Request logging with correlation IDs; rate limiting; anomaly alerts (e.g., bulk patient-data reads). |

## 4. Key Workflows

### 4.1 Onboarding & assignment
1. Patient registers (PAT-01), gives consent (PAT-18), records care plan from primary consultant (PAT-02, PAT-03), sets schedules (PAT-04).
2. Patient subscribes (PAT-16).
3. Admin assigns a verified supporting doctor with capacity (ADM-03).
4. Doctor reviews care plan and history (DOC-03), sends welcome guidance (DOC-06), schedules first follow-up (DOC-07).

### 4.2 Daily loop
1. Reminders fire per dose slot (PAT-05); patient logs doses (PAT-06), completes daily check-in (PAT-07), logs any symptoms/vitals (PAT-08/09).
2. Attention rules evaluate new data (DOC-04); items surface in the doctor's queue and notifications (DOC-13).
3. Doctor reviews, records observations (DOC-05), messages or guides the patient (DOC-06/09).

### 4.3 Escalation
1. Doctor raises escalation with reason + urgency (DOC-08).
2. Patient receives prominent notice and acknowledges (PAT-13); optionally downloads consultant summary (DOC-11).
3. Patient visits primary consultant; outcome recorded; escalation resolved; care plan updated as a new version (PAT-02).

## 5. API Surface (v1 sketch)

Base path `/api/v1`. JWT bearer auth on everything except `/auth/*`. Full request/response contracts live with the code (Swagger).

```
POST   /auth/register            POST  /auth/login              POST  /auth/refresh
POST   /auth/forgot-password     POST  /auth/reset-password

GET/PUT  /patients/me                      # profile
POST     /patients/me/care-plans           # PAT-02 (new version)
GET      /patients/me/care-plans
POST/GET /patients/me/medications          # PAT-03
PUT      /patients/me/medications/{id}/schedule
GET      /patients/me/dose-slots?date=     # PAT-06
POST     /patients/me/dose-slots/{id}/log
POST/GET /patients/me/checkins             # PAT-07
POST/GET /patients/me/symptoms             # PAT-08
POST/GET /patients/me/vitals               # PAT-09
GET      /patients/me/progress             # PAT-10 aggregate
GET      /patients/me/escalations          POST /patients/me/escalations/{id}/ack
POST/GET /patients/me/documents            # PAT-15
POST     /patients/me/export               # PAT-17
PUT      /patients/me/consent              # PAT-18

GET      /doctor/patients                  # DOC-02 roster
GET      /doctor/patients/{id}             # DOC-03 dashboard aggregate
GET      /doctor/attention-queue           # DOC-04
POST     /doctor/patients/{id}/observations
POST     /doctor/patients/{id}/guidance
POST/GET /doctor/patients/{id}/follow-ups  PUT /doctor/follow-ups/{id}
POST     /doctor/patients/{id}/escalations # DOC-08
GET      /doctor/patients/{id}/summary.pdf # DOC-11
GET/PUT  /doctor/availability              # DOC-12

GET/POST /conversations/{id}/messages      # PAT-11 / DOC-09

GET/PUT  /admin/users                      # ADM-01
POST     /admin/doctors/{id}/verify        # ADM-02
POST     /admin/assignments                # ADM-03
CRUD     /admin/plans                      # ADM-04
GET      /admin/billing                    # ADM-05
GET/PUT  /admin/settings                   # ADM-06
GET      /admin/audit-logs                 # ADM-07
GET      /admin/reports/*                  # ADM-08/09
CRUD     /admin/catalogs/*                 # ADM-10

GET      /notifications  PUT /notifications/{id}/read   # PAT-14 / DOC-13
POST     /webhooks/stripe                  # PLT-05 (signature-verified, anonymous)
```

## 6. Non-Functional Requirements

| Area | Requirement |
|---|---|
| **Security** | TLS everywhere; passwords BCrypt; PHI encrypted at rest (TDE + column-level for the most sensitive fields); JWT ≤ 60 min + refresh rotation; role + resource-ownership authorization on every endpoint (a doctor can only read *assigned* patients). |
| **Privacy/compliance** | Design to HIPAA/GDPR principles from day 1: consent (PAT-18), audit trail (PLT-03), data minimization, DSR workflows (ADM-08), configurable retention. Jurisdiction-specific certification is a business task, but the technical hooks ship in MVP. |
| **Availability** | 99.5% MVP target; graceful degradation — reminders queue and retry. |
| **Performance** | p95 API < 500 ms; dashboards < 2 s; pagination on all lists. |
| **Auditability** | Every access to patient data attributable to a user + timestamp + purpose (route). Append-only store. |
| **Scalability** | Stateless API; notification engine on a background worker + queue so reminder load never blocks the API. |
| **Localization-ready** | All user-facing strings externalized; timezone-aware scheduling from day 1 (dose slots are the hard case). |

## 7. Out of Scope (MVP)

- Video/tele-consultation (post-MVP; messaging is async-only in MVP).
- Device/wearable integrations (HealthKit, Google Fit) — manual vitals entry first; the vitals model is designed to accept device sources later.
- E-prescription integrations, pharmacy fulfilment, insurance claims.
- Native mobile apps — responsive web first; API is mobile-ready.
- AI-driven insights (adherence prediction, symptom triage) — rule-based attention queue first.

## 8. Success Metrics (first 6 months)

- ≥ 70% of active patients complete a daily check-in ≥ 5 days/week.
- Median medication adherence ≥ 80% among active patients.
- Doctor responds to attention-queue items in < 24 h (median).
- ≥ 90% of escalations acknowledged by patients within 48 h.
- Subscription churn < 8%/month after month 2.
