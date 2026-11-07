import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PwaService } from './core/pwa.service';

@Component({ selector: 'app-root', standalone: true, imports: [RouterOutlet], templateUrl: './app.html' })
export class App {
  constructor(_pwa: PwaService) {}
}
