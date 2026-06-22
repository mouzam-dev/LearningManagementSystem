import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  OnDestroy,
  Output,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../auth.service';
import { landingPathForRole } from '../../core/guards/auth.guard';

/**
 * Renders the official "Sign in with Google" button (Google Identity Services)
 * and exchanges the returned ID token for an app session. Drops in on both the
 * login and register pages. Renders nothing until the server reports a Google
 * OAuth client id, so it stays invisible when Google sign-in isn't configured.
 */
@Component({
  selector: 'app-google-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="gbtn" [hidden]="!ready()">
      <div class="gbtn-divider"><span>or</span></div>
      <div #host class="gbtn-host"></div>
      @if (busy()) {
        <p class="gbtn-status">Signing you in…</p>
      }
    </div>
  `,
  styles: [
    `
      .gbtn {
        margin-top: 0.25rem;
      }
      .gbtn-divider {
        display: flex;
        align-items: center;
        text-align: center;
        color: var(--c-subtle);
        font-size: 0.8rem;
        margin: 0.4rem 0 0.9rem;
      }
      .gbtn-divider::before,
      .gbtn-divider::after {
        content: '';
        flex: 1;
        border-bottom: 1px solid var(--c-line);
      }
      .gbtn-divider span {
        padding: 0 0.75rem;
      }
      .gbtn-host {
        display: flex;
        justify-content: center;
        min-height: 40px;
        color-scheme: light;
      }
      .gbtn-status {
        margin-top: 0.5rem;
        font-size: 0.8rem;
        color: var(--c-muted);
        text-align: center;
      }
    `,
  ],
})
export class GoogleButton implements AfterViewInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  /** Emits an error message to show in the host page's banner (empty string clears it). */
  @Output() authError = new EventEmitter<string>();
  @ViewChild('host') host?: ElementRef<HTMLElement>;

  readonly ready = signal(false);
  readonly busy = signal(false);

  private clientId = '';
  private destroyed = false;
  private rendered = false;

  ngAfterViewInit(): void {
    this.auth.googleClientId().subscribe({
      next: (id) => {
        if (!id) return; // Google sign-in not configured → render nothing
        this.clientId = id;
        this.waitForGis();
      },
      error: () => {
        /* config endpoint unreachable → leave the button hidden */
      },
    });
  }

  ngOnDestroy(): void {
    this.destroyed = true;
  }

  /** Poll until the GIS script has loaded (it's async in index.html), then render. */
  private waitForGis(attempt = 0): void {
    if (this.destroyed) return;
    const gsi = (window as any).google?.accounts?.id;
    if (!gsi) {
      if (attempt > 40) return; // ~6s — give up quietly
      setTimeout(() => this.waitForGis(attempt + 1), 150);
      return;
    }
    this.ready.set(true);
    this.renderButton(gsi);
  }

  private renderButton(gsi: any): void {
    if (this.rendered || this.destroyed || !this.host) return;
    this.rendered = true;

    gsi.initialize({
      client_id: this.clientId,
      callback: (resp: { credential?: string }) => this.handleCredential(resp?.credential),
    });

    const host = this.host.nativeElement;
    const width = Math.min(400, Math.max(220, host.offsetWidth || 300));
    gsi.renderButton(host, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'rectangular',
      logo_alignment: 'left',
      width,
    });
  }

  private handleCredential(credential?: string): void {
    if (!credential) {
      this.authError.emit('Google sign-in was cancelled.');
      return;
    }
    this.busy.set(true);
    this.authError.emit('');
    this.auth.loginWithGoogle(credential).subscribe({
      next: (res) => {
        this.busy.set(false);
        if (!res.success) {
          this.authError.emit(res.message || 'Google sign-in failed.');
          return;
        }
        const returnUrl =
          this.route.snapshot.queryParamMap.get('returnUrl') ?? landingPathForRole(res.user?.role);
        this.router.navigateByUrl(returnUrl);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        const body = err.error as { message?: string } | null;
        this.authError.emit(body?.message || 'Google sign-in failed. Please try again.');
      },
    });
  }
}
