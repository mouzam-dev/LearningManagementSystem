import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { VerifyCertificate } from '../student/models/certificate.models';
import { StudentService } from '../student/student.service';

/**
 * Public verification landing page. Reachable by anyone (no authGuard) at
 * /verify/:code. Hits the anonymous backend endpoint and renders either a
 * "verified" card with the public details, or a "not found" notice.
 */
@Component({
  selector: 'app-verify-certificate',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './verify-certificate.html',
})
export class VerifyCertificateComponent {
  private readonly studentService = inject(StudentService);

  readonly code = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly result = signal<VerifyCertificate | null>(null);

  constructor() {
    effect(() => {
      const code = this.code();
      if (code) this.lookup(code);
    });
  }

  private lookup(code: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.studentService.verifyCertificate(code).subscribe({
      next: (r) => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.result.set(null);
        this.loading.set(false);
        this.error.set(err.status === 0 ? 'Cannot reach the API.' : 'Verification failed.');
      },
    });
  }
}
