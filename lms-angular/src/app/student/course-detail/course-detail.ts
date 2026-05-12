import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { CourseDetail } from '../models/lesson.models';
import { AssessmentListItem } from '../models/assessment.models';
import { StudentService } from '../student.service';

@Component({
  selector: 'app-student-course-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './course-detail.html',
})
export class StudentCourseDetail implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly router = inject(Router);

  /** Bound from the route via withComponentInputBinding(). */
  readonly courseId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly course = signal<CourseDetail | null>(null);
  readonly assessments = signal<AssessmentListItem[]>([]);

  readonly progressLabel = computed(() => {
    const c = this.course();
    if (!c) return '';
    if (c.totalLessons === 0) return 'No lessons yet';
    return `${c.completedLessons} of ${c.totalLessons} lessons complete`;
  });

  ngOnInit(): void {
    this.fetch();
  }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.studentService.getCourseDetail(this.courseId()).subscribe({
      next: (c) => {
        this.course.set(c);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.course.set(null);
        this.loading.set(false);
        this.error.set(this.formatError(err));
      },
    });

    // Assessments load in parallel — they're a separate panel and shouldn't
    // block the syllabus view.
    this.studentService.getCourseAssessments(this.courseId()).subscribe({
      next: (a) => this.assessments.set(a),
      error: () => this.assessments.set([]),
    });
  }

  continueLearning(): void {
    const next = this.course()?.nextLessonId;
    if (next) {
      this.router.navigate(['/student/lessons', next]);
    }
  }

  /** Stable indigo/violet gradient for the course banner; matches the catalog
   * fallback palette so a course's visual identity is consistent. */
  bannerGradient(id: string): string {
    const palettes = [
      'from-indigo-500 to-violet-600',
      'from-violet-500 to-fuchsia-600',
      'from-fuchsia-500 to-pink-500',
      'from-blue-500 to-indigo-600',
      'from-sky-500 to-indigo-500',
      'from-purple-500 to-indigo-600',
    ];
    let hash = 0;
    for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) | 0;
    return palettes[Math.abs(hash) % palettes.length];
  }

  formatDuration(minutes?: number | null): string {
    if (!minutes || minutes <= 0) return '';
    if (minutes < 60) return `${minutes} min`;
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return m === 0 ? `${h}h` : `${h}h ${m}m`;
  }

  /** Friendly relative-due label (e.g. "Due in 3 days", "Due today", "Past due"). */
  dueLabel(dueDate?: string | null): string {
    if (!dueDate) return '';
    const due = new Date(dueDate);
    const diffMs = due.getTime() - Date.now();
    const days = Math.round(diffMs / (1000 * 60 * 60 * 24));
    if (days < 0) return `Past due (${due.toLocaleDateString()})`;
    if (days === 0) return 'Due today';
    if (days === 1) return 'Due tomorrow';
    if (days < 14) return `Due in ${days} days`;
    return `Due ${due.toLocaleDateString()}`;
  }

  totalDuration(course: CourseDetail): string {
    const mins = course.modules
      .flatMap((m) => m.lessons)
      .reduce((sum, l) => sum + (l.duration ?? 0), 0);
    return this.formatDuration(mins);
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) return 'Cannot reach the API. Is it running on http://localhost:5116?';
    if (err.status === 403) return "You're not enrolled in this course yet.";
    if (err.status === 404) return 'This course no longer exists.';
    const body = err.error as { message?: string } | null;
    return body?.message ?? err.statusText ?? 'Failed to load the course.';
  }
}
