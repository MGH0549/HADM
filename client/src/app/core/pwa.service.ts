import { Injectable, isDevMode, signal } from '@angular/core';

interface InstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed'; platform: string }>;
}

@Injectable({ providedIn: 'root' })
export class PwaService {
  readonly canInstall = signal(false);
  readonly isOnline = signal(typeof navigator === 'undefined' ? true : navigator.onLine);
  readonly isInstalled = signal(typeof window !== 'undefined' && window.matchMedia('(display-mode: standalone)').matches);
  readonly needsManualInstall = signal(typeof navigator !== 'undefined' && (/iPhone|iPad|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)));
  private installPrompt: InstallPromptEvent | null = null;

  constructor() {
    if (typeof window === 'undefined') return;
    window.addEventListener('beforeinstallprompt', this.onBeforeInstall);
    window.addEventListener('appinstalled', this.onInstalled);
    window.addEventListener('online', this.onOnline);
    window.addEventListener('offline', this.onOffline);
    if (!isDevMode() && 'serviceWorker' in navigator && window.isSecureContext) {
      navigator.serviceWorker.register('/sw.js', { scope: '/' }).catch(error => console.error('Hadm service worker registration failed.', error));
    }
  }

  async install(): Promise<void> {
    if (!this.installPrompt) return;
    const prompt = this.installPrompt;
    this.installPrompt = null;
    this.canInstall.set(false);
    await prompt.prompt();
    await prompt.userChoice;
  }

  private readonly onBeforeInstall = (event: Event): void => {
    event.preventDefault();
    this.installPrompt = event as InstallPromptEvent;
    this.canInstall.set(true);
  };
  private readonly onInstalled = (): void => { this.installPrompt = null; this.canInstall.set(false); this.isInstalled.set(true); };
  private readonly onOnline = (): void => this.isOnline.set(true);
  private readonly onOffline = (): void => this.isOnline.set(false);
}
