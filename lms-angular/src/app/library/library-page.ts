import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';

import { HadithHit, LibraryNotes, LibraryResults, LibraryScope, LibraryService, highlightRuns } from './library.service';

type Tab = 'search' | 'notes';

/**
 * Qur'an & hadith library (free, open to everyone): full-text search, and topic notes that gather
 * the matching verses and hadith from our own database, grouped under the books' chapter headings.
 */
@Component({
  selector: 'app-library-page',
  imports: [FormsModule],
  templateUrl: './library-page.html',
})
export class LibraryPage {
  private readonly library = inject(LibraryService);
  private readonly router = inject(Router);

  readonly amiri = "'Amiri', serif";
  readonly runs = highlightRuns;

  readonly tab = signal<Tab>('search');
  readonly text = signal('');
  readonly scope = signal<LibraryScope>('all');

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly results = signal<LibraryResults | null>(null);
  readonly page = signal(1);
  readonly moreLoading = signal(false);
  readonly open = signal<Set<number>>(new Set());

  readonly notes = signal<LibraryNotes | null>(null);
  readonly copied = signal(false);

  readonly canLoadMore = computed(() => {
    const r = this.results();
    return !!r && (r.quran.length < r.quranTotal || r.hadith.length < r.hadithTotal);
  });

  readonly examples: Record<Tab, string[]> = {
    search: ['patience', 'fasting in Ramadan', 'kindness to parents', 'الصبر'],
    notes: ['How to perform salah', 'Zakat', 'Rights of neighbours', 'Repentance'],
  };

  constructor() {
    // /library?q=...&tab=notes (home page search box, shared links): run it straight away.
    const params = inject(ActivatedRoute).snapshot.queryParamMap;
    if (params.get('tab') === 'notes') this.tab.set('notes');
    const q = params.get('q')?.trim();
    if (q) {
      this.text.set(q);
      this.run();
    }
  }

  setTab(t: Tab): void {
    this.tab.set(t);
    this.error.set(null);
    if (this.text().trim().length >= 2) this.run();
  }

  useExample(q: string): void {
    this.text.set(q);
    this.run();
  }

  run(): void {
    const q = this.text().trim();
    if (q.length < 2) return;
    this.error.set(null);
    this.loading.set(true);
    void this.router.navigate([], { queryParams: { q, tab: this.tab() === 'notes' ? 'notes' : null }, replaceUrl: true });
    if (this.tab() === 'search') {
      this.page.set(1);
      this.open.set(new Set());
      this.library.search(q, this.scope(), 1).subscribe({
        next: (r) => { this.results.set(r); this.loading.set(false); },
        error: (e) => this.fail(e),
      });
    } else {
      this.library.notes(q).subscribe({
        next: (n) => { this.notes.set(n); this.loading.set(false); },
        error: (e) => this.fail(e),
      });
    }
  }

  loadMore(): void {
    const r = this.results();
    if (!r || this.moreLoading()) return;
    const next = this.page() + 1;
    this.moreLoading.set(true);
    this.library.search(r.query, this.scope(), next).subscribe({
      next: (more) => {
        this.results.set({ ...r, quran: [...r.quran, ...more.quran], hadith: [...r.hadith, ...more.hadith] });
        this.page.set(next);
        this.moreLoading.set(false);
      },
      error: () => this.moreLoading.set(false),
    });
  }

  /** Long narrations are clipped in notes; "Read full" shows all of it. */
  short(s: string): string {
    return s.length <= 400 ? s : s.slice(0, 400).replace(/\s+\S*$/, '') + ' …';
  }

  toggle(h: HadithHit): void {
    this.open.update((s) => {
      const n = new Set(s);
      if (n.has(h.id)) n.delete(h.id); else n.add(h.id);
      return n;
    });
  }

  /** Plain-text copy of the notes with references, for a notebook or WhatsApp. */
  async copy(): Promise<void> {
    const n = this.notes();
    if (!n) return;
    const lines = [`Notes: ${n.topic}`, ''];
    if (n.quran.length) {
      lines.push("From the Qur'an");
      for (const v of n.quran) lines.push(`• ${v.textEn} (Qur'an ${v.surah}:${v.ayah})`);
      lines.push('');
    }
    for (const s of n.sections) {
      lines.push(s.heading);
      for (const h of s.hadith) lines.push(`• ${h.textEn} (${h.collectionTitle} ${h.hadithNumber}${h.grade ? ', ' + h.grade : ''})`);
      lines.push('');
    }
    lines.push('Daura-e-Ilmiyah Shariyah library');
    try {
      await navigator.clipboard.writeText(lines.join('\n'));
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    } catch {
      this.error.set('Could not copy on this device.');
    }
  }

  private fail(e: unknown): void {
    this.loading.set(false);
    const err = e as HttpErrorResponse;
    if (err?.status === 429) { this.error.set('Too many searches in a minute. Please wait a moment and try again.'); return; }
    const msg = err?.error?.message ?? Object.values(err?.error?.errors ?? {}).flat()[0];
    this.error.set(typeof msg === 'string' ? msg : 'Something went wrong. Please try again.');
  }
}
