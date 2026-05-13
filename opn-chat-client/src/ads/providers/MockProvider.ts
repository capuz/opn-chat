import type { AdsProvider } from '../AdsProvider';
import type { RewardContext } from '../types';

export class MockProvider implements AdsProvider {
  #inFlight = false;

  readonly #delayMs:     number;
  readonly #alwaysGrant: boolean;

  constructor(
    delayMs    = Number(import.meta.env.VITE_MOCK_AD_DELAY_MS ?? 800),
    alwaysGrant = (import.meta.env.VITE_MOCK_AD_GRANT ?? 'true') !== 'false',
  ) {
    this.#delayMs     = delayMs;
    this.#alwaysGrant = alwaysGrant;
  }

  get isAdInFlight(): boolean {
    return this.#inFlight;
  }

  async initialize(): Promise<void> {
    // No SDK to load
  }

  showRewardedAd(_context: RewardContext): Promise<boolean> {
    if (this.#inFlight) return Promise.resolve(false);

    return new Promise<boolean>((resolve) => {
      this.#inFlight = true;
      setTimeout(() => {
        this.#inFlight = false;
        resolve(this.#alwaysGrant);
      }, this.#delayMs);
    });
  }
}
