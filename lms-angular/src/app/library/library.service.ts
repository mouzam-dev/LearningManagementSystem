import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';

export interface QuranHit {
  surah: number; ayah: number; surahName: string; surahNameAr: string;
  textAr: string; textEn: string; textUr: string; snippet: string;
}

export interface HadithHit {
  id: number; collection: string; collectionTitle: string; bookNumber: string; bookName: string | null;
  hadithNumber: string; chapter: string | null; grade: string | null; textEn: string; textAr: string; snippet: string;
}

export interface LibraryResults {
  query: string; matchedAllWords: boolean; quranTotal: number; hadithTotal: number;
  quran: QuranHit[]; hadith: HadithHit[];
}

export interface NotesSection { heading: string; hadith: HadithHit[] }
export interface LibraryNotes {
  topic: string; matchedAllWords: boolean; quranTotal: number; hadithTotal: number;
  quran: QuranHit[]; sections: NotesSection[];
}

export type LibraryScope = 'all' | 'quran' | 'hadith';

/** Free search and topic notes over our own Qur'an and hadith tables (GET /api/library/...). */
@Injectable({ providedIn: 'root' })
export class LibraryService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/library`;

  search(q: string, scope: LibraryScope, page: number): Observable<LibraryResults> {
    const params = new HttpParams().set('q', q).set('scope', scope).set('page', page);
    return this.http.get<LibraryResults>(`${this.base}/search`, { params });
  }

  notes(q: string): Observable<LibraryNotes> {
    return this.http.get<LibraryNotes>(`${this.base}/notes`, { params: new HttpParams().set('q', q) });
  }
}

/** Splits a snippet with … highlight markers into plain/highlighted runs (no HTML involved). */
export function highlightRuns(s: string): { text: string; hit: boolean }[] {
  const runs: { text: string; hit: boolean }[] = [];
  for (const part of s.split('')) {
    const end = part.indexOf('');
    if (end < 0) { if (part) runs.push({ text: part, hit: false }); continue; }
    if (end > 0) runs.push({ text: part.slice(0, end), hit: true });
    const rest = part.slice(end + 1);
    if (rest) runs.push({ text: rest, hit: false });
  }
  return runs;
}
