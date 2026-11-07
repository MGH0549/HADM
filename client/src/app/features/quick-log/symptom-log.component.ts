import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DiaryApiService } from '../../core/diary-api.service';
import { DiaryStateService } from '../../core/diary-state.service';
import { AiNoteScannerService } from '../../core/ai-note-scanner.service';
import { FeedbackService } from '../../core/feedback.service';
import { LanguageService } from '../../core/language.service';
import { SymptomType } from '../../core/models';
import { localDateTimeInput, utcFromLocalInput } from '../../core/date-time';

@Component({ selector: 'hadm-symptom-log', standalone: true, imports: [FormsModule], templateUrl: './symptom-log.component.html' })
export class SymptomLogComponent implements OnInit {
  readonly busy = signal(false);
  readonly symptomTypes = signal<SymptomType[]>([]);
  symptomTypeId = '';
  loggedAt = localDateTimeInput();
  severity = 4;
  note = '';

  constructor(private readonly api: DiaryApiService, private readonly diary: DiaryStateService, private readonly feedback: FeedbackService, private readonly scanner: AiNoteScannerService, readonly language: LanguageService) {}

  ngOnInit(): void {
    this.api.getSymptomTypes().subscribe({
      next: types => { this.symptomTypes.set(types); if (!this.symptomTypeId && types.length) this.symptomTypeId = types[0].id; },
      error: error => this.feedback.fail(error),
    });
  }

  save(): void {
    if (!this.symptomTypeId) { this.feedback.validation('Choose a symptom.', 'اختر عرضاً.'); return; }
    this.busy.set(true);
    this.scanner.scan(this.note).subscribe(scan => this.api.createSymptom({ loggedAt: utcFromLocalInput(this.loggedAt), symptomTypeId: this.symptomTypeId, severity: this.severity, note: this.note || null }).subscribe({
      next: result => { this.busy.set(false); if (result.redFlag) this.feedback.warn(result.redFlagMessage || 'Please contact a doctor about this symptom.'); else if (scan?.flagged) this.feedback.warn(scan.reason || 'A note may mention a warning sign. Consider contacting a doctor.'); else this.feedback.success('Symptom saved.', 'تم حفظ العرض.'); this.note = ''; this.diary.refresh(); },
      error: error => { this.busy.set(false); this.feedback.fail(error); },
    }));
  }
}
