import { useQuery } from "@tanstack/react-query";
import { getTrendingVideos } from "@/api/videos";
import { VideoGrid, VideoGridSkeleton } from "@/components/VideoGrid";

export function TrendingPage() {
  const { data, isLoading } = useQuery({
    queryKey: ["trending"],
    queryFn: () => getTrendingVideos(50),
  });

  return (
    <div>
      <h1 className="font-display mb-6 text-xl font-semibold">Trending</h1>
      {isLoading ? <VideoGridSkeleton /> : <VideoGrid videos={data ?? []} emptyMessage="Nothing trending right now." />}
    </div>
  );
}
