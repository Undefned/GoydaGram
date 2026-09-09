import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { FeedVideo } from "@/types";
import { resolveMediaUrl, getHlsUrl } from "@/api/videos";
import { getVideoLikesCount, likeVideo, recordView, unlikeVideo } from "@/api/social";
import { HlsPlayer } from "./HlsPlayer";
import { Avatar } from "./Layout";
import { useAuth } from "@/context/AuthContext";
import { useAudioState } from "@/hooks/useAudioState";

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
  const [isPaused, setIsPaused] = useState(false);
  const [showPauseHint, setShowPauseHint] = useState(false);
  const [liked, setLiked] = useState(false);

  const { isMuted, toggleMuted } = useAudioState();

  const likesQuery = useQuery({
    queryKey: ["video-likes", video.id],
    queryFn: () => getVideoLikesCount(video.id),
    placeholderData: 0,
  });

  useEffect(() => {
    setLiked(false);
  }, [video.id]);

  const likeMutation = useMutation({
    mutationFn: () => {
      if (!user) throw new Error("Must be logged in");
      return liked ? unlikeVideo(video.id, user.id) : likeVideo(video.id, user.id);
    },
    onMutate: async () => {
      const newLiked = !liked;
      setLiked(newLiked);
      queryClient.setQueryData(
        ["video-likes", video.id],
        (old: number | undefined) => (old ?? 0) + (newLiked ? 1 : -1)
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["video-likes", video.id] });
    },
    onError: () => {
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
      recordView(video.id, user.id);
    }
  }, [isActive, user, video.id]);

  const handleTap = () => {
    setIsPaused((prev) => !prev);
    setShowPauseHint(true);
    setTimeout(() => setShowPauseHint(false), 500);
  };

  const author = video.user;
  const likesCount = likesQuery.data ?? video.likesCount ?? 0;
  
  const hlsUrl = getHlsUrl(video.hlsManifestUrl);
  const videoSrc = hlsUrl || resolveMediaUrl(video.originalUrl, "stream");

  return (
    <div
      ref={containerRef}
      className="relative flex h-[calc(100vh-7rem)] w-full shrink-0 snap-start snap-always items-center justify-center bg-black md:h-[calc(100vh-7rem)]"
    >
      <div className="relative h-full w-full max-w-[480px] md:rounded-2xl md:overflow-hidden">
        <HlsPlayer
          src={videoSrc}
          poster={video.previewUrl ? resolveMediaUrl(video.previewUrl, "preview") : undefined}
          controls={false}
          muted={isMuted}
          loop
          active={isActive && !isPaused}
          onClick={handleTap}
          className="h-full w-full object-contain"
        />

        {showPauseHint && (
          <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
            <div className="rounded-full bg-black/40 p-4">
              {isPaused ? (
                <svg viewBox="0 0 24 24" fill="white" className="h-10 w-10">
                  <path d="M8 5v14l11-7z" />
                </svg>
              ) : (
                <svg viewBox="0 0 24 24" fill="white" className="h-10 w-10">
                  <rect x="6" y="4" width="4" height="16" />
                  <rect x="14" y="4" width="4" height="16" />
                </svg>
              )}
            </div>
          </div>
        )}

        <div className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/85 via-black/25 to-transparent p-4 pr-20">
          <Link to={`/users/${video.userId}`} className="flex items-center gap-2">
            <Avatar url={author?.avatarUrl ?? null} name={author?.username ?? "?"} size="h-9 w-9" />
            <span className="text-sm font-semibold text-white">{author?.username ?? "Unknown"}</span>
          </Link>
          <p className="mt-2 line-clamp-2 text-sm font-medium text-white">{video.title}</p>
          {video.description && (
            <p className="mt-1 line-clamp-2 text-xs text-white/70">{video.description}</p>
          )}
        </div>

        <div className="absolute bottom-4 right-3 flex flex-col items-center gap-5">
          <button
            onClick={() => likeMutation.mutate()}
            disabled={!user || likeMutation.isPending}
            className="flex flex-col items-center gap-1 disabled:opacity-60"
          >
            <span className="flex h-11 w-11 items-center justify-center rounded-full bg-black/40 backdrop-blur">
              <HeartIcon filled={liked} className={`h-6 w-6 ${liked ? "text-flare-500" : "text-white"}`} />
            </span>
            <span className="text-xs font-medium text-white drop-shadow">
              {formatCount(likesCount)}
            </span>
          </button>

          <button onClick={() => onOpenComments(video.id)} className="flex flex-col items-center gap-1">
            <span className="flex h-11 w-11 items-center justify-center rounded-full bg-black/40 text-white backdrop-blur">
              <CommentIcon className="h-6 w-6" />
            </span>
            <span className="text-xs font-medium text-white drop-shadow">{formatCount(video.commentsCount)}</span>
          </button>

          <button
            onClick={toggleMuted}
            className="flex h-11 w-11 items-center justify-center rounded-full bg-black/40 text-white backdrop-blur"
          >
            {isMuted ? <MuteIcon className="h-5 w-5" /> : <UnmuteIcon className="h-5 w-5" />}
          </button>
        </div>
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