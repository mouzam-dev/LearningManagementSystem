import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AdminUserDetail } from '../models/admin.models';
import { AdminService } from '../admin.service';
import { AuthService } from '../../auth/auth.service';
import { OrganizationsService } from '../organizations/organizations.service';
import {
  BranchListItem,
  OrganizationListItem,
} from '../organizations/models/organization.models';

/**
 * The role being applied while a picker dialog is open. Promoting to OrgAdmin
 * needs an organization; assigning Teacher/Student needs a branch (and the
 * organization is inferred from it).
 */
type Picker =
  | { kind: 'none' }
  | { kind: 'org'; role: 'OrgAdmin'; selectedOrgId: string | null }
  | { kind: 'branch'; role: 'Teacher' | 'Student'; selectedOrgId: string | null; selectedBranchId: string | null }
  | { kind: 'transfer'; selectedOrgId: string | null; selectedBranchId: string | null };

@Component({
  selector: 'app-admin-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-user-detail.html',
})
export class AdminUserDetailPage {
  private readonly admin = inject(AdminService);
  private readonly auth = inject(AuthService);
  private readonly orgs = inject(OrganizationsService);

  readonly userId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly user = signal<AdminUserDetail | null>(null);
  readonly busy = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly roles = ['Student', 'Teacher', 'OrgAdmin', 'SuperAdmin'];

  // Organizations + their branches, fetched lazily when a picker opens.
  readonly orgsList = signal<OrganizationListItem[]>([]);
  readonly orgsLoading = signal(false);
  readonly branchesForSelected = signal<BranchListItem[]>([]);

  readonly picker = signal<Picker>({ kind: 'none' });

  /** True when the row being viewed is the signed-in admin themselves. */
  readonly isSelf = computed(() => {
    const u = this.user();
    return !!u && u.userId === this.auth.user()?.id;
  });

  constructor() {
    effect(() => {
      const id = this.userId();
      if (id) this.fetch(id);
    });
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.admin.getUser(id).subscribe({
      next: (u) => {
        this.user.set(u);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.user.set(null);
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API.'
          : err.status === 404
            ? 'This user was not found.'
            : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  toggleActive(): void {
    const u = this.user();
    if (!u) return;
    const next = !u.isActive;
    this.busy.set('active');
    this.actionError.set(null);

    this.admin.setUserActive(u.userId, next).subscribe({
      next: (updated) => {
        this.user.set(updated);
        this.busy.set(null);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        this.actionError.set(err.status === 409
          ? (err.error?.message ?? "That action isn't allowed.")
          : (err.error?.message ?? err.statusText ?? 'Could not update.'));
      },
    });
  }

  /**
   * Entry point from the role buttons. Roles that need extra context open a
   * picker dialog before the actual PATCH fires; SuperAdmin (or no-change)
   * goes straight through.
   */
  pickRole(role: string): void {
    const u = this.user();
    if (!u || u.role === role || this.isSelf()) return;
    this.actionError.set(null);

    if (role === 'OrgAdmin') {
      this.openOrgPicker();
      return;
    }
    if (role === 'Teacher' || role === 'Student') {
      this.openBranchPicker(role);
      return;
    }
    // SuperAdmin — no tenancy needed.
    this.submitRoleChange(role, null, null);
  }

  private openOrgPicker(): void {
    this.picker.set({ kind: 'org', role: 'OrgAdmin', selectedOrgId: this.user()?.organizationId ?? null });
    this.ensureOrgsLoaded();
  }

  /**
   * Entry point from the "Move to organization/branch" button — only enabled
   * for Teachers and Students. Reuses the same org+branch pickers.
   */
  openTransfer(): void {
    const u = this.user();
    if (!u) return;
    if (u.role !== 'Teacher' && u.role !== 'Student') return;
    this.actionError.set(null);
    this.picker.set({
      kind: 'transfer',
      selectedOrgId: u.organizationId ?? null,
      selectedBranchId: u.branchId ?? null,
    });
    this.ensureOrgsLoaded();
    if (u.organizationId) this.loadBranches(u.organizationId);
  }

  private openBranchPicker(role: 'Teacher' | 'Student'): void {
    const initialOrg = this.user()?.organizationId ?? null;
    this.picker.set({
      kind: 'branch',
      role,
      selectedOrgId: initialOrg,
      selectedBranchId: this.user()?.branchId ?? null,
    });
    this.ensureOrgsLoaded();
    if (initialOrg) this.loadBranches(initialOrg);
  }

  closePicker(): void { this.picker.set({ kind: 'none' }); }

  private ensureOrgsLoaded(): void {
    if (this.orgsList().length > 0) return;
    this.orgsLoading.set(true);
    this.orgs.list(undefined, 1, 100).subscribe({
      next: (page) => {
        this.orgsList.set(page.items.filter((o) => o.isActive));
        this.orgsLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.orgsLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Could not load organizations.');
      },
    });
  }

  onPickOrg(orgId: string): void {
    const p = this.picker();
    if (p.kind === 'org') {
      this.picker.set({ ...p, selectedOrgId: orgId });
    } else if (p.kind === 'branch' || p.kind === 'transfer') {
      // Picking a new org clears the previously chosen branch.
      this.picker.set({ ...p, selectedOrgId: orgId, selectedBranchId: null });
      this.loadBranches(orgId);
    }
  }

  onPickBranch(branchId: string): void {
    const p = this.picker();
    if (p.kind === 'branch' || p.kind === 'transfer') {
      this.picker.set({ ...p, selectedBranchId: branchId });
    }
  }

  private loadBranches(orgId: string): void {
    this.branchesForSelected.set([]);
    this.orgs.get(orgId).subscribe({
      next: (detail) => this.branchesForSelected.set(detail.branches.filter((b) => b.isActive)),
      error: () => this.branchesForSelected.set([]),
    });
  }

  confirmPicker(): void {
    const p = this.picker();
    if (p.kind === 'org') {
      if (!p.selectedOrgId) { this.actionError.set('Pick an organization first.'); return; }
      this.submitRoleChange(p.role, p.selectedOrgId, null);
    } else if (p.kind === 'branch') {
      if (!p.selectedBranchId) { this.actionError.set('Pick a branch first.'); return; }
      this.submitRoleChange(p.role, null, p.selectedBranchId);
    } else if (p.kind === 'transfer') {
      if (!p.selectedOrgId || !p.selectedBranchId) {
        this.actionError.set('Pick an organization and a branch.');
        return;
      }
      this.submitTransfer(p.selectedOrgId, p.selectedBranchId);
    }
  }

  private submitTransfer(organizationId: string, branchId: string): void {
    this.busy.set('transfer');
    this.actionError.set(null);
    const u = this.user();
    if (!u) return;

    this.admin.transferUser(u.userId, organizationId, branchId).subscribe({
      next: (updated) => {
        this.user.set(updated);
        this.busy.set(null);
        this.picker.set({ kind: 'none' });
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        if (err.status === 400 && err.error?.errors) {
          const messages = Object.values(err.error.errors as Record<string, string[]>).flat();
          this.actionError.set(messages.join(' '));
        } else {
          this.actionError.set(err.error?.message ?? err.statusText ?? 'Could not transfer user.');
        }
      },
    });
  }

  private submitRoleChange(role: string, organizationId: string | null, branchId: string | null): void {
    this.busy.set('role');
    this.actionError.set(null);
    const u = this.user();
    if (!u) return;

    this.admin.changeUserRole(u.userId, role, { organizationId, branchId }).subscribe({
      next: (updated) => {
        this.user.set(updated);
        this.busy.set(null);
        this.picker.set({ kind: 'none' });
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(null);
        if (err.status === 400 && err.error?.errors) {
          const messages = Object.values(err.error.errors as Record<string, string[]>).flat();
          this.actionError.set(messages.join(' '));
        } else {
          this.actionError.set(err.error?.message ?? err.statusText ?? 'Could not change role.');
        }
      },
    });
  }
}
