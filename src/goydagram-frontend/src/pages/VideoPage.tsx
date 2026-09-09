import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { deleteVideo, getVideo, resolveMediaUrl, getHlsUrl } from "@/api/videos";
import { getSubscriptions, getUser, subscribe, unsubscribe, refreshUserInterests } from "@/api/users";
import { getVideoLikesCount, likeVideo, recordView, unlikeVideo } from "@/api/social";
import { HlsPlayer } from "@/components/HlsPlayer";
import { CommentSection } from "@/components/CommentSection";
import { EditVideoModal } from "@/components/EditVideoModal";
import { Avatar } from "@/components/Layout";
import { useAuth } from "@/context/AuthContext";
import { useNavigate } from "react-router-dom";

function formatCount(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return `${n}`;
}

export function VideoPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [liked, setLiked] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const hasRecordedView = useRef(false);

  const videoQuery = useQuery({
    queryKey: ["video", id],
    queryFn: () => getVideo(id!),
    enabled: Boolean(id),
  });

  const authorQuery = useQuery({
    queryKey: ["user", videoQuery.data?.userId],
    queryFn: () => getUser(videoQuery.data!.userId),
    enabled: Boolean(videoQuery.data?.userId),
  });

  const likesQuery = useQuery({
    queryKey: ["video-likes", id],
    queryFn: () => getVideoLikesCount(id!),
    enabled: Boolean(id),
    placeholderData: 0,
  });

  const likeMutation = useMutation({
    mutationFn: () => {
      if (!user || !id) throw new Error("Must be logged in");
      return liked ? unlikeVideo(id, user.id) : likeVideo(id, user.id);
    },
    onSuccess: () => {
      setLiked((prev) => !prev);
      queryClient.invalidateQueries({ queryKey: ["video-likes", id] });
      // Обновляем интересы после лайка (fire-and-forget)
      if (!liked && user) {
        refreshUserInterests(user.id).catch(() => {});
      }
    },
  });

  const subscriptionsQuery = useQuery({
    queryKey: ["subscriptions", user?.id],
    queryFn: () => getSubscriptions(user!.id),
    enabled: Boolean(user),
  });

  const isSubscribed = Boolean(
    authorQuery.data && subscriptionsQuery.data?.some((u) => u.id === authorQuery.data!.id)
  );

  const subscribeMutation = useMutation({
    mutationFn: () => {
      if (!authorQuery.data) throw new Error("No author");
      return isSubscribed ? unsubscribe(authorQuery.data.id) : subscribe(authorQuery.data.id);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["subscriptions", user?.id] }),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteVideo(id!),
    onSuccess: () => navigate("/profile"),
  });

  const handlePlay = () => {
    if (!hasRecordedView.current && user && id) {
      hasRecordedView.current = true;
      recordView(id, user.id);
    }
  };

  useEffect(() => {
    hasRecordedView.current = false;
  }, [id]);

  const video = videoQuery.data;
  const likesCount = likesQuery.data ?? video?.likesCount ?? 0;

  if (videoQuery.isLoading) {
    return <p className="text-sm text-ink-400">Loading video…</p>;
  }

  if (!video) {
    return <p className="text-sm text-ink-400">Video not found.</p>;
  }

  const isOwner = user?.id === video.userId;

  return (
    <div className="mx-auto max-w-4xl">
      <div className="aspect-video">
        <HlsPlayer
          src={getHlsUrl(video.hlsManifestUrl) || resolveMediaUrl(video.originalUrl, "stream")}
          poster={video.previewUrl ? resolveMediaUrl(video.previewUrl, "preview") : undefined}
          onPlay={handlePlay}
        />
      </div>

      <h1 className="font-display mt-4 text-xl font-semibold">{video.title}</h1>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
        <Link to={`/users/${video.userId}`} className="flex items-center gap-3">
          <Avatar url={authorQuery.data?.avatarUrl ?? null} name={authorQuery.data?.username ?? "?"} />
          <div>
            <p className="text-sm font-medium">{authorQuery.data?.username ?? "…"}</p>
            <p className="text-xs text-ink-400">
              {authorQuery.data ? `${formatCount(authorQuery.data.followersCount)} followers` : ""}
            </p>
          </div>
        </Link>

        <div className="flex items-center gap-2">
          <button
            onClick={() => likeMutation.mutate()}
            disabled={!user || likeMutation.isPending}
            className={`rounded-full border px-4 py-2 text-sm font-medium transition-colors disabled:opacity-40 ${
              liked ? "border-flare-500 text-flare-500" : "border-ink-700 text-ink-200 hover:border-flare-500 hover:text-flare-500"
            }`}
          >
            {liked ? "Liked" : "Like"} · {formatCount(likesCount)}
          </button>

          {user && !isOwner && (
            <button
              onClick={() => subscribeMutation.mutate()}
              disabled={subscribeMutation.isPending}
              className={`rounded-full px-4 py-2 text-sm font-semibold transition-colors disabled:opacity-40 ${
                isSubscribed
                  ? "border border-ink-700 text-ink-200 hover:border-flare-500 hover:text-flare-400"
                  : "bg-flare-500 text-ink-950 hover:bg-flare-400"
              }`}
            >
              {isSubscribed ? "Subscribed" : "Subscribe"}
            </button>
          )}

          {isOwner && (
            <>
              <button
                onClick={() => setIsEditing(true)}
                className="rounded-full border border-ink-700 px-4 py-2 text-sm font-medium text-ink-200 hover:border-mint-400 hover:text-mint-400 transition-colors"
              >
                Edit
              </button>
              <button
                onClick={() => {
                  if (confirm("Delete this video?")) deleteMutation.mutate();
                }}
                className="rounded-full border border-ink-700 px-4 py-2 text-sm font-medium text-ink-400 hover:border-flare-500 hover:text-flare-400 transition-colors"
              >
                Delete
              </button>
            </>
          )}
        </div>
      </div>

      {video.description && (
        <p className="mt-4 whitespace-pre-wrap rounded-lg bg-ink-900 p-4 text-sm text-ink-200">
          {video.description}
        </p>
      )}

      {video.tags.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-2">
          {video.tags.map((tag) => (
            <Link
              key={tag}
              to={`/search?q=${encodeURIComponent(tag)}`}
              className="rounded-full bg-ink-900 px-3 py-1 text-xs text-mint-400 hover:bg-ink-800 transition-colors"
            >
              #{tag}
            </Link>
          ))}
        </div>
      )}

      <div className="mt-8 border-t border-ink-800 pt-6">
        <CommentSection videoId={video.id} />
      </div>

      {isEditing && <EditVideoModal video={video} onClose={() => setIsEditing(false)} />}
    </div>
  );
}