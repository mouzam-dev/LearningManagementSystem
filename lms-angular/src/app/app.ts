import { Component, HostListener, OnDestroy, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';

import { AuthService } from './auth/auth.service';
import { ThemeService } from './core/services/theme.service';
import { NotificationService } from './notifications/notification.service';

interface NavLink {
  label: string;
  link: string;
}

/** A top-level nav entry — either a direct link (has `link`) or a dropdown group (has `children`). */
interface NavEntry {
  label: string;
  key: string;
  link?: string;
  children?: NavLink[];
}

const STUDENT_NAV: NavEntry[] = [
  { label: 'Dashboard', key: 's-dash', link: '/student/dashboard' },
  { label: 'Browse courses', key: 's-catalog', link: '/student/catalog' },
  {
    label: 'Classes', key: 's-classes', children: [
      { label: 'Live classes', link: '/student/live-classes' },
      { label: 'Attendance', link: '/student/attendance' },
    ],
  },
  { label: 'Certificates', key: 's-cert', link: '/student/certificates' },
  {
    label: 'Deen', key: 's-deen', children: [
      { label: "Qur'an", link: '/student/quran' },
      { label: 'Hadith', link: '/student/hadith' },
    ],
  },
];

const TEACHER_NAV: NavEntry[] = [
  { label: 'Dashboard', key: 't-dash', link: '/teacher/dashboard' },
  { label: 'My courses', key: 't-courses', link: '/teacher/courses' },
  {
    label: 'Teaching', key: 't-teach', children: [
      { label: 'Grading', link: '/teacher/grading' },
      { label: 'Question bank', link: '/teacher/question-bank' },
      { label: 'Rubrics', link: '/teacher/rubrics' },
    ],
  },
  {
    label: 'Sessions', key: 't-sessions', children: [
      { label: 'Live classes', link: '/teacher/live-classes' },
      { label: 'Attendance', link: '/teacher/attendance' },
    ],
  },
  {
    label: 'Deen', key: 't-deen', children: [
      { label: "Qur'an", link: '/teacher/quran' },
      { label: 'Hadith', link: '/teacher/hadith' },
    ],
  },
];

const ADMIN_NAV: NavEntry[] = [
  { label: 'Dashboard', key: 'a-dash', link: '/admin/dashboard' },
  { label: 'Organizations', key: 'a-orgs', link: '/admin/organizations' },
  {
    label: 'People', key: 'a-people', children: [
      { label: 'Users', link: '/admin/users' },
      { label: 'Roles & Permissions', link: '/admin/role-permissions' },
    ],
  },
  { label: 'Courses', key: 'a-courses', link: '/admin/courses' },
  {
    label: 'Insights', key: 'a-insights', children: [
      { label: 'Reports', link: '/admin/reports' },
      { label: 'Audit log', link: '/admin/audit' },
      { label: 'Hadith data', link: '/admin/hadith' },
    ],
  },
];

const ORGADMIN_NAV: NavEntry[] = [
  { label: 'Dashboard', key: 'o-dash', link: '/orgadmin/dashboard' },
  { label: 'Branches', key: 'o-branches', link: '/orgadmin/branches' },
  { label: 'Teachers', key: 'o-teachers', link: '/orgadmin/teachers' },
  { label: 'Courses', key: 'o-courses', link: '/orgadmin/courses' },
  { label: 'Attendance', key: 'o-att', link: '/orgadmin/attendance' },
];

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
  /** Which desktop dropdown is open (by key), or null when none. */
  readonly openDropdown = signal<string | null>(null);
  private readonly currentUrl = signal<string>(this.router.url);

  /** Role-specific top nav (grouped). */
  readonly navEntries = computed<NavEntry[]>(() => {
    switch (this.user()?.role) {
      case 'Student': return STUDENT_NAV;
      case 'Teacher': return TEACHER_NAV;
      case 'SuperAdmin': return ADMIN_NAV;
      case 'OrgAdmin': return ORGADMIN_NAV;
      default: return [];
    }
  });

  /** The public landing page renders full-width so its hero / section
   *  backgrounds bleed edge-to-edge. Every other route stays inside the
   *  centered max-w-7xl column (each landing section centers its own content). */
  readonly fullWidthMain = computed(() => {
    const u = this.currentUrl();
    return u === '/' || u.startsWith('/home');
  });

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

    // Track the active URL (to highlight the parent of an active dropdown item)
    // and close any open dropdown when navigation happens.
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe((e) => {
      this.currentUrl.set((e as NavigationEnd).urlAfterRedirects);
      this.openDropdown.set(null);
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

  toggleDropdown(key: string, ev: Event): void {
    ev.stopPropagation(); // don't let the document handler immediately close it
    this.openDropdown.update((cur) => (cur === key ? null : key));
  }

  closeDropdowns(): void {
    this.openDropdown.set(null);
  }

  /** True when the current route belongs to a dropdown group (so it highlights). */
  isGroupActive(entry: NavEntry): boolean {
    const url = this.currentUrl();
    return !!entry.children?.some((c) => url.startsWith(c.link));
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.closeDropdowns();
  }

  logout(): void {
    this.menuOpen.set(false);
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
