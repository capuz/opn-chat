import type { AdsProvider } from './AdsProvider';
import type { AdEvent, AdEventName, AdsProviderName, RewardContext, RewardResult } from './types';
import { AdSenseProvider }     from './providers/AdSenseProvider';
import { MockProvider }        from './providers/MockProvider';
import { PaymentwallProvider } from './providers/PaymentwallProvider';
import { OfferToroProvider }   from './providers/OfferToroProvider';
import { LootablyProvider }    from './providers/LootablyProvider';
import { rewardHandler }       from './RewardHandler';

type EventListener = (event: AdEvent) => void;

class AdManager {
  #provider:    AdsProvider;
  #listeners  = new Map<AdEventName, Set<EventListener>>();
  #initialized = false;

  constructor() {
    const name = (import.meta.env.VITE_ADS_PROVIDER ?? 'adsense') as AdsProviderName;
    this.#provider = AdManager.#createProvider(name);
  }

  static #createProvider(name: AdsProviderName): AdsProvider {
    switch (name) {
      case 'adsense':     return new AdSenseProvider();
      case 'mock':        return new MockProvider();
      case 'paymentwall': return new PaymentwallProvider();
      case 'offertoro':   return new OfferToroProvider();
      case 'lootably':    return new LootablyProvider();
      default: {
        const _exhaustive: never = name;
        throw new Error(`AdManager: unknown provider "${String(_exhaustive)}"`);
      }
    }
  }

  async initialize(): Promise<void> {
    if (this.#initialized) return;
    await this.#provider.initialize();
    this.#initialized = true;
  }

  setProvider(provider: AdsProvider): void {
    this.#provider    = provider;
    this.#initialized = false;
  }

  get isAdInFlight(): boolean {
    return this.#provider.isAdInFlight;
  }

  async requestReward(context: RewardContext): Promise<RewardResult> {
    if (this.#provider.isAdInFlight) {
      return { granted: false, rewardType: context.rewardType };
    }

    this.#emit({ name: 'ad_started', rewardType: context.rewardType, timestamp: Date.now() });

    let adWatched: boolean;
    try {
      adWatched = await this.#provider.showRewardedAd(context);
    } catch {
      adWatched = false;
    }

    if (!adWatched) {
      this.#emit({ name: 'ad_failed', rewardType: context.rewardType, timestamp: Date.now() });
      return { granted: false, rewardType: context.rewardType };
    }

    this.#emit({ name: 'ad_completed', rewardType: context.rewardType, timestamp: Date.now() });

    try {
      const payload = await rewardHandler.handle(context);
      this.#emit({
        name:       'reward_granted',
        rewardType: context.rewardType,
        timestamp:  Date.now(),
        meta:       payload as unknown as Record<string, unknown>,
      });
      return { granted: true, rewardType: context.rewardType, payload };
    } catch {
      this.#emit({ name: 'ad_failed', rewardType: context.rewardType, timestamp: Date.now() });
      return { granted: false, rewardType: context.rewardType };
    }
  }

  on(event: AdEventName, listener: EventListener): () => void {
    if (!this.#listeners.has(event)) {
      this.#listeners.set(event, new Set());
    }
    this.#listeners.get(event)!.add(listener);
    return () => this.#listeners.get(event)?.delete(listener);
  }

  #emit(event: AdEvent): void {
    this.#listeners.get(event.name)?.forEach(fn => fn(event));
  }
}

export const adManager = new AdManager();
