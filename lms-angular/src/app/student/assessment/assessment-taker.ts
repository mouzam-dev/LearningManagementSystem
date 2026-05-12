import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import {
  AssessmentDetail,
  Question,
  SubmissionResult,
} from '../models/assessment.models';
import { StudentService } from '../student.service';

// Single answer for an assignment is stored under this synthetic key, since
// assignments don't have Question entities.
const ASSIGNMENT_ANSWER_KEY = 'assignment-response';

@Component({
  selector: 'app-student-assessment-taker',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './assessment-taker.html',
})
export class StudentAssessmentTaker {
  private readonly studentService = inject(StudentService);
  private readonly router = inject(Router);

  /** Bound from the route via withComponentInputBinding(). */
  readonly assessmentId = input.required<string>();

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly assessment = signal<AssessmentDetail | null>(null);

  /** In-memory answer state, keyed by questionId (or ASSIGNMENT_ANSWER_KEY). */
  readonly answers = signal<Record<string, string>>({});

  /** Last submission result (after the user clicks Submit). */
  readonly result = signal<SubmissionResult | null>(null);

  /** Disabled state mirrors a prior pass — once you've passed, the taker becomes a results view. */
  readonly readOnly = computed(() => {
    const a = this.assessment();
    return !!a?.lastSubmission?.isPassed
        || !!this.result()
        || (a?.attemptsRemaining === 0);
  });

  readonly answerCount = computed(() => Object.values(this.answers()).filter((v) => v?.trim()).length);

  readonly canSubmit = computed(() => {
    const a = this.assessment();
    if (!a) return false;
    if (this.submitting() || this.readOnly()) return false;
    if (a.type === 'Assignment') {
      return (this.answers()[ASSIGNMENT_ANSWER_KEY] ?? '').trim().length > 0;
    }
    // Quiz: require an answer for every question.
    return a.questions.length > 0 && this.answerCount() === a.questions.length;
  });

  constructor() {
    // Refetch when the route param changes.
    effect(() => {
      const id = this.assessmentId();
      if (id) this.fetch(id);
    });
  }

  fetch(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);
    this.answers.set({});

    this.studentService.getAssessment(id).subscribe({
      next: (a) => {
        this.assessment.set(a);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.assessment.set(null);
        this.loading.set(false);
        this.error.set(this.formatError(err));
      },
    });
  }

  setAnswer(questionId: string, value: string): void {
    this.answers.update((curr) => ({ ...curr, [questionId]: value }));
  }

  setAssignmentAnswer(value: string): void {
    this.setAnswer(ASSIGNMENT_ANSWER_KEY, value);
  }

  isOptionSelected(question: Question, option: string): boolean {
    return this.answers()[question.questionId] === option;
  }

  submit(): void {
    const a = this.assessment();
    if (!a || !this.canSubmit()) return;

    this.submitting.set(true);
    this.error.set(null);

    this.studentService
      .submitAssessment(a.assessmentId, { answers: this.answers() })
      .subscribe({
        next: (res) => {
          this.result.set(res);
          this.submitting.set(false);
          if (typeof window !== 'undefined') {
            window.scrollTo({ top: 0, behavior: 'smooth' });
          }
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          this.error.set(this.formatError(err));
        },
      });
  }

  retry(): void {
    this.fetch(this.assessmentId());
  }

  goToCourse(): void {
    const a = this.assessment();
    if (a) this.router.navigate(['/student/courses', a.courseId]);
  }

  /** "Due in 3 days" / "Past due" / etc. */
  dueLabel(dueDate?: string | null): string {
    if (!dueDate) return '';
    const due = new Date(dueDate);
    const days = Math.round((due.getTime() - Date.now()) / (1000 * 60 * 60 * 24));
    if (days < 0) return `Past due (${due.toLocaleDateString()})`;
    if (days === 0) return 'Due today';
    if (days === 1) return 'Due tomorrow';
    if (days < 14) return `Due in ${days} days`;
    return `Due ${due.toLocaleDateString()}`;
  }

  /** Convenience for the template — looks up a per-question result by id. */
  resultFor(questionId: string) {
    return this.result()?.questionResults?.find((r) => r.questionId === questionId);
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) return 'Cannot reach the API. Is it running on http://localhost:5116?';
    if (err.status === 403) return "You're not enrolled in the parent course.";
    if (err.status === 404) return 'This assessment no longer exists.';
    if (err.status === 409) return (err.error as { message?: string } | null)?.message ?? "You can't submit again.";
    const body = err.error as { message?: string } | null;
    return body?.message ?? err.statusText ?? 'Something went wrong.';
  }
}
