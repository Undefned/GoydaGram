import { useMemo, useState } from "react";
import { useInfiniteQuery } from "@tanstack/react-query";
import { getFeed } from "@/api/feed";
import { getVideosBatch } from "@/api/videos";
import { useQuery } from "@tanstack/react-query";
import { VideoGrid, VideoGridSkeleton } from "@/components/VideoGrid";
import { useAuth } from "@/context/AuthContext";
import { Link } from "react-router-dom";
import type { FeedItem } from "@/types";

const PAGE_SIZE = 30;

function extractVideoIds(items: FeedItem[]): string[] {
  return items
    .map((item) => (typeof item.video_id === "string" ? item.video_id : typeof item.id === "string" ? item.id : null))
    .filter((id): id is string => Boolean(id));
}

export function FeedPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [seen, setSeen] = useState<string[]>([]);

  const feedQuery = useInfiniteQuery({
    queryKey: ["feed"],
    queryFn: ({ pageParam = 0 }) => getFeed({ offset: pageParam, limit: PAGE_SIZE, seen }),
    getNextPageParam: (lastPage, allPages) => {
      const items = lastPage.items ?? lastPage.videos ?? [];
      if (items.length < PAGE_SIZE) return undefined;
      return allPages.length * PAGE_SIZE;
    },
    initialPageParam: 0,
    enabled: Boolean(user),
  });

  const videoIds = useMemo(() => {
    const allItems = feedQuery.data?.pages.flatMap((p) => p.items ?? []) ?? [];
    return extractVideoIds(allItems);
  }, [feedQuery.data]);

  // The feed service returns lightweight feed entries (video ids + ranking
  // signals) — hydrate them into full VideoDto objects via ContentService's
  // batch endpoint so the grid has titles/thumbnails/counts to render.
  const videosQuery = useQuery({
    queryKey: ["feed-videos", videoIds],
    queryFn: () => getVideosBatch(videoIds),
    enabled: videoIds.length > 0,
  });

  const inlineVideos = useMemo(() => {
    const withVideos = feedQuery.data?.pages.flatMap((p) => p.videos ?? []) ?? [];
    return withVideos.length > 0 ? withVideos : undefined;
  }, [feedQuery.data]);

  const videos = inlineVideos ?? videosQuery.data ?? [];

  if (!authLoading && !user) {
    return (
      <div className="mx-auto max-w-md py-16 text-center">
        <h1 className="font-display text-xl font-semibold">Log in for your feed</h1>
        <p className="mt-2 text-sm text-ink-400">
          GoydaGram personalizes your feed once you're signed in. In the meantime, check out{" "}
          <Link to="/trending" className="text-mint-400 hover:underline">
            what's trending
          </Link>
          .
        </p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="font-display mb-6 text-xl font-semibold">Your feed</h1>

      {(feedQuery.isLoading || (videoIds.length > 0 && videosQuery.isLoading && !inlineVideos)) && (
        <VideoGridSkeleton />
      )}

      {!feedQuery.isLoading && (
        <VideoGrid videos={videos} emptyMessage="Nothing in your feed yet — like a few videos to train it." />
      )}

      {feedQuery.hasNextPage && (
        <div className="mt-8 flex justify-center">
          <button
            onClick={() => {
              setSeen((prev) => [...prev, ...videoIds]);
              feedQuery.fetchNextPage();
            }}
            disabled={feedQuery.isFetchingNextPage}
            className="rounded-full border border-ink-700 px-5 py-2 text-sm font-medium text-ink-200 hover:border-mint-400 hover:text-mint-400 transition-colors disabled:opacity-40"
          >
            {feedQuery.isFetchingNextPage ? "Loading…" : "Load more"}
          </button>
        </div>
      )}
    </div>
  );
}
