import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const RETURN_URL_PARAM = 'returnUrl';

/** Only same-app paths may be returned to, so a crafted link can't bounce a fresh login to another site. */
export function isSafeReturnUrl(url: string | null | undefined): url is string {
  return !!url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/\\');
}

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(_route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | UrlTree {
    if (this.authService.isAuthenticated()) {
      return true;
    }

    // Remember where the user was heading so login can bring them straight back
    return this.router.createUrlTree(['/login'], { queryParams: { [RETURN_URL_PARAM]: state.url } });
  }
}
