import { Component, computed, effect, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';

import { ArabicService } from './arabic.service';
import { ArabicMeaning, ArabicPrefs, ArabicStars } from './arabic-ui';

/** A lesson: intro, dialogue, new words and a quiz link. Route: /arabic/lesson/:unitId/:n */
@Component({
  selector: 'app-arabic-lesson',
  imports: [RouterLink, ArabicStars, ArabicPrefs, ArabicMeaning],
  templateUrl: './arabic-lesson.html',
})
export class ArabicLessonPage {
  protected readonly svc = inject(ArabicService);
  readonly unitId = input.required<string>();
  readonly n = input.required<string>();

  protected readonly unit = toSignal(
    toObservable(this.unitId).pipe(switchMap((id) => this.svc.unit(id).pipe(catchError(() => of(null))))),
  );
  protected readonly lesson = computed(() => this.unit()?.lessons.find((l) => l.number === Number(this.n())));
  protected readonly prev = computed(() => this.unit()?.lessons.find((l) => l.number === Number(this.n()) - 1));
  protected readonly nextLesson = computed(() => this.unit()?.lessons.find((l) => l.number === Number(this.n()) + 1));
  /** Speakers in order of first appearance; bubbles alternate sides by speaker. */
  protected readonly speakers = computed(() => [...new Set(this.lesson()?.dialogue.map((d) => d.speaker) ?? [])]);

  protected readonly sections = computed(() => {
    const l = this.lesson();
    return [
      { id: 'learn', label: 'Learn', show: true },
      { id: 'dialogue', label: 'Dialogue', show: !!l?.dialogue.length },
      { id: 'words', label: 'Words', show: !!l?.vocab.length },
      { id: 'quiz', label: 'Quiz', show: true },
    ].filter((s) => s.show);
  });

  constructor() {
    this.svc.refreshScores();
    // Remember the lesson for the "Continue learning" card.
    effect(() => {
      if (this.lesson()) this.svc.lastLesson = { unitId: this.unitId(), n: Number(this.n()) };
    });
  }

  protected jump(id: string) {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  protected side(speaker: string) {
    return this.speakers().indexOf(speaker) % 2;
  }
}
