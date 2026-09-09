import { api } from "@/lib/api";
import type { SearchUsersResponse, SearchVideosResponse } from "@/types";

export async function searchVideos(params: { q: string; limit?: number; offset?: number; tags?: string[] }) {
  try {
    const { data } = await api.get<{ success: boolean; data: SearchVideosResponse }>("/api/search/videos", {
      params: {
        q: params.q,
        limit: params.limit ?? 30,
        offset: params.offset ?? 0,
        tags: params.tags?.length ? params.tags.join(",") : undefined,
      },
    });
    return data.data;
  } catch (error) {
    console.error("Failed to search videos:", error);
    return { videos: [], total: 0, offset: params.offset ?? 0, limit: params.limit ?? 30 };
  }
}

export async function searchUsers(params: { q: string; limit?: number; offset?: number }) {
  try {
    const { data } = await api.get<{ success: boolean; data: SearchUsersResponse }>("/api/search/users", {
      params: { q: params.q, limit: params.limit ?? 30, offset: params.offset ?? 0 },
    });
    return data.data;
  } catch (error) {
    console.error("Failed to search users:", error);
    return { users: [], total: 0, offset: params.offset ?? 0, limit: params.limit ?? 30 };
  }
}