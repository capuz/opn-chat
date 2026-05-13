import type { AdsProvider } from '../AdsProvider';
import type { RewardContext } from '../types';

declare global {
  interface Window {
    adsbygoogle?: unknown[];
  }
}

const AD_CLIENT     = import.meta.env.VITE_ADSENSE_CLIENT as string;
const AD_SLOT       = import.meta.env.VITE_ADSENSE_SLOT   as string;
const AD_TIMEOUT_MS = 30_000;

export class AdSenseProvider implements AdsProvider {
  #inFlight = false;

  get isAdInFlight(): boolean {
    return this.#inFlight;
  }

  async initialize(): Promise<void> {
    if (document.querySelector('script[data-ad-client]')) return;
    const script = document.createElement('script');
    script.async = true;
    script.src = `https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=${AD_CLIENT}`;
    script.setAttribute('crossorigin', 'anonymous');
    script.setAttribute('data-ad-client', AD_CLIENT);
    document.head.appendChild(script);
  }

  showRewardedAd(_context: RewardContext): Promise<boolean> {
    if (this.#inFlight) return Promise.resolve(false);

    return new Promise<boolean>((resolve) => {
      this.#inFlight = true;
      window.adsbygoogle = window.adsbygoogle ?? [];

      if (!Array.isArray(window.adsbygoogle)) {
        this.#inFlight = false;
        resolve(false);
        return;
      }

      let settled = false;
      const settle = (granted: boolean) => {
        if (!settled) {
          settled = true;
          this.#inFlight = false;
          resolve(granted);
        }
      };

      const timer = setTimeout(() => settle(false), AD_TIMEOUT_MS);

      try {
        (window.adsbygoogle as unknown[]).push({
          params: {
            google_ad_client: AD_CLIENT,
            google_ad_slot:   AD_SLOT,
          },
          type: 'reward',
          callback: (data: { type: string; amount: number } | null) => {
            clearTimeout(timer);
            settle(data !== null && data !== undefined);
          },
        });
      } catch {
        clearTimeout(timer);
        settle(false);
      }
    });
  }
}
