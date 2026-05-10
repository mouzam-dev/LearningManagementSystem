export type Role = 'Student' | 'Teacher' | 'Admin';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: Role;
  profilePictureUrl?: string | null;
  isVerified: boolean;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  accessToken?: string;
  refreshToken?: string;
  user?: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  role: Extract<Role, 'Student' | 'Teacher'>;
}

export interface MeResponse {
  id: string;
  email: string;
  role: Role;
}

/** Backend returns RFC 7807 ProblemDetails on validation failure. */
export interface ValidationProblem {
  type?: string;
  title?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
