import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { CourseStudentListItem, TeacherCourseDetail } from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

@Component({
  selector: 'app-teacher-course-students',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './course-students.html',
})
export class TeacherCourseStudentsPage {
  private readonly teacher = inject(TeacherService);

  readonly courseId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly students = signal<CourseStudentListItem[]>([]);
  readonly course = signal<TeacherCourseDetail | null>(null);
  readonly searchTerm = signal('');

  readonly filtered = computed(() => {
    const q = this.searchTerm().trim().toLowerCase();
    if (!q) return this.students();
    return this.students().filter((s) =>
      s.studentName.toLowerCase().includes(q) || s.studentEmail.toLowerCase().includes(q));
  });

  readonly totalStudents = computed(() => this.students().length);
  readonly completedCount = computed(() => this.students().filter((s) => s.isCompleted).length);
  readonly atRiskCount = computed(() => {
    const fortnight = 14 * 24 * 60 * 60 * 1000;
    const now = Date.now();
    return this.students().filter((s) =>
      !s.isCompleted
      && s.progressPercentage < 50
      && (!s.lastActivityAt || now - new Date(s.lastActivityAt).getTime() > fortnight)).length;
  });

  constructor() {
    effect(() => {
      const id = this.courseId();
      if (id) this.fetch(id);
    });
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getCourseStudents(id).subscribe({
      next: (list) => {
        this.students.set(list);
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

    // Side-load the course title for the header. Failures are silent.
    this.teacher.getCourseDetail(id).subscribe({
      next: (c) => this.course.set(c),
      error: () => this.course.set(null),
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

  isAtRisk(s: CourseStudentListItem): boolean {
    if (s.isCompleted) return false;
    if (s.progressPercentage >= 50) return false;
    if (!s.lastActivityAt) return true;
    const fortnight = 14 * 24 * 60 * 60 * 1000;
    return Date.now() - new Date(s.lastActivityAt).getTime() > fortnight;
  }
}
