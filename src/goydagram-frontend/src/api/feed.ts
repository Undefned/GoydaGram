import { api } from "@/lib/api";
import type { FeedResponse, Video } from "@/types";

export async function getFeed(params: { offset?: number; limit?: number; seen?: string[] }) {
  const { data } = await api.get<{ success: boolean; data: FeedResponse }>("/api/feed", {
    params: {
      offset: params.offset ?? 0,
      limit: params.limit ?? 30,
      seen: params.seen?.length ? params.seen.join(",") : undefined,
    },
  });
  return data.data;
}

export async function prefetchFeed(offset = 0, seen: string[] = []) {
  await api.get("/api/feed", {
    params: { offset, prefetch: true, seen: seen.length ? seen.join(",") : undefined },
  });
}

export async function getFeedTrending(limit = 30) {
  const { data } = await api.get<{ success: boolean; data: { videos: Video[] } }>("/api/feed/trending", {
    params: { limit },
  });
  return data.data.videos;
}
