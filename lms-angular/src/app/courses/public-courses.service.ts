import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  PagedResult,
  PublicCourseFilter,
  PublicCourseListItem,
  PublicTeacher,
} from './public-course.models';

/** Anonymous course-catalog reads — no auth, served by the API's /public endpoints. */
@Injectable({ providedIn: 'root' })
export class PublicCoursesService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/public`;

  getCourses(filter: PublicCourseFilter): Observable<PagedResult<PublicCourseListItem>> {
    let params = new HttpParams()
      .set('page', String(filter.page ?? 1))
      .set('pageSize', String(filter.pageSize ?? 12));
    if (filter.search) params = params.set('search', filter.search);
    if (filter.category) params = params.set('category', filter.category);
    if (filter.teacherId) params = params.set('teacherId', filter.teacherId);
    return this.http.get<PagedResult<PublicCourseListItem>>(`${this.base}/courses`, { params });
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/categories`);
  }

  getTeachers(): Observable<PublicTeacher[]> {
    return this.http.get<PublicTeacher[]>(`${this.base}/teachers`);
  }
}
