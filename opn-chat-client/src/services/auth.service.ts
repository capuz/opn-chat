import { api } from './api.service';
import type { AuthResponseDto, GoogleAuthDto } from '../types/auth';

export const authService = {
  async loginWithGoogle(googleToken: string): Promise<AuthResponseDto> {
    const response = await api.post<AuthResponseDto>('/api/auth/google', {
      googleToken,
    } as GoogleAuthDto);

    return response.data;
  },

  async logout(): Promise<void> {
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
      try {
        await api.post('/api/auth/logout', { refreshToken });
      } catch (error) {
        console.error('Logout error:', error);
      }
    }
    
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  },

  isAuthenticated(): boolean {
    return !!localStorage.getItem('accessToken');
  },

  getUser(): any {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  },
};
