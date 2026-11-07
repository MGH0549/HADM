import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { DiaryApiService } from '../../core/diary-api.service';
import { FeedbackService } from '../../core/feedback.service';
import { LanguageService } from '../../core/language.service';
import { AiPreferences } from '../../core/models';

@Component({ selector: 'hadm-settings-page', standalone: true, templateUrl: './settings-page.component.html' })
export class SettingsPageComponent implements OnInit {
  readonly aiPreferences = signal<AiPreferences | null>(null);
  readonly aiBusy = signal(false);
  constructor(private readonly api: DiaryApiService, private readonly auth: AuthService, private readonly feedback: FeedbackService, readonly language: LanguageService, private readonly router: Router) {}
  ngOnInit(): void { this.api.getAiPreferences().subscribe({ next: value => this.aiPreferences.set(value), error: error => this.feedback.fail(error) }); }
  setAiEnabled(enabled: boolean): void {
    this.aiBusy.set(true);
    this.api.setAiEnabled(enabled).subscribe({ next: value => { this.aiPreferences.set(value); this.aiBusy.set(false); }, error: error => { this.aiBusy.set(false); this.feedback.fail(error); } });
  }
  deleteAccount(): void {
    if (!confirm(this.language.text('Permanently delete your account and all diary entries?', 'هل تريد حذف حسابك وجميع إدخالات المذكرة نهائياً؟'))) return;
    this.api.deleteAccount().subscribe({ next: () => { this.auth.logout(); this.router.navigateByUrl('/auth'); }, error: error => this.feedback.fail(error) });
  }
}
