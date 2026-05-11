import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CourseFilter, CourseListItem, PagedResult } from './models/course.models';
import { Dashboard } from './models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class StudentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/student`;

  getDashboard(): Observable<Dashboard> {
    return this.http.get<Dashboard>(`${this.baseUrl}/dashboard`);
  }

  getCourses(filter: CourseFilter = {}): Observable<PagedResult<CourseListItem>> {
    let params = new HttpParams();
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.category?.trim()) params = params.set('category', filter.category.trim());
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);

    return this.http.get<PagedResult<CourseListItem>>(`${this.baseUrl}/courses`, { params });
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }

  enroll(courseId: string): Observable<CourseListItem> {
    return this.http.post<CourseListItem>(`${this.baseUrl}/enroll/${courseId}`, {});
  }
}
