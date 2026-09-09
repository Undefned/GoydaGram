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

// Author info as embedded by FeedService's own enrichment step (a smaller
// projection than the full User type ContentService/UserService return).
export interface FeedAuthor {
  id: string;
  username: string;
  email: string;
  avatarUrl: string | null;
  isVerified: boolean;
}

// /api/feed and /api/feed/trending — FeedService's own response shape,
// distinct from ContentService's VideoDto shape only in that each video may
// carry an embedded `user`. Assumes FeedService's Go structs are patched to
// use camelCase json tags matching ContentService's actual (System.Text.Json
// default) output — see the backend patch notes for why that matters.
export interface FeedVideo extends Video {
  user?: FeedAuthor;
}

export interface FeedPage {
  videos: FeedVideo[];
  nextOffset: number;
  hasMore: boolean;
  totalCount?: number;
}

export interface UpdateProfileInput {
  username?: string;
  avatarUrl?: string;
  bio?: string;
}

export interface UpdateVideoInput {
  title?: string;
  description?: string;
  tags?: string[];
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
