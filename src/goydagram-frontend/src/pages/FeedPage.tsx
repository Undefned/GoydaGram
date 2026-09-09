import { useEffect, useMemo, useRef, useState } from "react";
import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { getFeed, getFeedTrending } from "@/api/feed";
import { getUserVideos, getMyVideos } from "@/api/videos";
import { getSubscriptions } from "@/api/users";
import { ReelCard } from "@/components/ReelCard";
import { CommentsDrawer } from "@/components/CommentsDrawer";
import { useAuth } from "@/context/AuthContext";
import type { FeedVideo } from "@/types";

type Tab = "for-you" | "following" | "popular" | "my";

const TABS: { id: Tab; label: string }[] = [
  { id: "for-you", label: "For You" },
  { id: "following", label: "Following" },
  { id: "popular", label: "Popular" },
  { id: "my", label: "My" },
];

const PAGE_SIZE = 20;

function ReelsColumn({
  videos,
  isLoading,
  emptyMessage,
  onOpenComments,
  onReachEnd,
}: {
  videos: FeedVideo[];
  isLoading: boolean;
  emptyMessage: string;
  onOpenComments: (id: string) => void;
  onReachEnd?: () => void;
}) {
  const sentinelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!onReachEnd) return;
    const el = sentinelRef.current;
    if (!el) return;
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) onReachEnd();
    });
    observer.observe(el);
    return () => observer.disconnect();
  }, [onReachEnd]);

  if (isLoading) {
    return (
      <div className="flex h-[calc(100vh-3.5rem)] items-center justify-center md:h-screen">
        <p className="text-sm text-ink-400">Loading…</p>
      </div>
    );
  }

  if (videos.length === 0) {
    return (
      <div className="flex h-[calc(100vh-3.5rem)] items-center justify-center px-8 text-center md:h-screen">
        <p className="text-sm text-ink-400">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className="h-[calc(100vh-3.5rem)] snap-y snap-mandatory overflow-y-scroll no-scrollbar md:h-screen">
      {videos.map((video) => (
        <ReelCard key={video.id} video={video} onOpenComments={onOpenComments} />
      ))}
      <div ref={sentinelRef} className="h-px" />
    </div>
  );
}

export function FeedPage() {
  const { user, isLoading: authLoading } = useAuth();
  const [tab, setTab] = useState<Tab>("for-you");
  const [commentsVideoId, setCommentsVideoId] = useState<string | null>(null);

  // ---- For You: personalized, paginated -----------------------------
  const forYouQuery = useInfiniteQuery({
    queryKey: ["feed", "for-you"],
    queryFn: ({ pageParam = 0 }) => getFeed({ offset: pageParam, limit: PAGE_SIZE }),
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextOffset : undefined),
    initialPageParam: 0,
    enabled: Boolean(user) && tab === "for-you",
  });
  const forYouVideos = useMemo(
    () => forYouQuery.data?.pages.flatMap((p) => p.videos) ?? [],
    [forYouQuery.data]
  );

  // ---- Popular: FeedService's trending, already author-enriched -----
  const popularQuery = useQuery({
    queryKey: ["feed", "popular"],
    queryFn: () => getFeedTrending(PAGE_SIZE),
    enabled: tab === "popular",
  });

  // ---- Following: no dedicated backend endpoint — composed client-side
  // from each followee's latest uploads, sorted by recency. See README.
  const subscriptionsQuery = useQuery({
    queryKey: ["subscriptions", user?.id],
    queryFn: () => getSubscriptions(user!.id),
    enabled: Boolean(user) && tab === "following",
  });

  const followingQuery = useQuery({
    queryKey: ["feed", "following", subscriptionsQuery.data?.map((u) => u.id)],
    queryFn: async () => {
      const followees = subscriptionsQuery.data ?? [];
      const perFollowee = await Promise.all(
        followees.map(async (followee) => {
          const page = await getUserVideos(followee.id, 10, 0);
          return page.data.map((v): FeedVideo => ({ ...v, user: { ...followee, email: "" } }));
        })
      );
      return perFollowee
        .flat()
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    },
    enabled: Boolean(subscriptionsQuery.data),
  });

  // ---- My: own uploads, author = self, no extra lookup needed --------
  const myQuery = useQuery({
    queryKey: ["feed", "my"],
    queryFn: () => getMyVideos(PAGE_SIZE, 0),
    enabled: Boolean(user) && tab === "my",
  });
  const myVideos = useMemo(
    () => (myQuery.data?.data ?? []).map((v): FeedVideo => (user ? { ...v, user: { ...user, email: user.email } } : v)),
    [myQuery.data, user]
  );

  if (!authLoading && !user && tab !== "popular") {
    return (
      <div className="relative -mx-4 -my-6 md:-mx-8">
        <TabBar tab={tab} setTab={setTab} />
        <div className="flex h-[calc(100vh-7rem)] flex-col items-center justify-center px-8 text-center">
          <h1 className="font-display text-xl font-semibold">Log in for this tab</h1>
          <p className="mt-2 text-sm text-ink-400">
            Check out{" "}
            <button onClick={() => setTab("popular")} className="text-mint-400 hover:underline">
              Popular
            </button>{" "}
            in the meantime, or{" "}
            <Link to="/login" className="text-mint-400 hover:underline">
              log in
            </Link>
            .
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="relative -mx-4 -my-6 md:-mx-8">
      <TabBar tab={tab} setTab={setTab} />

      {tab === "for-you" && (
        <ReelsColumn
          videos={forYouVideos}
          isLoading={forYouQuery.isLoading}
          emptyMessage="Nothing in your feed yet — like a few videos to train it."
          onOpenComments={setCommentsVideoId}
          onReachEnd={forYouQuery.hasNextPage ? () => forYouQuery.fetchNextPage() : undefined}
        />
      )}

      {tab === "popular" && (
        <ReelsColumn
          videos={popularQuery.data ?? []}
          isLoading={popularQuery.isLoading}
          emptyMessage="Nothing trending right now."
          onOpenComments={setCommentsVideoId}
        />
      )}

      {tab === "following" && (
        <ReelsColumn
          videos={followingQuery.data ?? []}
          isLoading={subscriptionsQuery.isLoading || followingQuery.isLoading}
          emptyMessage="Follow a few people to see their videos here."
          onOpenComments={setCommentsVideoId}
        />
      )}

      {tab === "my" && (
        <ReelsColumn
          videos={myVideos}
          isLoading={myQuery.isLoading}
          emptyMessage="You haven't uploaded anything yet."
          onOpenComments={setCommentsVideoId}
        />
      )}

      <CommentsDrawer videoId={commentsVideoId} onClose={() => setCommentsVideoId(null)} />
    </div>
  );
}

function TabBar({ tab, setTab }: { tab: Tab; setTab: (t: Tab) => void }) {
  return (
    <div className="absolute inset-x-0 top-0 z-10 flex justify-center gap-1 bg-gradient-to-b from-black/70 to-transparent px-3 py-3">
      {TABS.map((t) => (
        <button
          key={t.id}
          onClick={() => setTab(t.id)}
          className={`rounded-full px-3 py-1.5 text-xs font-semibold transition-colors ${
            tab === t.id ? "bg-white text-ink-950" : "text-white/80 hover:text-white"
          }`}
        >
          {t.label}
        </button>
      ))}
    </div>
  );
}
