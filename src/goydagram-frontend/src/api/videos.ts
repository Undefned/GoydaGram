import { api } from "@/lib/api";
import type { UpdateVideoInput, Video } from "@/types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

export async function getVideo(id: string) {
  const { data } = await api.get<Video>(`/api/Videos/${id}`);
  return data;
}

export async function getVideosBatch(videoIds: string[]) {
  const { data } = await api.post<Video[]>("/api/Videos/batch", { videoIds });
  return data;
}

export async function getTrendingVideos(limit = 30) {
  const { data } = await api.get<Video[]>("/api/Videos/trending", { params: { limit } });
  return data;
}

export async function getMyVideos(limit = 30, offset = 0) {
  try {
    const { data } = await api.get<{ data: Video[]; pagination: { limit: number; offset: number; total: number } }>(
      "/api/Videos/user",
      { params: { limit, offset } }
    );
    return data;
  } catch (error) {
    console.error("Failed to fetch my videos:", error);
    return { data: [], pagination: { limit, offset, total: 0 } };
  }
}

export async function getUserVideos(userId: string, limit = 30, offset = 0) {
  const { data } = await api.get<{ data: Video[]; pagination: { limit: number; offset: number } }>(
    `/api/Videos/user/${userId}`,
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

  const { data } = await api.post<Video>("/api/Videos/upload", form, {
    headers: { "Content-Type": "multipart/form-data" },
    onUploadProgress: (evt) => {
      if (onProgress && evt.total) onProgress(Math.round((evt.loaded / evt.total) * 100));
    },
  });
  return data;
}

export async function updateVideo(id: string, input: UpdateVideoInput) {
  const { data } = await api.put<Video>(`/api/Videos/${id}`, input);
  return data;
}

export async function deleteVideo(id: string) {
  const { data } = await api.delete(`/api/Videos/${id}`);
  return data;
}

export function streamUrl(path: string) {
  const cleanPath = path.replace(/^\/+/, "");
  return `${BASE_URL}/api/videos/stream/${cleanPath}`;
}

export function previewUrl(path: string) {
  const cleanPath = path.replace(/^\/+/, "");
  return `${BASE_URL}/api/videos/stream/preview/${cleanPath}`;
}

export function resolveMediaUrl(urlOrPath: string, kind: "stream" | "preview" | "hls" = "stream") {
  if (/^https?:\/\//i.test(urlOrPath)) {
    return urlOrPath;
  }
  
  if (urlOrPath.startsWith("/api/")) {
    return `${BASE_URL}${urlOrPath}`;
  }
  
  if (kind === "hls" || urlOrPath.includes("/hls/") || urlOrPath.includes("playlist.m3u8")) {
    const cleanPath = urlOrPath.replace(/^\/+/, "");
    return `${BASE_URL}/api/videos/stream/${cleanPath}`;
  }
  
  if (kind === "preview" || urlOrPath.includes("preview") || urlOrPath.endsWith(".jpg")) {
    return previewUrl(urlOrPath);
  }
  
  return streamUrl(urlOrPath);
}

export function getHlsUrl(hlsManifestUrl: string | null | undefined): string | null {
  if (!hlsManifestUrl) return null;
  
  if (/^https?:\/\//i.test(hlsManifestUrl)) {
    return hlsManifestUrl;
  }
  
  if (hlsManifestUrl.startsWith("/api/")) {
    return `${BASE_URL}${hlsManifestUrl}`;
  }
  
  const cleanPath = hlsManifestUrl.replace(/^\/+/, "");
  
  if (cleanPath.startsWith("hls/")) {
    return `${BASE_URL}/api/videos/stream/${cleanPath}`;
  }
  
  return `${BASE_URL}/api/videos/stream/hls/${cleanPath}`;
}