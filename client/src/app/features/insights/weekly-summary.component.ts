import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DiaryApiService } from '../../core/diary-api.service';
import { FeedbackService } from '../../core/feedback.service';
import { AiWeeklySummary } from '../../core/models';
import { AiPreferences } from '../../core/models';
import { LanguageService } from '../../core/language.service';

@Component({ selector: 'hadm-weekly-summary', standalone: true, imports: [RouterLink], templateUrl: './weekly-summary.component.html' })
export class WeeklySummaryComponent implements OnInit {
  readonly busy = signal(false);
  readonly preferences = signal<AiPreferences | null>(null);
  readonly result = signal<AiWeeklySummary | null>(null);
  constructor(private readonly api: DiaryApiService, private readonly feedback: FeedbackService, readonly language: LanguageService) {}
  ngOnInit(): void { this.api.getAiPreferences().subscribe({ next: value => this.preferences.set(value), error: error => this.feedback.fail(error) }); }
  generate(): void {
    this.busy.set(true);
    this.api.getAiWeeklySummary().subscribe({ next: result => { this.result.set(result); this.busy.set(false); }, error: error => { this.busy.set(false); this.feedback.fail(error); } });
  }
}
