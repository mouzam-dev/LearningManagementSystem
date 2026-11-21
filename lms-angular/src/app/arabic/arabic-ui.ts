import { Component, computed, inject, input } from '@angular/core';

import { MeaningLang, starCount } from './arabic.models';
import { ArabicService } from './arabic.service';

/** 0-3 gold stars for a quiz score; renders nothing when never attempted. */
@Component({
  selector: 'app-arabic-stars',
  template: `
    @if (pct() != null) {
      <span class="inline-flex items-center gap-0.5" [attr.aria-label]="count() + ' of 3 stars'">
        @for (i of [1, 2, 3]; track i) {
          <svg viewBox="0 0 24 24" class="h-4 w-4" aria-hidden="true"
            [class]="i <= count() ? 'fill-primary-500 text-primary-500' : 'fill-surface-2 text-line-strong'"
            stroke="currentColor" stroke-width="1.5" stroke-linejoin="round">
            <path d="m12 3 2.8 5.7 6.2.9-4.5 4.4 1 6.2L12 17.3 6.5 20.2l1-6.2L3 9.6l6.2-.9z" />
          </svg>
        }
      </span>
    }
  `,
})
export class ArabicStars {
  readonly pct = input<number | undefined>();
  readonly count = computed(() => starCount(this.pct()));
}

/** Translation language + transliteration toggles, shared by every Arabic page. */
@Component({
  selector: 'app-arabic-prefs',
  template: `
    <div class="flex flex-wrap items-center gap-2 text-xs font-semibold">
      <span class="text-muted">Meaning:</span>
      <div class="inline-flex overflow-hidden rounded-lg border border-line" role="group" aria-label="Meaning language">
        @for (o of options; track o.value) {
          <button type="button" (click)="svc.lang.set(o.value)" [attr.aria-pressed]="svc.lang() === o.value"
            class="px-2.5 py-1.5 transition"
            [class]="svc.lang() === o.value ? 'bg-primary-600 text-white' : 'bg-surface text-ink-soft hover:bg-surface-2'">{{ o.label }}</button>
        }
      </div>
      <button type="button" (click)="svc.showTr.set(!svc.showTr())" [attr.aria-pressed]="svc.showTr()"
        class="rounded-lg border px-2.5 py-1.5 transition"
        [class]="svc.showTr() ? 'border-accent-300 bg-accent-50 text-accent-700 dark:border-accent-700 dark:bg-accent-900/40 dark:text-accent-200' : 'border-line bg-surface text-ink-soft hover:bg-surface-2'">
        Transliteration
      </button>
    </div>
  `,
})
export class ArabicPrefs {
  protected readonly svc = inject(ArabicService);
  protected readonly options: { value: MeaningLang; label: string }[] = [
    { value: 'both', label: 'English + Urdu' },
    { value: 'en', label: 'English' },
    { value: 'ur', label: 'Roman Urdu' },
  ];
}

/** Transliteration + English and/or Roman Urdu meaning of an Arabic word or line. */
@Component({
  selector: 'app-arabic-meaning',
  template: `
    @if (svc.showTr() && item().tr) { <span class="block text-sm italic text-accent-700 dark:text-accent-300">{{ item().tr }}</span> }
    @if (svc.lang() !== 'ur') { <span class="block text-sm text-ink">{{ item().en }}</span> }
    @if (svc.lang() !== 'en' && item().ur) { <span class="block text-sm text-muted">{{ item().ur }}</span> }
  `,
})
export class ArabicMeaning {
  protected readonly svc = inject(ArabicService);
  readonly item = input.required<{ tr?: string; en: string; ur: string }>();
}
