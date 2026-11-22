import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { landingPathForRole } from '../core/guards/auth.guard';

/**
 * App-style bottom navigation for the Deen readers (Qur'an, Hadith) on phones and tablets.
 * Hidden from lg up, where the top navigation is enough. Pages using it add bottom padding
 * (pb-24 lg:pb-8) so the bar never covers their last lines.
 */
@Component({
  selector: 'app-deen-tab-bar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-surface/95 backdrop-blur lg:hidden" aria-label="Deen">
      <div class="mx-auto grid max-w-md grid-cols-4">
        @for (t of tabs(); track t.label) {
          <a [routerLink]="t.link" routerLinkActive="!text-primary-700 dark:!text-primary-300" #rla="routerLinkActive"
             [attr.aria-current]="rla.isActive ? 'page' : null"
             class="flex min-h-[58px] flex-col items-center justify-center gap-0.5 text-[11px] font-semibold text-muted">
            <span class="flex h-7 w-12 items-center justify-center rounded-full transition"
                  [class]="rla.isActive ? 'bg-primary-50 dark:bg-primary-900/40' : ''">
              <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                   stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                @switch (t.icon) {
                  @case ('home') { <path d="M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6h-6v6H4a1 1 0 0 1-1-1z" /> }
                  @case ('quran') { <path d="M2 4h6a4 4 0 0 1 4 4v13a3 3 0 0 0-3-3H2z" /><path d="M22 4h-6a4 4 0 0 0-4 4v13a3 3 0 0 1 3-3h7z" /> }
                  @case ('hadith') { <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20V3H6.5A2.5 2.5 0 0 0 4 5.5z" /><path d="M8 7h8M8 11h6" /> }
                  @case ('arabic') { <path d="M5 8h6M8 5v3M7 8c0 4 2 7 5 8" /><path d="M13 20l4-9 4 9M14.5 17h5" /> }
                }
              </svg>
            </span>
            {{ t.label }}
          </a>
        }
      </div>
    </nav>
  `,
})
export class DeenTabBar {
  private readonly auth = inject(AuthService);

  /** Students and teachers have their own copies of the readers; everyone else uses the public ones. */
  readonly tabs = computed(() => {
    const role = this.auth.user()?.role;
    const base = role === 'Student' ? '/student' : role === 'Teacher' ? '/teacher' : '';
    return [
      { label: 'Home', icon: 'home', link: this.auth.isAuthenticated() ? landingPathForRole(role) : '/home' },
      { label: "Qur'an", icon: 'quran', link: `${base}/quran` },
      { label: 'Hadith', icon: 'hadith', link: `${base}/hadith` },
      { label: 'Arabic', icon: 'arabic', link: '/arabic' },
    ];
  });
}
