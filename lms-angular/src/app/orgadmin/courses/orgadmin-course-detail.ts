import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { OrgCourseDetail } from '../models/orgadmin.models';
import { OrgAdminService } from '../orgadmin.service';

@Component({
  selector: 'app-orgadmin-course-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './orgadmin-course-detail.html',
})
export class OrgAdminCourseDetailPage {
  private readonly api = inject(OrgAdminService);
  private readonly router = inject(Router);

  readonly courseId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly course = signal<OrgCourseDetail | null>(null);
  readonly busy = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  constructor() {
    effect(() => {
      const id = this.courseId();
      if (id) this.fetch(id);
    });
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getCourse(id).subscribe({
      next: (c) => {
        this.course.set(c);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.course.set(null);
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API.'
          : err.status === 404
            ? 'This course was not found in your organization.'
            : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  togglePublished(): void {
    const c = this.course();
    if (!c) return;
    const next = !c.isPublished;
    this.busy.set('publish');
    this.actionError.set(null);

    this.api.setCoursePublished(c.courseId, next).subscribe({
      next: (updated) => {
        this.course.set(updated);
        this.busy.set(null);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        this.actionError.set(err.error?.message ?? err.statusText ?? 'Could not update.');
      },
    });
  }

  deleteCourse(): void {
    const c = this.course();
    if (!c) return;
    const msg =
      `Delete "${c.title}"? This permanently removes its ${c.moduleCount} module(s), ` +
      `${c.lessonCount} lesson(s), ${c.assessmentCount} assessment(s), and ` +
      `${c.studentCount} enrolment(s). This cannot be undone.`;
    if (typeof window !== 'undefined' && !window.confirm(msg)) return;

    this.busy.set('delete');
    this.actionError.set(null);

    this.api.deleteCourse(c.courseId).subscribe({
      next: () => {
        this.busy.set(null);
        this.router.navigate(['/orgadmin/courses']);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        this.actionError.set(err.error?.message ?? err.statusText ?? 'Could not delete the course.');
      },
    });
  }
}
