import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AuthService } from '../auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.html',
})
export class ForgotPasswordPage {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly submitting = signal(false);
  readonly sent = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.error.set(null);
    this.auth.forgotPassword(this.form.getRawValue().email.trim().toLowerCase()).subscribe({
      next: () => { this.submitting.set(false); this.sent.set(true); },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        // Backend deliberately returns 200 even for unknown emails, so any
        // error here is a real transport/network failure worth surfacing.
        this.error.set(err.status === 0
          ? 'Cannot reach the API. Try again in a moment.'
          : err.error?.message ?? 'Could not request a reset link.');
      },
    });
  }
}
