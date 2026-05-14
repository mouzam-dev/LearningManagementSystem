import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  CreateAssessmentBody,
  CreateCourseBody,
  CreateLessonBody,
  CreateModuleBody,
  CreateQuestionBody,
  CreatedCourse,
  GradeSubmissionBody,
  GradingQueueItem,
  TeacherAssessment,
  TeacherCourseDetail,
  TeacherCourseListItem,
  TeacherDashboard,
  TeacherLesson,
  TeacherModule,
  TeacherQuestion,
  TeacherSubmissionDetail,
  UpdateAssessmentBody,
  UpdateCourseBody,
  UpdateLessonBody,
  UpdateModuleBody,
  UpdateQuestionBody,
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

  // ---- Assessments ------------------------------------------------------

  getAssessment(assessmentId: string): Observable<TeacherAssessment> {
    return this.http.get<TeacherAssessment>(`${this.baseUrl}/assessments/${assessmentId}`);
  }

  createAssessment(courseId: string, body: CreateAssessmentBody): Observable<TeacherAssessment> {
    return this.http.post<TeacherAssessment>(
      `${this.baseUrl}/courses/${courseId}/assessments`, body);
  }

  updateAssessment(assessmentId: string, body: UpdateAssessmentBody): Observable<TeacherAssessment> {
    return this.http.put<TeacherAssessment>(`${this.baseUrl}/assessments/${assessmentId}`, body);
  }

  deleteAssessment(assessmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/assessments/${assessmentId}`);
  }

  // ---- Questions --------------------------------------------------------

  createQuestion(assessmentId: string, body: CreateQuestionBody): Observable<TeacherQuestion> {
    return this.http.post<TeacherQuestion>(
      `${this.baseUrl}/assessments/${assessmentId}/questions`, body);
  }

  updateQuestion(questionId: string, body: UpdateQuestionBody): Observable<TeacherQuestion> {
    return this.http.put<TeacherQuestion>(`${this.baseUrl}/questions/${questionId}`, body);
  }

  deleteQuestion(questionId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/questions/${questionId}`);
  }

  // ---- Grading ----------------------------------------------------------

  getGradingQueue(includeGraded = false): Observable<GradingQueueItem[]> {
    const params = includeGraded ? '?includeGraded=true' : '';
    return this.http.get<GradingQueueItem[]>(`${this.baseUrl}/grading/queue${params}`);
  }

  getSubmission(submissionId: string): Observable<TeacherSubmissionDetail> {
    return this.http.get<TeacherSubmissionDetail>(`${this.baseUrl}/submissions/${submissionId}`);
  }

  gradeSubmission(submissionId: string, body: GradeSubmissionBody): Observable<TeacherSubmissionDetail> {
    return this.http.put<TeacherSubmissionDetail>(
      `${this.baseUrl}/submissions/${submissionId}/grade`, body);
  }
}
