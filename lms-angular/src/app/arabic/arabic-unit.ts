import { Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';

import { ArabicUnit, UNIT_TEST } from './arabic.models';
import { ArabicService } from './arabic.service';
import { ArabicStars } from './arabic-ui';

/** Unit overview + learning path of its lessons. Route: /arabic/unit/:unitId */
@Component({
  selector: 'app-arabic-unit',
  imports: [RouterLink, ArabicStars],
  templateUrl: './arabic-unit.html',
})
export class ArabicUnitPage {
  protected readonly svc = inject(ArabicService);
  protected readonly UNIT_TEST = UNIT_TEST;
  readonly unitId = input.required<string>();

  /** undefined = loading, null = not found. */
  protected readonly unit = toSignal(
    toObservable(this.unitId).pipe(switchMap((id) => this.svc.unit(id).pipe(catchError(() => of(null))))),
  );

  protected readonly progress = computed(() => {
    const u = this.unit();
    return u ? this.svc.unitProgress({ id: u.id, lessonCount: u.lessons.length }) : 0;
  });
  protected readonly done = computed(() => {
    const u = this.unit();
    return u ? u.lessons.filter((l) => this.svc.score(u.id, l.number) != null).length : 0;
  });
  /** First lesson without a quiz result, else the first lesson. */
  protected readonly next = computed(() => {
    const u = this.unit();
    return u ? (u.lessons.find((l) => this.svc.score(u.id, l.number) == null) ?? u.lessons[0]) : undefined;
  });

  constructor() {
    this.svc.refreshScores();
  }

  protected score(u: ArabicUnit, n: number) {
    return this.svc.score(u.id, n);
  }
}
