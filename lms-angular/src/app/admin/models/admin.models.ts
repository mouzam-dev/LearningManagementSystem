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

// ---------------- Course moderation -------------------------------------

export interface AdminCourseListItem {
  courseId: string;
  title: string;
  category: string;
  isPublished: boolean;
  teacherId: string;
  teacherName: string;
  teacherEmail: string;
  moduleCount: number;
  lessonCount: number;
  studentCount: number;
  assessmentCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface AdminCoursesPage {
  items: AdminCourseListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminCourseModule {
  moduleId: string;
  title: string;
  order: number;
  lessonCount: number;
}

export interface AdminCourseDetail {
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
  moduleCount: number;
  lessonCount: number;
  assessmentCount: number;
  studentCount: number;
  completedCount: number;
  certificatesIssued: number;
  averageProgress: number;
  modules: AdminCourseModule[];
}

export interface AdminCourseFilter {
  search?: string;
  category?: string;
  isPublished?: boolean | null;
  page?: number;
  pageSize?: number;
}

// ---------------- Audit log ---------------------------------------------

export interface AdminAuditLog {
  id: string;
  actorId?: string | null;
  actorName: string;
  actorEmail: string;
  action: string;
  entity: string;
  entityId?: string | null;
  changes?: string | null;
  createdAt: string;
}

export interface AdminAuditLogPage {
  items: AdminAuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ---------------- Reporting ---------------------------------------------

export interface MonthlyRegistration {
  year: number;
  month: number;
  label: string;
  students: number;
  teachers: number;
  total: number;
}

export interface MonthlyCount {
  year: number;
  month: number;
  label: string;
  count: number;
}

export interface CategoryReport {
  category: string;
  courseCount: number;
  publishedCount: number;
  enrollmentCount: number;
  completionRate: number;
}

export interface TopTeacherReport {
  teacherId: string;
  name: string;
  email: string;
  courseCount: number;
  publishedCount: number;
  totalStudents: number;
}

export interface CompletionFunnel {
  enrolled: number;
  started: number;
  completed: number;
  certified: number;
}

export interface AdminReport {
  generatedAt: string;
  totalUsers: number;
  totalCourses: number;
  totalEnrollments: number;
  totalCertificates: number;
  registrationTrend: MonthlyRegistration[];
  enrollmentTrend: MonthlyCount[];
  categories: CategoryReport[];
  topTeachers: TopTeacherReport[];
  funnel: CompletionFunnel;
}
