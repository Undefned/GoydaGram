import { CommentSection } from "./CommentSection";

interface CommentsDrawerProps {
  videoId: string | null;
  onClose: () => void;
}

export function CommentsDrawer({ videoId, onClose }: CommentsDrawerProps) {
  if (!videoId) return null;

  return (
    <div className="fixed inset-0 z-30 flex items-end justify-center">
      <button aria-label="Close comments" onClick={onClose} className="absolute inset-0 bg-black/60" />
      <div className="relative z-10 max-h-[75vh] w-full max-w-lg overflow-y-auto rounded-t-2xl border-t border-ink-800 bg-ink-950 p-4 pb-8">
        <div className="mx-auto mb-3 h-1 w-10 rounded-full bg-ink-700" />
        <CommentSection videoId={videoId} />
      </div>
    </div>
  );
}
