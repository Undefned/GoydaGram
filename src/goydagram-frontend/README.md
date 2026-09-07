# GoydaGram Frontend

React + TypeScript client for GoydaGram, covering auth, feed, search, video
upload/watch, likes/comments, and subscriptions — talking to your ApiGateway.

## Stack

- Vite + React 18 + TypeScript
- Tailwind CSS (dark theme: `ink` background, `flare` accent, `mint` secondary)
- TanStack React Query for data fetching/caching
- React Router
- Axios, with a request interceptor that attaches the JWT and a response
  interceptor that transparently refreshes on 401 (single-flight, so
  concurrent requests don't race the refresh-token rotation your
  `RefreshAccessTokenCommandHandler` does)
- hls.js for HLS playback (falls back to native `<video>` HLS on Safari)

## Getting started

```bash
npm install
cp .env.example .env   # set VITE_API_BASE_URL to your ApiGateway origin
npm run dev
```

Requires the ApiGateway (and the services behind it) running and reachable
from the browser, with CORS allowing the dev origin — `AllowAll` is already
configured in `ApiGateway/Program.cs`.

## API surface this app assumes

Extracted from the actual controllers/handlers in your source, not guessed.
All routes assumed to be proxied by the gateway under `/api/**` unmodified
(every service's Swagger `BasePath` is `/api`, and each controller uses
`[Route("api/[controller]")]` or an explicit `/api/...` router group).

| Area | Route | Service |
|---|---|---|
| Auth | `POST /api/auth/register\|login\|refresh\|logout` | UserService |
| Users | `GET /api/users/me\|{id}`, `GET/POST/DELETE .../subscriptions\|subscribe\|unsubscribe` | UserService |
| Interests | `GET/POST /api/users/{id}/interests[/refresh]` | SocialService |
| Videos | `GET /api/videos/{id}\|trending\|user[/{id}]`, `POST /api/videos/batch\|upload`, `DELETE /api/videos/{id}` | ContentService |
| Streaming | `GET /api/videos/stream/**`, `/api/videos/stream/preview/**` (anonymous) | ContentService |
| Feed | `GET /api/feed`, `/api/feed/trending`, `/api/feed/prefetch` | FeedService (Go) |
| Comments/Likes/Views | `/api/comments`, `/api/likes`, `/api/views`, `/api/videos/{id}/comments\|likes[/count]` | SocialService (Go) |
| Search | `GET /api/search/videos\|users`, `POST /api/search/recommendations` | SearchService (Python) |

## Known gaps / things to wire up when you touch the backend

- **Feed shape**: `FeedController.GetFeed` returns `map[string]interface{}`,
  so its exact JSON shape isn't pinned down in the source. `src/api/feed.ts`
  and `FeedPage.tsx` handle two plausible shapes (`{ items: [...] }` with
  video IDs to hydrate via `/api/videos/batch`, or `{ videos: [...] }`
  already hydrated) — check your actual response and simplify once you know
  which one it is.
- **"Have I liked this video?"**: there's no endpoint that returns whether
  the current user already liked a video, only aggregate counts and a
  paginated like list. The Like button on `VideoPage` is optimistic/local
  only — wire it to real state once such an endpoint exists (or fetch the
  like list and check membership, which doesn't scale).
- **Comment author display**: `Comment.user_id` is a bare ID (Mongo doc has
  no username/avatar). `CommentSection` shows the raw ID for now — consider
  a batch user-lookup endpoint or denormalizing username onto the comment at
  write time.
- **Gateway route prefixes** are assumed, not confirmed — `ApiGateway`'s
  `appsettings.json` (which holds the actual YARP route/cluster config)
  wasn't in the file dump. If your gateway rewrites paths (e.g. strips a
  service name segment), update the `baseURL`/paths in `src/api/*.ts`
  accordingly.

## Structure

```
src/
  api/          one module per backend service
  components/   Layout (nav shell), VideoCard/Grid, HlsPlayer, CommentSection, UploadForm
  context/      AuthContext (token + current user)
  lib/          axios instance + interceptors, React Query client
  pages/        one per route
  types/        DTOs mirrored from the C#/Go/Python source
```
