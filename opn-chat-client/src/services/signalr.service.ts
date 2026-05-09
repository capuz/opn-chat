import * as signalR from '@microsoft/signalr';
import { apiService } from './api.service';

const connections = new Map<string, signalR.HubConnection>();

export const getSignalRConnection = (hubName: string): signalR.HubConnection => {
  const existing = connections.get(hubName);
  if (existing) return existing;

  const conn = new signalR.HubConnectionBuilder()
    .withUrl(`${import.meta.env.VITE_API_URL || 'http://localhost:5091'}/hubs/${hubName}`, {
      accessTokenFactory: () => {
        const token = localStorage.getItem('accessToken');
        console.log(`SignalR accessTokenFactory (${hubName}): ${token ? 'token present' : 'no token'}`);
        return token || '';
      },
      transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      logMessageContent: true,
      skipNegotiation: false,
      withCredentials: false,
    })
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

  conn.onreconnecting((error) => console.log(`SignalR reconnecting (${hubName}):`, error));
  conn.onreconnected((id) => console.log(`SignalR reconnected (${hubName}):`, id));

  connections.set(hubName, conn);
  return conn;
};

export const startConnection = async (hubName: string): Promise<void> => {
  const conn = getSignalRConnection(hubName);

  if (conn.state === signalR.HubConnectionState.Connected) {
    return;
  }

  // If not in Disconnected state, stop and recreate the connection
  if (conn.state !== signalR.HubConnectionState.Disconnected) {
    try {
      await conn.stop();
    } catch (e) {
      // Ignore errors when stopping
    }
    connections.delete(hubName);
    const newConn = getSignalRConnection(hubName);
    await newConn.start();
    console.log(`SignalR Connected (${hubName})`);
    return;
  }

  try {
    await conn.start();
    console.log(`SignalR Connected (${hubName})`);
  } catch (error) {
    console.error(`SignalR Connection Error (${hubName}):`, error);
    
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) throw error;

    try {
      const response = await apiService.getAxiosInstance().post('/api/auth/refresh', { refreshToken });
      const { accessToken } = response.data;
      localStorage.setItem('accessToken', accessToken);
      await conn.stop();
      connections.delete(hubName);
      const newConn = getSignalRConnection(hubName);
      await newConn.start();
      console.log(`SignalR Connected after token refresh (${hubName})`);
    } catch (refreshError) {
      console.error('Token refresh failed:', refreshError);
      throw refreshError;
    }
  }
};

export const stopConnection = async (hubName: string) => {
  const conn = connections.get(hubName);
  if (conn && conn.state !== signalR.HubConnectionState.Disconnected) {
    await conn.stop();
    console.log(`SignalR Disconnected (${hubName})`);
  }
  connections.delete(hubName);
};
