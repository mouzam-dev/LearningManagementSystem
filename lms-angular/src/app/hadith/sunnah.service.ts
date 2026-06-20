import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { SunnahBook, SunnahCollection, SunnahHadith, SunnahPage } from './sunnah.models';

@Injectable({ providedIn: 'root' })
export class SunnahService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/sunnah`;

  getCollections(): Observable<SunnahPage<SunnahCollection>> {
    return this.http.get<SunnahPage<SunnahCollection>>(`${this.base}/collections?limit=100`);
  }

  getBooks(collection: string): Observable<SunnahPage<SunnahBook>> {
    return this.http.get<SunnahPage<SunnahBook>>(
      `${this.base}/collections/${encodeURIComponent(collection)}/books?limit=200`,
    );
  }

  getHadiths(collection: string, bookNumber: string, page: number): Observable<SunnahPage<SunnahHadith>> {
    return this.http.get<SunnahPage<SunnahHadith>>(
      `${this.base}/collections/${encodeURIComponent(collection)}/books/${encodeURIComponent(bookNumber)}/hadiths?page=${page}&limit=25`,
    );
  }

  search(opts: { q?: string; collection?: string; grade?: string; page: number }): Observable<SunnahPage<SunnahHadith>> {
    const p = new URLSearchParams();
    if (opts.q?.trim()) p.set('q', opts.q.trim());
    if (opts.collection) p.set('collection', opts.collection);
    if (opts.grade) p.set('grade', opts.grade);
    p.set('page', String(opts.page));
    p.set('limit', '25');
    return this.http.get<SunnahPage<SunnahHadith>>(`${this.base}/search?${p.toString()}`);
  }
}
