import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnDestroy, computed, inject, signal } from '@angular/core';

import { environment } from '../../../environments/environment';

interface HarvestStatus {
  state: 'Idle' | 'Running' | 'Completed' | 'Failed';
  startedAt: string | null;
  finishedAt: string | null;
  currentCollection: string | null;
  collectionsDone: number;
  totalCollections: number;
  hadithsWritten: number;
  error: string | null;
}

@Component({
  selector: 'app-admin-hadith',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './admin-hadith.html',
})
export class AdminHadithPage implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/admin/hadith`;

  readonly status = signal<HarvestStatus | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly starting = signal(false);
  private timer: ReturnType<typeof setTimeout> | null = null;

  readonly running = computed(() => this.status()?.state === 'Running');
  readonly pct = computed(() => {
    const s = this.status();
    if (!s || s.totalCollections === 0) return 0;
    return Math.round((s.collectionsDone / s.totalCollections) * 100);
  });

  constructor() {
    this.poll();
  }

  ngOnDestroy(): void {
    if (this.timer) clearTimeout(this.timer);
  }

  private poll(): void {
    this.http.get<HarvestStatus>(`${this.base}/status`).subscribe({
      next: (s) => {
        this.status.set(s);
        this.loading.set(false);
        if (s.state === 'Running') this.timer = setTimeout(() => this.poll(), 3000);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load harvest status.');
      },
    });
  }

  refresh(): void {
    if (this.running()) return;
    this.starting.set(true);
    this.error.set(null);
    this.http.post<HarvestStatus>(`${this.base}/refresh`, {}).subscribe({
      next: () => {
        this.starting.set(false);
        this.poll();
      },
      error: (e) => {
        this.starting.set(false);
        this.error.set(e?.error?.message ?? 'Could not start the refresh.');
        this.poll();
      },
    });
  }

  badge(state?: string): string {
    switch (state) {
      case 'Running': return 'bg-sky-100 text-sky-700';
      case 'Completed': return 'bg-emerald-100 text-emerald-700';
      case 'Failed': return 'bg-rose-100 text-rose-700';
      default: return 'bg-surface-2 text-muted';
    }
  }
}
