import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AdminUserDetail } from '../models/admin.models';
import { AdminService } from '../admin.service';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-admin-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-user-detail.html',
})
export class AdminUserDetailPage {
  private readonly admin = inject(AdminService);
  private readonly auth = inject(AuthService);

  readonly userId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly user = signal<AdminUserDetail | null>(null);
  readonly busy = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly roles = ['Student', 'Teacher', 'Admin'];

  /** True when the row being viewed is the signed-in admin themselves. */
  readonly isSelf = computed(() => {
    const u = this.user();
    return !!u && u.userId === this.auth.user()?.id;
  });

  constructor() {
    effect(() => {
      const id = this.userId();
      if (id) this.fetch(id);
    });
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.admin.getUser(id).subscribe({
      next: (u) => {
        this.user.set(u);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.user.set(null);
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API.'
          : err.status === 404
            ? 'This user was not found.'
            : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  toggleActive(): void {
    const u = this.user();
    if (!u) return;
    const next = !u.isActive;
    this.busy.set('active');
    this.actionError.set(null);

    this.admin.setUserActive(u.userId, next).subscribe({
      next: (updated) => {
        this.user.set(updated);
        this.busy.set(null);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        this.actionError.set(err.status === 409
          ? (err.error?.message ?? "That action isn't allowed.")
          : (err.error?.message ?? err.statusText ?? 'Could not update.'));
      },
    });
  }

  changeRole(role: string): void {
    const u = this.user();
    if (!u || u.role === role) return;
    this.busy.set('role');
    this.actionError.set(null);

    this.admin.changeUserRole(u.userId, role).subscribe({
      next: (updated) => {
        this.user.set(updated);
        this.busy.set(null);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        if (err.status === 400 && err.error?.errors) {
          const messages = Object.values(err.error.errors as Record<string, string[]>).flat();
          this.actionError.set(messages.join(' '));
        } else {
          this.actionError.set(err.error?.message ?? err.statusText ?? 'Could not change role.');
        }
      },
    });
  }
}
