export interface NotificationItem {
  id: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationFeed {
  unreadCount: number;
  items: NotificationItem[];
}
