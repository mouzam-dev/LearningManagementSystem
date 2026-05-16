import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthService } from '../auth.service';

type Status = 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './verify-email.html',
})
export class VerifyEmailPage {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly status = signal<Status>('verifying');
  readonly message = signal<string>('Verifying…');

  constructor() {
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!token) {
      this.status.set('error');
      this.message.set('This verification link is missing its token.');
      return;
    }
    this.auth.verifyEmail(token).subscribe({
      next: (res) => {
        this.status.set('success');
        this.message.set(res.message ?? 'Email verified.');
        this.auth.markVerified();
      },
      error: (err: HttpErrorResponse) => {
        this.status.set('error');
        this.message.set(err.error?.message ?? 'That link is invalid or has expired.');
      },
    });
  }
}
