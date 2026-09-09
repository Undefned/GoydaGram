import { useQuery } from "@tanstack/react-query";
import { getMyVideos } from "@/api/videos";
import { getSubscriptions } from "@/api/users";
import { VideoGrid, VideoGridSkeleton } from "@/components/VideoGrid";
import { Avatar } from "@/components/Layout";
import { useAuth } from "@/context/AuthContext";
import { Link } from "react-router-dom";

function formatCount(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return `${n}`;
}

export function ProfilePage() {
  const { user } = useAuth();

  const videosQuery = useQuery({
    queryKey: ["my-videos"],
    queryFn: () => getMyVideos(50, 0),
    enabled: Boolean(user),
  });

  const subscriptionsQuery = useQuery({
    queryKey: ["subscriptions", user?.id],
    queryFn: () => getSubscriptions(user!.id),
    enabled: Boolean(user),
  });

  if (!user) return null;

  return (
    <div>
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <Avatar url={user.avatarUrl} name={user.username} size="h-16 w-16" />
          <div>
            <h1 className="font-display text-xl font-semibold">{user.username}</h1>
            <p className="text-sm text-ink-400">
              {formatCount(user.followersCount)} followers · {formatCount(user.followingCount)} following
            </p>
          </div>
        </div>
        <Link
          to="/profile/edit"
          className="rounded-full border border-ink-700 px-4 py-2 text-sm font-medium text-ink-200 hover:border-mint-400 hover:text-mint-400 transition-colors"
        >
          Edit profile
        </Link>
      </div>

      {user.bio && <p className="mt-4 max-w-lg text-sm text-ink-200">{user.bio}</p>}

      {subscriptionsQuery.data && subscriptionsQuery.data.length > 0 && (
        <div className="mt-6">
          <h2 className="mb-3 text-sm font-medium text-ink-200">Following</h2>
          <div className="flex flex-wrap gap-3">
            {subscriptionsQuery.data.map((sub) => (
              <Link
                key={sub.id}
                to={`/users/${sub.id}`}
                className="flex items-center gap-2 rounded-full border border-ink-800 py-1 pl-1 pr-3 hover:border-mint-400 transition-colors"
              >
                <Avatar url={sub.avatarUrl} name={sub.username} size="h-6 w-6" />
                <span className="text-xs">{sub.username}</span>
              </Link>
            ))}
          </div>
        </div>
      )}

      <h2 className="font-display mt-8 mb-4 text-lg font-semibold">Your videos</h2>
      {videosQuery.isLoading ? (
        <VideoGridSkeleton />
      ) : (
        <VideoGrid videos={videosQuery.data?.data ?? []} emptyMessage="You haven't uploaded anything yet." />
      )}
    </div>
  );
}
