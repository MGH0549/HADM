import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { guestGuard } from './core/guest.guard';
import { AuthPageComponent } from './features/auth/auth-page.component';
import { InsightsPageComponent } from './features/insights/insights-page.component';
import { ReportPageComponent } from './features/report/report-page.component';
import { SettingsPageComponent } from './features/settings/settings-page.component';
import { TodayPageComponent } from './features/today/today-page.component';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
  { path: 'auth', component: AuthPageComponent, canActivate: [guestGuard] },
  { path: '', component: AppShellComponent, canActivate: [authGuard], children: [
    { path: '', pathMatch: 'full', redirectTo: 'today' },
    { path: 'today', component: TodayPageComponent },
    { path: 'insights', component: InsightsPageComponent },
    { path: 'report', component: ReportPageComponent },
    { path: 'settings', component: SettingsPageComponent },
  ] },
  { path: '**', redirectTo: 'today' },
];
