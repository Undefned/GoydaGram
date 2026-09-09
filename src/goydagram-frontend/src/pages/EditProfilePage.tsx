import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { AxiosError } from "axios";
import { updateProfile } from "@/api/users";
import { useAuth } from "@/context/AuthContext";
import { Avatar } from "@/components/Layout";

export function EditProfilePage() {
  const { user, refetchUser } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState(user?.username ?? "");
  const [avatarUrl, setAvatarUrl] = useState(user?.avatarUrl ?? "");
  const [bio, setBio] = useState(user?.bio ?? "");
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: () =>
      updateProfile({
        username: username.trim() || undefined,
        avatarUrl: avatarUrl.trim() || undefined,
        bio: bio.trim(),
      }),
    onSuccess: async () => {
      await refetchUser();
      navigate("/profile");
    },
    onError: (err) => {
      const message =
        err instanceof AxiosError
          ? err.response?.data?.message ?? "Couldn't save your profile"
          : "Something went wrong";
      setError(message);
    },
  });

  if (!user) return null;

  return (
    <div className="mx-auto max-w-md">
      <h1 className="font-display mb-6 text-xl font-semibold">Edit profile</h1>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          mutation.mutate();
        }}
        className="space-y-5"
      >
        <div className="flex items-center gap-4">
          <Avatar url={avatarUrl || null} name={username || user.username} size="h-16 w-16" />
          <div className="flex-1">
            <label className="mb-1.5 block text-sm font-medium text-ink-200">Avatar URL</label>
            <input
              value={avatarUrl}
              onChange={(e) => setAvatarUrl(e.target.value)}
              placeholder="https://…"
              className="w-full rounded-lg border border-ink-700 bg-ink-900 px-3 py-2 text-sm placeholder:text-ink-400 focus:border-mint-400 focus:outline-none"
            />
          </div>
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-medium text-ink-200">Username</label>
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            minLength={3}
            className="w-full rounded-lg border border-ink-700 bg-ink-900 px-3 py-2 text-sm focus:border-mint-400 focus:outline-none"
          />
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-medium text-ink-200">Bio</label>
          <textarea
            value={bio}
            onChange={(e) => setBio(e.target.value)}
            rows={3}
            maxLength={280}
            className="w-full resize-none rounded-lg border border-ink-700 bg-ink-900 px-3 py-2 text-sm placeholder:text-ink-400 focus:border-mint-400 focus:outline-none"
            placeholder="Tell people about yourself"
          />
        </div>

        {error && <p className="text-sm text-flare-400">{error}</p>}

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={mutation.isPending}
            className="rounded-full bg-flare-500 px-5 py-2 text-sm font-semibold text-ink-950 hover:bg-flare-400 transition-colors disabled:opacity-40"
          >
            {mutation.isPending ? "Saving…" : "Save changes"}
          </button>
          <button
            type="button"
            onClick={() => navigate("/profile")}
            className="rounded-full border border-ink-700 px-5 py-2 text-sm font-medium text-ink-200 hover:border-ink-600 transition-colors"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
