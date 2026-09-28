import { useEffect, type ReactNode } from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { apiUrl, getRealtimeAccessToken } from '@/auth/auth-api'
import { useAuth } from '@/auth/AuthProvider'
import type { NotificationItem } from './notifications-api'
import { notificationKeys } from './notification-queries'

export function NotificationRealtimeProvider({ children }: { children: ReactNode }) {
  const { status } = useAuth()
  const client = useQueryClient()

  useEffect(() => {
    if (status !== 'authenticated') return

    const connection = new HubConnectionBuilder()
      .withUrl(apiUrl('/api/v1/notifications/hub'), {
        accessTokenFactory: getRealtimeAccessToken,
      })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.None)
      .build()

    connection.on('NotificationReceived', (notification: NotificationItem) => {
      if (!notification.isRead)
        client.setQueryData<number>(notificationKeys.unread, (current = 0) => current + 1)
      void client.invalidateQueries({ queryKey: notificationKeys.lists })
    })

    let disposed = false
    void connection.start().catch(() => {
      // Polling remains the fallback when the realtime transport is unavailable.
    })

    return () => {
      disposed = true
      if (disposed) void connection.stop()
    }
  }, [client, status])

  return children
}
