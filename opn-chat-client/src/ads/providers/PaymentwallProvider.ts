import type { AdsProvider } from '../AdsProvider';
import type { RewardContext } from '../types';

export class PaymentwallProvider implements AdsProvider {
  readonly isAdInFlight = false;

  async initialize(): Promise<void> {
    throw new Error('PaymentwallProvider: not implemented');
  }

  showRewardedAd(_context: RewardContext): Promise<boolean> {
    return Promise.reject(new Error('PaymentwallProvider: not implemented'));
  }
}
