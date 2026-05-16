export interface OrganizationListItem {
  id: string;
  name: string;
  slug?: string | null;
  contactEmail?: string | null;
  isActive: boolean;
  branchCount: number;
  userCount: number;
  orgAdminCount: number;
  teacherCount: number;
  studentCount: number;
  createdAt: string;
}

export interface OrganizationsPage {
  items: OrganizationListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface BranchListItem {
  id: string;
  organizationId: string;
  name: string;
  code?: string | null;
  location?: string | null;
  contactEmail?: string | null;
  isActive: boolean;
  teacherCount: number;
  studentCount: number;
  createdAt: string;
}

export interface OrgAdminListItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
}

export interface OrganizationDetail {
  id: string;
  name: string;
  slug?: string | null;
  description?: string | null;
  contactEmail?: string | null;
  logoUrl?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  branchCount: number;
  orgAdminCount: number;
  teacherCount: number;
  studentCount: number;
  branches: BranchListItem[];
  orgAdmins: OrgAdminListItem[];
}

export interface CreateOrganizationRequest {
  name: string;
  slug?: string | null;
  description?: string | null;
  contactEmail?: string | null;
  logoUrl?: string | null;
}

export interface UpdateOrganizationRequest extends CreateOrganizationRequest {
  isActive: boolean;
}

export interface CreateBranchRequest {
  name: string;
  code?: string | null;
  location?: string | null;
  contactEmail?: string | null;
}

export interface UpdateBranchRequest extends CreateBranchRequest {
  isActive: boolean;
}

export interface CreateOrgAdminRequest {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
}

export interface CreateOrgAdminResponse {
  admin: OrgAdminListItem;
}
