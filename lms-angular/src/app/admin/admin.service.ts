import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  AdminDashboard,
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

  changeUserRole(userId: string, role: string): Observable<AdminUserDetail> {
    return this.http.patch<AdminUserDetail>(
      `${this.baseUrl}/users/${userId}/role`, { role });
  }
}
