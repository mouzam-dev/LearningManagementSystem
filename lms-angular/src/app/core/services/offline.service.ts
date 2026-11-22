import { Injectable, inject, signal } from '@angular/core';
import { SwUpdate } from '@angular/service-worker';
import { firstValueFrom } from 'rxjs';

import { QuranService } from '../../quran/quran.service';
import { SunnahService } from '../../hadith/sunnah.service';

export interface OfflineProgress {
  key: string;
  done: number;
  total: number;
  failed: number;
}

/**
 * "Save for offline": requests every page of the Qur'an (one translation) or of a hadith
 * collection once, through the normal services. The service worker's data groups
 * (ngsw-config.json: "quran", "hadith") keep those responses, so the same pages open
 * later with no internet. Only works where the service worker runs (production, https).
 */
@Injectable({ providedIn: 'root' })
export class OfflineService {
  private readonly quran = inject(QuranService);
  private readonly sunnah = inject(SunnahService);
  readonly available = inject(SwUpdate).isEnabled;
  readonly progress = signal<OfflineProgress | null>(null);
  /** Bumped after each save so templates re-read isSaved(). */
  readonly savedVersion = signal(0);

  isSaved(key: string): boolean {
    this.savedVersion();
    try {
      return localStorage.getItem(`offline.${key}`) !== null;
    } catch {
      return false;
    }
  }

  async saveQuran(translationId: number): Promise<boolean> {
    const chapters = await firstValueFrom(this.quran.getChapters());
    return this.run(
      `quran.${translationId}`,
      chapters.map((c) => () => firstValueFrom(this.quran.getVerses(c.id, translationId))),
    );
  }

  async saveCollection(name: string): Promise<boolean> {
    const { data: books } = await firstValueFrom(this.sunnah.getBooks(name));
    const tasks = books.flatMap((b) =>
      Array.from({ length: Math.max(1, Math.ceil(b.numberOfHadith / 25)) }, (_, i) =>
        () => firstValueFrom(this.sunnah.getHadiths(name, b.bookNumber, i + 1)),
      ),
    );
    return this.run(`hadith.${name}`, tasks);
  }

  /** Runs the requests 4 at a time; marks the key saved only if every one succeeded. */
  private async run(key: string, tasks: (() => Promise<unknown>)[]): Promise<boolean> {
    if (this.progress()) return false;
    const p: OfflineProgress = { key, done: 0, total: tasks.length, failed: 0 };
    this.progress.set({ ...p });
    let next = 0;
    const worker = async () => {
      while (next < tasks.length) {
        const task = tasks[next++];
        try {
          await task();
        } catch {
          p.failed++;
        }
        p.done++;
        this.progress.set({ ...p });
      }
    };
    await Promise.all(Array.from({ length: 4 }, worker));
    this.progress.set(null);
    if (p.failed === 0) {
      try {
        localStorage.setItem(`offline.${key}`, new Date().toISOString());
      } catch {
        // storage blocked - the data is still cached, only the "saved" tick is lost
      }
      this.savedVersion.update((n) => n + 1);
    }
    return p.failed === 0;
  }
}
