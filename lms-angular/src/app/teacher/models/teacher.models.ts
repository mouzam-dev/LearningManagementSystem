export interface TeacherDashboardSummary {
  totalCourses: number;
  publishedCourses: number;
  draftCourses: number;
  totalStudents: number;
  totalActiveStudents: number;
  completedEnrollments: number;
  pendingGradingCount: number;
  certificatesIssued: number;
}

export interface TeacherCourseSummary {
  courseId: string;
  title: string;
  category: string;
  isPublished: boolean;
  studentCount: number;
  lessonCount: number;
  updatedAt: string;
}

export interface RecentEnrollment {
  enrollmentId: string;
  courseId: string;
  courseTitle: string;
  studentName: string;
  enrolledAt: string;
}

export interface PendingGrading {
  submissionId: string;
  assessmentId: string;
  assessmentTitle: string;
  courseTitle: string;
  studentName: string;
  submittedAt: string;
}

export interface TeacherDashboard {
  summary: TeacherDashboardSummary;
  topCourses: TeacherCourseSummary[];
  recentEnrollments: RecentEnrollment[];
  pendingGrading: PendingGrading[];
}

export interface TeacherCourseListItem {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
  isPublished: boolean;
  moduleCount: number;
  lessonCount: number;
  assessmentCount: number;
  studentCount: number;
  averageProgress: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCourseBody {
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
}

export interface CreatedCourse {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
  isPublished: boolean;
  createdAt: string;
}
