import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { TeacherCourseListItem } from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

type Visibility = 'active' | 'archived' | 'all';

@Component({
  selector: 'app-teacher-courses',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './teacher-courses.html',
})
export class TeacherCoursesPage {
  private readonly teacher = inject(TeacherService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly courses = signal<TeacherCourseListItem[]>([]);

  /** Visibility filter — defaults to active (archived courses hidden). */
  readonly visibility = signal<Visibility>('active');

  /** Inline status for the duplicate action, keyed by source courseId. */
  readonly duplicatingId = signal<string | null>(null);

  /** Filtered list per the chip selection. */
  readonly visibleCourses = computed(() => {
    const v = this.visibility();
    const all = this.courses();
    if (v === 'active') return all.filter((c) => !c.isArchived);
    if (v === 'archived') return all.filter((c) => c.isArchived);
    return all;
  });

  readonly archivedCount = computed(() => this.courses().filter((c) => c.isArchived).length);

  constructor() {
    this.fetch();
  }

  setVisibility(v: Visibility): void {
    this.visibility.set(v);
    // Re-fetch with includeArchived=true so we have the full set to filter.
    if (v !== 'active' && this.courses().every((c) => !c.isArchived)) {
      this.fetch(true);
    }
  }

  duplicate(course: TeacherCourseListItem): void {
    if (this.duplicatingId() !== null) return;
    this.duplicatingId.set(course.courseId);
    this.teacher.duplicateCourse(course.courseId).subscribe({
      next: (created) => {
        this.duplicatingId.set(null);
        // Drop the user straight into the new draft so they can rename / publish.
        this.router.navigate(['/teacher/courses', created.courseId]);
      },
      error: (err: HttpErrorResponse) => {
        this.duplicatingId.set(null);
        this.error.set(err.error?.message ?? err.statusText ?? 'Could not duplicate the course.');
      },
    });
  }

  private fetch(includeArchived = false): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getCourses(includeArchived).subscribe({
      next: (list) => {
        this.courses.set(list);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API. Is it running on http://localhost:5117?'
          : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }
}
