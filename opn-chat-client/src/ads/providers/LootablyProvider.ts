import type { AdsProvider } from '../AdsProvider';
import type { RewardContext } from '../types';

export class LootablyProvider implements AdsProvider {
  readonly isAdInFlight = false;

  async initialize(): Promise<void> {
    throw new Error('LootablyProvider: not implemented');
  }

  showRewardedAd(_context: RewardContext): Promise<boolean> {
    return Promise.reject(new Error('LootablyProvider: not implemented'));
  }
}
