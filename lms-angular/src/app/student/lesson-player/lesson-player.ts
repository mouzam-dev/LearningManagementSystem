import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  AfterViewInit,
  Component,
  DestroyRef,
  ElementRef,
  ViewChild,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, fromEvent, throttleTime } from 'rxjs';

import { LessonDetail } from '../models/lesson.models';
import { StudentService } from '../student.service';

/**
 * How a lesson's video URL should be rendered:
 *  - 'file'  → a direct media file (.mp4/.webm/…) playable in a <video> tag
 *  - 'embed' → a hosted player (YouTube / Vimeo) that needs an <iframe>
 */
type VideoSource =
  | { kind: 'file'; src: string }
  | { kind: 'embed'; src: SafeResourceUrl };

@Component({
  selector: 'app-student-lesson-player',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './lesson-player.html',
})
export class StudentLessonPlayer implements AfterViewInit {
  private readonly studentService = inject(StudentService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sanitizer = inject(DomSanitizer);

  /** Bound from the route via withComponentInputBinding(). */
  readonly lessonId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly lesson = signal<LessonDetail | null>(null);
  readonly markingComplete = signal(false);
  readonly savingProgress = signal(false);

  readonly progressPercentage = computed(() => {
    const l = this.lesson();
    if (!l) return 0;
    const total = l.modules.reduce((s, m) => s + m.lessons.length, 0);
    if (total === 0) return 0;
    const completed = l.modules.reduce(
      (s, m) => s + m.lessons.filter((x) => x.isCompleted).length,
      0,
    );
    return Math.round((completed / total) * 100);
  });

  /**
   * Resolves the lesson's video URL into a renderable source. YouTube / Vimeo
   * links can't play in a raw <video> element — they're rewritten to embed
   * URLs and rendered through an <iframe>. Direct media files stay on <video>
   * (which keeps the watch-time tracking + auto-complete-on-ended behaviour).
   */
  readonly videoSource = computed<VideoSource | null>(() => {
    const url = this.lesson()?.videoUrl?.trim();
    if (!url) return null;

    const embed = StudentLessonPlayer.toEmbedUrl(url);
    if (embed) {
      return { kind: 'embed', src: this.sanitizer.bypassSecurityTrustResourceUrl(embed) };
    }
    return { kind: 'file', src: url };
  });

  /** True when the lesson's document looks like a PDF (browser-previewable). */
  readonly documentIsPdf = computed(() => {
    const url = this.lesson()?.documentUrl?.trim().toLowerCase();
    return !!url && url.split('?')[0].endsWith('.pdf');
  });

  /** Sanitized document URL for embedding a PDF in an <iframe>. */
  readonly documentSafeUrl = computed<SafeResourceUrl | null>(() => {
    const url = this.lesson()?.documentUrl?.trim();
    if (!url || !this.documentIsPdf()) return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  });

  /**
   * Maps a YouTube or Vimeo watch URL to its embeddable player URL.
   * Returns null for anything that looks like a plain media file.
   */
  private static toEmbedUrl(url: string): string | null {
    // YouTube: watch?v=, youtu.be/, /embed/, /shorts/  — capture the 11-char id.
    const yt = url.match(
      /(?:youtube\.com\/(?:watch\?(?:[^&]*&)*v=|embed\/|shorts\/|live\/)|youtu\.be\/)([A-Za-z0-9_-]{11})/i,
    );
    if (yt) return `https://www.youtube.com/embed/${yt[1]}`;

    // Vimeo: vimeo.com/123456789 or player.vimeo.com/video/123456789
    const vimeo = url.match(/vimeo\.com\/(?:video\/)?(\d+)/i);
    if (vimeo) return `https://player.vimeo.com/video/${vimeo[1]}`;

    return null;
  }

  @ViewChild('videoEl') videoEl?: ElementRef<HTMLVideoElement>;

  /** Stream of HTMLVideoElement.currentTime values (seconds), throttled. */
  private readonly watchPing$ = new Subject<number>();

  constructor() {
    // Refetch when the route param changes (the player is re-used across lessons).
    effect(() => {
      const id = this.lessonId();
      if (id) this.fetch(id);
    });

    // Persist watch progress every ~10s.
    this.watchPing$
      .pipe(throttleTime(10000), debounceTime(500), takeUntilDestroyed())
      .subscribe((seconds) => this.persistProgress(seconds, false));
  }

  ngAfterViewInit(): void {
    // Hook into the video element for periodic time-update + ended events
    // each time the underlying lesson changes (lesson() signal effect-driven).
    effect(() => {
      const l = this.lesson();
      const v = this.videoEl?.nativeElement;
      if (!l || !v) return;

      // Restore prior position so the user resumes where they left off.
      if (l.watchTimeSeconds > 0 && v.duration && l.watchTimeSeconds < v.duration - 1) {
        v.currentTime = l.watchTimeSeconds;
      }

      const sub = fromEvent(v, 'timeupdate')
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.watchPing$.next(Math.floor(v.currentTime)));

      const endSub = fromEvent(v, 'ended')
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.markComplete());

      this.destroyRef.onDestroy(() => {
        sub.unsubscribe();
        endSub.unsubscribe();
      });
    });
  }

  fetch(lessonId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.studentService.getLesson(lessonId).subscribe({
      next: (l) => {
        this.lesson.set(l);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.lesson.set(null);
        this.loading.set(false);
        this.error.set(this.formatError(err));
      },
    });
  }

  markComplete(): void {
    const l = this.lesson();
    if (!l || l.isCompleted || this.markingComplete()) return;
    this.markingComplete.set(true);

    const watch = Math.max(
      l.watchTimeSeconds,
      Math.floor(this.videoEl?.nativeElement?.currentTime ?? 0),
    );

    this.studentService
      .updateLessonProgress(l.lessonId, { watchTimeSeconds: watch, markComplete: true })
      .subscribe({
        next: (updated) => {
          this.lesson.set(updated);
          this.markingComplete.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.markingComplete.set(false);
          this.error.set(this.formatError(err));
        },
      });
  }

  goToLesson(id: string | null | undefined): void {
    if (!id) return;
    this.router.navigate(['/student/lessons', id]);
  }

  formatDuration(minutes?: number | null): string {
    if (!minutes || minutes <= 0) return '';
    if (minutes < 60) return `${minutes} min`;
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return m === 0 ? `${h}h` : `${h}h ${m}m`;
  }

  private persistProgress(seconds: number, markComplete: boolean): void {
    const l = this.lesson();
    if (!l) return;
    if (this.savingProgress()) return;
    this.savingProgress.set(true);

    this.studentService
      .updateLessonProgress(l.lessonId, { watchTimeSeconds: seconds, markComplete })
      .subscribe({
        next: (updated) => {
          // Don't overwrite the playing video element via lesson(); just record
          // the latest progress numbers from the server.
          this.lesson.update((curr) =>
            curr ? { ...curr, watchTimeSeconds: updated.watchTimeSeconds, isCompleted: updated.isCompleted, completedAt: updated.completedAt, modules: updated.modules } : curr,
          );
          this.savingProgress.set(false);
        },
        error: () => {
          // Soft-fail — periodic pings shouldn't surface a banner.
          this.savingProgress.set(false);
        },
      });
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) return 'Cannot reach the API. Is it running on http://localhost:5116?';
    if (err.status === 403) return "You're not enrolled in the parent course.";
    if (err.status === 404) return 'This lesson no longer exists.';
    const body = err.error as { message?: string } | null;
    return body?.message ?? err.statusText ?? 'Failed to load the lesson.';
  }
}
