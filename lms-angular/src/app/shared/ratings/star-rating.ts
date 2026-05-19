import { CommonModule } from '@angular/common';
import { Component, computed, input, output, signal } from '@angular/core';

/**
 * Reusable 5-star rating widget. Read-only when `readonly` is true; otherwise
 * acts as an input — clicking commits the value, hovering previews it.
 *
 *   <app-star-rating [value]="3.7" [readonly]="true" size="sm" />
 *   <app-star-rating [value]="myRating()" (valueChange)="setRating($event)" />
 *
 * Supports half-stars in read-only mode (rendered with a clipped fill) so
 * aggregate averages don't lose precision.
 */
@Component({
  selector: 'app-star-rating',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      class="inline-flex items-center gap-1"
      [class.cursor-pointer]="!readonly()"
      role="img"
      [attr.aria-label]="ariaLabel()"
    >
      @for (i of stars; track i) {
        <button
          type="button"
          class="relative inline-flex"
          [class.pointer-events-none]="readonly()"
          [attr.aria-label]="'Rate ' + i + ' star' + (i === 1 ? '' : 's')"
          [tabindex]="readonly() ? -1 : 0"
          (mouseenter)="onHover(i)"
          (mouseleave)="onHover(0)"
          (click)="onPick(i)"
        >
          <!-- Empty star (background) -->
          <svg
            [class]="sizeClass()"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
            class="text-amber-300"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <path
              d="M12 17.27 18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"
            ></path>
          </svg>
          <!-- Filled overlay, clipped to fillFraction -->
          <span
            class="absolute inset-0 overflow-hidden"
            [style.width.%]="fillPercent(i) * 100"
          >
            <svg
              [class]="sizeClass()"
              viewBox="0 0 24 24"
              fill="currentColor"
              class="text-amber-400"
            >
              <path
                d="M12 17.27 18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"
              ></path>
            </svg>
          </span>
        </button>
      }
    </div>
  `,
})
export class StarRating {
  readonly value = input<number>(0);
  readonly readonly = input<boolean>(false);
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly valueChange = output<number>();

  protected readonly stars = [1, 2, 3, 4, 5] as const;
  private readonly hovered = signal(0);

  protected readonly sizeClass = computed(() => {
    switch (this.size()) {
      case 'sm':
        return 'h-4 w-4';
      case 'lg':
        return 'h-7 w-7';
      default:
        return 'h-5 w-5';
    }
  });

  protected readonly ariaLabel = computed(() =>
    this.readonly() ? `${this.value().toFixed(1)} out of 5 stars` : 'Star rating',
  );

  protected fillPercent(i: number): number {
    const v = this.hovered() > 0 && !this.readonly() ? this.hovered() : this.value();
    if (v >= i) return 1;
    if (v <= i - 1) return 0;
    return v - (i - 1);
  }

  protected onHover(i: number): void {
    if (!this.readonly()) this.hovered.set(i);
  }

  protected onPick(i: number): void {
    if (this.readonly()) return;
    this.valueChange.emit(i);
  }
}
