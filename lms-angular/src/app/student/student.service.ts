import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CourseFilter, CourseListItem, PagedResult } from './models/course.models';
import { Dashboard } from './models/dashboard.models';
import { CourseDetail, LessonDetail, LessonProgressUpdate } from './models/lesson.models';
import {
  AssessmentDetail,
  AssessmentListItem,
  SubmissionResult,
  SubmitAssessmentBody,
} from './models/assessment.models';
import {
  Certificate,
  CertificateSummary,
  VerifyCertificate,
} from './models/certificate.models';

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

  getCourseDetail(courseId: string): Observable<CourseDetail> {
    return this.http.get<CourseDetail>(`${this.baseUrl}/courses/${courseId}`);
  }

  getLesson(lessonId: string): Observable<LessonDetail> {
    return this.http.get<LessonDetail>(`${this.baseUrl}/lessons/${lessonId}`);
  }

  updateLessonProgress(
    lessonId: string,
    update: LessonProgressUpdate,
  ): Observable<LessonDetail> {
    return this.http.put<LessonDetail>(
      `${this.baseUrl}/lessons/${lessonId}/progress`,
      update,
    );
  }

  getCourseAssessments(courseId: string): Observable<AssessmentListItem[]> {
    return this.http.get<AssessmentListItem[]>(
      `${this.baseUrl}/courses/${courseId}/assessments`,
    );
  }

  getAssessment(assessmentId: string): Observable<AssessmentDetail> {
    return this.http.get<AssessmentDetail>(`${this.baseUrl}/assessments/${assessmentId}`);
  }

  submitAssessment(
    assessmentId: string,
    body: SubmitAssessmentBody,
  ): Observable<SubmissionResult> {
    return this.http.post<SubmissionResult>(
      `${this.baseUrl}/assessments/${assessmentId}/submit`,
      body,
    );
  }

  getMyCertificates(): Observable<CertificateSummary[]> {
    return this.http.get<CertificateSummary[]>(`${this.baseUrl}/certificates`);
  }

  getCertificate(certificateId: string): Observable<Certificate> {
    return this.http.get<Certificate>(`${this.baseUrl}/certificates/${certificateId}`);
  }

  /**
   * Public verification — calls the anonymous /api/certificates/verify endpoint
   * (not under /api/student). Bypasses the JWT interceptor.
   */
  verifyCertificate(code: string): Observable<VerifyCertificate> {
    return this.http.get<VerifyCertificate>(
      `${environment.apiUrl}/certificates/verify/${encodeURIComponent(code)}`,
    );
  }

  /** Recent announcements from enrolled courses — powers the dashboard widget. */
  getAnnouncements(limit = 5): Observable<StudentAnnouncement[]> {
    return this.http.get<StudentAnnouncement[]>(
      `${this.baseUrl}/announcements?limit=${limit}`);
  }
}

export interface StudentAnnouncement {
  id: string;
  courseId: string;
  courseTitle: string;
  authorId: string;
  authorName: string;
  title: string;
  body: string;
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
}
