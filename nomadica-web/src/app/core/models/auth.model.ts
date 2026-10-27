export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  displayName: string;
  email: string;
  password: string;
  socialProfileUrl: string;
}

export interface RegisterResponse {
  membershipStatus: string;
  message: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthUserDto;
}

export interface AuthUserDto {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  roles?: string[];
  subscriptionPlan?: string;
  createdAt?: string;
}
