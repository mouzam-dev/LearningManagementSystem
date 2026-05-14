import { Injectable, effect, signal } from '@angular/core';

type Theme = 'light' | 'dark';

const STORAGE_KEY = 'lms.theme';

/**
 * Light/dark theme state. The initial value is resolved from localStorage, or
 * the OS preference on first visit. A pre-paint script in index.html applies
 * the `.dark` class before Angular boots so there's no flash; this service
 * keeps the class in sync afterwards and persists the user's choice.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _theme = signal<Theme>(this.resolveInitial());

  /** Read-only current theme. */
  readonly theme = this._theme.asReadonly();

  constructor() {
    // Keep <html class="dark"> and localStorage in sync with the signal.
    effect(() => {
      const theme = this._theme();
      if (typeof document !== 'undefined') {
        document.documentElement.classList.toggle('dark', theme === 'dark');
      }
      try {
        localStorage.setItem(STORAGE_KEY, theme);
      } catch {
        /* storage blocked — runtime toggle still works for the session */
      }
    });
  }

  isDark(): boolean {
    return this._theme() === 'dark';
  }

  toggle(): void {
    this._theme.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }

  set(theme: Theme): void {
    this._theme.set(theme);
  }

  private resolveInitial(): Theme {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved === 'light' || saved === 'dark') return saved;
    } catch {
      /* ignore */
    }
    if (typeof window !== 'undefined' && window.matchMedia) {
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return 'light';
  }
}
