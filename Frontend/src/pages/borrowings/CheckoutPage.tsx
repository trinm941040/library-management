import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import {
  AlertCircle,
  Barcode,
  BookOpen,
  Calendar,
  CheckCircle2,
  Clock,
  CreditCard,
  RotateCcw,
  ShieldAlert,
  ShieldCheck,
  User,
  UserCheck,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { PageShell, useToast } from '@/common/components'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import {
  checkoutWithBarcode,
  lookupBookCopyForCheckout,
  lookupMemberForCheckout,
  type BookCopyCheckoutLookup,
  type LibraryBorrowing,
  type MemberCheckoutLookup,
} from './borrowing-api'

const formatDate = (value: string | DateOnlyLike | null | undefined) => {
  if (!value) return '—'
  const date = new Date(String(value))
  if (Number.isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium' }).format(date)
}

type DateOnlyLike = string

export function CheckoutPage() {
  const { showToast } = useToast()

  // Input states
  const [cardOrCodeInput, setCardOrCodeInput] = useState('')
  const [barcodeInput, setBarcodeInput] = useState('')
  const [loanDaysOverride] = useState<string>('')

  // Lookup results
  const [member, setMember] = useState<MemberCheckoutLookup | null>(null)
  const [copy, setCopy] = useState<BookCopyCheckoutLookup | null>(null)

  // Loading & Error states
  const [isSearchingMember, setIsSearchingMember] = useState(false)
  const [memberError, setMemberError] = useState('')
  const [isSearchingCopy, setIsSearchingCopy] = useState(false)
  const [copyError, setCopyError] = useState('')

  // Checkout execution states
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [checkoutError, setCheckoutError] = useState('')
  const [createdBorrowing, setCreatedBorrowing] = useState<LibraryBorrowing | null>(null)

  // Refs for scanner autofocus
  const cardInputRef = useRef<HTMLInputElement>(null)
  const copyInputRef = useRef<HTMLInputElement>(null)

  // Initial focus on member card input
  useEffect(() => {
    cardInputRef.current?.focus()
  }, [])

  // Handle member lookup
  const handleMemberLookup = useCallback(async (cardOrCode: string) => {
    const trimmed = cardOrCode.trim()
    if (!trimmed) return

    setIsSearchingMember(true)
    setMemberError('')
    setCheckoutError('')
    setCreatedBorrowing(null)

    try {
      const result = await lookupMemberForCheckout(trimmed)
      setMember(result)
      if (result.isEligible) {
        showToast(`Đã nhận diện độc giả: ${result.fullName}`, 'success')
        // Automatically move focus to book barcode input
        setTimeout(() => copyInputRef.current?.focus(), 150)
      } else {
        showToast('Độc giả chưa đủ điều kiện mượn sách.', 'error')
      }
    } catch (caught) {
      setMember(null)
      const message = caught instanceof Error ? caught.message : 'Không thể tra cứu độc giả.'
      setMemberError(message)
      showToast(message, 'error')
    } finally {
      setIsSearchingMember(false)
    }
  }, [showToast])

  // Handle book copy lookup
  const handleCopyLookup = useCallback(async (barcode: string) => {
    const trimmed = barcode.trim()
    if (!trimmed) return

    setIsSearchingCopy(true)
    setCopyError('')
    setCheckoutError('')

    try {
      const result = await lookupBookCopyForCheckout(trimmed, member?.memberId)
      setCopy(result)
      if (!result.isAvailable) {
        showToast(result.ineligibilityReason ?? 'Bản sao sách không khả dụng.', 'error')
      } else {
        showToast(`Đã nhận diện sách: ${result.title}`, 'success')
      }
    } catch (caught) {
      setCopy(null)
      const message = caught instanceof Error ? caught.message : 'Không thể tra cứu bản sao sách.'
      setCopyError(message)
      showToast(message, 'error')
    } finally {
      setIsSearchingCopy(false)
    }
  }, [member, showToast])

  // Form submit: Member Card
  const onMemberFormSubmit = (event: FormEvent) => {
    event.preventDefault()
    handleMemberLookup(cardOrCodeInput)
  }

  // Form submit: Book Barcode
  const onCopyFormSubmit = (event: FormEvent) => {
    event.preventDefault()
    handleCopyLookup(barcodeInput)
  }

  // Execute checkout
  const handleCheckout = async () => {
    if (!member || !copy) return
    if (!member.isEligible) {
      setCheckoutError('Độc giả không đủ điều kiện mượn sách.')
      return
    }
    if (!copy.isAvailable) {
      setCheckoutError(copy.ineligibilityReason ?? 'Bản sao sách không khả dụng.')
      return
    }

    setIsSubmitting(true)
    setCheckoutError('')

    try {
      const overrideDays = loanDaysOverride ? Number.parseInt(loanDaysOverride, 10) : undefined
      const created = await checkoutWithBarcode({
        memberCardOrCode: member.cardNumber || member.memberCode,
        bookBarcode: copy.barcode,
        loanDaysOverride: Number.isFinite(overrideDays) && overrideDays! > 0 ? overrideDays : undefined,
      })

      setCreatedBorrowing(created)
      showToast('Đã lập phiếu mượn thành công!', 'success')
      // Update local member count
      setMember((prev) =>
        prev
          ? {
              ...prev,
              activeBorrowingsCount: prev.activeBorrowingsCount + 1,
              isEligible: prev.activeBorrowingsCount + 1 < prev.borrowingLimit,
            }
          : null,
      )
      // Reset copy
      setCopy(null)
      setBarcodeInput('')
    } catch (caught) {
      const message = caught instanceof Error ? caught.message : 'Lập phiếu mượn thất bại.'
      setCheckoutError(message)
      showToast(message, 'error')
    } finally {
      setIsSubmitting(false)
    }
  }

  // Action: Quét tiếp sách khác cho độc giả hiện tại
  const handleScanAnotherCopy = () => {
    setCreatedBorrowing(null)
    setCopy(null)
    setBarcodeInput('')
    setCheckoutError('')
    setTimeout(() => copyInputRef.current?.focus(), 150)
  }

  // Action: Reset toàn bộ để quét cho độc giả mới
  const handleResetAll = () => {
    setCreatedBorrowing(null)
    setMember(null)
    setCopy(null)
    setCardOrCodeInput('')
    setBarcodeInput('')
    setMemberError('')
    setCopyError('')
    setCheckoutError('')
    setTimeout(() => cardInputRef.current?.focus(), 150)
  }

  const isCheckoutReady =
    member?.isEligible === true && copy?.isAvailable === true && !isSubmitting

  return (
    <PageShell
      eyebrow="Quản lý tác vụ"
      title="Lập phiếu mượn bằng mã thẻ và mã vạch"
      description="Quét mã thẻ độc giả và mã vạch sách để kiểm tra điều kiện mượn, tính hạn trả theo chính sách và lập phiếu tức thì."
      actions={
        <div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row">
          <Button variant="outline" onClick={handleResetAll}>
            <RotateCcw className="mr-2 h-4 w-4" /> Làm mới
          </Button>
          <Button variant="ghost" asChild>
            <Link to="/borrowings">Về danh sách phiếu mượn</Link>
          </Button>
        </div>
      }
    >
      {/* Thông báo thành công nếu vừa tạo phiếu mượn */}
      {createdBorrowing && (
        <Card className="mb-6 border-emerald-500 bg-emerald-50/50 dark:bg-emerald-950/20">
          <CardHeader className="pb-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="flex items-center gap-2">
                <CheckCircle2 className="h-6 w-6 text-emerald-600 dark:text-emerald-400" />
                <CardTitle className="text-emerald-900 dark:text-emerald-100">
                  Lập phiếu mượn thành công!
                </CardTitle>
              </div>
              <Badge variant="outline" className="border-emerald-600 text-emerald-700">
                Mã phiếu: {createdBorrowing.id.slice(0, 8)}
              </Badge>
            </div>
            <CardDescription className="text-emerald-800 dark:text-emerald-200">
              Giao dịch đã được ghi nhận nguyên tử kèm cập nhật trạng thái bản sao sách và nhật ký kiểm toán.
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <div className="rounded-lg border bg-background p-3">
              <span className="text-xs text-muted-foreground">Độc giả</span>
              <p className="font-semibold">{createdBorrowing.borrowerName}</p>
              <p className="text-xs text-muted-foreground">{createdBorrowing.borrowerEmail}</p>
            </div>
            <div className="rounded-lg border bg-background p-3">
              <span className="text-xs text-muted-foreground">Đầu sách</span>
              <p className="font-semibold line-clamp-1">{createdBorrowing.bookTitle}</p>
              {createdBorrowing.bookCopyBarcode && (
                <p className="text-xs font-mono text-muted-foreground">
                  Mã vạch: {createdBorrowing.bookCopyBarcode}
                </p>
              )}
            </div>
            <div className="rounded-lg border bg-background p-3">
              <span className="text-xs text-muted-foreground">Ngày mượn</span>
              <p className="font-semibold">{formatDate(createdBorrowing.borrowedAtUtc)}</p>
            </div>
            <div className="rounded-lg border bg-background p-3">
              <span className="text-xs text-muted-foreground">Hạn trả</span>
              <p className="font-semibold text-primary">{formatDate(createdBorrowing.dueAtUtc)}</p>
            </div>

            <div className="flex flex-wrap gap-2 sm:col-span-2 lg:col-span-4 pt-2">
              <Button onClick={handleScanAnotherCopy}>
                <Barcode className="mr-2 h-4 w-4" /> Quét tiếp sách cho độc giả này
              </Button>
              <Button variant="outline" onClick={handleResetAll}>
                <UserCheck className="mr-2 h-4 w-4" /> Lập phiếu cho độc giả mới
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Thông báo lỗi chung nếu có conflict/lỗi checkout */}
      {checkoutError && (
        <div className="mb-6 flex items-start gap-3 rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-destructive">
          <AlertCircle className="mt-0.5 h-5 w-5 shrink-0" />
          <div className="flex-1">
            <p className="font-semibold">Không thể hoàn tất lập phiếu mượn</p>
            <p className="text-sm">{checkoutError}</p>
          </div>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        {/* ================= BƯỚC 1: QUÉT THẺ ĐỘC GIẢ ================= */}
        <div className="space-y-4">
          <Card className={`border-2 transition-colors ${member ? 'border-primary/50' : 'border-border'}`}>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary text-xs font-bold text-primary-foreground">
                    1
                  </span>
                  <CardTitle className="text-lg">Quét mã thẻ độc giả</CardTitle>
                </div>
                {member && (
                  <Badge variant={member.isEligible ? 'default' : 'destructive'} className="gap-1">
                    {member.isEligible ? (
                      <>
                        <ShieldCheck className="h-3 w-3" /> Đủ điều kiện
                      </>
                    ) : (
                      <>
                        <ShieldAlert className="h-3 w-3" /> Bị chặn mượn
                      </>
                    )}
                  </Badge>
                )}
              </div>
              <CardDescription>
                Nhập hoặc quét mã vạch trên thẻ thư viện / mã số sinh viên / mã độc giả.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <form onSubmit={onMemberFormSubmit} className="flex flex-col gap-2 sm:flex-row">
                <div className="relative flex-1">
                  <CreditCard className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                  <Input
                    ref={cardInputRef}
                    placeholder="Quét mã vạch thẻ hoặc nhập mã độc giả..."
                    value={cardOrCodeInput}
                    onChange={(e) => setCardOrCodeInput(e.target.value)}
                    className="pl-9 font-mono"
                    disabled={isSearchingMember}
                  />
                </div>
                <Button type="submit" disabled={isSearchingMember || !cardOrCodeInput.trim()}>
                  {isSearchingMember ? 'Đang tìm...' : 'Tra cứu'}
                </Button>
              </form>

              {memberError && (
                <div className="flex items-center gap-2 text-sm text-destructive">
                  <AlertCircle className="h-4 w-4 shrink-0" />
                  <span>{memberError}</span>
                </div>
              )}

              {/* Thông tin độc giả chi tiết */}
              {member && (
                <div className="rounded-lg border bg-muted/30 p-4 space-y-3">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div>
                      <h4 className="font-semibold text-base flex items-center gap-2">
                        <User className="h-4 w-4 text-primary" /> {member.fullName}
                      </h4>
                      <p className="text-xs text-muted-foreground">{member.email}</p>
                    </div>
                    <div className="text-right">
                      <span className="text-xs font-mono font-medium block">
                        Mã ĐG: {member.memberCode}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        Nhóm: {member.memberGroup || 'Chung'}
                      </span>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-2 text-xs border-t pt-3">
                    <div>
                      <span className="text-muted-foreground">Mã thẻ: </span>
                      <span className="font-mono font-medium">{member.cardNumber || 'Chưa cấp'}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Hạn thẻ: </span>
                      <span className="font-medium">{formatDate(member.cardExpiresOn)}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Đang mượn: </span>
                      <span className="font-bold text-foreground">
                        {member.activeBorrowingsCount} / {member.borrowingLimit} cuốn
                      </span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Sách quá hạn: </span>
                      <span
                        className={`font-bold ${
                          member.overdueLoansCount > 0 ? 'text-destructive' : 'text-emerald-600'
                        }`}
                      >
                        {member.overdueLoansCount} cuốn
                      </span>
                    </div>
                  </div>

                  {/* Lý do nếu không đủ điều kiện */}
                  {!member.isEligible && member.ineligibilityReasons.length > 0 && (
                    <div className="rounded border border-destructive/30 bg-destructive/10 p-3 text-xs text-destructive space-y-1">
                      <p className="font-semibold flex items-center gap-1">
                        <AlertCircle className="h-3.5 w-3.5" /> Không được phép mượn sách vì:
                      </p>
                      <ul className="list-disc pl-4 space-y-0.5">
                        {member.ineligibilityReasons.map((reason, index) => (
                          <li key={index}>{reason}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* ================= BƯỚC 2: QUÉT MÃ VẠCH SÁCH ================= */}
        <div className="space-y-4">
          <Card
            className={`border-2 transition-colors ${
              copy ? 'border-primary/50' : 'border-border'
            } ${!member?.isEligible ? 'opacity-70' : ''}`}
          >
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary text-xs font-bold text-primary-foreground">
                    2
                  </span>
                  <CardTitle className="text-lg">Quét mã vạch bản sao sách</CardTitle>
                </div>
                {copy && (
                  <Badge variant={copy.isAvailable ? 'default' : 'destructive'} className="gap-1">
                    {copy.isAvailable ? (
                      <>
                        <CheckCircle2 className="h-3 w-3" /> Sách khả dụng
                      </>
                    ) : (
                      <>
                        <ShieldAlert className="h-3 w-3" /> Không khả dụng
                      </>
                    )}
                  </Badge>
                )}
              </div>
              <CardDescription>
                Quét mã vạch dán trên gáy hoặc bìa cuốn sách độc giả muốn mượn.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <form onSubmit={onCopyFormSubmit} className="flex flex-col gap-2 sm:flex-row">
                <div className="relative flex-1">
                  <Barcode className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                  <Input
                    ref={copyInputRef}
                    placeholder="Quét hoặc nhập mã vạch cuốn sách..."
                    value={barcodeInput}
                    onChange={(e) => setBarcodeInput(e.target.value)}
                    className="pl-9 font-mono"
                    disabled={isSearchingCopy}
                  />
                </div>
                <Button type="submit" disabled={isSearchingCopy || !barcodeInput.trim()}>
                  {isSearchingCopy ? 'Đang tìm...' : 'Tra cứu'}
                </Button>
              </form>

              {copyError && (
                <div className="flex items-center gap-2 text-sm text-destructive">
                  <AlertCircle className="h-4 w-4 shrink-0" />
                  <span>{copyError}</span>
                </div>
              )}

              {/* Thông tin cuốn sách chi tiết */}
              {copy && (
                <div className="rounded-lg border bg-muted/30 p-4 space-y-3">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div>
                      <h4 className="font-semibold text-base flex items-center gap-2">
                        <BookOpen className="h-4 w-4 text-primary" /> {copy.title}
                      </h4>
                      <p className="text-xs text-muted-foreground">Tác giả: {copy.author}</p>
                    </div>
                    <div className="text-right">
                      <span className="text-xs font-mono font-bold block">
                        Barcode: {copy.barcode}
                      </span>
                      <span className="text-xs text-muted-foreground">Thể loại: {copy.category}</span>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-2 text-xs border-t pt-3">
                    <div>
                      <span className="text-muted-foreground">ISBN: </span>
                      <span className="font-mono">{copy.isbn || '—'}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Tình trạng sách: </span>
                      <span className="font-medium">{copy.condition}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Trạng thái bản sao: </span>
                      <span
                        className={`font-semibold ${
                          copy.isAvailable ? 'text-emerald-600' : 'text-destructive'
                        }`}
                      >
                        {copy.status}
                      </span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Chính sách áp dụng: </span>
                      <span className="font-medium">{copy.policyName || 'Mặc định'}</span>
                    </div>
                  </div>

                  {/* Chi tiết tính toán hạn trả theo chính sách */}
                  {copy.isAvailable && (
                    <div className="rounded border bg-background p-3 text-xs space-y-1">
                      <div className="flex justify-between items-center">
                        <span className="text-muted-foreground flex items-center gap-1">
                          <Clock className="h-3.5 w-3.5" /> Thời hạn mượn quy định:
                        </span>
                        <span className="font-bold">{copy.loanPeriodDays} ngày</span>
                      </div>
                      <div className="flex justify-between items-center">
                        <span className="text-muted-foreground flex items-center gap-1">
                          <Calendar className="h-3.5 w-3.5" /> Ngày đến hạn hoàn trả:
                        </span>
                        <span className="font-bold text-primary">
                          {formatDate(copy.sampleDueAtUtc)}
                        </span>
                      </div>
                    </div>
                  )}

                  {!copy.isAvailable && copy.ineligibilityReason && (
                    <div className="rounded border border-destructive/30 bg-destructive/10 p-2.5 text-xs text-destructive flex items-center gap-2">
                      <AlertCircle className="h-4 w-4 shrink-0" />
                      <span>{copy.ineligibilityReason}</span>
                    </div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* ================= BƯỚC 3: TỔNG KẾT & XÁC NHẬN LẬP PHIẾU ================= */}
      <Card className="mt-6 border-2 border-primary/20">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="text-lg flex items-center gap-2">
              <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary text-xs font-bold text-primary-foreground">
                3
              </span>
              Xác nhận và Hoàn tất giao dịch mượn
            </CardTitle>
          </div>
          <CardDescription>
            Kiểm tra lại thông tin độc giả và bản sao sách trước khi tiến hành ghi sổ lưu thông.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col md:flex-row items-center justify-between gap-4 rounded-lg bg-muted/40 p-4">
            <div className="space-y-1 text-sm">
              <p>
                <span className="text-muted-foreground">Người mượn: </span>
                <span className="font-semibold">
                  {member ? `${member.fullName} (${member.memberCode})` : 'Chưa quét thẻ'}
                </span>
              </p>
              <p>
                <span className="text-muted-foreground">Sách mượn: </span>
                <span className="font-semibold">
                  {copy ? `${copy.title} [${copy.barcode}]` : 'Chưa quét mã vạch sách'}
                </span>
              </p>
              {copy?.sampleDueAtUtc && (
                <p>
                  <span className="text-muted-foreground">Hạn trả dự kiến: </span>
                  <span className="font-bold text-primary">{formatDate(copy.sampleDueAtUtc)}</span>
                </p>
              )}
            </div>

            <div className="flex w-full flex-col gap-3 sm:flex-row md:w-auto">
              <Button
                size="lg"
                disabled={!isCheckoutReady}
                onClick={handleCheckout}
                className="w-full sm:min-w-[200px] md:w-auto"
              >
                {isSubmitting ? (
                  'Đang xử lý...'
                ) : (
                  <>
                    <CheckCircle2 className="mr-2 h-5 w-5" /> Hoàn tất lập phiếu mượn
                  </>
                )}
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>
    </PageShell>
  )
}
