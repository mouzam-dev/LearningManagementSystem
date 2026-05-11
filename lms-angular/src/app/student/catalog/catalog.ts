import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { CourseListItem, PagedResult } from '../models/course.models';
import { StudentService } from '../student.service';

@Component({
  selector: 'app-student-catalog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './catalog.html',
})
export class StudentCatalog implements OnInit {
  private readonly studentService = inject(StudentService);

  readonly searchControl = new FormControl<string>('', { nonNullable: true });
  readonly categoryControl = new FormControl<string>('', { nonNullable: true });

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly result = signal<PagedResult<CourseListItem> | null>(null);
  readonly categories = signal<string[]>([]);
  /** Per-card busy state keyed by courseId. */
  readonly enrollingIds = signal<ReadonlySet<string>>(new Set());

  readonly currentPage = signal(1);
  readonly pageSize = 12;

  /** Range string like "1–12 of 47" for the filter bar. */
  readonly rangeLabel = computed(() => {
    const r = this.result();
    if (!r || r.totalCount === 0) return '0 results';
    const start = (r.page - 1) * r.pageSize + 1;
    const end = Math.min(r.page * r.pageSize, r.totalCount);
    return `${start}–${end} of ${r.totalCount}`;
  });

  ngOnInit(): void {
    this.fetchCategories();
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
  }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.studentService
      .getCourses({
        search: this.searchControl.value,
        category: this.categoryControl.value,
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
    this.studentService.getCategories().subscribe({
      next: (cats) => this.categories.set(cats),
      error: () => this.categories.set([]),
    });
  }

  enroll(course: CourseListItem): void {
    if (course.isEnrolled || this.enrollingIds().has(course.courseId)) return;

    this.enrollingIds.update((s) => new Set(s).add(course.courseId));

    this.studentService.enroll(course.courseId).subscribe({
      next: (updated) => {
        this.enrollingIds.update((s) => {
          const next = new Set(s);
          next.delete(course.courseId);
          return next;
        });
        // Update the matching item in place so the card re-renders to "Enrolled".
        const r = this.result();
        if (r) {
          this.result.set({
            ...r,
            items: r.items.map((c) => (c.courseId === updated.courseId ? updated : c)),
          });
        }
      },
      error: (err: HttpErrorResponse) => {
        this.enrollingIds.update((s) => {
          const next = new Set(s);
          next.delete(course.courseId);
          return next;
        });
        this.error.set(this.formatError(err));
      },
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
    this.currentPage.set(1);
    this.fetch();
  }

  initial(name: string): string {
    return (name?.trim()?.charAt(0) || '?').toUpperCase();
  }

  /** Stable hue per-course so the gradient fallback is varied but deterministic. */
  hueFor(id: string): number {
    let hash = 0;
    for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) | 0;
    return Math.abs(hash) % 360;
  }

  trackPage(_: number, p: number): number {
    return p;
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Cannot reach the API. Is it running on http://localhost:5116?';
    }
    if (err.status === 403) {
      return 'You need a Student account to use the catalog.';
    }
    if (err.status === 409) {
      return (err.error as { message?: string } | null)?.message ?? 'Could not enroll.';
    }
    if (err.status === 404) {
      return (err.error as { message?: string } | null)?.message ?? 'Course not found.';
    }
    const body = err.error as { message?: string; errors?: Record<string, string[]> } | null;
    if (body?.errors) {
      return Object.values(body.errors).flat().join(' ');
    }
    if (body?.message) {
      return body.message;
    }
    return err.statusText || 'Something went wrong.';
  }
}
