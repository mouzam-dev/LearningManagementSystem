import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  CreateCourseBody,
  CreateLessonBody,
  CreateModuleBody,
  CreatedCourse,
  TeacherCourseDetail,
  TeacherCourseListItem,
  TeacherDashboard,
  TeacherLesson,
  TeacherModule,
  UpdateCourseBody,
  UpdateLessonBody,
  UpdateModuleBody,
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

  // ---- Course builder ---------------------------------------------------

  getCourseDetail(courseId: string): Observable<TeacherCourseDetail> {
    return this.http.get<TeacherCourseDetail>(`${this.baseUrl}/courses/${courseId}`);
  }

  updateCourse(courseId: string, body: UpdateCourseBody): Observable<TeacherCourseDetail> {
    return this.http.put<TeacherCourseDetail>(`${this.baseUrl}/courses/${courseId}`, body);
  }

  setCoursePublished(courseId: string, isPublished: boolean): Observable<TeacherCourseDetail> {
    return this.http.patch<TeacherCourseDetail>(
      `${this.baseUrl}/courses/${courseId}/published`,
      { isPublished },
    );
  }

  // ---- Modules ----------------------------------------------------------

  createModule(courseId: string, body: CreateModuleBody): Observable<TeacherModule> {
    return this.http.post<TeacherModule>(`${this.baseUrl}/courses/${courseId}/modules`, body);
  }

  updateModule(moduleId: string, body: UpdateModuleBody): Observable<TeacherModule> {
    return this.http.put<TeacherModule>(`${this.baseUrl}/modules/${moduleId}`, body);
  }

  deleteModule(moduleId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/modules/${moduleId}`);
  }

  // ---- Lessons ----------------------------------------------------------

  createLesson(moduleId: string, body: CreateLessonBody): Observable<TeacherLesson> {
    return this.http.post<TeacherLesson>(`${this.baseUrl}/modules/${moduleId}/lessons`, body);
  }

  updateLesson(lessonId: string, body: UpdateLessonBody): Observable<TeacherLesson> {
    return this.http.put<TeacherLesson>(`${this.baseUrl}/lessons/${lessonId}`, body);
  }

  deleteLesson(lessonId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/lessons/${lessonId}`);
  }
}
