import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CourseStudentDetail } from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

@Component({
  selector: 'app-teacher-student-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './student-detail.html',
})
export class TeacherStudentDetailPage {
  private readonly teacher = inject(TeacherService);

  readonly courseId = input.required<string>();
  readonly studentId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly data = signal<CourseStudentDetail | null>(null);

  readonly modules = computed(() => {
    const d = this.data();
    if (!d) return [];
    // Group lessons back by module so the timeline shows the syllabus shape.
    const byModule = new Map<string, { title: string; order: number; lessons: typeof d.lessons }>();
    for (const l of d.lessons) {
      const key = `${l.moduleOrder}:${l.moduleTitle}`;
      if (!byModule.has(key)) {
        byModule.set(key, { title: l.moduleTitle, order: l.moduleOrder, lessons: [] });
      }
      byModule.get(key)!.lessons.push(l);
    }
    return [...byModule.values()].sort((a, b) => a.order - b.order);
  });

  constructor() {
    effect(() => {
      const c = this.courseId();
      const s = this.studentId();
      if (c && s) this.fetch(c, s);
    });
  }

  private fetch(courseId: string, studentId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getCourseStudent(courseId, studentId).subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.data.set(null);
        this.error.set(err.status === 0
          ? 'Cannot reach the API.'
          : err.status === 404
            ? 'This student is not enrolled in this course.'
            : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  formatDuration(seconds: number): string {
    if (!seconds || seconds <= 0) return '—';
    const min = Math.round(seconds / 60);
    if (min < 60) return `${min} min`;
    const h = Math.floor(min / 60);
    const m = min % 60;
    return m === 0 ? `${h}h` : `${h}h ${m}m`;
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
