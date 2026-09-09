import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { FeedVideo } from "@/types";
import { resolveMediaUrl } from "@/api/videos";
import { getVideoLikesCount, likeVideo, recordView, unlikeVideo } from "@/api/social";
import { HlsPlayer } from "./HlsPlayer";
import { Avatar } from "./Layout";
import { useAuth } from "@/context/AuthContext";

function formatCount(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return `${n}`;
}

interface ReelCardProps {
  video: FeedVideo;
  onOpenComments: (videoId: string) => void;
}

export function ReelCard({ video, onOpenComments }: ReelCardProps) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const containerRef = useRef<HTMLDivElement>(null);
  const hasRecordedView = useRef(false);

  const [isActive, setIsActive] = useState(false);
  const [isMuted, setIsMuted] = useState(true);
  const [liked, setLiked] = useState(false);
  const [showPauseHint, setShowPauseHint] = useState(false);

  const likesQuery = useQuery({
    queryKey: ["video-likes", video.id],
    queryFn: () => getVideoLikesCount(video.id),
  });

  const likeMutation = useMutation({
    mutationFn: () => {
      if (!user) throw new Error("Must be logged in");
      return liked ? unlikeVideo(video.id, user.id) : likeVideo(video.id, user.id);
    },
    onSuccess: () => {
      setLiked((prev) => !prev);
      queryClient.invalidateQueries({ queryKey: ["video-likes", video.id] });
    },
  });

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const observer = new IntersectionObserver(
      ([entry]) => setIsActive(entry.isIntersecting && entry.intersectionRatio > 0.6),
      { threshold: [0, 0.6, 1] }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    if (isActive && !hasRecordedView.current && user) {
      hasRecordedView.current = true;
      recordView(video.id, user.id).catch(() => {
        hasRecordedView.current = false;
      });
    }
  }, [isActive, user, video.id]);

  const handleTap = () => {
    setShowPauseHint(true);
    setTimeout(() => setShowPauseHint(false), 500);
  };

  const author = video.user;

  return (
    <div
      ref={containerRef}
      className="relative h-[calc(100vh-3.5rem)] md:h-screen w-full snap-start snap-always shrink-0 bg-black"
    >
      <HlsPlayer
        src={resolveMediaUrl(video.hlsManifestUrl || video.originalUrl, "stream")}
        poster={video.previewUrl ? resolveMediaUrl(video.previewUrl, "preview") : undefined}
        controls={false}
        muted={isMuted}
        loop
        active={isActive}
        onClick={handleTap}
        className="h-full w-full object-contain"
      />

      {showPauseHint && (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
          <div className="rounded-full bg-black/40 p-4">
            <svg viewBox="0 0 24 24" fill="white" className="h-10 w-10">
              <path d="M8 5v14l11-7z" />
            </svg>
          </div>
        </div>
      )}

      {/* Bottom-left: author + title */}
      <div className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent p-4 pr-20">
        <Link to={`/users/${video.userId}`} className="flex items-center gap-2">
          <Avatar url={author?.avatarUrl ?? null} name={author?.username ?? "?"} size="h-9 w-9" />
          <span className="text-sm font-semibold text-white">{author?.username ?? "Unknown"}</span>
        </Link>
        <p className="mt-2 line-clamp-2 text-sm text-white/90">{video.title}</p>
      </div>

      {/* Right-side icon rail */}
      <div className="absolute bottom-4 right-3 flex flex-col items-center gap-5">
        <button
          onClick={() => likeMutation.mutate()}
          disabled={!user || likeMutation.isPending}
          className="flex flex-col items-center gap-1 disabled:opacity-60"
        >
          <span
            className={`flex h-11 w-11 items-center justify-center rounded-full backdrop-blur ${
              liked ? "bg-flare-500 text-white" : "bg-black/40 text-white"
            }`}
          >
            <HeartIcon filled={liked} className="h-6 w-6" />
          </span>
          <span className="text-xs font-medium text-white drop-shadow">
            {formatCount(likesQuery.data ?? video.likesCount)}
          </span>
        </button>

        <button onClick={() => onOpenComments(video.id)} className="flex flex-col items-center gap-1">
          <span className="flex h-11 w-11 items-center justify-center rounded-full bg-black/40 text-white backdrop-blur">
            <CommentIcon className="h-6 w-6" />
          </span>
          <span className="text-xs font-medium text-white drop-shadow">{formatCount(video.commentsCount)}</span>
        </button>

        <button
          onClick={() => navigator.clipboard?.writeText(`${window.location.origin}/video/${video.id}`)}
          className="flex flex-col items-center gap-1"
        >
          <span className="flex h-11 w-11 items-center justify-center rounded-full bg-black/40 text-white backdrop-blur">
            <ShareIcon className="h-6 w-6" />
          </span>
          <span className="text-xs font-medium text-white drop-shadow">Share</span>
        </button>

        <button
          onClick={() => setIsMuted((m) => !m)}
          className="flex h-11 w-11 items-center justify-center rounded-full bg-black/40 text-white backdrop-blur"
        >
          {isMuted ? <MuteIcon className="h-5 w-5" /> : <UnmuteIcon className="h-5 w-5" />}
        </button>
      </div>
    </div>
  );
}

function HeartIcon({ filled, ...props }: React.SVGProps<SVGSVGElement> & { filled: boolean }) {
  return (
    <svg viewBox="0 0 24 24" fill={filled ? "currentColor" : "none"} stroke="currentColor" strokeWidth="1.8" {...props}>
      <path d="M12 20s-7-4.35-9.5-8.5C.8 8.2 2.2 5 5.5 5c1.9 0 3.3 1 4.5 2.5C11.2 6 12.6 5 14.5 5 17.8 5 19.2 8.2 21.5 11.5 19 15.65 12 20 12 20z" />
    </svg>
  );
}

function CommentIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <path
        d="M21 12a8.5 8.5 0 01-8.5 8.5c-1.2 0-2.3-.25-3.3-.7L3 21l1.3-4.3A8.4 8.4 0 013.5 12 8.5 8.5 0 0112 3.5 8.5 8.5 0 0121 12z"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function ShareIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <path
        d="M4 12v7a1 1 0 001 1h14a1 1 0 001-1v-7M16 6l-4-4-4 4M12 2v14"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function MuteIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <path d="M11 5 6 9H3v6h3l5 4V5z" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M16 9l5 6M21 9l-5 6" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function UnmuteIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <path d="M11 5 6 9H3v6h3l5 4V5z" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M16 8a5 5 0 010 8M19 5a9 9 0 010 14" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
