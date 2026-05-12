import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  CreateCourseBody,
  CreatedCourse,
  TeacherCourseListItem,
  TeacherDashboard,
} from './models/teacher.models';

@Injectable({ providedIn: 'root' })
export class TeacherService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/teacher`;

  getDashboard(): Observable<TeacherDashboard> {
    return this.http.get<TeacherDashboard>(`${this.baseUrl}/dashboard`);
  }

  getCourses(): Observable<TeacherCourseListItem[]> {
    return this.http.get<TeacherCourseListItem[]>(`${this.baseUrl}/courses`);
  }

  createCourse(body: CreateCourseBody): Observable<CreatedCourse> {
    return this.http.post<CreatedCourse>(`${this.baseUrl}/courses`, body);
  }
}
