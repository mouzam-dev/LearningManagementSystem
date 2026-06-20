import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { TeacherCourseListItem } from '../teacher/models/teacher.models';
import { TeacherService } from '../teacher/teacher.service';
import {
  ATTENDANCE_STATUSES,
  AttendanceRecord,
  AttendanceSession,
  AttendanceStatus,
  CourseAttendanceSummary,
  SessionRoster,
  SessionStatus,
} from './attendance.models';
import { AttendanceService } from './attendance.service';

@Component({
  selector: 'app-teacher-attendance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './teacher-attendance.html',
})
export class TeacherAttendancePage {
  private readonly teacher = inject(TeacherService);
  private readonly attendance = inject(AttendanceService);

  readonly statuses = ATTENDANCE_STATUSES;

  readonly courses = signal<TeacherCourseListItem[]>([]);
  readonly selectedCourseId = signal<string>('');
  readonly sessions = signal<AttendanceSession[]>([]);
  readonly summary = signal<CourseAttendanceSummary | null>(null);

  readonly roster = signal<SessionRoster | null>(null);
  readonly records = signal<AttendanceRecord[]>([]);

  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly dirty = signal(false);
  readonly tab = signal<'sessions' | 'summary'>('sessions');

  // New-session form
  readonly newDate = signal<string>(new Date().toISOString().slice(0, 10));
  readonly newSlot = signal<number>(1);
  readonly newTopic = signal<string>('');

  readonly selectedCourse = computed(
    () => this.courses().find((c) => c.courseId === this.selectedCourseId()) ?? null,
  );

  /** Live tallies for the open grid (recomputed as the teacher edits). */
  readonly counts = computed(() => {
    const r = this.records();
    const inSet = (set: AttendanceStatus[]) => r.filter((x) => set.includes(x.status)).length;
    const present = inSet(['Present', 'Remote']);
    const late = inSet(['Late', 'LeftEarly']);
    const absent = inSet(['Absent']);
    const excused = inSet(['Excused']);
    const counted = r.length - excused;
    const pct = counted === 0 ? 0 : Math.round(((present + late * 0.5) / counted) * 1000) / 10;
    return { total: r.length, present, late, absent, excused, pct };
  });

  readonly sessionOpen = computed(() => this.roster()?.session.status === 'Open');

  constructor() {
    this.loadCourses();
  }

  private loadCourses(): void {
    this.loading.set(true);
    this.teacher.getCourses().subscribe({
      next: (list) => {
        this.courses.set(list);
        this.loading.set(false);
        if (list.length && !this.selectedCourseId()) this.selectCourse(list[0].courseId);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(this.msg(err));
      },
    });
  }

  onCourseChange(ev: Event): void {
    this.selectCourse((ev.target as HTMLSelectElement).value);
  }

  selectCourse(courseId: string): void {
    this.selectedCourseId.set(courseId);
    this.roster.set(null);
    this.records.set([]);
    this.error.set(null);
    this.loadSessions();
    this.loadSummary();
  }

  private loadSessions(): void {
    const id = this.selectedCourseId();
    if (!id) return;
    this.attendance.getCourseSessions(id).subscribe({
      next: (s) => this.sessions.set(s),
      error: (err: HttpErrorResponse) => this.error.set(this.msg(err)),
    });
  }

  private loadSummary(): void {
    const id = this.selectedCourseId();
    if (!id) return;
    this.attendance.getCourseSummary(id).subscribe({
      next: (s) => this.summary.set(s),
      error: () => this.summary.set(null),
    });
  }

  createSession(): void {
    const courseId = this.selectedCourseId();
    if (!courseId) return;
    this.busy.set(true);
    this.error.set(null);
    this.attendance
      .createSession({
        courseId,
        sessionDate: this.newDate(),
        slot: this.newSlot() || 1,
        topic: this.newTopic().trim() || null,
      })
      .subscribe({
        next: (r) => {
          this.openRoster(r);
          this.busy.set(false);
          this.newTopic.set('');
          this.loadSessions();
        },
        error: (err: HttpErrorResponse) => {
          this.busy.set(false);
          this.error.set(this.msg(err));
        },
      });
  }

  openSession(sessionId: string): void {
    this.busy.set(true);
    this.error.set(null);
    this.attendance.getSessionRoster(sessionId).subscribe({
      next: (r) => {
        this.openRoster(r);
        this.busy.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(this.msg(err));
      },
    });
  }

  private openRoster(r: SessionRoster): void {
    this.roster.set(r);
    this.records.set(r.records.map((x) => ({ ...x })));
    this.dirty.set(false);
  }

  closeRoster(): void {
    this.roster.set(null);
    this.records.set([]);
    this.dirty.set(false);
  }

  onStatusChange(studentId: string, ev: Event): void {
    const status = (ev.target as HTMLSelectElement).value as AttendanceStatus;
    this.records.update((rs) => rs.map((r) => (r.studentId === studentId ? { ...r, status } : r)));
    this.dirty.set(true);
  }

  onRemarkChange(studentId: string, ev: Event): void {
    const remark = (ev.target as HTMLInputElement).value;
    this.records.update((rs) => rs.map((r) => (r.studentId === studentId ? { ...r, remark } : r)));
    this.dirty.set(true);
  }

  markAll(status: AttendanceStatus): void {
    this.records.update((rs) => rs.map((r) => ({ ...r, status })));
    this.dirty.set(true);
  }

  save(): void {
    const session = this.roster()?.session;
    if (!session) return;
    this.busy.set(true);
    const marks = this.records().map((r) => ({
      studentId: r.studentId,
      status: r.status,
      remark: r.remark ?? null,
    }));
    this.attendance.markAttendance(session.id, marks).subscribe({
      next: (r) => {
        this.openRoster(r);
        this.busy.set(false);
        this.loadSessions();
        this.loadSummary();
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(this.msg(err));
      },
    });
  }

  changeSessionStatus(status: SessionStatus): void {
    const session = this.roster()?.session;
    if (!session) return;
    this.busy.set(true);
    this.attendance.setSessionStatus(session.id, status).subscribe({
      next: (s) => {
        const cur = this.roster();
        if (cur) this.roster.set({ ...cur, session: s });
        this.busy.set(false);
        this.loadSessions();
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(this.msg(err));
      },
    });
  }

  statusBadge(status: string): string {
    switch (status) {
      case 'Present': return 'bg-emerald-100 text-emerald-700';
      case 'Remote': return 'bg-teal-100 text-teal-700';
      case 'Late': return 'bg-amber-100 text-amber-700';
      case 'LeftEarly': return 'bg-orange-100 text-orange-700';
      case 'Excused': return 'bg-sky-100 text-sky-700';
      case 'Absent': return 'bg-rose-100 text-rose-700';
      default: return 'bg-surface-2 text-muted';
    }
  }

  sessionBadge(status: string): string {
    switch (status) {
      case 'Open': return 'bg-indigo-100 text-indigo-700';
      case 'Finalized': return 'bg-emerald-100 text-emerald-700';
      case 'Cancelled': return 'bg-rose-100 text-rose-700';
      default: return 'bg-surface-2 text-muted';
    }
  }

  private msg(err: HttpErrorResponse): string {
    if (err.status === 0) return 'Cannot reach the API.';
    if (err.status === 403) return 'You do not have access to this course.';
    if (err.status === 409) return err.error?.message ?? 'A session already exists for that date/slot.';
    return err.error?.message ?? err.statusText ?? 'Something went wrong.';
  }
}
