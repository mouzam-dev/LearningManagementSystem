import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { MeResponse } from '../auth/models/auth.models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.auth.user;
  readonly meResult = signal<MeResponse | null>(null);
  readonly meError = signal<string | null>(null);
  readonly meLoading = signal(false);

  callMe(): void {
    this.meLoading.set(true);
    this.meError.set(null);
    this.meResult.set(null);

    this.auth.me().subscribe({
      next: (res) => {
        this.meResult.set(res);
        this.meLoading.set(false);
      },
      error: (err) => {
        this.meError.set(`HTTP ${err.status}: ${err.statusText || 'request failed'}`);
        this.meLoading.set(false);
      },
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
