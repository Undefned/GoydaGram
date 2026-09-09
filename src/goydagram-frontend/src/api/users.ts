import { api } from "@/lib/api";
import type { UpdateProfileInput, User } from "@/types";

export async function getMe() {
  const { data } = await api.get<User>("/api/users/me");
  return data;
}

// Requires PUT /api/users/me on UserService — not present in the current
// backend (User.UpdateProfile exists on the domain entity but nothing wires
// it to a command/endpoint yet). See the README for the handler + route to add.
export async function updateProfile(input: UpdateProfileInput) {
  const { data } = await api.put<User>("/api/users/me", input);
  return data;
}

export async function getUser(id: string) {
  const { data } = await api.get<User>(`/api/users/${id}`);
  return data;
}

export async function getSubscriptions(id: string) {
  const { data } = await api.get<User[]>(`/api/users/${id}/subscriptions`);
  return data;
}

export async function subscribe(id: string) {
  await api.post(`/api/users/${id}/subscribe`);
}

export async function unsubscribe(id: string) {
  await api.delete(`/api/users/${id}/unsubscribe`);
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
