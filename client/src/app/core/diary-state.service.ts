import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class DiaryStateService {
  readonly selectedDate = signal(this.localDate(new Date()));
  readonly refreshVersion = signal(0);

  refresh(): void { this.refreshVersion.update(value => value + 1); }

  private localDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
