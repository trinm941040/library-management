# MS2-23 — Kết quả Ghi nhận vi phạm và tính tiền phạt (Violation Recording & Fine Calculation Workflow)

Hoàn thành toàn diện phân hệ Ghi nhận vi phạm và tính tiền phạt theo đúng yêu cầu nghiệp vụ và Acceptance Criteria cho cả Backend .NET 9 Clean Architecture và Frontend React TypeScript:

---

## 1. Hành vi và nghiệp vụ sau hoàn thiện

- **Chuẩn hóa thực thể Vi phạm (Violation Entity Normalization):**
  - Hỗ trợ đầy đủ các loại vi phạm: Quá hạn (`overdue`), Mất tài liệu (`lost`), Hư hỏng tài liệu (`damage`), và Vi phạm quy chế khác (`other`).
  - Liên kết đa chiều nguồn phát sinh: `BorrowingId` (khoản mượn), `BorrowerId` (độc giả/thành viên), `BookId` (đầu sách), `BookCopyId` (bản sao sách), và `BookCopyBarcode` (mã vạch bản sao).
  - Trích xuất và lưu trữ dữ liệu an toàn trong `AppliedPolicySnapshot` (JSONB) mà không làm phá vỡ schema database hoặc yêu cầu migration phức tạp.

- **Căn cứ và công thức tính phạt minh bạch (Calculation Basis & Policy Snapshot):**
  - Endpoint `POST /api/v1/violations/preview` cho phép tính toán và xem trước cấu phần phạt theo chính sách lưu thông hiện hành:
    - Phạt quá hạn: `Số ngày trễ hạn × Đơn giá phạt mỗi ngày (VND/ngày)` có áp trần tối đa theo quy định.
    - Hư hỏng tài liệu: Tính theo tỷ lệ phần trăm mức độ hỏng trên giá bìa sách (ví dụ: `Tỷ lệ % × Giá sách`).
    - Mất tài liệu: Tính theo hệ số đền bù trên giá bìa sách kèm phí xử lý nghiệp vụ (ví dụ: `Hệ số đền bù × Giá sách + Phí xử lý`).
    - Vi phạm khác: Mức phạt quy định cố định hoặc tùy chỉnh kèm lý do chi tiết.
  - Lưu trữ toàn diện `CalculationBasis`, `PolicyId`, và `Version` trong `AppliedPolicySnapshot` để đảm bảo tính bất biến (immutability) cho mục đích kiểm toán sau này.

- **Bảo toàn số dư công nợ (Real-time Balance Synchronization):**
  - Số dư còn nợ được tính toán động và chính xác:
    $$\text{Balance} = \max(0, \text{OriginalFineAmount} + \text{TotalAdjustments} - \text{TotalPayments})$$
  - Trạng thái vi phạm được chuẩn hóa thành 4 trạng thái phản ánh đúng thực tế tài chính:
    - `open`: Chưa thanh toán (số dư bằng toàn bộ số tiền phạt gốc).
    - `partially_paid`: Đã nộp một phần (đã thanh toán hoặc điều chỉnh nhưng số dư $> 0$).
    - `paid`: Đã hoàn tất thanh toán (số dư $= 0$ do các khoản nộp phạt).
    - `waived`: Đã được miễn giảm toàn bộ (được ban giám hiệu hoặc thủ thư phê duyệt miễn phạt).

- **Cơ chế chống ghi nhận trùng lặp (Idempotency Control):**
  - Phương thức `GetExistingViolationAsync` tự động kiểm tra xem khoản mượn (`BorrowingId`) hoặc bản sao sách (`BookCopyId`) với cùng loại vi phạm đã tồn tại bản ghi chưa giải quyết (`ResolvedAtUtc == null`) hay chưa.
  - Ngăn ngừa tình trạng ghi nhận 2 lần khi người dùng gửi lại request hoặc hệ thống quét hàng loạt tự động kích hoạt.

- **Kiểm toán nghiệp vụ & xử lý xung đột (Audit Trail & Concurrency):**
  - Mọi thao tác ghi nhận vi phạm (`violation.created`), thu phạt (`violation.paid`), và miễn giảm phạt (`violation.waived`) đều được ghi nhận vào `AuditLog` với đầy đủ thông tin người thực hiện (`ActorId`), dấu vết dữ liệu trước/sau và lý do nghiệp vụ.
  - Bảo vệ dữ liệu với `ConcurrencyToken` (trả về mã lỗi `409 Conflict` nếu dữ liệu vi phạm bị thay đổi đồng thời).

- **Giao diện người dùng (`/violations`):**
  - Giao diện 100% tiếng Việt chuẩn mực và tự nhiên, không sử dụng tiếng Anh rời rạc.
  - Cột "Số dư còn nợ" làm nổi bật công nợ: màu đỏ cảnh báo (`text-rose-600 font-semibold`) khi còn nợ $> 0$, màu xanh lá (`text-emerald-600`) khi đã nộp xong (`0 ₫`).
  - Hộp thoại chi tiết vi phạm (`ViolationDetailDialog`):
    - Trực quan hóa nguồn gốc phát sinh (Độc giả, Thẻ thư viện, Đầu sách, Bản sao cụ thể kèm mã vạch).
    - Phân tích chi tiết căn cứ và công thức tính toán mức phạt theo chính sách.
    - Bảng lịch sử các đợt nộp phạt (`FinePayment`) và lịch sử các đợt điều chỉnh/miễn giảm (`FineAdjustment`).
  - Hộp thoại ghi nhận vi phạm (`ViolationFormDialog`):
    - Tự động gọi API `preview` khi thay đổi loại vi phạm, số ngày quá hạn, giá bìa hoặc mức độ hư hỏng để gợi ý mức phạt chuẩn xác kèm giải thích công thức trực tiếp trên form.
  - Đồng bộ bộ lọc (Tìm kiếm, Loại vi phạm, Trạng thái, Chỉ lọc khoản còn nợ, Phân trang) lên URL Search Params giúp giữ nguyên ngữ cảnh khi tải lại trang hoặc chia sẻ liên kết.

---

## 2. Danh mục tệp tin đã thay đổi

### Backend
1. [`Violation.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Domain\Entities\Violation.cs): Bổ sung các phương thức trích xuất dữ liệu snapshot: `ExtractBorrowingId()`, `ExtractBookCopyId()`, `ExtractBookCopyBarcode()`, `ExtractCalculationBasis()`.
2. [`IViolationRepository.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Features\Violations\IViolationRepository.cs): Bổ sung `GetExistingViolationAsync`, `GetFinanceSummaryAsync`, `GetFinanceTransactionsAsync`, `AddAuditLogAsync`, mở rộng tham số tìm kiếm và bộ lọc cho `GetPageAsync`.
3. [`ViolationRepository.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Infrastructure\Persistence\Repositories\ViolationRepository.cs): Cài đặt LINQ/EF Core cho tính toán số dư động, lịch sử giao dịch tài chính độc giả, kiểm tra trùng lặp và ghi nhật ký kiểm toán.
4. [`ViolationModels.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Features\Violations\ViolationModels.cs): Bổ sung các mô hình nghiệp vụ: `ViolationModel` (số dư, liên kết nguồn, token), `ViolationListQuery`, `CreateViolationCommand`, `FinePreviewCommand`, `FinePreviewResult`, `PaymentHistoryItemModel`, `AdjustmentHistoryItemModel`, `ViolationDetailModel`.
5. [`ViolationService.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Features\Violations\ViolationService.cs): Cài đặt `CalculateFinePreviewAsync`, `GetDetailAsync`, hoàn thiện `CreateAsync` (chống trùng lặp, lưu snapshot tính toán và ghi audit log), cập nhật `GetAsync` và ánh xạ trạng thái nợ/thanh toán.
6. [`ViolationContracts.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Api\Contracts\Violations\ViolationContracts.cs): Chuẩn hóa DTO hợp đồng: `ViolationFilterRequest`, `CreateViolationRequest`, `FinePreviewRequest`, `FinePreviewResponse`, `ViolationResponse`, `ViolationDetailResponse`.
7. [`ViolationsController.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Api\Controllers\ViolationsController.cs): Cài đặt các endpoint `POST /preview`, `GET /{id}`, `GET /`, `POST /`, `POST /{id}/pay`, `POST /{id}/waive`.

### Frontend
1. [`violation-api.ts`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\violations\violation-api.ts): Mở rộng interface `LibraryViolation`, thêm các kiểu `PaymentHistoryItem`, `AdjustmentHistoryItem`, `ViolationDetailResponse`, `FinePreviewInput`, `FinePreviewResponse`, cập nhật hàm `getViolations`, thêm `getViolationDetail` và `previewFine`.
2. [`ViolationDetailDialog.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\violations\components\ViolationDetailDialog.tsx): Hộp thoại hiển thị chi tiết vi phạm, căn cứ tính toán, thông tin độc giả, sách, bản sao và lịch sử thanh toán/điều chỉnh.
3. [`ViolationFormDialog.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\violations\components\ViolationFormDialog.tsx): Hộp thoại tạo vi phạm có tích hợp tính năng tự động gợi ý mức phạt qua `previewFine`, cho phép chọn bản sao sách và ghi chú lý do.
4. [`ViolationsPage.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\violations\ViolationsPage.tsx): Trang danh sách vi phạm hoàn thiện với bộ lọc đồng bộ URL params, hiển thị số dư còn nợ nổi bật, nút xem chi tiết và các hành động nộp phạt/miễn giảm.
5. [`textarea.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\common\components\ui\textarea.tsx): Thành phần giao diện dùng chung hỗ trợ các trường văn bản nhiều dòng.
6. [`MANIFEST.md`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\MANIFEST.md): Cập nhật mã task `MS2-23` vào danh sách Đã hoàn thành.
