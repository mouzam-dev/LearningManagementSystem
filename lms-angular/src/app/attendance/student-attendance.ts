import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';

import { MyAttendance } from './attendance.models';
import { AttendanceService } from './attendance.service';

@Component({
  selector: 'app-student-attendance',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-attendance.html',
})
export class StudentAttendancePage {
  private readonly attendance = inject(AttendanceService);

  readonly data = signal<MyAttendance | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.attendance.getMyAttendance().subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0 ? 'Cannot reach the API.' : (err.error?.message ?? 'Something went wrong.'));
      },
    });
  }

  rateClass(pct: number): string {
    return pct < 75 ? 'text-rose-600' : pct < 90 ? 'text-amber-600' : 'text-emerald-600';
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
}
