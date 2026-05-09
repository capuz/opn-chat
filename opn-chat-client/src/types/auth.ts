export interface UserDto {
  id: string;
  email: string;
  nickname: string;
  avatarUrl?: string;
  bio?: string;
  status?: string;
  lastSeen: string;
  countryCode?: string;
  showFlag?: boolean;
}

export type MessageType = 'normal' | 'action';

export interface Message {
  id?: string;
  userId: string;
  userName?: string;
  content: string;
  timestamp: string;
  replyToId?: string;
  type?: MessageType;
  badge?: string;
  createdAt?: string;
}

export interface AuthResponseDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
  user: UserDto;
}

export interface GoogleAuthDto {
  googleToken: string;
}

export interface RefreshTokenDto {
  refreshToken: string;
}
