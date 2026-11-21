import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { environment } from '../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { ArabicBook, ArabicProgress, ArabicUnit, ArabicUnitSummary, ArabicWordCard, MeaningLang } from './arabic.models';

// Browser storage is only for per-viewer conveniences (prefs, last lesson); progress lives on the server.
const read = <T>(k: string, d: T): T => {
  try { return (JSON.parse(localStorage.getItem(k) ?? 'null') as T) ?? d; } catch { return d; }
};
const write = (k: string, v: unknown) => {
  try { localStorage.setItem(k, JSON.stringify(v)); } catch { /* storage blocked: keep in memory */ }
};

@Injectable({ providedIn: 'root' })
export class ArabicService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly base = `${environment.apiUrl}/arabic`;

  readonly books$: Observable<ArabicBook[]> = this.http.get<ArabicBook[]>(`${this.base}/books`).pipe(shareReplay(1));
  private readonly units = new Map<string, Observable<ArabicUnit>>();

  readonly lang = signal<MeaningLang>(read('arabic.lang', 'both'));
  readonly showTr = signal<boolean>(read('arabic.showTr', true));
  /** Best score per `${unitId}-${lessonNumber}` (lesson 0 = unit test). */
  readonly scores = signal<Record<string, number>>({});

  constructor() {
    effect(() => write('arabic.lang', this.lang()));
    effect(() => write('arabic.showTr', this.showTr()));
  }

  unit(id: string): Observable<ArabicUnit> {
    let u = this.units.get(id);
    if (!u) {
      u = this.http.get<ArabicUnit>(`${this.base}/units/${encodeURIComponent(id)}`).pipe(shareReplay(1));
      this.units.set(id, u);
    }
    return u;
  }

  words(book: number, unitId?: string | null): Observable<ArabicWordCard[]> {
    const q = unitId ? `&unitId=${encodeURIComponent(unitId)}` : '';
    return this.http.get<ArabicWordCard[]>(`${this.base}/words?book=${book}${q}`);
  }

  /** Loads the signed-in learner's best scores from the server. */
  refreshScores(): void {
    this.http.get<ArabicProgress[]>(`${this.base}/progress`).subscribe({
      next: (rows) => this.scores.set(Object.fromEntries(rows.map((r) => [`${r.unitId}-${r.lessonNumber}`, r.bestScore]))),
      error: () => this.scores.set({}),
    });
  }

  score(unitId: string, lessonNumber: number): number | undefined {
    return this.scores()[`${unitId}-${lessonNumber}`];
  }

  saveScore(unitId: string, lessonNumber: number, pct: number): void {
    const key = `${unitId}-${lessonNumber}`;
    this.scores.update((s) => ({ ...s, [key]: Math.max(s[key] ?? 0, pct) }));
    this.http.post(`${this.base}/progress`, { unitId, lessonNumber, score: pct }).subscribe();
  }

  /** % of a unit's lessons with a quiz result. */
  unitProgress(u: Pick<ArabicUnitSummary, 'id' | 'lessonCount'>): number {
    if (!u.lessonCount) return 0;
    let done = 0;
    for (let n = 1; n <= u.lessonCount; n++) if (this.score(u.id, n) != null) done++;
    return Math.round((done / u.lessonCount) * 100);
  }

  bookProgress(b: ArabicBook): number {
    return b.units.length ? Math.round(b.units.reduce((a, u) => a + this.unitProgress(u), 0) / b.units.length) : 0;
  }

  // Per user, so two learners sharing a browser don't resume each other's lesson.
  private get lastKey() { return `arabic.last.${this.auth.user()?.id ?? ''}`; }
  get lastLesson(): { unitId: string; n: number } | null { return read(this.lastKey, null); }
  set lastLesson(v: { unitId: string; n: number } | null) { write(this.lastKey, v); }
}
