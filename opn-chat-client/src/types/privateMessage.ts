export interface PrivateMessage {
  id: string;
  senderId?: string;
  senderName?: string;
  receiverId?: string;
  receiverName?: string;
  content: string;
  timestamp: string;
  isRead: boolean;
  isDeletedForEveryone?: boolean;
}

export interface SendPrivateMessageDto {
  receiverId: string;
  content: string;
}

export interface Conversation {
  userId: string;
  nickname: string;
  avatarUrl?: string;
  lastMessage: string;
  lastMessageTime: string;
  unreadCount: number;
}
