import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';

import { jitsiRoomUrl } from './jitsi';
import { LiveSession } from './live-classes.models';
import { LiveClassesService } from './live-classes.service';

@Component({
  selector: 'app-student-live-classes',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-live-classes.html',
})
export class StudentLiveClassesPage {
  private readonly live = inject(LiveClassesService);

  readonly sessions = signal<LiveSession[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.live.getMySessions().subscribe({
      next: (s) => {
        this.sessions.set(s);
        this.loading.set(false);
      },
      error: (e: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(this.msg(e));
      },
    });
  }

  join(s: LiveSession): void {
    // Open a blank tab synchronously (within the click) so popup blockers allow it,
    // then point it at the room once the join — which marks attendance — succeeds.
    const win = window.open('', '_blank');
    this.busy.set(true);
    this.error.set(null);
    this.live.join(s.id).subscribe({
      next: (info) => {
        this.busy.set(false);
        const url = jitsiRoomUrl(info.roomName);
        if (win) win.location.href = url;
        else window.open(url, '_blank', 'noopener,noreferrer');
      },
      error: (e: HttpErrorResponse) => {
        this.busy.set(false);
        win?.close();
        this.error.set(this.msg(e));
      },
    });
  }

  private msg(e: HttpErrorResponse): string {
    if (e.status === 0) return 'Cannot reach the API.';
    if (e.status === 403) return 'You are not enrolled in this course.';
    if (e.status === 409) return e.error?.message ?? "This class isn't live right now.";
    return e.error?.message ?? e.statusText ?? 'Something went wrong.';
  }
}
