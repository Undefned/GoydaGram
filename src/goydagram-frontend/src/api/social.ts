import { api } from "@/lib/api";
import type { Comment } from "@/types";

// -- Comments --------------------------------------------------------------

export async function addComment(input: { videoId: string; userId: string; text: string; parentId?: string }) {
  const { data } = await api.post<{ comment_id: string; message: string }>("/api/comments", {
    video_id: input.videoId,
    user_id: input.userId,
    text: input.text,
    parent_id: input.parentId,
  });
  return data;
}

export async function deleteComment(commentId: string) {
  await api.delete(`/api/comments/${commentId}`);
}

export async function getVideoComments(videoId: string, limit = 20, offset = 0) {
  const { data } = await api.get<{ data: Comment[]; total: number; limit: number; offset: number }>(
    `/api/videos/${videoId}/comments`,
    { params: { limit, offset } }
  );
  return data;
}

export async function getVideoCommentsCount(videoId: string) {
  const { data } = await api.get<{ video_id: string; comments: number }>(
    `/api/videos/${videoId}/comments/count`
  );
  return data.comments;
}

// -- Likes -------------------------------------------------------------

export async function likeVideo(videoId: string, userId: string) {
  await api.post("/api/likes", { video_id: videoId, user_id: userId });
}

export async function unlikeVideo(videoId: string, userId: string) {
  await api.delete("/api/likes", { data: { video_id: videoId, user_id: userId } });
}

export async function getVideoLikesCount(videoId: string) {
  const { data } = await api.get<{ video_id: string; likes: number }>(`/api/videos/${videoId}/likes/count`);
  return data.likes;
}

// -- Views ---------------------------------------------------------------

export async function recordView(videoId: string, userId: string) {
  await api.post("/api/views", { video_id: videoId, user_id: userId });
}
