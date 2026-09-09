import { forwardRef, useEffect, useImperativeHandle, useRef, useState } from "react";
import Hls from "hls.js";

interface HlsPlayerProps {
  src: string | null;
  poster?: string;
  autoPlay?: boolean;
  controls?: boolean;
  muted?: boolean;
  loop?: boolean;
  active?: boolean;
  className?: string;
  onPlay?: () => void;
  onClick?: () => void;
  onError?: () => void;
}

export interface HlsPlayerHandle {
  video: HTMLVideoElement | null;
}

export const HlsPlayer = forwardRef<HlsPlayerHandle, HlsPlayerProps>(function HlsPlayer(
  { src, poster, autoPlay, controls = true, muted, loop, active, className, onPlay, onClick, onError },
  ref
) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [loadError, setLoadError] = useState(false);

  useImperativeHandle(ref, () => ({ video: videoRef.current }), []);

  useEffect(() => {
    const video = videoRef.current;
    if (!video || !src) return;

    setLoadError(false);
    let hls: Hls | null = null;

    // Пробуем HLS
    if (video.canPlayType("application/vnd.apple.mpegurl")) {
      video.src = src;
    } else if (Hls.isSupported()) {
      hls = new Hls({
        enableWorker: true,
        lowLatencyMode: true
    });
      
      hls.loadSource(src);
      hls.attachMedia(video);
      
      hls.on(Hls.Events.MANIFEST_PARSED, () => {
        if (autoPlay || active) {
          video.play().catch(() => {});
        }
      });

      hls.on(Hls.Events.ERROR, (event, data) => {
        console.error("HLS Error:", data);
        
        // Если ошибка критическая - пробуем перезагрузить или переключиться на MP4
        if (data.fatal) {
          switch (data.type) {
            case Hls.ErrorTypes.NETWORK_ERROR:
              console.log("HLS network error, trying to recover...");
              hls?.startLoad();
              break;
            case Hls.ErrorTypes.MEDIA_ERROR:
              console.log("HLS media error, trying to recover...");
              hls?.recoverMediaError();
              break;
            default:
              console.log("HLS fatal error, fallback to MP4");
              setLoadError(true);
              if (onError) onError();
              hls?.destroy();
              break;
          }
        }
      });
    } else {
      // Если HLS не поддерживается
      video.src = src;
      video.onerror = () => {
        console.error("Video load error");
        setLoadError(true);
        if (onError) onError();
      };
    }

    return () => {
      hls?.destroy();
      if (video) {
        video.onerror = null;
      }
    };
  }, [src, autoPlay]);

  useEffect(() => {
    const video = videoRef.current;
    if (!video || active === undefined) return;
    if (active) {
      video.play().catch(() => {});
    } else {
      video.pause();
    }
  }, [active]);

  if (!src) {
    return (
      <div className={className ?? "w-full h-full rounded-xl bg-black flex items-center justify-center"}>
        <p className="text-ink-400 text-sm">Video not available</p>
      </div>
    );
  }

  if (loadError) {
    return (
      <div className={className ?? "w-full h-full rounded-xl bg-black flex items-center justify-center flex-col gap-2"}>
        <p className="text-ink-400 text-sm">Video unavailable</p>
        <button 
          onClick={() => setLoadError(false)}
          className="text-xs text-mint-400 hover:underline"
        >
          Retry
        </button>
      </div>
    );
  }

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