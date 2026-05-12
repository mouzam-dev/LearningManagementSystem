import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CertificateSummary } from '../models/certificate.models';
import { StudentService } from '../student.service';

/**
 * Earned-certificates grid. Acts as both a celebration and a hub — clicking
 * any card opens the printable view at /student/certificates/:id.
 */
@Component({
  selector: 'app-student-certificate-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './certificate-list.html',
})
export class StudentCertificateList {
  private readonly studentService = inject(StudentService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly certificates = signal<CertificateSummary[]>([]);

  constructor() {
    this.fetch();
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.studentService.getMyCertificates().subscribe({
      next: (list) => {
        this.certificates.set(list);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API. Is it running on http://localhost:5116?'
          : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  /**
   * Pick a deterministic gradient based on the verify code so each card has
   * its own visual identity — same code always picks the same palette.
   */
  paletteFor(code: string): { from: string; to: string } {
    const palettes = [
      { from: 'from-indigo-500', to: 'to-violet-600' },
      { from: 'from-sky-500', to: 'to-cyan-600' },
      { from: 'from-emerald-500', to: 'to-teal-600' },
      { from: 'from-amber-500', to: 'to-orange-600' },
      { from: 'from-rose-500', to: 'to-pink-600' },
      { from: 'from-fuchsia-500', to: 'to-purple-600' },
    ];
    let hash = 0;
    for (let i = 0; i < code.length; i++) hash = (hash * 31 + code.charCodeAt(i)) >>> 0;
    return palettes[hash % palettes.length];
  }
}
