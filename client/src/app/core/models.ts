export interface AuthResponse {
  token: string;
  email: string;
  preferredLanguage: string;
}

export interface Food {
  id: string;
  key: string;
  nameEn: string;
  nameAr: string;
  isBuiltIn: boolean;
}

export interface SymptomType {
  id: string;
  key: string;
  nameEn: string;
  nameAr: string;
  isRedFlag: boolean;
}

export interface TimelineEvent {
  kind: string;
  id: string;
  loggedAt: string;
  titleKey: string;
  nameEn: string;
  nameAr: string;
  detailEn: string;
  detailAr: string;
  severity?: number;
  redFlag: boolean;
}

export interface DailyContext {
  sleepHours?: number;
  stressLevel?: number;
  exercise: boolean;
  medicationTaken: boolean;
}

export interface DailyLog extends DailyContext {
  date: string;
  note?: string;
}

export interface Timeline {
  date: string;
  events: TimelineEvent[];
  daily?: DailyContext;
}

export interface SeverityTrend {
  points: { date: string; averageSeverity?: number; count: number }[];
}

export interface FoodCorrelation {
  enoughData: boolean;
  mealCount: number;
  dayCount: number;
  symptomCount: number;
  requiredMeals: number;
  requiredDays: number;
  requiredSymptoms: number;
  windowHours: number;
  items: {
    key: string;
    nameEn: string;
    nameAr: string;
    averageInside: number;
    averageOutside: number;
    difference: number;
    insideCount: number;
    outsideCount: number;
  }[];
}

export interface SymptomFrequency {
  items: { key: string; nameEn: string; nameAr: string; count: number; averageSeverity: number }[];
}

export interface BristolDistribution {
  items: { type: number; count: number }[];
}

export interface ContextInsight {
  byStress: { key: string; averageSeverity: number; count: number }[];
  bySleep: { key: string; averageSeverity: number; count: number }[];
  byExercise: { key: string; averageSeverity: number; count: number }[];
}

export interface AiPreferences {
  enabled: boolean;
  consentAt: string | null;
  configured: boolean;
}

export interface AiLogDraft {
  kind: 'meal' | 'symptom';
  loggedAt: string;
  mealType: number | null;
  foodIds: string[];
  symptomTypeId: string | null;
  severity: number | null;
  note: string | null;
  include?: boolean;
}

export interface AiWeeklySummary {
  available: boolean;
  message?: string;
  summary: string | null;
  observations: string[];
  facts: { days?: number; mealCount: number; symptomCount: number; averageSeverity?: number | null };
}
