# Project Backlog

Working list of features deferred from the current implementation pass.
Items here are tracked separately from [LMS_BRD_DotNetCore_Angular.docx](LMS_BRD_DotNetCore_Angular.docx)
so we can iterate on scope without re-versioning the BRD on every change.

When the BRD is next refreshed, fold these into the appropriate sections
(Student / Teacher / Admin / Non-functional).

---

## Notifications & messaging (extends STU-053, STU-054, TCH-050)

Path A shipped the in-app notification feed + course announcements + fan-out.
Future extensions:

- **More notification types** — surface system events as notifications so they
  use the same UI plumbing. Suggested types:
  - `grade.posted` — when a teacher grades a submission
  - `assignment.due_soon` — 24h before a deadline (needs a scheduled job)
  - `enrollment.welcome` — first sign-on after enrolling in a course
  - `announcement.updated` — when a teacher edits a pinned post
- **Per-user notification preferences** — new `UserNotificationPreferences`
  table keyed on `(UserId, Type)` with `InApp` + `Email` toggles. Honor in
  `NotificationService` before writing the row.
- **Push notifications** — extend `INotificationService` into a façade that
  also calls an `IPushSender`. Mobile / desktop push later.
- **Real-time delivery (SignalR)** — replace the 30s poll on the bell with a
  WebSocket push so new notifications land instantly. SignalR is already in
  the BRD's tech stack but isn't wired yet.
- **Announcement editing / deletion** — current API is create-only. Add
  PATCH + DELETE on `/api/teacher/courses/{id}/announcements/{id}` for typo
  fixes / takedowns. Edits should NOT re-notify; deletes should soft-delete.
- **Direct messaging (STU-052, TCH-052)** — student↔teacher 1:1 threads
  rather than course-wide broadcasts.
- **Discussion forums (STU-050)** — threaded per-course discussions.

---

## Email & account lifecycle (Path C — IN PROGRESS)

- SMTP-backed `IEmailSender`
- Email verification at registration (STU-002)
- Password reset flow (STU-005)

---

## Org module follow-ups (extends Slice 1)

- **Course tenancy** — denormalize `OrganizationId` + `BranchId` onto `Course`
  so OrgAdmins can moderate their own org's courses without a SuperAdmin
- **Org-scoped student catalog filter** — student catalog gains an org filter
  so students see "all orgs" or just their own
- **Per-user permission overrides** — `UserPermissionOverride` table for
  granting specific perms to a single user without changing role defaults

---

## Other BRD gaps (from audit)

See the audit produced 2026-05-16 — short summary of the bigger missing buckets:

- **Admin system config (5.4)** — institution settings, email config UI,
  notification rules, storage, security policy, maintenance mode
- **Bulk operations** — TCH-043 (bulk enroll), ADM-012 (bulk import users),
  TCH-032 (bulk grading) — all CSV-based
- **Rich teacher tools** — co-instructors (TCH-007), archive/duplicate
  courses (TCH-005/006), question bank (TCH-021), rubric builder (TCH-025),
  content scheduling (TCH-017)
- **Certificates** — PDF download (STU-061), badges (STU-063), social
  sharing (STU-064)
