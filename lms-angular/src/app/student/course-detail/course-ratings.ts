import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { StarRating } from '../../shared/ratings/star-rating';
import { StudentService } from '../student.service';
import {
  CourseRating,
  RatingSummary,
  TeacherRating,
} from '../models/rating.models';

type Mode = 'course' | 'teacher';

/**
 * Ratings panel for a course-detail page. Renders three things in one card:
 *
 *  1. Aggregate (average, count, 1-5 histogram) for either the course or the
 *     teacher — controlled by `mode`.
 *  2. The caller's own editable rating (when mode === 'course'; the API
 *     surfaces "MyRating" only for the course query, intentionally — teachers
 *     can be rated per-course so we always treat the form as a fresh submit).
 *  3. Paginated list of recent reviews.
 *
 * Eligibility (enrollment) is enforced server-side; if the user isn't enrolled
 * the POST returns 403 and we surface the message.
 */
@Component({
  selector: 'app-course-ratings',
  standalone: true,
  imports: [CommonModule, FormsModule, StarRating],
  templateUrl: './course-ratings.html',
})
export class CourseRatings {
  private readonly studentService = inject(StudentService);

  readonly mode = input.required<Mode>();
  readonly courseId = input.required<string>();
  /** Required when mode === 'teacher'. */
  readonly teacherId = input<string | null>(null);
  /** Whether the caller is enrolled — controls visibility of the editor. */
  readonly canRate = input<boolean>(false);

  readonly loading = signal(true);
  readonly summary = signal<RatingSummary | null>(null);
  readonly items = signal<(CourseRating | TeacherRating)[]>([]);
  readonly myRating = signal<CourseRating | null>(null);

  // Editor state
  readonly editorOpen = signal(false);
  readonly editorRating = signal(0);
  readonly editorReview = signal('');
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly averageLabel = computed(() => {
    const s = this.summary();
    return s && s.totalCount > 0 ? s.average.toFixed(1) : '—';
  });

  readonly histogramRows = computed(() => {
    const s = this.summary();
    if (!s || s.totalCount === 0) return [];
    return [5, 4, 3, 2, 1].map((star) => {
      const count =
        star === 5 ? s.fiveStar
          : star === 4 ? s.fourStar
          : star === 3 ? s.threeStar
          : star === 2 ? s.twoStar
          : s.oneStar;
      return {
        star,
        count,
        percent: Math.round((count / s.totalCount) * 100),
      };
    });
  });

  ngOnInit(): void {
    this.fetch();
  }

  fetch(): void {
    this.loading.set(true);
    if (this.mode() === 'course') {
      this.studentService.getCourseRatings(this.courseId()).subscribe({
        next: (page) => {
          this.summary.set(page.summary);
          this.items.set(page.items);
          this.myRating.set(page.myRating);
          if (page.myRating) {
            this.editorRating.set(page.myRating.rating);
            this.editorReview.set(page.myRating.review);
          }
          this.loading.set(false);
        },
        error: () => {
          this.summary.set(null);
          this.items.set([]);
          this.loading.set(false);
        },
      });
    } else {
      const tId = this.teacherId();
      if (!tId) {
        this.loading.set(false);
        return;
      }
      this.studentService.getTeacherRatings(tId).subscribe({
        next: (page) => {
          this.summary.set(page.summary);
          this.items.set(page.items);
          this.loading.set(false);
        },
        error: () => {
          this.summary.set(null);
          this.items.set([]);
          this.loading.set(false);
        },
      });
    }
  }

  openEditor(): void {
    if (!this.canRate()) return;
    this.editorOpen.set(true);
    this.submitError.set(null);
  }

  closeEditor(): void {
    this.editorOpen.set(false);
    this.submitError.set(null);
    // Reset to last-saved values
    const mine = this.myRating();
    this.editorRating.set(mine?.rating ?? 0);
    this.editorReview.set(mine?.review ?? '');
  }

  submit(): void {
    if (this.editorRating() < 1) {
      this.submitError.set('Pick at least one star.');
      return;
    }
    this.submitting.set(true);
    this.submitError.set(null);

    const body = { rating: this.editorRating(), review: this.editorReview().trim() };

    if (this.mode() === 'course') {
      this.studentService.submitCourseRating(this.courseId(), body).subscribe({
        next: () => {
          this.submitting.set(false);
          this.editorOpen.set(false);
          this.fetch();
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          this.submitError.set(this.formatError(err));
        },
      });
    } else {
      const tId = this.teacherId();
      if (!tId) {
        this.submitting.set(false);
        return;
      }
      this.studentService
        .submitTeacherRating(tId, { ...body, courseId: this.courseId() })
        .subscribe({
          next: () => {
            this.submitting.set(false);
            this.editorOpen.set(false);
            this.fetch();
          },
          error: (err: HttpErrorResponse) => {
            this.submitting.set(false);
            this.submitError.set(this.formatError(err));
          },
        });
    }
  }

  /** Lazy: handles either rating shape. Both have studentName/rating/review. */
  asReview(r: CourseRating | TeacherRating): CourseRating | TeacherRating {
    return r;
  }

  isTeacherReview(r: CourseRating | TeacherRating): r is TeacherRating {
    return (r as TeacherRating).courseTitle !== undefined;
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 403) return "You need to be enrolled in this course to leave a rating.";
    if (err.status === 409) return (err.error as { message?: string })?.message ?? 'Conflict.';
    const body = err.error as { message?: string } | null;
    return body?.message ?? err.statusText ?? 'Failed to submit your rating.';
  }
}
