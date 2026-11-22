import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';

import { AdminAnalytics, AdminService } from '../admin.service';

/** Friendly names for the most common routes; anything else shows its path. */
const PAGE_NAMES: Record<string, string> = {
  '/home': 'Home page', '/courses': 'Course catalogue (public)', '/quran': "Qur'an (public)", '/hadith': 'Hadith (public)',
  '/login': 'Sign in', '/register': 'Register', '/student/dashboard': 'Student dashboard', '/student/catalog': 'Browse courses',
  '/student/courses/:id': 'Course page', '/student/lessons/:id': 'Lesson', '/student/assessments/:id': 'Quiz / assessment',
  '/student/quran': "Qur'an", '/student/hadith': 'Hadith', '/student/certificates': 'Certificates',
  '/teacher/dashboard': 'Teacher dashboard', '/teacher/courses': 'My courses (teacher)', '/teacher/arabic-progress': "Students' Arabic progress",
  '/learn-arabic/home': 'Learn Arabic: home', '/learn-arabic/lesson': 'Learn Arabic: lesson', '/learn-arabic/quiz': 'Learn Arabic: quiz',
  '/learn-arabic/unit': 'Learn Arabic: unit', '/learn-arabic/book': 'Learn Arabic: book', '/learn-arabic/words': 'Learn Arabic: word cards',
  '/admin/dashboard': 'Admin dashboard', '/profile': 'Profile', '/notifications': 'Notifications',
};

@Component({
  selector: 'app-admin-analytics',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './admin-analytics.html',
})
export class AdminAnalyticsPage {
  private readonly admin = inject(AdminService);

  readonly ranges = [7, 30, 90];
  readonly days = signal(30);
  readonly data = signal<AdminAnalytics | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  /** Index of the day column under the pointer / keyboard focus, for the tooltip. */
  readonly hover = signal<number | null>(null);

  readonly maxVisitors = computed(() => Math.max(1, ...(this.data()?.daily.map((d) => d.visitors) ?? [1])));
  readonly maxPageViews = computed(() => Math.max(1, ...(this.data()?.topPages.map((p) => p.views) ?? [1])));
  readonly hovered = computed(() => { const i = this.hover(); return i == null ? null : this.data()?.daily[i] ?? null; });
  /** Day labels under the chart: first, middle and last day only, so they never collide. */
  readonly axisDays = computed(() => {
    const d = this.data()?.daily ?? [];
    return d.length ? [d[0], d[Math.floor((d.length - 1) / 2)], d[d.length - 1]] : [];
  });

  constructor() {
    this.load();
  }

  setRange(days: number): void {
    this.days.set(days);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.admin.getAnalytics(this.days()).subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: (e: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(e.status === 0 ? 'Cannot reach the API.' : 'Could not load analytics.');
      },
    });
  }

  pageName(path: string): string {
    return PAGE_NAMES[path] ?? path;
  }

  share(views: number, list: { views: number }[]): number {
    const total = list.reduce((a, x) => a + x.views, 0);
    return total ? Math.round((views / total) * 100) : 0;
  }
}
