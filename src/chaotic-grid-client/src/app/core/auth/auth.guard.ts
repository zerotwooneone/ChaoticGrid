import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { inject } from '@angular/core';
import { map } from 'rxjs';

import { AuthService } from './auth.service';
import { ApiService } from '../services/api.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const api = inject(ApiService);
  const router = inject(Router);

  if (auth.currentUser()) {
    return true;
  }

  return api.getAuthStatus().pipe(
    map(status => {
      const target = status.isSetupRequired ? '/setup' : '/';
      return router.parseUrl(target) as UrlTree;
    })
  );
};
