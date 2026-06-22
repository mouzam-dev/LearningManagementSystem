import { Routes } from '@angular/router';

import { authGuard, guestGuard, publicLandingGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'home' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/register/register').then((m) => m.Register),
  },
  {
    path: 'auth/forgot-password',
    loadComponent: () =>
      import('./auth/forgot-password/forgot-password').then((m) => m.ForgotPasswordPage),
  },
  {
    path: 'auth/reset-password',
    loadComponent: () =>
      import('./auth/reset-password/reset-password').then((m) => m.ResetPasswordPage),
  },
  {
    path: 'auth/verify-email',
    loadComponent: () =>
      import('./auth/verify-email/verify-email').then((m) => m.VerifyEmailPage),
  },
  {
    // Public marketing/landing page — visible to anonymous visitors.
    // Signed-in users are bounced to their role dashboard by publicLandingGuard.
    path: 'home',
    canActivate: [publicLandingGuard],
    loadComponent: () => import('./home/home').then((m) => m.Home),
  },
  // Public course catalog — anyone can browse all published courses with
  // category + teacher filters; enrolling still requires an account.
  {
    path: 'courses',
    loadComponent: () => import('./courses/public-courses').then((m) => m.PublicCoursesPage),
  },
  // Public Deen readers — no auth, so anyone (signed in or not) can read.
  {
    path: 'quran',
    loadComponent: () => import('./quran/quran').then((m) => m.StudentQuranPage),
  },
  {
    path: 'hadith',
    loadComponent: () => import('./hadith/student-hadith').then((m) => m.StudentHadithPage),
  },
  {
    path: 'student',
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./student/dashboard/dashboard').then((m) => m.StudentDashboard),
      },
      {
        path: 'catalog',
        loadComponent: () =>
          import('./student/catalog/catalog').then((m) => m.StudentCatalog),
      },
      {
        path: 'courses/:courseId',
        loadComponent: () =>
          import('./student/course-detail/course-detail').then((m) => m.StudentCourseDetail),
      },
      {
        path: 'lessons/:lessonId',
        loadComponent: () =>
          import('./student/lesson-player/lesson-player').then((m) => m.StudentLessonPlayer),
      },
      {
        path: 'assessments/:assessmentId',
        loadComponent: () =>
          import('./student/assessment/assessment-taker').then((m) => m.StudentAssessmentTaker),
      },
      {
        path: 'certificates',
        loadComponent: () =>
          import('./student/certificates/certificate-list').then((m) => m.StudentCertificateList),
      },
      {
        path: 'certificates/:certificateId',
        loadComponent: () =>
          import('./student/certificates/certificate-detail').then((m) => m.StudentCertificateDetail),
      },
      {
        path: 'attendance',
        loadComponent: () =>
          import('./attendance/student-attendance').then((m) => m.StudentAttendancePage),
      },
      {
        path: 'live-classes',
        loadComponent: () =>
          import('./live-classes/student-live-classes').then((m) => m.StudentLiveClassesPage),
      },
      {
        path: 'quran',
        loadComponent: () => import('./quran/quran').then((m) => m.StudentQuranPage),
      },
      {
        path: 'hadith',
        loadComponent: () => import('./hadith/student-hadith').then((m) => m.StudentHadithPage),
      },
    ],
  },
  {
    path: 'teacher',
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./teacher/dashboard/teacher-dashboard').then((m) => m.TeacherDashboardPage),
      },
      {
        path: 'courses',
        loadComponent: () =>
          import('./teacher/courses/teacher-courses').then((m) => m.TeacherCoursesPage),
      },
      {
        path: 'courses/new',
        loadComponent: () =>
          import('./teacher/courses/create-course').then((m) => m.TeacherCreateCoursePage),
      },
      {
        path: 'courses/:courseId',
        loadComponent: () =>
          import('./teacher/courses/course-builder').then((m) => m.TeacherCourseBuilderPage),
      },
      {
        path: 'courses/:courseId/assessments/new',
        loadComponent: () =>
          import('./teacher/assessments/create-assessment').then((m) => m.TeacherCreateAssessmentPage),
      },
      {
        path: 'assessments/:assessmentId',
        loadComponent: () =>
          import('./teacher/assessments/assessment-editor').then((m) => m.TeacherAssessmentEditorPage),
      },
      {
        path: 'grading',
        loadComponent: () =>
          import('./teacher/grading/grading-inbox').then((m) => m.TeacherGradingInboxPage),
      },
      {
        path: 'submissions/:submissionId',
        loadComponent: () =>
          import('./teacher/grading/submission-grader').then((m) => m.TeacherSubmissionGraderPage),
      },
      {
        path: 'courses/:courseId/students',
        loadComponent: () =>
          import('./teacher/students/course-students').then((m) => m.TeacherCourseStudentsPage),
      },
      {
        path: 'courses/:courseId/students/:studentId',
        loadComponent: () =>
          import('./teacher/students/student-detail').then((m) => m.TeacherStudentDetailPage),
      },
      {
        path: 'courses/:courseId/analytics',
        loadComponent: () =>
          import('./teacher/students/course-analytics').then((m) => m.TeacherCourseAnalyticsPage),
      },
      {
        path: 'question-bank',
        loadComponent: () =>
          import('./teacher/question-bank/question-bank-list').then((m) => m.TeacherQuestionBankListPage),
      },
      {
        path: 'question-bank/new',
        loadComponent: () =>
          import('./teacher/question-bank/question-bank-edit').then((m) => m.TeacherQuestionBankEditPage),
      },
      {
        path: 'question-bank/:id',
        loadComponent: () =>
          import('./teacher/question-bank/question-bank-edit').then((m) => m.TeacherQuestionBankEditPage),
      },
      {
        path: 'rubrics',
        loadComponent: () =>
          import('./teacher/rubrics/rubrics-list').then((m) => m.TeacherRubricsListPage),
      },
      {
        path: 'rubrics/new',
        loadComponent: () =>
          import('./teacher/rubrics/rubric-edit').then((m) => m.TeacherRubricEditPage),
      },
      {
        path: 'rubrics/:id',
        loadComponent: () =>
          import('./teacher/rubrics/rubric-edit').then((m) => m.TeacherRubricEditPage),
      },
      {
        path: 'attendance',
        loadComponent: () =>
          import('./attendance/teacher-attendance').then((m) => m.TeacherAttendancePage),
      },
      {
        path: 'live-classes',
        loadComponent: () =>
          import('./live-classes/teacher-live-classes').then((m) => m.TeacherLiveClassesPage),
      },
      {
        path: 'quran',
        loadComponent: () => import('./quran/quran').then((m) => m.StudentQuranPage),
      },
      {
        path: 'hadith',
        loadComponent: () => import('./hadith/student-hadith').then((m) => m.StudentHadithPage),
      },
    ],
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./admin/dashboard/admin-dashboard').then((m) => m.AdminDashboardPage),
      },
      {
        path: 'organizations',
        loadComponent: () =>
          import('./admin/organizations/organizations-list').then((m) => m.OrganizationsListPage),
      },
      {
        path: 'organizations/:organizationId',
        loadComponent: () =>
          import('./admin/organizations/organization-detail').then((m) => m.OrganizationDetailPage),
      },
      {
        path: 'role-permissions',
        loadComponent: () =>
          import('./admin/role-permissions/role-permissions').then((m) => m.RolePermissionsPage),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./admin/users/admin-users').then((m) => m.AdminUsersPage),
      },
      {
        path: 'users/:userId',
        loadComponent: () =>
          import('./admin/users/admin-user-detail').then((m) => m.AdminUserDetailPage),
      },
      {
        path: 'courses',
        loadComponent: () =>
          import('./admin/courses/admin-courses').then((m) => m.AdminCoursesPage),
      },
      {
        path: 'courses/:courseId',
        loadComponent: () =>
          import('./admin/courses/admin-course-detail').then((m) => m.AdminCourseDetailPage),
      },
      {
        path: 'audit',
        loadComponent: () =>
          import('./admin/audit/admin-audit').then((m) => m.AdminAuditPage),
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./admin/reports/admin-reports').then((m) => m.AdminReportsPage),
      },
      {
        path: 'hadith',
        loadComponent: () =>
          import('./admin/hadith/admin-hadith').then((m) => m.AdminHadithPage),
      },
    ],
  },
  {
    path: 'orgadmin',
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./orgadmin/dashboard/orgadmin-dashboard').then((m) => m.OrgAdminDashboardPage),
      },
      {
        path: 'branches',
        loadComponent: () =>
          import('./orgadmin/branches/orgadmin-branches').then((m) => m.OrgAdminBranchesPage),
      },
      {
        path: 'teachers',
        loadComponent: () =>
          import('./orgadmin/teachers/orgadmin-teachers').then((m) => m.OrgAdminTeachersPage),
      },
      {
        path: 'courses',
        loadComponent: () =>
          import('./orgadmin/courses/orgadmin-courses').then((m) => m.OrgAdminCoursesPage),
      },
      {
        path: 'courses/:courseId',
        loadComponent: () =>
          import('./orgadmin/courses/orgadmin-course-detail').then((m) => m.OrgAdminCourseDetailPage),
      },
      {
        path: 'attendance',
        loadComponent: () =>
          import('./attendance/orgadmin-attendance').then((m) => m.OrgAdminAttendancePage),
      },
    ],
  },
  // Profile — any authenticated role manages their own account here.
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./profile/profile-page').then((m) => m.ProfilePage),
  },
  // Notification center — same idea: any signed-in role can see their own feed.
  {
    path: 'notifications',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./notifications/notifications-page').then((m) => m.NotificationsPage),
  },
  // Support / user manual — same: any signed-in role can read the manual
  // for every role, since the doc covers the whole product.
  {
    path: 'support',
    canActivate: [authGuard],
    loadComponent: () => import('./support/support-page').then((m) => m.SupportPage),
  },
  // Public certificate verification — no authGuard, anyone with the code can hit this.
  {
    path: 'verify/:code',
    loadComponent: () =>
      import('./verify/verify-certificate').then((m) => m.VerifyCertificateComponent),
  },
  { path: '**', redirectTo: 'home' },
];
