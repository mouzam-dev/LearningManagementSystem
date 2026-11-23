import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService } from '../auth/auth.service';

import {
  AiPassage, AiResult, AiUsage, HadithHit, LibraryResults, LibraryScope, LibraryService, highlightRuns,
} from './library.service';

type Tab = 'search' | 'answer' | 'notes';

/**
 * Qur'an & hadith library: free full-text search, plus AI answers and study notes that are written
 * only from the verses and hadith in our own database (each point cites its sources).
 */
@Component({
  selector: 'app-library-page',
  imports: [FormsModule, RouterLink],
  templateUrl: './library-page.html',
})
export class LibraryPage {
  private readonly library = inject(LibraryService);
  private readonly router = inject(Router);
  readonly signedIn = inject(AuthService).isAuthenticated;
  readonly returnUrl = '/library';

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

  readonly usage = signal<AiUsage | null>(null);
  readonly ai = signal<AiResult | null>(null);
  readonly copied = signal(false);
  readonly highlighted = signal<string | null>(null);

  readonly aiLeft = computed(() => {
    const u = this.usage();
    return u ? Math.max(0, u.limit - u.used) : null;
  });
  readonly canLoadMore = computed(() => {
    const r = this.results();
    return !!r && (r.quran.length < r.quranTotal || r.hadith.length < r.hadithTotal);
  });
  readonly passageById = computed(() => new Map((this.ai()?.passages ?? []).map((p) => [p.id, p])));

  readonly examples: Record<Tab, string[]> = {
    search: ['patience', 'fasting in Ramadan', 'kindness to parents', 'الصبر'],
    answer: ['How to perform salah?', 'What breaks the fast?', 'What are the rights of neighbours?'],
    notes: ['Zakat', 'Honesty', 'Etiquette of eating', 'Repentance (tawbah)'],
  };

  constructor() {
    if (this.signedIn()) {
      this.library.usage().subscribe({ next: (u) => this.usage.set(u), error: () => this.usage.set(null) });
    }
    // /library?q=... (the home page search box): run that search straight away.
    const q = inject(ActivatedRoute).snapshot.queryParamMap.get('q')?.trim();
    if (q) {
      this.text.set(q);
      this.run();
    }
  }

  setTab(t: Tab): void {
    this.tab.set(t);
    this.error.set(null);
  }

  useExample(q: string): void {
    this.text.set(q);
    this.run();
  }

  run(): void {
    const q = this.text().trim();
    if (q.length < 2 || this.loading()) return;
    this.error.set(null);
    this.loading.set(true);
    if (this.tab() === 'search') {
      this.page.set(1);
      this.open.set(new Set());
      void this.router.navigate([], { queryParams: { q }, replaceUrl: true });
      this.library.search(q, this.scope(), 1).subscribe({
        next: (r) => { this.results.set(r); this.loading.set(false); },
        error: (e) => this.fail(e),
      });
    } else {
      this.ai.set(null);
      this.library.ask(q, this.tab() as 'answer' | 'notes').subscribe({
        next: (r) => {
          this.ai.set(r);
          this.usage.update((u) => (u ? { ...u, used: r.usedToday, limit: r.dailyLimit } : u));
          this.loading.set(false);
        },
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

  toggle(h: HadithHit): void {
    this.open.update((s) => {
      const n = new Set(s);
      if (n.has(h.id)) n.delete(h.id); else n.add(h.id);
      return n;
    });
  }

  /** Scrolls to a cited passage in the Sources list and flashes it. */
  showSource(id: string): void {
    this.highlighted.set(id);
    document.getElementById('src-' + id.replace(':', '-'))?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    setTimeout(() => this.highlighted.set(null), 2000);
  }

  sourceLabel(id: string): string {
    const p = this.passageById().get(id);
    if (!p) return id;
    return p.kind === 'quran' ? p.reference.replace(/ \(.*\)$/, '') : p.reference.split(',')[0];
  }

  anchor(p: AiPassage): string {
    return 'src-' + p.id.replace(':', '-');
  }

  /** Plain-text copy of the notes with their references, for pasting into a notebook or WhatsApp. */
  async copy(): Promise<void> {
    const r = this.ai();
    if (!r) return;
    const a = r.answer;
    const lines = [a.title, '', a.summary, ''];
    for (const s of a.sections) {
      lines.push(s.heading);
      for (const p of s.points) lines.push(`• ${p.text} (${p.sources.map((id) => this.sourceLabel(id)).join('; ')})`);
      lines.push('');
    }
    if (a.caution) lines.push(a.caution, '');
    lines.push("Sources: our Qur'an and hadith library.");
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
    const msg = err?.error?.message ?? Object.values(err?.error?.errors ?? {}).flat()[0];
    this.error.set(typeof msg === 'string' ? msg : 'Something went wrong. Please try again.');
    if (err?.status === 429) this.usage.update((u) => (u ? { ...u, used: u.limit } : u));
  }
}
