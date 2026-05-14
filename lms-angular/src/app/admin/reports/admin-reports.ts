import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';

import { AdminReport } from '../models/admin.models';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-reports.html',
})
export class AdminReportsPage {
  private readonly admin = inject(AdminService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly report = signal<AdminReport | null>(null);
  readonly downloading = signal<'users' | 'courses' | null>(null);

  /** Largest monthly registration total — used to scale the trend bars. Min 1 to avoid /0. */
  readonly maxRegistration = computed(() => {
    const r = this.report();
    if (!r) return 1;
    return Math.max(1, ...r.registrationTrend.map((m) => m.total));
  });

  /** Largest monthly enrollment count — scales the enrollment trend bars. */
  readonly maxEnrollment = computed(() => {
    const r = this.report();
    if (!r) return 1;
    return Math.max(1, ...r.enrollmentTrend.map((m) => m.count));
  });

  constructor() {
    this.fetch();
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.admin.getReport().subscribe({
      next: (r) => {
        this.report.set(r);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API.'
          : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  exportCsv(kind: 'users' | 'courses'): void {
    this.downloading.set(kind);
    this.admin.downloadCsv(kind);
    // The download is fire-and-forget; clear the spinner after a short beat.
    setTimeout(() => this.downloading.set(null), 1200);
  }

  /** Bar height as a percentage of the chart area, given a value + its scale max. */
  barPct(value: number, max: number): number {
    if (max <= 0) return 0;
    return Math.round((value / max) * 100);
  }

  /** Funnel-step width as a percentage of the first (widest) step. */
  funnelPct(value: number, base: number): number {
    if (base <= 0) return 0;
    return Math.max(2, Math.round((value / base) * 100));
  }
}
