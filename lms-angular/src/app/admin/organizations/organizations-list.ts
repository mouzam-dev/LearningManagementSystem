import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { OrganizationListItem } from './models/organization.models';
import { OrganizationsService } from './organizations.service';

@Component({
  selector: 'app-organizations-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './organizations-list.html',
})
export class OrganizationsListPage {
  private readonly orgs = inject(OrganizationsService);
  private readonly fb = inject(FormBuilder);
  private readonly searchInput$ = new Subject<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<OrganizationListItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly searchTerm = signal('');

  readonly creating = signal(false);
  readonly createError = signal<string | null>(null);
  readonly createOpen = signal(false);

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  readonly rangeLabel = computed(() => {
    const total = this.totalCount();
    if (total === 0) return 'No organizations';
    const start = (this.page() - 1) * this.pageSize() + 1;
    const end = Math.min(total, this.page() * this.pageSize());
    return `${start}–${end} of ${total}`;
  });

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    slug: ['', [Validators.pattern(/^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/)]],
    description: ['', [Validators.maxLength(2000)]],
    contactEmail: ['', [Validators.email]],
  });

  constructor() {
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchTerm.set(term);
        this.page.set(1);
        this.fetch();
      });
    this.fetch();
  }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.orgs.list(this.searchTerm() || undefined, this.page(), this.pageSize())
      .subscribe({
        next: (res) => {
          this.items.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.error.set(err.status === 0
            ? 'Cannot reach the API.'
            : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
        },
      });
  }

  onSearchInput(value: string): void { this.searchInput$.next(value); }

  prevPage(): void {
    if (this.page() > 1) { this.page.update((p) => p - 1); this.fetch(); }
  }
  nextPage(): void {
    if (this.page() < this.totalPages()) { this.page.update((p) => p + 1); this.fetch(); }
  }

  openCreate(): void {
    this.form.reset({ name: '', slug: '', description: '', contactEmail: '' });
    this.createError.set(null);
    this.createOpen.set(true);
  }

  closeCreate(): void { this.createOpen.set(false); }

  submitCreate(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.creating.set(true);
    this.createError.set(null);

    const v = this.form.getRawValue();
    this.orgs.create({
      name: v.name.trim(),
      slug: v.slug.trim() || null,
      description: v.description.trim() || null,
      contactEmail: v.contactEmail.trim() || null,
    }).subscribe({
      next: () => {
        this.creating.set(false);
        this.createOpen.set(false);
        this.page.set(1);
        this.fetch();
      },
      error: (err: HttpErrorResponse) => {
        this.creating.set(false);
        const errors = err.error?.errors as Record<string, string[]> | undefined;
        const first = errors ? Object.values(errors).flat()[0] : undefined;
        this.createError.set(first ?? err.error?.message ?? err.statusText ?? 'Could not create organization.');
      },
    });
  }
}
