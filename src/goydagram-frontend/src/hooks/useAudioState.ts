import { useState, useEffect, useCallback } from 'react';

// Глобальное состояние для звука
let globalMutedState = true;
const listeners: Set<(muted: boolean) => void> = new Set();

export function useAudioState() {
  const [isMuted, setIsMuted] = useState(globalMutedState);

  useEffect(() => {
    const listener = (muted: boolean) => {
      setIsMuted(muted);
    };
    listeners.add(listener);
    
    setIsMuted(globalMutedState);
    
    return () => {
      listeners.delete(listener);
    };
  }, []);

  const toggleMuted = useCallback(() => {
    const newState = !globalMutedState;
    globalMutedState = newState;
    listeners.forEach(listener => listener(newState));
    setIsMuted(newState);
  }, []);

  return { isMuted, toggleMuted };
}