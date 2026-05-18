import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { PublicCourseListItem, PublicService } from './public.service';

/**
 * Deterministic coral-tinted gradient set used as a thumbnail fallback for
 * courses without an uploaded image. Full Tailwind utility strings so v4's
 * content scanner picks them up at build time.
 */
const CARD_GRADIENTS = [
  'from-orange-400 to-rose-500',
  'from-amber-400 to-orange-500',
  'from-rose-400 to-pink-500',
  'from-orange-500 to-red-500',
  'from-amber-500 to-orange-600',
  'from-pink-400 to-rose-500',
] as const;

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private readonly publicSvc = inject(PublicService);
  private readonly router = inject(Router);

  readonly searchControl = new FormControl<string>('', { nonNullable: true });

  readonly courses = signal<PublicCourseListItem[]>([]);
  readonly categories = signal<string[]>([]);
  readonly selectedCategory = signal<string>('');
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  /** Tracks the number of courses to show in the grid (top-N for the landing). */
  readonly previewSize = 8;

  ngOnInit(): void {
    this.fetchCategories();
    this.fetchCourses();
  }

  fetchCategories(): void {
    this.publicSvc.getCategories().subscribe({
      next: (cats) => this.categories.set(cats),
      error: () => this.categories.set([]),
    });
  }

  fetchCourses(): void {
    this.loading.set(true);
    this.error.set(null);

    this.publicSvc
      .getCourses({
        search: this.searchControl.value,
        category: this.selectedCategory(),
        page: 1,
        pageSize: this.previewSize,
      })
      .subscribe({
        next: (res) => {
          this.courses.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.error.set(this.formatError(err));
        },
      });
  }

  /** Submitting the hero search just refilters the preview grid below. */
  search(): void {
    this.fetchCourses();
    if (typeof document !== 'undefined') {
      document.getElementById('courses')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  selectCategory(cat: string): void {
    this.selectedCategory.set(this.selectedCategory() === cat ? '' : cat);
    this.fetchCourses();
  }

  /**
   * Anonymous visitors who tap a course CTA are sent to register with a
   * returnUrl back to the student catalog — they'll land on the real enroll
   * flow once signed up.
   */
  goEnroll(course: PublicCourseListItem): void {
    this.router.navigate(['/register'], {
      queryParams: { returnUrl: `/student/courses/${course.courseId}` },
    });
  }

  initial(name: string): string {
    return (name?.trim()?.charAt(0) || '?').toUpperCase();
  }

  gradientFor(id: string): string {
    let hash = 0;
    for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) | 0;
    return CARD_GRADIENTS[Math.abs(hash) % CARD_GRADIENTS.length];
  }

  private formatError(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return "We couldn't reach the courses service. Please check your connection and try again.";
    }
    return err.statusText || 'Could not load courses right now.';
  }
}
