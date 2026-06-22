import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

import {
  PagedResult,
  PublicCourseListItem,
  PublicTeacher,
} from './public-course.models';
import { PublicCoursesService } from './public-courses.service';

/** Branded gradient fallbacks for cards with no thumbnail (full utility strings
 *  so the Tailwind v4 scanner keeps them). */
const THUMBNAIL_GRADIENTS = [
  'from-indigo-500 to-violet-600',
  'from-violet-500 to-fuchsia-600',
  'from-fuchsia-500 to-pink-500',
  'from-blue-500 to-indigo-600',
  'from-sky-500 to-indigo-500',
  'from-purple-500 to-indigo-600',
] as const;

@Component({
  selector: 'app-public-courses',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './public-courses.html',
})
export class PublicCoursesPage implements OnInit {
  private readonly courses = inject(PublicCoursesService);

  readonly searchControl = new FormControl<string>('', { nonNullable: true });
  readonly categoryControl = new FormControl<string>('', { nonNullable: true });
  readonly teacherControl = new FormControl<string>('', { nonNullable: true });

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly result = signal<PagedResult<PublicCourseListItem> | null>(null);
  readonly categories = signal<string[]>([]);
  readonly teachers = signal<PublicTeacher[]>([]);

  readonly currentPage = signal(1);
  readonly pageSize = 12;

  readonly rangeLabel = computed(() => {
    const r = this.result();
    if (!r || r.totalCount === 0) return '0 courses';
    const start = (r.page - 1) * r.pageSize + 1;
    const end = Math.min(r.page * r.pageSize, r.totalCount);
    return `${start}–${end} of ${r.totalCount}`;
  });

  readonly pageNumbers = computed<number[]>(() => {
    const r = this.result();
    if (!r || r.totalPages <= 1) return [];
    return Array.from({ length: r.totalPages }, (_, i) => i + 1);
  });

  readonly hasFilters = computed(
    () =>
      !!this.searchControl.value ||
      !!this.categoryControl.value ||
      !!this.teacherControl.value,
  );

  ngOnInit(): void {
    this.fetchCategories();
    this.fetchTeachers();
    this.fetch();

    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.currentPage.set(1);
        this.fetch();
      });

    this.categoryControl.valueChanges.subscribe(() => {
      this.currentPage.set(1);
      this.fetch();
    });

    this.teacherControl.valueChanges.subscribe(() => {
      this.currentPage.set(1);
      this.fetch();
    });
  }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.courses
      .getCourses({
        search: this.searchControl.value,
        category: this.categoryControl.value,
        teacherId: this.teacherControl.value,
        page: this.currentPage(),
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.result.set(res);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.result.set(null);
          this.loading.set(false);
          this.error.set(this.formatError(err));
        },
      });
  }

  fetchCategories(): void {
    this.courses.getCategories().subscribe({
      next: (cats) => this.categories.set(cats),
      error: () => this.categories.set([]),
    });
  }

  fetchTeachers(): void {
    this.courses.getTeachers().subscribe({
      next: (t) => this.teachers.set(t),
      error: () => this.teachers.set([]),
    });
  }

  goToPage(page: number): void {
    const r = this.result();
    if (!r || page < 1 || page > r.totalPages) return;
    this.currentPage.set(page);
    this.fetch();
    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.categoryControl.setValue('', { emitEvent: false });
    this.teacherControl.setValue('', { emitEvent: false });
    this.currentPage.set(1);
    this.fetch();
  }

  initial(name: string): string {
    return (name?.trim()?.charAt(0) || '?').toUpperCase();
  }

  gradientFor(id: string): string {
    let hash = 0;
    for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) | 0;
    return THUMBNAIL_GRADIENTS[Math.abs(hash) % THUMBNAIL_GRADIENTS.length];
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Cannot reach the API. Please try again in a moment.';
    }
    const body = err.error as { message?: string } | null;
    return body?.message || err.statusText || 'Could not load courses.';
  }
}
