import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BristolDistribution,
  AiLogDraft,
  AiPreferences,
  AiWeeklySummary,
  ContextInsight,
  DailyLog,
  Food,
  FoodCorrelation,
  SeverityTrend,
  SymptomFrequency,
  SymptomType,
  Timeline,
  TimelineEvent,
} from './models';

@Injectable({ providedIn: 'root' })
export class DiaryApiService {
  constructor(private readonly http: HttpClient) {}

  getFoods(): Observable<Food[]> { return this.http.get<Food[]>('/api/foods'); }
  createFood(name: string): Observable<Food> { return this.http.post<Food>('/api/foods', { name }); }
  getSymptomTypes(): Observable<SymptomType[]> { return this.http.get<SymptomType[]>('/api/symptom-types'); }
  getTimeline(date: string): Observable<Timeline> { return this.http.get<Timeline>(`/api/timeline?date=${date}&offsetMinutes=${this.offsetMinutes}`); }
  getDaily(date: string): Observable<DailyLog | null> { return this.http.get<DailyLog | null>(`/api/daily/${date}`); }
  saveDaily(date: string, value: DailyLog): Observable<unknown> { return this.http.put(`/api/daily/${date}`, value); }

  createMeal(value: unknown): Observable<unknown> { return this.http.post('/api/meals', value); }
  createSymptom(value: unknown): Observable<any> { return this.http.post<any>('/api/symptoms', value); }
  createBowel(value: unknown): Observable<any> { return this.http.post<any>('/api/bowel', value); }
  deleteEvent(event: TimelineEvent): Observable<unknown> {
    const resource = event.kind === 'meal' ? 'meals' : event.kind === 'symptom' ? 'symptoms' : 'bowel';
    return this.http.delete(`/api/${resource}/${event.id}`);
  }

  getSeverityTrend(days = 14): Observable<SeverityTrend> {
    return this.http.get<SeverityTrend>(`/api/insights/severity-trend?days=${days}&offsetMinutes=${this.offsetMinutes}`);
  }
  getFoodCorrelation(): Observable<FoodCorrelation> {
    return this.http.get<FoodCorrelation>(`/api/insights/food-correlation?windowHours=6&offsetMinutes=${this.offsetMinutes}`);
  }
  getSymptomFrequency(): Observable<SymptomFrequency> {
    return this.http.get<SymptomFrequency>(`/api/insights/symptom-frequency?days=30&offsetMinutes=${this.offsetMinutes}`);
  }
  getBristolDistribution(): Observable<BristolDistribution> {
    return this.http.get<BristolDistribution>(`/api/insights/bristol-distribution?days=30&offsetMinutes=${this.offsetMinutes}`);
  }
  getContextInsight(): Observable<ContextInsight> {
    return this.http.get<ContextInsight>(`/api/insights/context?days=30&offsetMinutes=${this.offsetMinutes}`);
  }
  downloadReport(from: string, to: string, language: string): Observable<Blob> {
    return this.http.get(`/api/reports/pdf?from=${from}&to=${to}&lang=${language}&offsetMinutes=${this.offsetMinutes}`, { responseType: 'blob' });
  }
  deleteAccount(): Observable<unknown> { return this.http.delete('/api/me'); }
  getAiPreferences(): Observable<AiPreferences> { return this.http.get<AiPreferences>('/api/ai/preferences'); }
  setAiEnabled(enabled: boolean): Observable<AiPreferences> { return this.http.put<AiPreferences>('/api/ai/preferences', { enabled }); }
  parseAiLog(text: string): Observable<{ entries: AiLogDraft[]; needsClarification: boolean; clarification: string | null }> {
    return this.http.post<{ entries: AiLogDraft[]; needsClarification: boolean; clarification: string | null }>('/api/ai/parse', { text, offsetMinutes: -new Date().getTimezoneOffset() });
  }
  getAiWeeklySummary(): Observable<AiWeeklySummary> { return this.http.post<AiWeeklySummary>('/api/ai/weekly-summary', {}); }
  scanAiNote(note: string, language: string): Observable<{ flagged: boolean; reason: string }> { return this.http.post<{ flagged: boolean; reason: string }>('/api/ai/scan-note', { note, language }); }

  private get offsetMinutes(): number { return -new Date().getTimezoneOffset(); }
}
