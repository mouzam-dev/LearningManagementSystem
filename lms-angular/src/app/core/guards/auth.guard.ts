import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../../auth/auth.service';

/** Role-aware landing page — where a signed-in user belongs by default. */
export function landingPathForRole(role: string | undefined): string {
  switch (role) {
    case 'Student':
      return '/student/dashboard';
    case 'Teacher':
      return '/teacher/dashboard';
    case 'OrgAdmin':
      return '/orgadmin/dashboard';
    case 'SuperAdmin':
      return '/admin/dashboard';
    default:
      return '/home';
  }
}

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // An already-signed-in user hitting /login or /register goes straight to
  // their role's dashboard rather than the generic landing card.
  return auth.isAuthenticated()
    ? router.createUrlTree([landingPathForRole(auth.user()?.role)])
    : true;
};

/**
 * Public marketing/landing page guard. Anonymous visitors get the page;
 * already-signed-in users are bounced to their role dashboard so they don't
 * see the marketing CTAs.
 */
export const publicLandingGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated()
    ? router.createUrlTree([landingPathForRole(auth.user()?.role)])
    : true;
};
