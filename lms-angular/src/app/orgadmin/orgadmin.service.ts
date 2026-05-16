import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  OrgAdminDashboard,
  OrgBranch,
  OrgCourseDetail,
  OrgCourseFilter,
  OrgCoursesPage,
  OrgCreateBranchRequest,
  OrgTeacher,
  OrgUpdateBranchRequest,
} from './models/orgadmin.models';

@Injectable({ providedIn: 'root' })
export class OrgAdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/orgadmin`;

  getDashboard(): Observable<OrgAdminDashboard> {
    return this.http.get<OrgAdminDashboard>(`${this.baseUrl}/dashboard`);
  }

  listBranches(): Observable<OrgBranch[]> {
    return this.http.get<OrgBranch[]>(`${this.baseUrl}/branches`);
  }

  createBranch(req: OrgCreateBranchRequest): Observable<OrgBranch> {
    return this.http.post<OrgBranch>(`${this.baseUrl}/branches`, req);
  }

  updateBranch(branchId: string, req: OrgUpdateBranchRequest): Observable<OrgBranch> {
    return this.http.put<OrgBranch>(`${this.baseUrl}/branches/${branchId}`, req);
  }

  listTeachers(search?: string, branchId?: string | null): Observable<OrgTeacher[]> {
    let params = new HttpParams();
    if (search?.trim()) params = params.set('search', search.trim());
    if (branchId) params = params.set('branchId', branchId);
    return this.http.get<OrgTeacher[]>(`${this.baseUrl}/teachers`, { params });
  }

  assignTeacherToBranch(teacherId: string, branchId: string): Observable<OrgTeacher> {
    return this.http.patch<OrgTeacher>(
      `${this.baseUrl}/teachers/${teacherId}/branch`,
      { branchId },
    );
  }

  createTeacher(req: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    branchId: string;
  }): Observable<OrgTeacher> {
    return this.http.post<OrgTeacher>(`${this.baseUrl}/teachers`, req);
  }

  // ---- Course moderation -----------------------------------------------

  listCourses(filter: OrgCourseFilter = {}): Observable<OrgCoursesPage> {
    let params = new HttpParams();
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.category?.trim()) params = params.set('category', filter.category.trim());
    if (filter.isPublished !== null && filter.isPublished !== undefined) {
      params = params.set('isPublished', filter.isPublished);
    }
    if (filter.branchId) params = params.set('branchId', filter.branchId);
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);
    return this.http.get<OrgCoursesPage>(`${this.baseUrl}/courses`, { params });
  }

  getCourse(courseId: string): Observable<OrgCourseDetail> {
    return this.http.get<OrgCourseDetail>(`${this.baseUrl}/courses/${courseId}`);
  }

  setCoursePublished(courseId: string, isPublished: boolean): Observable<OrgCourseDetail> {
    return this.http.patch<OrgCourseDetail>(
      `${this.baseUrl}/courses/${courseId}/published`, { isPublished });
  }

  deleteCourse(courseId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/courses/${courseId}`);
  }
}
