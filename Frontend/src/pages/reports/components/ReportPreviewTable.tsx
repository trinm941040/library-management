import { FileSpreadsheet } from 'lucide-react'
import { Spinner } from '@/common/components/atoms/Spinner'
import { Badge } from '@/common/components/ui/badge'
import { Pagination } from '@/common/components/ui/pagination'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import type { ReportColumnDefinition, ReportPreviewResult } from '../reports-api'

type ReportPreviewTableProps = {
  preview: ReportPreviewResult | null
  loading: boolean
  onPageChange: (page: number) => void
}

export function ReportPreviewTable({ preview, loading, onPageChange }: ReportPreviewTableProps) {
  if (loading) {
    return (
      <div className="border rounded-lg p-12 text-center bg-card shadow-xs">
        <Spinner size="lg" decorative className="mx-auto mb-3" />
        <p className="text-sm font-semibold text-foreground">Đang tổng hợp dữ liệu báo cáo...</p>
        <p className="text-xs text-muted-foreground mt-1">Đang phân tích và truy vấn số liệu theo bộ lọc của bạn.</p>
      </div>
    )
  }

  if (!preview) {
    return (
      <div className="border border-dashed rounded-lg p-12 text-center bg-card/50">
        <FileSpreadsheet className="w-10 h-10 text-muted-foreground mx-auto mb-3 opacity-60" />
        <p className="text-sm font-medium text-foreground">Chưa chạy xem trước báo cáo</p>
        <p className="text-xs text-muted-foreground mt-1">
          Chọn các tiêu chí lọc bên trên và bấm nút "Chạy xem trước" để hiển thị dữ liệu phân trang.
        </p>
      </div>
    )
  }

  const { columns, rows, totalRows, pageNumber, pageSize, totalPages, summaryStats } = preview

  const renderCellContent = (col: ReportColumnDefinition, val: unknown) => {
    if (val === null || val === undefined || val === '') return '-'

    if (col.type === 'currency') {
      const num = typeof val === 'number' ? val : Number(val)
      return isNaN(num) ? '-' : `${num.toLocaleString('vi-VN')} ₫`
    }

    if (col.type === 'datetime') {
      const d = new Date(String(val))
      if (isNaN(d.getTime())) return String(val)
      return `${d.toLocaleDateString('vi-VN')} ${d.toLocaleTimeString('vi-VN', {
        hour: '2-digit',
        minute: '2-digit',
      })}`
    }

    if (col.type === 'badge') {
      const str = String(val)
      const isDanger = str.includes('Quá hạn') || str.includes('Hỏng') || str.includes('Mất') || str.includes('Lost') || str.includes('Damaged')
      const isSuccess = str.includes('Đã trả') || str.includes('Hoạt động') || str.includes('Active') || str.includes('Available') || str.includes('Sẵn sàng') || str.includes('Đã giải quyết')

      return (
        <Badge
          variant={isDanger ? 'destructive' : isSuccess ? 'secondary' : 'outline'}
          className="text-[11px] font-normal"
        >
          {str}
        </Badge>
      )
    }

    return String(val)
  }

  return (
    <div className="space-y-4">
      {/* Summary statistics strip */}
      {summaryStats && Object.keys(summaryStats).length > 0 && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          {Object.entries(summaryStats).map(([key, val]) => (
            <div key={key} className="bg-card border rounded-lg p-3 shadow-xs">
              <span className="text-xs text-muted-foreground">{key}</span>
              <div className="text-lg font-bold text-foreground mt-0.5">
                {typeof val === 'number' && key.toLowerCase().includes('tiền')
                  ? `${val.toLocaleString('vi-VN')} ₫`
                  : typeof val === 'number'
                  ? val.toLocaleString('vi-VN')
                  : String(val)}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Table container */}
      <div className="border rounded-lg bg-card shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <Table>
            <TableHeader className="bg-muted/40">
              <TableRow>
                {columns.map((col) => (
                  <TableHead
                    key={col.key}
                    className={`text-xs font-semibold whitespace-nowrap ${
                      col.align === 'right'
                        ? 'text-right'
                        : col.align === 'center'
                        ? 'text-center'
                        : 'text-left'
                    }`}
                  >
                    {col.label}
                  </TableHead>
                ))}
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={columns.length}
                    className="text-center py-10 text-xs text-muted-foreground"
                  >
                    Không có bản ghi nào phù hợp với bộ lọc hiện tại.
                  </TableCell>
                </TableRow>
              ) : (
                rows.map((row, idx) => (
                  <TableRow key={idx} className="hover:bg-muted/30 text-xs">
                    {columns.map((col) => (
                      <TableCell
                        key={col.key}
                        className={`py-2.5 whitespace-nowrap ${
                          col.align === 'right'
                            ? 'text-right'
                            : col.align === 'center'
                            ? 'text-center'
                            : 'text-left'
                        }`}
                      >
                        {renderCellContent(col, row[col.key])}
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {/* Pagination footer */}
        <div className="flex flex-wrap items-center justify-between gap-2 border-t px-4 py-3 text-xs text-muted-foreground">
          <span>
            Hiển thị{' '}
            <strong className="text-foreground">
              {rows.length > 0 ? (pageNumber - 1) * pageSize + 1 : 0} -{' '}
              {Math.min(pageNumber * pageSize, totalRows)}
            </strong>{' '}
            trong tổng số <strong className="text-foreground">{totalRows.toLocaleString('vi-VN')}</strong> bản ghi
          </span>

          <Pagination
            currentPage={pageNumber}
            totalPages={totalPages}
            totalCount={totalRows}
            itemCount={rows.length}
            pageSize={pageSize}
            loading={loading}
            onPageChange={onPageChange}
          />
        </div>
      </div>
    </div>
  )
}
