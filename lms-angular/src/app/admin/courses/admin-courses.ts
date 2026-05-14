import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AdminCourseListItem } from '../models/admin.models';
import { AdminService } from '../admin.service';

type StatusFilter = 'all' | 'published' | 'draft';

@Component({
  selector: 'app-admin-courses',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-courses.html',
})
export class AdminCoursesPage {
  private readonly admin = inject(AdminService);
  private readonly searchInput$ = new Subject<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly courses = signal<AdminCourseListItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);

  readonly searchTerm = signal('');
  readonly statusFilter = signal<StatusFilter>('all');

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  readonly rangeLabel = computed(() => {
    const total = this.totalCount();
    if (total === 0) return 'No courses';
    const start = (this.page() - 1) * this.pageSize() + 1;
    const end = Math.min(total, this.page() * this.pageSize());
    return `${start}–${end} of ${total}`;
  });

  constructor() {
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchTerm.set(term);
        this.page.set(1);
        this.fetch();
      });

    this.fetch();
  }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    const status = this.statusFilter();
    this.admin
      .getCourses({
        search: this.searchTerm() || undefined,
        isPublished: status === 'all' ? null : status === 'published',
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (res) => {
          this.courses.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.error.set(err.status === 0
            ? 'Cannot reach the API.'
            : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
        },
      });
  }

  onSearchInput(value: string): void {
    this.searchInput$.next(value);
  }

  setStatus(status: StatusFilter): void {
    this.statusFilter.set(status);
    this.page.set(1);
    this.fetch();
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.fetch();
    }
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((p) => p + 1);
      this.fetch();
    }
  }

  relativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const d = Math.round(diff / (24 * 60 * 60 * 1000));
    if (d < 1) return 'today';
    if (d === 1) return 'yesterday';
    if (d < 30) return `${d} days ago`;
    return new Date(iso).toLocaleDateString();
  }
}
