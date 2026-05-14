export interface Profile {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  bio?: string | null;
  profilePictureUrl?: string | null;
  isVerified: boolean;
  createdAt: string;
}

export interface UpdateProfileBody {
  firstName: string;
  lastName: string;
  bio?: string | null;
  profilePictureUrl?: string | null;
}

export interface ChangePasswordBody {
  currentPassword: string;
  newPassword: string;
}
