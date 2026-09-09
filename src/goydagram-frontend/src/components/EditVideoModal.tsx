import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { AxiosError } from "axios";
import { updateVideo } from "@/api/videos";
import type { Video } from "@/types";

interface EditVideoModalProps {
  video: Video;
  onClose: () => void;
}

export function EditVideoModal({ video, onClose }: EditVideoModalProps) {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState(video.title);
  const [description, setDescription] = useState(video.description);
  const [tags, setTags] = useState(video.tags.join(", "));
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: () =>
      updateVideo(video.id, {
        title: title.trim(),
        description: description.trim(),
        tags: tags
          .split(",")
          .map((t) => t.trim())
          .filter(Boolean),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["video", video.id] });
      queryClient.invalidateQueries({ queryKey: ["my-videos"] });
      onClose();
    },
    onError: (err) => {
      const message =
        err instanceof AxiosError
          ? err.response?.data?.message ?? "Couldn't save changes"
          : "Something went wrong";
      setError(message);
    },
  });

  return (
    <div className="fixed inset-0 z-30 flex items-center justify-center p-4">
      <button aria-label="Close" onClick={onClose} className="absolute inset-0 bg-black/60" />
      <div className="relative z-10 w-full max-w-md rounded-2xl border border-ink-800 bg-ink-950 p-5">
        <h2 className="font-display mb-4 text-lg font-semibold">Edit video</h2>

        <form
          onSubmit={(e) => {
            e.preventDefault();
            mutation.mutate();
          }}
          className="space-y-4"
        >
          <div>
            <label className="mb-1.5 block text-sm font-medium text-ink-200">Title</label>
            <input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              maxLength={100}
              className="w-full rounded-lg border border-ink-700 bg-ink-900 px-3 py-2 text-sm focus:border-mint-400 focus:outline-none"
            />
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-medium text-ink-200">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={4}
              className="w-full resize-none rounded-lg border border-ink-700 bg-ink-900 px-3 py-2 text-sm focus:border-mint-400 focus:outline-none"
            />
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-medium text-ink-200">Tags</label>
            <input
              value={tags}
              onChange={(e) => setTags(e.target.value)}
              className="w-full rounded-lg border border-ink-700 bg-ink-900 px-3 py-2 text-sm placeholder:text-ink-400 focus:border-mint-400 focus:outline-none"
              placeholder="comma, separated, tags"
            />
          </div>

          {error && <p className="text-sm text-flare-400">{error}</p>}

          <div className="flex gap-3 pt-1">
            <button
              type="submit"
              disabled={!title.trim() || mutation.isPending}
              className="rounded-full bg-flare-500 px-5 py-2 text-sm font-semibold text-ink-950 hover:bg-flare-400 transition-colors disabled:opacity-40"
            >
              {mutation.isPending ? "Saving…" : "Save changes"}
            </button>
            <button
              type="button"
              onClick={onClose}
              className="rounded-full border border-ink-700 px-5 py-2 text-sm font-medium text-ink-200 hover:border-ink-600 transition-colors"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
