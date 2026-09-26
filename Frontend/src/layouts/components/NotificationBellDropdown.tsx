import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { Bell, CheckCheck, Clock, ExternalLink } from 'lucide-react'
import { useAuth } from '@/auth/AuthProvider'
import { useToast } from '@/common/components'
import type { NotificationItem } from '@/pages/notifications/notifications-api'
import { useMarkAllNotificationsRead, useMarkNotificationRead, useMyNotifications, useUnreadNotificationCount } from '@/pages/notifications/notification-queries'
import { formatNotificationTime, resolveNotificationLink, stripNotificationHtml } from '@/pages/notifications/notification-navigation'

export function NotificationBellDropdown() {
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const navigate = useNavigate()
  const { user } = useAuth()
  const { showToast } = useToast()
  const unreadQuery = useUnreadNotificationCount()
  const recentQuery = useMyNotifications({ pageSize: 5 })
  const markReadMutation = useMarkNotificationRead()
  const markAllMutation = useMarkAllNotificationsRead()
  const unreadCount = unreadQuery.data ?? 0
  const items = recentQuery.data?.items ?? []

  // Handle open/close
  const toggleDropdown = () => {
    if (!isOpen) {
      void recentQuery.refetch()
      void unreadQuery.refetch()
    }
    setIsOpen(!isOpen)
  }

  // Close when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
      const handleEscape = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
          setIsOpen(false)
          triggerRef.current?.focus()
        }
      }
      document.addEventListener('keydown', handleEscape)
      return () => {
        document.removeEventListener('mousedown', handleClickOutside)
        document.removeEventListener('keydown', handleEscape)
      }
    }
    return undefined
  }, [isOpen])

  const handleMarkAsRead = async (id: string) => {
    try {
      await markReadMutation.mutateAsync(id)
    } catch {
      showToast('Không thể đánh dấu thông báo đã đọc.', 'error')
    }
  }

  const handleMarkAllRead = async () => {
    try {
      await markAllMutation.mutateAsync()
    } catch {
      showToast('Không thể đánh dấu tất cả thông báo đã đọc.', 'error')
    }
  }

  const handleViewAll = () => {
    setIsOpen(false)
    navigate('/notifications')
  }

  const handleOpenNotification = async (item: NotificationItem) => {
    if (!item.isRead) await handleMarkAsRead(item.id)
    const link = resolveNotificationLink(item.deepLink, user?.permissions ?? [])
    if (item.deepLink && !link.allowed) {
      showToast(link.reason ?? 'Không thể mở liên kết thông báo.', 'error')
      return
    }
    if (link.path) {
      setIsOpen(false)
      navigate(link.path)
    }
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        ref={triggerRef}
        type="button"
        onClick={toggleDropdown}
        aria-label="Xem thông báo"
        aria-expanded={isOpen}
        aria-controls="notification-dropdown"
        className="header-action-button relative"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span aria-live="polite" className="absolute top-1 right-1 flex items-center justify-center min-w-[18px] h-[18px] px-1 text-[10px] font-bold text-white bg-rose-500 rounded-full shadow-sm motion-safe:animate-pulse">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div id="notification-dropdown" role="dialog" aria-label="Thông báo gần đây" className="fixed top-16 right-2 left-2 z-50 mt-2 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-2xl duration-150 motion-safe:animate-in motion-safe:fade-in motion-safe:slide-in-from-top-2 sm:absolute sm:top-auto sm:right-0 sm:left-auto sm:w-96 dark:border-slate-800 dark:bg-slate-900">
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/40">
            <div className="flex items-center gap-2">
              <span className="font-bold text-slate-800 dark:text-slate-200 text-sm">Thông báo của bạn</span>
              {unreadCount > 0 && (
                <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-indigo-100 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300">
                  {unreadCount} chưa đọc
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={handleMarkAllRead}
                disabled={markAllMutation.isPending}
                className="text-[11px] font-medium text-indigo-600 dark:text-indigo-400 hover:underline flex items-center gap-1"
              >
                <CheckCheck className="w-3.5 h-3.5" />
                Đọc tất cả
              </button>
            )}
          </div>

          {/* List */}
          <div className="divide-y divide-slate-100 dark:divide-slate-800 max-h-[360px] overflow-y-auto">
            {recentQuery.isLoading ? (
              <div className="p-6 text-center text-xs text-slate-400">Đang tải thông báo...</div>
            ) : recentQuery.isError ? (
              <div className="p-6 text-center text-xs text-slate-500">
                <p>Không thể tải thông báo.</p>
                <button type="button" onClick={() => void recentQuery.refetch()} className="mt-2 font-semibold text-indigo-600 hover:underline">Thử lại</button>
              </div>
            ) : items.length === 0 ? (
              <div className="p-8 text-center text-xs text-slate-400">
                <Bell className="w-8 h-8 mx-auto mb-2 text-slate-300 dark:text-slate-600 opacity-60" />
                Không có thông báo mới nào
              </div>
            ) : (
              items.map((item) => (
                <button
                  type="button"
                  key={item.id}
                  onClick={() => void handleOpenNotification(item)}
                  className={`w-full p-3.5 text-left hover:bg-slate-50 dark:hover:bg-slate-800/60 cursor-pointer transition-colors text-xs flex gap-3 ${
                    !item.isRead ? 'bg-indigo-50/40 dark:bg-indigo-950/20' : ''
                  }`}
                >
                  <div className="mt-0.5 shrink-0"><Bell className="w-3.5 h-3.5 text-indigo-500" /></div>
                  <div className="flex-1 min-w-0 space-y-1">
                    <div className="flex items-center justify-between gap-1">
                      <h4
                        className={`truncate text-xs ${
                          !item.isRead
                            ? 'font-bold text-slate-900 dark:text-slate-100'
                            : 'font-medium text-slate-700 dark:text-slate-300'
                        }`}
                      >
                        {item.subject || item.templateName}
                      </h4>
                      {!item.isRead && (
                        <span className="w-2 h-2 rounded-full bg-indigo-500 shrink-0" title="Chưa đọc" />
                      )}
                    </div>
                    <p className="text-[11px] text-slate-500 dark:text-slate-400 line-clamp-2 leading-relaxed">
                      {stripNotificationHtml(item.body)}
                    </p>
                    <div className="flex items-center gap-1 text-[10px] text-slate-400">
                      <Clock className="w-3 h-3" />
                      {formatNotificationTime(item.createdAtUtc || item.sentAtUtc)}
                      <span aria-hidden="true">•</span>
                      <span>{item.severity === 'Error' ? 'Quan trọng' : item.severity === 'Warning' ? 'Cảnh báo' : item.severity === 'Success' ? 'Thành công' : 'Thông tin'}</span>
                    </div>
                  </div>
                </button>
              ))
            )}
          </div>

          {/* Footer */}
          <div className="p-2.5 border-t border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/40 text-center">
            <button
              type="button"
              onClick={handleViewAll}
              className="text-xs font-semibold text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300 flex items-center justify-center gap-1.5 w-full py-1"
            >
              Xem tất cả thông báo
              <ExternalLink className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
