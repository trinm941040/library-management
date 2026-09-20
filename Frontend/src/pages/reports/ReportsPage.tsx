import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  AlertCircle,
  CheckCircle2,
  Coins,
  Download,
  FileSpreadsheet,
  FileText,
  Layers,
  Loader2,
  RefreshCw,
  Users,
} from 'lucide-react'
import { useAuth } from '@/auth/AuthProvider'
import { Button } from '@/common/components/ui/button'
import { Card } from '@/common/components/ui/card'
import { fetchDashboardBranches } from '../dashboard/dashboard-api'
import { FilterBuilder } from './components/FilterBuilder'
import { ReportPreviewTable } from './components/ReportPreviewTable'
import { SaveFilterDialog } from './components/SaveFilterDialog'
import { SavedFiltersBar } from './components/SavedFiltersBar'
import {
  createSavedFilter,
  deleteSavedFilter,
  downloadReportFile,
  exportReport,
  fetchReportDefinitions,
  fetchSavedFilters,
  previewReport,
  type ReportDefinition,
  type ReportExportResult,
  type ReportPreviewResult,
  type SavedFilter,
} from './reports-api'

const REPORT_ICONS: Record<string, React.ComponentType<{ className?: string }>> = {
  circulation_loans: RefreshCw,
  inventory_copies: Layers,
  financial_fines: Coins,
  members_activity: Users,
}

export function ReportsPage() {
  const { user } = useAuth()

  const [definitions, setDefinitions] = useState<ReportDefinition[]>([])
  const [selectedReportCode, setSelectedReportCode] = useState<string>('circulation_loans')
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([])

  const [filters, setFilters] = useState<Record<string, string>>({
    range: 'month',
    status: 'all',
  })

  const [preview, setPreview] = useState<ReportPreviewResult | null>(null)
  const [previewLoading, setPreviewLoading] = useState(false)
  const [exportLoading, setExportLoading] = useState(false)
  const [, setLastExport] = useState<ReportExportResult | null>(null)

  const [savedFilters, setSavedFilters] = useState<SavedFilter[]>([])
  const [activeFilterId, setActiveFilterId] = useState<string | null>(null)
  const [saveDialogOpen, setSaveDialogOpen] = useState(false)

  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const isGlobalAdmin = useMemo(() => {
    if (!user) return false
    return user.roles.includes('Administrator') || !user.branch
  }, [user])

  const activeDefinition = useMemo(() => {
    return definitions.find((d) => d.code === selectedReportCode) || definitions[0]
  }, [definitions, selectedReportCode])

  // Load report definitions & branches on mount
  useEffect(() => {
    async function loadMeta() {
      try {
        const [defs, branchList] = await Promise.all([
          fetchReportDefinitions(),
          fetchDashboardBranches().catch(() => []),
        ])
        setDefinitions(defs)
        setBranches(branchList)
        if (defs.length > 0 && !defs.some((d) => d.code === selectedReportCode)) {
          setSelectedReportCode(defs[0].code)
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Không thể tải danh mục báo cáo.')
      }
    }
    void loadMeta()
  }, [])

  // Load saved filters when active report changes
  const loadSavedFilters = useCallback(async () => {
    if (!selectedReportCode) return
    try {
      const list = await fetchSavedFilters(selectedReportCode)
      setSavedFilters(list)
    } catch {
      setSavedFilters([])
    }
  }, [selectedReportCode])

  useEffect(() => {
    void loadSavedFilters()
  }, [loadSavedFilters])

  // Execute preview
  const handleRunPreview = useCallback(
    async (pageNumber = 1) => {
      if (!selectedReportCode) return
      setPreviewLoading(true)
      setError(null)

      try {
        const res = await previewReport({
          reportCode: selectedReportCode,
          filters,
          pageNumber,
          pageSize: 20,
        })
        setPreview(res)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Không thể tạo xem trước báo cáo.')
      } finally {
        setPreviewLoading(false)
      }
    },
    [selectedReportCode, filters],
  )

  // Trigger preview when switching report
  useEffect(() => {
    if (selectedReportCode && definitions.length > 0) {
      void handleRunPreview(1)
    }
  }, [selectedReportCode, definitions.length])

  // Handle report tab switch
  const handleReportChange = (code: string) => {
    setSelectedReportCode(code)
    setActiveFilterId(null)
    setFilters({ range: 'month', status: 'all' })
  }

  // Filter changes
  const handleFilterChange = (key: string, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }))
    setActiveFilterId(null)
  }

  const handleResetFilters = () => {
    setFilters({ range: 'month', status: 'all' })
    setActiveFilterId(null)
  }

  // Saved filters actions
  const handleApplySavedFilter = (sf: SavedFilter) => {
    try {
      const parsed = JSON.parse(sf.criteria) as Record<string, string>
      setFilters(parsed)
      setActiveFilterId(sf.id)
      setNotice(`Đã áp dụng bộ lọc: ${sf.name}`)
      setTimeout(() => setNotice(null), 3000)
    } catch {
      setError('Định dạng bộ lọc đã lưu không hợp lệ.')
    }
  }

  const handleSaveFilter = async (name: string) => {
    const created = await createSavedFilter({
      name,
      scope: selectedReportCode,
      criteria: JSON.stringify(filters),
    })
    setActiveFilterId(created.id)
    setNotice(`Đã lưu bộ lọc "${name}".`)
    setTimeout(() => setNotice(null), 3000)
    await loadSavedFilters()
  }

  const handleDeleteSavedFilter = async (id: string) => {
    try {
      await deleteSavedFilter(id)
      if (activeFilterId === id) setActiveFilterId(null)
      await loadSavedFilters()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể xóa bộ lọc.')
    }
  }

  // Export action
  const handleExport = async () => {
    if (!selectedReportCode) return
    setExportLoading(true)
    setError(null)

    try {
      const res = await exportReport({
        reportCode: selectedReportCode,
        filters,
        format: 'csv',
      })
      setLastExport(res)

      // Automatically trigger download
      await downloadReportFile(res.reportId, res.fileName)
      setNotice(`Đã xuất thành công ${res.totalRecords.toLocaleString('vi-VN')} dòng dữ liệu và tải về máy!`)
      setTimeout(() => setNotice(null), 4500)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể xuất tệp báo cáo.')
    } finally {
      setExportLoading(false)
    }
  }

  return (
    <div className="content-wrap space-y-6 pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b pb-5">
        <div>
          <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            <FileSpreadsheet className="w-3.5 h-3.5" />
            <span>Trung tâm báo cáo & Xuất dữ liệu</span>
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground mt-1">
            Báo cáo vận hành thư viện
          </h1>
          <p className="text-sm text-muted-foreground">
            Tùy biến tiêu chí lọc, lưu bộ lọc cá nhân, xem trước phân trang và xuất tệp CSV chuẩn UTF-8.
          </p>
        </div>

        <div className="flex items-center gap-2.5">
          <Button
            size="sm"
            onClick={handleExport}
            disabled={exportLoading || previewLoading || !preview || preview.rows.length === 0}
            className="text-xs h-9 px-3.5"
          >
            {exportLoading ? (
              <>
                <Loader2 className="w-3.5 h-3.5 mr-1.5 animate-spin" /> Đang xuất tệp...
              </>
            ) : (
              <>
                <Download className="w-3.5 h-3.5 mr-1.5" /> Xuất tệp CSV
              </>
            )}
          </Button>
        </div>
      </div>

      {/* Notice / Error banners */}
      {notice && (
        <div className="flex items-center gap-2 p-3 text-xs bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800 rounded-md">
          <CheckCircle2 className="w-4 h-4 shrink-0" />
          <span>{notice}</span>
        </div>
      )}

      {error && (
        <div className="flex items-center justify-between p-3 text-xs bg-destructive/10 text-destructive border border-destructive/20 rounded-md">
          <div className="flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{error}</span>
          </div>
          <Button variant="ghost" size="sm" className="h-6 text-xs" onClick={() => setError(null)}>
            Đóng
          </Button>
        </div>
      )}

      {/* Report Selection Tabs / Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        {definitions.map((def) => {
          const isSelected = def.code === selectedReportCode
          const Icon = REPORT_ICONS[def.code] || FileText

          return (
            <Card
              key={def.code}
              className={`cursor-pointer transition-all duration-200 p-3.5 ${
                isSelected
                  ? 'border-primary bg-primary/5 shadow-xs ring-1 ring-primary'
                  : 'hover:border-primary/40 hover:bg-muted/30'
              }`}
              onClick={() => handleReportChange(def.code)}
            >
              <div className="flex items-start gap-3">
                <div
                  className={`w-9 h-9 rounded-lg flex items-center justify-center shrink-0 ${
                    isSelected ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'
                  }`}
                >
                  <Icon className="w-4 h-4" />
                </div>
                <div className="min-w-0">
                  <h4 className="text-xs font-bold text-foreground truncate">{def.name}</h4>
                  <p className="text-[11px] text-muted-foreground line-clamp-2 mt-0.5">
                    {def.description}
                  </p>
                </div>
              </div>
            </Card>
          )
        })}
      </div>

      {activeDefinition && (
        <>
          {/* Saved filters bar */}
          <SavedFiltersBar
            savedFilters={savedFilters}
            activeFilterId={activeFilterId}
            onApply={handleApplySavedFilter}
            onDelete={(id) => void handleDeleteSavedFilter(id)}
          />

          {/* Filter builder */}
          <FilterBuilder
            definition={activeDefinition}
            filters={filters}
            branches={branches}
            userBranch={user?.branch}
            isGlobalAdmin={isGlobalAdmin}
            loading={previewLoading || exportLoading}
            onChange={handleFilterChange}
            onReset={handleResetFilters}
            onPreview={() => void handleRunPreview(1)}
            onOpenSaveDialog={() => setSaveDialogOpen(true)}
          />

          {/* Report preview table */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-bold text-foreground flex items-center gap-1.5">
                <FileSpreadsheet className="w-4 h-4 text-primary" />
                Bảng xem trước dữ liệu: {activeDefinition.name}
              </h3>
              {preview && (
                <span className="text-xs text-muted-foreground">
                  Cập nhật lúc:{' '}
                  {new Date(preview.generatedAtUtc).toLocaleTimeString('vi-VN', {
                    hour: '2-digit',
                    minute: '2-digit',
                  })}
                </span>
              )}
            </div>

            <ReportPreviewTable
              preview={preview}
              loading={previewLoading}
              onPageChange={(page) => void handleRunPreview(page)}
            />
          </div>
        </>
      )}

      {/* Save Filter Dialog */}
      <SaveFilterDialog
        open={saveDialogOpen}
        onOpenChange={setSaveDialogOpen}
        onSave={handleSaveFilter}
      />
    </div>
  )
}
