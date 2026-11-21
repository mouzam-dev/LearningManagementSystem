import { Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { BOOK_AR } from './arabic.models';
import { ArabicService } from './arabic.service';

/** One book: its units as cards with progress. Route: /arabic/book/:n */
@Component({
  selector: 'app-arabic-book',
  imports: [RouterLink],
  templateUrl: './arabic-book.html',
})
export class ArabicBookPage {
  protected readonly svc = inject(ArabicService);
  protected readonly bookAr = BOOK_AR;
  readonly n = input.required<string>();

  protected readonly books = toSignal(this.svc.books$);
  protected readonly book = computed(() => this.books()?.find((b) => b.number === Number(this.n())));

  constructor() {
    this.svc.refreshScores();
  }
}
