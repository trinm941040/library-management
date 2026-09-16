import { useState, useEffect, useCallback } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/common/components/ui/table'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { Badge } from '@/common/components/ui/badge'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/common/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import { Plus, Search, Check, Ban, Pencil, RefreshCw, CircleAlert } from 'lucide-react'
import type { CirculationPolicy } from '../circulation-policy-api'
import {
  getCirculationPolicies,
  activateCirculationPolicy,
  deactivateCirculationPolicy,
} from '../circulation-policy-api'
import { CirculationPolicyDialog } from './CirculationPolicyDialog'
import { Pagination } from '@/common/components/ui/pagination'

type CirculationPolicyListProps = { canManage: boolean }

export function CirculationPolicyList({ canManage }: CirculationPolicyListProps) {
  const [policies, setPolicies] = useState<CirculationPolicy[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all')
  const [memberGroupFilter, setMemberGroupFilter] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  // Dialog states
  const [dialogOpen, setDialogOpen] = useState(false)
  const [dialogMode, setDialogMode] = useState<'create' | 'edit' | 'new-version'>('create')
  const [selectedPolicy, setSelectedPolicy] = useState<CirculationPolicy | null>(null)

  const loadPolicies = useCallback(async () => {
    setIsLoading(true)
    setErrorMessage(null)
    try {
      const res = await getCirculationPolicies({
        search: search.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter === 'active',
        memberGroup: memberGroupFilter || undefined,
        pageNumber,
        pageSize,
      })
      setPolicies(res.items)
      setTotalCount(res.totalCount)
      setTotalPages(res.totalPages)
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : 'Không thể tải danh sách chính sách lưu thông.')
    } finally {
      setIsLoading(false)
    }
  }, [search, statusFilter, memberGroupFilter, pageNumber, pageSize])

  useEffect(() => {
    loadPolicies()
  }, [loadPolicies])

  const handleToggleActive = async (policy: CirculationPolicy) => {
    setErrorMessage(null)
    try {
      if (policy.isActive) {
        await deactivateCirculationPolicy(policy.id, policy.concurrencyToken)
      } else {
        await activateCirculationPolicy(policy.id, policy.concurrencyToken)
      }
      await loadPolicies()
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : 'Thao tác kích hoạt / tạm ngưng thất bại.')
    }
  }

  const handleCreateNew = () => {
    setSelectedPolicy(null)
    setDialogMode('create')
    setDialogOpen(true)
  }

  const handleEdit = (policy: CirculationPolicy) => {
    setSelectedPolicy(policy)
    setDialogMode('edit')
    setDialogOpen(true)
  }

  const handleNewVersion = (policy: CirculationPolicy) => {
    setSelectedPolicy(policy)
    setDialogMode('new-version')
    setDialogOpen(true)
  }

  const formatCurrency = (val: number) =>
    new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val)

  return (
    <div className="space-y-4">
      {errorMessage && (
        <div className="flex items-center gap-2 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
          <CircleAlert className="size-4" />
          {errorMessage}
        </div>
      )}

      {/* Card Danh sách & Bộ lọc theo chuẩn UI nhóm */}
      <Card>
        <CardHeader className="gap-4">
          <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
            <div>
              <CardTitle>Danh sách chính sách lưu thông</CardTitle>
              <CardDescription>
                {totalCount} chính sách phù hợp với bộ lọc hiện tại.
              </CardDescription>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <div className="relative w-full sm:w-64">
                <Search className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  className="pl-9"
                  placeholder="Tìm theo tên chính sách..."
                  value={search}
                  onChange={(e) => { setSearch(e.target.value); setPageNumber(1) }}
                />
              </div>

              <Select
                value={statusFilter}
                onValueChange={(val) => { setStatusFilter(val as 'all' | 'active' | 'inactive'); setPageNumber(1) }}
              >
                <SelectTrigger className="w-full sm:w-40" aria-label="Lọc trạng thái">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả trạng thái</SelectItem>
                  <SelectItem value="active">Đang hiệu lực</SelectItem>
                  <SelectItem value="inactive">Tạm ngưng</SelectItem>
                </SelectContent>
              </Select>

              <Select
                value={memberGroupFilter || 'all'}
                onValueChange={(val) => { setMemberGroupFilter(val === 'all' ? '' : val); setPageNumber(1) }}
              >
                <SelectTrigger className="w-full sm:w-44" aria-label="Lọc nhóm độc giả">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tất cả nhóm</SelectItem>
                  <SelectItem value="Student">Sinh viên</SelectItem>
                  <SelectItem value="Faculty">Giảng viên / Cán bộ</SelectItem>
                  <SelectItem value="Researcher">Nghiên cứu sinh</SelectItem>
                  <SelectItem value="General">Độc giả tự do</SelectItem>
                </SelectContent>
              </Select>

              <Button variant="outline" size="icon" loading={isLoading} loadingLabel="Đang tải lại chính sách" onClick={loadPolicies} title="Làm mới">
                <RefreshCw className="size-4" />
              </Button>

              {canManage && (
                <Button onClick={handleCreateNew}>
                  <Plus className="mr-1 size-4" />
                  Thêm chính sách
                </Button>
              )}
            </div>
          </div>
        </CardHeader>

        <CardContent>
          <div className="max-h-[55vh] overflow-auto rounded-md border bg-background">
        <Table>
          <TableHeader className="sticky top-0 z-10 bg-background">
            <TableRow>
              <TableHead>Tên chính sách</TableHead>
              <TableHead>Phiên bản</TableHead>
              <TableHead>Phạm vi (Scope)</TableHead>
              <TableHead>Hạn mức mượn</TableHead>
              <TableHead>Mức phạt / ngày</TableHead>
              <TableHead>Hiệu lực</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead className="text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {policies.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} className="py-8 text-center text-muted-foreground">
                  {isLoading ? 'Đang tải danh sách chính sách...' : 'Không tìm thấy chính sách lưu thông nào.'}
                </TableCell>
              </TableRow>
            ) : (
              policies.map((p) => (
                <TableRow key={p.id}>
                  <TableCell className="font-medium">
                    <div>{p.name}</div>
                    {p.description && (
                      <div className="text-xs text-muted-foreground">{p.description}</div>
                    )}
                  </TableCell>

                  <TableCell>
                    <Badge variant="outline">v{p.version}</Badge>
                  </TableCell>

                  <TableCell className="text-xs">
                    <div>
                      <span className="text-muted-foreground">Độc giả:</span>{' '}
                      <span className="font-semibold">{p.memberGroup || 'Tất cả'}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Loại sách:</span>{' '}
                      <span className="font-semibold">{p.documentType || 'Tất cả'}</span>
                    </div>
                  </TableCell>

                  <TableCell className="text-xs">
                    <div>
                      <span className="font-semibold">{p.maxLoanBooks}</span> cuốn /{' '}
                      <span className="font-semibold">{p.loanPeriodDays}</span> ngày
                    </div>
                    <div className="text-muted-foreground">
                      Gia hạn: {p.maxRenewals} lần ({p.renewalPeriodDays} ngày/lần)
                    </div>
                  </TableCell>

                  <TableCell className="text-xs">
                    <div className="font-semibold text-destructive">{formatCurrency(p.finePerDay)}</div>
                    <div className="text-muted-foreground">Trần: {formatCurrency(p.maxFineAmount)}</div>
                  </TableCell>

                  <TableCell className="text-xs">
                    <div>Từ: {new Date(p.effectiveFrom).toLocaleDateString('vi-VN')}</div>
                    {p.effectiveTo ? (
                      <div>Đến: {new Date(p.effectiveTo).toLocaleDateString('vi-VN')}</div>
                    ) : (
                      <div className="text-muted-foreground">Vô thời hạn</div>
                    )}
                  </TableCell>

                  <TableCell>
                    {p.isActive ? (
                      <Badge className="bg-green-600 text-white hover:bg-green-700">Đang bật</Badge>
                    ) : (
                      <Badge variant="secondary">Tạm ngưng</Badge>
                    )}
                  </TableCell>

                  <TableCell className="text-right">
                    {canManage && (
                    <div className="flex items-center justify-end gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        title={p.isActive ? 'Tạm ngưng chính sách' : 'Kích hoạt chính sách'}
                        onClick={() => handleToggleActive(p)}
                      >
                        {p.isActive ? (
                          <Ban className="h-4 w-4 text-amber-600" />
                        ) : (
                          <Check className="h-4 w-4 text-green-600" />
                        )}
                      </Button>

                      <Button
                        variant="ghost"
                        size="icon"
                        title="Tạo phiên bản mới (New Version)"
                        onClick={() => handleNewVersion(p)}
                      >
                        <Plus className="h-4 w-4 text-primary" />
                      </Button>

                      <Button
                        variant="ghost"
                        size="icon"
                        title="Chỉnh sửa chính sách"
                        onClick={() => handleEdit(p)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </div>
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
          </div>

          <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-muted-foreground">
              Tổng số: <span className="font-semibold">{totalCount}</span> chính sách.
            </p>
            {totalPages > 0 && (
              <Pagination
                currentPage={pageNumber}
                totalPages={totalPages}
                pageSize={pageSize}
                onPageChange={setPageNumber}
                onPageSizeChange={(size) => {
                  setPageSize(size)
                  setPageNumber(1)
                }}
              />
            )}
          </div>
        </CardContent>
      </Card>

      {canManage && <CirculationPolicyDialog
        open={dialogOpen}
        mode={dialogMode}
        policy={selectedPolicy}
        onOpenChange={setDialogOpen}
        onSuccess={loadPolicies}
      />}
    </div>
  )
}
