import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Certificate } from '../models/certificate.models';
import { StudentService } from '../student.service';

/**
 * Printable certificate view — the diploma. Two surfaces:
 *  1. Page chrome (header, action buttons) — visible on screen, hidden in print.
 *  2. The certificate itself — gradient border, ornamental flourishes, the
 *     student's name in display type, course title, signature line, verify
 *     code. Designed to print at A4-landscape.
 */
@Component({
  selector: 'app-student-certificate-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './certificate-detail.html',
  styles: [`
    @media print {
      :host {
        display: block;
      }
      .print-hidden { display: none !important; }
      .print-page {
        margin: 0 !important;
        padding: 0 !important;
        background: white !important;
      }
      .print-certificate {
        box-shadow: none !important;
        border-radius: 0 !important;
        page-break-inside: avoid;
        width: 100vw !important;
        max-width: none !important;
      }
    }
  `],
})
export class StudentCertificateDetail {
  private readonly studentService = inject(StudentService);

  readonly certificateId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly cert = signal<Certificate | null>(null);
  readonly copied = signal(false);
  readonly downloading = signal(false);
  readonly downloadError = signal<string | null>(null);

  readonly verifyUrl = computed(() => {
    const c = this.cert();
    if (!c || typeof window === 'undefined') return '';
    return `${window.location.origin}/verify/${c.verifyCode}`;
  });

  constructor() {
    effect(() => {
      const id = this.certificateId();
      if (id) this.fetch(id);
    });
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.studentService.getCertificate(id).subscribe({
      next: (c) => {
        this.cert.set(c);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.cert.set(null);
        this.error.set(err.status === 404
          ? "That certificate doesn't exist or isn't yours."
          : err.status === 0
            ? 'Cannot reach the API.'
            : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  print(): void {
    if (typeof window !== 'undefined') window.print();
  }

  async copyVerifyLink(): Promise<void> {
    const url = this.verifyUrl();
    if (!url || typeof navigator === 'undefined') return;
    try {
      await navigator.clipboard.writeText(url);
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2500);
    } catch {
      // Fallback — surface the URL via prompt so users can copy manually.
      if (typeof window !== 'undefined') window.prompt('Copy this link:', url);
    }
  }

  downloadPdf(): void {
    const c = this.cert();
    if (!c || this.downloading()) return;
    this.downloading.set(true);
    this.downloadError.set(null);

    this.studentService.downloadCertificatePdf(c.certificateId).subscribe({
      next: (blob) => {
        triggerBlobDownload(blob, `Certificate-${c.verifyCode}.pdf`);
        this.downloading.set(false);
      },
      error: () => {
        this.downloading.set(false);
        this.downloadError.set('Could not download the PDF. Please try again.');
      },
    });
  }
}

/**
 * Browser-only file-download helper. Creates a temporary object URL, clicks
 * an anchor, then revokes the URL to release the blob. Kept top-level so the
 * cert-list page can reuse it later without going through the component.
 */
function triggerBlobDownload(blob: Blob, fileName: string): void {
  if (typeof window === 'undefined') return;
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  window.URL.revokeObjectURL(url);
}
