import { useState, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { uploadVideo } from "@/api/videos";

export function UploadForm() {
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [tags, setTags] = useState("");
  const [progress, setProgress] = useState(0);

  const mutation = useMutation({
    mutationFn: () => {
      if (!file) throw new Error("Pick a video file first");
      return uploadVideo(
        {
          title,
          description,
          tags: tags
            .split(",")
            .map((t) => t.trim())
            .filter(Boolean),
          file,
        },
        setProgress
      );
    },
    onSuccess: (video) => navigate(`/video/${video.id}`),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (file && title.trim()) mutation.mutate();
  };

  return (
    <form onSubmit={handleSubmit} className="max-w-xl space-y-5">
      <button
        type="button"
        onClick={() => fileInputRef.current?.click()}
        className="flex w-full flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-ink-700 bg-ink-900 py-12 text-sm text-ink-400 hover:border-mint-400 hover:text-ink-100 transition-colors"
      >
        {file ? (
          <span className="text-ink-100">{file.name}</span>
        ) : (
          <>
            <span className="font-medium">Choose a video file</span>
            <span className="text-xs">MP4, MOV, or WebM</span>
          </>
        )}
      </button>
      <input
        ref={fileInputRef}
        type="file"
        accept="video/*"
        className="hidden"
        onChange={(e) => setFile(e.target.files?.[0] ?? null)}
      />

      <div>
        <label className="mb-1.5 block text-sm font-medium text-ink-200">Title</label>
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          maxLength={100}
          className="w-full rounded-lg border border-ink-700 bg-ink-900 px-3 py-2 text-sm placeholder:text-ink-400 focus:border-mint-400 focus:outline-none"
          placeholder="Give it a title"
        />
      </div>

      <div>
        <label className="mb-1.5 block text-sm font-medium text-ink-200">Description</label>
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={4}
          className="w-full resize-none rounded-lg border border-ink-700 bg-ink-900 px-3 py-2 text-sm placeholder:text-ink-400 focus:border-mint-400 focus:outline-none"
          placeholder="What's this video about?"
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

      {mutation.isPending && (
        <div className="h-1.5 w-full overflow-hidden rounded-full bg-ink-800">
          <div className="h-full bg-mint-400 transition-all" style={{ width: `${progress}%` }} />
        </div>
      )}

      {mutation.isError && (
        <p className="text-sm text-flare-400">Upload failed. Check the file and try again.</p>
      )}

      <button
        type="submit"
        disabled={!file || !title.trim() || mutation.isPending}
        className="rounded-full bg-flare-500 px-6 py-2.5 text-sm font-semibold text-ink-950 disabled:opacity-40 hover:bg-flare-400 transition-colors"
      >
        {mutation.isPending ? `Uploading… ${progress}%` : "Publish"}
      </button>
    </form>
  );
}
