import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { TeacherDashboard } from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

@Component({
  selector: 'app-teacher-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './teacher-dashboard.html',
})
export class TeacherDashboardPage {
  private readonly teacher = inject(TeacherService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly data = signal<TeacherDashboard | null>(null);

  readonly summary = computed(() => this.data()?.summary ?? null);

  constructor() {
    this.fetch();
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getDashboard().subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API. Is it running on http://localhost:5116?'
          : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  /** "3 hours ago" / "2 days ago" — for the activity timeline. */
  relativeTime(iso: string): string {
    const diffMs = Date.now() - new Date(iso).getTime();
    const min = Math.round(diffMs / 60_000);
    if (min < 1) return 'just now';
    if (min < 60) return `${min} min ago`;
    const h = Math.round(min / 60);
    if (h < 24) return `${h} hr ago`;
    const d = Math.round(h / 24);
    if (d < 14) return `${d} day${d === 1 ? '' : 's'} ago`;
    return new Date(iso).toLocaleDateString();
  }
}
