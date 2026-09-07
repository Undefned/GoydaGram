import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center text-center">
      <h1 className="font-display text-3xl font-bold text-flare-500">404</h1>
      <p className="mt-2 text-sm text-ink-400">This page doesn't exist.</p>
      <Link to="/" className="mt-4 text-sm text-mint-400 hover:underline">
        Back to feed
      </Link>
    </div>
  );
}
