import { Injectable, signal, computed } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { AuthUserDto, LoginRequest, LoginResponse, RegisterRequest, RegisterResponse } from '../models/auth.model';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly USER_KEY = 'auth_user';

  currentUser = signal<AuthUserDto | null>(null);
  isAuthenticated = computed(() => this.currentUser() !== null);

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
