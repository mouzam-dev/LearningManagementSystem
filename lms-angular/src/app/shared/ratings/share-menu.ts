import { CommonModule } from '@angular/common';
import { Component, computed, input, signal } from '@angular/core';

/**
 * Floating social-share menu for the course-detail page. Builds share URLs
 * client-side (no backend needed) for Facebook, X/Twitter, LinkedIn, and
 * WhatsApp, plus a one-click "Copy link" using the Clipboard API. Falls
 * back to a hidden textarea when the Clipboard API is unavailable (e.g.,
 * non-secure context). Opens each target in a new tab.
 */
@Component({
  selector: 'app-share-menu',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="relative inline-block">
      <button
        type="button"
        (click)="toggle()"
        class="inline-flex items-center gap-1.5 rounded-xl border border-line bg-surface px-3 py-2 text-sm font-semibold text-ink shadow-sm transition hover:border-primary-300 hover:text-primary-700"
        [attr.aria-expanded]="open()"
        aria-haspopup="menu"
      >
        <svg
          class="h-4 w-4"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
        >
          <circle cx="18" cy="5" r="3"></circle>
          <circle cx="6" cy="12" r="3"></circle>
          <circle cx="18" cy="19" r="3"></circle>
          <path d="m8.59 13.51 6.83 3.98"></path>
          <path d="m15.41 6.51-6.82 3.98"></path>
        </svg>
        Share
      </button>

      @if (open()) {
        <!-- Backdrop catches outside clicks -->
        <button
          type="button"
          class="fixed inset-0 z-40 cursor-default"
          aria-label="Close share menu"
          (click)="close()"
        ></button>

        <div
          class="absolute right-0 z-50 mt-2 w-64 overflow-hidden rounded-2xl border border-line bg-surface shadow-xl"
          role="menu"
        >
          <a
            [href]="facebookUrl()"
            target="_blank"
            rel="noopener noreferrer"
            class="flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-ink hover:bg-primary-50"
            role="menuitem"
            (click)="close()"
          >
            <span
              class="flex h-8 w-8 items-center justify-center rounded-full bg-[#1877F2] text-white"
              aria-hidden="true"
            >
              <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                <path
                  d="M22 12a10 10 0 1 0-11.56 9.88v-7H7.9V12h2.54V9.8c0-2.5 1.5-3.9 3.8-3.9 1.1 0 2.25.2 2.25.2v2.47h-1.27c-1.25 0-1.64.78-1.64 1.57V12h2.79l-.45 2.88h-2.34v7A10 10 0 0 0 22 12Z"
                ></path>
              </svg>
            </span>
            Facebook
          </a>

          <a
            [href]="twitterUrl()"
            target="_blank"
            rel="noopener noreferrer"
            class="flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-ink hover:bg-primary-50"
            role="menuitem"
            (click)="close()"
          >
            <span
              class="flex h-8 w-8 items-center justify-center rounded-full bg-black text-white"
              aria-hidden="true"
            >
              <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                <path
                  d="M18.244 2H21l-6.52 7.453L22 22h-6.812l-4.79-6.27L4.8 22H2l7.06-8.07L2 2h6.91l4.34 5.74L18.244 2Zm-1.193 18.04h1.502L7.04 3.88H5.43l11.62 16.16Z"
                ></path>
              </svg>
            </span>
            X / Twitter
          </a>

          <a
            [href]="linkedinUrl()"
            target="_blank"
            rel="noopener noreferrer"
            class="flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-ink hover:bg-primary-50"
            role="menuitem"
            (click)="close()"
          >
            <span
              class="flex h-8 w-8 items-center justify-center rounded-full bg-[#0A66C2] text-white"
              aria-hidden="true"
            >
              <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                <path
                  d="M20.45 20.45h-3.55v-5.57c0-1.33-.03-3.04-1.85-3.04-1.86 0-2.14 1.45-2.14 2.95v5.66H9.36V9h3.41v1.56h.05c.48-.9 1.64-1.85 3.37-1.85 3.6 0 4.27 2.37 4.27 5.46v6.28ZM5.34 7.43a2.06 2.06 0 1 1 0-4.12 2.06 2.06 0 0 1 0 4.12Zm1.78 13.02H3.56V9h3.56v11.45ZM22.22 0H1.77C.79 0 0 .77 0 1.72v20.56C0 23.23.79 24 1.77 24h20.45c.98 0 1.78-.77 1.78-1.72V1.72C24 .77 23.2 0 22.22 0Z"
                ></path>
              </svg>
            </span>
            LinkedIn
          </a>

          <a
            [href]="whatsappUrl()"
            target="_blank"
            rel="noopener noreferrer"
            class="flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-ink hover:bg-primary-50"
            role="menuitem"
            (click)="close()"
          >
            <span
              class="flex h-8 w-8 items-center justify-center rounded-full bg-[#25D366] text-white"
              aria-hidden="true"
            >
              <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                <path
                  d="M.04 24l1.69-6.19A11.85 11.85 0 0 1 .15 11.9C.15 5.33 5.49 0 12.06 0c3.19 0 6.18 1.24 8.43 3.5a11.83 11.83 0 0 1 3.49 8.42c0 6.57-5.34 11.91-11.92 11.91-1.99 0-3.94-.5-5.68-1.45L.04 24Zm6.6-3.81 .35 .21a9.86 9.86 0 0 0 5.06 1.39c5.46 0 9.91-4.45 9.91-9.92a9.83 9.83 0 0 0-2.9-7.02 9.86 9.86 0 0 0-7.01-2.91c-5.47 0-9.91 4.45-9.91 9.92 0 1.87.52 3.69 1.51 5.27l.24 .38-1 3.66 3.75-.98ZM17.47 14.38c-.07-.12-.27-.2-.57-.35-.3-.15-1.77-.87-2.04-.97-.28-.1-.48-.15-.67.15s-.78.97-.95 1.17c-.18.2-.35.22-.65.07-.3-.15-1.26-.46-2.4-1.48a9.06 9.06 0 0 1-1.66-2.06c-.18-.3-.02-.46.13-.61.13-.13.3-.35.45-.52.15-.18.2-.3.3-.5.1-.2.05-.37-.02-.52-.07-.15-.67-1.62-.93-2.22-.24-.58-.49-.5-.67-.51-.17-.01-.37-.01-.57-.01s-.52.07-.8.37c-.27.3-1.04 1.02-1.04 2.49 0 1.47 1.07 2.89 1.22 3.09.15.2 2.1 3.2 5.08 4.49a17 17 0 0 0 1.7.63c.71.23 1.36.2 1.87.12.57-.09 1.77-.72 2.02-1.42.25-.7.25-1.3.18-1.42Z"
                ></path>
              </svg>
            </span>
            WhatsApp
          </a>

          <button
            type="button"
            (click)="copyLink()"
            class="flex w-full items-center gap-3 border-t border-line px-4 py-2.5 text-left text-sm font-medium text-ink hover:bg-primary-50"
            role="menuitem"
          >
            <span
              class="flex h-8 w-8 items-center justify-center rounded-full bg-surface-2 text-ink"
              aria-hidden="true"
            >
              @if (copied()) {
                <svg
                  class="h-4 w-4 text-emerald-600"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="3"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                >
                  <path d="m5 12 5 5L20 7"></path>
                </svg>
              } @else {
                <svg
                  class="h-4 w-4"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                >
                  <path
                    d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"
                  ></path>
                  <path
                    d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"
                  ></path>
                </svg>
              }
            </span>
            {{ copied() ? 'Link copied!' : 'Copy link' }}
          </button>
        </div>
      }
    </div>
  `,
})
export class ShareMenu {
  readonly title = input.required<string>();
  /** Optional override; defaults to the current window.location.href. */
  readonly url = input<string | null>(null);

  protected readonly open = signal(false);
  protected readonly copied = signal(false);

  private readonly shareUrl = computed(
    () => this.url() ?? (typeof window !== 'undefined' ? window.location.href : ''),
  );

  protected readonly facebookUrl = computed(
    () =>
      `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(this.shareUrl())}`,
  );

  protected readonly twitterUrl = computed(
    () =>
      `https://twitter.com/intent/tweet?url=${encodeURIComponent(this.shareUrl())}&text=${encodeURIComponent(this.title())}`,
  );

  protected readonly linkedinUrl = computed(
    () =>
      `https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(this.shareUrl())}`,
  );

  protected readonly whatsappUrl = computed(
    () =>
      `https://wa.me/?text=${encodeURIComponent(`${this.title()} ${this.shareUrl()}`)}`,
  );

  protected toggle(): void {
    this.open.update((v) => !v);
    this.copied.set(false);
  }

  protected close(): void {
    this.open.set(false);
  }

  protected async copyLink(): Promise<void> {
    const link = this.shareUrl();
    try {
      if (navigator?.clipboard?.writeText) {
        await navigator.clipboard.writeText(link);
      } else {
        // Fallback for non-secure contexts where Clipboard API is unavailable.
        const ta = document.createElement('textarea');
        ta.value = link;
        ta.style.position = 'fixed';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.select();
        document.execCommand('copy');
        ta.remove();
      }
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 1500);
    } catch {
      this.copied.set(false);
    }
  }
}
