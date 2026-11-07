import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { DiaryApiService } from '../../core/diary-api.service';
import { DiaryStateService } from '../../core/diary-state.service';
import { FeedbackService } from '../../core/feedback.service';
import { AiLogDraft, AiPreferences, Food, SymptomType } from '../../core/models';
import { LanguageService } from '../../core/language.service';
import { localDateTimeInput, utcFromLocalInput } from '../../core/date-time';

@Component({ selector: 'hadm-ai-log', standalone: true, imports: [FormsModule], templateUrl: './ai-log.component.html' })
export class AiLogComponent implements OnInit {
  readonly busy = signal(false);
  readonly parsing = signal(false);
  readonly preferences = signal<AiPreferences | null>(null);
  readonly drafts = signal<AiLogDraft[]>([]);
  readonly foods = signal<Food[]>([]);
  readonly symptoms = signal<SymptomType[]>([]);
  text = '';
  clarification = '';
  riskNotice = '';

  constructor(private readonly api: DiaryApiService, private readonly diary: DiaryStateService, private readonly feedback: FeedbackService, readonly language: LanguageService) {}
  ngOnInit(): void {
    this.api.getAiPreferences().subscribe({ next: value => this.preferences.set(value), error: error => this.feedback.fail(error) });
    this.api.getFoods().subscribe({ next: items => this.foods.set(items), error: error => this.feedback.fail(error) });
    this.api.getSymptomTypes().subscribe({ next: items => this.symptoms.set(items), error: error => this.feedback.fail(error) });
  }
  foodName(food: Food): string { return this.language.isArabic ? food.nameAr : food.nameEn; }
  symptomName(type: SymptomType): string { return this.language.isArabic ? type.nameAr : type.nameEn; }
  parse(): void {
    if (!this.preferences()?.enabled || !this.preferences()?.configured) return;
    if (!this.text.trim()) return;
    this.parsing.set(true); this.clarification = ''; this.riskNotice = '';
    this.api.parseAiLog(this.text.trim()).subscribe({
      next: result => {
        this.parsing.set(false);
        this.clarification = result.clarification ?? '';
        this.drafts.set(result.entries.map(entry => ({ ...entry, loggedAt: localDateTimeInput(new Date(entry.loggedAt)), include: true })));
        if (!result.entries.length && !this.clarification) this.clarification = this.language.text('I could not find a clear meal or symptom entry. Try adding a time and a symptom or food.', 'لم أتمكن من تحديد وجبة أو عرض بوضوح. حاول إضافة الوقت والطعام أو العرض.');
      },
      error: error => { this.parsing.set(false); this.feedback.fail(error); },
    });
  }
  update(index: number, change: Partial<AiLogDraft>): void { this.drafts.update(items => items.map((item, i) => i === index ? { ...item, ...change } : item)); }
  toggleFood(index: number, id: string): void {
    const item = this.drafts()[index]; const ids = item.foodIds.includes(id) ? item.foodIds.filter(value => value !== id) : [...item.foodIds, id];
    this.update(index, { foodIds: ids });
  }
  async saveSelected(): Promise<void> {
    const selected = this.drafts().filter(x => x.include);
    if (!selected.length) return;
    this.busy.set(true); this.riskNotice = '';
    let saved = 0;
    for (const entry of selected) {
      const note = entry.note?.trim();
      if (note) {
        try {
          const scan = await firstValueFrom(this.api.scanAiNote(note, this.language.current()));
          if (scan.flagged) this.riskNotice = scan.reason || this.language.text('A note may describe a warning sign. Consider contacting a doctor.', 'قد تشير الملاحظة إلى علامة تحذيرية. فكّر بالتواصل مع الطبيب.');
        } catch { /* Saving remains possible; keyword red-flag checks remain active on the API. */ }
      }
      const loggedAt = utcFromLocalInput(entry.loggedAt);
      if (entry.kind === 'meal' && entry.mealType && entry.foodIds.length) {
        await firstValueFrom(this.api.createMeal({ loggedAt, mealType: entry.mealType, foodIds: entry.foodIds, note: note || null }));
        saved++;
      } else if (entry.kind === 'symptom' && entry.symptomTypeId && entry.severity !== null) {
        const result = await firstValueFrom(this.api.createSymptom({ loggedAt, symptomTypeId: entry.symptomTypeId, severity: entry.severity, note: note || null }));
        if (result.redFlag) this.riskNotice = result.redFlagMessage || this.riskNotice;
        saved++;
      }
    }
    this.busy.set(false);
    this.drafts.set([]);
    this.text = '';
    this.diary.refresh();
    this.feedback.success(saved + ' ' + this.language.text('entries saved after your confirmation.', 'إدخالات حُفظت بعد تأكيدك.'), saved + ' إدخالات حُفظت بعد تأكيدك.');
  }
}
