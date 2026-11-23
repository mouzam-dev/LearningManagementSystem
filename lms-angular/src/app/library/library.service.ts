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

export interface AiPassage { id: string; kind: 'quran' | 'hadith'; reference: string; textEn: string; textAr: string; grade: string | null }
export interface AiAnswer {
  found: boolean; title: string; summary: string; caution: string;
  sections: { heading: string; points: { text: string; sources: string[] }[] }[];
}
export interface AiResult { mode: 'answer' | 'notes'; question: string; answer: AiAnswer; passages: AiPassage[]; usedToday: number; dailyLimit: number }
export interface AiUsage { aiEnabled: boolean; used: number; limit: number }

export type LibraryScope = 'all' | 'quran' | 'hadith';

/** Search + AI answers over our own Qur'an and hadith tables (GET/POST /api/library/...). */
@Injectable({ providedIn: 'root' })
export class LibraryService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/library`;

  search(q: string, scope: LibraryScope, page: number): Observable<LibraryResults> {
    const params = new HttpParams().set('q', q).set('scope', scope).set('page', page);
    return this.http.get<LibraryResults>(`${this.base}/search`, { params });
  }

  usage(): Observable<AiUsage> {
    return this.http.get<AiUsage>(`${this.base}/usage`);
  }

  ask(question: string, mode: 'answer' | 'notes'): Observable<AiResult> {
    return this.http.post<AiResult>(`${this.base}/ask`, { question, mode });
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
