import { api, tokenStore } from "@/lib/api";
import type { AuthResult } from "@/types";

export async function register(input: { username: string; email: string; password: string }) {
  const { data } = await api.post<AuthResult>("/api/auth/register", input);
  tokenStore.set(data.accessToken, data.refreshToken);
  return data;
}

export async function login(input: { email: string; password: string }) {
  const { data } = await api.post<AuthResult>("/api/auth/login", input);
  tokenStore.set(data.accessToken, data.refreshToken);
  return data;
}

export async function logout() {
  const refreshToken = tokenStore.getRefresh();
  try {
    if (refreshToken) {
      await api.post("/api/auth/logout", { refreshToken });
    }
  } finally {
    tokenStore.clear();
  }
}
