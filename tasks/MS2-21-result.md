# MS2-21 — Kết quả Gia hạn khoản mượn (Borrowing Renewal Workflow)

Hoàn thành toàn diện phân hệ Gia hạn khoản mượn theo đúng yêu cầu nghiệp vụ và Acceptance Criteria cho cả Backend .NET 9 Clean Architecture và Frontend React TypeScript:

---

## 1. Hành vi và nghiệp vụ sau hoàn thiện

- **Quy trình kiểm tra điều kiện gia hạn (Renewal Eligibility Check):**
  - **Kiểm tra trạng thái khoản mượn (Borrowing Status):**
    - Khoản mượn phải ở trạng thái đang mượn (chưa hoàn trả: `ReturnedAtUtc is null`). Nếu đã hoàn trả, hệ thống từ chối gia hạn.
    - So sánh `DueAtUtc` với thời điểm hiện tại (`timeProvider.GetUtcNow()`). Nếu đã quá hạn, kiểm tra chính sách `AllowRenewalIfOverdue`. Nếu không cho phép khi quá hạn, từ chối gia hạn.
  - **Kiểm tra giới hạn số lần gia hạn (Max Renewals):**
    - Kiểm tra `RenewalCount` hiện tại của khoản mượn với `MaxRenewals` quy định trong chính sách lưu thông (`CirculationPolicy`). Nếu đã đạt số lần tối đa, từ chối gia hạn.
  - **Kiểm tra thông tin độc giả và hạn chế (Member & Restrictions):**
    - Độc giả phải ở trạng thái hoạt động (`MemberStatus.Active`).
    - Thẻ thư viện (`MembershipCard`) phải còn hiệu lực và chưa hết hạn.
    - Độc giả không có bất kỳ hạn chế mượn sách hoặc hạn chế giao dịch nào đang có hiệu lực (`MemberRestrictionType.Borrowing` hoặc `AllTransactions`).
  - **Kiểm tra hàng đợi đặt trước (Reservation Priority Queue):**
    - Kiểm tra xem sách có độc giả khác đang đặt trước và chờ nhận hay không (`IReservationRepository.GetFirstWaitingReservationForBookAsync`). Nếu có người khác đang chờ, từ chối gia hạn để ưu tiên người đặt trước.
  - **Chính sách lưu thông (Circulation Policy):**
    - Phải có chính sách cho phép gia hạn (`AllowRenewal == true`).
    - Tính toán thời hạn gia hạn (`RenewalPeriodDays` ngày, nếu không cấu hình thì dùng `LoanPeriodDays` hoặc 14 ngày mặc định) cộng dồn vào hạn hiện tại.

- **Thực thi gia hạn nguyên tử & Idempotency / Concurrency:**
  - Cập nhật hạn trả `DueAtUtc` của `Borrowing`, tăng `RenewalCount`, sinh `ConcurrencyToken` mới.
  - Tạo bản ghi `Renewal` lưu trữ lịch sử: hạn cũ (`PreviousDueAtUtc`), hạn mới (`NewDueAtUtc`), người thực hiện (`RenewedByUserId`), thời điểm (`RenewedAtUtc`), chính sách áp dụng (`AppliedPolicyId`, `AppliedPolicyVersion`, `AppliedPolicySnapshot`).
  - Tạo bản ghi `AuditLog` với action `borrowing.renewed` ghi nhận snapshot dữ liệu trước và sau gia hạn.
  - Toàn bộ được commit nguyên tử trong cùng transaction qua `borrowings.SaveChangesAsync`.
  - Kiểm tra `ConcurrencyToken`: nếu khoản mượn bị thay đổi bởi thao tác khác cùng thời điểm, trả về mã lỗi `409 Conflict`.

- **Phân quyền và bảo mật:**
  - Định nghĩa permission riêng: `borrowings.renew = "borrowings.renew"`.
  - Endpoint `POST /api/v1/borrowings/{id}/renew` yêu cầu quyền `borrowings.renew`.
  - Endpoints `GET /api/v1/borrowings/{id}` và `GET /api/v1/borrowings/{id}/renewal-preview` yêu cầu quyền `borrowings.read`.

- **Giao diện người dùng (`/loans/:id` & `/borrowings/:id`):**
  - Trang chi tiết khoản mượn hiển thị thông tin tác phẩm (Tên sách, Tác giả, ISBN, Mã vạch bản sao, Thể loại), độc giả (Họ tên, Email, Mã độc giả, Số thẻ thư viện, Nhóm độc giả) và tiến độ mượn trả (Ngày mượn, Hạn trả hiện tại, Ngày trả).
  - Thẻ kiểm tra điều kiện gia hạn (Renewal Eligibility Card):
    - Đủ điều kiện: Huy hiệu xanh "Đủ điều kiện", hiển thị hạn trả mới dự kiến (+X ngày), số lần gia hạn còn lại.
    - Không đủ điều kiện: Huy hiệu đỏ "Không đủ điều kiện" kèm danh sách chi tiết các lý do từ chối.
  - Nút hành động "Gia hạn khoản mượn" được bảo vệ bởi quyền `borrowings.renew` và chỉ mở khi đủ điều kiện.
  - Hộp thoại xác nhận (Confirm Dialog) trước khi thực hiện, hiển thị rõ ràng hạn cũ, hạn mới, lần gia hạn tiếp theo.
  - Xử lý lỗi `403` (không có quyền) và `409` (xung đột dữ liệu) trực quan, có nút "Tải lại dữ liệu mới nhất" mà không làm mất trạng thái của người dùng.
  - Lịch sử gia hạn (Renewal History Timeline / Table) hiển thị chi tiết tất cả các lần gia hạn trước đó: hạn cũ, hạn mới, thời điểm, nhân viên thực hiện và phiên bản chính sách.
  - Danh sách phiếu mượn (`/borrowings`) bổ sung nút liên kết "Chi tiết" trỏ trực tiếp đến trang `/loans/:id`.

---

## 2. Danh mục tệp tin đã thay đổi

### Backend
1. [`AuthModels.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Abstractions\Identity\AuthModels.cs): Bổ sung hằng số quyền `BorrowingsRenew = "borrowings.renew"` và thêm vào `Permissions.All`.
2. [`IBorrowingRepository.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Abstractions\Persistence\IBorrowingRepository.cs): Bổ sung method `GetRenewalsByBorrowingIdAsync` và `AddAuditLogAsync`.
3. [`BorrowingRepository.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Infrastructure\Persistence\Repositories\BorrowingRepository.cs): Cài đặt `GetRenewalsByBorrowingIdAsync` và `AddAuditLogAsync`.
4. [`BorrowingModels.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Features\Borrowings\BorrowingModels.cs): Bổ sung `RenewBorrowingCommand`, `RenewalHistoryModel`, `RenewalPreviewResult`, `BorrowingDetailModel`.
5. [`BorrowingService.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Features\Borrowings\BorrowingService.cs): Cài đặt `GetRenewalPreviewAsync`, `GetDetailAsync`, `RenewAsync` (xác thực đa tầng, ghi nhận Renewal, AuditLog và concurrency token).
6. [`BorrowingContracts.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Api\Contracts\Borrowings\BorrowingContracts.cs): Bổ sung `RenewBorrowingRequest`, `RenewalHistoryResponse`, `RenewalPreviewResponse`, `BorrowingDetailResponse`.
7. [`BorrowingsController.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Api\Controllers\BorrowingsController.cs): Bổ sung các endpoints:
   - `GET /api/v1/borrowings/{id}` (phân quyền: `borrowings.read`)
   - `GET /api/v1/borrowings/{id}/renewal-preview` (phân quyền: `borrowings.read`)
   - `POST /api/v1/borrowings/{id}/renew` (phân quyền: `borrowings.renew`)

### Frontend
1. [`borrowing-api.ts`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\borrowings\borrowing-api.ts): Thêm class `ApiError` lưu `status`, các kiểu dữ liệu `RenewalHistoryItem`, `RenewalPreview`, `BorrowingDetail` và client functions `getBorrowingDetail`, `getRenewalPreview`, `renewBorrowing`.
2. [`LoanDetailPage.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\borrowings\LoanDetailPage.tsx): Giao diện chi tiết khoản mượn, thẻ điều kiện gia hạn, xem trước hạn mới, dialog xác nhận, xử lý 403/409 và lịch sử gia hạn.
3. [`AppRoutes.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\routes\AppRoutes.tsx): Đăng ký các routes `/loans/:id` và `/borrowings/:id` với `ProtectedRoute`.
4. [`navigation.ts`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\app\navigation.ts): Bổ sung phân quyền cho route `/loans/:id` và `/borrowings/:id` (`borrowings.read`).
5. [`BorrowingsPage.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\borrowings\BorrowingsPage.tsx): Thêm nút "Chi tiết" dẫn đến `/loans/:id` trong cột Thao tác.
6. [`MANIFEST.md`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\MANIFEST.md): Cập nhật trạng thái `MS2-21` sang "Đã hoàn thành".

---

## 3. Đối chiếu Acceptance Criteria

- [x] **Renewal handler:** Kiểm tra policy / restriction / reservation, lưu `Renewal` và cập nhật `Borrowing` / `AuditLog` nguyên tử với version check.
- [x] **Loan detail:** Hiển thị eligibility, danh sách lý do từ chối nếu có, hạn cũ / hạn mới dự kiến, lịch sử renewal và xử lý `403/409` rõ ràng.
- [x] **Điều kiện gia hạn:** Chỉ `Borrowing` active và đủ điều kiện theo policy mới được gia hạn.
- [x] **Từ chối vi phạm:** Từ chối khi đạt max renewals, đã quá hạn không được phép, Member bị hạn chế hoặc có reservation ưu tiên.
- [x] **Dữ liệu Renewal:** Lưu hạn cũ, hạn mới, thời điểm và nhân viên thực hiện.
- [x] **Nguyên tử & Phiên bản:** Borrowing cập nhật due date và concurrency token trong cùng transaction với `Renewal` và `AuditLog`.
- [x] **Giao diện xác nhận:** UI hiển thị lý do không đủ điều kiện và hạn mới trước khi mở hộp thoại xác nhận.
