import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { TeacherSubmissionDetail } from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

@Component({
  selector: 'app-teacher-submission-grader',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './submission-grader.html',
})
export class TeacherSubmissionGraderPage {
  private readonly teacher = inject(TeacherService);

  readonly submissionId = input.required<string>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly submission = signal<TeacherSubmissionDetail | null>(null);
  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);

  // Flat-score mode (used when no rubric is attached).
  readonly draftScore = signal<number>(0);
  readonly draftFeedback = signal<string>('');

  // Rubric mode (TCH-025) — map of criterionId -> awarded points.
  readonly draftCriterionScores = signal<Record<string, number>>({});

  readonly hasRubric = computed(() => !!this.submission()?.rubric);

  readonly rubricTotal = computed(() => {
    const cs = this.draftCriterionScores();
    return Math.min(100, Object.values(cs).reduce((sum, v) => sum + (v || 0), 0));
  });

  readonly isPassed = computed(() => {
    const s = this.submission();
    if (!s || s.score === null || s.score === undefined) return false;
    return s.score >= s.passingScore;
  });

  constructor() {
    effect(() => {
      const id = this.submissionId();
      if (id) this.fetch(id);
    });
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getSubmission(id).subscribe({
      next: (s) => {
        this.submission.set(s);
        this.draftScore.set(s.score ?? 0);
        this.draftFeedback.set(s.feedback ?? '');

        // Seed the rubric inputs: prefer persisted breakdown, else 0 for each
        // criterion so the form is in a coherent starting state.
        if (s.rubric) {
          const seed: Record<string, number> = {};
          for (const c of s.rubric.criteria) {
            seed[c.id] = s.criterionScores?.[c.id] ?? 0;
          }
          this.draftCriterionScores.set(seed);
        } else {
          this.draftCriterionScores.set({});
        }

        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.submission.set(null);
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API.'
          : err.status === 404
            ? 'This submission was not found.'
            : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }

  setCriterionScore(criterionId: string, value: number, max: number): void {
    const clamped = Math.max(0, Math.min(max, Number.isFinite(value) ? value : 0));
    this.draftCriterionScores.set({
      ...this.draftCriterionScores(),
      [criterionId]: clamped,
    });
  }

  save(): void {
    const s = this.submission();
    if (!s) return;
    this.saving.set(true);
    this.saveError.set(null);

    const body = this.hasRubric()
      ? {
          criterionScores: this.draftCriterionScores(),
          feedback: this.draftFeedback().trim() || null,
        }
      : {
          score: this.draftScore(),
          feedback: this.draftFeedback().trim() || null,
        };

    this.teacher.gradeSubmission(s.submissionId, body).subscribe({
      next: (updated) => {
        this.submission.set(updated);
        this.draftScore.set(updated.score ?? 0);
        this.draftFeedback.set(updated.feedback ?? '');
        if (updated.rubric) {
          const seed: Record<string, number> = {};
          for (const c of updated.rubric.criteria) {
            seed[c.id] = updated.criterionScores?.[c.id] ?? 0;
          }
          this.draftCriterionScores.set(seed);
        }
        this.saving.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        if (err.status === 400 && err.error?.errors) {
          const messages = Object.values(err.error.errors as Record<string, string[]>).flat();
          this.saveError.set(messages.join(' '));
        } else {
          this.saveError.set(err.error?.message ?? err.statusText ?? 'Could not save grade.');
        }
      },
    });
  }
}
