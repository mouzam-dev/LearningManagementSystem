import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  AuthResponse,
  LoginRequest,
  MeResponse,
  RegisterRequest,
  User,
} from './models/auth.models';

const ACCESS_TOKEN_KEY = 'lms.accessToken';
const REFRESH_TOKEN_KEY = 'lms.refreshToken';
const USER_KEY = 'lms.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  private readonly _user = signal<User | null>(this.readStoredUser());
  private readonly _accessToken = signal<string | null>(this.readToken(ACCESS_TOKEN_KEY));

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._accessToken() !== null);

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/register`, data)
      .pipe(tap((res) => this.handleAuthResponse(res)));
  }

  login(data: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/login`, data)
      .pipe(tap((res) => this.handleAuthResponse(res)));
  }

  me(): Observable<MeResponse> {
    return this.http.get<MeResponse>(`${this.baseUrl}/me`);
  }

  // ---- Account lifecycle ------------------------------------------------

  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/forgot-password`, { email });
  }

  resetPassword(token: string, password: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/reset-password`, { token, password });
  }

  verifyEmail(token: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/verify-email`, { token });
  }

  resendVerification(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/resend-verification`, {});
  }

  /** Update the in-memory user's IsVerified flag without re-fetching. */
  markVerified(): void {
    const current = this._user();
    if (!current || current.isVerified) return;
    this.patchStoredUser({ isVerified: true });
  }

  logout(): void {
    this.clearStorage();
    this._accessToken.set(null);
    this._user.set(null);
  }

  /**
   * Merges updated fields into the stored user — used after a profile edit so
   * the header chip + role-aware nav reflect the change without a re-login.
   */
  patchStoredUser(patch: Partial<User>): void {
    const current = this._user();
    if (!current) return;
    const next = { ...current, ...patch };
    localStorage.setItem(USER_KEY, JSON.stringify(next));
    this._user.set(next);
  }

  getAccessToken(): string | null {
    return this._accessToken();
  }

  private handleAuthResponse(res: AuthResponse): void {
    if (!res.success || !res.accessToken || !res.user) {
      return;
    }
    localStorage.setItem(ACCESS_TOKEN_KEY, res.accessToken);
    if (res.refreshToken) {
      localStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
    }
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this._accessToken.set(res.accessToken);
    this._user.set(res.user);
  }

  private readToken(key: string): string | null {
    if (typeof localStorage === 'undefined') return null;
    return localStorage.getItem(key);
  }

  private readStoredUser(): User | null {
    if (typeof localStorage === 'undefined') return null;
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as User;
    } catch {
      localStorage.removeItem(USER_KEY);
      return null;
    }
  }

  private clearStorage(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }
}
