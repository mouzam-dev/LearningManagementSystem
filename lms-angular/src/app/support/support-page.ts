import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-support',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './support-page.html',
  styleUrl: './support-page.css',
})
export class SupportPage {
  private readonly auth = inject(AuthService);

  readonly user = this.auth.user;

  /**
   * Path to the generated user-manual HTML (lives in lms-angular/public/).
   * The iframe uses a literal `src=` attribute (not `[src]=` binding) to skip
   * Angular's resource-URL sanitizer, which insists on a SafeResourceUrl
   * wrapper. Same-origin static asset, no untrusted input — safe to inline.
   */
  readonly manualUrl = '/docs/user-manual/index.html';
  readonly pdfUrl = '/docs/user-manual/LMS-User-Manual.pdf';
}
