import { api } from "@/lib/api";
import type { Video } from "@/types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";

export async function getVideo(id: string) {
  const { data } = await api.get<Video>(`/api/videos/${id}`);
  return data;
}

export async function getVideosBatch(videoIds: string[]) {
  const { data } = await api.post<Video[]>("/api/videos/batch", { videoIds });
  return data;
}

export async function getTrendingVideos(limit = 30) {
  const { data } = await api.get<Video[]>("/api/videos/trending", { params: { limit } });
  return data;
}

export async function getMyVideos(limit = 30, offset = 0) {
  const { data } = await api.get<{ data: Video[]; pagination: { limit: number; offset: number; total: number } }>(
    "/api/videos/user",
    { params: { limit, offset } }
  );
  return data;
}

export async function getUserVideos(userId: string, limit = 30, offset = 0) {
  const { data } = await api.get<{ data: Video[]; pagination: { limit: number; offset: number } }>(
    `/api/videos/user/${userId}`,
    { params: { limit, offset } }
  );
  return data;
}

export async function uploadVideo(
  input: { title: string; description: string; tags: string[]; file: File },
  onProgress?: (percent: number) => void
) {
  const form = new FormData();
  form.append("Title", input.title);
  form.append("Description", input.description);
  form.append("Tags", input.tags.join(","));
  form.append("File", input.file);

  const { data } = await api.post<Video>("/api/videos/upload", form, {
    headers: { "Content-Type": "multipart/form-data" },
    onUploadProgress: (evt) => {
      if (onProgress && evt.total) onProgress(Math.round((evt.loaded / evt.total) * 100));
    },
  });
  return data;
}

export async function deleteVideo(id: string) {
  const { data } = await api.delete(`/api/videos/${id}`);
  return data;
}

// Stream/preview are public (AllowAnonymous) and served straight off ApiGateway,
// so these are plain URL builders rather than axios calls.
export function streamUrl(path: string) {
  return `${BASE_URL}/api/videos/stream/${path.replace(/^\/+/, "")}`;
}

export function previewUrl(path: string) {
  return `${BASE_URL}/api/videos/stream/preview/${path.replace(/^\/+/, "")}`;
}

// VideoDto.HlsManifestUrl / PreviewUrl may already come back as absolute URLs
// (if the storage service returns them that way) or as bare object keys —
// this normalizes either case to something an <img>/<video> tag can load.
export function resolveMediaUrl(urlOrPath: string, kind: "stream" | "preview") {
  if (/^https?:\/\//i.test(urlOrPath)) return urlOrPath;
  return kind === "stream" ? streamUrl(urlOrPath) : previewUrl(urlOrPath);
}
