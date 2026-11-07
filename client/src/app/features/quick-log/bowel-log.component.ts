import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DiaryApiService } from '../../core/diary-api.service';
import { DiaryStateService } from '../../core/diary-state.service';
import { AiNoteScannerService } from '../../core/ai-note-scanner.service';
import { FeedbackService } from '../../core/feedback.service';
import { LanguageService } from '../../core/language.service';
import { localDateTimeInput, utcFromLocalInput } from '../../core/date-time';

@Component({ selector: 'hadm-bowel-log', standalone: true, imports: [FormsModule], templateUrl: './bowel-log.component.html' })
export class BowelLogComponent {
  readonly busy = signal(false);
  loggedAt = localDateTimeInput();
  bristolType = 4;
  urgency = 0;
  hasBlood = false;
  note = '';

  constructor(private readonly api: DiaryApiService, private readonly diary: DiaryStateService, private readonly feedback: FeedbackService, private readonly scanner: AiNoteScannerService, readonly language: LanguageService) {}

  save(): void {
    this.busy.set(true);
    this.scanner.scan(this.note).subscribe(scan => this.api.createBowel({ loggedAt: utcFromLocalInput(this.loggedAt), bristolType: this.bristolType, urgency: this.urgency, hasBlood: this.hasBlood, note: this.note || null }).subscribe({
      next: result => { this.busy.set(false); if (result.redFlag) this.feedback.warn(result.redFlagMessage || 'Please contact a doctor about this entry.'); else if (scan?.flagged) this.feedback.warn(scan.reason || 'A note may mention a warning sign. Consider contacting a doctor.'); else this.feedback.success('Bowel movement saved.', 'تم حفظ حركة الأمعاء.'); this.hasBlood = false; this.note = ''; this.diary.refresh(); },
      error: error => { this.busy.set(false); this.feedback.fail(error); },
    }));
  }
}
