import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { NotificationService } from './notification.service';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './notifications-page.html',
})
export class NotificationsPage {
  private readonly api = inject(NotificationService);

  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly items = this.api.items;
  readonly unreadCount = this.api.unreadCount;

  constructor() { this.fetch(); }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.fetch(100).subscribe({
      next: () => this.loading.set(false),
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? 'Could not load notifications.');
      },
    });
  }

  markAllRead(): void {
    if (this.unreadCount() === 0) return;
    this.busy.set(true);
    this.api.markAllRead().subscribe({
      next: () => this.busy.set(false),
      error: () => this.busy.set(false),
    });
  }

  toggleRead(id: string, isRead: boolean): void {
    if (isRead) return;
    this.api.markRead(id).subscribe({ error: () => {} });
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
}
