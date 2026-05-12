import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { TeacherService } from '../teacher.service';

/** Mirror of the FluentValidation rules on CreateCourseCommand. Kept here so
 *  the form can give field-level feedback before the request even goes out. */
interface CreateCourseFormControls {
  title: FormControl<string>;
  description: FormControl<string>;
  category: FormControl<string>;
  thumbnailUrl: FormControl<string>;
  maxStudents: FormControl<number | null>;
}

@Component({
  selector: 'app-teacher-create-course',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './create-course.html',
})
export class TeacherCreateCoursePage {
  private readonly teacher = inject(TeacherService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly submitting = signal(false);
  readonly serverError = signal<string | null>(null);
  /** Field-level errors returned by the API (when FluentValidation fails). */
  readonly fieldErrors = signal<Record<string, string[]>>({});

  readonly form: FormGroup<CreateCourseFormControls> = this.fb.nonNullable.group({
    title: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    description: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(4000)]),
    category: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(80)]),
    thumbnailUrl: this.fb.nonNullable.control('', [Validators.maxLength(500), this.optionalHttpUrl]),
    maxStudents: this.fb.control<number | null>(null, [Validators.min(1)]),
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
      .createCourse({
        title: raw.title.trim(),
        description: raw.description.trim(),
        category: raw.category.trim(),
        thumbnailUrl: raw.thumbnailUrl.trim() || null,
        maxStudents: raw.maxStudents,
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.router.navigate(['/teacher/courses']);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          if (err.status === 400 && err.error?.errors) {
            this.fieldErrors.set(err.error.errors);
            this.serverError.set('Please fix the highlighted fields.');
          } else if (err.status === 0) {
            this.serverError.set('Cannot reach the API.');
          } else {
            this.serverError.set(err.error?.message ?? err.statusText ?? 'Could not create the course.');
          }
        },
      });
  }

  /** Server-side error message keyed by property name (case-insensitive). */
  serverFieldError(name: string): string | null {
    const errors = this.fieldErrors();
    const key = Object.keys(errors).find(k => k.toLowerCase() === name.toLowerCase());
    return key ? errors[key].join(' ') : null;
  }

  /** Empty string is allowed; otherwise must be a valid http(s) URL. */
  private optionalHttpUrl(control: AbstractControl): ValidationErrors | null {
    const v = (control.value ?? '').toString().trim();
    if (!v) return null;
    try {
      const u = new URL(v);
      return u.protocol === 'http:' || u.protocol === 'https:' ? null : { url: true };
    } catch {
      return { url: true };
    }
  }
}
