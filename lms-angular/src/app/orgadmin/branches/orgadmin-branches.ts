import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { OrgAdminService } from '../orgadmin.service';
import { OrgBranch } from '../models/orgadmin.models';

type Dialog = 'none' | 'create' | 'edit';

@Component({
  selector: 'app-orgadmin-branches',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './orgadmin-branches.html',
})
export class OrgAdminBranchesPage {
  private readonly api = inject(OrgAdminService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly branches = signal<OrgBranch[]>([]);

  readonly dialog = signal<Dialog>('none');
  readonly editingId = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly dialogError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    code: ['', [Validators.pattern(/^[A-Z0-9](?:[A-Z0-9_-]*[A-Z0-9])?$/)]],
    location: ['', [Validators.maxLength(200)]],
    contactEmail: ['', [Validators.email]],
    isActive: [true],
  });

  constructor() { this.fetch(); }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listBranches().subscribe({
      next: (rows) => { this.branches.set(rows); this.loading.set(false); },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? err.statusText ?? 'Could not load branches.');
      },
    });
  }

  openCreate(): void {
    this.form.reset({ name: '', code: '', location: '', contactEmail: '', isActive: true });
    this.editingId.set(null);
    this.dialogError.set(null);
    this.dialog.set('create');
  }

  openEdit(id: string): void {
    const b = this.branches().find((x) => x.id === id);
    if (!b) return;
    this.form.reset({
      name: b.name,
      code: b.code ?? '',
      location: b.location ?? '',
      contactEmail: b.contactEmail ?? '',
      isActive: b.isActive,
    });
    this.editingId.set(id);
    this.dialogError.set(null);
    this.dialog.set('edit');
  }

  closeDialog(): void { this.dialog.set('none'); }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.dialogError.set(null);
    const v = this.form.getRawValue();
    const payload = {
      name: v.name.trim(),
      code: v.code.trim() || null,
      location: v.location.trim() || null,
      contactEmail: v.contactEmail.trim() || null,
    };
    const editingId = this.editingId();

    const obs = editingId
      ? this.api.updateBranch(editingId, { ...payload, isActive: v.isActive })
      : this.api.createBranch(payload);

    obs.subscribe({
      next: () => { this.submitting.set(false); this.dialog.set('none'); this.fetch(); },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const errors = err.error?.errors as Record<string, string[]> | undefined;
        const first = errors ? Object.values(errors).flat()[0] : undefined;
        this.dialogError.set(first ?? err.error?.message ?? err.statusText ?? 'Could not save branch.');
      },
    });
  }
}
