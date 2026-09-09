import { api } from "@/lib/api";
import type { Comment } from "@/types";

// -- Comments --------------------------------------------------------------

export async function addComment(input: { videoId: string; userId: string; text: string; parentId?: string }) {
  try {
    const { data } = await api.post<{ comment_id: string; message: string }>("/api/comments", {
      video_id: input.videoId,
      user_id: input.userId,
      text: input.text,
      parent_id: input.parentId,
    });
    return data;
  } catch (error) {
    console.error("Failed to add comment:", error);
    throw error;
  }
}

export async function deleteComment(commentId: string) {
  try {
    await api.delete(`/api/comments/${commentId}`);
  } catch (error) {
    console.error("Failed to delete comment:", error);
    throw error;
  }
}

export async function getVideoComments(videoId: string, limit = 20, offset = 0) {
  try {
    console.log(`Fetching comments for video ${videoId}`);
    const { data } = await api.get<{ 
      success: boolean; 
      data: { 
        data: Comment[]; 
        total: number; 
        limit: number; 
        offset: number 
      } 
    }>(
      `/api/videos/${videoId}/comments`,
      { params: { limit, offset } }
    );
    console.log("Comments response:", data);
    
    // ✅ Правильная структура ответа от SocialService
    const responseData = data.data || data; // fallback если обертки нет
    const comments = responseData.data ?? [];
    const total = responseData.total ?? 0;
    const actualLimit = responseData.limit ?? limit;
    const actualOffset = responseData.offset ?? offset;
    
    return {
      data: Array.isArray(comments) ? comments : [],
      total: total,
      limit: actualLimit,
      offset: actualOffset,
    };
  } catch (error) {
    console.error("Failed to fetch comments:", error);
    return { data: [], total: 0, limit, offset };
  }
}

export async function getVideoCommentsCount(videoId: string) {
  try {
    const { data } = await api.get<{ video_id: string; comments: number }>(
      `/api/videos/${videoId}/comments/count`
    );
    return data.comments ?? 0;
  } catch (error) {
    console.error("Failed to fetch comments count:", error);
    return 0;
  }
}

// -- Likes -------------------------------------------------------------

export async function likeVideo(videoId: string, userId: string) {
  try {
    await api.post("/api/likes", { video_id: videoId, user_id: userId });
  } catch (error) {
    console.error("Failed to like video:", error);
    throw error;
  }
}

export async function unlikeVideo(videoId: string, userId: string) {
  try {
    await api.delete("/api/likes", { data: { video_id: videoId, user_id: userId } });
  } catch (error) {
    console.error("Failed to unlike video:", error);
    throw error;
  }
}

export async function getVideoLikesCount(videoId: string) {
  try {
    const { data } = await api.get<{ video_id: string; likes: number }>(`/api/videos/${videoId}/likes/count`);
    return data.likes ?? 0;
  } catch (error) {
    console.error("Failed to fetch likes count:", error);
    return 0;
  }
}

// -- Views ---------------------------------------------------------------

export async function recordView(videoId: string, userId: string) {
  try {
    await api.post("/api/views", { video_id: videoId, user_id: userId });
  } catch (error) {
    console.error("Failed to record view:", error);
  }
}