import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, finalize, map, Observable, of, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { APPROVER_ROLES, AuthResponse, AuthUser, STAFF_ROLES } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly ACCESS_KEY = 'if_access_token';
  private readonly REFRESH_KEY = 'if_refresh_token';
  private readonly USER_KEY = 'if_user';

  private _user = signal<AuthUser | null>(this._loadUser());

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly currentRole = computed(() => this._user()?.role ?? null);
  /** VendorId is only set for Vendor-role users. */
  readonly currentVendorId = computed(() => this._user()?.vendorId ?? null);

  login(email: string, password: string): Observable<void> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, { email, password })
      .pipe(
        tap(res => this._storeSession(res)),
        map(() => void 0),
      );
  }

  logout(): Observable<void> {
    const refreshToken = this.getRefreshToken();
    return this.http
      .post<void>(`${environment.apiBaseUrl}/auth/logout`, { refreshToken })
      .pipe(
        catchError(() => of(void 0)),
        finalize(() => {
          this.clearSession();
          this.router.navigate(['/login']);
        }),
        map(() => void 0),
      );
  }

  refresh(): Observable<void> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      this.clearSession();
      return throwError(() => new Error('No refresh token available'));
    }
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/refresh`, { refreshToken })
      .pipe(
        tap(res => this._storeSession(res)),
        map(() => void 0),
      );
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_KEY);
  }

  /** Returns true if the current user has any of the given role strings. */
  hasRole(roles: string[]): boolean {
    const role = this._user()?.role;
    return role != null && roles.includes(role);
  }

  canApproveReject(): boolean {
    return this.hasRole(APPROVER_ROLES);
  }

  canSubmitCorrect(): boolean {
    return this.hasRole(STAFF_ROLES);
  }

  isAdmin(): boolean {
    return this._user()?.role === 'Admin';
  }

  isVendor(): boolean {
    return this._user()?.role === 'Vendor';
  }

  clearSession(): void {
    localStorage.removeItem(this.ACCESS_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_KEY);
    this._user.set(null);
  }

  private _storeSession(res: AuthResponse): void {
    localStorage.setItem(this.ACCESS_KEY, res.accessToken);
    localStorage.setItem(this.REFRESH_KEY, res.refreshToken);
    const user: AuthUser = { userId: res.userId, email: res.email, role: res.role, vendorId: res.vendorId ?? null };
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this._user.set(user);
  }

  private _loadUser(): AuthUser | null {
    try {
      const stored = localStorage.getItem(this.USER_KEY);
      return stored ? (JSON.parse(stored) as AuthUser) : null;
    } catch {
      return null;
    }
  }
}
