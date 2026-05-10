const STORAGE_KEY = 'chat:sound:enabled';

export type SoundEvent = 'highlight' | 'join' | 'part' | 'privateMessage' | 'mention' | 'error' | 'success';

function playTone(
  ctx: AudioContext,
  freq: number,
  startTime: number,
  duration: number,
  volume = 0.12,
): void {
  const osc  = ctx.createOscillator();
  const gain = ctx.createGain();
  osc.connect(gain);
  gain.connect(ctx.destination);
  osc.type = 'square';
  osc.frequency.value = freq;
  gain.gain.setValueAtTime(volume, startTime);
  gain.gain.exponentialRampToValueAtTime(0.001, startTime + duration);
  osc.start(startTime);
  osc.stop(startTime + duration);
}

function withCtx(fn: (ctx: AudioContext) => void): void {
  let ctx: AudioContext | null = null;
  try {
    ctx = new AudioContext();
    fn(ctx);
    setTimeout(() => ctx!.close().catch(() => {}), 1500);
  } catch {
    ctx?.close().catch(() => {});
  }
}

const sounds: Record<SoundEvent, () => void> = {
  // mIRC: two short ascending tones (classic highlight)
  highlight: () => withCtx((ctx) => {
    playTone(ctx, 800,  ctx.currentTime,       0.07);
    playTone(ctx, 1050, ctx.currentTime + 0.09, 0.07);
  }),

  // mIRC: ascending three-step (user joined channel)
  join: () => withCtx((ctx) => {
    playTone(ctx, 600,  ctx.currentTime,       0.06);
    playTone(ctx, 800,  ctx.currentTime + 0.07, 0.06);
    playTone(ctx, 1000, ctx.currentTime + 0.14, 0.06);
  }),

  // mIRC: descending three-step (user left channel)
  part: () => withCtx((ctx) => {
    playTone(ctx, 1000, ctx.currentTime,       0.06);
    playTone(ctx, 800,  ctx.currentTime + 0.07, 0.06);
    playTone(ctx, 600,  ctx.currentTime + 0.14, 0.06);
  }),

  // mIRC: double pulse (private message received)
  privateMessage: () => withCtx((ctx) => {
    playTone(ctx, 900, ctx.currentTime,       0.055);
    playTone(ctx, 900, ctx.currentTime + 0.1,  0.055);
  }),

  // mIRC: two tones, louder (your nick was mentioned)
  mention: () => withCtx((ctx) => {
    playTone(ctx, 750,  ctx.currentTime,       0.08, 0.18);
    playTone(ctx, 1050, ctx.currentTime + 0.1,  0.1,  0.18);
  }),

  // mIRC: descending buzz (error / invalid command)
  error: () => withCtx((ctx) => {
    playTone(ctx, 400, ctx.currentTime,       0.1);
    playTone(ctx, 280, ctx.currentTime + 0.13, 0.15);
  }),

  // warm ascending 3-step (command success, e.g. nick change)
  success: () => withCtx((ctx) => {
    playTone(ctx, 700,  ctx.currentTime,        0.055, 0.09);
    playTone(ctx, 900,  ctx.currentTime + 0.07, 0.055, 0.09);
    playTone(ctx, 1100, ctx.currentTime + 0.14, 0.08,  0.09);
  }),
};

// Self-initialize from localStorage on module load
let enabled = (() => {
  const stored = localStorage.getItem(STORAGE_KEY);
  return stored === null ? true : stored === 'true';
})();

export const chatSounds = {
  play(event: SoundEvent): void {
    if (!enabled) return;
    sounds[event]?.();
  },

  setEnabled(value: boolean): void {
    enabled = value;
    localStorage.setItem(STORAGE_KEY, String(value));
  },

  /** Re-reads localStorage — call after external settings changes. */
  loadSettings(): void {
    const stored = localStorage.getItem(STORAGE_KEY);
    enabled = stored === null ? true : stored === 'true';
  },

  isEnabled(): boolean {
    return enabled;
  },
};
