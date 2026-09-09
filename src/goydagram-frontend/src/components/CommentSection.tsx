import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addComment, deleteComment, getVideoComments } from "@/api/social";
import { useAuth } from "@/context/AuthContext";
import { Avatar } from "./Layout";

function timeAgo(iso: string) {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export function CommentSection({ videoId }: { videoId: string }) {
  const { user } = useAuth();
  const [text, setText] = useState("");
  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery({
    queryKey: ["comments", videoId],
    queryFn: () => getVideoComments(videoId, 50, 0),
    placeholderData: { data: [], total: 0, limit: 50, offset: 0 },
  });

  const addMutation = useMutation({
    mutationFn: () => {
      if (!user) throw new Error("Must be logged in");
      return addComment({ videoId, userId: user.id, text: text.trim() });
    },
    onSuccess: () => {
      setText("");
      queryClient.invalidateQueries({ queryKey: ["comments", videoId] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (commentId: string) => deleteComment(commentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["comments", videoId] });
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (text.trim()) addMutation.mutate();
  };

  const comments = Array.isArray(data?.data) ? data.data : [];
  const total = data?.total ?? 0;

  return (
    <div>
      <h2 className="font-display text-base font-semibold mb-4">
        Comments {total > 0 ? `(${total})` : ""}
      </h2>

      {user ? (
        <form onSubmit={handleSubmit} className="mb-6 flex gap-3">
          <Avatar url={user.avatarUrl} name={user.username} size="h-8 w-8" />
          <div className="flex-1">
            <textarea
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder="Add a comment"
              rows={2}
              maxLength={500}
              className="w-full resize-none rounded-lg border border-ink-700 bg-ink-900 px-3 py-2 text-sm placeholder:text-ink-400 focus:border-mint-400 focus:outline-none"
            />
            <div className="mt-2 flex justify-end">
              <button
                type="submit"
                disabled={!text.trim() || addMutation.isPending}
                className="rounded-full bg-flare-500 px-4 py-1.5 text-xs font-semibold text-ink-950 disabled:opacity-40 hover:bg-flare-400 transition-colors"
              >
                Post
              </button>
            </div>
          </div>
        </form>
      ) : (
        <p className="mb-6 text-sm text-ink-400">Log in to leave a comment.</p>
      )}

      {isLoading && <p className="text-sm text-ink-400">Loading comments…</p>}

      {error && (
        <p className="text-sm text-flare-400">Failed to load comments: {String(error)}</p>
      )}

      <ul className="space-y-4">
        {comments.map((comment) => (
          <li key={comment.id} className="flex gap-3">
            <Avatar url={null} name={comment.user_id} size="h-8 w-8" />
            <div className="flex-1">
              <p className="text-sm text-ink-100">{comment.text}</p>
              <div className="mt-1 flex items-center gap-3 text-xs text-ink-400">
                <span>{timeAgo(comment.created_at)}</span>
                {user?.id === comment.user_id && (
                  <button
                    onClick={() => deleteMutation.mutate(comment.id)}
                    className="hover:text-flare-400 transition-colors"
                  >
                    Delete
                  </button>
                )}
              </div>
            </div>
          </li>
        ))}
      </ul>

      {!isLoading && comments.length === 0 && (
        <p className="text-sm text-ink-400">No comments yet — be the first.</p>
      )}
    </div>
  );
}