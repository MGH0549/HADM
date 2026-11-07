import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { DiaryStateService } from '../core/diary-state.service';
import { FeedbackService } from '../core/feedback.service';
import { LanguageService } from '../core/language.service';
import { PwaStatusComponent } from './pwa-status.component';

@Component({
  selector: 'hadm-app-shell',
  standalone: true,
  imports: [FormsModule, RouterLink, RouterLinkActive, RouterOutlet, PwaStatusComponent],
  templateUrl: './app-shell.component.html',
})
export class AppShellComponent {
  constructor(
    readonly auth: AuthService,
    readonly diary: DiaryStateService,
    readonly feedback: FeedbackService,
    readonly language: LanguageService,
    private readonly router: Router,
  ) {}

  signOut(): void {
    this.auth.logout();
    this.router.navigateByUrl('/auth');
  }
}
