import { useState, useEffect, useCallback } from 'react'
import { useAuth } from '@/auth/AuthProvider'
import type { NotificationTemplate } from './notifications-api'
import { fetchTemplates } from './notifications-api'
import { SendNotificationTab } from './components/SendNotificationTab'
import { NotificationHistoryTab } from './components/NotificationHistoryTab'
import { TemplateManagementTab } from './components/TemplateManagementTab'
import { PageShell } from '@/common/components'
import { Button } from '@/common/components/ui/button'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/common/components/ui/tabs'
import {
  Send,
  History,
  FileCode,
  RefreshCw,
} from 'lucide-react'

type NotificationsPageProps = { initialTab?: 'send' | 'history' | 'templates' }
export function NotificationsPage({ initialTab = 'send' }: NotificationsPageProps) {
  const { user } = useAuth()
  const [activeTab, setActiveTab] = useState<'send' | 'history' | 'templates'>(initialTab)
  const [templates, setTemplates] = useState<NotificationTemplate[]>([])
  const [templatesLoading, setTemplatesLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const isManager = Boolean(
    user?.permissions?.includes('notifications.manage') ||
      user?.permissions?.includes('notification-templates.manage'),
  )

  const loadTemplates = useCallback(async () => {
    setTemplatesLoading(true)
    setError(null)
    try {
      const list = await fetchTemplates()
      setTemplates(list)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể tải danh sách mẫu thông báo.')
    } finally {
      setTemplatesLoading(false)
    }
  }, [])

  useEffect(() => {
    loadTemplates()
  }, [loadTemplates])

  return (
    <PageShell
      eyebrow="Vận hành & Giao tiếp"
      title="Thông báo & Mẫu thông báo"
      description="Gửi thông báo vận hành đa kênh (In-App, Email, SMS), theo dõi trạng thái gửi và cấu hình mẫu thông báo chuẩn."
      actions={
        <Button
          variant="outline"
          onClick={loadTemplates}
          disabled={templatesLoading}
          className="gap-2"
        >
          <RefreshCw className={`size-4 ${templatesLoading ? 'animate-spin' : ''}`} />
          Làm mới mẫu
        </Button>
      }
    >
      {error && (
        <div className="mb-6 rounded-lg border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive">
          {error}
        </div>
      )}

      <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as 'send' | 'history' | 'templates')} className="w-full">
        <TabsList className="mb-6 h-10">
          <TabsTrigger value="send" className="gap-2 px-4">
            <Send className="size-4" />
            Gửi thông báo
          </TabsTrigger>
          <TabsTrigger value="history" className="gap-2 px-4">
            <History className="size-4" />
            Lịch sử gửi & Vận hành
          </TabsTrigger>
          <TabsTrigger value="templates" className="gap-2 px-4">
            <FileCode className="size-4" />
            Mẫu thông báo ({templates.length})
          </TabsTrigger>
        </TabsList>

        <TabsContent value="send" className="mt-0 outline-none">
          <SendNotificationTab
            templates={templates}
            loading={templatesLoading}
            onSent={() => setActiveTab('history')}
            onSentSuccess={() => setActiveTab('history')}
          />
        </TabsContent>

        <TabsContent value="history" className="mt-0 outline-none">
          <NotificationHistoryTab isManager={isManager} />
        </TabsContent>

        <TabsContent value="templates" className="mt-0 outline-none">
          <TemplateManagementTab
            templates={templates}
            loading={templatesLoading}
            onReload={loadTemplates}
            isManager={isManager}
          />
        </TabsContent>
      </Tabs>
    </PageShell>
  )
}
export default NotificationsPage
