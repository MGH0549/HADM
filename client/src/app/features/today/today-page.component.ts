import { Component } from '@angular/core';
import { LanguageService } from '../../core/language.service';
import { BowelLogComponent } from '../quick-log/bowel-log.component';
import { MealLogComponent } from '../quick-log/meal-log.component';
import { SymptomLogComponent } from '../quick-log/symptom-log.component';
import { DailyContextComponent } from './daily-context.component';
import { AiLogComponent } from '../ai/ai-log.component';
import { TimelineComponent } from './timeline.component';

@Component({ selector: 'hadm-today-page', standalone: true, imports: [MealLogComponent, SymptomLogComponent, BowelLogComponent, DailyContextComponent, TimelineComponent, AiLogComponent], templateUrl: './today-page.component.html' })
export class TodayPageComponent { constructor(readonly language: LanguageService) {} }
