export interface CourseRating {
  id: string;
  courseId: string;
  studentId: string;
  studentName: string;
  studentAvatarUrl?: string | null;
  rating: number;
  review: string;
  createdAt: string;
  updatedAt: string;
}

export interface TeacherRating {
  id: string;
  teacherId: string;
  studentId: string;
  studentName: string;
  studentAvatarUrl?: string | null;
  courseId: string;
  courseTitle: string;
  rating: number;
  review: string;
  createdAt: string;
  updatedAt: string;
}

export interface RatingSummary {
  average: number;
  totalCount: number;
  oneStar: number;
  twoStar: number;
  threeStar: number;
  fourStar: number;
  fiveStar: number;
}

export interface CourseRatingsPage {
  summary: RatingSummary;
  items: CourseRating[];
  myRating: CourseRating | null;
}

export interface TeacherRatingsPage {
  summary: RatingSummary;
  items: TeacherRating[];
}

export interface SubmitRatingBody {
  rating: number;
  review?: string;
}

export interface SubmitTeacherRatingBody {
  courseId: string;
  rating: number;
  review?: string;
}
