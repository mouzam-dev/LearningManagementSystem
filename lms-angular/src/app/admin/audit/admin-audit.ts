import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';

import { AdminAuditLog } from '../models/admin.models';
import { AdminService } from '../admin.service';

type EntityFilter = '' | 'User' | 'Course';

/** Human-readable label + accent for each known audit action. */
const ACTION_META: Record<string, { label: string; tone: 'rose' | 'emerald' | 'amber' | 'primary' }> = {
  'user.suspended':     { label: 'Suspended user',    tone: 'rose' },
  'user.reactivated':   { label: 'Reactivated user',  tone: 'emerald' },
  'user.role_changed':  { label: 'Changed role',      tone: 'primary' },
  'course.published':   { label: 'Published course',  tone: 'emerald' },
  'course.unpublished': { label: 'Unpublished course', tone: 'amber' },
  'course.deleted':     { label: 'Deleted course',    tone: 'rose' },
};

@Component({
  selector: 'app-admin-audit',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-audit.html',
})
export class AdminAuditPage {
  private readonly admin = inject(AdminService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly entries = signal<AdminAuditLog[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(30);
  readonly entityFilter = signal<EntityFilter>('');

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  readonly rangeLabel = computed(() => {
    const total = this.totalCount();
    if (total === 0) return 'No entries';
    const start = (this.page() - 1) * this.pageSize() + 1;
    const end = Math.min(total, this.page() * this.pageSize());
    return `${start}–${end} of ${total}`;
  });

  constructor() {
    this.fetch();
  }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.admin.getAuditLog(this.entityFilter() || undefined, this.page(), this.pageSize()).subscribe({
      next: (res) => {
        this.entries.set(res.items);
        this.totalCount.set(res.totalCount);
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

  setEntity(entity: EntityFilter): void {
    this.entityFilter.set(entity);
    this.page.set(1);
    this.fetch();
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.fetch();
    }
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((p) => p + 1);
      this.fetch();
    }
  }

  actionLabel(action: string): string {
    return ACTION_META[action]?.label ?? action;
  }

  actionTone(action: string): string {
    return ACTION_META[action]?.tone ?? 'primary';
  }

  /** Pretty-print the JSON `changes` payload into a one-line summary. */
  changesSummary(changes?: string | null): string {
    if (!changes) return '';
    try {
      const obj = JSON.parse(changes) as Record<string, unknown>;
      return Object.entries(obj)
        .map(([k, v]) => `${k}: ${v}`)
        .join(' · ');
    } catch {
      return changes;
    }
  }

  relativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const min = Math.round(diff / 60_000);
    if (min < 1) return 'just now';
    if (min < 60) return `${min} min ago`;
    const h = Math.round(min / 60);
    if (h < 24) return `${h} hr ago`;
    const d = Math.round(h / 24);
    if (d < 30) return `${d} day${d === 1 ? '' : 's'} ago`;
    return new Date(iso).toLocaleDateString();
  }
}
