import type { RewardContext } from './types';

export interface AdsProvider {
  initialize(): Promise<void>;
  showRewardedAd(context: RewardContext): Promise<boolean>;
  readonly isAdInFlight: boolean;
}
