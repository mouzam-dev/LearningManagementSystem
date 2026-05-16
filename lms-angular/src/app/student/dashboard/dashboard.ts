import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../auth/auth.service';
import { Dashboard } from '../models/dashboard.models';
import { StudentAnnouncement, StudentService } from '../student.service';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class StudentDashboard implements OnInit {
  private readonly studentService = inject(StudentService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.auth.user;
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly dashboard = signal<Dashboard | null>(null);
  readonly announcements = signal<StudentAnnouncement[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.studentService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.dashboard.set(null);
        this.loading.set(false);
        this.error.set(this.formatError(err));
      },
    });
    // Best-effort — failure here shouldn't break the rest of the dashboard.
    this.studentService.getAnnouncements(5).subscribe({
      next: (rows) => this.announcements.set(rows),
      error: () => this.announcements.set([]),
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

  continueCourse(courseId: string): void {
    this.router.navigate(['/student/courses', courseId]);
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Cannot reach the API. Is it running on http://localhost:5116?';
    }
    if (err.status === 403) {
      return 'You need a Student account to view this dashboard.';
    }
    const body = err.error as { message?: string; errors?: Record<string, string[]> } | null;
    if (body?.errors) {
      return Object.values(body.errors).flat().join(' ');
    }
    if (body?.message) {
      return body.message;
    }
    return err.statusText || 'Failed to load the dashboard.';
  }
}
