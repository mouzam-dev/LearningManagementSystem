import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { TeacherCourseListItem } from '../models/teacher.models';
import { TeacherService } from '../teacher.service';

@Component({
  selector: 'app-teacher-courses',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './teacher-courses.html',
})
export class TeacherCoursesPage {
  private readonly teacher = inject(TeacherService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly courses = signal<TeacherCourseListItem[]>([]);

  constructor() {
    this.fetch();
  }

  private fetch(): void {
    this.loading.set(true);
    this.error.set(null);

    this.teacher.getCourses().subscribe({
      next: (list) => {
        this.courses.set(list);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(err.status === 0
          ? 'Cannot reach the API. Is it running on http://localhost:5116?'
          : (err.error?.message ?? err.statusText ?? 'Something went wrong.'));
      },
    });
  }
}
