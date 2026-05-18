import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { PagedResult } from '../../student/models/course.models';
import { RubricListItem } from '../models/rubric.models';
import { TeacherService } from '../teacher.service';

@Component({
  selector: 'app-teacher-rubrics-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './rubrics-list.html',
})
export class TeacherRubricsListPage {
  private readonly service = inject(TeacherService);
  private readonly router = inject(Router);

  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly result = signal<PagedResult<RubricListItem> | null>(null);
  readonly deleting = signal<string | null>(null);

  readonly totalPages = computed(() => this.result()?.totalPages ?? 0);
  readonly hasItems = computed(() => (this.result()?.items.length ?? 0) > 0);
  readonly emptyState = computed(() => !this.loading() && !this.hasItems() && !this.search());

  constructor() {
    this.fetch();
  }

  applyFilter(): void {
    this.page.set(1);
    this.fetch();
  }

  clearFilter(): void {
    this.search.set('');
    this.page.set(1);
    this.fetch();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.fetch();
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.getRubrics({
      search: this.search().trim() || undefined,
      page: this.page(),
      pageSize: this.pageSize,
    }).subscribe({
      next: (r) => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0 ? 'Cannot reach the API.' : 'Failed to load rubrics.');
      },
    });
  }

  edit(r: RubricListItem): void {
    this.router.navigate(['/teacher/rubrics', r.id]);
  }

  remove(r: RubricListItem): void {
    if (this.deleting()) return;
    const ok = typeof window !== 'undefined'
      ? window.confirm(`Delete the rubric "${r.name}"?`)
      : true;
    if (!ok) return;

    this.deleting.set(r.id);
    this.error.set(null);
    this.service.deleteRubric(r.id).subscribe({
      next: () => {
        this.deleting.set(null);
        const cur = this.result();
        if (cur) {
          this.result.set({
            ...cur,
            items: cur.items.filter((x) => x.id !== r.id),
            totalCount: cur.totalCount - 1,
          });
        }
      },
      error: (err: HttpErrorResponse) => {
        this.deleting.set(null);
        this.error.set(err.status === 409
          ? (err.error?.message ?? 'This rubric is in use by an assessment.')
          : 'Could not delete that rubric.');
      },
    });
  }
}
