import type { RewardContext, RewardPayload } from './types';
import { api } from '../services/api.service';
import { getSignalRConnection } from '../services/signalr.service';

export class RewardHandler {
  async handle(context: RewardContext): Promise<RewardPayload> {
    switch (context.rewardType) {
      case 'nickname_change': return this.#handleNicknameChange();
      case 'boost':           return this.#handleBoost(context.roomId);
      case 'room_slot':       return this.#handleRoomSlot();
      case 'coins':           throw new Error('RewardHandler: coins not implemented');
      case 'temp_perk':       throw new Error('RewardHandler: temp_perk not implemented');
      case 'temp_premium':    throw new Error('RewardHandler: temp_premium not implemented');
      default: {
        const _exhaustive: never = context.rewardType;
        throw new Error(`RewardHandler: unknown reward type "${String(_exhaustive)}"`);
      }
    }
  }

  async #handleNicknameChange(): Promise<RewardPayload> {
    const res = await api.post<{ unlockedUntil: string }>('/api/profile/nick-ad-unlock');
    return {
      kind:          'nickname_change',
      unlockedUntil: new Date(res.data.unlockedUntil).getTime(),
    };
  }

  async #handleBoost(roomId: string | undefined): Promise<RewardPayload> {
    if (!roomId) throw new Error('RewardHandler: boost requires roomId');
    const conn = getSignalRConnection('chat');
    await conn.invoke('BoostRoom', roomId);
    return { kind: 'boost', roomId };
  }

  async #handleRoomSlot(): Promise<RewardPayload> {
    // No backend endpoint yet — grant client-side only
    // TODO: add POST /api/monetization/watch-ad-room-slot once backend is ready
    return { kind: 'room_slot' };
  }
}

export const rewardHandler = new RewardHandler();
