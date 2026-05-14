export interface AdminDashboardSummary {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  students: number;
  teachers: number;
  admins: number;
  registrationsLast7Days: number;
  totalCourses: number;
  publishedCourses: number;
  draftCourses: number;
  totalEnrollments: number;
  completedEnrollments: number;
  certificatesIssued: number;
  totalSubmissions: number;
  ungradedSubmissions: number;
}

export interface RecentRegistration {
  userId: string;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface TopCourse {
  courseId: string;
  title: string;
  category: string;
  teacherName: string;
  studentCount: number;
  isPublished: boolean;
}

export interface AdminDashboard {
  summary: AdminDashboardSummary;
  recentRegistrations: RecentRegistration[];
  topCourses: TopCourse[];
}

export interface AdminUserListItem {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  isActive: boolean;
  isVerified: boolean;
  createdAt: string;
  coursesTaught: number;
  enrollments: number;
  certificates: number;
}

export interface AdminUsersPage {
  items: AdminUserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminUserDetail {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  bio?: string | null;
  profilePictureUrl?: string | null;
  isActive: boolean;
  isVerified: boolean;
  createdAt: string;
  updatedAt: string;
  coursesTaught: number;
  publishedCourses: number;
  totalStudentsAcrossCourses: number;
  enrollments: number;
  completedCourses: number;
  certificatesEarned: number;
  submissions: number;
}

export interface AdminUserFilter {
  search?: string;
  role?: string;
  isActive?: boolean | null;
  page?: number;
  pageSize?: number;
}
