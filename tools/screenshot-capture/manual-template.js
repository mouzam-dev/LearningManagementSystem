// HTML template for the LMS user manual. Returns a self-contained HTML string
// that consumes screenshots from docs/user-manual/screenshots/<role>/<slug>.png.

const today = new Date().toISOString().slice(0, 10);

const CHAPTERS = [
  {
    role: 'Getting started',
    intro: `The LMS is a web-based learning platform that brings students, teachers, organizations, and administrators onto one site. Anyone can browse the public catalogue; signing in unlocks the experience tailored to your role.`,
    folder: 'public',
    sections: [
      {
        slug: '01-landing-hero',
        title: 'The landing page',
        body: `When you visit the site, you land on the marketing homepage. It headlines the brand promise, a search bar, the number of courses currently published, and quick "Get started" and "Sign in" actions.`,
      },
      {
        slug: '02-landing-courses-grid',
        title: 'Browsing courses without an account',
        body: `Scroll down to see the most popular courses, each with its category, teacher, enrolled count, and an Enroll now button. Clicking Enroll while signed out takes you to the registration form with a return link, so you finish the sign-up and land on that course's enrolment page.`,
      },
      {
        slug: '03-landing-value-props',
        title: 'What you get',
        body: `Further down the page, the value-prop section summarises the four pillars of the LMS: video-first lessons, quizzes and assessments, verifiable certificates, and tools to teach and run cohorts.`,
      },
      {
        slug: '05-register',
        title: 'Creating an account',
        body: `Tap Register from the header or the hero. Fill in your name, email, password, and pick a role (Student or Teacher). After you submit, a verification email is sent and you're signed straight in.`,
      },
      {
        slug: '04-login',
        title: 'Signing back in',
        body: `Already have an account? Click Sign in and enter your email + password. Forgot your password? The Forgot password link below the form starts an email-based reset.`,
      },
    ],
  },
  {
    role: 'For students',
    intro: `Students discover and enrol in courses, work through lessons, take quizzes and assignments, and collect certificates as they complete courses. Everything lives behind the Student dashboard.`,
    folder: 'student',
    sections: [
      {
        slug: '01-dashboard',
        title: 'Your dashboard',
        body: `Signing in as a student lands you here. The dashboard surfaces your in-progress courses, recent announcements from your enrolled courses, and links to keep learning.`,
      },
      {
        slug: '02-catalog',
        title: 'Browse the course catalog',
        body: `Use the catalog to discover everything published on the platform. Filter by category, search by title or description, and tap Enroll now on any card to join the course in one click.`,
      },
      {
        slug: '03-course-detail',
        title: 'Inside a course',
        body: `Opening an enrolled course shows the syllabus (modules and lessons), your overall progress, and the course's announcements and assessments. Tap any lesson to start it.`,
      },
      {
        slug: '04-certificates',
        title: 'Your certificates',
        body: `Finishing a course awards a certificate. The Certificates page lists every certificate you've earned. From here you can preview it, download a PDF copy, or share the public verification link with anyone (an employer, for example) so they can confirm it's genuine.`,
      },
      {
        slug: '06-notifications',
        title: 'Notifications',
        body: `The bell in the header (and the dedicated Notifications page) collects everything aimed at you: new announcements, grading results, enrolment confirmations. Unread items show a coral badge on the bell.`,
      },
      {
        slug: '05-profile',
        title: 'Your profile',
        body: `Manage your name, email, profile picture, and password from the Profile page. Any role can use this page — the change-password flow is shared.`,
      },
    ],
  },
  {
    role: 'For teachers',
    intro: `Teachers build and run their own courses end-to-end: drafting modules and lessons, attaching assessments, grading submissions, and tracking student progress.`,
    folder: 'teacher',
    sections: [
      {
        slug: '01-dashboard',
        title: 'Teacher dashboard',
        body: `Your home base. At a glance: courses you teach, recent submissions waiting for grading, and your active cohorts.`,
      },
      {
        slug: '02-courses-list',
        title: 'My courses',
        body: `Every course you teach (draft, published, or archived) is here. Click any card to open its builder; click New course to start a fresh one.`,
      },
      {
        slug: '04-create-course',
        title: 'Creating a new course',
        body: `Give the course a title, description, category, optional max student count, and a thumbnail. Once saved, you can keep editing inside the course builder and publish whenever it's ready.`,
      },
      {
        slug: '03-course-builder',
        title: 'Course builder',
        body: `This is the workshop. Add modules to structure the syllabus, then add lessons (video, text, or document) inside each module. The right-hand panel collects everything else: Students, Analytics, Edit details, Publish, Archive, Duplicate. The Assessments section below lets you attach quizzes or assignments to the course.`,
      },
      {
        slug: '05-grading-inbox',
        title: 'Grading inbox',
        body: `Submissions from your courses' assessments land here. Click any submission to open the grader — score it against your rubric (if attached), leave feedback, and publish the grade back to the student.`,
      },
      {
        slug: '06-question-bank',
        title: 'Question bank',
        body: `Build a reusable library of questions. When you create an assessment, you can import questions from any bank instead of rewriting them every time.`,
      },
      {
        slug: '07-rubrics',
        title: 'Rubrics',
        body: `Define criteria and point ranges once, then attach the rubric to any assignment. The grading inbox uses the rubric to drive consistent, structured feedback.`,
      },
      {
        slug: '08-profile',
        title: 'Your profile',
        body: `Same profile page every role uses. Update your name, picture, bio, and password.`,
      },
    ],
  },
  {
    role: 'For org admins',
    intro: `Organisation admins look after a single tenant: its branches, the teachers who deliver content there, and the courses scoped to the organisation.`,
    folder: 'orgadmin',
    sections: [
      {
        slug: '01-dashboard',
        title: 'Org admin dashboard',
        body: `Top-line metrics for your organisation: branches, teachers, courses, and students.`,
      },
      {
        slug: '02-branches',
        title: 'Branches',
        body: `Create branches to mirror your physical locations or business units. Teachers and courses can be attached to a specific branch.`,
      },
      {
        slug: '03-teachers',
        title: 'Teachers',
        body: `Invite teachers into your organisation and assign them to branches. From here you can also revoke access if a teacher leaves.`,
      },
      {
        slug: '04-courses',
        title: 'Courses',
        body: `Every course owned by your organisation. Open any course to inspect its modules, enrolments, and metadata. Use this view to keep your catalogue tidy.`,
      },
      {
        slug: '05-profile',
        title: 'Your profile',
        body: `Update your own admin profile and password.`,
      },
    ],
  },
  {
    role: 'For super admins',
    intro: `Super admins operate the whole platform: every organisation, every user, role-permission mappings, audit logs, and reports.`,
    folder: 'superadmin',
    sections: [
      {
        slug: '01-dashboard',
        title: 'Super admin dashboard',
        body: `Platform-wide KPIs: organisations, users by role, courses, recent activity.`,
      },
      {
        slug: '02-organizations',
        title: 'Organisations',
        body: `Create new tenants, see who runs each one, and drill into an organisation to manage its branches, teachers, and courses.`,
      },
      {
        slug: '03-users',
        title: 'All users',
        body: `Search and filter every user on the platform. Filter by role (Students, Teachers, Org admins, Super admins) or status (Active, Suspended). Bulk-import users from CSV with the button top-right.`,
      },
      {
        slug: '04-role-permissions',
        title: 'Roles &amp; permissions',
        body: `Toggle which permissions each role has. Changes apply platform-wide.`,
      },
      {
        slug: '05-courses',
        title: 'All courses',
        body: `Every course across every organisation. Useful for moderation and platform-wide analytics.`,
      },
      {
        slug: '06-reports',
        title: 'Reports',
        body: `Built-in reports cover enrolments, completion rates, engagement, and per-organisation breakdowns.`,
      },
      {
        slug: '07-audit',
        title: 'Audit log',
        body: `Every privileged action on the platform — who did what and when. Use this to investigate incidents or compliance questions.`,
      },
      {
        slug: '08-profile',
        title: 'Your profile',
        body: `Manage your own super-admin account.`,
      },
    ],
  },
];

/**
 * @param {string} screenshotsRoot - Path used as the prefix for <img src>.
 *   Pass an absolute path (e.g. "D:/.../screenshots") when generating the
 *   PDF via a file:// URL — the template will turn it into a file:// img src.
 *   Pass a relative path (e.g. "screenshots") when emitting HTML to be
 *   served from a web origin alongside the screenshots/ folder; the template
 *   will leave it as a relative URL.
 */
function renderHtml(screenshotsRoot) {
  const isAbsolute = /^[a-zA-Z]:[\\/]|^\//.test(screenshotsRoot);
  const css = `
    @page { size: A4; margin: 22mm 18mm; }
    * { box-sizing: border-box; }
    html { font-family: 'Geist', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #2c3540; -webkit-font-smoothing: antialiased; }
    body { margin: 0; line-height: 1.55; font-size: 11pt; }

    /* Cover */
    .cover { page-break-after: always; padding: 18mm 0; }
    .cover .brand { display: flex; align-items: center; gap: 0.6rem; margin-bottom: 6rem; }
    .cover .brand-mark { width: 44px; height: 44px; background: #ee5a1f; color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 22px; border-radius: 6px; }
    .cover .brand-name { font-size: 18pt; font-weight: 800; letter-spacing: -0.02em; }
    .cover h1 { font-size: 42pt; font-weight: 800; line-height: 1.05; letter-spacing: -0.03em; margin: 0 0 1rem; }
    .cover h1 .accent { color: #ee5a1f; }
    .cover .lede { font-size: 13pt; color: #475569; max-width: 28em; margin: 0 0 4rem; }
    .cover .meta { display: flex; gap: 3rem; padding-top: 1.2rem; border-top: 1px solid #e5e7eb; font-size: 9pt; text-transform: uppercase; letter-spacing: 0.08em; color: #6b7280; }
    .cover .meta strong { display: block; font-size: 12pt; color: #2c3540; letter-spacing: -0.01em; text-transform: none; margin-top: 0.25rem; font-weight: 700; }

    /* TOC */
    .toc { page-break-after: always; }
    .toc h2 { font-size: 22pt; margin: 0 0 2rem; font-weight: 800; letter-spacing: -0.02em; }
    .toc ol { padding-left: 1.5rem; margin: 0; }
    .toc ol li { font-size: 12pt; margin-bottom: 0.8rem; font-weight: 600; }
    .toc ol li small { display: block; color: #6b7280; font-weight: 400; font-size: 10pt; margin-top: 0.15rem; }

    /* Chapter title */
    .chapter-title { page-break-before: always; padding-top: 1rem; }
    .chapter-title .eyebrow { display: inline-block; padding: 0.3rem 0.7rem; background: #fff4ee; color: #c04417; font-size: 9pt; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; border-radius: 999px; }
    .chapter-title h2 { margin: 1.25rem 0 1rem; font-size: 28pt; font-weight: 800; letter-spacing: -0.025em; line-height: 1.05; }
    .chapter-title h2 .accent { color: #ee5a1f; }
    .chapter-title p.intro { font-size: 12pt; color: #475569; max-width: 32em; margin: 0; }

    /* Sections */
    section.entry { page-break-inside: avoid; margin-top: 1.5rem; padding-top: 1rem; }
    section.entry:first-of-type { margin-top: 2rem; }
    section.entry h3 { font-size: 15pt; font-weight: 700; letter-spacing: -0.01em; margin: 0 0 0.5rem; }
    section.entry h3 .num { color: #c04417; font-weight: 800; margin-right: 0.5rem; }
    section.entry p { margin: 0 0 1rem; color: #2c3540; }
    section.entry .frame { border: 1px solid #e5e7eb; padding: 6px; border-radius: 6px; background: #fff; }
    section.entry .frame img { display: block; max-width: 100%; height: auto; border-radius: 4px; }
    section.entry .caption { margin-top: 0.5rem; font-size: 9pt; color: #6b7280; font-style: italic; }

    /* Appendix */
    .appendix { page-break-before: always; }
    .appendix h2 { font-size: 22pt; font-weight: 800; letter-spacing: -0.02em; margin: 0 0 1rem; }
    .appendix p { color: #2c3540; }
    .appendix .kv { display: grid; grid-template-columns: max-content 1fr; gap: 0.4rem 1rem; margin: 1rem 0; }
    .appendix .kv dt { color: #6b7280; font-size: 9pt; font-weight: 600; letter-spacing: 0.06em; text-transform: uppercase; padding-top: 0.15rem; }
    .appendix .kv dd { margin: 0; font-size: 11pt; font-weight: 500; }
    .appendix .kv dd code { font-family: 'JetBrains Mono', ui-monospace, Consolas, monospace; font-size: 10pt; background: #f6f7f8; padding: 0.15rem 0.4rem; border-radius: 4px; }

    footer.colophon { margin-top: 4rem; padding-top: 1rem; border-top: 1px solid #e5e7eb; font-size: 8pt; color: #9ca3af; text-align: center; }
  `;

  const cover = `
    <div class="cover">
      <div class="brand">
        <span class="brand-mark">L</span>
        <span class="brand-name">LMS</span>
      </div>
      <h1>LMS<br/><span class="accent">User Manual</span></h1>
      <p class="lede">A guided tour of the Learning Management System for students, teachers, organisation admins, and super admins.</p>
      <div class="meta">
        <div>Version<strong>1.0</strong></div>
        <div>Generated<strong>${today}</strong></div>
        <div>Roles covered<strong>4</strong></div>
      </div>
    </div>
  `;

  const toc = `
    <div class="toc">
      <h2>Table of contents</h2>
      <ol>
        ${CHAPTERS.map((c, i) => `<li>${c.role}<small>${c.sections.length} sections — ${c.intro.slice(0, 110)}…</small></li>`).join('')}
        <li>Appendix<small>Production URL, demo credentials, support pointers</small></li>
      </ol>
    </div>
  `;

  const chapters = CHAPTERS.map((chapter) => {
    const sections = chapter.sections
      .map((s, i) => {
        const rel = `${screenshotsRoot}/${chapter.folder}/${s.slug}.png`;
        const src = isAbsolute ? `file://${rel.replace(/\\/g, '/')}` : rel;
        return `
          <section class="entry">
            <h3><span class="num">${(i + 1).toString().padStart(2, '0')}.</span>${s.title}</h3>
            <p>${s.body}</p>
            <div class="frame"><img src="${src}" alt="${s.title}"></div>
            <p class="caption">${chapter.role} · ${s.title}</p>
          </section>
        `;
      })
      .join('\n');

    return `
      <div class="chapter-title">
        <span class="eyebrow">Chapter</span>
        <h2><span class="accent">${chapter.role.split(' ')[0]}</span> ${chapter.role.split(' ').slice(1).join(' ')}</h2>
        <p class="intro">${chapter.intro}</p>
      </div>
      ${sections}
    `;
  }).join('\n');

  const appendix = `
    <div class="appendix">
      <h2>Appendix</h2>
      <p>Quick references and credentials for anyone evaluating the platform.</p>
      <dl class="kv">
        <dt>Production URL</dt><dd><code>https://lms-app-3195.azurewebsites.net</code></dd>
        <dt>Demo student</dt><dd><code>demo.student@lms.dev</code> / <code>Password1!</code></dd>
        <dt>Demo teacher</dt><dd><code>demo.teacher@lms.dev</code> / <code>Password1!</code></dd>
        <dt>Demo org admin</dt><dd><code>demo.orgadmin@lms.dev</code> / <code>Password1!</code></dd>
        <dt>Demo super admin</dt><dd><code>demo.admin@lms.dev</code> / <code>Password1!</code></dd>
        <dt>API base</dt><dd><code>/api</code> (same-origin in production)</dd>
        <dt>Tech stack</dt><dd>ASP.NET Core 8 · EF Core 8 · SQL Server · Angular 20 · Tailwind · JWT</dd>
      </dl>
      <p>The demo accounts above are intended for evaluation only. Reset them or remove them before going live with real users.</p>
      <footer class="colophon">LMS user manual · generated ${today} · screenshots captured against the local development build.</footer>
    </div>
  `;

  return `<!doctype html>
    <html><head>
      <meta charset="utf-8">
      <title>LMS — User Manual</title>
      <link rel="preconnect" href="https://fonts.googleapis.com">
      <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
      <link href="https://fonts.googleapis.com/css2?family=Geist:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
      <style>${css}</style>
    </head><body>
      ${cover}
      ${toc}
      ${chapters}
      ${appendix}
    </body></html>`;
}

module.exports = { renderHtml };
