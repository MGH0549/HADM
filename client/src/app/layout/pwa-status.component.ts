import { Component } from '@angular/core';
import { PwaService } from '../core/pwa.service';
import { LanguageService } from '../core/language.service';

@Component({ selector: 'hadm-pwa-status', standalone: true, templateUrl: './pwa-status.component.html' })
export class PwaStatusComponent {
  constructor(readonly pwa: PwaService, readonly language: LanguageService) {}
}
