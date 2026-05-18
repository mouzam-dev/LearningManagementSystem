import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AdminUserListItem } from '../models/admin.models';
import { AdminService, BulkImportUsersResult } from '../admin.service';

type RoleFilter = '' | 'Student' | 'Teacher' | 'OrgAdmin' | 'SuperAdmin';
type StatusFilter = 'all' | 'active' | 'suspended';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-users.html',
})
export class AdminUsersPage {
  private readonly admin = inject(AdminService);
  private readonly searchInput$ = new Subject<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly users = signal<AdminUserListItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);

  readonly searchTerm = signal('');
  readonly roleFilter = signal<RoleFilter>('');
  readonly statusFilter = signal<StatusFilter>('all');

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  readonly rangeLabel = computed(() => {
    const total = this.totalCount();
    if (total === 0) return 'No users';
    const start = (this.page() - 1) * this.pageSize() + 1;
    const end = Math.min(total, this.page() * this.pageSize());
    return `${start}–${end} of ${total}`;
  });

  constructor() {
    // Debounce the search box so we're not firing a request per keystroke.
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

    const status = this.statusFilter();
    this.admin
      .getUsers({
        search: this.searchTerm() || undefined,
        role: this.roleFilter() || undefined,
        isActive: status === 'all' ? null : status === 'active',
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (res) => {
          this.users.set(res.items);
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

  onSearchInput(value: string): void {
    this.searchInput$.next(value);
  }

  setRole(role: RoleFilter): void {
    this.roleFilter.set(role);
    this.page.set(1);
    this.fetch();
  }

  setStatus(status: StatusFilter): void {
    this.statusFilter.set(status);
    this.page.set(1);
    this.fetch();
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.fetch();
    }
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((p) => p + 1);
      this.fetch();
    }
  }

  relativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const d = Math.round(diff / (24 * 60 * 60 * 1000));
    if (d < 1) return 'today';
    if (d === 1) return 'yesterday';
    if (d < 30) return `${d} days ago`;
    return new Date(iso).toLocaleDateString();
  }

  // ----- Bulk import (BRD ADM-012) --------------------------------------

  readonly importOpen = signal(false);
  readonly importBusy = signal(false);
  readonly importError = signal<string | null>(null);
  readonly importResult = signal<BulkImportUsersResult | null>(null);
  readonly importFileName = signal<string | null>(null);

  openImport(): void {
    this.importOpen.set(true);
    this.importResult.set(null);
    this.importError.set(null);
    this.importFileName.set(null);
  }

  closeImport(): void {
    this.importOpen.set(false);
    this.importBusy.set(false);
    // If we did any imports, refresh the user list so they show up.
    if (this.importResult()?.successCount) {
      this.page.set(1);
      this.fetch();
    }
  }

  onImportFile(input: HTMLInputElement): void {
    const file = input.files?.[0];
    if (!file) return;
    this.importFileName.set(file.name);
    this.importResult.set(null);
    this.importError.set(null);
    this.importBusy.set(true);

    this.admin.bulkImportUsers(file).subscribe({
      next: (res) => {
        this.importResult.set(res);
        this.importBusy.set(false);
        input.value = '';
      },
      error: (err: HttpErrorResponse) => {
        this.importBusy.set(false);
        input.value = '';
        this.importError.set(err.status === 400
          ? (err.error?.message ?? 'CSV was rejected.')
          : err.status === 0 ? 'Cannot reach the API.' : 'Import failed.');
      },
    });
  }

  /** Builds a tiny sample CSV in-memory and triggers a browser download. */
  downloadSampleTemplate(): void {
    const lines = [
      'Email,FirstName,LastName,Role,OrganizationSlug,BranchName',
      'ada.lovelace@example.com,Ada,Lovelace,Student,,',
      'alan.turing@example.com,Alan,Turing,Teacher,pioneer-tech,Pioneer HQ',
      'grace.hopper@example.com,Grace,Hopper,OrgAdmin,codeforge,',
    ];
    const blob = new Blob([lines.join('\n')], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'lms-bulk-import-template.csv';
    a.click();
    URL.revokeObjectURL(url);
  }
}
