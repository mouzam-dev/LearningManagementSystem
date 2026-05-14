import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { GradingQueueItem } from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

@Component({
  selector: 'app-teacher-grading-inbox',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './grading-inbox.html',
})
export class TeacherGradingInboxPage {
  private readonly teacher = inject(TeacherService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<GradingQueueItem[]>([]);
  readonly showGraded = signal(false);

  readonly pendingCount = computed(() => this.items().filter(i => i.score === null || i.score === undefined).length);

  constructor() {
    effect(() => {
      const include = this.showGraded();
      this.fetch(include);
    });
  }

  private fetch(includeGraded: boolean): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getGradingQueue(includeGraded).subscribe({
      next: (list) => {
        this.items.set(list);
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

  toggleShowGraded(): void {
    this.showGraded.update(v => !v);
  }

  relativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const min = Math.round(diff / 60_000);
    if (min < 1) return 'just now';
    if (min < 60) return `${min} min ago`;
    const h = Math.round(min / 60);
    if (h < 24) return `${h} hr ago`;
    const d = Math.round(h / 24);
    if (d < 14) return `${d} day${d === 1 ? '' : 's'} ago`;
    return new Date(iso).toLocaleDateString();
  }
}
