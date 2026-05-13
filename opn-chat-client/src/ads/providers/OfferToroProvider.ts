import type { AdsProvider } from '../AdsProvider';
import type { RewardContext } from '../types';

export class OfferToroProvider implements AdsProvider {
  readonly isAdInFlight = false;

  async initialize(): Promise<void> {
    throw new Error('OfferToroProvider: not implemented');
  }

  showRewardedAd(_context: RewardContext): Promise<boolean> {
    return Promise.reject(new Error('OfferToroProvider: not implemented'));
  }
}
