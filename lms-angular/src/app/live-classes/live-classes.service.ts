import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  LiveJoinInfo,
  LiveSession,
  LiveSessionStatus,
  ScheduleLiveSessionBody,
} from './live-classes.models';

@Injectable({ providedIn: 'root' })
export class LiveClassesService {
  private readonly http = inject(HttpClient);
  private readonly teacherUrl = `${environment.apiUrl}/teacher/live-sessions`;
  private readonly studentUrl = `${environment.apiUrl}/student/live-sessions`;

  // ---- Teacher ----
  scheduleSession(body: ScheduleLiveSessionBody): Observable<LiveSession> {
    return this.http.post<LiveSession>(this.teacherUrl, body);
  }

  getCourseSessions(courseId: string): Observable<LiveSession[]> {
    return this.http.get<LiveSession[]>(`${this.teacherUrl}/courses/${courseId}`);
  }

  setStatus(liveSessionId: string, status: LiveSessionStatus): Observable<LiveSession> {
    return this.http.put<LiveSession>(`${this.teacherUrl}/${liveSessionId}/status`, { status });
  }

  // ---- Student ----
  getMySessions(): Observable<LiveSession[]> {
    return this.http.get<LiveSession[]>(this.studentUrl);
  }

  join(liveSessionId: string): Observable<LiveJoinInfo> {
    return this.http.post<LiveJoinInfo>(`${this.studentUrl}/${liveSessionId}/join`, {});
  }
}
