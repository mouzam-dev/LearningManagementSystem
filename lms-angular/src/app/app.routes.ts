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
    ],
  },
  { path: '**', redirectTo: 'home' },
];
