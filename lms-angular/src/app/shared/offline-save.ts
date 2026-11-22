import { Component, computed, inject, input, signal } from '@angular/core';

import { OfflineService } from '../core/services/offline.service';

/** "Save for offline" button with progress, for the whole Qur'an or one hadith collection. */
@Component({
  selector: 'app-offline-save',
  template: `
    @if (offline.available) {
      @if (running(); as p) {
        <div class="flex min-w-0 items-center gap-2 text-xs text-muted" role="status">
          <div class="h-1.5 w-24 shrink-0 rounded-full bg-surface-3"><div class="h-full rounded-full bg-accent-600" [style.width.%]="(p.done / p.total) * 100"></div></div>
          Saving {{ p.done }}/{{ p.total }}
        </div>
      } @else if (saved()) {
        <span class="inline-flex items-center gap-1 text-xs font-semibold text-accent-700 dark:text-accent-400">✓ Saved for offline</span>
      } @else {
        <button type="button" (click)="save()" [disabled]="!!offline.progress()"
          class="h-8 rounded-lg border border-line px-2.5 text-sm font-semibold text-ink-soft hover:border-line-strong disabled:opacity-40">
          {{ failed() ? 'Retry saving' : 'Save for offline' }}
        </button>
      }
    }
  `,
})
export class OfflineSave {
  readonly offline = inject(OfflineService);
  readonly kind = input.required<'quran' | 'hadith'>();
  readonly id = input.required<string | number>();
  readonly failed = signal(false);

  private readonly key = computed(() => `${this.kind()}.${this.id()}`);
  readonly saved = computed(() => this.offline.isSaved(this.key()));
  readonly running = computed(() => {
    const p = this.offline.progress();
    return p?.key === this.key() ? p : null;
  });

  async save(): Promise<void> {
    this.failed.set(false);
    try {
      const ok = this.kind() === 'quran'
        ? await this.offline.saveQuran(Number(this.id()))
        : await this.offline.saveCollection(String(this.id()));
      this.failed.set(!ok);
    } catch {
      this.failed.set(true);
    }
  }
}
