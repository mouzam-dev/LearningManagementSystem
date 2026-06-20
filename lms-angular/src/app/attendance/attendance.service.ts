import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  AttendanceSession,
  BranchAttendanceDetail,
  CourseAttendanceSummary,
  CreateSessionBody,
  MarkInput,
  MyAttendance,
  OrgAttendanceOverview,
  SessionRoster,
  SessionStatus,
} from './attendance.models';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly http = inject(HttpClient);
  private readonly teacherUrl = `${environment.apiUrl}/teacher/attendance`;
  private readonly orgUrl = `${environment.apiUrl}/orgadmin/attendance`;
  private readonly studentUrl = `${environment.apiUrl}/student/attendance`;

  // ---- Teacher ----
  createSession(body: CreateSessionBody): Observable<SessionRoster> {
    return this.http.post<SessionRoster>(`${this.teacherUrl}/sessions`, body);
  }

  getCourseSessions(courseId: string): Observable<AttendanceSession[]> {
    return this.http.get<AttendanceSession[]>(`${this.teacherUrl}/courses/${courseId}/sessions`);
  }

  getSessionRoster(sessionId: string): Observable<SessionRoster> {
    return this.http.get<SessionRoster>(`${this.teacherUrl}/sessions/${sessionId}`);
  }

  markAttendance(sessionId: string, marks: MarkInput[]): Observable<SessionRoster> {
    return this.http.post<SessionRoster>(`${this.teacherUrl}/sessions/${sessionId}/marks`, { marks });
  }

  setSessionStatus(sessionId: string, status: SessionStatus): Observable<AttendanceSession> {
    return this.http.put<AttendanceSession>(`${this.teacherUrl}/sessions/${sessionId}/status`, { status });
  }

  getCourseSummary(courseId: string): Observable<CourseAttendanceSummary> {
    return this.http.get<CourseAttendanceSummary>(`${this.teacherUrl}/courses/${courseId}/summary`);
  }

  // ---- Org admin ----
  getOrgOverview(fromDate?: string, toDate?: string): Observable<OrgAttendanceOverview> {
    return this.http.get<OrgAttendanceOverview>(`${this.orgUrl}/overview`, {
      params: this.range(fromDate, toDate),
    });
  }

  getBranchDetail(branchId: string, fromDate?: string, toDate?: string): Observable<BranchAttendanceDetail> {
    return this.http.get<BranchAttendanceDetail>(`${this.orgUrl}/branches/${branchId}/detail`, {
      params: this.range(fromDate, toDate),
    });
  }

  exportOverviewCsv(fromDate?: string, toDate?: string): Observable<Blob> {
    return this.http.get(`${this.orgUrl}/overview/export`, {
      params: this.range(fromDate, toDate),
      responseType: 'blob',
    });
  }

  // ---- Student ----
  getMyAttendance(courseId?: string, fromDate?: string, toDate?: string): Observable<MyAttendance> {
    let params = this.range(fromDate, toDate);
    if (courseId) params = params.set('courseId', courseId);
    return this.http.get<MyAttendance>(this.studentUrl, { params });
  }

  private range(fromDate?: string, toDate?: string): HttpParams {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return params;
  }
}
