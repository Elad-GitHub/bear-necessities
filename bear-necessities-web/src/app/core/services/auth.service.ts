import { Injectable, signal, computed } from '@angular/core';
import { Observable, finalize, map, shareReplay, tap } from 'rxjs';
import { ApiService } from './api.service';
import {
  AuthUserDto, LoginRequest, LoginResponse, RefreshTokenResponse, RegisterRequest, RegisterResponse
} from '../models/auth.model';
import { Router } from '@angular/router';
import { resolveMediaUrl } from '../utils/media-url';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly USER_KEY = 'auth_user';

  currentUser = signal<AuthUserDto | null>(null);
  isAuthenticated = computed(() => this.currentUser() !== null);
  isAdmin = computed(() => (this.currentUser()?.roles || []).includes('Admin'));
  avatarSrc = computed(() => resolveMediaUrl(this.currentUser()?.avatarUrl));

  private pendingRefresh: Observable<string> | null = null;

  constructor(private api: ApiService, private router: Router) {
    this.loadFromStorage();
  }

  login(req: LoginRequest): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('/auth/login', req).pipe(
      tap(response => {
        localStorage.setItem(this.TOKEN_KEY, response.accessToken);
        localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
        localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
        this.currentUser.set(response.user);
      })
    );
  }

  /** Registration no longer logs the user in — membership requires admin approval first. */
  register(req: RegisterRequest): Observable<RegisterResponse> {
    return this.api.post<RegisterResponse>('/auth/register', req);
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();

    if (refreshToken) {
      this.api.post('/auth/logout', { refreshToken }).subscribe({
        next: () => console.log('Refresh token revoked'),
        error: () => console.log('Logout error (non-critical)')
      });
    }

    this.clearStorage();
    this.router.navigate(['/map']);
  }

  /** Keeps the signed-in user (and its stored copy) in step after they edit their profile. */
  updateCurrentUser(changes: Partial<AuthUserDto>): void {
    const user = this.currentUser();
    if (!user) return;
    const updated = { ...user, ...changes };
    localStorage.setItem(this.USER_KEY, JSON.stringify(updated));
    this.currentUser.set(updated);
  }

  /** Trades the refresh token for a new access token; parallel 401s share one refresh call. */
  refreshAccessToken(): Observable<string> {
    if (!this.pendingRefresh) {
      this.pendingRefresh = this.api.post<RefreshTokenResponse>('/auth/refresh', { refreshToken: this.getRefreshToken() }).pipe(
        tap(response => {
          localStorage.setItem(this.TOKEN_KEY, response.accessToken);
          localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
        }),
        map(response => response.accessToken),
        finalize(() => this.pendingRefresh = null),
        shareReplay(1)
      );
    }
    return this.pendingRefresh;
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  loadFromStorage(): void {
    const userJson = localStorage.getItem(this.USER_KEY);
    const token = localStorage.getItem(this.TOKEN_KEY);

    if (userJson && token) {
      try {
        const user = JSON.parse(userJson);
        this.currentUser.set(user);
      } catch (e) {
        console.error('Failed to parse stored user:', e);
        this.clearStorage();
      }
    } else if (token && !userJson) {
      // Token exists but user data is missing, try to decode from JWT
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const user: AuthUserDto = {
          id: payload.sub || payload.id,
          email: payload.email,
          displayName: payload.displayName || payload.name || '',
          avatarUrl: payload.avatarUrl,
          roles: payload.roles ? (typeof payload.roles === 'string' ? payload.roles.split(',') : payload.roles) : [],
          subscriptionPlan: 'Free'
        };
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
        this.currentUser.set(user);
      } catch (e) {
        console.error('Failed to decode JWT:', e);
        this.clearStorage();
      }
    }
  }

  private clearStorage(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUser.set(null);
  }
}
