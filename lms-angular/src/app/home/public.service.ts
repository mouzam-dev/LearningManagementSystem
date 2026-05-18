import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { PagedResult } from '../student/models/course.models';

export interface PublicCourseListItem {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  teacherName: string;
  enrolledCount: number;
  maxStudents?: number | null;
}

export interface PublicCoursesFilter {
  search?: string;
  category?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class PublicService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/public`;

  getCourses(filter: PublicCoursesFilter = {}): Observable<PagedResult<PublicCourseListItem>> {
    let params = new HttpParams();
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.category?.trim()) params = params.set('category', filter.category.trim());
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);

    return this.http.get<PagedResult<PublicCourseListItem>>(
      `${this.baseUrl}/courses`,
      { params },
    );
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }
}
