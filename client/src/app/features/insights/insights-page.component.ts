import { Component, OnInit, signal } from '@angular/core';
import { SlicePipe } from '@angular/common';
import { forkJoin } from 'rxjs';
import { DiaryApiService } from '../../core/diary-api.service';
import { FeedbackService } from '../../core/feedback.service';
import { BristolDistribution, ContextInsight, FoodCorrelation, SeverityTrend, SymptomFrequency } from '../../core/models';
import { LanguageService } from '../../core/language.service';
import { WeeklySummaryComponent } from './weekly-summary.component';

@Component({ selector: 'hadm-insights-page', standalone: true, imports: [SlicePipe, WeeklySummaryComponent], templateUrl: './insights-page.component.html' })
export class InsightsPageComponent implements OnInit {
  readonly trend = signal<SeverityTrend | null>(null);
  readonly correlation = signal<FoodCorrelation | null>(null);
  readonly frequency = signal<SymptomFrequency | null>(null);
  readonly bristol = signal<BristolDistribution | null>(null);
  readonly context = signal<ContextInsight | null>(null);
  constructor(private readonly api: DiaryApiService, private readonly feedback: FeedbackService, readonly language: LanguageService) {}
  ngOnInit(): void { forkJoin({ trend: this.api.getSeverityTrend(), correlation: this.api.getFoodCorrelation(), frequency: this.api.getSymptomFrequency(), bristol: this.api.getBristolDistribution(), context: this.api.getContextInsight() }).subscribe({ next: d => { this.trend.set(d.trend); this.correlation.set(d.correlation); this.frequency.set(d.frequency); this.bristol.set(d.bristol); this.context.set(d.context); }, error: e => this.feedback.fail(e) }); }
  bar(value: number | undefined, max: number): string { return Math.max(3, Math.min(100, ((value || 0) / Math.max(1, max)) * 100)) + '%'; }
  maxBristol(items: { count: number }[]): number { return Math.max(1, ...items.map(x => x.count)); }
  name(en: string, ar: string): string { return this.language.isArabic ? ar : en; }
  contextName(key: string): string { const [kind, value] = key.split('.'); if (kind === 'stress') return this.language.text('Stress', 'التوتر') + ' ' + value + '/5'; if (kind === 'sleep') return ({ under6: this.language.text('Under 6 hours', 'أقل من 6 ساعات'), '6to8': this.language.text('6 to 8 hours', 'من 6 إلى 8 ساعات'), over8: this.language.text('Over 8 hours', 'أكثر من 8 ساعات') } as Record<string,string>)[value] || key; if (kind === 'exercise') return value === 'yes' ? this.language.text('Exercise', 'نشاط بدني') : this.language.text('No exercise', 'دون نشاط بدني'); return key; }
}
