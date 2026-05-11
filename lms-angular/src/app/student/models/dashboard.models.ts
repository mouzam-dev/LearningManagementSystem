export interface CourseProgress {
  courseId: string;
  title: string;
  thumbnailUrl?: string | null;
  teacherName: string;
  /** Per-enrollment progress, 0-100. */
  progressPercentage: number;
  isCompleted: boolean;
  enrolledAt: string;
}

export interface Deadline {
  assessmentId: string;
  courseId: string;
  title: string;
  courseTitle: string;
  /** "Quiz" or "Assignment". */
  type: string;
  dueDate: string;
}

export interface Dashboard {
  totalCourses: number;
  completedCourses: number;
  /** Average enrollment progress across all courses, 0-100. */
  overallProgress: number;
  enrolledCourses: CourseProgress[];
  upcomingDeadlines: Deadline[];
}
