import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { SunnahBook, SunnahCollection, SunnahHadith } from './sunnah.models';
import { SunnahService } from './sunnah.service';

type View = 'collections' | 'books' | 'hadiths' | 'search';

const GRADE_OPTIONS = ['Sahih', 'Hasan', 'Daif', 'Maudu', 'Other'];

@Component({
  selector: 'app-student-hadith',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './student-hadith.html',
  styles: [
    `
      :host ::ng-deep .hadith-en p { margin: 0.5rem 0; }
      :host ::ng-deep .hadith-ar p { margin: 0.6rem 0; }
    `,
  ],
})
export class StudentHadithPage {
  private readonly sunnah = inject(SunnahService);

  readonly amiri = "'Amiri', serif";
  readonly gradeOptions = GRADE_OPTIONS;

  readonly view = signal<View>('collections');
  readonly collections = signal<SunnahCollection[]>([]);
  readonly books = signal<SunnahBook[]>([]);
  readonly hadiths = signal<SunnahHadith[]>([]);
  readonly selectedCollection = signal<SunnahCollection | null>(null);
  readonly selectedBook = signal<SunnahBook | null>(null);
  readonly page = signal(1);
  readonly hasNext = signal(false);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  // ----- search -----
  readonly searchQuery = signal('');
  readonly filterCollection = signal('');
  readonly filterGrade = signal('');
  readonly searchResults = signal<SunnahHadith[]>([]);
  readonly searchPage = signal(1);
  readonly searchHasNext = signal(false);
  readonly searchTotal = signal(0);
  readonly searching = signal(false);

  /** Map collection slug -> title for labelling search results. */
  private readonly titleBySlug = computed(() => {
    const m = new Map<string, string>();
    for (const c of this.collections()) m.set(c.name, c.titleEn);
    return m;
  });

  constructor() {
    this.loadCollections();
  }

  collectionTitle(slug: string): string {
    return this.titleBySlug().get(slug) ?? slug;
  }

  loadCollections(): void {
    this.loading.set(true);
    this.error.set(null);
    this.sunnah.getCollections().subscribe({
      next: (p) => {
        this.collections.set(p.data.filter((c) => c.hasBooks && c.totalHadith > 0));
        this.loading.set(false);
      },
      error: (e: HttpErrorResponse) => this.fail(e),
    });
  }

  openCollection(c: SunnahCollection): void {
    this.selectedCollection.set(c);
    this.view.set('books');
    this.loading.set(true);
    this.error.set(null);
    this.books.set([]);
    this.sunnah.getBooks(c.name).subscribe({
      next: (p) => {
        this.books.set(p.data.filter((b) => b.numberOfHadith > 0));
        this.loading.set(false);
      },
      error: (e: HttpErrorResponse) => this.fail(e),
    });
  }

  openBook(b: SunnahBook): void {
    this.selectedBook.set(b);
    this.page.set(1);
    this.view.set('hadiths');
    this.loadHadiths();
  }

  loadHadiths(): void {
    const c = this.selectedCollection();
    const b = this.selectedBook();
    if (!c || !b) return;
    this.loading.set(true);
    this.error.set(null);
    this.sunnah.getHadiths(c.name, b.bookNumber, this.page()).subscribe({
      next: (p) => {
        this.hadiths.set(p.data);
        this.hasNext.set(p.next != null);
        this.loading.set(false);
        window.scrollTo({ top: 0, behavior: 'smooth' });
      },
      error: (e: HttpErrorResponse) => this.fail(e),
    });
  }

  nextPage(): void {
    if (this.hasNext()) {
      this.page.update((n) => n + 1);
      this.loadHadiths();
    }
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update((n) => n - 1);
      this.loadHadiths();
    }
  }

  backToCollections(): void {
    this.view.set('collections');
    this.selectedCollection.set(null);
    this.selectedBook.set(null);
  }

  backToBooks(): void {
    this.view.set('books');
    this.selectedBook.set(null);
  }

  // ----- search -----

  runSearch(): void {
    const q = this.searchQuery().trim();
    if (!q && !this.filterCollection() && !this.filterGrade()) return; // nothing to search
    this.searchPage.set(1);
    this.fetchSearch();
  }

  private fetchSearch(): void {
    this.view.set('search');
    this.searching.set(true);
    this.error.set(null);
    this.sunnah
      .search({
        q: this.searchQuery(),
        collection: this.filterCollection(),
        grade: this.filterGrade(),
        page: this.searchPage(),
      })
      .subscribe({
        next: (p) => {
          this.searchResults.set(p.data);
          this.searchTotal.set(p.total);
          this.searchHasNext.set(p.next != null);
          this.searching.set(false);
          window.scrollTo({ top: 0, behavior: 'smooth' });
        },
        error: (e: HttpErrorResponse) => {
          this.searching.set(false);
          this.fail(e);
        },
      });
  }

  searchNext(): void {
    if (this.searchHasNext()) {
      this.searchPage.update((n) => n + 1);
      this.fetchSearch();
    }
  }

  searchPrev(): void {
    if (this.searchPage() > 1) {
      this.searchPage.update((n) => n - 1);
      this.fetchSearch();
    }
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.filterCollection.set('');
    this.filterGrade.set('');
    this.searchResults.set([]);
    this.searchTotal.set(0);
    this.searchPage.set(1);
    this.view.set('collections');
  }

  gradeBadge(g?: string | null): string {
    const s = (g || '').toLowerCase();
    if (s.includes('sahih') || s.includes('authentic')) return 'bg-emerald-100 text-emerald-700';
    if (s.includes('hasan') || s.includes('good')) return 'bg-sky-100 text-sky-700';
    if (s.includes('da') || s.includes('weak')) return 'bg-amber-100 text-amber-700';
    if (s.includes('maudu') || s.includes('fabric')) return 'bg-rose-100 text-rose-700';
    return 'bg-surface-2 text-muted';
  }

  private fail(e: HttpErrorResponse): void {
    this.loading.set(false);
    this.error.set(
      e.status === 0
        ? 'Cannot reach the server.'
        : (e.error?.message ?? 'Could not load hadith data. Please try again.'),
    );
  }
}
