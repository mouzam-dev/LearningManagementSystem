import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  ARABIC_FONTS,
  DEFAULT_RECITER,
  QURAN_SCRIPTS,
  QURAN_TAFSIRS,
  QURAN_TRANSLATIONS,
  QuranChapter,
  QuranSearchResult,
  QuranVerse,
  ReciterOption,
  ResourceOption,
} from './quran.models';
import { QuranService } from './quran.service';

const LS = {
  chapter: 'quran.lastChapter',
  translation: 'quran.translation',
  tafsir: 'quran.tafsir',
  reciter: 'quran.reciter',
  font: 'quran.font',
  script: 'quran.script',
};
const BISMILLAH = 'بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ';

interface TafsirState {
  loading: boolean;
  html: string;
}

@Component({
  selector: 'app-student-quran',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './quran.html',
  styles: [
    `
      :host ::ng-deep .tafsir-html {
        font-size: 0.875rem;
        line-height: 1.65;
      }
      :host ::ng-deep .tafsir-html h1,
      :host ::ng-deep .tafsir-html h2,
      :host ::ng-deep .tafsir-html h3 {
        font-weight: 700;
        margin: 0.6rem 0 0.3rem;
      }
      :host ::ng-deep .tafsir-html p {
        margin: 0.5rem 0;
      }
    `,
  ],
})
export class StudentQuranPage {
  private readonly quran = inject(QuranService);

  readonly fonts = ARABIC_FONTS;
  readonly scripts = QURAN_SCRIPTS;
  readonly bismillah = BISMILLAH;
  readonly nastaliq = "'Noto Nastaliq Urdu', 'Noto Naskh Arabic', serif";

  readonly chapters = signal<QuranChapter[]>([]);
  readonly translations = signal<ResourceOption[]>(QURAN_TRANSLATIONS);
  readonly tafsirs = signal<ResourceOption[]>(QURAN_TAFSIRS);
  readonly reciters = signal<ReciterOption[]>([]);

  readonly verses = signal<QuranVerse[]>([]);
  readonly search = signal(''); // sūrah-list filter

  readonly selectedId = signal<number>(this.num(LS.chapter, 1, 114));
  // 0 = "None" (plain Arabic) — also the default until the reader opts into one.
  readonly translationId = signal<number>(this.allowed(LS.translation, QURAN_TRANSLATIONS, 0));
  readonly tafsirId = signal<number>(this.allowed(LS.tafsir, QURAN_TAFSIRS, 0));
  readonly reciterId = signal<number>(this.num(LS.reciter, DEFAULT_RECITER));
  readonly fontFamily = signal<string>(localStorage.getItem(LS.font) ?? ARABIC_FONTS[0].family);
  readonly scriptKey = signal<'uthmani' | 'indopak'>(
    localStorage.getItem(LS.script) === 'indopak' ? 'indopak' : 'uthmani',
  );
  readonly fontScale = signal(1);

  readonly audioUrl = signal<string | null>(null);
  readonly tafsirByKey = signal<Record<string, TafsirState>>({});

  // Full-text search
  readonly query = signal('');
  readonly searchResults = signal<QuranSearchResult[]>([]);
  readonly searching = signal(false);
  readonly searchOpen = signal(false);

  readonly loadingChapters = signal(true);
  readonly loadingVerses = signal(false);
  readonly error = signal<string | null>(null);

  private pendingScrollAyah: number | null = null;

  readonly filteredChapters = computed(() => {
    const q = this.search().trim().toLowerCase();
    const list = this.chapters();
    if (!q) return list;
    return list.filter(
      (c) =>
        c.nameSimple.toLowerCase().includes(q) ||
        c.translatedName.toLowerCase().includes(q) ||
        String(c.id) === q,
    );
  });
  readonly selectedChapter = computed(
    () => this.chapters().find((c) => c.id === this.selectedId()) ?? null,
  );
  readonly arabicSize = computed(() => `${(1.9 * this.fontScale()).toFixed(2)}rem`);
  readonly translationSize = computed(() => `${(1.0 * this.fontScale()).toFixed(2)}rem`);
  readonly translationRtl = computed(
    () => this.translations().find((t) => t.id === this.translationId())?.rtl ?? false,
  );
  readonly tafsirRtl = computed(
    () => this.tafsirs().find((t) => t.id === this.tafsirId())?.rtl ?? false,
  );

  constructor() {
    this.loadResources();
  }

  private num(key: string, def: number, max = Number.MAX_SAFE_INTEGER): number {
    const v = Number(localStorage.getItem(key));
    return Number.isFinite(v) && v >= 1 && v <= max ? v : def;
  }

  /** Returns the stored id only if it's in the allowed list, else the default. */
  private allowed(key: string, list: ResourceOption[], def: number): number {
    const v = Number(localStorage.getItem(key));
    return list.some((x) => x.id === v) ? v : def;
  }

  private loadResources(): void {
    this.loadingChapters.set(true);
    this.quran.getChapters().subscribe({
      next: (list) => {
        this.chapters.set(list);
        this.loadingChapters.set(false);
        this.loadVerses();
        this.loadAudio();
      },
      error: (e: HttpErrorResponse) => {
        this.loadingChapters.set(false);
        this.error.set(this.msg(e));
      },
    });
    this.quran.getReciters().subscribe({ next: (r) => this.reciters.set(r), error: () => {} });
  }

  private loadVerses(): void {
    this.loadingVerses.set(true);
    this.error.set(null);
    this.verses.set([]);
    this.tafsirByKey.set({});
    this.quran.getVerses(this.selectedId(), this.translationId()).subscribe({
      next: (v) => {
        this.verses.set(v);
        this.loadingVerses.set(false);
        if (this.pendingScrollAyah != null) {
          const n = this.pendingScrollAyah;
          this.pendingScrollAyah = null;
          setTimeout(
            () => document.getElementById('ayah-' + n)?.scrollIntoView({ behavior: 'smooth', block: 'center' }),
            80,
          );
        }
      },
      error: (e: HttpErrorResponse) => {
        this.loadingVerses.set(false);
        this.error.set(this.msg(e));
      },
    });
  }

  private loadAudio(): void {
    this.audioUrl.set(null);
    this.quran.getChapterAudioUrl(this.reciterId(), this.selectedId()).subscribe({
      next: (url) => this.audioUrl.set(url || null),
      error: () => this.audioUrl.set(null),
    });
  }

  selectChapter(id: number): void {
    if (id < 1 || id > 114) return;
    const scrollTop = this.pendingScrollAyah == null;
    this.selectedId.set(id);
    localStorage.setItem(LS.chapter, String(id));
    this.loadVerses();
    this.loadAudio();
    if (scrollTop) window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  onTranslationChange(ev: Event): void {
    const id = +(ev.target as HTMLSelectElement).value;
    this.translationId.set(id);
    localStorage.setItem(LS.translation, String(id));
    this.loadVerses();
  }
  onTafsirChange(ev: Event): void {
    const id = +(ev.target as HTMLSelectElement).value;
    this.tafsirId.set(id);
    localStorage.setItem(LS.tafsir, String(id));
    this.tafsirByKey.set({}); // re-fetch with the new tafsir on next expand
  }
  onReciterChange(ev: Event): void {
    const id = +(ev.target as HTMLSelectElement).value;
    this.reciterId.set(id);
    localStorage.setItem(LS.reciter, String(id));
    this.loadAudio();
  }
  onFontChange(ev: Event): void {
    const f = (ev.target as HTMLSelectElement).value;
    this.fontFamily.set(f);
    localStorage.setItem(LS.font, f);
  }
  onScriptChange(ev: Event): void {
    const k = (ev.target as HTMLSelectElement).value === 'indopak' ? 'indopak' : 'uthmani';
    this.scriptKey.set(k);
    localStorage.setItem(LS.script, k);
  }

  arabicText(v: QuranVerse): string {
    return this.scriptKey() === 'indopak' ? v.textIndopak || v.textUthmani : v.textUthmani;
  }

  prev(): void {
    const c = this.selectedChapter();
    if (c && c.id > 1) this.selectChapter(c.id - 1);
  }
  next(): void {
    const c = this.selectedChapter();
    if (c && c.id < 114) this.selectChapter(c.id + 1);
  }
  biggerFont(): void {
    this.fontScale.update((s) => Math.min(1.6, +(s + 0.1).toFixed(2)));
  }
  smallerFont(): void {
    this.fontScale.update((s) => Math.max(0.8, +(s - 0.1).toFixed(2)));
  }

  toggleTafsir(verseKey: string): void {
    const cur = this.tafsirByKey();
    if (cur[verseKey]) {
      const copy = { ...cur };
      delete copy[verseKey];
      this.tafsirByKey.set(copy);
      return;
    }
    this.tafsirByKey.set({ ...cur, [verseKey]: { loading: true, html: '' } });
    this.quran.getTafsir(this.tafsirId(), verseKey).subscribe({
      next: (html) =>
        this.tafsirByKey.update((m) => ({
          ...m,
          [verseKey]: { loading: false, html: html || 'No tafsir available for this āyah.' },
        })),
      error: () =>
        this.tafsirByKey.update((m) => ({
          ...m,
          [verseKey]: { loading: false, html: 'Could not load tafsir.' },
        })),
    });
  }

  doSearch(): void {
    const q = this.query().trim();
    if (q.length < 2) return;
    this.searching.set(true);
    this.searchOpen.set(true);
    this.quran.search(q).subscribe({
      next: (res) => {
        this.searchResults.set(res);
        this.searching.set(false);
      },
      error: () => {
        this.searchResults.set([]);
        this.searching.set(false);
      },
    });
  }

  goToResult(r: QuranSearchResult): void {
    this.searchOpen.set(false);
    this.pendingScrollAyah = r.verseNumber;
    this.selectChapter(r.chapterId);
  }
  closeSearch(): void {
    this.searchOpen.set(false);
  }

  private msg(e: HttpErrorResponse): string {
    if (e.status === 0) return 'Cannot reach the Quran.com API — check your connection.';
    return 'Could not load the Qur’an text. Please try again.';
  }
}
