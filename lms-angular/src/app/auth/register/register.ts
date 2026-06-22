import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../auth.service';
import { ValidationProblem } from '../models/auth.models';
import { landingPathForRole } from '../../core/guards/auth.guard';
import { GoogleButton } from '../google-button/google-button';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, GoogleButton],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    // Backend rules: min 8, has upper, lower, digit. Mirror here for fast feedback.
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/),
      ],
    ],
    role: ['Student' as 'Student' | 'Teacher', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: (res) => {
        this.submitting.set(false);
        if (!res.success) {
          this.errorMessage.set(res.message || 'Registration failed.');
          return;
        }
        this.router.navigateByUrl(landingPathForRole(res.user?.role));
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.errorMessage.set(this.formatError(err));
      },
    });
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Cannot reach the API. Is it running on http://localhost:5116?';
    }
    const body = err.error as ValidationProblem | { message?: string } | null;
    if (body && 'errors' in body && body.errors) {
      return Object.values(body.errors).flat().join(' ');
    }
    if (body && 'message' in body && body.message) {
      return body.message;
    }
    return err.statusText || 'Registration failed.';
  }
}
