import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import type { AuthResult } from "@/types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

const ACCESS_TOKEN_KEY = "goydagram_access_token";
const REFRESH_TOKEN_KEY = "goydagram_refresh_token";

export const tokenStore = {
  getAccess: () => localStorage.getItem(ACCESS_TOKEN_KEY),
  getRefresh: () => localStorage.getItem(REFRESH_TOKEN_KEY),
  set: (accessToken: string, refreshToken: string) => {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  },
  clear: () => {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  },
};

export const api = axios.create({
  baseURL: BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ✅ Добавляем токен в каждый запрос
api.interceptors.request.use(
  (config) => {
    const token = tokenStore.getAccess();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Single-flight refresh
let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = tokenStore.getRefresh();
  if (!refreshToken) return null;

  if (!refreshPromise) {
    refreshPromise = axios
      .post<AuthResult>(`${BASE_URL}/api/Auth/refresh`, { refreshToken })
      .then((res) => {
        tokenStore.set(res.data.accessToken, res.data.refreshToken);
        return res.data.accessToken;
      })
      .catch((error) => {
        console.error("Refresh token failed:", error);
        tokenStore.clear();
        return null;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }
  return refreshPromise;
}

api.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined;

    // Не пытаемся рефрешить на эндпоинтах авторизации
    const isAuthEndpoint = original?.url?.includes("/api/Auth/login") || 
                           original?.url?.includes("/api/Auth/register") ||
                           original?.url?.includes("/api/Auth/refresh");

    if (error.response?.status === 401 && original && !original._retried && !isAuthEndpoint) {
      original._retried = true;
      const newToken = await refreshAccessToken();
      if (newToken) {
        original.headers = original.headers ?? {};
        (original.headers as Record<string, string>).Authorization = `Bearer ${newToken}`;
        return api(original);
      }
      // Refresh failed — force a clean login.
      tokenStore.clear();
      window.location.assign("/login");
      return Promise.reject(error);
    }

    return Promise.reject(error);
  }
);

api.interceptors.request.use(
  (config) => {
    const token = tokenStore.getAccess();
    console.log('🔑 Sending request to:', config.url, 'Token:', token ? '✅ Present' : '❌ Missing');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);