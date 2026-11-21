import { Component, computed, inject, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, combineLatest, map, of, startWith, switchMap } from 'rxjs';

import { ArabicWordCard, BOOK_AR, shuffle } from './arabic.models';
import { ArabicService } from './arabic.service';
import { ArabicMeaning, ArabicPrefs } from './arabic-ui';

const PAGE = 120; // tiles rendered per page so phones stay fast

/** Vocabulary practice cards. Route: /arabic/words?book=2&unit=b2u05 */
@Component({
  selector: 'app-arabic-words',
  imports: [RouterLink, ArabicMeaning, ArabicPrefs],
  templateUrl: './arabic-words.html',
})
export class ArabicWordsPage {
  protected readonly svc = inject(ArabicService);
  protected readonly bookAr = BOOK_AR;
  readonly book = input<string>();
  readonly unit = input<string>();

  protected readonly bookNumber = computed(() => Number(this.book()) || 1);
  protected readonly books = toSignal(this.svc.books$);
  protected readonly units = computed(() => this.books()?.find((b) => b.number === this.bookNumber())?.units ?? []);
  protected readonly unitInfo = computed(() => this.units().find((u) => u.id === this.unit()));

  /** undefined while loading. */
  protected readonly words = toSignal(
    combineLatest([toObservable(this.bookNumber), toObservable(this.unit)]).pipe(
      switchMap(([b, u]) => this.svc.words(b, u).pipe(
        map((w): ArabicWordCard[] | undefined => w),
        catchError(() => of([] as ArabicWordCard[])),
        startWith(undefined),
      )),
    ),
  );

  protected readonly hide = signal(false);
  protected readonly shuffled = signal(false);
  protected readonly limit = signal(PAGE);
  protected readonly revealed = signal(new Set<number>());

  protected readonly list = computed(() => {
    const w = this.words() ?? [];
    return this.shuffled() ? shuffle(w) : w;
  });
  protected readonly visible = computed(() => this.list().slice(0, this.limit()));

  protected toggleHide() {
    this.hide.update((h) => !h);
    this.revealed.set(new Set());
  }

  protected reveal(i: number) {
    this.revealed.update((s) => new Set(s).add(i));
  }

  protected more() {
    this.limit.update((l) => l + PAGE);
  }
}
