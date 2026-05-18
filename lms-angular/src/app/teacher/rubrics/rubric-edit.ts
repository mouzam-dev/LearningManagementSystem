import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { Rubric, RubricCriterionInput, RubricUpsertBody } from '../models/rubric.models';
import { TeacherService } from '../teacher.service';

interface DraftCriterion {
  name: string;
  description: string;
  maxPoints: number;
}

interface DraftRubric {
  name: string;
  description: string;
  criteria: DraftCriterion[];
}

@Component({
  selector: 'app-teacher-rubric-edit',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './rubric-edit.html',
})
export class TeacherRubricEditPage {
  private readonly service = inject(TeacherService);
  private readonly router = inject(Router);

  readonly id = input<string | undefined>(undefined);
  readonly isEdit = computed(() => !!this.id());

  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly draft = signal<DraftRubric>(emptyDraft());

  readonly totalPoints = computed(() =>
    this.draft().criteria.reduce((sum, c) => sum + (c.maxPoints || 0), 0));

  constructor() {
    effect(() => {
      const id = this.id();
      if (id) this.load(id);
      else this.draft.set(emptyDraft());
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.service.getRubric(id).subscribe({
      next: (r) => {
        this.draft.set(toDraft(r));
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.loadError.set(err.status === 404
          ? "That rubric doesn't exist or isn't yours."
          : err.status === 0 ? 'Cannot reach the API.' : 'Failed to load rubric.');
      },
    });
  }

  updateName(v: string): void {
    this.draft.set({ ...this.draft(), name: v });
  }
  updateDescription(v: string): void {
    this.draft.set({ ...this.draft(), description: v });
  }

  addCriterion(): void {
    const cur = this.draft();
    if (cur.criteria.length >= 20) return;
    this.draft.set({
      ...cur,
      criteria: [...cur.criteria, { name: '', description: '', maxPoints: 10 }],
    });
  }

  removeCriterion(idx: number): void {
    const cur = this.draft();
    if (cur.criteria.length <= 1) return;
    this.draft.set({ ...cur, criteria: cur.criteria.filter((_, i) => i !== idx) });
  }

  updateCriterion<K extends keyof DraftCriterion>(idx: number, field: K, value: DraftCriterion[K]): void {
    const cur = this.draft();
    const next = cur.criteria.map((c, i) => (i === idx ? { ...c, [field]: value } : c));
    this.draft.set({ ...cur, criteria: next });
  }

  moveUp(idx: number): void {
    if (idx === 0) return;
    const cur = this.draft();
    const next = [...cur.criteria];
    [next[idx - 1], next[idx]] = [next[idx], next[idx - 1]];
    this.draft.set({ ...cur, criteria: next });
  }

  moveDown(idx: number): void {
    const cur = this.draft();
    if (idx === cur.criteria.length - 1) return;
    const next = [...cur.criteria];
    [next[idx + 1], next[idx]] = [next[idx], next[idx + 1]];
    this.draft.set({ ...cur, criteria: next });
  }

  save(): void {
    if (this.saving()) return;
    const body = this.toBody();
    if (!body) return;

    this.saving.set(true);
    this.saveError.set(null);

    const obs = this.id()
      ? this.service.updateRubric(this.id()!, body)
      : this.service.createRubric(body);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/teacher/rubrics']);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        if (err.status === 400) {
          // First validation error message (good enough for the form footer).
          const first = firstValidationMessage(err.error?.errors);
          this.saveError.set(first ?? 'Please fix the highlighted fields.');
        } else if (err.status === 404) {
          this.saveError.set('Rubric no longer exists.');
        } else {
          this.saveError.set('Could not save. Please try again.');
        }
      },
    });
  }

  private toBody(): RubricUpsertBody | null {
    const d = this.draft();
    if (!d.name.trim()) {
      this.saveError.set('Rubric name is required.');
      return null;
    }
    if (d.criteria.length === 0) {
      this.saveError.set('Add at least one criterion.');
      return null;
    }
    const criteria: RubricCriterionInput[] = [];
    for (const c of d.criteria) {
      const name = c.name.trim();
      if (!name) {
        this.saveError.set('Every criterion needs a name.');
        return null;
      }
      if (!Number.isFinite(c.maxPoints) || c.maxPoints < 1 || c.maxPoints > 100) {
        this.saveError.set('Max points must be between 1 and 100 for each criterion.');
        return null;
      }
      criteria.push({
        name,
        description: c.description.trim() || null,
        maxPoints: c.maxPoints,
      });
    }
    return {
      name: d.name.trim(),
      description: d.description.trim() || null,
      criteria,
    };
  }
}

function emptyDraft(): DraftRubric {
  return {
    name: '',
    description: '',
    criteria: [{ name: '', description: '', maxPoints: 10 }],
  };
}

function toDraft(r: Rubric): DraftRubric {
  return {
    name: r.name,
    description: r.description ?? '',
    criteria: r.criteria.length
      ? r.criteria.map((c) => ({
          name: c.name,
          description: c.description ?? '',
          maxPoints: c.maxPoints,
        }))
      : [{ name: '', description: '', maxPoints: 10 }],
  };
}

function firstValidationMessage(errors: Record<string, string[]> | undefined): string | null {
  if (!errors) return null;
  for (const arr of Object.values(errors)) {
    if (arr?.length) return arr[0];
  }
  return null;
}
