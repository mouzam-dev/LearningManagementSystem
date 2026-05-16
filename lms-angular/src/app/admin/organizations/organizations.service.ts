import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  BranchListItem,
  CreateBranchRequest,
  CreateOrgAdminRequest,
  CreateOrgAdminResponse,
  CreateOrganizationRequest,
  OrganizationDetail,
  OrganizationsPage,
  UpdateBranchRequest,
  UpdateOrganizationRequest,
} from './models/organization.models';

@Injectable({ providedIn: 'root' })
export class OrganizationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/superadmin`;

  list(search?: string, page = 1, pageSize = 25): Observable<OrganizationsPage> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search?.trim()) params = params.set('search', search.trim());
    return this.http.get<OrganizationsPage>(`${this.baseUrl}/organizations`, { params });
  }

  get(organizationId: string): Observable<OrganizationDetail> {
    return this.http.get<OrganizationDetail>(`${this.baseUrl}/organizations/${organizationId}`);
  }

  create(request: CreateOrganizationRequest): Observable<OrganizationDetail> {
    return this.http.post<OrganizationDetail>(`${this.baseUrl}/organizations`, request);
  }

  update(organizationId: string, request: UpdateOrganizationRequest): Observable<OrganizationDetail> {
    return this.http.put<OrganizationDetail>(
      `${this.baseUrl}/organizations/${organizationId}`, request);
  }

  createBranch(organizationId: string, request: CreateBranchRequest): Observable<BranchListItem> {
    return this.http.post<BranchListItem>(
      `${this.baseUrl}/organizations/${organizationId}/branches`, request);
  }

  updateBranch(branchId: string, request: UpdateBranchRequest): Observable<BranchListItem> {
    return this.http.put<BranchListItem>(`${this.baseUrl}/branches/${branchId}`, request);
  }

  createOrgAdmin(
    organizationId: string,
    request: CreateOrgAdminRequest,
  ): Observable<CreateOrgAdminResponse> {
    return this.http.post<CreateOrgAdminResponse>(
      `${this.baseUrl}/organizations/${organizationId}/admins`, request);
  }
}
