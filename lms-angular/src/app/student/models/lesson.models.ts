export interface LessonListItem {
  lessonId: string;
  title: string;
  /** "Video" | "Document" | "Text" | "Quiz" | "Assignment". */
  type: string;
  /** Duration in minutes. */
  duration?: number | null;
  order: number;
  isCompleted: boolean;
  watchTimeSeconds: number;
}

export interface ModuleSummary {
  moduleId: string;
  title: string;
  description?: string | null;
  order: number;
  lessons: LessonListItem[];
}

export interface CourseDetail {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  teacherName: string;
  enrolledCount: number;
  maxStudents?: number | null;

  isEnrolled: boolean;
  /** 0-100. */
  progressPercentage: number;
  totalLessons: number;
  completedLessons: number;

  modules: ModuleSummary[];
  /** First incomplete lesson, or first lesson if everything is done. */
  nextLessonId?: string | null;
}

export interface LessonDetail {
  lessonId: string;
  title: string;
  type: string;
  duration?: number | null;
  order: number;

  videoUrl?: string | null;
  body?: string | null;

  courseId: string;
  courseTitle: string;
  moduleId: string;
  moduleTitle: string;

  watchTimeSeconds: number;
  isCompleted: boolean;
  completedAt?: string | null;

  previousLessonId?: string | null;
  nextLessonId?: string | null;

  modules: ModuleSummary[];
}

export interface LessonProgressUpdate {
  watchTimeSeconds: number;
  markComplete: boolean;
}
