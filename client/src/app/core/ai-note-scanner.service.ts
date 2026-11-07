import { Injectable } from '@angular/core';
import { Observable, catchError, of, switchMap } from 'rxjs';
import { DiaryApiService } from './diary-api.service';
import { LanguageService } from './language.service';

@Injectable({ providedIn: 'root' })
export class AiNoteScannerService {
  constructor(private readonly api: DiaryApiService, private readonly language: LanguageService) {}
  scan(note: string): Observable<{ flagged: boolean; reason: string } | null> {
    if (!note.trim()) return of(null);
    return this.api.getAiPreferences().pipe(
      switchMap(preferences => preferences.enabled ? this.api.scanAiNote(note, this.language.current()) : of(null)),
      catchError(() => of(null)),
    );
  }
}
