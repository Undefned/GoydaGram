// These mirror the DTOs found in the GoydaGram backend source
// (UserService.Application.DTOs.UserDto, ContentService.Application.DTOs.VideoDto, etc.)
// Field names are kept exactly as the services emit them.

export interface User {
  id: string;
  username: string;
  email: string;
  avatarUrl: string | null;
  bio: string | null;
  isVerified: boolean;
  followersCount: number;
  followingCount: number;
  createdAt: string;
  role: string;
}

export interface AuthResult {
  userId: string;
  username: string;
  email: string;
  accessToken: string;
  refreshToken: string;
}

export interface Video {
  id: string;
  userId: string;
  title: string;
  description: string;
  duration: number;
  originalUrl: string;
  hlsManifestUrl: string;
  previewUrl: string;
  status: string;
  viewsCount: number;
  likesCount: number;
  commentsCount: number;
  createdAt: string;
  tags: string[];
}

export interface Paginated<T> {
  data: T[];
  pagination: {
    limit: number;
    offset: number;
    total?: number;
  };
}

export interface Comment {
  id: string;
  video_id: string;
  user_id: string;
  text: string;
  parent_id: string | null;
  created_at: string;
  updated_at: string;
}

export interface FeedItem {
  video_id?: string;
  id?: string;
  [key: string]: unknown;
}

export interface FeedResponse {
  items?: FeedItem[];
  videos?: Video[];
  offset?: number;
  limit?: number;
  [key: string]: unknown;
}

export interface SearchVideosResponse {
  videos: Video[];
  total: number;
  offset: number;
  limit: number;
}

export interface SearchUsersResponse {
  users: User[];
  total: number;
  offset: number;
  limit: number;
}
