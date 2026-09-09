import { useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { AxiosError } from "axios";
import { searchUsers, searchVideos } from "@/api/search";
import { VideoGrid, VideoGridSkeleton } from "@/components/VideoGrid";
import { Avatar } from "@/components/Layout";
import { Link } from "react-router-dom";

type Tab = "videos" | "users";

function errorMessage(err: unknown) {
  if (err instanceof AxiosError) {
    if (!err.response) return "Couldn't reach the search service — check it's running and reachable via the gateway.";
    return `Search failed (${err.response.status}): ${err.response.data?.message ?? err.response.statusText}`;
  }
  return "Something went wrong while searching.";
}

export function SearchPage() {
  const [params] = useSearchParams();
  const q = params.get("q") ?? "";
  const [tab, setTab] = useState<Tab>("videos");

  const videosQuery = useQuery({
    queryKey: ["search-videos", q],
    queryFn: () => searchVideos({ q, limit: 40 }),
    enabled: q.length > 0 && tab === "videos",
    retry: false,
    placeholderData: { videos: [], total: 0, offset: 0, limit: 40 },
  });

  const usersQuery = useQuery({
    queryKey: ["search-users", q],
    queryFn: () => searchUsers({ q, limit: 40 }),
    enabled: q.length > 0 && tab === "users",
    retry: false,
    placeholderData: { users: [], total: 0, offset: 0, limit: 40 },
  });

  if (!q) {
    return <p className="text-sm text-ink-400">Search for videos or people using the bar above.</p>;
  }

  const videos = videosQuery.data?.videos ?? [];
  const users = usersQuery.data?.users ?? [];

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

      {tab === "videos" && (
        <>
          {videosQuery.isError && (
            <p className="mb-4 rounded-lg bg-flare-500/10 px-3 py-2 text-sm text-flare-400">
              {errorMessage(videosQuery.error)}
            </p>
          )}
          {videosQuery.isLoading ? (
            <VideoGridSkeleton />
          ) : (
            !videosQuery.isError && (
              <VideoGrid videos={videos} emptyMessage="No videos matched." />
            )
          )}
        </>
      )}

      {tab === "users" && (
        <div className="space-y-3">
          {usersQuery.isError && (
            <p className="rounded-lg bg-flare-500/10 px-3 py-2 text-sm text-flare-400">
              {errorMessage(usersQuery.error)}
            </p>
          )}
          {usersQuery.isLoading && <p className="text-sm text-ink-400">Searching…</p>}
          {!usersQuery.isLoading && users.map((u) => (
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
          {!usersQuery.isLoading && users.length === 0 && (
            <p className="text-sm text-ink-400">No people matched.</p>
          )}
        </div>
      )}
    </div>
  );
}