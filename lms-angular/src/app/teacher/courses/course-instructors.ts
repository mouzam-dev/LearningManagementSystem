import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { CourseInstructor, EligibleCoInstructor } from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

/**
 * Roster of instructors on a course. The primary teacher (passed via the
 * <c>canManage</c> input) can add or remove co-instructors; co-instructors
 * see the list read-only but can resign (remove themselves).
 */
@Component({
  selector: 'app-course-instructors',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="rounded-2xl border border-line bg-surface shadow-sm">
      <header class="flex flex-wrap items-center justify-between gap-3 border-b border-line px-5 py-4">
        <div>
          <h2 class="text-base font-bold text-ink">Instructors</h2>
          <p class="text-xs text-muted">
            The primary teacher owns the course; co-instructors can author and grade alongside them.
          </p>
        </div>
        @if (canManage()) {
          <button type="button" (click)="openPicker()"
                  class="rounded-xl bg-primary-600 px-3 py-1.5 text-sm font-semibold text-white shadow-sm transition hover:bg-primary-700">
            + Add co-instructor
          </button>
        }
      </header>

      @if (loading()) {
        <div class="px-5 py-6 text-sm text-muted">Loading instructors…</div>
      } @else if (error(); as msg) {
        <div class="px-5 py-4 text-sm text-rose-700">{{ msg }}</div>
      } @else {
        <ul class="divide-y divide-line">
          @for (i of instructors(); track i.userId) {
            <li class="flex flex-wrap items-center justify-between gap-3 px-5 py-3.5">
              <div class="flex min-w-0 items-center gap-3">
                @if (i.profilePictureUrl) {
                  <img [src]="i.profilePictureUrl" [alt]="i.firstName + ' ' + i.lastName"
                       class="h-9 w-9 shrink-0 rounded-full object-cover ring-1 ring-line" />
                } @else {
                  <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary-50 text-xs font-bold text-primary-700"
                        aria-hidden="true">{{ i.firstName.charAt(0) }}{{ i.lastName.charAt(0) }}</span>
                }
                <div class="min-w-0">
                  <div class="flex flex-wrap items-center gap-2">
                    <p class="truncate text-sm font-semibold text-ink">{{ i.firstName }} {{ i.lastName }}</p>
                    @if (i.isPrimary) {
                      <span class="rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold text-amber-700">Primary</span>
                    } @else {
                      <span class="rounded-full bg-primary-100 px-2 py-0.5 text-[10px] font-semibold text-primary-700">Co-instructor</span>
                    }
                  </div>
                  <p class="truncate text-xs text-muted">
                    {{ i.email }}
                    @if (i.branchName) { · {{ i.branchName }} }
                    @if (i.addedAt) { · added {{ i.addedAt | date: 'mediumDate' }} }
                  </p>
                </div>
              </div>
              @if (!i.isPrimary && (canManage() || i.userId === currentUserId())) {
                <button type="button" (click)="remove(i)"
                        [disabled]="removingId() === i.userId"
                        class="text-xs font-semibold text-muted transition hover:text-rose-600 disabled:opacity-50">
                  {{ removingId() === i.userId ? 'Removing…' : (i.userId === currentUserId() ? 'Leave' : 'Remove') }}
                </button>
              }
            </li>
          }
        </ul>
      }

      <!-- Add co-instructor picker (primary teacher only) -->
      @if (pickerOpen()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4"
             (click)="closePicker()">
          <div class="w-full max-w-lg rounded-2xl border border-line bg-surface p-6 shadow-xl"
               (click)="$event.stopPropagation()">
            <h2 class="mb-4 text-lg font-bold text-ink">Add a co-instructor</h2>
            <p class="mb-3 text-sm text-muted">
              Pick from teachers in your organization. The primary teacher and existing co-instructors are filtered out.
            </p>

            <input type="search" [(ngModel)]="searchText" (ngModelChange)="onSearchChange($event)"
                   placeholder="Search by name or email…"
                   class="mb-3 w-full rounded-xl border border-line bg-surface-2 px-3 py-2 text-sm text-ink shadow-sm focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-200" />

            @if (eligibleLoading()) {
              <p class="px-1 py-4 text-sm text-muted">Searching…</p>
            } @else if (eligible().length === 0) {
              <p class="px-1 py-4 text-sm text-muted">No matching teachers found.</p>
            } @else {
              <ul class="max-h-64 divide-y divide-line overflow-y-auto rounded-xl border border-line">
                @for (e of eligible(); track e.userId) {
                  <li>
                    <button type="button" (click)="add(e)"
                            [disabled]="addingId() === e.userId"
                            class="flex w-full items-center justify-between px-4 py-3 text-left transition hover:bg-surface-2 disabled:opacity-50">
                      <div class="min-w-0">
                        <p class="truncate text-sm font-semibold text-ink">{{ e.firstName }} {{ e.lastName }}</p>
                        <p class="truncate text-xs text-muted">
                          {{ e.email }}@if (e.branchName) { · {{ e.branchName }} }
                        </p>
                      </div>
                      <span class="shrink-0 text-xs font-semibold text-primary-600">
                        {{ addingId() === e.userId ? 'Adding…' : '+ Add' }}
                      </span>
                    </button>
                  </li>
                }
              </ul>
            }

            @if (pickerError(); as msg) {
              <p class="mt-3 rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-700">{{ msg }}</p>
            }
            <div class="mt-4 flex justify-end">
              <button type="button" (click)="closePicker()"
                      class="rounded-xl border border-line bg-surface px-4 py-2 text-sm font-semibold text-muted transition hover:border-line-strong hover:text-ink">
                Close
              </button>
            </div>
          </div>
        </div>
      }
    </section>
  `,
})
export class CourseInstructorsPanel {
  private readonly teacher = inject(TeacherService);
  private readonly searchInput$ = new Subject<string>();

  readonly courseId = input.required<string>();
  readonly canManage = input<boolean>(false);
  readonly currentUserId = input<string>('');

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly instructors = signal<CourseInstructor[]>([]);

  readonly pickerOpen = signal(false);
  readonly pickerError = signal<string | null>(null);
  readonly searchText = signal('');
  readonly eligibleLoading = signal(false);
  readonly eligible = signal<EligibleCoInstructor[]>([]);
  readonly addingId = signal<string | null>(null);
  readonly removingId = signal<string | null>(null);

  constructor() {
    // Reload roster whenever the bound courseId changes.
    effect(() => {
      const id = this.courseId();
      if (id) this.fetch(id);
    });

    this.searchInput$
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((q) => this.fetchEligible(q));
  }

  onSearchChange(v: string): void { this.searchInput$.next(v); }

  openPicker(): void {
    this.pickerError.set(null);
    this.searchText.set('');
    this.eligible.set([]);
    this.pickerOpen.set(true);
    this.fetchEligible('');
  }
  closePicker(): void { this.pickerOpen.set(false); }

  add(candidate: EligibleCoInstructor): void {
    if (this.addingId() !== null) return;
    this.addingId.set(candidate.userId);
    this.pickerError.set(null);
    this.teacher.addCoInstructor(this.courseId(), candidate.userId).subscribe({
      next: (roster) => {
        this.instructors.set(roster);
        this.addingId.set(null);
        // Refresh the candidate list (the one we just added drops off).
        this.fetchEligible(this.searchText());
      },
      error: (err: HttpErrorResponse) => {
        this.addingId.set(null);
        this.pickerError.set(err.error?.message ?? err.statusText ?? 'Could not add.');
      },
    });
  }

  remove(target: CourseInstructor): void {
    if (this.removingId() !== null) return;
    if (typeof window !== 'undefined' &&
        !window.confirm(`Remove ${target.firstName} ${target.lastName} as a co-instructor?`)) {
      return;
    }
    this.removingId.set(target.userId);
    this.teacher.removeCoInstructor(this.courseId(), target.userId).subscribe({
      next: (roster) => {
        this.instructors.set(roster);
        this.removingId.set(null);
      },
      error: (err: HttpErrorResponse) => {
        this.removingId.set(null);
        this.error.set(err.error?.message ?? err.statusText ?? 'Could not remove.');
      },
    });
  }

  private fetch(courseId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.teacher.listCourseInstructors(courseId).subscribe({
      next: (rows) => { this.instructors.set(rows); this.loading.set(false); },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? err.statusText ?? 'Could not load instructors.');
      },
    });
  }

  private fetchEligible(q: string): void {
    if (!this.pickerOpen()) return;
    this.eligibleLoading.set(true);
    this.teacher.listEligibleCoInstructors(this.courseId(), q || undefined).subscribe({
      next: (rows) => { this.eligible.set(rows); this.eligibleLoading.set(false); },
      error: () => { this.eligible.set([]); this.eligibleLoading.set(false); },
    });
  }
}
