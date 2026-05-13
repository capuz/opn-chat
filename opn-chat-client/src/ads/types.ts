export type RewardType =
  | 'boost'
  | 'nickname_change'
  | 'room_slot'
  | 'coins'
  | 'temp_perk'
  | 'temp_premium';

export const MODAL_TO_REWARD: Record<'boost' | 'nickname' | 'room', RewardType> = {
  boost:    'boost',
  nickname: 'nickname_change',
  room:     'room_slot',
};

export type AdEventName =
  | 'ad_started'
  | 'ad_completed'
  | 'reward_granted'
  | 'ad_failed'
  | 'cooldown_started';

export interface AdEvent {
  readonly name:       AdEventName;
  readonly rewardType: RewardType;
  readonly timestamp:  number;
  readonly meta?:      Record<string, unknown>;
}

export type RewardPayload =
  | { kind: 'nickname_change'; unlockedUntil: number }
  | { kind: 'boost';           roomId: string }
  | { kind: 'room_slot' }
  | { kind: 'coins';           amount: number }
  | { kind: 'temp_perk';       perkId: string; expiresAt: number }
  | { kind: 'temp_premium';    expiresAt: number };

export interface RewardResult {
  readonly granted:    boolean;
  readonly rewardType: RewardType;
  readonly payload?:   RewardPayload;
}

export interface RewardContext {
  rewardType: RewardType;
  roomId?:    string;
}

export type AdsProviderName =
  | 'adsense'
  | 'mock'
  | 'paymentwall'
  | 'offertoro'
  | 'lootably';
