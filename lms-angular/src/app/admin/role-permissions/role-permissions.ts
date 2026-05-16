import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';

import { environment } from '../../../environments/environment';

interface PermissionDto { id: string; code: string; description: string; }
interface RolePermissionMatrix {
  roles: string[];
  permissions: PermissionDto[];
  grants: Record<string, string[]>;
}

@Component({
  selector: 'app-role-permissions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './role-permissions.html',
})
export class RolePermissionsPage {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/superadmin`;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly matrix = signal<RolePermissionMatrix | null>(null);

  /** Local edit state: role -> Set of permission codes granted. */
  readonly draft = signal<Record<string, Set<string>>>({});
  readonly busyRole = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly savedRole = signal<string | null>(null);

  readonly dirtyRoles = computed(() => {
    const m = this.matrix();
    const d = this.draft();
    if (!m) return new Set<string>();
    const dirty = new Set<string>();
    for (const role of m.roles) {
      const baseline = new Set(m.grants[role] ?? []);
      const next = d[role] ?? new Set<string>();
      if (baseline.size !== next.size || [...baseline].some((c) => !next.has(c))) {
        dirty.add(role);
      }
    }
    return dirty;
  });

  constructor() { this.fetch(); }

  fetch(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<RolePermissionMatrix>(`${this.baseUrl}/role-permissions`).subscribe({
      next: (m) => {
        this.matrix.set(m);
        const draft: Record<string, Set<string>> = {};
        for (const role of m.roles) {
          draft[role] = new Set(m.grants[role] ?? []);
        }
        this.draft.set(draft);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? err.statusText ?? 'Could not load the role/permission matrix.');
      },
    });
  }

  isGranted(role: string, code: string): boolean {
    return !!this.draft()[role]?.has(code);
  }

  toggle(role: string, code: string): void {
    const current = this.draft();
    const next: Record<string, Set<string>> = { ...current };
    const set = new Set(current[role] ?? []);
    if (set.has(code)) set.delete(code); else set.add(code);
    next[role] = set;
    this.draft.set(next);
    this.savedRole.set(null);
  }

  save(role: string): void {
    const codes = Array.from(this.draft()[role] ?? new Set<string>());
    this.busyRole.set(role);
    this.saveError.set(null);
    this.savedRole.set(null);

    this.http
      .put<RolePermissionMatrix>(`${this.baseUrl}/role-permissions`, { role, permissionCodes: codes })
      .subscribe({
        next: (m) => {
          this.matrix.set(m);
          const draft: Record<string, Set<string>> = {};
          for (const r of m.roles) draft[r] = new Set(m.grants[r] ?? []);
          this.draft.set(draft);
          this.busyRole.set(null);
          this.savedRole.set(role);
        },
        error: (err: HttpErrorResponse) => {
          this.busyRole.set(null);
          this.saveError.set(err.error?.message ?? err.statusText ?? 'Could not save changes.');
        },
      });
  }

  revert(role: string): void {
    const m = this.matrix();
    if (!m) return;
    const baseline = new Set(m.grants[role] ?? []);
    this.draft.update((d) => ({ ...d, [role]: baseline }));
    this.savedRole.set(null);
  }
}
