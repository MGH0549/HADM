import { Injectable, signal } from '@angular/core';
import { LanguageService } from './language.service';

@Injectable({ providedIn: 'root' })
export class FeedbackService {
  readonly error = signal('');
  readonly notice = signal('');
  readonly warning = signal('');

  constructor(private readonly language: LanguageService) {}

  clear(): void {
    this.error.set('');
    this.notice.set('');
    this.warning.set('');
  }

  success(english: string, arabic: string): void {
    this.error.set('');
    this.warning.set('');
    this.notice.set(this.language.text(english, arabic));
  }

  validation(english: string, arabic: string): void {
    this.notice.set('');
    this.warning.set('');
    this.error.set(this.language.text(english, arabic));
  }

  warn(message: string): void {
    this.error.set('');
    this.notice.set('');
    this.warning.set(message);
  }

  fail(error: any): void {
    const detail = error?.error?.detail;
    this.warning.set('');
    this.error.set(detail || this.language.text('Something went wrong. Please try again.', 'حدث خطأ. يرجى المحاولة مجدداً.'));
  }
}
