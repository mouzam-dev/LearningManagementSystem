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

// ---------------- Course detail / builder --------------------------------

export interface TeacherLesson {
  lessonId: string;
  title: string;
  /** "Video" | "Document" | "Text" | "Quiz" | "Assignment". */
  type: string;
  /** Raw JSON content payload. For "Video" lessons it's `{"videoUrl":"…"}`. */
  content?: string | null;
  duration?: number | null;
  order: number;
  isPublished: boolean;
}

export interface TeacherModule {
  moduleId: string;
  title: string;
  description?: string | null;
  order: number;
  lessons: TeacherLesson[];
}

export interface TeacherCourseDetail {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string;
  studentCount: number;
  assessmentCount: number;
  averageProgress: number;
  modules: TeacherModule[];
}

export interface UpdateCourseBody {
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  maxStudents?: number | null;
}

export interface CreateModuleBody {
  title: string;
  description?: string | null;
}

export interface UpdateModuleBody {
  title: string;
  description?: string | null;
  order?: number | null;
}

export interface CreateLessonBody {
  title: string;
  type: string;
  content?: string | null;
  duration?: number | null;
  isPublished: boolean;
}

export interface UpdateLessonBody {
  title: string;
  type: string;
  content?: string | null;
  duration?: number | null;
  order?: number | null;
  isPublished: boolean;
}
