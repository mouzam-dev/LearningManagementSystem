export interface PublicCourseListItem {
  courseId: string;
  title: string;
  description: string;
  category: string;
  thumbnailUrl?: string | null;
  teacherName: string;
  enrolledCount: number;
  maxStudents?: number | null;
}

export interface PublicTeacher {
  id: string;
  name: string;
  courseCount: number;
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

export interface PublicCourseFilter {
  search?: string;
  category?: string;
  teacherId?: string;
  page?: number;
  pageSize?: number;
}
