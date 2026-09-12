import { useEffect, useState, useCallback } from 'react'
import {
  Save,
  RefreshCw,
  Layers,
  Settings,
  Package,
  Download,
  Upload,
  Eye,
  EyeOff,
  History,
  CheckCircle2,
  AlertCircle,
  FileCode,
  Shield,
  Mail,
  Building,
  SlidersHorizontal,
} from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Switch } from '@/common/components/ui/switch'
import { Badge } from '@/common/components/ui/badge'
import { useToast } from '@/common/components/organisms/ToastProvider'
import { CirculationPolicyList } from './components/CirculationPolicyList'
import { CirculationPolicyPreview } from './components/CirculationPolicyPreview'
import { ConfigPackageDialog } from './components/ConfigPackageDialog'
import {
  getSystemSettings,
  batchUpdateSystemSettings,
  exportConfigurationPackage,
  resetSystemSettingsToDefaults,
  getConfigurationPackageHistory,
  type SystemSettingItem,
  type ConfigurationPackageSummary,
} from './system-settings-api'

const FRIENDLY_SETTING_NAMES: Record<string, string> = {
  'circulation.max_days': 'Thời hạn mượn sách mặc định (ngày)',
  'circulation.max_books': 'Số sách mượn tối đa cho mỗi độc giả',
  'circulation.max_renewals': 'Số lần gia hạn tối đa cho một lượt mượn',
  'circulation.hold_days': 'Thời hạn giữ sách đặt trước (ngày)',
  'circulation.fine_per_day': 'Đơn giá phạt trễ hạn mỗi ngày (VNĐ)',
  'circulation.block_overdue': 'Tự động chặn mượn khi có sách quá hạn',
  'circulation.lost_penalty_ratio': 'Tỷ lệ đền bù làm mất sách (%)',
  'library.name': 'Tên thư viện',
  'library.email': 'Email liên hệ thư viện',
  'library.address': 'Địa chỉ thư viện',
  'library.phone': 'Số điện thoại liên hệ',
  'library.hours': 'Khung giờ hoạt động',
  'notification.smtp_host': 'Địa chỉ máy chủ SMTP',
  'notification.smtp_port': 'Cổng kết nối SMTP',
  'notification.smtp_user': 'Tài khoản email gửi tin',
  'notification.smtp_password': 'Mật khẩu ứng dụng SMTP',
  'system.auto_backup': 'Tự động sao lưu định kỳ',
  'system.backup_retention_days': 'Thời gian lưu trữ bản sao lưu (ngày)',
}

function getSettingLabel(s: SystemSettingItem): string {
  return s.description || FRIENDLY_SETTING_NAMES[s.key] || s.key
}

export function ConfigPage() {
  const { showToast } = useToast()
  const [activeTab, setActiveTab] = useState<'settings' | 'policies' | 'packages'>('settings')

  // Settings State
  const [settings, setSettings] = useState<SystemSettingItem[]>([])
  const [formValues, setFormValues] = useState<Record<string, string>>({})
  const [isLoading, setIsLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [isResetting, setIsResetting] = useState(false)
  const [isExporting, setIsExporting] = useState(false)
  const [showSecrets, setShowSecrets] = useState<Record<string, boolean>>({})

  // Packages State
  const [packageHistory, setPackageHistory] = useState<ConfigurationPackageSummary[]>([])
  const [isPackageDialogOpen, setIsPackageDialogOpen] = useState(false)

  const loadSettings = useCallback(async () => {
    setIsLoading(true)
    try {
      const data = await getSystemSettings()
      setSettings(data)
      const values: Record<string, string> = {}
      for (const s of data) {
        values[s.key] = s.value
      }
      setFormValues(values)
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Không thể tải thiết lập hệ thống.', 'error')
    } finally {
      setIsLoading(false)
    }
  }, [showToast])

  const loadPackages = useCallback(async () => {
    try {
      const history = await getConfigurationPackageHistory()
      setPackageHistory(history)
    } catch {
      // Non-blocking
    }
  }, [])

  useEffect(() => {
    loadSettings()
    loadPackages()
  }, [loadSettings, loadPackages])

  const handleInputChange = (key: string, val: string) => {
    setFormValues((prev) => ({ ...prev, [key]: val }))
  }

  const handleSwitchChange = (key: string, checked: boolean) => {
    setFormValues((prev) => ({ ...prev, [key]: checked ? 'true' : 'false' }))
  }

  const toggleShowSecret = (key: string) => {
    setShowSecrets((prev) => ({ ...prev, [key]: !prev[key] }))
  }

  const handleSaveSettings = async () => {
    setIsSaving(true)
    try {
      const updates = Object.entries(formValues).map(([key, value]) => ({ key, value }))
      await batchUpdateSystemSettings(updates)
      showToast('Đã lưu toàn bộ cấu hình hệ thống và ghi nhận vào Nhật ký kiểm toán!', 'success')
      await loadSettings()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Lỗi khi lưu thiết lập.', 'error')
    } finally {
      setIsSaving(false)
    }
  }

  const handleResetDefaults = async () => {
    if (!confirm('Bạn có chắc chắn muốn khôi phục tất cả thiết lập về giá trị khuyến nghị của hệ thống?')) {
      return
    }

    setIsResetting(true)
    try {
      const res = await resetSystemSettingsToDefaults()
      showToast(res.message, 'success')
      await loadSettings()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Lỗi khi khôi phục mặc định.', 'error')
    } finally {
      setIsResetting(false)
    }
  }

  const handleExportPackage = async () => {
    setIsExporting(true)
    try {
      await exportConfigurationPackage()
      showToast('Đã xuất gói cấu hình hệ thống kèm mã kiểm tra toàn vẹn SHA-256!', 'success')
      await loadPackages()
    } catch (err) {
      showToast(err instanceof Error ? err.message : 'Lỗi khi xuất gói cấu hình.', 'error')
    } finally {
      setIsExporting(false)
    }
  }

  // Filter settings by scope
  const circulationSettings = settings.filter((s) => s.scope === 'circulation')
  const librarySettings = settings.filter((s) => s.scope === 'library')
  const notificationSettings = settings.filter((s) => s.scope === 'notification')
  const systemSettings = settings.filter((s) => s.scope === 'system')

  return (
    <div className="mx-auto w-full max-w-7xl px-4 py-8 md:px-8 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <p className="text-xs font-bold uppercase tracking-widest text-primary">
            Quản lý hệ thống
          </p>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight">
            Cấu hình &amp; Thiết lập hệ thống
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Quản lý tham số vận hành, quy định lưu thông, kết nối thông báo và sao lưu/nạp gói cấu hình.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {activeTab === 'settings' && (
            <>
              <Button
                variant="outline"
                size="sm"
                onClick={handleResetDefaults}
                disabled={isResetting || isSaving || isLoading}
              >
                <RefreshCw className={`mr-1.5 h-3.5 w-3.5 ${isResetting ? 'animate-spin' : ''}`} />
                {isResetting ? 'Đang khôi phục...' : 'Khôi phục mặc định'}
              </Button>
              <Button
                size="sm"
                onClick={handleSaveSettings}
                disabled={isSaving || isResetting || isLoading}
              >
                <Save className="mr-1.5 h-3.5 w-3.5" />
                {isSaving ? 'Đang lưu...' : 'Lưu thiết lập'}
              </Button>
            </>
          )}

          {activeTab === 'packages' && (
            <>
              <Button
                variant="outline"
                size="sm"
                onClick={handleExportPackage}
                disabled={isExporting}
              >
                <Download className="mr-1.5 h-3.5 w-3.5" />
                {isExporting ? 'Đang xuất...' : 'Xuất gói cấu hình (Export)'}
              </Button>
              <Button
                size="sm"
                onClick={() => setIsPackageDialogOpen(true)}
              >
                <Upload className="mr-1.5 h-3.5 w-3.5" />
                Nạp gói cấu hình (Import)
              </Button>
            </>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-border">
        <button
          onClick={() => setActiveTab('settings')}
          className={`flex items-center gap-2 border-b-2 px-5 py-2.5 text-sm font-medium transition-colors ${
            activeTab === 'settings'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <SlidersHorizontal className="h-4 w-4" />
          Tham số hệ thống
        </button>

        <button
          onClick={() => setActiveTab('policies')}
          className={`flex items-center gap-2 border-b-2 px-5 py-2.5 text-sm font-medium transition-colors ${
            activeTab === 'policies'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Layers className="h-4 w-4" />
          Chính sách lưu thông
        </button>

        <button
          onClick={() => setActiveTab('packages')}
          className={`flex items-center gap-2 border-b-2 px-5 py-2.5 text-sm font-medium transition-colors ${
            activeTab === 'packages'
              ? 'border-primary text-primary'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Package className="h-4 w-4" />
          Gói cấu hình &amp; Sao lưu
        </button>
      </div>

      {/* TAB 1: THIẾT LẬP THAM SỐ HỆ THỐNG */}
      {activeTab === 'settings' && (
        <div className="space-y-6">
          {isLoading && (
            <div className="py-12 text-center text-muted-foreground text-sm">
              Đang tải danh sách tham số cấu hình...
            </div>
          )}

          {!isLoading && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Card 1: Quy định mượn trả & Tiền phạt */}
              <Card>
                <CardHeader>
                  <div className="flex items-center gap-2">
                    <Layers className="h-5 w-5 text-primary" />
                    <CardTitle className="text-base">Quy định mượn trả &amp; Tiền phạt</CardTitle>
                  </div>
                  <CardDescription>
                    Các tham số mặc định áp dụng cho phân hệ Mượn/Trả và Xử lý Vi phạm.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  {circulationSettings.map((s) => (
                    <div key={s.key} className="space-y-1.5">
                      <Label htmlFor={s.key} className="text-xs font-semibold">
                        {getSettingLabel(s)}
                      </Label>
                      {s.valueType === 'Boolean' ? (
                        <div className="flex items-center justify-between pt-1">
                          <span className="text-xs text-muted-foreground">
                            {formValues[s.key] === 'true' ? 'Đang bật' : 'Đang tắt'}
                          </span>
                          <Switch
                            id={s.key}
                            checked={formValues[s.key] === 'true'}
                            onCheckedChange={(checked) => handleSwitchChange(s.key, checked)}
                          />
                        </div>
                      ) : (
                        <Input
                          id={s.key}
                          type={s.valueType === 'Number' ? 'number' : 'text'}
                          value={formValues[s.key] ?? ''}
                          onChange={(e) => handleInputChange(s.key, e.target.value)}
                          className="h-8 text-sm"
                        />
                      )}
                    </div>
                  ))}
                </CardContent>
              </Card>

              {/* Card 2: Thông tin thư viện */}
              <Card>
                <CardHeader>
                  <div className="flex items-center gap-2">
                    <Building className="h-5 w-5 text-primary" />
                    <CardTitle className="text-base">Thông tin liên hệ thư viện</CardTitle>
                  </div>
                  <CardDescription>
                    Hiển thị trên phiếu in mượn trả, email thông báo và thông tin chung.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  {librarySettings.map((s) => (
                    <div key={s.key} className="space-y-1.5">
                      <Label htmlFor={s.key} className="text-xs font-semibold">
                        {getSettingLabel(s)}
                      </Label>
                      <Input
                        id={s.key}
                        value={formValues[s.key] ?? ''}
                        onChange={(e) => handleInputChange(s.key, e.target.value)}
                        className="h-8 text-sm"
                      />
                    </div>
                  ))}
                </CardContent>
              </Card>

              {/* Card 3: Thông báo & Máy chủ SMTP */}
              <Card>
                <CardHeader>
                  <div className="flex items-center gap-2">
                    <Mail className="h-5 w-5 text-primary" />
                    <CardTitle className="text-base">Cấu hình gửi Email (SMTP)</CardTitle>
                  </div>
                  <CardDescription>
                    Máy chủ gửi email thông báo nhắc trả sách và xác nhận đặt trước.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  {notificationSettings.map((s) => (
                    <div key={s.key} className="space-y-1.5">
                      <Label htmlFor={s.key} className="text-xs font-semibold">
                        {getSettingLabel(s)}
                      </Label>
                      {s.isSecret ? (
                        <div className="relative">
                          <Input
                            id={s.key}
                            type={showSecrets[s.key] ? 'text' : 'password'}
                            value={formValues[s.key] ?? ''}
                            onChange={(e) => handleInputChange(s.key, e.target.value)}
                            className="h-8 pr-9 text-sm font-mono"
                            placeholder="•••••••• (Bảo mật)"
                          />
                          <button
                            type="button"
                            onClick={() => toggleShowSecret(s.key)}
                            className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                          >
                            {showSecrets[s.key] ? (
                              <EyeOff className="h-3.5 w-3.5" />
                            ) : (
                              <Eye className="h-3.5 w-3.5" />
                            )}
                          </button>
                        </div>
                      ) : (
                        <Input
                          id={s.key}
                          type={s.valueType === 'Number' ? 'number' : 'text'}
                          value={formValues[s.key] ?? ''}
                          onChange={(e) => handleInputChange(s.key, e.target.value)}
                          className="h-8 text-sm"
                        />
                      )}
                    </div>
                  ))}
                </CardContent>
              </Card>

              {/* Card 4: Bảo mật & Sao lưu hệ thống */}
              <Card>
                <CardHeader>
                  <div className="flex items-center gap-2">
                    <Shield className="h-5 w-5 text-primary" />
                    <CardTitle className="text-base">Sao lưu &amp; Vận hành hệ thống</CardTitle>
                  </div>
                  <CardDescription>
                    Chính sách sao lưu tự động và thời gian lưu trữ snapshot dữ liệu.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  {systemSettings.map((s) => (
                    <div key={s.key} className="space-y-1.5">
                      <Label htmlFor={s.key} className="text-xs font-semibold">
                        {getSettingLabel(s)}
                      </Label>
                      {s.valueType === 'Boolean' ? (
                        <div className="flex items-center justify-between pt-1">
                          <span className="text-xs text-muted-foreground">
                            {formValues[s.key] === 'true' ? 'Đang bật' : 'Đang tắt'}
                          </span>
                          <Switch
                            id={s.key}
                            checked={formValues[s.key] === 'true'}
                            onCheckedChange={(checked) => handleSwitchChange(s.key, checked)}
                          />
                        </div>
                      ) : (
                        <Input
                          id={s.key}
                          type={s.valueType === 'Number' ? 'number' : 'text'}
                          value={formValues[s.key] ?? ''}
                          onChange={(e) => handleInputChange(s.key, e.target.value)}
                          className="h-8 text-sm"
                        />
                      )}
                    </div>
                  ))}
                </CardContent>
              </Card>
            </div>
          )}
        </div>
      )}

      {/* TAB 2: CHÍNH SÁCH LƯU THÔNG (MS2-18) */}
      {activeTab === 'policies' && (
        <div className="space-y-6">
          <CirculationPolicyList />
          <CirculationPolicyPreview />
        </div>
      )}

      {/* TAB 3: GÓI CẤU HÌNH & SAO LƯU (MS2-32) */}
      {activeTab === 'packages' && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Download className="h-5 w-5 text-primary" />
                  <CardTitle className="text-base">Xuất gói cấu hình (Export Package)</CardTitle>
                </div>
                <CardDescription>
                  Tạo tệp JSON chứa toàn bộ thông số cấu hình hệ thống kèm mã băm kiểm tra tính toàn vẹn SHA-256. Dữ liệu nhạy cảm được tự động che để tránh lộ lọt.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="rounded-md border bg-muted/20 p-4 space-y-2 text-xs text-muted-foreground">
                  <div className="flex items-center gap-2 font-medium text-foreground">
                    <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                    <span>Định dạng chuẩn Semantic Versioning (v1.0.0)</span>
                  </div>
                  <div className="flex items-center gap-2 font-medium text-foreground">
                    <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                    <span>Mã hóa băm Checksum SHA-256 chống sửa đổi dữ liệu</span>
                  </div>
                  <div className="flex items-center gap-2 font-medium text-foreground">
                    <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                    <span>Tự động khử trường bí mật (SMTP password) sang [REDACTED]</span>
                  </div>
                </div>

                <Button
                  onClick={handleExportPackage}
                  disabled={isExporting}
                  className="w-full"
                >
                  <Download className="mr-2 h-4 w-4" />
                  {isExporting ? 'Đang xuất gói...' : 'Tải về tệp cấu hình (.json)'}
                </Button>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Upload className="h-5 w-5 text-primary" />
                  <CardTitle className="text-base">Nạp gói cấu hình (Import Package)</CardTitle>
                </div>
                <CardDescription>
                  Quy trình an toàn: Nạp file $\rightarrow$ Xác thực Checksum $\rightarrow$ So sánh Preview Diff $\rightarrow$ Xác nhận áp dụng trong Transaction cơ sở dữ liệu.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="rounded-md border bg-muted/20 p-4 space-y-2 text-xs text-muted-foreground">
                  <div className="flex items-center gap-2 font-medium text-foreground">
                    <CheckCircle2 className="h-4 w-4 text-primary" />
                    <span>Kiểm tra toàn vẹn mã Checksum trước khi xem trước</span>
                  </div>
                  <div className="flex items-center gap-2 font-medium text-foreground">
                    <CheckCircle2 className="h-4 w-4 text-primary" />
                    <span>Hiển thị rõ Before / After các trường Thêm mới &amp; Cập nhật</span>
                  </div>
                  <div className="flex items-center gap-2 font-medium text-foreground">
                    <CheckCircle2 className="h-4 w-4 text-primary" />
                    <span>Áp dụng an toàn trong Database Transaction &amp; tạo AuditLog</span>
                  </div>
                </div>

                <Button
                  onClick={() => setIsPackageDialogOpen(true)}
                  className="w-full"
                  variant="outline"
                >
                  <Upload className="mr-2 h-4 w-4" />
                  Mở trình nạp gói &amp; So sánh thay đổi
                </Button>
              </CardContent>
            </Card>
          </div>

          {/* Package History */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <History className="h-5 w-5 text-primary" />
                <CardTitle className="text-base">Lịch sử gói cấu hình (Configuration Packages)</CardTitle>
              </div>
              <CardDescription>
                Danh sách các gói cấu hình đã được ghi nhận trong cơ sở dữ liệu.
              </CardDescription>
            </CardHeader>
            <CardContent>
              {packageHistory.length === 0 ? (
                <div className="py-8 text-center text-muted-foreground text-sm">
                  Chưa có gói cấu hình nào được nạp vào hệ thống.
                </div>
              ) : (
                <div className="rounded-md border overflow-x-auto">
                  <table className="w-full text-xs text-left">
                    <thead className="bg-muted/40 border-b">
                      <tr>
                        <th className="p-3">Phiên bản</th>
                        <th className="p-3">Mã Checksum SHA-256</th>
                        <th className="p-3">Người thực hiện</th>
                        <th className="p-3">Thời gian (UTC)</th>
                        <th className="p-3 text-right">Số thiết lập</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {packageHistory.map((pkg) => (
                        <tr key={pkg.id} className="hover:bg-muted/20">
                          <td className="p-3 font-semibold">v{pkg.version}</td>
                          <td className="p-3 font-mono text-muted-foreground truncate max-w-xs">
                            {pkg.checksum}
                          </td>
                          <td className="p-3">{pkg.createdByDisplayName || 'Hệ thống'}</td>
                          <td className="p-3 text-muted-foreground">
                            {new Date(pkg.createdAtUtc).toLocaleString('vi-VN')}
                          </td>
                          <td className="p-3 text-right font-mono font-medium">
                            {pkg.settingsCount > 0 ? pkg.settingsCount : '-'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* Package Import & Diff Modal */}
      <ConfigPackageDialog
        open={isPackageDialogOpen}
        onOpenChange={setIsPackageDialogOpen}
        onSuccess={() => {
          showToast('Nạp gói cấu hình thành công! Các thông số đã được cập nhật toàn hệ thống.', 'success')
          loadSettings()
          loadPackages()
        }}
      />
    </div>
  )
}
