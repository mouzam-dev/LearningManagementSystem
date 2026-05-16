import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { NotificationFeed, NotificationItem } from './notification.models';

/**
 * Notification feed for the signed-in user. Holds the unread count + most
 * recent items as signals so the header bell and the /notifications page can
 * subscribe to the same state without re-fetching. Polls every 30s while the
 * user is on a page that displays the bell.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/notifications`;

  private readonly _items = signal<NotificationItem[]>([]);
  private readonly _unread = signal<number>(0);
  private pollHandle: ReturnType<typeof setInterval> | null = null;
  private subscribers = 0;

  readonly items = this._items.asReadonly();
  readonly unreadCount = this._unread.asReadonly();
  readonly hasUnread = computed(() => this._unread() > 0);

  /** Fetch the latest feed and update local state. */
  fetch(limit = 30): Observable<NotificationFeed> {
    const params = new HttpParams().set('limit', limit);
    return this.http.get<NotificationFeed>(this.baseUrl, { params }).pipe(
      tap((feed) => {
        this._items.set(feed.items);
        this._unread.set(feed.unreadCount);
      }),
    );
  }

  markRead(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/read`, {}).pipe(
      tap(() => {
        const next = this._items().map((n) => (n.id === id ? { ...n, isRead: true } : n));
        this._items.set(next);
        this._unread.set(Math.max(0, this._unread() - 1));
      }),
    );
  }

  markAllRead(): Observable<{ marked: number }> {
    return this.http.patch<{ marked: number }>(`${this.baseUrl}/read-all`, {}).pipe(
      tap(() => {
        this._items.set(this._items().map((n) => ({ ...n, isRead: true })));
        this._unread.set(0);
      }),
    );
  }

  /**
   * Reference-counted polling. Each consumer (e.g. the header bell) calls
   * startPolling() on init and stopPolling() on destroy; the interval only
   * runs while at least one consumer is active.
   */
  startPolling(intervalMs = 30_000): void {
    this.subscribers += 1;
    if (this.pollHandle === null) {
      this.fetch().subscribe({ error: () => {} });
      this.pollHandle = setInterval(() => this.fetch().subscribe({ error: () => {} }), intervalMs);
    }
  }

  stopPolling(): void {
    this.subscribers = Math.max(0, this.subscribers - 1);
    if (this.subscribers === 0 && this.pollHandle !== null) {
      clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
  }

  /** Clear in-memory state — called on logout. */
  reset(): void {
    this._items.set([]);
    this._unread.set(0);
    if (this.pollHandle !== null) {
      clearInterval(this.pollHandle);
      this.pollHandle = null;
    }
    this.subscribers = 0;
  }
}
