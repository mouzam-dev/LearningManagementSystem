import { Component, DestroyRef, HostListener, computed, effect, inject, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';

import { ArabicQuiz, UNIT_TEST, shuffle } from './arabic.models';
import { ArabicService } from './arabic.service';
import { ArabicStars } from './arabic-ui';

/**
 * Lesson quiz (/arabic/quiz/:unitId/:n) or unit test (/arabic/quiz/:unitId/test:
 * 10 random questions from all lessons). Best score is saved on finish.
 */
@Component({
  selector: 'app-arabic-quiz',
  imports: [RouterLink, ArabicStars],
  templateUrl: './arabic-quiz.html',
})
export class ArabicQuizPage {
  protected readonly svc = inject(ArabicService);
  readonly unitId = input.required<string>();
  readonly n = input.required<string>();

  protected readonly unit = toSignal(
    toObservable(this.unitId).pipe(switchMap((id) => this.svc.unit(id).pipe(catchError(() => of(null))))),
  );
  protected readonly isTest = computed(() => this.n() === 'test');
  protected readonly lessonNumber = computed(() => (this.isTest() ? UNIT_TEST : Number(this.n())));

  /** Bumped by "Try again" to reshuffle. */
  private readonly round = signal(0);
  protected readonly questions = computed<ArabicQuiz[]>(() => {
    this.round();
    const u = this.unit();
    if (!u) return [];
    if (this.isTest()) return shuffle(u.lessons.flatMap((l) => l.quiz)).slice(0, 10);
    return shuffle(u.lessons.find((l) => l.number === this.lessonNumber())?.quiz ?? []);
  });

  protected readonly i = signal(0);
  protected readonly right = signal(0);
  protected readonly picked = signal<number | null>(null);

  protected readonly current = computed(() => this.questions()[this.i()]);
  /** Options in a random display order (indices into current().options). */
  protected readonly order = computed(() => {
    const q = this.current();
    return q ? shuffle(q.options.map((_, k) => k)) : [];
  });
  protected readonly finished = computed(() => this.questions().length > 0 && this.i() >= this.questions().length);
  protected readonly pct = computed(() => Math.round((this.right() / Math.max(1, this.questions().length)) * 100));
  protected readonly nextLesson = computed(() =>
    this.isTest() ? undefined : this.unit()?.lessons.find((l) => l.number === this.lessonNumber() + 1));
  protected readonly backLink = computed(() =>
    this.isTest() ? ['/arabic/unit', this.unitId()] : ['/arabic/lesson', this.unitId(), this.n()]);

  private timer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.svc.refreshScores();
    // New question set (route change or retry): start over.
    effect(() => {
      this.questions();
      clearTimeout(this.timer);
      this.i.set(0);
      this.right.set(0);
      this.picked.set(null);
    });
    inject(DestroyRef).onDestroy(() => clearTimeout(this.timer));
  }

  protected questionText(q: ArabicQuiz) {
    return this.svc.lang() === 'ur' ? q.questionUr : q.questionEn;
  }

  protected pick(k: number) {
    const q = this.current();
    if (!q || this.picked() !== null) return;
    this.picked.set(k);
    const ok = k === q.answerIndex;
    if (ok) this.right.update((r) => r + 1);
    this.timer = setTimeout(() => {
      this.picked.set(null);
      this.i.update((i) => i + 1);
      if (this.finished()) this.svc.saveScore(this.unitId(), this.lessonNumber(), this.pct());
    }, ok ? 1000 : 2000);
  }

  protected retry() {
    this.round.update((r) => r + 1);
  }

  /** Keys 1-4 pick the matching option. */
  @HostListener('document:keydown', ['$event'])
  protected onKey(e: KeyboardEvent) {
    const k = this.order()[Number(e.key) - 1];
    if (k !== undefined && !this.finished()) this.pick(k);
  }

  protected readonly hasArabic = (s: string) => /[؀-ۿ]/.test(s);
}
