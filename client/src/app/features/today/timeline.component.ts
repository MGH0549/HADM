import { Component, effect, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DiaryApiService } from '../../core/diary-api.service';
import { DiaryStateService } from '../../core/diary-state.service';
import { FeedbackService } from '../../core/feedback.service';
import { LanguageService } from '../../core/language.service';
import { TimelineEvent } from '../../core/models';

@Component({ selector: 'hadm-timeline', standalone: true, imports: [DatePipe], templateUrl: './timeline.component.html' })
export class TimelineComponent {
  readonly events = signal<TimelineEvent[]>([]);
  constructor(private readonly api: DiaryApiService, private readonly diary: DiaryStateService, private readonly feedback: FeedbackService, readonly language: LanguageService) {
    effect(() => { const date = diary.selectedDate(); diary.refreshVersion(); this.load(date); });
  }
  eventName(event: TimelineEvent): string {
    if (event.kind === 'bowel') return this.language.text('Bristol type ', 'نوع بريستول ') + event.titleKey;
    if (event.kind === 'meal') {
      const meals: Record<string, [string, string]> = { Breakfast: ['Breakfast', 'إفطار'], Lunch: ['Lunch', 'غداء'], Dinner: ['Dinner', 'عشاء'], Snack: ['Snack', 'وجبة خفيفة'] };
      const names = meals[event.titleKey];
      return names ? this.language.text(names[0], names[1]) : this.language.text('Meal', 'وجبة');
    }
    return this.language.isArabic ? event.nameAr || event.titleKey : event.nameEn || event.titleKey;
  }
  eventDetail(event: TimelineEvent): string {
    const detail = this.language.isArabic ? event.detailAr : event.detailEn;
    if (detail === 'blood') return this.language.text('Blood noted', 'تمت ملاحظة دم');
    if (event.kind === 'bowel') {
      const urgency: Record<string, [string, string]> = { None: ['No urgency', 'لا يوجد إلحاح'], Low: ['Low urgency', 'إلحاح خفيف'], Moderate: ['Moderate urgency', 'إلحاح متوسط'], High: ['High urgency', 'إلحاح شديد'] };
      const label = urgency[event.nameEn];
      return [label ? this.language.text(label[0], label[1]) : '', detail].filter(Boolean).join(' · ');
    }
    return detail;
  }
  remove(event: TimelineEvent): void {
    if (!confirm(this.language.text('Delete this diary entry?', 'هل تريد حذف هذا الإدخال؟'))) return;
    this.api.deleteEvent(event).subscribe({ next: () => this.diary.refresh(), error: error => this.feedback.fail(error) });
  }
  private load(date: string): void { this.api.getTimeline(date).subscribe({ next: timeline => this.events.set([...timeline.events].sort((a,b) => b.loggedAt.localeCompare(a.loggedAt))), error: error => this.feedback.fail(error) }); }
}
