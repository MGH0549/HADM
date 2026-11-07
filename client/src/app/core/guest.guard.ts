import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const guestGuard: CanActivateFn = () => {
  const router = inject(Router);
  return inject(AuthService).token() ? router.createUrlTree(['/today']) : true;
};
