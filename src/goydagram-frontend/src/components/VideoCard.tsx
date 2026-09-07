import { Link } from "react-router-dom";
import type { Video } from "@/types";
import { resolveMediaUrl } from "@/api/videos";

function formatDuration(seconds: number) {
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return `${m}:${s.toString().padStart(2, "0")}`;
}

function formatCount(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return `${n}`;
}

export function VideoCard({ video }: { video: Video }) {
  return (
    <Link to={`/video/${video.id}`} className="group block">
      <div className="relative aspect-video overflow-hidden rounded-xl bg-ink-800">
        {video.previewUrl ? (
          <img
            src={resolveMediaUrl(video.previewUrl, "preview")}
            alt={video.title}
            className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
            loading="lazy"
          />
        ) : (
          <div className="h-full w-full flex items-center justify-center text-ink-600">No preview</div>
        )}
        <span className="absolute bottom-2 right-2 rounded bg-ink-950/80 px-1.5 py-0.5 text-xs font-medium text-ink-100">
          {formatDuration(video.duration)}
        </span>
      </div>
      <h3 className="mt-2 line-clamp-2 text-sm font-medium text-ink-100 group-hover:text-mint-400 transition-colors">
        {video.title}
      </h3>
      <p className="mt-1 text-xs text-ink-400">
        {formatCount(video.viewsCount)} views · {formatCount(video.likesCount)} likes
      </p>
    </Link>
  );
}
