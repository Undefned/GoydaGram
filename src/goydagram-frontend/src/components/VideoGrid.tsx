import type { Video } from "@/types";
import { VideoCard } from "./VideoCard";

export function VideoGrid({ videos, emptyMessage = "Nothing here yet." }: { videos: Video[]; emptyMessage?: string }) {
  if (videos.length === 0) {
    return <p className="text-sm text-ink-400 py-12 text-center">{emptyMessage}</p>;
  }

  return (
    <div className="grid grid-cols-2 gap-x-4 gap-y-6 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
      {videos.map((video) => (
        <VideoCard key={video.id} video={video} />
      ))}
    </div>
  );
}

export function VideoGridSkeleton({ count = 10 }: { count?: number }) {
  return (
    <div className="grid grid-cols-2 gap-x-4 gap-y-6 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="animate-pulse">
          <div className="aspect-video rounded-xl bg-ink-800" />
          <div className="mt-2 h-3.5 w-4/5 rounded bg-ink-800" />
          <div className="mt-1.5 h-3 w-1/2 rounded bg-ink-800" />
        </div>
      ))}
    </div>
  );
}
