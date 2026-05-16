import { BranchListItem } from '../../admin/organizations/models/organization.models';

export interface OrgAdminBranchSummary {
  id: string;
  name: string;
  code?: string | null;
  isActive: boolean;
  teacherCount: number;
  studentCount: number;
}

export interface OrgAdminDashboard {
  organizationId: string;
  organizationName: string;
  organizationSlug?: string | null;
  organizationDescription?: string | null;
  branchCount: number;
  teacherCount: number;
  studentCount: number;
  courseCount: number;
  publishedCourseCount: number;
  branches: OrgAdminBranchSummary[];
}

export interface OrgTeacher {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  branchId?: string | null;
  branchName?: string | null;
  courseCount: number;
  publishedCourseCount: number;
  createdAt: string;
}

export type OrgBranch = BranchListItem;

export interface OrgCreateBranchRequest {
  name: string;
  code?: string | null;
  location?: string | null;
  contactEmail?: string | null;
}

export interface OrgUpdateBranchRequest extends OrgCreateBranchRequest {
  isActive: boolean;
}
