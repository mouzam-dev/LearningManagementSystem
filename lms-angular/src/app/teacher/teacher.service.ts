import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { PagedResult } from '../student/models/course.models';
import {
  BankQuestion,
  BankQuestionFilter,
  BankQuestionListItem,
  BankQuestionUpsertBody,
} from './models/bank-question.models';
import {
  Rubric,
  RubricFilter,
  RubricListItem,
  RubricUpsertBody,
} from './models/rubric.models';
import {
  CourseAnalytics,
  CourseInstructor,
  CourseStudentDetail,
  CourseStudentListItem,
  CreateAssessmentBody,
  CreateCourseBody,
  CreateLessonBody,
  CreateModuleBody,
  CreateQuestionBody,
  CreatedCourse,
  EligibleCoInstructor,
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

  getCourses(includeArchived = false): Observable<TeacherCourseListItem[]> {
    let params = new HttpParams();
    if (includeArchived) params = params.set('includeArchived', 'true');
    return this.http.get<TeacherCourseListItem[]>(`${this.baseUrl}/courses`, { params });
  }

  createCourse(body: CreateCourseBody): Observable<CreatedCourse> {
    return this.http.post<CreatedCourse>(`${this.baseUrl}/courses`, body);
  }

  setCourseArchived(courseId: string, isArchived: boolean): Observable<TeacherCourseDetail> {
    return this.http.patch<TeacherCourseDetail>(
      `${this.baseUrl}/courses/${courseId}/archived`,
      { isArchived },
    );
  }

  duplicateCourse(courseId: string, newTitle?: string | null): Observable<CreatedCourse> {
    return this.http.post<CreatedCourse>(
      `${this.baseUrl}/courses/${courseId}/duplicate`,
      { newTitle: newTitle ?? null },
    );
  }

  // ---- Co-instructors --------------------------------------------------

  listCourseInstructors(courseId: string): Observable<CourseInstructor[]> {
    return this.http.get<CourseInstructor[]>(
      `${this.baseUrl}/courses/${courseId}/instructors`);
  }

  listEligibleCoInstructors(courseId: string, search?: string): Observable<EligibleCoInstructor[]> {
    let params = new HttpParams();
    if (search?.trim()) params = params.set('search', search.trim());
    return this.http.get<EligibleCoInstructor[]>(
      `${this.baseUrl}/courses/${courseId}/instructors/eligible`, { params });
  }

  addCoInstructor(courseId: string, userId: string): Observable<CourseInstructor[]> {
    return this.http.post<CourseInstructor[]>(
      `${this.baseUrl}/courses/${courseId}/instructors`, { userId });
  }

  removeCoInstructor(courseId: string, userId: string): Observable<CourseInstructor[]> {
    return this.http.delete<CourseInstructor[]>(
      `${this.baseUrl}/courses/${courseId}/instructors/${userId}`);
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

  /**
   * Uploads a document file for a Document-type lesson. Returns the absolute
   * URL it's served from + the original filename.
   */
  uploadDocument(file: File): Observable<{ url: string; fileName: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ url: string; fileName: string }>(
      `${this.baseUrl}/uploads/document`,
      form,
    );
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

  // ---- Per-course students + analytics ---------------------------------

  getCourseStudents(courseId: string): Observable<CourseStudentListItem[]> {
    return this.http.get<CourseStudentListItem[]>(`${this.baseUrl}/courses/${courseId}/students`);
  }

  getCourseStudent(courseId: string, studentId: string): Observable<CourseStudentDetail> {
    return this.http.get<CourseStudentDetail>(
      `${this.baseUrl}/courses/${courseId}/students/${studentId}`);
  }

  getCourseAnalytics(courseId: string): Observable<CourseAnalytics> {
    return this.http.get<CourseAnalytics>(`${this.baseUrl}/courses/${courseId}/analytics`);
  }

  // ---- Announcements ---------------------------------------------------

  getCourseAnnouncements(courseId: string): Observable<TeacherAnnouncement[]> {
    return this.http.get<TeacherAnnouncement[]>(
      `${this.baseUrl}/courses/${courseId}/announcements`);
  }

  createCourseAnnouncement(
    courseId: string,
    body: { title: string; body: string; isPinned: boolean },
  ): Observable<TeacherAnnouncement> {
    return this.http.post<TeacherAnnouncement>(
      `${this.baseUrl}/courses/${courseId}/announcements`, body);
  }

  // ---------------- Question bank (BRD TCH-021) ----------------------------

  getBankQuestions(filter: BankQuestionFilter = {}): Observable<PagedResult<BankQuestionListItem>> {
    let params = new HttpParams();
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.tag?.trim()) params = params.set('tag', filter.tag.trim());
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);
    return this.http.get<PagedResult<BankQuestionListItem>>(
      `${this.baseUrl}/question-bank`, { params });
  }

  getBankQuestionTags(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/question-bank/tags`);
  }

  getBankQuestion(id: string): Observable<BankQuestion> {
    return this.http.get<BankQuestion>(`${this.baseUrl}/question-bank/${id}`);
  }

  createBankQuestion(body: BankQuestionUpsertBody): Observable<BankQuestion> {
    return this.http.post<BankQuestion>(`${this.baseUrl}/question-bank`, body);
  }

  updateBankQuestion(id: string, body: BankQuestionUpsertBody): Observable<BankQuestion> {
    return this.http.put<BankQuestion>(`${this.baseUrl}/question-bank/${id}`, body);
  }

  deleteBankQuestion(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/question-bank/${id}`);
  }

  /** Copy the picked bank questions into an assessment; returns the refreshed assessment. */
  importBankQuestionsToAssessment(
    assessmentId: string,
    questionIds: string[],
  ): Observable<TeacherAssessment> {
    return this.http.post<TeacherAssessment>(
      `${this.baseUrl}/assessments/${assessmentId}/import-bank-questions`,
      { questionIds });
  }

  // ---------------- Rubrics (BRD TCH-025) ----------------------------------

  getRubrics(filter: RubricFilter = {}): Observable<PagedResult<RubricListItem>> {
    let params = new HttpParams();
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);
    return this.http.get<PagedResult<RubricListItem>>(`${this.baseUrl}/rubrics`, { params });
  }

  getRubric(id: string): Observable<Rubric> {
    return this.http.get<Rubric>(`${this.baseUrl}/rubrics/${id}`);
  }

  createRubric(body: RubricUpsertBody): Observable<Rubric> {
    return this.http.post<Rubric>(`${this.baseUrl}/rubrics`, body);
  }

  updateRubric(id: string, body: RubricUpsertBody): Observable<Rubric> {
    return this.http.put<Rubric>(`${this.baseUrl}/rubrics/${id}`, body);
  }

  deleteRubric(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/rubrics/${id}`);
  }
}

export interface TeacherAnnouncement {
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
