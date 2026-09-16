import { Bookmark, Building2, Clock, Filter, RotateCcw, Search } from 'lucide-react'
import { Button } from '@/common/components/ui/button'
import { Label } from '@/common/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import type { ReportDefinition } from '../reports-api'

type FilterBuilderProps = {
  definition: ReportDefinition
  filters: Record<string, string>
  branches: { id: string; name: string }[]
  userBranch?: { id: string; name: string } | null
  isGlobalAdmin: boolean
  loading: boolean
  onChange: (key: string, value: string) => void
  onReset: () => void
  onPreview: () => void
  onOpenSaveDialog: () => void
}

export function FilterBuilder({
  definition,
  filters,
  branches,
  userBranch,
  isGlobalAdmin,
  loading,
  onChange,
  onReset,
  onPreview,
  onOpenSaveDialog,
}: FilterBuilderProps) {
  return (
    <div className="bg-card border rounded-lg p-4 shadow-xs space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-primary" />
          <h3 className="text-sm font-bold text-foreground">Bộ lọc tiêu chí báo cáo</h3>
        </div>
        <Button
          variant="ghost"
          size="sm"
          className="text-xs h-7 text-muted-foreground hover:text-foreground"
          onClick={onReset}
          disabled={loading}
        >
          <RotateCcw className="w-3 h-3 mr-1" /> Đặt lại
        </Button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
        {definition.filterFields.map((field) => {
          if (field.type === 'branch') {
            return (
              <div key={field.key} className="space-y-1">
                <Label className="text-xs text-muted-foreground flex items-center gap-1">
                  <Building2 className="w-3 h-3" /> {field.label}
                </Label>
                {isGlobalAdmin ? (
                  <Select
                    value={filters[field.key] || 'all'}
                    onValueChange={(val) => onChange(field.key, val)}
                    disabled={loading}
                  >
                    <SelectTrigger className="h-9 text-xs w-full">
                      <SelectValue placeholder="Tất cả chi nhánh" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">Tất cả chi nhánh</SelectItem>
                      {branches.map((b) => (
                        <SelectItem key={b.id} value={b.id}>
                          {b.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : (
                  <div className="h-9 border rounded-md px-3 flex items-center text-xs bg-muted/40 font-medium text-foreground">
                    {userBranch?.name || 'Chi nhánh của bạn'}
                  </div>
                )}
              </div>
            )
          }

          if (field.type === 'select') {
            return (
              <div key={field.key} className="space-y-1">
                <Label className="text-xs text-muted-foreground flex items-center gap-1">
                  {field.key.includes('range') ? <Clock className="w-3 h-3" /> : null}
                  {field.label}
                </Label>
                <Select
                  value={filters[field.key] || field.options?.[0]?.value || 'all'}
                  onValueChange={(val) => onChange(field.key, val)}
                  disabled={loading}
                >
                  <SelectTrigger className="h-9 text-xs w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {field.options?.map((opt) => (
                      <SelectItem key={opt.value} value={opt.value}>
                        {opt.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )
          }

          return null
        })}
      </div>

      <div className="flex items-center justify-between pt-2 border-t">
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="text-xs h-8"
          onClick={onOpenSaveDialog}
          disabled={loading}
        >
          <Bookmark className="w-3.5 h-3.5 mr-1 text-primary" /> Lưu bộ lọc hiện tại
        </Button>

        <Button
          type="button"
          size="sm"
          className="text-xs h-8"
          onClick={onPreview}
          disabled={loading}
        >
          <Search className="w-3.5 h-3.5 mr-1" /> Chạy xem trước
        </Button>
      </div>
    </div>
  )
}
