import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { OrgAdminService } from '../orgadmin.service';
import { OrgBranch, OrgTeacher } from '../models/orgadmin.models';

@Component({
  selector: 'app-orgadmin-teachers',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './orgadmin-teachers.html',
})
export class OrgAdminTeachersPage {
  private readonly api = inject(OrgAdminService);
  private readonly fb = inject(FormBuilder);
  private readonly searchInput$ = new Subject<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly teachers = signal<OrgTeacher[]>([]);
  readonly branches = signal<OrgBranch[]>([]);

  readonly search = signal('');
  readonly branchFilter = signal<string>('');

  // Reassign-branch picker state (inline beneath a row)
  readonly assignTarget = signal<string | null>(null);
  readonly assignSelection = signal<string | null>(null);
  readonly assignBusy = signal(false);
  readonly assignError = signal<string | null>(null);

  // Add-teacher dialog state
  readonly addDialogOpen = signal(false);
  readonly addSubmitting = signal(false);
  readonly addError = signal<string | null>(null);
  readonly addForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).+$/),
    ]],
    branchId: ['', [Validators.required]],
  });

  readonly activeBranches = computed(() => this.branches().filter((b) => b.isActive));

  constructor() {
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((s) => { this.search.set(s); this.fetchTeachers(); });

    this.fetchBranches();
    this.fetchTeachers();
  }

  private fetchBranches(): void {
    this.api.listBranches().subscribe({
      next: (rows) => this.branches.set(rows),
      error: () => this.branches.set([]),
    });
  }

  fetchTeachers(): void {
    this.loading.set(true);
    this.error.set(null);
    const branch = this.branchFilter();
    this.api.listTeachers(this.search() || undefined, branch === '' || branch === 'none' ? null : branch).subscribe({
      next: (rows) => {
        const filtered = branch === 'none' ? rows.filter((t) => !t.branchId) : rows;
        this.teachers.set(filtered);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? err.statusText ?? 'Could not load teachers.');
      },
    });
  }

  onSearchInput(v: string): void { this.searchInput$.next(v); }
  setBranchFilter(value: string): void { this.branchFilter.set(value); this.fetchTeachers(); }

  // ----- Reassign branch (existing teacher) -----------------------------

  openAssign(teacherId: string, currentBranchId: string | null | undefined): void {
    this.assignTarget.set(teacherId);
    this.assignSelection.set(currentBranchId ?? null);
    this.assignError.set(null);
  }

  cancelAssign(): void { this.assignTarget.set(null); this.assignSelection.set(null); }
  pickBranch(branchId: string): void { this.assignSelection.set(branchId); }

  confirmAssign(): void {
    const teacherId = this.assignTarget();
    const branchId = this.assignSelection();
    if (!teacherId || !branchId) { this.assignError.set('Pick a branch first.'); return; }
    this.assignBusy.set(true);
    this.assignError.set(null);
    this.api.assignTeacherToBranch(teacherId, branchId).subscribe({
      next: () => { this.assignBusy.set(false); this.assignTarget.set(null); this.fetchTeachers(); },
      error: (err: HttpErrorResponse) => {
        this.assignBusy.set(false);
        const errors = err.error?.errors as Record<string, string[]> | undefined;
        const first = errors ? Object.values(errors).flat()[0] : undefined;
        this.assignError.set(first ?? err.error?.message ?? err.statusText ?? 'Could not reassign teacher.');
      },
    });
  }

  // ----- Add new teacher -----------------------------------------------

  openAddDialog(): void {
    this.addForm.reset({
      firstName: '', lastName: '', email: '', password: '', branchId: '',
    });
    this.addError.set(null);
    this.addDialogOpen.set(true);
  }

  closeAddDialog(): void { this.addDialogOpen.set(false); }

  submitAddTeacher(): void {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.addSubmitting.set(true);
    this.addError.set(null);
    const v = this.addForm.getRawValue();
    this.api.createTeacher({
      firstName: v.firstName.trim(),
      lastName: v.lastName.trim(),
      email: v.email.trim().toLowerCase(),
      password: v.password,
      branchId: v.branchId,
    }).subscribe({
      next: () => {
        this.addSubmitting.set(false);
        this.addDialogOpen.set(false);
        this.fetchTeachers();
      },
      error: (err: HttpErrorResponse) => {
        this.addSubmitting.set(false);
        const errors = err.error?.errors as Record<string, string[]> | undefined;
        const first = errors ? Object.values(errors).flat()[0] : undefined;
        this.addError.set(first ?? err.error?.message ?? err.statusText ?? 'Could not add teacher.');
      },
    });
  }
}
