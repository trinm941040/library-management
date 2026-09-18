import { useCallback, useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import {
  AlertCircle,
  AlertTriangle,
  Barcode,
  BookOpen,
  Calendar,
  CheckCircle2,
  Clock,
  Coins,
  CreditCard,
  History,
  RotateCcw,
  Search,
  ShieldAlert,
  User,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/auth/AuthProvider'
import { PageShell, useToast } from '@/common/components'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'
import { can } from '@/shared/auth/permissions'
import {
  confirmReturn,
  lookupBookCopyForReturn,
  type BookCopyReturnLookup,
  type ReturnExecutionResult,
} from './borrowing-api'

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount)
}

const formatDate = (value: string | null | undefined) => {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date)
}

export function ReturnPage() {
  const { user } = useAuth()
  const canReturn = can(user?.permissions ?? [], 'borrowings.return')
  const { showToast } = useToast()

  // Input states
  const [barcodeInput, setBarcodeInput] = useState('')
  const [condition, setCondition] = useState<'Good' | 'Worn' | 'Damaged' | 'Lost'>('Good')
  const [note, setNote] = useState('')
  const [customDamageFine, setCustomDamageFine] = useState<string>('')
  const [customLostFine, setCustomLostFine] = useState<string>('')

  // Lookup & Return state
  const [copyData, setCopyData] = useState<BookCopyReturnLookup | null>(null)
  const [isSearching, setIsSearching] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [searchError, setSearchError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  // Success result & Session history for continuous scanning
  const [lastResult, setLastResult] = useState<ReturnExecutionResult | null>(null)
  const [sessionHistory, setSessionHistory] = useState<ReturnExecutionResult[]>([])

  // DOM ref for autofocus
  const barcodeInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    barcodeInputRef.current?.focus()
  }, [])

  const handleLookup = useCallback(async () => {
    const trimmed = barcodeInput.trim()
    if (!trimmed) {
      setSearchError('Vui lòng nhập hoặc quét mã vạch bản sao sách.')
      return
    }

    setIsSearching(true)
    setSearchError(null)
    setSubmitError(null)
    setLastResult(null)

    try {
      const data = await lookupBookCopyForReturn(trimmed)
      setCopyData(data)
      setCondition('Good')
      setNote('')
      setCustomDamageFine(data.fixedDamageFine > 0 ? String(data.fixedDamageFine) : '50000')
      setCustomLostFine(data.estimatedLostFine > 0 ? String(data.estimatedLostFine) : '150000')
    } catch (err: unknown) {
      setCopyData(null)
      const message = err instanceof Error ? err.message : 'Không tìm thấy bản sao sách hoặc bản sao không đang được mượn.'
      setSearchError(message)
    } finally {
      setIsSearching(false)
    }
  }, [barcodeInput])

  const handleBarcodeKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      void handleLookup()
    }
  }

  // Calculate current fine based on condition and overdue
  const currentOverdueFine = copyData?.isOverdue ? copyData.estimatedOverdueFine : 0
  const currentDamageFine = condition === 'Damaged' ? Number(customDamageFine || 0) : 0
  const currentLostFine = condition === 'Lost' ? Number(customLostFine || 0) : 0
  const totalEstimatedFine = currentOverdueFine + currentDamageFine + currentLostFine

  const handleConfirmReturn = async (e: FormEvent) => {
    e.preventDefault()
    if (!copyData) return

    if ((condition === 'Damaged' || condition === 'Lost') && !note.trim()) {
      setSubmitError(`Bắt buộc phải nhập ghi chú khi xác nhận sách ${condition === 'Damaged' ? 'bị hư hỏng' : 'bị mất'}.`)
      return
    }

    setIsSubmitting(true)
    setSubmitError(null)

    try {
      const result = await confirmReturn({
        barcode: copyData.barcode,
        condition,
        note: note.trim() ? note.trim() : null,
        customDamageFine: condition === 'Damaged' ? Number(customDamageFine || 0) : null,
        customLostFine: condition === 'Lost' ? Number(customLostFine || 0) : null,
        concurrencyToken: copyData.concurrencyToken,
      })

      showToast(`Đã nhận trả sách "${copyData.title}" thành công!`, 'success')
      setLastResult(result)
      setSessionHistory((prev) => [result, ...prev])
      setCopyData(null)
      setBarcodeInput('')
      setNote('')
      setCondition('Good')

      // Refocus scanner for continuous workflow
      setTimeout(() => {
        barcodeInputRef.current?.focus()
      }, 50)
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Lỗi khi ghi nhận trả sách.'
      setSubmitError(message)
      showToast(message, 'error')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleReset = () => {
    setBarcodeInput('')
    setCopyData(null)
    setSearchError(null)
    setSubmitError(null)
    setLastResult(null)
    setCondition('Good')
    setNote('')
    barcodeInputRef.current?.focus()
  }

  if (!canReturn) {
    return (
      <PageShell title="Trả sách & Xử lý vi phạm" description="Hoàn trả tài liệu theo mã vạch">
        <div className="flex flex-col items-center justify-center p-12 text-center">
          <ShieldAlert className="h-16 w-16 text-rose-500 mb-4" />
          <h2 className="text-xl font-bold text-slate-800 dark:text-slate-100 mb-2">Quyền truy cập bị từ chối</h2>
          <p className="text-slate-600 dark:text-slate-400 max-w-md mb-6">
            Bạn không có quyền thực hiện chức năng trả sách (<code className="bg-slate-100 dark:bg-slate-800 px-1 py-0.5 rounded">borrowings.return</code>).
          </p>
          <Button asChild variant="outline">
            <Link to="/circulation">Quay lại danh sách mượn trả</Link>
          </Button>
        </div>
      </PageShell>
    )
  }

  return (
    <PageShell
      title="Trả sách & Xử lý vi phạm"
      description="Quét mã vạch bản sao để đóng phiếu mượn, cập nhật trạng thái sách và tính phạt tự động"
    >
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 max-w-7xl mx-auto">
        {/* Left Column: Scanner & Return Execution */}
        <div className="lg:col-span-8 space-y-6">
          {/* Barcode Scanner Card */}
          <Card className="border-indigo-200 dark:border-indigo-900 shadow-sm">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="p-2 rounded-lg bg-indigo-50 dark:bg-indigo-950/50 text-indigo-600 dark:text-indigo-400">
                    <Barcode className="h-5 w-5" />
                  </div>
                  <div>
                    <CardTitle className="text-lg">Quét mã vạch bản sao</CardTitle>
                    <CardDescription>Hỗ trợ đầu đọc mã vạch tự động (bấm Enter sau khi quét)</CardDescription>
                  </div>
                </div>
                {copyData && (
                  <Button variant="ghost" size="sm" onClick={handleReset} className="text-slate-500 hover:text-slate-700">
                    <RotateCcw className="h-4 w-4 mr-1" /> Quét lại
                  </Button>
                )}
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex gap-2">
                <div className="relative flex-1">
                  <Barcode className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                  <Input
                    ref={barcodeInputRef}
                    id="return-barcode-input"
                    placeholder="Quét hoặc nhập mã vạch sách (ví dụ: BC-BOOK-001)..."
                    value={barcodeInput}
                    onChange={(e) => setBarcodeInput(e.target.value)}
                    onKeyDown={handleBarcodeKeyDown}
                    disabled={isSearching || isSubmitting}
                    className="pl-9 font-mono"
                    autoComplete="off"
                  />
                </div>
                <Button onClick={handleLookup} disabled={isSearching || !barcodeInput.trim()} className="min-w-28">
                  {isSearching ? 'Đang tìm...' : (
                    <>
                      <Search className="h-4 w-4 mr-1.5" /> Tra cứu
                    </>
                  )}
                </Button>
              </div>

              {searchError && (
                <div className="mt-3 flex items-center gap-2 text-sm text-rose-600 dark:text-rose-400 bg-rose-50 dark:bg-rose-950/40 p-3 rounded-md border border-rose-200 dark:border-rose-900">
                  <AlertCircle className="h-4 w-4 flex-shrink-0" />
                  <span>{searchError}</span>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Return Execution & Fine Preview Card */}
          {copyData && (
            <Card className="border-slate-200 dark:border-slate-800 shadow-md animate-in fade-in-50 duration-200">
              <CardHeader className="pb-4 border-b border-slate-100 dark:border-slate-800">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <CardTitle className="text-xl text-slate-900 dark:text-slate-100 flex items-center gap-2">
                      <BookOpen className="h-5 w-5 text-indigo-600 dark:text-indigo-400" />
                      {copyData.title}
                    </CardTitle>
                    <CardDescription className="mt-0.5">Tác giả: {copyData.author}</CardDescription>
                  </div>
                  <div>
                    {copyData.isOverdue ? (
                      <Badge variant="destructive" className="flex items-center gap-1 text-sm py-1 px-3">
                        <Clock className="h-3.5 w-3.5" /> Quá hạn {copyData.overdueDays} ngày
                      </Badge>
                    ) : (
                      <Badge className="bg-emerald-600 hover:bg-emerald-700 text-white flex items-center gap-1 text-sm py-1 px-3">
                        <CheckCircle2 className="h-3.5 w-3.5" /> Đúng hạn
                      </Badge>
                    )}
                  </div>
                </div>
              </CardHeader>
              <CardContent className="pt-5 space-y-6">
                {/* Details Grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 p-4 rounded-lg bg-slate-50 dark:bg-slate-900/50 border border-slate-200 dark:border-slate-800 text-sm">
                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 font-medium">
                      <User className="h-4 w-4" /> Độc giả đang mượn:
                    </div>
                    <div className="pl-6 font-semibold text-slate-900 dark:text-slate-100">
                      {copyData.borrowerName}
                    </div>
                    <div className="pl-6 text-xs text-slate-500 font-mono">
                      Mã: {copyData.memberCode ?? '—'} | Thẻ: {copyData.cardNumber ?? '—'}
                    </div>
                  </div>

                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 font-medium">
                      <Calendar className="h-4 w-4" /> Thông tin mượn:
                    </div>
                    <div className="pl-6 text-slate-700 dark:text-slate-300">
                      Ngày mượn: <span className="font-medium">{formatDate(copyData.borrowedAtUtc)}</span>
                    </div>
                    <div className="pl-6 text-slate-700 dark:text-slate-300">
                      Hạn trả: <span className={`font-semibold ${copyData.isOverdue ? 'text-rose-600 dark:text-rose-400' : ''}`}>{formatDate(copyData.dueAtUtc)}</span>
                    </div>
                  </div>
                </div>

                {/* Return Condition & Note Form */}
                <form onSubmit={handleConfirmReturn} className="space-y-5">
                  <div className="space-y-2">
                    <Label className="text-sm font-semibold text-slate-800 dark:text-slate-200">
                      Tình trạng sách khi trả:
                    </Label>
                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
                      <button
                        type="button"
                        onClick={() => setCondition('Good')}
                        className={`p-3 rounded-lg border text-sm font-medium transition-all text-center ${
                          condition === 'Good'
                            ? 'bg-emerald-50 border-emerald-500 text-emerald-700 dark:bg-emerald-950/40 dark:border-emerald-500 dark:text-emerald-300 ring-2 ring-emerald-500/20'
                            : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:border-slate-300'
                        }`}
                      >
                        Tốt (Bình thường)
                      </button>
                      <button
                        type="button"
                        onClick={() => setCondition('Worn')}
                        className={`p-3 rounded-lg border text-sm font-medium transition-all text-center ${
                          condition === 'Worn'
                            ? 'bg-amber-50 border-amber-500 text-amber-700 dark:bg-amber-950/40 dark:border-amber-500 dark:text-amber-300 ring-2 ring-amber-500/20'
                            : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:border-slate-300'
                        }`}
                      >
                        Hao mòn tự nhiên
                      </button>
                      <button
                        type="button"
                        onClick={() => setCondition('Damaged')}
                        className={`p-3 rounded-lg border text-sm font-medium transition-all text-center ${
                          condition === 'Damaged'
                            ? 'bg-orange-50 border-orange-500 text-orange-700 dark:bg-orange-950/40 dark:border-orange-500 dark:text-orange-300 ring-2 ring-orange-500/20'
                            : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:border-slate-300'
                        }`}
                      >
                        Hư hỏng
                      </button>
                      <button
                        type="button"
                        onClick={() => setCondition('Lost')}
                        className={`p-3 rounded-lg border text-sm font-medium transition-all text-center ${
                          condition === 'Lost'
                            ? 'bg-rose-50 border-rose-500 text-rose-700 dark:bg-rose-950/40 dark:border-rose-500 dark:text-rose-300 ring-2 ring-rose-500/20'
                            : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:border-slate-300'
                        }`}
                      >
                        Báo mất sách
                      </button>
                    </div>
                  </div>

                  {/* Dynamic Fine Inputs */}
                  {condition === 'Damaged' && (
                    <div className="p-4 rounded-lg bg-orange-50/50 dark:bg-orange-950/30 border border-orange-200 dark:border-orange-900/60 space-y-2">
                      <Label htmlFor="damage-fine-input" className="text-xs font-semibold text-orange-800 dark:text-orange-300">
                        Tiền phạt bồi thường hư hỏng (VND):
                      </Label>
                      <Input
                        id="damage-fine-input"
                        type="number"
                        min="0"
                        step="1000"
                        value={customDamageFine}
                        onChange={(e) => setCustomDamageFine(e.target.value)}
                        placeholder="Mặc định theo chính sách..."
                        className="bg-white dark:bg-slate-900"
                      />
                      <p className="text-xs text-orange-600 dark:text-orange-400">
                        * Bản sao sẽ chuyển trạng thái "Hư hỏng" và không đưa về giá khả dụng.
                      </p>
                    </div>
                  )}

                  {condition === 'Lost' && (
                    <div className="p-4 rounded-lg bg-rose-50/50 dark:bg-rose-950/30 border border-rose-200 dark:border-rose-900/60 space-y-2">
                      <Label htmlFor="lost-fine-input" className="text-xs font-semibold text-rose-800 dark:text-rose-300">
                        Tiền phạt bồi thường mất sách (VND):
                      </Label>
                      <Input
                        id="lost-fine-input"
                        type="number"
                        min="0"
                        step="1000"
                        value={customLostFine}
                        onChange={(e) => setCustomLostFine(e.target.value)}
                        placeholder="Mặc định theo chính sách..."
                        className="bg-white dark:bg-slate-900"
                      />
                      <p className="text-xs text-rose-600 dark:text-rose-400">
                        * Bản sao sẽ chuyển trạng thái "Mất sách" và không đưa về giá khả dụng.
                      </p>
                    </div>
                  )}

                  {/* Note Input */}
                  <div className="space-y-1.5">
                    <Label htmlFor="return-note-input" className="text-sm font-medium">
                      Ghi chú {(condition === 'Damaged' || condition === 'Lost') && <span className="text-rose-500 font-bold">*</span>}:
                    </Label>
                    <Input
                      id="return-note-input"
                      value={note}
                      onChange={(e) => setNote(e.target.value)}
                      placeholder={
                        condition === 'Damaged'
                          ? 'Mô tả hư hỏng (bắt buộc, ví dụ: rách bìa, mất trang 15-20)...'
                          : condition === 'Lost'
                            ? 'Mô tả lý do mất (bắt buộc)...'
                            : 'Ghi chú thêm nếu có...'
                      }
                      className="bg-white dark:bg-slate-900"
                    />
                  </div>

                  {/* Fine Summary Breakdown */}
                  <div className="p-4 rounded-lg bg-indigo-50/50 dark:bg-indigo-950/30 border border-indigo-100 dark:border-indigo-900/40 space-y-2">
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-slate-600 dark:text-slate-400">Tiền phạt quá hạn ({copyData.overdueDays} ngày):</span>
                      <span className="font-semibold text-slate-900 dark:text-slate-100">{formatCurrency(currentOverdueFine)}</span>
                    </div>
                    {condition === 'Damaged' && (
                      <div className="flex items-center justify-between text-sm">
                        <span className="text-slate-600 dark:text-slate-400">Tiền phạt hư hỏng:</span>
                        <span className="font-semibold text-orange-600 dark:text-orange-400">{formatCurrency(currentDamageFine)}</span>
                      </div>
                    )}
                    {condition === 'Lost' && (
                      <div className="flex items-center justify-between text-sm">
                        <span className="text-slate-600 dark:text-slate-400">Tiền phạt mất sách:</span>
                        <span className="font-semibold text-rose-600 dark:text-rose-400">{formatCurrency(currentLostFine)}</span>
                      </div>
                    )}
                    <div className="pt-2 border-t border-indigo-200/60 dark:border-indigo-900/60 flex items-center justify-between">
                      <span className="font-bold text-slate-800 dark:text-slate-200 flex items-center gap-1.5">
                        <Coins className="h-4 w-4 text-indigo-600" /> Tổng tiền phạt tạo vi phạm:
                      </span>
                      <span className="text-lg font-bold text-indigo-700 dark:text-indigo-300">
                        {formatCurrency(totalEstimatedFine)}
                      </span>
                    </div>
                  </div>

                  {submitError && (
                    <div className="flex items-center gap-2 text-sm text-rose-600 dark:text-rose-400 bg-rose-50 dark:bg-rose-950/40 p-3 rounded-md border border-rose-200 dark:border-rose-900">
                      <AlertCircle className="h-4 w-4 flex-shrink-0" />
                      <span>{submitError}</span>
                    </div>
                  )}

                  {/* Submit Action */}
                  <div className="flex items-center justify-end gap-3 pt-2">
                    <Button type="button" variant="outline" onClick={handleReset} disabled={isSubmitting}>
                      Hủy bỏ
                    </Button>
                    <Button
                      type="submit"
                      disabled={isSubmitting}
                      className="bg-indigo-600 hover:bg-indigo-700 text-white min-w-36"
                    >
                      {isSubmitting ? 'Đang xử lý...' : 'Xác nhận trả sách'}
                    </Button>
                  </div>
                </form>
              </CardContent>
            </Card>
          )}

          {/* Last Result Highlight Card */}
          {lastResult && (
            <Card className="border-emerald-200 dark:border-emerald-900 bg-emerald-50/40 dark:bg-emerald-950/20 shadow-sm animate-in fade-in-50">
              <CardContent className="pt-5 pb-4">
                <div className="flex items-start gap-3">
                  <div className="p-2 bg-emerald-100 dark:bg-emerald-900/60 rounded-full text-emerald-600 dark:text-emerald-400 mt-0.5">
                    <CheckCircle2 className="h-6 w-6" />
                  </div>
                  <div className="flex-1 space-y-2">
                    <div className="flex items-center justify-between flex-wrap gap-2">
                      <h4 className="font-bold text-emerald-900 dark:text-emerald-200 text-base">
                        Ghi nhận trả sách thành công!
                      </h4>
                      <span className="text-xs text-emerald-700 dark:text-emerald-400 font-mono">
                        {formatDate(lastResult.returnedAtUtc)}
                      </span>
                    </div>
                    <p className="text-sm text-emerald-800 dark:text-emerald-300">
                      Đã hoàn tất khoản mượn cho tác phẩm <strong>{lastResult.borrowing.bookTitle}</strong> (Độc giả: {lastResult.borrowing.borrowerName}).
                    </p>

                    {lastResult.hasWaitingReservation && (
                      <div className="flex items-center gap-1.5 text-xs font-medium text-amber-800 dark:text-amber-300 bg-amber-100/70 dark:bg-amber-950/50 p-2 rounded">
                        <AlertTriangle className="h-4 w-4 flex-shrink-0 text-amber-600" />
                        Sách này có độc giả đang đặt trước! Bản sao đã được chuyển sang trạng thái "Đã đặt trước" (Reserved).
                      </div>
                    )}

                    {lastResult.violations.length > 0 && (
                      <div className="mt-2 space-y-1">
                        <p className="text-xs font-semibold text-rose-800 dark:text-rose-300">
                          Các khoản vi phạm đã tạo tự động:
                        </p>
                        <div className="space-y-1">
                          {lastResult.violations.map((v) => (
                            <div key={v.id} className="text-xs flex items-center justify-between bg-white dark:bg-slate-900 p-2 rounded border border-emerald-200 dark:border-emerald-800">
                              <span>• {v.note}</span>
                              <span className="font-bold text-rose-600 dark:text-rose-400">{formatCurrency(v.fineAmount)}</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    <div className="pt-2 flex justify-end">
                      <Button size="sm" onClick={handleReset} className="bg-emerald-700 hover:bg-emerald-800 text-white">
                        <Barcode className="h-4 w-4 mr-1.5" /> Quét tiếp sách khác
                      </Button>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Right Column: Return Session History */}
        <div className="lg:col-span-4 space-y-6">
          <Card className="border-slate-200 dark:border-slate-800">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base flex items-center gap-2 text-slate-800 dark:text-slate-200">
                  <History className="h-4 w-4 text-indigo-600" /> Lịch sử ca trực này
                </CardTitle>
                <Badge variant="secondary">{sessionHistory.length} cuốn</Badge>
              </div>
              <CardDescription>Danh sách sách đã quét trả trong phiên hiện tại</CardDescription>
            </CardHeader>
            <CardContent>
              {sessionHistory.length === 0 ? (
                <div className="text-center py-8 text-slate-400 text-sm">
                  Chưa có giao dịch trả nào trong phiên làm việc này.
                </div>
              ) : (
                <div className="space-y-3 max-h-[500px] overflow-y-auto pr-1">
                  {sessionHistory.map((item, index) => (
                    <div
                      key={item.borrowing.id + '-' + index}
                      className="p-3 rounded-lg border border-slate-100 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/40 text-xs space-y-1.5"
                    >
                      <div className="font-semibold text-slate-900 dark:text-slate-100 line-clamp-1">
                        {item.borrowing.bookTitle}
                      </div>
                      <div className="flex items-center justify-between text-slate-500">
                        <span>Độc giả: {item.borrowing.borrowerName}</span>
                        <Badge variant="outline" className="text-[10px] py-0">
                          {item.condition}
                        </Badge>
                      </div>
                      {item.totalFine > 0 && (
                        <div className="text-rose-600 dark:text-rose-400 font-medium">
                          Phạt: {formatCurrency(item.totalFine)}
                        </div>
                      )}
                      <div className="text-[10px] text-slate-400 text-right">
                        {new Date(item.returnedAtUtc).toLocaleTimeString('vi-VN')}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Useful Navigation Links */}
          <Card className="border-slate-200 dark:border-slate-800">
            <CardContent className="pt-4 space-y-2 text-sm">
              <Link
                to="/circulation/checkout"
                className="flex items-center justify-between p-2.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors text-slate-700 dark:text-slate-300 font-medium"
              >
                <span className="flex items-center gap-2">
                  <Barcode className="h-4 w-4 text-indigo-600" /> Chuyển sang Lập phiếu mượn
                </span>
                <span>→</span>
              </Link>
              <Link
                to="/circulation"
                className="flex items-center justify-between p-2.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors text-slate-700 dark:text-slate-300 font-medium"
              >
                <span className="flex items-center gap-2">
                  <CreditCard className="h-4 w-4 text-indigo-600" /> Quản lý danh sách mượn trả
                </span>
                <span>→</span>
              </Link>
            </CardContent>
          </Card>
        </div>
      </div>
    </PageShell>
  )
}
