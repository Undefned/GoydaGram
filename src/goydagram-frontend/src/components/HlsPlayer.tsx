import { forwardRef, useEffect, useImperativeHandle, useRef } from "react";
import Hls from "hls.js";

interface HlsPlayerProps {
  src: string;
  poster?: string;
  autoPlay?: boolean;
  /** Show native <video> controls. Set false for the reels feed, which uses its own tap gestures. */
  controls?: boolean;
  muted?: boolean;
  loop?: boolean;
  /** Play when true, pause when false. Only takes effect when provided (undefined = uncontrolled). */
  active?: boolean;
  className?: string;
  onPlay?: () => void;
  onClick?: () => void;
}

export interface HlsPlayerHandle {
  video: HTMLVideoElement | null;
}

export const HlsPlayer = forwardRef<HlsPlayerHandle, HlsPlayerProps>(function HlsPlayer(
  { src, poster, autoPlay, controls = true, muted, loop, active, className, onPlay, onClick },
  ref
) {
  const videoRef = useRef<HTMLVideoElement>(null);

  useImperativeHandle(ref, () => ({ video: videoRef.current }), []);

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    let hls: Hls | null = null;

    if (video.canPlayType("application/vnd.apple.mpegurl")) {
      // Safari plays HLS natively.
      video.src = src;
    } else if (Hls.isSupported()) {
      hls = new Hls({ enableWorker: true });
      hls.loadSource(src);
      hls.attachMedia(video);
    }

    return () => {
      hls?.destroy();
    };
  }, [src]);

  useEffect(() => {
    const video = videoRef.current;
    if (!video || active === undefined) return;
    if (active) {
      video.play().catch(() => {
        // Autoplay can be blocked before the first user gesture — harmless, the
        // tap-to-play affordance in ReelCard covers this case.
      });
    } else {
      video.pause();
    }
  }, [active]);

  return (
    <video
      ref={videoRef}
      poster={poster}
      controls={controls}
      autoPlay={autoPlay}
      muted={muted}
      loop={loop}
      playsInline
      onPlay={onPlay}
      onClick={onClick}
      className={className ?? "w-full h-full rounded-xl bg-black"}
    />
  );
});
