import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { FeedbackService } from '../../core/feedback.service';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'hadm-auth-page',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './auth-page.component.html',
})
export class AuthPageComponent {
  readonly busy = signal(false);
  mode: 'login' | 'register' = 'login';
  email = '';
  password = '';

  constructor(
    readonly language: LanguageService,
    readonly feedback: FeedbackService,
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    this.feedback.clear();
    this.busy.set(true);
    this.auth.authenticate(this.mode, this.email, this.password, this.language.current()).subscribe({
      next: () => { this.busy.set(false); this.router.navigateByUrl('/today'); },
      error: error => { this.busy.set(false); this.feedback.fail(error); },
    });
  }
}
