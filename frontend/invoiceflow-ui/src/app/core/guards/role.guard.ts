import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Checks if the current user has any of the roles listed in route.data['roles'].
 * Redirects to /dashboard if authenticated but lacks the required role.
 */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const required: string[] = route.data['roles'] ?? [];
  if (required.length === 0 || auth.hasRole(required)) return true;
  return router.parseUrl('/dashboard');
};
