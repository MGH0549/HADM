import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly token = signal(localStorage.getItem('hadm-token') ?? '');
  readonly email = signal(localStorage.getItem('hadm-email') ?? '');

  constructor(private readonly http: HttpClient) {}

  authenticate(mode: 'login' | 'register', email: string, password: string, language: string): Observable<AuthResponse> {
    const endpoint = mode === 'login' ? '/api/auth/login' : '/api/auth/register';
    const payload = mode === 'login'
      ? { email, password }
      : { email, password, preferredLanguage: language };
    return this.http.post<AuthResponse>(endpoint, payload).pipe(tap(response => this.store(response)));
  }

  store(response: AuthResponse): void {
    this.token.set(response.token);
    this.email.set(response.email);
    localStorage.setItem('hadm-token', response.token);
    localStorage.setItem('hadm-email', response.email);
  }

  logout(): void {
    localStorage.removeItem('hadm-token');
    localStorage.removeItem('hadm-email');
    this.token.set('');
    this.email.set('');
  }
}
