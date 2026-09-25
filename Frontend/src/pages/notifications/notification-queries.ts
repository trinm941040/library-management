import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { MyNotificationParams, NotificationPageResult } from './notifications-api'
import {
  fetchMyNotifications,
  fetchUnreadCount,
  markAllNotificationsRead,
  markNotificationRead,
} from './notifications-api'

export const notificationKeys = {
  all: ['notifications'] as const,
  unread: ['notifications', 'unread-count'] as const,
  lists: ['notifications', 'my'] as const,
  list: (params: MyNotificationParams) => ['notifications', 'my', params] as const,
}

export function useUnreadNotificationCount() {
  return useQuery({
    queryKey: notificationKeys.unread,
    queryFn: ({ signal }) => fetchUnreadCount(signal),
    refetchInterval: 30_000,
    refetchOnWindowFocus: true,
  })
}

export function useMyNotifications(params: MyNotificationParams) {
  return useQuery({
    queryKey: notificationKeys.list(params),
    queryFn: ({ signal }) => fetchMyNotifications(params, signal),
    refetchOnWindowFocus: true,
  })
}

export function useMarkNotificationRead() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => markNotificationRead(id),
    onSuccess: (_, id) => {
      const readAtUtc = new Date().toISOString()
      client.setQueriesData<NotificationPageResult>({ queryKey: notificationKeys.lists }, (current) =>
        current ? { ...current, items: current.items.map((item) => item.id === id ? { ...item, isRead: true, readAtUtc } : item) } : current,
      )
      void client.invalidateQueries({ queryKey: notificationKeys.unread })
    },
  })
}

export function useMarkAllNotificationsRead() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: () => markAllNotificationsRead(),
    onSuccess: () => {
      const readAtUtc = new Date().toISOString()
      client.setQueriesData<NotificationPageResult>({ queryKey: notificationKeys.lists }, (current) =>
        current ? { ...current, items: current.items.map((item) => ({ ...item, isRead: true, readAtUtc })) } : current,
      )
      client.setQueryData(notificationKeys.unread, 0)
    },
  })
}
