import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { OrgAdminService } from '../orgadmin.service';
import { OrgAdminDashboard } from '../models/orgadmin.models';

@Component({
  selector: 'app-orgadmin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './orgadmin-dashboard.html',
})
export class OrgAdminDashboardPage {
  private readonly api = inject(OrgAdminService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly data = signal<OrgAdminDashboard | null>(null);

  constructor() { this.fetch(); }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getDashboard().subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 401
          ? 'Your account has no organization assigned. Ask a Super Admin to attach you to an org.'
          : err.status === 0
            ? 'Cannot reach the API.'
            : err.error?.message ?? err.statusText ?? 'Something went wrong.');
      },
    });
  }
}
