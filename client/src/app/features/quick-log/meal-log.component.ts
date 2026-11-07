import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DiaryApiService } from '../../core/diary-api.service';
import { DiaryStateService } from '../../core/diary-state.service';
import { AiNoteScannerService } from '../../core/ai-note-scanner.service';
import { FeedbackService } from '../../core/feedback.service';
import { Food } from '../../core/models';
import { LanguageService } from '../../core/language.service';
import { localDateTimeInput, utcFromLocalInput } from '../../core/date-time';

@Component({ selector: 'hadm-meal-log', standalone: true, imports: [FormsModule], templateUrl: './meal-log.component.html' })
export class MealLogComponent implements OnInit {
  readonly busy = signal(false);
  readonly foods = signal<Food[]>([]);
  mealType = 1;
  loggedAt = localDateTimeInput();
  foodIds: string[] = [];
  customFood = '';
  note = '';

  constructor(private readonly api: DiaryApiService, private readonly diary: DiaryStateService, private readonly feedback: FeedbackService, private readonly scanner: AiNoteScannerService, readonly language: LanguageService) {}

  ngOnInit(): void { this.api.getFoods().subscribe({ next: foods => this.foods.set(foods), error: error => this.feedback.fail(error) }); }

  foodName(food: Food): string { return this.language.isArabic ? food.nameAr : food.nameEn; }
  toggleFood(id: string): void { this.foodIds = this.foodIds.includes(id) ? this.foodIds.filter(value => value !== id) : [...this.foodIds, id]; }

  addFood(): void {
    const name = this.customFood.trim();
    if (!name) return;
    this.api.createFood(name).subscribe({
      next: food => { this.foods.update(items => [...items, food]); this.toggleFood(food.id); this.customFood = ''; },
      error: error => this.feedback.fail(error),
    });
  }

  save(): void {
    if (!this.foodIds.length) { this.feedback.validation('Add at least one food to the meal.', 'أضف طعاماً واحداً على الأقل للوجبة.'); return; }
    this.busy.set(true);
    this.scanner.scan(this.note).subscribe(scan => this.api.createMeal({ loggedAt: utcFromLocalInput(this.loggedAt), mealType: this.mealType, foodIds: this.foodIds, note: this.note || null }).subscribe({
      next: () => { this.busy.set(false); this.foodIds = []; this.note = ''; if (scan?.flagged) this.feedback.warn(scan.reason || 'A note may mention a warning sign. Consider contacting a doctor.'); else this.feedback.success('Meal saved.', 'تم حفظ الوجبة.'); this.diary.refresh(); },
      error: error => { this.busy.set(false); this.feedback.fail(error); },
    }));
  }
}
