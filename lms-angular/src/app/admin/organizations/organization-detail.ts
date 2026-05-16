import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { OrganizationDetail } from './models/organization.models';
import { OrganizationsService } from './organizations.service';

type Dialog = 'none' | 'edit-org' | 'create-branch' | 'edit-branch' | 'create-admin';

@Component({
  selector: 'app-organization-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './organization-detail.html',
})
export class OrganizationDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly orgs = inject(OrganizationsService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly data = signal<OrganizationDetail | null>(null);

  readonly dialog = signal<Dialog>('none');
  readonly editingBranchId = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly dialogError = signal<string | null>(null);

  readonly organizationId = computed(() => this.route.snapshot.paramMap.get('organizationId') ?? '');

  readonly orgForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    slug: ['', [Validators.pattern(/^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/)]],
    description: ['', [Validators.maxLength(2000)]],
    contactEmail: ['', [Validators.email]],
    isActive: [true],
  });

  readonly branchForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    code: ['', [Validators.pattern(/^[A-Z0-9](?:[A-Z0-9_-]*[A-Z0-9])?$/)]],
    location: ['', [Validators.maxLength(200)]],
    contactEmail: ['', [Validators.email]],
    isActive: [true],
  });

  readonly adminForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).+$/),
    ]],
  });

  constructor() { this.fetch(); }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);
    this.orgs.get(this.organizationId()).subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 404
          ? 'This organization no longer exists.'
          : err.error?.message ?? err.statusText ?? 'Something went wrong.');
      },
    });
  }

  // ---- Edit organization -----------------------------------------------

  openEditOrg(): void {
    const o = this.data();
    if (!o) return;
    this.orgForm.reset({
      name: o.name,
      slug: o.slug ?? '',
      description: o.description ?? '',
      contactEmail: o.contactEmail ?? '',
      isActive: o.isActive,
    });
    this.dialogError.set(null);
    this.dialog.set('edit-org');
  }

  submitEditOrg(): void {
    if (this.orgForm.invalid) { this.orgForm.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.dialogError.set(null);
    const v = this.orgForm.getRawValue();
    this.orgs.update(this.organizationId(), {
      name: v.name.trim(),
      slug: v.slug.trim() || null,
      description: v.description.trim() || null,
      contactEmail: v.contactEmail.trim() || null,
      logoUrl: this.data()?.logoUrl ?? null,
      isActive: v.isActive,
    }).subscribe({
      next: (updated) => {
        this.data.set({ ...updated, branches: updated.branches, orgAdmins: updated.orgAdmins });
        this.submitting.set(false);
        this.dialog.set('none');
      },
      error: (err: HttpErrorResponse) => this.handleDialogError(err),
    });
  }

  // ---- Create / edit branch --------------------------------------------

  openCreateBranch(): void {
    this.branchForm.reset({ name: '', code: '', location: '', contactEmail: '', isActive: true });
    this.editingBranchId.set(null);
    this.dialogError.set(null);
    this.dialog.set('create-branch');
  }

  openEditBranch(branchId: string): void {
    const b = this.data()?.branches.find((x) => x.id === branchId);
    if (!b) return;
    this.branchForm.reset({
      name: b.name,
      code: b.code ?? '',
      location: b.location ?? '',
      contactEmail: b.contactEmail ?? '',
      isActive: b.isActive,
    });
    this.editingBranchId.set(branchId);
    this.dialogError.set(null);
    this.dialog.set('edit-branch');
  }

  submitBranch(): void {
    if (this.branchForm.invalid) { this.branchForm.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.dialogError.set(null);
    const v = this.branchForm.getRawValue();
    const editingId = this.editingBranchId();
    const payload = {
      name: v.name.trim(),
      code: v.code.trim() || null,
      location: v.location.trim() || null,
      contactEmail: v.contactEmail.trim() || null,
      isActive: v.isActive,
    };

    const obs = editingId
      ? this.orgs.updateBranch(editingId, payload)
      : this.orgs.createBranch(this.organizationId(), payload);

    obs.subscribe({
      next: () => { this.submitting.set(false); this.dialog.set('none'); this.fetch(); },
      error: (err: HttpErrorResponse) => this.handleDialogError(err),
    });
  }

  // ---- Create OrgAdmin -------------------------------------------------

  openCreateAdmin(): void {
    this.adminForm.reset({ firstName: '', lastName: '', email: '', password: '' });
    this.dialogError.set(null);
    this.dialog.set('create-admin');
  }

  submitCreateAdmin(): void {
    if (this.adminForm.invalid) { this.adminForm.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.dialogError.set(null);
    const v = this.adminForm.getRawValue();
    this.orgs.createOrgAdmin(this.organizationId(), {
      firstName: v.firstName.trim(),
      lastName: v.lastName.trim(),
      email: v.email.trim().toLowerCase(),
      password: v.password,
    }).subscribe({
      next: () => { this.submitting.set(false); this.dialog.set('none'); this.fetch(); },
      error: (err: HttpErrorResponse) => this.handleDialogError(err),
    });
  }

  closeDialog(): void { this.dialog.set('none'); }

  private handleDialogError(err: HttpErrorResponse): void {
    this.submitting.set(false);
    const errors = err.error?.errors as Record<string, string[]> | undefined;
    const first = errors ? Object.values(errors).flat()[0] : undefined;
    this.dialogError.set(first ?? err.error?.message ?? err.statusText ?? 'Something went wrong.');
  }
}
