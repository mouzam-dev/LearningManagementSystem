import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CourseAnalytics } from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

@Component({
  selector: 'app-teacher-course-analytics',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './course-analytics.html',
})
export class TeacherCourseAnalyticsPage {
  private readonly teacher = inject(TeacherService);

  readonly courseId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly data = signal<CourseAnalytics | null>(null);

  constructor() {
    effect(() => {
      const id = this.courseId();
      if (id) this.fetch(id);
    });
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getCourseAnalytics(id).subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API.'
          : err.status === 404
            ? 'This course was not found.'
            : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  relativeTime(iso?: string | null): string {
    if (!iso) return 'No activity yet';
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
