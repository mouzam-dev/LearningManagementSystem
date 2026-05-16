import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  OrgAdminDashboard,
  OrgBranch,
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
}
