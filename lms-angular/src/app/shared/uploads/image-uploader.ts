import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, model, output, signal } from '@angular/core';

import { ImageUploadKind, UploadsService } from './uploads.service';

/**
 * Drop-in image picker that uploads a local file via UploadsService and emits
 * the resulting absolute URL. Two-way bound via `value()` so it also accepts
 * an existing URL on edit forms — shows a preview, lets the user replace or
 * clear, and re-emits the new value.
 *
 * Usage:
 *   <app-image-uploader [(value)]="logoUrl" kind="logo" label="Organization logo" />
 */
@Component({
  selector: 'app-image-uploader',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-2">
      <div class="flex items-center gap-3">
        <!-- Preview / placeholder -->
        @if (value()) {
          <img [src]="value()" [alt]="label() || 'Uploaded image'"
               class="h-20 w-20 shrink-0 rounded-md object-cover ring-1 ring-line" />
        } @else {
          <div class="flex h-20 w-20 shrink-0 items-center justify-center rounded-md border border-dashed border-line-strong bg-surface-2 text-subtle"
               aria-hidden="true">
            <svg class="h-7 w-7" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round">
              <rect x="3" y="3" width="18" height="18" rx="2"></rect>
              <circle cx="9" cy="9" r="2"></circle>
              <path d="m21 15-5-5L5 21"></path>
            </svg>
          </div>
        }

        <!-- Actions -->
        <div class="flex min-w-0 flex-col gap-1.5">
          <input #picker type="file" accept="image/*" class="hidden"
                 (change)="onFilePicked($any($event.target).files?.[0])" />
          <div class="flex flex-wrap items-center gap-2">
            <button type="button" (click)="picker.click()" [disabled]="uploading()"
                    class="inline-flex items-center gap-1.5 rounded-md border border-line bg-surface px-3 py-1.5 text-xs font-semibold text-ink-soft transition hover:border-primary-300 hover:text-primary-600 disabled:cursor-not-allowed disabled:opacity-50">
              @if (uploading()) {
                <svg class="h-3.5 w-3.5 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25"></circle>
                  <path d="M22 12a10 10 0 0 0-10-10" stroke="currentColor" stroke-width="3" stroke-linecap="round"></path>
                </svg>
                Uploading…
              } @else if (value()) {
                Replace
              } @else {
                Choose image
              }
            </button>
            @if (value() && !uploading()) {
              <button type="button" (click)="clear()"
                      class="text-xs font-semibold text-muted transition hover:text-rose-600">
                Remove
              </button>
            }
          </div>
          <p class="text-[11px] text-subtle">{{ hint() }}</p>
        </div>
      </div>

      @if (errorMessage(); as msg) {
        <p class="text-xs text-rose-600">{{ msg }}</p>
      }
    </div>
  `,
})
export class ImageUploaderComponent {
  private readonly uploads = inject(UploadsService);

  // Two-way bound URL. Parent can pass an existing one in (edit forms) and
  // gets a new one out when the user uploads / clears.
  readonly value = model<string | null>(null);
  readonly kind = input<ImageUploadKind>('misc');
  readonly label = input<string>('');
  readonly maxBytes = input<number>(5 * 1024 * 1024); // 5 MB, matches API
  readonly uploaded = output<string>();

  readonly uploading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly hint = computed(() =>
    `PNG, JPG, WebP, GIF or SVG · up to ${Math.round(this.maxBytes() / (1024 * 1024))} MB`);

  onFilePicked(file: File | undefined): void {
    if (!file) return;
    this.errorMessage.set(null);

    // Client-side guard mirrors the API limit — saves the round-trip.
    if (file.size > this.maxBytes()) {
      this.errorMessage.set(
        `Image is too large — the limit is ${Math.round(this.maxBytes() / (1024 * 1024))} MB.`,
      );
      return;
    }

    this.uploading.set(true);
    this.uploads.uploadImage(file, this.kind()).subscribe({
      next: (res) => {
        this.uploading.set(false);
        this.value.set(res.url);
        this.uploaded.emit(res.url);
      },
      error: (err: HttpErrorResponse) => {
        this.uploading.set(false);
        const errors = err.error?.errors as Record<string, string[]> | undefined;
        const first = errors ? Object.values(errors).flat()[0] : undefined;
        this.errorMessage.set(
          first ?? err.error?.message ?? err.statusText ?? 'Upload failed.',
        );
      },
    });
  }

  clear(): void {
    this.errorMessage.set(null);
    this.value.set(null);
    this.uploaded.emit('');
  }
}
