import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { BOOK_AR, starCount } from './arabic.models';
import { ArabicService } from './arabic.service';
import { ArabicPrefs } from './arabic-ui';

/** Landing page: greeting, progress stats, continue card, and the four books. */
@Component({
  selector: 'app-arabic-home',
  imports: [RouterLink, ArabicPrefs],
  templateUrl: './arabic-home.html',
})
export class ArabicHomePage {
  protected readonly svc = inject(ArabicService);
  protected readonly bookAr = BOOK_AR;
  protected readonly books = toSignal(this.svc.books$);
  protected readonly last = this.svc.lastLesson;
  // Unit ids look like "b2u05".
  protected readonly lastLabel = this.last
    ? `Book ${this.last.unitId[1]} · Unit ${Number(this.last.unitId.slice(3))} · Lesson ${this.last.n}`
    : 'Book 1 · Unit 1 · Lesson 1';

  protected readonly stats = computed(() => {
    const s = Object.entries(this.svc.scores());
    return {
      stars: s.reduce((a, [, p]) => a + starCount(p), 0),
      lessons: s.filter(([k]) => !k.endsWith('-0')).length,
    };
  });

  constructor() {
    this.svc.refreshScores();
  }
}
