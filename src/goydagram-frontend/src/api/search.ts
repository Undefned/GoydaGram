import { api } from "@/lib/api";
import type { SearchUsersResponse, SearchVideosResponse } from "@/types";

export async function searchVideos(params: { q: string; limit?: number; offset?: number; tags?: string[] }) {
  const { data } = await api.get<{ success: boolean; data: SearchVideosResponse }>("/api/search/videos", {
    params: {
      q: params.q,
      limit: params.limit ?? 30,
      offset: params.offset ?? 0,
      tags: params.tags?.length ? params.tags.join(",") : undefined,
    },
  });
  return data.data;
}

export async function searchUsers(params: { q: string; limit?: number; offset?: number }) {
  const { data } = await api.get<{ success: boolean; data: SearchUsersResponse }>("/api/search/users", {
    params: { q: params.q, limit: params.limit ?? 30, offset: params.offset ?? 0 },
  });
  return data.data;
}