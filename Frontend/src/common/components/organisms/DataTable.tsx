import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import type { ReactNode } from 'react'
import { Button } from '@/common/components/ui/button'
import { Pagination } from '@/common/components/ui/pagination'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import { ScreenState } from '../molecules/ScreenState'

export type SortDirection = 'asc' | 'desc'
export type DataTableColumn<Row> = {
  id: string
  header: string
  cell: (row: Row) => ReactNode
  sortable?: boolean
  className?: string
}

export function DataTable<Row>({
  caption,
  rows,
  columns,
  getRowId,
  isLoading,
  error,
  emptyTitle = 'Chưa có dữ liệu',
  emptyDescription,
  onRetry,
  sort,
  onSortChange,
  selectedIds = new Set(),
  onSelectionChange,
  bulkActions,
  filters,
  page,
  totalPages,
  totalCount,
  onPageChange,
  pageSize,
  onPageSizeChange,
}: {
  caption: string
  rows: readonly Row[]
  columns: readonly DataTableColumn<Row>[]
  getRowId: (row: Row) => string
  isLoading?: boolean
  error?: string
  emptyTitle?: string
  emptyDescription?: string
  onRetry?: () => void
  sort?: { columnId: string; direction: SortDirection }
  onSortChange?: (columnId: string, direction: SortDirection) => void
  selectedIds?: ReadonlySet<string>
  onSelectionChange?: (ids: Set<string>) => void
  bulkActions?: ReactNode
  filters?: ReactNode
  page?: number
  totalPages?: number
  totalCount?: number
  onPageChange?: (page: number) => void
  pageSize?: number
  onPageSizeChange?: (pageSize: number) => void
}) {
  const selectable = Boolean(onSelectionChange)
  const visibleIds = rows.map(getRowId)
  const allSelected = visibleIds.length > 0 && visibleIds.every((id) => selectedIds.has(id))
  const toggleAll = () =>
    onSelectionChange?.(
      allSelected
        ? new Set([...selectedIds].filter((id) => !visibleIds.includes(id)))
        : new Set([...selectedIds, ...visibleIds]),
    )
  const toggleRow = (id: string) => {
    const next = new Set(selectedIds)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    onSelectionChange?.(next)
  }
  return (
    <div className="grid min-w-0 max-w-full gap-3">
      {filters ? <div className="min-w-0 max-w-full">{filters}</div> : null}
      {selectable && selectedIds.size > 0 ? (
        <div
          className="flex flex-wrap items-center justify-between gap-3 rounded-md border bg-muted/40 p-2"
          role="region"
          aria-label="Thao tác hàng loạt"
        >
          <span className="text-sm font-medium">Đã chọn {selectedIds.size} dòng</span>
          <div className="flex w-full flex-wrap gap-2 sm:w-auto max-sm:[&>*]:flex-1">{bulkActions}</div>
        </div>
      ) : null}
      <div className="min-w-0 max-w-full rounded-md border">
        <Table aria-busy={isLoading}>
          <caption className="sr-only">{caption}</caption>
          <TableHeader>
            <TableRow>
              {selectable ? (
                <TableHead className="w-10">
                  <input
                    type="checkbox"
                    checked={allSelected}
                    onChange={toggleAll}
                    aria-label="Chọn tất cả dòng trên trang"
                    className="size-4 accent-primary"
                  />
                </TableHead>
              ) : null}
              {columns.map((column) => (
                <TableHead
                  key={column.id}
                  className={column.className}
                  aria-sort={
                    column.sortable
                      ? sort?.columnId === column.id
                        ? sort.direction === 'asc'
                          ? 'ascending'
                          : 'descending'
                        : 'none'
                      : undefined
                  }
                >
                  {column.sortable && onSortChange ? (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="-ml-2"
                      onClick={() =>
                        onSortChange(
                          column.id,
                          sort?.columnId === column.id && sort.direction === 'asc' ? 'desc' : 'asc',
                        )
                      }
                      aria-label={`Sắp xếp theo ${column.header}`}
                    >
                      {column.header}
                      {sort?.columnId !== column.id ? (
                        <ChevronsUpDown aria-hidden="true" />
                      ) : sort.direction === 'asc' ? (
                        <ArrowUp aria-hidden="true" />
                      ) : (
                        <ArrowDown aria-hidden="true" />
                      )}
                    </Button>
                  ) : (
                    column.header
                  )}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {!error
              ? rows.map((row) => {
                  const id = getRowId(row)
                  return (
                    <TableRow key={id} data-state={selectedIds.has(id) ? 'selected' : undefined}>
                      {selectable ? (
                        <TableCell>
                          <input
                            type="checkbox"
                            checked={selectedIds.has(id)}
                            onChange={() => toggleRow(id)}
                            aria-label={`Chọn dòng ${id}`}
                            className="size-4 accent-primary"
                          />
                        </TableCell>
                      ) : null}
                      {columns.map((column) => (
                        <TableCell key={column.id} className={column.className}>
                          {column.cell(row)}
                        </TableCell>
                      ))}
                    </TableRow>
                  )
                })
              : null}
            {(isLoading && rows.length === 0) || error || rows.length === 0 ? (
              <TableRow className="hover:bg-transparent">
                <TableCell
                  colSpan={columns.length + (selectable ? 1 : 0)}
                  className="p-0 whitespace-normal"
                >
                  {isLoading ? (
                    <ScreenState compact kind="loading" title="Đang tải dữ liệu..." />
                  ) : error ? (
                    <ScreenState
                      compact
                      kind="error"
                      title="Không thể tải dữ liệu"
                      description={error}
                      actionLabel={onRetry ? 'Thử lại' : undefined}
                      onAction={onRetry}
                    />
                  ) : (
                    <ScreenState
                      compact
                      kind="empty"
                      title={emptyTitle}
                      description={emptyDescription}
                    />
                  )}
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </div>
      {page !== undefined && totalPages !== undefined && totalPages > 0 && onPageChange ? (
        <div className="flex min-w-0 flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-muted-foreground">
            {typeof totalCount === 'number' ? `${totalCount} kết quả` : ''}
          </p>
          <Pagination
            className="w-full sm:w-auto"
            currentPage={page}
            totalPages={totalPages}
            totalCount={totalCount}
            itemCount={rows.length}
            loading={isLoading}
            onPageChange={onPageChange}
            pageSize={pageSize}
            onPageSizeChange={onPageSizeChange}
          />
        </div>
      ) : null}
    </div>
  )
}
