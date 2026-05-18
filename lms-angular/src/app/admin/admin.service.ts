import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  AdminAuditLogPage,
  AdminCourseDetail,
  AdminCourseFilter,
  AdminCoursesPage,
  AdminDashboard,
  AdminReport,
  AdminUserDetail,
  AdminUserFilter,
  AdminUsersPage,
} from './models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin`;

  getDashboard(): Observable<AdminDashboard> {
    return this.http.get<AdminDashboard>(`${this.baseUrl}/dashboard`);
  }

  // ---- Users ------------------------------------------------------------

  getUsers(filter: AdminUserFilter = {}): Observable<AdminUsersPage> {
    let params = new HttpParams();
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.role?.trim()) params = params.set('role', filter.role.trim());
    if (filter.isActive !== null && filter.isActive !== undefined) {
      params = params.set('isActive', filter.isActive);
    }
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);

    return this.http.get<AdminUsersPage>(`${this.baseUrl}/users`, { params });
  }

  getUser(userId: string): Observable<AdminUserDetail> {
    return this.http.get<AdminUserDetail>(`${this.baseUrl}/users/${userId}`);
  }

  setUserActive(userId: string, isActive: boolean): Observable<AdminUserDetail> {
    return this.http.patch<AdminUserDetail>(
      `${this.baseUrl}/users/${userId}/active`, { isActive });
  }

  changeUserRole(
    userId: string,
    role: string,
    opts?: { organizationId?: string | null; branchId?: string | null },
  ): Observable<AdminUserDetail> {
    return this.http.patch<AdminUserDetail>(
      `${this.baseUrl}/users/${userId}/role`,
      { role, organizationId: opts?.organizationId ?? null, branchId: opts?.branchId ?? null },
    );
  }

  /**
   * SuperAdmin-only: move a Teacher or Student to a different organization
   * and branch in one shot. Hits the dedicated transfer endpoint so it can
   * be audited separately from a role change.
   */
  transferUser(
    userId: string,
    organizationId: string,
    branchId: string,
  ): Observable<AdminUserDetail> {
    return this.http.patch<AdminUserDetail>(
      `${environment.apiUrl}/superadmin/users/${userId}/transfer`,
      { organizationId, branchId },
    );
  }

  // ---- Course moderation -----------------------------------------------

  getCourses(filter: AdminCourseFilter = {}): Observable<AdminCoursesPage> {
    let params = new HttpParams();
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.category?.trim()) params = params.set('category', filter.category.trim());
    if (filter.isPublished !== null && filter.isPublished !== undefined) {
      params = params.set('isPublished', filter.isPublished);
    }
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);

    return this.http.get<AdminCoursesPage>(`${this.baseUrl}/courses`, { params });
  }

  getCourse(courseId: string): Observable<AdminCourseDetail> {
    return this.http.get<AdminCourseDetail>(`${this.baseUrl}/courses/${courseId}`);
  }

  setCoursePublished(courseId: string, isPublished: boolean): Observable<AdminCourseDetail> {
    return this.http.patch<AdminCourseDetail>(
      `${this.baseUrl}/courses/${courseId}/published`, { isPublished });
  }

  deleteCourse(courseId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/courses/${courseId}`);
  }

  // ---- Audit log --------------------------------------------------------

  getAuditLog(entity?: string, page = 1, pageSize = 30): Observable<AdminAuditLogPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (entity?.trim()) params = params.set('entity', entity.trim());
    return this.http.get<AdminAuditLogPage>(`${this.baseUrl}/audit`, { params });
  }

  // ---- Reporting + CSV exports -----------------------------------------

  getReport(): Observable<AdminReport> {
    return this.http.get<AdminReport>(`${this.baseUrl}/reports`);
  }

  /** Triggers a browser download of the given CSV export. */
  downloadCsv(kind: 'users' | 'courses'): void {
    this.http
      .get(`${this.baseUrl}/reports/${kind}.csv`, { responseType: 'blob' })
      .subscribe((blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `lms-${kind}.csv`;
        a.click();
        URL.revokeObjectURL(url);
      });
  }

  // ---- Bulk import (BRD ADM-012) ---------------------------------------

  /** Multipart upload of a CSV file; server returns the per-row import result. */
  bulkImportUsers(file: File): Observable<BulkImportUsersResult> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<BulkImportUsersResult>(
      `${this.baseUrl}/users/bulk-import`, form);
  }
}

export interface BulkImportRowResult {
  line: number;
  email: string;
  status: 'Created' | 'Failed';
  userId?: string | null;
  error?: string | null;
}

export interface BulkImportUsersResult {
  totalRows: number;
  successCount: number;
  failureCount: number;
  rows: BulkImportRowResult[];
}
