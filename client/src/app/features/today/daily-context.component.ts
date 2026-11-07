import { Component, effect, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DiaryApiService } from '../../core/diary-api.service';
import { DiaryStateService } from '../../core/diary-state.service';
import { FeedbackService } from '../../core/feedback.service';
import { DailyLog } from '../../core/models';
import { LanguageService } from '../../core/language.service';

@Component({ selector: 'hadm-daily-context', standalone: true, imports: [FormsModule], templateUrl: './daily-context.component.html' })
export class DailyContextComponent {
  readonly busy = signal(false);
  sleepHours: number | null = null;
  stressLevel: number | null = null;
  exercise = false;
  medicationTaken = false;
  note = '';

  constructor(private readonly api: DiaryApiService, private readonly diary: DiaryStateService, private readonly feedback: FeedbackService, readonly language: LanguageService) {
    effect(() => { const date = diary.selectedDate(); diary.refreshVersion(); this.load(date); });
  }

  private load(date: string): void {
    this.api.getDaily(date).subscribe({ next: value => { this.sleepHours = value?.sleepHours ?? null; this.stressLevel = value?.stressLevel ?? null; this.exercise = value?.exercise ?? false; this.medicationTaken = value?.medicationTaken ?? false; this.note = value?.note ?? ''; }, error: error => this.feedback.fail(error) });
  }

  save(): void {
    this.busy.set(true);
    const value: DailyLog = { date: this.diary.selectedDate(), sleepHours: this.sleepHours ?? undefined, stressLevel: this.stressLevel ?? undefined, exercise: this.exercise, medicationTaken: this.medicationTaken, note: this.note || undefined };
    this.api.saveDaily(value.date, value).subscribe({ next: () => { this.busy.set(false); this.feedback.success('Daily check-in saved.', 'تم حفظ ملخص اليوم.'); this.diary.refresh(); }, error: error => { this.busy.set(false); this.feedback.fail(error); } });
  }
}
