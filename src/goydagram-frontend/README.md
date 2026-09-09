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

## What's new since the first pass

- **Reels-style feed** (`FeedPage`/`ReelCard`): full-screen vertical video, autoplay via `IntersectionObserver`, tap-to-pause, side icon rail (like/comment/share/mute). Tabs:
  - **For You** — `GET /api/feed`, paginated, infinite-scroll via a sentinel `IntersectionObserver`.
  - **Popular** — `GET /api/feed/trending` (already author-enriched by FeedService's own `enrichWithAuthors`).
  - **Following** — **no dedicated backend endpoint for this.** Composed client-side: fetch subscriptions, then `GET /api/videos/user/{id}` per followee, merge, sort by `createdAt`. Fine for a handful of follows; won't scale — a real "following feed" belongs in FeedService if you want this for real.
  - **My** — `GET /api/videos/user`, author synthesized from the logged-in user (no extra lookup).
- **Comments** open in a bottom sheet (`CommentsDrawer`) instead of inline, to fit the full-screen video layout.
- **Edit profile** (`/profile/edit`) and **edit video** (Edit button on `VideoPage`, owner-only) — call `PUT /api/users/me` and `PUT /api/videos/{id}`, **neither of which exists on the backend yet**. See "Backend changes required" below.
- Mobile bottom nav now includes a Profile icon (was missing before).

## Backend changes required for this build to fully work

None of this is optional polish — without it the reels feed either 401s, shows blank thumbnails, or the new edit forms hit 404s. In rough priority order:

1. **`FeedService` doesn't wire `AuthMiddleware` into any route**, so `/api/feed` always returns 401 regardless of a valid token. Fix in `main.go`: add `api.Use(middleware.AuthMiddleware(cfg.JwtSecret))` to the `/api` group, and add a `JwtSecret` field to `config.Config` (currently absent) loaded from `JWT_SECRET`. Must match the same secret UserService signs with.

2. **`FeedService`'s Go structs use snake_case JSON tags** (`user_id`, `preview_url`, `hls_manifest`, `views_count`, …) but ContentService/UserService emit **camelCase** (ASP.NET default, no naming policy override in either `Program.cs`). Go's `encoding/json` does case-insensitive matching but does *not* bridge `userId` ↔ `user_id` — so every multi-word field on videos/authors FeedService fetches from those services silently zeroes out. This breaks thumbnails, HLS URLs, and author info in the reels feed specifically. Fix: change `FeedService/models/video.go` and the embedded `User` struct's tags to camelCase (`userId`, `previewUrl`, `hlsManifestUrl`, `viewsCount`, `likesCount`, `commentsCount`, `createdAt`, `avatarUrl`, `isVerified`). This also makes what FeedService returns to the frontend camelCase, consistent with the rest of the API.

3. **Gateway route conflict**: SocialService's `/api/videos/{id}/comments|likes|views` routes collide with ContentService's `/api/videos/**` prefix — add specific higher-priority YARP routes for the SocialService sub-paths (see chat for the exact `appsettings.json` snippet; the file itself wasn't in the source dump so I couldn't diff it directly). Same conflict exists for `/api/users/{id}/interests`.

4. **`VideosController.Delete` reads the wrong claim** — `User.FindFirst("userId")` instead of `ClaimTypes.NameIdentifier` (every other action in that controller uses the latter). Delete will 401 until this one-line fix lands.

5. **No update endpoints exist**: `UpdateProfileCommand`/`UpdateVideoCommand` (MediatR command + handler + controller route) need to be added to UserService and ContentService respectively. `User.UpdateProfile(username, avatarUrl, bio)` already exists on the domain entity and just needs wiring up; `Video` needs a new mutator (e.g. `UpdateDetails(title, description)` + tag replacement) since none exists today.

I can write out the actual C#/Go patches for all of these — say the word and I'll paste them in rather than leaving them as TODOs.



```
src/
  api/          one module per backend service
  components/   Layout (nav shell), VideoCard/Grid, HlsPlayer, CommentSection, UploadForm
  context/      AuthContext (token + current user)
  lib/          axios instance + interceptors, React Query client
  pages/        one per route
  types/        DTOs mirrored from the C#/Go/Python source
```
