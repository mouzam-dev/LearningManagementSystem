import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { TeacherCourseListItem } from '../teacher/models/teacher.models';
import { TeacherService } from '../teacher/teacher.service';
import { jitsiRoomUrl } from './jitsi';
import { LiveSession, LiveSessionStatus } from './live-classes.models';
import { LiveClassesService } from './live-classes.service';

@Component({
  selector: 'app-teacher-live-classes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './teacher-live-classes.html',
})
export class TeacherLiveClassesPage {
  private readonly teacher = inject(TeacherService);
  private readonly live = inject(LiveClassesService);

  readonly courses = signal<TeacherCourseListItem[]>([]);
  readonly selectedCourseId = signal<string>('');
  readonly sessions = signal<LiveSession[]>([]);

  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  // Schedule form
  readonly title = signal('');
  readonly startLocal = signal(this.defaultStart());
  readonly duration = signal(60);

  readonly selectedCourse = computed(
    () => this.courses().find((c) => c.courseId === this.selectedCourseId()) ?? null,
  );

  constructor() {
    this.loadCourses();
  }

  private defaultStart(): string {
    const d = new Date(Date.now() + 5 * 60000);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  private loadCourses(): void {
    this.loading.set(true);
    this.teacher.getCourses().subscribe({
      next: (list) => {
        this.courses.set(list);
        this.loading.set(false);
        if (list.length && !this.selectedCourseId()) this.selectCourse(list[0].courseId);
      },
      error: (e: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(this.msg(e));
      },
    });
  }

  onCourseChange(ev: Event): void {
    this.selectCourse((ev.target as HTMLSelectElement).value);
  }

  selectCourse(id: string): void {
    this.selectedCourseId.set(id);
    this.error.set(null);
    this.loadSessions();
  }

  private loadSessions(): void {
    const id = this.selectedCourseId();
    if (!id) return;
    this.live.getCourseSessions(id).subscribe({
      next: (s) => this.sessions.set(s),
      error: (e: HttpErrorResponse) => this.error.set(this.msg(e)),
    });
  }

  schedule(): void {
    const courseId = this.selectedCourseId();
    if (!courseId || !this.title().trim()) {
      this.error.set('Enter a class title.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    const iso = new Date(this.startLocal()).toISOString();
    this.live
      .scheduleSession({
        courseId,
        title: this.title().trim(),
        scheduledStart: iso,
        durationMinutes: this.duration() || 60,
      })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.title.set('');
          this.loadSessions();
        },
        error: (e: HttpErrorResponse) => {
          this.busy.set(false);
          this.error.set(this.msg(e));
        },
      });
  }

  setStatus(s: LiveSession, status: LiveSessionStatus): void {
    this.busy.set(true);
    this.live.setStatus(s.id, status).subscribe({
      next: () => {
        this.busy.set(false);
        this.loadSessions();
      },
      error: (e: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(this.msg(e));
      },
    });
  }

  /** Opens the live room in a new tab on the community Jitsi server. */
  openRoom(s: LiveSession): void {
    window.open(jitsiRoomUrl(s.roomName), '_blank', 'noopener,noreferrer');
  }

  statusBadge(status: string): string {
    switch (status) {
      case 'Scheduled': return 'bg-indigo-100 text-indigo-700';
      case 'Live': return 'bg-emerald-100 text-emerald-700';
      case 'Ended': return 'bg-surface-2 text-muted';
      case 'Cancelled': return 'bg-rose-100 text-rose-700';
      default: return 'bg-surface-2 text-muted';
    }
  }

  private msg(e: HttpErrorResponse): string {
    if (e.status === 0) return 'Cannot reach the API.';
    if (e.status === 403) return 'You do not teach this course.';
    return e.error?.message ?? e.statusText ?? 'Something went wrong.';
  }
}
