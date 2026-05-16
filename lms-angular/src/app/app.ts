import { Component, OnDestroy, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from './auth/auth.service';
import { ThemeService } from './core/services/theme.service';
import { NotificationService } from './notifications/notification.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly themeService = inject(ThemeService);
  private readonly notifications = inject(NotificationService);

  readonly isAuthed = this.auth.isAuthenticated;
  readonly user = this.auth.user;
  readonly theme = this.themeService.theme;
  readonly unreadCount = this.notifications.unreadCount;

  /** Mobile nav open/closed. */
  readonly menuOpen = signal(false);

  private notificationsActive = false;

  constructor() {
    // Reference-count the notification polling against auth state so we don't
    // hit the API once the user signs out.
    effect(() => {
      const signedIn = this.isAuthed();
      if (signedIn && !this.notificationsActive) {
        this.notifications.startPolling();
        this.notificationsActive = true;
      } else if (!signedIn && this.notificationsActive) {
        this.notifications.stopPolling();
        this.notifications.reset();
        this.notificationsActive = false;
      }
    });
  }

  ngOnDestroy(): void {
    if (this.notificationsActive) {
      this.notifications.stopPolling();
      this.notificationsActive = false;
    }
  }

  toggleTheme(): void {
    this.themeService.toggle();
  }

  toggleMenu(): void {
    this.menuOpen.update((v) => !v);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.menuOpen.set(false);
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
