import type { Message } from '../types/auth';
import { apiService } from './api.service';

const api = apiService.getAxiosInstance();

export interface RoomDto {
  id: string;
  name: string;
  description?: string;
  isPrivate: boolean;
  createdByName?: string;
  memberCount: number;
}

export interface CreateRoomDto {
  name: string;
  description?: string;
  isPrivate: boolean;
  password?: string;
}

export interface RoomMemberDto {
  userId: string;
  nickname: string;
  avatarUrl?: string;
  roleName: string;
  joinedAt: string;
}

export const roomService = {
  // Obtener salas públicas
  async getPublicRooms(): Promise<RoomDto[]> {
    const response = await api.get<RoomDto[]>('/api/rooms/public');
    return response.data;
  },

  // Obtener sala por ID
  async getRoom(roomId: string): Promise<RoomDto> {
    const response = await api.get<RoomDto>(`/api/rooms/${roomId}`);
    return response.data;
  },

  // Crear una sala
  async createRoom(room: CreateRoomDto): Promise<RoomDto> {
    const response = await api.post<RoomDto>('/api/rooms', room);
    return response.data;
  },

  // Unirse a una sala
  async joinRoom(roomId: string, password?: string): Promise<void> {
    await api.post(`/api/rooms/${roomId}/join`, password);
  },

  // Abandonar una sala
  async leaveRoom(roomId: string): Promise<void> {
    await api.delete(`/api/rooms/${roomId}/leave`);
  },

  // Obtener miembros de una sala
  async getRoomMembers(roomId: string): Promise<RoomMemberDto[]> {
    const response = await api.get<RoomMemberDto[]>(`/api/rooms/${roomId}/members`);
    return response.data;
  },

  // Obtener mensajes de una sala
  async getRoomMessages(roomId: string, skip = 0, take = 50): Promise<Message[]> {
    const response = await api.get<Message[]>(`/api/rooms/${roomId}/messages`, {
      params: { skip, take }
    });
    return response.data;
  }
};