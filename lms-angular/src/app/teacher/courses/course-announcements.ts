import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { TeacherAnnouncement, TeacherService } from '../teacher.service';

/**
 * Announcements panel for a single course. Drops into the course-builder
 * page as a self-contained section so the builder stays focused on modules
 * + lessons. Posting an announcement fans out as a notification to every
 * enrolled student (backend side).
 */
@Component({
  selector: 'app-course-announcements',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './course-announcements.html',
})
export class CourseAnnouncementsPanel {
  private readonly api = inject(TeacherService);
  private readonly fb = inject(FormBuilder);

  readonly courseId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<TeacherAnnouncement[]>([]);

  readonly composerOpen = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    body: ['', [Validators.required, Validators.maxLength(8000)]],
    isPinned: [false],
  });

  constructor() {
    effect(() => {
      const id = this.courseId();
      if (id) this.fetch();
    });
  }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getCourseAnnouncements(this.courseId()).subscribe({
      next: (rows) => { this.items.set(rows); this.loading.set(false); },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Could not load announcements.');
      },
    });
  }

  openComposer(): void {
    this.form.reset({ title: '', body: '', isPinned: false });
    this.submitError.set(null);
    this.composerOpen.set(true);
  }

  closeComposer(): void { this.composerOpen.set(false); }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.submitError.set(null);
    const v = this.form.getRawValue();
    this.api.createCourseAnnouncement(this.courseId(), {
      title: v.title.trim(),
      body: v.body.trim(),
      isPinned: v.isPinned,
    }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.composerOpen.set(false);
        this.fetch();
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const errors = err.error?.errors as Record<string, string[]> | undefined;
        const first = errors ? Object.values(errors).flat()[0] : undefined;
        this.submitError.set(first ?? err.error?.message ?? 'Could not post announcement.');
      },
    });
  }

  relativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const m = Math.round(diff / 60000);
    if (m < 1) return 'just now';
    if (m < 60) return `${m} min ago`;
    const h = Math.round(m / 60);
    if (h < 24) return `${h} hr ago`;
    const d = Math.round(h / 24);
    if (d < 14) return `${d} day${d === 1 ? '' : 's'} ago`;
    return new Date(iso).toLocaleDateString();
  }
}
