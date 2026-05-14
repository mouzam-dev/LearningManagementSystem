import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/guards/auth.guard';

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
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () => import('./home/home').then((m) => m.Home),
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
    ],
  },
  // Public certificate verification — no authGuard, anyone with the code can hit this.
  {
    path: 'verify/:code',
    loadComponent: () =>
      import('./verify/verify-certificate').then((m) => m.VerifyCertificateComponent),
  },
  { path: '**', redirectTo: 'home' },
];
