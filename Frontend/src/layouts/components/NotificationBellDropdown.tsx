import React, { useState, useEffect, useRef, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Bell, CheckCheck, Clock, Mail, MessageSquare, ExternalLink } from 'lucide-react'
import type { NotificationItem } from '@/pages/notifications/notifications-api'
import {
  fetchUnreadCount,
  fetchMyNotifications,
  markNotificationRead,
  markAllNotificationsRead,
} from '@/pages/notifications/notifications-api'

export function NotificationBellDropdown() {
  const [unreadCount, setUnreadCount] = useState<number>(0)
  const [isOpen, setIsOpen] = useState(false)
  const [items, setItems] = useState<NotificationItem[]>([])
  const [loading, setLoading] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()

  const loadUnreadCount = useCallback(async () => {
    try {
      const count = await fetchUnreadCount()
      setUnreadCount(count)
    } catch {
      // silently handle auth/network issues for unread poll
    }
  }, [])

  const loadRecentNotifications = useCallback(async () => {
    setLoading(true)
    try {
      const res = await fetchMyNotifications({ pageSize: 5 })
      setItems(res.items)
    } catch {
      // ignore
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadUnreadCount()
    const interval = setInterval(loadUnreadCount, 30000) // poll every 30s
    return () => clearInterval(interval)
  }, [loadUnreadCount])

  // Handle open/close
  const toggleDropdown = () => {
    if (!isOpen) {
      loadRecentNotifications()
      loadUnreadCount()
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
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  const handleMarkAsRead = async (id: string, e?: React.MouseEvent) => {
    e?.stopPropagation()
    try {
      await markNotificationRead(id)
      setItems((prev) =>
        prev.map((item) => (item.id === id ? { ...item, isRead: true, readAtUtc: new Date().toISOString() } : item)),
      )
      setUnreadCount((c) => Math.max(0, c - 1))
    } catch {
      // ignore
    }
  }

  const handleMarkAllRead = async () => {
    try {
      await markAllNotificationsRead()
      setItems((prev) =>
        prev.map((item) => ({ ...item, isRead: true, readAtUtc: new Date().toISOString() })),
      )
      setUnreadCount(0)
    } catch {
      // ignore
    }
  }

  const handleViewAll = () => {
    setIsOpen(false)
    navigate('/notifications')
  }

  const renderChannelIcon = (c: string) => {
    switch (c) {
      case 'Email':
        return <Mail className="w-3.5 h-3.5 text-sky-500" />
      case 'Sms':
        return <MessageSquare className="w-3.5 h-3.5 text-amber-500" />
      case 'InApp':
      default:
        return <Bell className="w-3.5 h-3.5 text-indigo-500" />
    }
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        type="button"
        onClick={toggleDropdown}
        aria-label="Xem thông báo"
        className="relative flex items-center justify-center w-9 h-9 rounded-lg text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors focus:outline-none focus:ring-2 focus:ring-indigo-500"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 flex items-center justify-center min-w-[18px] h-[18px] px-1 text-[10px] font-bold text-white bg-rose-500 rounded-full shadow-sm animate-pulse">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 sm:w-96 rounded-xl bg-white dark:bg-slate-900 shadow-2xl border border-slate-200 dark:border-slate-800 z-50 overflow-hidden animate-in fade-in slide-in-from-top-2 duration-150">
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
                className="text-[11px] font-medium text-indigo-600 dark:text-indigo-400 hover:underline flex items-center gap-1"
              >
                <CheckCheck className="w-3.5 h-3.5" />
                Đọc tất cả
              </button>
            )}
          </div>

          {/* List */}
          <div className="divide-y divide-slate-100 dark:divide-slate-800 max-h-[360px] overflow-y-auto">
            {loading ? (
              <div className="p-6 text-center text-xs text-slate-400">Đang tải thông báo...</div>
            ) : items.length === 0 ? (
              <div className="p-8 text-center text-xs text-slate-400">
                <Bell className="w-8 h-8 mx-auto mb-2 text-slate-300 dark:text-slate-600 opacity-60" />
                Không có thông báo mới nào
              </div>
            ) : (
              items.map((item) => (
                <div
                  key={item.id}
                  onClick={() => handleMarkAsRead(item.id)}
                  className={`p-3.5 hover:bg-slate-50 dark:hover:bg-slate-800/60 cursor-pointer transition-colors text-xs flex gap-3 ${
                    !item.isRead ? 'bg-indigo-50/40 dark:bg-indigo-950/20' : ''
                  }`}
                >
                  <div className="mt-0.5 shrink-0">{renderChannelIcon(item.channel)}</div>
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
                      {item.body.replace(/<[^>]*>?/gm, '')}
                    </p>
                    <div className="flex items-center gap-1 text-[10px] text-slate-400">
                      <Clock className="w-3 h-3" />
                      {item.sentAtUtc ? new Date(item.sentAtUtc).toLocaleString('vi-VN') : 'Vừa xong'}
                    </div>
                  </div>
                </div>
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
              Xem trang quản lý thông báo
              <ExternalLink className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
