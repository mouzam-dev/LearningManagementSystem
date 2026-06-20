import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { BranchAttendanceDetail, OrgAttendanceOverview } from './attendance.models';
import { AttendanceService } from './attendance.service';

@Component({
  selector: 'app-orgadmin-attendance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './orgadmin-attendance.html',
})
export class OrgAdminAttendancePage {
  private readonly attendance = inject(AttendanceService);

  readonly overview = signal<OrgAttendanceOverview | null>(null);
  readonly detail = signal<BranchAttendanceDetail | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly fromDate = signal<string>('');
  readonly toDate = signal<string>('');

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.detail.set(null);
    this.attendance.getOrgOverview(this.fromDate() || undefined, this.toDate() || undefined).subscribe({
      next: (o) => {
        this.overview.set(o);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(this.msg(err));
      },
    });
  }

  openBranch(branchId?: string | null): void {
    if (!branchId) return;
    this.busy.set(true);
    this.attendance.getBranchDetail(branchId, this.fromDate() || undefined, this.toDate() || undefined).subscribe({
      next: (d) => {
        this.detail.set(d);
        this.busy.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(this.msg(err));
      },
    });
  }

  closeDetail(): void {
    this.detail.set(null);
  }

  exportCsv(): void {
    this.attendance.exportOverviewCsv(this.fromDate() || undefined, this.toDate() || undefined).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'attendance-overview.csv';
        a.click();
        URL.revokeObjectURL(url);
      },
      error: (err: HttpErrorResponse) => this.error.set(this.msg(err)),
    });
  }

  rateClass(pct: number): string {
    return pct < 75 ? 'text-rose-600' : pct < 90 ? 'text-amber-600' : 'text-emerald-600';
  }

  private msg(err: HttpErrorResponse): string {
    if (err.status === 0) return 'Cannot reach the API.';
    if (err.status === 401) return 'Your session expired. Sign in again.';
    return err.error?.message ?? err.statusText ?? 'Something went wrong.';
  }
}
