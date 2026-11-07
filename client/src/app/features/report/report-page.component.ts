import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DiaryApiService } from '../../core/diary-api.service';
import { DiaryStateService } from '../../core/diary-state.service';
import { FeedbackService } from '../../core/feedback.service';
import { LanguageService } from '../../core/language.service';
import { reportStartDate } from '../../core/date-time';

@Component({ selector: 'hadm-report-page', standalone: true, imports: [FormsModule], templateUrl: './report-page.component.html' })
export class ReportPageComponent {
  readonly busy = signal(false);
  days = 7;
  get diaryDate(): string { return this.diary.selectedDate(); }
  constructor(private readonly api: DiaryApiService, private readonly diary: DiaryStateService, private readonly feedback: FeedbackService, readonly language: LanguageService) {}
  download(): void {
    this.busy.set(true);
    const to = this.diary.selectedDate();
    this.api.downloadReport(reportStartDate(to, this.days), to, this.language.current()).subscribe({
      next: blob => { const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = 'hadm-report-' + to + '.pdf'; link.click(); URL.revokeObjectURL(url); this.busy.set(false); },
      error: error => { this.busy.set(false); this.feedback.fail(error); },
    });
  }
}
