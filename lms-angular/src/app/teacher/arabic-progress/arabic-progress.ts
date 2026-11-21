import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';

import { TeacherArabicStudent } from '../../arabic/arabic.models';
import { TeacherService } from '../teacher.service';

@Component({
  selector: 'app-teacher-arabic-progress',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './arabic-progress.html',
})
export class TeacherArabicProgressPage {
  private readonly teacher = inject(TeacherService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly students = signal<TeacherArabicStudent[]>([]);
  readonly totalLessons = signal(0);
  readonly totalUnits = signal(0);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 25;
  readonly search = signal('');

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly started = computed(() => this.students().filter((s) => s.lastActivity).length);

  constructor() {
    this.fetch();
  }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);
    this.teacher.getArabicProgress(this.search().trim(), this.page(), this.pageSize).subscribe({
      next: (r) => {
        this.students.set(r.students.items);
        this.totalCount.set(r.students.totalCount);
        this.totalLessons.set(r.totalLessons);
        this.totalUnits.set(r.totalUnits);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0 ? 'Cannot reach the API.' : (err.error?.message ?? 'Could not load progress.'));
      },
    });
  }

  runSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
    this.fetch();
  }

  goTo(page: number): void {
    this.page.set(page);
    this.fetch();
  }

  pct(s: TeacherArabicStudent): number {
    return this.totalLessons() ? Math.round((s.lessonsDone / this.totalLessons()) * 100) : 0;
  }

  position(s: TeacherArabicStudent): string {
    if (s.lastBook == null) return 'Not started';
    const where = `Book ${s.lastBook} · Unit ${s.lastUnit}`;
    return s.lastLesson ? `${where} · Lesson ${s.lastLesson}` : `${where} · Unit test`;
  }
}
