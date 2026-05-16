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

// ---------------- Course moderation -------------------------------------

export interface OrgCourseListItem {
  courseId: string;
  title: string;
  category: string;
  isPublished: boolean;
  teacherId: string;
  teacherName: string;
  teacherEmail: string;
  branchId?: string | null;
  branchName?: string | null;
  moduleCount: number;
  lessonCount: number;
  studentCount: number;
  assessmentCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface OrgCoursesPage {
  items: OrgCourseListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface OrgCourseDetail {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string;
  teacherId: string;
  teacherName: string;
  teacherEmail: string;
  branchId?: string | null;
  branchName?: string | null;
  moduleCount: number;
  lessonCount: number;
  assessmentCount: number;
  studentCount: number;
  completedCount: number;
  averageProgress: number;
}

export interface OrgCourseFilter {
  search?: string;
  category?: string;
  isPublished?: boolean | null;
  branchId?: string | null;
  page?: number;
  pageSize?: number;
}
