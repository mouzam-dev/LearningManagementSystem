import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { TeacherService } from '../teacher.service';

interface CreateAssessmentControls {
  title: FormControl<string>;
  type: FormControl<string>;
  timeLimit: FormControl<number | null>;
  passingScore: FormControl<number>;
  dueDate: FormControl<string>;
  maxAttempts: FormControl<number | null>;
  isFinalExam: FormControl<boolean>;
}

/**
 * Step 1 of authoring an assessment: collect metadata. After save, redirect
 * to /teacher/assessments/:id where the teacher adds questions.
 */
@Component({
  selector: 'app-teacher-create-assessment',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './create-assessment.html',
})
export class TeacherCreateAssessmentPage {
  private readonly teacher = inject(TeacherService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly courseId = input.required<string>();

  readonly submitting = signal(false);
  readonly serverError = signal<string | null>(null);
  readonly fieldErrors = signal<Record<string, string[]>>({});

  readonly form: FormGroup<CreateAssessmentControls> = this.fb.nonNullable.group({
    title: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    type: this.fb.nonNullable.control('Quiz', [Validators.required]),
    timeLimit: this.fb.control<number | null>(null, [Validators.min(1)]),
    passingScore: this.fb.nonNullable.control(70, [Validators.required, Validators.min(0), Validators.max(100)]),
    dueDate: this.fb.nonNullable.control(''),
    maxAttempts: this.fb.control<number | null>(null, [Validators.min(1)]),
    isFinalExam: this.fb.nonNullable.control(false),
  });

  submit(): void {
    this.serverError.set(null);
    this.fieldErrors.set({});

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.submitting.set(true);

    this.teacher
      .createAssessment(this.courseId(), {
        title: raw.title.trim(),
        type: raw.type,
        timeLimit: raw.timeLimit,
        passingScore: raw.passingScore,
        dueDate: raw.dueDate ? new Date(raw.dueDate).toISOString() : null,
        maxAttempts: raw.maxAttempts,
        isFinalExam: raw.isFinalExam,
      })
      .subscribe({
        next: (a) => {
          this.submitting.set(false);
          this.router.navigate(['/teacher/assessments', a.assessmentId]);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          if (err.status === 400 && err.error?.errors) {
            this.fieldErrors.set(err.error.errors);
            this.serverError.set('Please fix the highlighted fields.');
          } else {
            this.serverError.set(err.error?.message ?? err.statusText ?? 'Could not create.');
          }
        },
      });
  }

  serverFieldError(name: string): string | null {
    const errors = this.fieldErrors();
    const key = Object.keys(errors).find(k => k.toLowerCase() === name.toLowerCase());
    return key ? errors[key].join(' ') : null;
  }
}
