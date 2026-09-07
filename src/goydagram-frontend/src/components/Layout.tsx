import { NavLink, useNavigate } from "react-router-dom";
import { ReactNode, useState } from "react";
import { useAuth } from "@/context/AuthContext";

const navItems = [
  { to: "/", label: "Feed", icon: FeedIcon },
  { to: "/trending", label: "Trending", icon: TrendingIcon },
  { to: "/search", label: "Search", icon: SearchIcon },
  { to: "/upload", label: "Upload", icon: UploadIcon },
];

export function Layout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) navigate(`/search?q=${encodeURIComponent(query.trim())}`);
  };

  return (
    <div className="min-h-screen flex">
      {/* Sidebar (desktop) */}
      <aside className="hidden md:flex md:flex-col w-60 shrink-0 border-r border-ink-800 px-5 py-6">
        <NavLink to="/" className="mb-8 block">
          <span className="font-display text-2xl font-bold tracking-tight">
            Goyda<span className="text-flare-500">Gram</span>
          </span>
        </NavLink>

        <nav className="flex-1 space-y-1">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === "/"}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${
                  isActive ? "bg-ink-800 text-ink-100" : "text-ink-400 hover:text-ink-100 hover:bg-ink-900"
                }`
              }
            >
              <Icon className="h-5 w-5" />
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="border-t border-ink-800 pt-4">
          {user ? (
            <div className="flex items-center gap-3">
              <NavLink to="/profile" className="flex items-center gap-3 flex-1 min-w-0">
                <Avatar url={user.avatarUrl} name={user.username} />
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium">{user.username}</p>
                  <p className="truncate text-xs text-ink-400">@{user.username}</p>
                </div>
              </NavLink>
              <button
                onClick={() => logout()}
                className="text-xs text-ink-400 hover:text-flare-400 transition-colors"
              >
                Log out
              </button>
            </div>
          ) : (
            <NavLink
              to="/login"
              className="block rounded-lg bg-flare-500 px-3 py-2 text-center text-sm font-semibold text-ink-950 hover:bg-flare-400 transition-colors"
            >
              Log in
            </NavLink>
          )}
        </div>
      </aside>

      {/* Main column */}
      <div className="flex-1 flex flex-col min-w-0">
        <header className="sticky top-0 z-10 border-b border-ink-800 bg-ink-950/90 backdrop-blur px-4 py-3 md:px-8">
          <form onSubmit={handleSearch} className="max-w-md">
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Search videos or people"
              className="w-full rounded-full border border-ink-700 bg-ink-900 px-4 py-2 text-sm text-ink-100 placeholder:text-ink-400 focus:border-mint-400 focus:outline-none"
            />
          </form>
        </header>

        <main className="flex-1 px-4 py-6 md:px-8">{children}</main>
      </div>

      {/* Mobile bottom nav */}
      <nav className="md:hidden fixed bottom-0 inset-x-0 z-20 flex justify-around border-t border-ink-800 bg-ink-950/95 backdrop-blur py-2">
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === "/"}
            className={({ isActive }) =>
              `flex flex-col items-center gap-1 px-3 py-1 text-[11px] ${
                isActive ? "text-flare-500" : "text-ink-400"
              }`
            }
          >
            <Icon className="h-5 w-5" />
            {label}
          </NavLink>
        ))}
      </nav>
    </div>
  );
}

export function Avatar({ url, name, size = "h-9 w-9" }: { url: string | null; name: string; size?: string }) {
  if (url) {
    return <img src={url} alt={name} className={`${size} rounded-full object-cover`} />;
  }
  return (
    <div
      className={`${size} rounded-full bg-ink-800 text-mint-400 flex items-center justify-center font-display text-sm font-semibold`}
    >
      {name.slice(0, 1).toUpperCase()}
    </div>
  );
}

function FeedIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <rect x="3" y="4" width="7" height="7" rx="1.5" />
      <rect x="14" y="4" width="7" height="16" rx="1.5" />
      <rect x="3" y="14" width="7" height="6" rx="1.5" />
    </svg>
  );
}

function TrendingIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <path d="M3 17l6-6 4 4 8-9" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M14 6h7v7" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function SearchIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <circle cx="11" cy="11" r="7" />
      <path d="M20 20l-4-4" strokeLinecap="round" />
    </svg>
  );
}

function UploadIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" {...props}>
      <path d="M12 16V4M12 4l-4 4M12 4l4 4" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M4 16v3a2 2 0 002 2h12a2 2 0 002-2v-3" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
