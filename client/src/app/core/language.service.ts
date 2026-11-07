import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localeAr from '@angular/common/locales/ar';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly current = signal(localStorage.getItem('hadm-language') ?? 'en');

  constructor(private readonly http: HttpClient, private readonly auth: AuthService) {
    registerLocaleData(localeAr);
    this.applyDirection();
  }

  get isArabic(): boolean { return this.current() === 'ar'; }

  text(english: string, arabic: string): string {
    return this.isArabic ? arabic : english;
  }

  set(language: string): void {
    const selected = language === 'ar' ? 'ar' : 'en';
    this.current.set(selected);
    localStorage.setItem('hadm-language', selected);
    this.applyDirection();
    if (this.auth.token()) {
      this.http.put('/api/me/language', { preferredLanguage: selected }).subscribe();
    }
  }

  private applyDirection(): void {
    document.documentElement.lang = this.current();
    document.documentElement.dir = this.isArabic ? 'rtl' : 'ltr';
  }
}
