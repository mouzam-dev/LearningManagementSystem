import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { PagedResult } from '../../student/models/course.models';
import { BankQuestionListItem } from '../models/bank-question.models';
import { TeacherService } from '../teacher.service';

/**
 * Question bank list page (BRD TCH-021). Filter by search + tag, paginate, and
 * jump into edit / delete. "New question" routes to the editor page so we keep
 * the form full-width (matches the existing course-builder modal-less feel).
 */
@Component({
  selector: 'app-teacher-question-bank-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './question-bank-list.html',
})
export class TeacherQuestionBankListPage {
  private readonly service = inject(TeacherService);
  private readonly router = inject(Router);

  // Filter state
  readonly search = signal('');
  readonly tag = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;

  // Async state
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly result = signal<PagedResult<BankQuestionListItem> | null>(null);
  readonly tags = signal<string[]>([]);
  readonly deleting = signal<string | null>(null);

  readonly totalPages = computed(() => this.result()?.totalPages ?? 0);
  readonly hasItems = computed(() => (this.result()?.items.length ?? 0) > 0);
  readonly emptyState = computed(() =>
    !this.loading() && !this.hasItems() && !this.search() && !this.tag(),
  );

  constructor() {
    this.fetch();
    this.fetchTags();
  }

  applyFilter(): void {
    this.page.set(1);
    this.fetch();
  }

  clearFilter(): void {
    this.search.set('');
    this.tag.set('');
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
    this.service.getBankQuestions({
      search: this.search().trim() || undefined,
      tag: this.tag().trim() || undefined,
      page: this.page(),
      pageSize: this.pageSize,
    }).subscribe({
      next: (r) => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0 ? 'Cannot reach the API.' : 'Failed to load question bank.');
      },
    });
  }

  private fetchTags(): void {
    this.service.getBankQuestionTags().subscribe({
      next: (t) => this.tags.set(t),
      error: () => this.tags.set([]),
    });
  }

  edit(q: BankQuestionListItem): void {
    this.router.navigate(['/teacher/question-bank', q.id]);
  }

  remove(q: BankQuestionListItem): void {
    if (this.deleting()) return;
    const ok = typeof window !== 'undefined'
      ? window.confirm(`Delete this question from your bank?\n\n"${q.questionText.slice(0, 80)}${q.questionText.length > 80 ? '…' : ''}"`)
      : true;
    if (!ok) return;

    this.deleting.set(q.id);
    this.service.deleteBankQuestion(q.id).subscribe({
      next: () => {
        this.deleting.set(null);
        // Optimistic: drop it from the current page; refetch the tag list since
        // the deleted question may have been the last bearer of a tag.
        const cur = this.result();
        if (cur) {
          this.result.set({
            ...cur,
            items: cur.items.filter((x) => x.id !== q.id),
            totalCount: cur.totalCount - 1,
          });
        }
        this.fetchTags();
      },
      error: () => {
        this.deleting.set(null);
        this.error.set('Could not delete that question. Please try again.');
      },
    });
  }

  typeLabel(t: string): string {
    switch (t) {
      case 'MCQ': return 'Multiple choice';
      case 'TrueFalse': return 'True / false';
      case 'ShortAnswer': return 'Short answer';
      case 'Essay': return 'Essay';
      default: return t;
    }
  }
}
