import { useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { searchUsers, searchVideos } from "@/api/search";
import { VideoGrid, VideoGridSkeleton } from "@/components/VideoGrid";
import { Avatar } from "@/components/Layout";
import { Link } from "react-router-dom";

type Tab = "videos" | "users";

export function SearchPage() {
  const [params] = useSearchParams();
  const q = params.get("q") ?? "";
  const [tab, setTab] = useState<Tab>("videos");

  const videosQuery = useQuery({
    queryKey: ["search-videos", q],
    queryFn: () => searchVideos({ q, limit: 40 }),
    enabled: q.length > 0 && tab === "videos",
  });

  const usersQuery = useQuery({
    queryKey: ["search-users", q],
    queryFn: () => searchUsers({ q, limit: 40 }),
    enabled: q.length > 0 && tab === "users",
  });

  if (!q) {
    return <p className="text-sm text-ink-400">Search for videos or people using the bar above.</p>;
  }

  return (
    <div>
      <h1 className="font-display mb-4 text-xl font-semibold">Results for "{q}"</h1>

      <div className="mb-6 flex gap-2 border-b border-ink-800">
        {(["videos", "users"] as Tab[]).map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium capitalize transition-colors ${
              tab === t ? "border-b-2 border-flare-500 text-ink-100" : "text-ink-400 hover:text-ink-100"
            }`}
          >
            {t}
          </button>
        ))}
      </div>

      {tab === "videos" &&
        (videosQuery.isLoading ? (
          <VideoGridSkeleton />
        ) : (
          <VideoGrid videos={videosQuery.data?.videos ?? []} emptyMessage="No videos matched." />
        ))}

      {tab === "users" && (
        <div className="space-y-3">
          {usersQuery.isLoading && <p className="text-sm text-ink-400">Searching…</p>}
          {usersQuery.data?.users.map((u) => (
            <Link
              key={u.id}
              to={`/users/${u.id}`}
              className="flex items-center gap-3 rounded-lg p-2 hover:bg-ink-900 transition-colors"
            >
              <Avatar url={u.avatarUrl} name={u.username} />
              <div>
                <p className="text-sm font-medium">{u.username}</p>
                <p className="text-xs text-ink-400">{u.followersCount} followers</p>
              </div>
            </Link>
          ))}
          {usersQuery.data && usersQuery.data.users.length === 0 && (
            <p className="text-sm text-ink-400">No people matched.</p>
          )}
        </div>
      )}
    </div>
  );
}
