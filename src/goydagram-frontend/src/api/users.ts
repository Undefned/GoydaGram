import { api } from "@/lib/api";
import type { UpdateProfileInput, User } from "@/types";

export async function getMe() {
  const { data } = await api.get<User>("/api/Users/me");
  return data;
}

export async function updateProfile(input: UpdateProfileInput) {
  const { data } = await api.put<User>("/api/Users/me", input);
  return data;
}

export async function getUser(id: string) {
  const { data } = await api.get<User>(`/api/Users/${id}`);
  return data;
}

export async function getSubscriptions(id: string) {
  const { data } = await api.get<User[]>(`/api/Users/${id}/subscriptions`);
  return data;
}

export async function subscribe(id: string) {
  await api.post(`/api/Users/${id}/subscribe`);
}

export async function unsubscribe(id: string) {
  await api.delete(`/api/Users/${id}/unsubscribe`);
}

export interface Interest {
  tag: string;
  weight: number;
}

export async function getUserInterests(userId: string) {
  const { data } = await api.get<{ user_id: string; interests: Interest[] }>(
    `/api/users/${userId}/interests`
  );
  return data.interests;
}

export async function refreshUserInterests(userId: string) {
  await api.post(`/api/users/${userId}/interests/refresh`);
}