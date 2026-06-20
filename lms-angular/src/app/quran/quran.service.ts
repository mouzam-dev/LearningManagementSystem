import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import {
  QuranChapter,
  QuranSearchResult,
  QuranVerse,
  ReciterOption,
  ResourceOption,
} from './quran.models';

/** Removes Quran.com footnote superscripts and other markup for clean reading. */
function stripHtml(s: string): string {
  return (s ?? '')
    .replace(/<sup[^>]*>.*?<\/sup>/g, '')
    .replace(/<[^>]*>/g, '')
    .trim();
}

@Injectable({ providedIn: 'root' })
export class QuranService {
  private readonly http = inject(HttpClient);
  private readonly base = 'https://api.quran.com/api/v4';

  getChapters(): Observable<QuranChapter[]> {
    return this.http.get<{ chapters: any[] }>(`${this.base}/chapters?language=en`).pipe(
      map((r) =>
        r.chapters.map((c) => ({
          id: c.id,
          nameSimple: c.name_simple,
          nameArabic: c.name_arabic,
          nameComplex: c.name_complex,
          versesCount: c.verses_count,
          revelationPlace: c.revelation_place,
          bismillahPre: c.bismillah_pre,
          translatedName: c.translated_name?.name ?? '',
        })),
      ),
    );
  }

  getVerses(chapterId: number, translationId: number): Observable<QuranVerse[]> {
    const url =
      `${this.base}/verses/by_chapter/${chapterId}` +
      `?language=en&fields=text_uthmani,text_indopak&translations=${translationId}&per_page=300`;
    return this.http.get<{ verses: any[] }>(url).pipe(
      map((r) =>
        r.verses.map((v) => ({
          verseNumber: v.verse_number,
          verseKey: v.verse_key,
          textUthmani: v.text_uthmani,
          textIndopak: v.text_indopak,
          translation: stripHtml(v.translations?.[0]?.text ?? ''),
        })),
      ),
    );
  }

  getTranslations(): Observable<ResourceOption[]> {
    return this.http
      .get<{ translations: any[] }>(`${this.base}/resources/translations?language=en`)
      .pipe(
        map((r) =>
          r.translations
            .filter((t) => t.language_name === 'english')
            .map((t) => ({ id: t.id, label: t.name }))
            .sort((a, b) => a.label.localeCompare(b.label)),
        ),
      );
  }

  getTafsirs(): Observable<ResourceOption[]> {
    return this.http
      .get<{ tafsirs: any[] }>(`${this.base}/resources/tafsirs?language=en`)
      .pipe(
        map((r) =>
          r.tafsirs
            .filter((t) => t.language_name === 'english')
            .map((t) => ({ id: t.id, label: t.name }))
            .sort((a, b) => a.label.localeCompare(b.label)),
        ),
      );
  }

  getReciters(): Observable<ReciterOption[]> {
    return this.http
      .get<{ recitations: any[] }>(`${this.base}/resources/recitations?language=en`)
      .pipe(
        map((r) =>
          r.recitations.map((x) => ({
            id: x.id,
            label: x.style ? `${x.reciter_name} — ${x.style}` : x.reciter_name,
          })),
        ),
      );
  }

  getChapterAudioUrl(reciterId: number, chapterId: number): Observable<string> {
    return this.http
      .get<{ audio_file: { audio_url: string } }>(
        `${this.base}/chapter_recitations/${reciterId}/${chapterId}`,
      )
      .pipe(map((r) => r.audio_file?.audio_url ?? ''));
  }

  getTafsir(tafsirId: number, verseKey: string): Observable<string> {
    return this.http
      .get<{ tafsir: { text: string } }>(`${this.base}/tafsirs/${tafsirId}/by_ayah/${verseKey}`)
      .pipe(map((r) => r.tafsir?.text ?? ''));
  }

  search(query: string): Observable<QuranSearchResult[]> {
    const url = `${this.base}/search?q=${encodeURIComponent(query)}&size=25&page=0&language=en`;
    return this.http.get<{ search: { results: any[] } }>(url).pipe(
      map((r) =>
        (r.search?.results ?? []).map((x) => {
          const [c, v] = String(x.verse_key).split(':');
          return {
            verseKey: x.verse_key,
            chapterId: Number(c),
            verseNumber: Number(v),
            textArabic: x.text ?? '',
          };
        }),
      ),
    );
  }
}
