import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { CourseStudentListItem, TeacherCourseDetail } from '../models/teacher.models';
import { BulkEnrollResult, TeacherService } from '../teacher.service';

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

  // ----- Bulk enroll (BRD TCH-043) --------------------------------------

  readonly enrollOpen = signal(false);
  readonly enrollBusy = signal(false);
  readonly enrollError = signal<string | null>(null);
  readonly enrollResult = signal<BulkEnrollResult | null>(null);
  readonly enrollFileName = signal<string | null>(null);

  openEnroll(): void {
    this.enrollOpen.set(true);
    this.enrollResult.set(null);
    this.enrollError.set(null);
    this.enrollFileName.set(null);
  }

  closeEnroll(): void {
    this.enrollOpen.set(false);
    this.enrollBusy.set(false);
    // Refresh the students list if anything new landed.
    if (this.enrollResult()?.enrolledCount) {
      this.fetch(this.courseId());
    }
  }

  onEnrollFile(input: HTMLInputElement): void {
    const file = input.files?.[0];
    if (!file) return;
    this.enrollFileName.set(file.name);
    this.enrollResult.set(null);
    this.enrollError.set(null);
    this.enrollBusy.set(true);

    this.teacher.bulkEnrollStudents(this.courseId(), file).subscribe({
      next: (res) => {
        this.enrollResult.set(res);
        this.enrollBusy.set(false);
        input.value = '';
      },
      error: (err: HttpErrorResponse) => {
        this.enrollBusy.set(false);
        input.value = '';
        this.enrollError.set(err.status === 400
          ? (err.error?.message ?? 'CSV was rejected.')
          : err.status === 404 ? 'Course no longer exists.'
          : err.status === 0 ? 'Cannot reach the API.' : 'Enrollment failed.');
      },
    });
  }

  /** Build a tiny sample CSV in-memory and trigger a browser download. */
  downloadEnrollTemplate(): void {
    const lines = [
      'Email',
      'student1@example.com',
      'student2@example.com',
      'student3@example.com',
    ];
    const blob = new Blob([lines.join('\n')], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'lms-bulk-enroll-template.csv';
    a.click();
    URL.revokeObjectURL(url);
  }
}
