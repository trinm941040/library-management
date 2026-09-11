import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { Badge } from '@/common/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/common/components/ui/select'
import { BookOpen, Check, CircleAlert, RefreshCw } from 'lucide-react'
import type { PolicyPreviewResponse } from '../circulation-policy-api'
import { previewCirculationPolicy } from '../circulation-policy-api'

export function CirculationPolicyPreview() {
  const [memberGroup, setMemberGroup] = useState('Student')
  const [documentType, setDocumentType] = useState('Giáo trình')
  const [testOverdueDays, setTestOverdueDays] = useState(3)
  const [testBookPrice, setTestBookPrice] = useState(100000)
  const [isLoading, setIsLoading] = useState(false)
  const [previewResult, setPreviewResult] = useState<PolicyPreviewResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleRunPreview = async () => {
    setIsLoading(true)
    setError(null)
    try {
      const res = await previewCirculationPolicy({
        memberGroup: memberGroup || undefined,
        documentType: documentType || undefined,
        testOverdueDays,
        testBookPrice,
      })
      setPreviewResult(res)
    } catch (err: any) {
      setError(err?.message || 'Không thể chạy xem trước chính sách.')
    } finally {
      setIsLoading(false)
    }
  }

  const formatCurrency = (val: number) =>
    new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val)

  return (
    <Card className="border-primary/20 shadow-sm">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2 text-lg font-bold">
              <BookOpen className="h-5 w-5 text-primary" />
              Công cụ xem trước chính sách
            </CardTitle>
            <CardDescription>
              Kiểm tra trực tiếp kết quả phân giải chính sách (Resolver), giới hạn mượn và mức phạt với dữ liệu mẫu.
            </CardDescription>
          </div>
          <Button onClick={handleRunPreview} disabled={isLoading}>
            <RefreshCw className={`mr-2 h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
            {isLoading ? 'Đang tính toán...' : 'Chạy xem trước'}
          </Button>
        </div>
      </CardHeader>

      <CardContent className="space-y-5">
        {/* Bộ lọc thử nghiệm */}
        <div className="grid grid-cols-1 gap-3 rounded-lg border bg-muted/30 p-4 sm:grid-cols-4">
          <div className="flex flex-col justify-between space-y-1.5">
            <Label htmlFor="preview-member-group" className="text-sm font-medium">
              Nhóm độc giả thử nghiệm
            </Label>
            <Select
              value={memberGroup || 'all'}
              onValueChange={(val) => setMemberGroup(val === 'all' ? '' : val)}
            >
              <SelectTrigger id="preview-member-group" className="h-9 w-full">
                <SelectValue placeholder="Chọn nhóm" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả nhóm</SelectItem>
                <SelectItem value="Student">Sinh viên</SelectItem>
                <SelectItem value="Faculty">Giảng viên / Cán bộ</SelectItem>
                <SelectItem value="Researcher">Nghiên cứu sinh</SelectItem>
                <SelectItem value="General">Độc giả tự do</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col justify-between space-y-1.5">
            <Label htmlFor="preview-doc-type" className="text-sm font-medium">
              Thể loại sách thử nghiệm
            </Label>
            <Select
              value={documentType || 'all'}
              onValueChange={(val) => setDocumentType(val === 'all' ? '' : val)}
            >
              <SelectTrigger id="preview-doc-type" className="h-9 w-full">
                <SelectValue placeholder="Chọn thể loại" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả thể loại</SelectItem>
                <SelectItem value="Giáo trình">Giáo trình</SelectItem>
                <SelectItem value="Tham khảo">Tài liệu tham khảo</SelectItem>
                <SelectItem value="Luận văn">Luận văn / Khóa luận</SelectItem>
                <SelectItem value="Công nghệ thông tin">Công nghệ thông tin</SelectItem>
                <SelectItem value="Khoa học">Khoa học</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col justify-between space-y-1.5">
            <Label htmlFor="test-overdue" className="text-sm font-medium">
              Giả lập trễ hạn (ngày)
            </Label>
            <Input
              id="test-overdue"
              type="number"
              min="0"
              max="100"
              className="h-9 w-full"
              value={testOverdueDays}
              onChange={(e) => setTestOverdueDays(Number(e.target.value))}
            />
          </div>

          <div className="flex flex-col justify-between space-y-1.5">
            <Label htmlFor="test-price" className="text-sm font-medium">
              Giá bìa sách (VNĐ)
            </Label>
            <Input
              id="test-price"
              type="number"
              min="0"
              step="10000"
              className="h-9 w-full"
              value={testBookPrice}
              onChange={(e) => setTestBookPrice(Number(e.target.value))}
            />
          </div>
        </div>

        {error && (
          <div className="flex items-center gap-2 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
            <CircleAlert className="h-4 w-4" />
            {error}
          </div>
        )}

        {/* Kết quả xem trước */}
        {previewResult ? (
          <div className="space-y-4 rounded-lg border border-primary/20 bg-primary/5 p-4">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border/50 pb-3">
              <div className="flex items-center gap-2">
                <Check className="h-5 w-5 text-green-600 dark:text-green-400" />
                <span className="font-semibold">Chính sách được áp dụng:</span>
                <span className="text-base font-bold text-primary">
                  {previewResult.policy.policyName}
                </span>
                <Badge variant="outline">v{previewResult.policy.version}</Badge>
                {previewResult.policy.isDefaultFallback && (
                  <Badge variant="secondary">Mặc định hệ thống</Badge>
                )}
              </div>
              <div className="text-xs text-muted-foreground">
                Điểm ưu tiên phân giải (Match Score): <span className="font-bold">{previewResult.policy.matchScore}</span>
              </div>
            </div>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              {/* Giới hạn mượn */}
              <div className="space-y-2 rounded-md bg-background p-3 shadow-xs">
                <h4 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                  Quy định mượn & giữ chỗ
                </h4>
                <div className="grid grid-cols-2 gap-y-1 text-sm">
                  <span className="text-muted-foreground">Số sách mượn tối đa:</span>
                  <span className="font-semibold">{previewResult.policy.maxLoanBooks} cuốn</span>

                  <span className="text-muted-foreground">Thời hạn mượn:</span>
                  <span className="font-semibold">{previewResult.policy.loanPeriodDays} ngày</span>

                  <span className="text-muted-foreground">Số lần gia hạn cho phép:</span>
                  <span className="font-semibold">{previewResult.policy.maxRenewals} lần ({previewResult.policy.renewalPeriodDays} ngày/lần)</span>

                  <span className="text-muted-foreground">Thời hạn giữ đặt trước:</span>
                  <span className="font-semibold">{previewResult.policy.holdDays} ngày</span>

                  <span className="text-muted-foreground">Tự động chặn khi quá hạn:</span>
                  <span className="font-semibold">
                    {previewResult.policy.blockIfOverdue ? 'Có chặn' : 'Không chặn'}
                  </span>
                </div>
              </div>

              {/* Tính toán tiền phạt */}
              <div className="space-y-2 rounded-md bg-background p-3 shadow-xs">
                <h4 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                  Chính sách tiền phạt
                </h4>
                <div className="grid grid-cols-2 gap-y-1 text-sm">
                  <span className="text-muted-foreground">Đơn giá phạt / ngày:</span>
                  <span className="font-semibold">{formatCurrency(previewResult.policy.finePerDay)}</span>

                  <span className="text-muted-foreground">Trần phạt tối đa:</span>
                  <span className="font-semibold">{formatCurrency(previewResult.policy.maxFineAmount)}</span>

                  <span className="text-muted-foreground">Tiền phạt {testOverdueDays} ngày trễ:</span>
                  <span className="font-bold text-destructive">
                    {formatCurrency(previewResult.calculatedOverdueFine)}
                  </span>

                  <span className="text-muted-foreground">Bồi thường nếu mất ({previewResult.policy.lostBookPenaltyRatio}%):</span>
                  <span className="font-bold text-amber-600 dark:text-amber-400">
                    {formatCurrency(previewResult.calculatedLostPenalty)}
                  </span>
                </div>
              </div>
            </div>
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed p-6 text-center text-muted-foreground">
            <BookOpen className="mb-2 h-8 w-8 opacity-40" />
            <p className="text-sm">Bấm nút "Chạy xem trước" ở trên để kiểm tra kết quả phân giải chính sách với bộ dữ liệu mẫu.</p>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
