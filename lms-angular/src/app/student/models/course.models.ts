export interface CourseListItem {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  teacherName: string;
  enrolledCount: number;
  maxStudents?: number | null;
  isEnrolled: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface CourseFilter {
  search?: string;
  category?: string;
  page?: number;
  pageSize?: number;
}
