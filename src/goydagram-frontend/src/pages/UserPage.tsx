import { useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getUser, getSubscriptions, subscribe, unsubscribe } from "@/api/users";
import { getUserVideos } from "@/api/videos";
import { VideoGrid, VideoGridSkeleton } from "@/components/VideoGrid";
import { Avatar } from "@/components/Layout";
import { useAuth } from "@/context/AuthContext";

function formatCount(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return `${n}`;
}

export function UserPage() {
  const { id } = useParams<{ id: string }>();
  const { user: me } = useAuth();
  const queryClient = useQueryClient();

  const userQuery = useQuery({
    queryKey: ["user", id],
    queryFn: () => getUser(id!),
    enabled: Boolean(id),
  });

  const videosQuery = useQuery({
    queryKey: ["user-videos", id],
    queryFn: () => getUserVideos(id!, 50, 0),
    enabled: Boolean(id),
  });

  const mySubscriptionsQuery = useQuery({
    queryKey: ["subscriptions", me?.id],
    queryFn: () => getSubscriptions(me!.id),
    enabled: Boolean(me),
  });

  const isSubscribed = Boolean(id && mySubscriptionsQuery.data?.some((u) => u.id === id));

  const subscribeMutation = useMutation({
    mutationFn: () => (isSubscribed ? unsubscribe(id!) : subscribe(id!)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["subscriptions", me?.id] }),
  });

  const profile = userQuery.data;

  if (userQuery.isLoading) return <p className="text-sm text-ink-400">Loading…</p>;
  if (!profile) return <p className="text-sm text-ink-400">User not found.</p>;

  return (
    <div>
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <Avatar url={profile.avatarUrl} name={profile.username} size="h-16 w-16" />
          <div>
            <h1 className="font-display text-xl font-semibold">{profile.username}</h1>
            <p className="text-sm text-ink-400">
              {formatCount(profile.followersCount)} followers · {formatCount(profile.followingCount)} following
            </p>
          </div>
        </div>

        {me && me.id !== profile.id && (
          <button
            onClick={() => subscribeMutation.mutate()}
            disabled={subscribeMutation.isPending}
            className={`rounded-full px-5 py-2 text-sm font-semibold transition-colors disabled:opacity-40 ${
              isSubscribed
                ? "border border-ink-700 text-ink-200 hover:border-flare-500 hover:text-flare-400"
                : "bg-flare-500 text-ink-950 hover:bg-flare-400"
            }`}
          >
            {isSubscribed ? "Subscribed" : "Subscribe"}
          </button>
        )}
      </div>

      {profile.bio && <p className="mt-4 max-w-lg text-sm text-ink-200">{profile.bio}</p>}

      <h2 className="font-display mt-8 mb-4 text-lg font-semibold">Videos</h2>
      {videosQuery.isLoading ? (
        <VideoGridSkeleton />
      ) : (
        <VideoGrid videos={videosQuery.data?.data ?? []} emptyMessage="No videos yet." />
      )}
    </div>
  );
}
