Все необходимые компоненты на месте:
- ✅ **API Gateway** — единая точка входа
- ✅ **JWT-аутентификация** — готова
- ✅ **Все эндпоинты** — описаны и работают
- ✅ **Swagger/OpenAPI** — доступен для каждого сервиса
- ✅ **CORS** — настроен на Gateway
- ✅ **Docker** — всё поднимается одной командой

---

# 📱 ПОЛНАЯ ДОКУМЕНТАЦИЯ ДЛЯ ФРОНТЕНД-РАЗРАБОТЧИКА

## 1. Общая архитектура

### 1.1. Базовый URL
```
API Gateway: http://localhost:8080
```

Все запросы от фронтенда идут **ТОЛЬКО** на API Gateway. Он проксирует их в нужные микросервисы.

### 1.2. Структура URL
```
http://localhost:8080/{префикс}/{эндпоинт}
```

**Префиксы:**
| Префикс | Назначение |
|---------|------------|
| `/api/auth` | Аутентификация и регистрация |
| `/api/users` | Пользователи и подписки |
| `/api/videos` | Видео (получение, загрузка) |
| `/api/stream` | Стриминг видео (HLS/MP4) |
| `/api/social` | Лайки, комментарии, просмотры |
| `/api/feed` | Лента и тренды |
| `/api/search` | Поиск |

---

## 2. Аутентификация и авторизация

### 2.1. JWT Токены
Система использует **два типа токенов**:

| Токен | Живет | Назначение |
|-------|-------|------------|
| **Access Token** | 60 минут | Для всех API-запросов |
| **Refresh Token** | 30 дней | Для обновления Access Token |

### 2.2. Как отправлять токены
**Для всех защищенных эндпоинтов (кроме /auth):**
```http
Authorization: Bearer {access_token}
```

**Пример заголовка:**
```javascript
headers: {
  'Authorization': `Bearer ${accessToken}`,
  'Content-Type': 'application/json',
  'X-Correlation-ID': 'uuid-for-tracing' // опционально
}
```

### 2.3. Получение токенов

**Регистрация:**
```http
POST /api/auth/register
```

**Тело запроса:**
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Ответ:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "john_doe",
  "email": "john@example.com",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "abc123xyz..."
}
```

---

**Логин:**
```http
POST /api/auth/login
```

**Тело запроса:**
```json
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Ответ:** Аналогичный регистрации

---

**Обновление Access Token:**
```http
POST /api/auth/refresh
```

**Тело запроса:**
```json
{
  "refreshToken": "abc123xyz..."
}
```

**Ответ:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "def456uvw..." // НОВЫЙ refresh токен (ротация!)
}
```

---

**Выход (Logout):**
```http
POST /api/auth/logout
```

**Тело запроса:**
```json
{
  "refreshToken": "abc123xyz..."
}
```

**Ответ:** `204 No Content`

---

## 3. Эндпоинты для фронтенда

### 3.1. Пользователи (`/api/users`)

**Получить текущего пользователя:**
```
GET /api/users/me
```
**Headers:** `Authorization: Bearer {token}`
**Ответ:** `UserDto`

**Получить пользователя по ID:**
```
GET /api/users/{userId}
```
**Ответ:** `UserDto`

**Получить подписки пользователя:**
```
GET /api/users/{userId}/subscriptions
```
**Ответ:** `UserDto[]`

**Подписаться на пользователя:**
```
POST /api/users/{userId}/subscribe
```
**Headers:** `Authorization: Bearer {token}`
**Ответ:** `204 No Content`

**Отписаться от пользователя:**
```
DELETE /api/users/{userId}/unsubscribe
```
**Headers:** `Authorization: Bearer {token}`
**Ответ:** `204 No Content`

---

### 3.2. Видео (`/api/videos`)

**Получить видео по ID:**
```
GET /api/videos/{videoId}
```
**Ответ:** `VideoDto`

**Получить несколько видео (батч):**
```
POST /api/videos/batch
```
**Тело:**
```json
{
  "videoIds": ["id1", "id2", "id3"]
}
```
**Ответ:** `VideoDto[]`

**Получить тренды:**
```
GET /api/videos/trending?limit=30
```
**Ответ:** `VideoDto[]`

**Получить видео пользователя:**
```
GET /api/videos/user?limit=30&offset=0
```
**Headers:** `Authorization: Bearer {token}`
**Ответ:**
```json
{
  "data": [VideoDto],
  "pagination": {
    "limit": 30,
    "offset": 0,
    "total": 42
  }
}
```

**Загрузить видео:**
```
POST /api/videos/upload
```
**Headers:** `Authorization: Bearer {token}`
**Тип:** `multipart/form-data`
**Поля:**
| Поле | Тип | Описание |
|------|-----|----------|
| `file` | File | Видеофайл (MP4) |
| `title` | string | Название |
| `description` | string | Описание |
| `tags` | string | Теги через запятую: "funny,music,dance" |

**Ответ:**
```json
{
  "videoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "url": "/api/videos/stream/videos/user123/video.mp4",
  "previewUrl": "/images/default-preview.jpg",
  "status": "Ready"
}
```

**Удалить видео (только свой):**
```
DELETE /api/videos/{videoId}
```
**Headers:** `Authorization: Bearer {token}`
**Ответ:** `{ "success": true, "message": "..." }`

---

### 3.3. Стриминг видео (`/api/stream`)

**Простой MP4:**
```
GET /api/videos/stream/videos/{userId}/{videoId}.mp4
```
**Примечание:** поддерживает `Range` заголовки для перемотки.

**HLS плейлист:**
```
GET /api/videos/stream/hls/{userId}/{videoId}/playlist.m3u8
```

**HLS сегменты:**
```
GET /api/videos/stream/hls/{userId}/{videoId}/segment_001.ts
```

**Превью:**
```
GET /api/videos/stream/preview/previews/{userId}/{videoId}.jpg
```

---

### 3.4. Социальное взаимодействие (`/api/social`)

**Поставить лайк:**
```
POST /api/likes
```
**Тело:**
```json
{
  "videoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Убрать лайк:**
```
DELETE /api/likes
```
**Тело:** аналогично

**Получить лайки видео:**
```
GET /api/videos/{videoId}/likes?limit=20&offset=0
```
**Ответ:** `{ data: Like[], total: 42 }`

**Получить количество лайков:**
```
GET /api/videos/{videoId}/likes/count
```
**Ответ:** `{ videoId: "...", likes: 42 }`

---

**Добавить комментарий:**
```
POST /api/comments
```
**Тело:**
```json
{
  "videoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "text": "Awesome video!",
  "parentId": null // опционально, для ответов
}
```

**Удалить комментарий:**
```
DELETE /api/comments/{commentId}
```

**Получить комментарии видео:**
```
GET /api/videos/{videoId}/comments?limit=20&offset=0
```

**Количество комментариев:**
```
GET /api/videos/{videoId}/comments/count
```

---

**Записать просмотр:**
```
POST /api/views
```
**Тело:**
```json
{
  "videoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Количество просмотров:**
```
GET /api/videos/{videoId}/views/count
```
**Ответ:** `{ videoId: "...", views: 1337 }`

---

### 3.5. Лента (`/api/feed`)

**Получить ленту:**
```
GET /api/feed?offset=0&limit=30&seen=id1,id2,id3&prefetch=false
```
**Headers:** `Authorization: Bearer {token}`

**Параметры:**
| Параметр | Описание |
|----------|----------|
| `offset` | Смещение (пагинация) |
| `limit` | Количество (макс 50) |
| `seen` | ID просмотренных видео (чтобы не показывать повторно) |
| `prefetch=true` | Запустить предзагрузку в фоне (вернется сразу) |

**Ответ:**
```json
{
  "videos": [VideoDto],
  "nextOffset": 30,
  "hasMore": true,
  "totalCount": 157
}
```

**Получить тренды:**
```
GET /api/feed/trending?limit=30
```
**Ответ:** `{ videos: VideoDto[] }`

**Предзагрузка ленты (кеширование):**
```
GET /api/feed/prefetch?offset=0&seen=id1,id2
```
**Headers:** `Authorization: Bearer {token}`

---

### 3.6. Поиск (`/api/search`)

**Поиск видео:**
```
GET /api/search/videos?q=funny%20cats&limit=30&offset=0&user_id=&tags=
```
**Параметры:**
| Параметр | Описание |
|----------|----------|
| `q` | Поисковый запрос |
| `limit` | Макс 100 |
| `offset` | Смещение |
| `user_id` | Фильтр по пользователю |
| `tags` | Фильтр по тегам: "funny,music" |

**Ответ:**
```json
{
  "videos": [VideoDocument],
  "total": 42,
  "offset": 0,
  "limit": 30
}
```

**Поиск пользователей:**
```
GET /api/search/users?q=john&limit=30&offset=0
```
**Ответ:**
```json
{
  "users": [UserDocument],
  "total": 5,
  "offset": 0,
  "limit": 30
}
```

**Рекомендации (для ленты):**
```
POST /api/search/recommendations
```
**Тело:**
```json
{
  "interests": [
    { "tag": "funny", "weight": 0.8 },
    { "tag": "music", "weight": 0.6 }
  ],
  "limit": 30
}
```
**Ответ:**
```json
{
  "video_ids": ["id1", "id2", "id3"]
}
```

---

## 4. Модели данных (DTO)

### 4.1. UserDto
```typescript
interface UserDto {
  id: string;                  // UUID
  username: string;
  email: string;
  avatarUrl: string | null;
  bio: string | null;
  isVerified: boolean;
  followersCount: number;
  followingCount: number;
  createdAt: string;           // ISO 8601
  role: "User" | "Admin";      // только в UserService
}
```

### 4.2. VideoDto
```typescript
interface VideoDto {
  id: string;
  userId: string;
  title: string;
  description: string;
  duration: number;            // секунды
  originalUrl: string;         // MP4
  hlsManifestUrl: string;      // HLS плейлист
  previewUrl: string;          // Превью
  status: "Processing" | "Ready" | "Failed" | "Blocked";
  viewsCount: number;
  likesCount: number;
  commentsCount: number;
  createdAt: string;
  tags: string[];
}
```

### 4.3. VideoDocument (для поиска)
```typescript
interface VideoDocument {
  id: string;
  userId: string;
  title: string;
  description: string;
  duration: number;
  url: string;
  previewUrl: string;
  username: string;            // Имя автора (для поиска)
  userAvatarUrl: string | null;
  tags: string[];
  likesCount: number;
  commentsCount: number;
  viewsCount: number;
  engagementScore: number;
  isVerified: boolean;
  createdAt: string;
}
```

---

## 5. Ошибки и коды ответов

### 5.1. Формат ошибки
```json
{
  "error": "Сообщение об ошибке",
  "correlationId": "uuid-for-debugging"
}
```

### 5.2. HTTP коды
| Код | Смысл |
|-----|-------|
| 200 | Успешно |
| 204 | Успешно, но нет контента (DELETE) |
| 400 | Ошибка валидации |
| 401 | Не авторизован (нет/плохой токен) |
| 403 | Нет прав |
| 404 | Ресурс не найден |
| 409 | Конфликт (уже существует) |
| 500 | Внутренняя ошибка сервера |

### 5.3. Типичные ошибки
```json
{ "error": "Invalid email or password" }
{ "error": "Email already registered" }
{ "error": "Username already taken" }
{ "error": "Video not found" }
{ "error": "You don't have permission to delete this video" }
{ "error": "Invalid or expired refresh token" }
```

---

## 6. Health Checks (для мониторинга)
```
GET /health
GET /healthz
GET /metrics  // Prometheus метрики
```

---

## 7. Swagger UI
Каждый сервис имеет свой Swagger:

| Сервис | Swagger URL |
|--------|-------------|
| API Gateway | `http://localhost:8080/swagger` |
| User Service | `http://localhost:5001/swagger` |
| Content Service | `http://localhost:5002/swagger` |
| Social Service | `http://localhost:5003/swagger` |
| Feed Service | `http://localhost:5004/swagger` |
| Search Service | `http://localhost:8000/docs` (FastAPI) |

---

## 8. Примеры для популярных фреймворков

### 8.1. React + Axios
```javascript
import axios from 'axios';

const API = axios.create({
  baseURL: 'http://localhost:8080',
});

// Перехватчик для добавления токена
API.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Перехватчик для обновления токена
API.interceptors.response.use(
  response => response,
  async error => {
    if (error.response?.status === 401) {
      const refreshToken = localStorage.getItem('refreshToken');
      try {
        const { data } = await axios.post('http://localhost:8080/api/auth/refresh', {
          refreshToken
        });
        localStorage.setItem('accessToken', data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
        // Повторяем запрос
        error.config.headers.Authorization = `Bearer ${data.accessToken}`;
        return axios(error.config);
      } catch {
        // Refresh токен протух — редирект на логин
        localStorage.clear();
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

// Пример запроса
export const feedApi = {
  getFeed: (offset = 0, limit = 30, seen = []) =>
    API.get('/api/feed', { params: { offset, limit, seen: seen.join(',') } }),
  
  getTrending: (limit = 30) =>
    API.get('/api/feed/trending', { params: { limit } }),
  
  uploadVideo: (formData) =>
    API.post('/api/videos/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    }),
};

export const authApi = {
  login: (email, password) =>
    API.post('/api/auth/login', { email, password }),
  
  register: (username, email, password) =>
    API.post('/api/auth/register', { username, email, password }),
  
  logout: (refreshToken) =>
    API.post('/api/auth/logout', { refreshToken }),
};
```

---

### 8.2. Vue + Pinia
```javascript
// stores/auth.js
import { defineStore } from 'pinia';
import axios from 'axios';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null,
    accessToken: localStorage.getItem('accessToken'),
    refreshToken: localStorage.getItem('refreshToken'),
  }),
  
  actions: {
    async login(email, password) {
      const { data } = await axios.post('/api/auth/login', { email, password });
      this.accessToken = data.accessToken;
      this.refreshToken = data.refreshToken;
      localStorage.setItem('accessToken', data.accessToken);
      localStorage.setItem('refreshToken', data.refreshToken);
      await this.fetchMe();
    },
    
    async refreshToken() {
      const { data } = await axios.post('/api/auth/refresh', {
        refreshToken: this.refreshToken
      });
      this.accessToken = data.accessToken;
      this.refreshToken = data.refreshToken;
      localStorage.setItem('accessToken', data.accessToken);
      localStorage.setItem('refreshToken', data.refreshToken);
    },
  },
});
```

---

### 8.3. Flutter/Dart
```dart
class ApiClient {
  static const baseUrl = 'http://localhost:8080';
  final Dio dio = Dio(BaseOptions(baseUrl: baseUrl));

  ApiClient() {
    dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) {
        final token = storage.read('accessToken');
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        return handler.next(options);
      },
      onError: (e, handler) async {
        if (e.response?.statusCode == 401) {
          final refreshToken = storage.read('refreshToken');
          if (refreshToken != null) {
            try {
              final response = await dio.post('/api/auth/refresh', data: {
                'refreshToken': refreshToken
              });
              final newToken = response.data['accessToken'];
              final newRefresh = response.data['refreshToken'];
              storage.write('accessToken', newToken);
              storage.write('refreshToken', newRefresh);
              e.requestOptions.headers['Authorization'] = 'Bearer $newToken';
              return handler.resolve(await dio.request(
                e.requestOptions.path,
                options: e.requestOptions,
              ));
            } catch (_) {
              storage.deleteAll();
              // Редирект на логин
            }
          }
        }
        return handler.next(e);
      }
    ));
  }
}
```

---

### 8.4. iOS/Swift
```swift
class APIClient {
    static let shared = APIClient()
    private let baseURL = "http://localhost:8080"
    private let session = URLSession.shared
    
    func request<T: Decodable>(
        _ endpoint: String,
        method: String = "GET",
        body: Data? = nil,
        auth: Bool = true
    ) async throws -> T {
        guard let url = URL(string: baseURL + endpoint) else {
            throw APIError.invalidURL
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        
        if auth, let token = Keychain.shared.getAccessToken() {
            request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }
        
        request.httpBody = body
        
        let (data, response) = try await session.data(for: request)
        
        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }
        
        if httpResponse.statusCode == 401 {
            // Обновляем токен
            try await refreshToken()
            return try await request(endpoint, method: method, body: body, auth: auth)
        }
        
        return try JSONDecoder().decode(T.self, from: data)
    }
}
```

---

## 9. Важные моменты

### 9.1. Refresh Token Ротация
При каждом обновлении Access Token вы получаете **НОВЫЙ** Refresh Token. Старый становится недействительным.

### 9.2. Видео после загрузки
- Сразу доступен MP4
- HLS генерируется **в фоне** (может занять 1-5 минут)
- Статус видео: `"Processing"` → `"Ready"` → `"Failed"`
- Подписывайтесь на WebSocket/SSE или используйте поллинг для отслеживания статуса

### 9.3. Пагинация
Все списки используют `offset/limit`:
```javascript
{
  data: [...],
  pagination: {
    limit: 30,
    offset: 0,
    total: 42
  }
}
```

### 9.4. Correlation ID
Каждый запрос может содержать заголовок `X-Correlation-ID`. Если его нет, сервер создаст свой. Это помогает отслеживать запросы через все микросервисы — полезно для отладки.

### 9.5. CORS
Настроен на Gateway: `AllowAnyOrigin, AllowAnyMethod, AllowAnyHeader`.

### 9.6. Временные форматы
Все даты — **ISO 8601**: `2024-01-15T12:30:00Z`

---

## 10. Быстрый старт

### 10.1. Поднять все сервисы
```bash
docker-compose up -d
```

### 10.2. Проверить доступность
```bash
curl http://localhost:8080/health
```

### 10.3. Создать пользователя и войти
```bash
# Регистрация
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@test.com","password":"Test123!"}'

# Логин
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test123!"}'
```

---

## 11. Рекомендации по фронтенду

### 11.1. Структура стейта
```javascript
{
  auth: {
    user: null | UserDto,
    accessToken: null | string,
    refreshToken: null | string,
    isAuthenticated: boolean,
  },
  feed: {
    videos: VideoDto[],
    nextOffset: number,
    hasMore: boolean,
    loading: boolean,
  },
  video: {
    current: null | VideoDto,
    player: { playing: false, time: 0 },
  },
  social: {
    likes: { [videoId]: number },
    comments: { [videoId]: Comment[] },
    views: { [videoId]: number },
  },
  search: {
    query: string,
    results: VideoDto[],
    loading: boolean,
  },
}
```

### 11.2. Типизированные хуки (React)
```typescript
// hooks/useAuth.ts
export const useAuth = () => {
  const login = useMutation({
    mutationFn: (data: LoginData) => api.auth.login(data),
    onSuccess: (data) => {
      setAccessToken(data.accessToken);
      setRefreshToken(data.refreshToken);
      queryClient.invalidateQueries(['user', 'me']);
    },
  });
  
  return { login, logout, refresh };
};

// hooks/useFeed.ts
export const useFeed = (offset = 0, limit = 30) => {
  return useQuery({
    queryKey: ['feed', offset, limit],
    queryFn: () => api.feed.getFeed(offset, limit),
    staleTime: 30_000, // 30 секунд
  });
};
```

### 11.3. Оптимизация видео
```html
<!-- Для MP4 (прогрессивный) -->
<video src="/api/videos/stream/videos/user123/video.mp4" controls />

<!-- Для HLS (адаптивный) -->
<script src="https://cdn.jsdelivr.net/npm/hls.js@latest"></script>
<video id="video" controls></video>
<script>
  if (Hls.isSupported()) {
    const video = document.getElementById('video');
    const hls = new Hls();
    hls.loadSource('/api/videos/stream/hls/user123/video/playlist.m3u8');
    hls.attachMedia(video);
  }
</script>
```

