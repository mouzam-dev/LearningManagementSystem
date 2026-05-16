# LMS — User Manual

**Version 1.0 · May 2026**

A complete guide to using the Learning Management System for every role: Students, Teachers, Organization Administrators, and Super Administrators. This manual is generated from the live application and reflects the shipped behavior of the LMS as of May 2026.

---

## Table of contents

1. [Overview](#1-overview)
2. [Getting started](#2-getting-started)
3. [Your account](#3-your-account)
4. [Student guide](#4-student-guide)
5. [Teacher guide](#5-teacher-guide)
6. [Organization Administrator guide](#6-organization-administrator-guide)
7. [Super Administrator guide](#7-super-administrator-guide)
8. [Notifications](#8-notifications)
9. [Certificates](#9-certificates)
10. [Appendix](#10-appendix)

---

## 1. Overview

The LMS is a multi-tenant learning platform organized around four roles. Each role sees a different application surface and has different permissions.

### Roles at a glance

| Role | What they do |
|---|---|
| **Student** | Browses a course catalog, enrolls in courses, watches lessons, takes assessments, earns certificates. |
| **Teacher** | Builds courses (modules, lessons, assessments), grades submissions, posts announcements, tracks student progress. |
| **Organization Administrator (OrgAdmin)** | Manages a single organization — its branches, its teachers, and moderates the courses produced inside it. |
| **Super Administrator (SuperAdmin)** | Platform-wide control — manages all organizations, all users, role permissions, audit logs, and reports. |

### Tenancy model

Everything in the LMS belongs to a tenancy hierarchy:

```
Organization
└── Branch
    ├── Teacher → owns Courses
    └── Student → enrolls in Courses
```

- **Super Administrators** have no organization or branch — they sit above tenancy and can see everything.
- **Organization Administrators** belong to an organization but not a specific branch — they see every branch inside their org.
- **Teachers and Students** belong to both an organization and a branch.

Courses also carry an OrganizationId and BranchId, denormalized from the owning teacher. That means OrgAdmins can scope moderation queries to their own org without joining through users.

### What you can build

A typical organization runs like this:

1. The **SuperAdmin** creates the organization and a default Org Admin account.
2. The **Org Admin** sets up branches (locations or business units) and invites teachers.
3. **Teachers** build courses inside the organization, add modules and lessons, and publish them.
4. **Students** browse the catalog, enroll, work through the lessons, take quizzes, and earn certificates.

---

## 2. Getting started

### Reaching the app

The application runs at:

- **Web app (development):** `http://localhost:4201`
- **API (development):** `http://localhost:5117`
- **API documentation:** `http://localhost:5117/swagger`

In production, your administrator will give you the URL.

### Signing in

1. Open the application URL in your browser.
2. You will land on the **Sign in** page.
3. Enter your email and password.
4. Click **Sign in**.

After sign-in, you are redirected to the dashboard for your role.

### Demo accounts

The seeded development database includes the following demo logins. The password for every demo account is `Password1!`.

| Email | Role | Organization |
|---|---|---|
| `demo.admin@lms.dev` | Super Administrator | (platform-wide) |
| `demo.orgadmin@lms.dev` | Organization Administrator | Pioneer Tech Academy |
| `demo.teacher@lms.dev` | Teacher | Pioneer Tech Academy |
| `demo.student@lms.dev` | Student | Pioneer Tech Academy |
| `admin@pioneer-tech.example` | Org Admin | Pioneer Tech Academy |
| `admin@global-business.example` | Org Admin | Global Business School |
| `admin@codeforge.example` | Org Admin | CodeForge Bootcamp |

There are also ~75 additional teachers and students seeded across the three organizations. See section 7.1 for how to browse them as a Super Administrator.

### Registering a new account

If you don't have an account yet, click **Register** in the top-right corner of the sign-in page.

The registration form collects:

- **First name** and **Last name**
- **Email** (used to sign in)
- **Password** — must be at least 8 characters and contain an uppercase letter, a lowercase letter, and a digit
- **Role** — either Student or Teacher (Org Admin and Super Admin accounts are created administratively, never via self-signup)

New accounts land in the platform's **Default Organization / Default Branch** until an administrator moves them to a real tenancy.

### Forgotten password

If you've forgotten your password:

1. From the sign-in page, click **Forgot password?**
2. Enter your email and click **Send reset link**.
3. Open the email and click the reset link.
4. Set a new password.

If you don't receive the email, check your spam folder and confirm the email address is correct.

### Email verification

When you register, a verification email is sent. Some features (like enrolling in paid courses or receiving notifications) require a verified email. To verify:

1. Open the email titled "Verify your LMS email address."
2. Click the verification link.
3. You will be redirected back to the LMS with your email marked verified.

You can request a new verification email from the **Profile** page at any time.

### Signing out

Click your name in the top-right of the navigation bar, then **Sign out**.

---

## 3. Your account

Every authenticated role has access to the **Profile** page from the top-right user chip in the navigation bar. The profile is split into two cards:

### 3.1 Your details

Edit your visible identity:

- **First name** and **Last name** — appear on your dashboard, in instructor lists, and on certificates.
- **Bio** (optional) — a short description (up to 1000 characters) shown to teachers and other learners.
- **Avatar** — your profile picture.

To set or change your avatar:

1. Click **Choose image** under the Avatar field.
2. Select an image from your computer (PNG, JPG, WebP, GIF, or SVG, up to 5 MB).
3. The image uploads and previews immediately.
4. Click **Save changes** to persist.

To remove your current avatar, click **Remove** next to the preview and then **Save changes**.

### 3.2 Password

To change your password:

1. Enter your **Current password**.
2. Enter your **New password** (8+ characters, with at least one uppercase, one lowercase, and one digit).
3. Confirm the new password.
4. Click **Change password**.

A confirmation banner appears when the change succeeds. You stay signed in on this device.

### 3.3 Theme

The LMS supports light and dark themes. Click the sun/moon icon in the top-right of the navigation bar to toggle. Your preference is remembered on the device.

### 3.4 Notifications

See section 8 for the notifications center.

---

## 4. Student guide

This section walks through everything a student does, from first sign-in through earning a certificate.

### 4.1 The dashboard

After signing in, students land on the **Student Dashboard** at `/student/dashboard`. The dashboard shows:

- **Three KPIs at the top** — Enrolled courses, Overall progress, Completed courses
- **My courses** — a tile grid of every course you're enrolled in, each with a progress bar and a Continue button
- **Upcoming deadlines** — a table of assessments due soon
- **Latest announcements** — a feed of announcements from teachers across your enrolled courses

Click **Continue** on any course tile to jump back into where you left off.

### 4.2 Browsing the catalog

To find a new course, click **Browse courses** in the top navigation. The catalog at `/student/catalog` shows every published course you're eligible to enroll in.

The catalog supports:

- **Search by title** — type into the search box; results filter as you type
- **Filter by category** — click a category chip (e.g. Frontend, Data, Leadership) to narrow down
- **Sort** — newest first, most popular, or alphabetical

Click any course card to open its detail page.

### 4.3 Viewing a course

The course detail page shows:

- The **course title, description, and thumbnail**
- The **instructor's name and bio**
- A list of **modules and lessons**, with totals (e.g. "3 modules · 14 lessons")
- A list of **assessments** (quizzes and assignments)
- An **Enroll** button (if you're not yet enrolled)

If you're already enrolled, the Enroll button is replaced by a **Continue learning** button that takes you to the next unfinished lesson.

### 4.4 Enrolling in a course

1. Open the course detail page.
2. Click **Enroll**.
3. The page refreshes; the course now appears on your dashboard.

A confirmation toast appears, and you can immediately start the first lesson.

Some courses have an enrollment cap (set by the teacher). If the cap is reached, the Enroll button shows "Full" and is disabled.

### 4.5 Taking lessons

Click any lesson in the course outline (or **Continue** from the dashboard) to open the **Lesson player**.

Lesson types:

- **Video lessons** — an embedded video player with standard controls (play, pause, seek, fullscreen). Your watch position is saved automatically; reopening the lesson resumes where you left off.
- **Text lessons** — formatted reading content. Scroll through; "Mark as complete" appears at the bottom.
- **Document lessons** — a downloadable file (PDF, Word, PowerPoint). Click **Download document** to save it locally.
- **Quiz lessons** — see section 4.6.

When you finish a lesson, click **Mark as complete**. The lesson's progress bar fills, and the overall course progress updates.

#### Lesson player controls

| Control | Function |
|---|---|
| Previous lesson | Jump to the previous lesson in the module |
| Next lesson | Jump to the next lesson (only enabled after current is complete) |
| Module outline | Toggle a sidebar listing every lesson in the course |
| Mark complete | Record completion of the current lesson |

### 4.6 Taking quizzes and assessments

When you reach an assessment (a Quiz lesson, or a standalone assessment listed on the course page), click **Start**.

The assessment player shows:

- The **assessment title** and **time limit** (if set)
- The **number of questions** and **passing score**
- One question at a time, with Next/Previous navigation
- A **progress indicator** at the top showing which question you're on

#### Question types

- **Multiple choice (MCQ)** — select one option
- **True / False** — select True or False
- **Short answer** — type a one-line response
- **Essay** — type a free-form response in a textarea

#### Submitting

When you've answered every question, click **Submit assessment**. You see your score and a breakdown of correct/incorrect answers. If the assessment had a passing score, you see a Pass or Fail badge.

#### Multiple attempts

If the assessment allows multiple attempts (set by the teacher), the **Retake** button appears after submission. Your best score is kept.

#### Time limit

If the assessment has a time limit, a countdown appears in the top-right. When time runs out, the assessment auto-submits with your current answers.

### 4.7 Tracking your progress

Your overall progress in a course is visible in three places:

- The **dashboard** course tile (percentage bar)
- The **course detail** page (top of the page)
- The **lesson player** outline (a check mark next to every completed lesson)

When you reach 100%, the course is marked Completed and a certificate is automatically issued (see section 9).

### 4.8 Course announcements

Teachers can post announcements pinned to a course. They appear:

- On your **dashboard** under **Latest announcements**
- On the **course detail** page in a panel below the description
- In your **notifications** center (with a red badge on the bell icon)

---

## 5. Teacher guide

Teachers are the content creators. This section covers everything a teacher does: building courses, managing modules and lessons, creating assessments, grading submissions, posting announcements, and tracking students.

### 5.1 The Teacher dashboard

At `/teacher/dashboard`, you see:

- **Four KPI tiles** — Courses, Students, Completions, To grade
- **Your top courses** — a list of your most-enrolled courses
- **Recent enrollments** — students who signed up in the last 7 days
- **Waiting to grade** — assignment submissions that need your attention

The **To grade** tile is highlighted in amber when there are submissions waiting; if there are zero, it shows "All caught up 🎉".

### 5.2 Your courses

Click **My courses** in the top nav to land at `/teacher/courses`. Every course you own appears here, with:

- Status pill (**Published** in green, **Draft** in amber)
- Title, category, and short description
- Stats: modules, lessons, students enrolled
- Last updated date

Click any course to open the **Course builder** (section 5.4).

### 5.3 Creating a course

1. Click **+ New course** in the top-right of the My Courses page.
2. Fill in:
   - **Title** (required, up to 200 chars)
   - **Description** (required, up to 4000 chars)
   - **Category** (required, e.g. Frontend, Data, Leadership)
   - **Max students** (optional — leave blank for unlimited)
   - **Thumbnail** (optional — upload an image; PNG/JPG/WebP/GIF/SVG up to 5 MB)
3. Click **Create draft**.

The course is created as a draft. You won't be able to enroll students until you publish it (section 5.6).

### 5.4 The Course builder

Opening any course takes you to `/teacher/courses/{id}` — the **Course builder**. The page is divided into four areas:

1. **Header** — course title, status, and a **Publish/Unpublish** toggle
2. **Course metadata** — Edit button to update title, description, category, thumbnail, max students
3. **Modules** — the spine of the course, ordered by drag-and-drop
4. **Assessments** — quizzes and assignments attached to this course

To edit course metadata, click **Edit** in the metadata card. The same fields as the create form open in a modal. Save to apply changes.

### 5.5 Modules and lessons

A course is organized as **Modules → Lessons**.

#### Add a module

1. In the Course builder, click **+ Add module**.
2. Enter a title and an optional description.
3. Click **Save**.

The new module appears at the bottom of the list. You can drag modules to reorder.

#### Add a lesson

1. Click the **+ Add lesson** button inside the target module.
2. Enter:
   - **Title** (required)
   - **Type** — Video, Text, Document, Quiz, or Assignment
   - **Duration** (minutes, optional)
   - **Published** (checkbox — uncheck to hide from students)
3. Fill the type-specific content:
   - **Video** — enter a video URL (YouTube, Vimeo, or a direct MP4 link)
   - **Text** — write the body in the inline editor (Markdown-supported)
   - **Document** — click **Upload document** and select a file (PDF, DOC, DOCX, PPT, PPTX, TXT, up to 20 MB)
   - **Quiz / Assignment** — these reference an Assessment (see section 5.7)
4. Click **Save**.

You can drag lessons within a module to reorder them, and drag a lesson into a different module.

#### Edit a lesson

Click the lesson title to expand its editor, change anything, and click **Save**.

#### Delete a lesson or module

Click the trash icon next to the lesson or module. A confirmation prompt asks you to confirm. Deletions are permanent — cascade removes any student progress for that lesson.

### 5.6 Publishing a course

A course must be published before students can enroll. From the Course builder:

1. Confirm every module has at least one published lesson.
2. Click **Publish** in the header.
3. The status pill flips from Draft to Published.

You can unpublish at any time (e.g. while making major changes). Unpublishing hides the course from the catalog but does **not** unenroll students or delete their progress.

### 5.7 Assessments (quizzes and assignments)

Assessments are graded items: quizzes (auto-graded MCQ/True-False/Short answer) or assignments (manually graded essays / file submissions).

#### Create an assessment

1. From the Course builder, scroll to the **Assessments** section.
2. Click **+ New assessment**.
3. Enter:
   - **Title** (e.g. "End-of-course assessment")
   - **Type** — Quiz or Assignment
   - **Time limit** in minutes (optional)
   - **Passing score** (0–100)
   - **Max attempts** (optional)
   - **Due date** (optional)
4. Click **Create**.

You land on the **Assessment editor** for the new assessment.

#### Add questions

In the Assessment editor:

1. Click **+ Add question**.
2. Choose a type:
   - **MCQ** — type the question, add 2–6 options, mark the correct one
   - **True/False** — type the question, mark True or False
   - **Short answer** — type the question and the expected answer (case-insensitive match)
   - **Essay** — type the question; grading is manual
3. Set **Points** (default 1).
4. Save.

Drag question rows to reorder. Click a row to edit. Trash icon to delete.

#### Preview an assessment

Click **Preview** to see how students will experience the assessment.

### 5.8 Grading submissions

When a student submits an assignment (or an essay question on a quiz), it lands in your **Grading inbox** at `/teacher/grading`.

The inbox shows:

- Student name
- Assessment title and course
- Submitted time
- Status (Pending grading)

#### Grade a submission

1. Click the submission to open the **Submission grader**.
2. Review the student's response.
3. For essay or short-answer questions, enter a score per question and optional feedback.
4. For file uploads, click the file to download and review it locally.
5. Enter overall feedback (optional).
6. Click **Submit grade**.

The student is notified, and the score is recorded against their enrollment.

#### Bulk grading

For assessments where every question is auto-gradable (MCQ, True/False, short-answer), submissions are graded instantly — they never appear in the inbox.

### 5.9 Course announcements

Pin announcements to a course to broadcast to all enrolled students.

1. Open the course in the Course builder.
2. Scroll to the **Announcements** panel.
3. Click **+ New announcement**.
4. Enter a **Title** and **Body** (Markdown supported).
5. Optionally check **Pin to top**.
6. Click **Post**.

Every enrolled student gets a notification (bell icon + email if their preferences allow). The announcement also appears on their dashboard.

To edit or delete an announcement, click the kebab menu (⋯) on the announcement card.

### 5.10 Course students and analytics

Click **Students** on a course to open `/teacher/courses/{id}/students`. You see a table of every enrolled student with:

- Name, email
- Progress percentage
- Last activity
- Enrollment date

Click a student to open their per-course detail — a per-lesson completion breakdown and their submission history.

Click **Analytics** to see course-level metrics: enrollment count, completion rate, average progress, lesson drop-off curve, and assessment pass rates.

---

## 6. Organization Administrator guide

Organization Administrators manage a single tenant — its branches, its teachers, and they moderate the courses produced inside it. OrgAdmins cannot create courses themselves; that's the teacher's job.

### 6.1 The OrgAdmin dashboard

At `/orgadmin/dashboard`, you see:

- **Organization name and description**
- **Five KPI tiles** — Branches, Teachers, Students, Courses, Published
- **Branches** panel — every branch in your org with its teacher/student counts and active status

The **Courses** and **Published** tiles are clickable shortcuts to the courses page.

### 6.2 Managing branches

A **Branch** is a location, department, or business unit inside your organization (e.g. "HQ — San Francisco", "EU Remote").

Navigate to **Branches** in the top nav, or click "Manage branches →" from the dashboard.

The Branches page lists every branch in your org.

#### Add a branch

1. Click **+ Add branch**.
2. Fill in:
   - **Name** (required, e.g. "London Campus")
   - **Code** (optional, short identifier like "LON")
   - **Location** (optional, e.g. "London, UK")
   - **Contact email** (optional)
3. Click **Save**.

#### Edit a branch

Click **Edit** on any branch row. You can change every field and toggle **Active**. Inactive branches still exist but new users can't be assigned to them.

### 6.3 Managing teachers

Click **Teachers** in the top nav to land at `/orgadmin/teachers`.

The page lists every teacher in your org with their branch assignment, course count, and active status.

#### Add a teacher

1. Click **+ Add teacher**.
2. Fill in:
   - **First name**, **Last name**
   - **Email** (used to sign in)
   - **Temporary password** — the teacher should change it on first sign-in
   - **Branch** — assign to one of your branches
3. Click **Create teacher**.

The teacher can now sign in. They receive a welcome email if email is configured.

#### Reassign a teacher to a different branch

1. Click the teacher row.
2. Click **Reassign branch**.
3. Pick the target branch and confirm.

When a teacher is moved, all of their courses move with them — branch chips on courses update automatically.

#### Filter and search

Use the search box to find a teacher by name or email. Use the **Branch** dropdown to filter the list.

### 6.4 Course moderation

Click **Courses** in the top nav to land at `/orgadmin/courses`.

This page lists every course authored by a teacher in your organization. You can:

- Search by course title
- Filter by status (All / Published / Draft)
- Filter by branch

Click any course to open the **Course detail** page.

#### Force unpublish a course

If a course is published but you need to take it offline (content issue, complaint, compliance), click **Force unpublish** on its detail page. The course is hidden from the catalog immediately; students who are already enrolled keep their progress but can't access new lessons.

Click **Publish course** to put it back live.

Every unpublish and republish is recorded in the audit log.

#### Delete a course

Click **Delete course** on the detail page. A confirmation prompt warns about cascade deletion: modules, lessons, assessments, questions, submissions, enrollments, and progress are all permanently removed. **This cannot be undone.**

Use Force Unpublish first if you might want the course back later.

---

## 7. Super Administrator guide

Super Administrators have platform-wide access — every organization, every user, every course. They also configure role permissions, view the audit log, and run reports.

### 7.1 The Admin dashboard

At `/admin/dashboard`, you see:

- **Headline KPIs** — Total users (active/suspended split), New this week, Total courses (published/draft), Total submissions (with ungraded count highlighted)
- **Users by role** — horizontal bar chart of Students vs Teachers vs Admins
- **Learning activity** — Enrollments, Completions, Certificates, Completion rate
- **Top courses by enrollment**
- **Recent registrations**

### 7.2 Managing organizations

Click **Organizations** in the top nav.

The list page shows every organization with its slug, branch count, user count, and active status.

#### Create an organization

1. Click **+ New organization**.
2. Fill in:
   - **Name** (required)
   - **Slug** (optional URL identifier, lowercase letters/digits/hyphens)
   - **Contact email** (optional)
   - **Description** (optional, up to 2000 characters)
3. Click **Save**.

After creation, you land on the organization detail page where you can:

- Upload a **Logo** (PNG/JPG/WebP/GIF/SVG up to 5 MB)
- Add **Branches**
- Create the first **Organization Administrator** for this org

#### Edit an organization

Click **Edit** on the header card. You can change every field, replace the logo, or toggle **Active**.

#### View an organization

The detail page shows:

- Header card with logo, name, slug, description, KPI strip (Branches, Org admins, Teachers, Students)
- **Branches** panel — add, edit, deactivate branches
- **Organization admins** panel — see who manages this org

### 7.3 Managing users

Click **Users** in the top nav to land at `/admin/users`. This is a paginated, searchable, filterable list of every user on the platform.

#### Filters

- **Search** by name or email
- **Role** — Student, Teacher, OrgAdmin, SuperAdmin
- **Status** — Active / Suspended

#### User detail

Click any user to open `/admin/users/{userId}`. You see:

- Identity strip (avatar, name, email, role, organization, branch)
- Learning stats (courses taught or enrolled, completions, certificates)
- Activity stats (submissions, last sign-in)
- Action buttons (described below)

#### Suspend / reactivate a user

Click **Suspend** to disable the account. The user can no longer sign in; their data is preserved. Click **Reactivate** to restore access.

#### Change a user's role

Click **Change role**. Pick the new role:

- **Student** — requires Organization + Branch
- **Teacher** — requires Organization + Branch
- **OrgAdmin** — requires Organization (no branch)
- **SuperAdmin** — no tenancy

Role changes are audited.

#### Transfer a user

For Teachers and Students only, click **Transfer**. Pick the target Organization, then a Branch within that org. The user is moved; for teachers, all of their courses move with them.

### 7.4 Roles and Permissions

Click **Roles & Permissions** to land at `/admin/role-permissions`. This page lets you customize what each role can do beyond the defaults.

The matrix has roles across the top (SuperAdmin, OrgAdmin, Teacher, Student) and permission codes down the side. Check a cell to grant; uncheck to revoke.

#### Permission codes

| Code | What it allows |
|---|---|
| `org.read` | View organizations |
| `org.create` | Create new organizations |
| `org.update` | Edit organizations |
| `org.delete` | Delete organizations |
| `branch.read` | View branches |
| `branch.create` | Create branches |
| `branch.update` | Edit branches |
| `branch.delete` | Delete branches |
| `orgadmin.create` | Create org admins |
| `orgadmin.list` | List org admins |
| `teacher.create` | Create teachers within an organization |
| `teacher.list` | List teachers within an organization |
| `course.moderate` | Unpublish / delete courses inside an organization |

Changes take effect immediately. Existing sessions need to sign out and back in to pick up new permissions.

### 7.5 Course moderation (platform-wide)

Click **Courses** in the top nav to land at `/admin/courses`. Same UI as the OrgAdmin courses page but scoped to **every** course on the platform.

You can force-unpublish or delete any course, regardless of which organization owns it. Every action is audited.

### 7.6 The audit log

Click **Audit log** to land at `/admin/audit`. Every administrative action is recorded with:

- Timestamp
- Actor (who did it)
- Action (`user.suspended`, `course.deleted`, `user.transferred`, etc.)
- Entity type and ID
- A JSON diff of what changed

Filter by entity type to investigate a specific area (e.g. "User" for account changes, "Course" for moderation events).

### 7.7 Reports

Click **Reports** to land at `/admin/reports`. The reports page shows:

- **Registration trend** — monthly student/teacher signups
- **Enrollment trend** — monthly enrollment counts
- **Categories** — per-category course count, enrollments, completion rate
- **Top teachers** — by published courses and total students
- **Completion funnel** — Enrolled → Started → Completed → Certified

#### Exports

Two CSV exports are available:

- **Users CSV** — every user with name, email, role, status, verified flag, joined date
- **Courses CSV** — every course with title, category, teacher, published status, modules/lessons/students counts, created date

Click the **Download CSV** buttons to save them. Files are UTF-8 with BOM so Excel opens accented characters correctly.

---

## 8. Notifications

Every role has access to the **Notifications** center via the bell icon in the top-right of the navigation bar.

A red badge on the bell shows the count of unread notifications.

### Notification types

- **Course announcements** — when a teacher posts to a course you're enrolled in
- **Grading complete** — when your assignment has been graded
- **Enrollment confirmations** — when you successfully enroll
- **System messages** — account changes, role updates, etc.

### Reading and acting on notifications

Click the bell to open the dropdown of recent notifications. Click any notification to:

- Mark it as read
- Navigate to the relevant page (the linked course, your graded submission, etc.)

Click **View all** at the bottom of the dropdown to land at `/notifications` for a full paginated list.

### Marking all as read

On the notifications page, click **Mark all as read**.

### Notification preferences (per-user)

Per-type opt-out is on the roadmap. Currently every user receives every notification type their role triggers.

---

## 9. Certificates

When a student reaches 100% progress in a course, a certificate is automatically issued.

### Viewing your certificates

As a student, click **Certificates** in the top navigation to land at `/student/certificates`. Every certificate you've earned appears as a card showing:

- Course title and teacher
- Issue date
- Verification code

### Certificate detail

Click any certificate to open the detail view. The page shows a printable certificate with your name, course title, completion date, and the verification code.

### Public verification

Every certificate has a public verification URL: `/verify/{code}`. Anyone with the code can confirm the certificate is authentic (without needing to sign in). This is useful for sharing on LinkedIn or in job applications.

The verification page shows:

- "Certificate verified ✓"
- Student name
- Course title
- Issue date
- Issuing organization (if the course belonged to one)

### PDF download

PDF download of certificates is on the roadmap. For now, use your browser's Print → Save as PDF.

---

## 10. Appendix

### 10.1 Keyboard shortcuts

| Shortcut | Action |
|---|---|
| `Esc` | Close any open modal or dropdown |
| `/` | Focus the search box (where available) |
| `Ctrl/Cmd + K` | Open quick navigation (roadmap) |

### 10.2 Browser support

The LMS is tested on the latest versions of:

- Chrome / Edge
- Firefox
- Safari

Internet Explorer is not supported.

### 10.3 Mobile

The app is fully responsive — every screen works on phones and tablets. The navigation bar collapses to a hamburger menu below 768px. Lesson video players use the browser's native fullscreen on touch devices.

### 10.4 Tech stack

For administrators and developers:

| Layer | Technology |
|---|---|
| Backend | .NET 8 (ASP.NET Core 8 Web API) |
| ORM | Entity Framework Core 8 |
| Database | SQL Server 2022 |
| Auth | JWT (Bearer tokens), BCrypt password hashing |
| Validation | FluentValidation |
| Pattern | CQRS via MediatR |
| Frontend | Angular 20 |
| Styling | Tailwind CSS |
| State | Angular Signals + RxJS |

See `CLAUDE.md` in the repository root for the full architecture documentation.

### 10.5 Common error messages

| Message | What it means | What to do |
|---|---|---|
| "Invalid credentials." | Email or password was wrong | Retype carefully; use Forgot Password if needed |
| "Your account has been suspended." | An administrator suspended you | Contact your Org Admin or Super Admin |
| "Cannot reach the API." | The backend is down or unreachable | Refresh; if it persists, contact your administrator |
| "This course is full." | Enrollment cap reached | Ask the teacher to raise the cap, or wait for a slot |
| "File is too large — the limit is 5 MB." | Image upload over the limit | Compress the image or pick a smaller one |

### 10.6 Demo data overview

When you sign in to the development environment, you'll see:

- **3 organizations** — Pioneer Tech Academy, Global Business School, CodeForge Bootcamp
- **6 branches** (2 per organization)
- **~80 users** (3 org admins, ~15 teachers, ~60 students)
- **15 courses** (12 published, 3 drafts)
- **~180 enrollments** with mixed progress and ~30 completions
- **~30 certificates** issued

Every demo password is `Password1!`. See section 2 for the seeded demo accounts.

### 10.7 Support and feedback

- **GitHub issues** — report bugs or request features at the project repository
- **In-app feedback** — coming soon

---

*End of manual — version 1.0, May 2026. The LMS is built and maintained by the Pioneer Tech Academy engineering team.*
