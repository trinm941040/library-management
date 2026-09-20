# MS2-22 — Kết quả Đặt trước và xác nhận nhận sách (Reservation Lifecycle & Pickup Workflow)

Hoàn thành toàn diện phân hệ Đặt trước và xác nhận nhận sách theo đúng yêu cầu nghiệp vụ và Acceptance Criteria cho cả Backend .NET 9 Clean Architecture và Frontend React TypeScript:

---

## 1. Hành vi và nghiệp vụ sau hoàn thiện

- **Quản lý hàng đợi đặt trước (Reservation Priority Queue):**
  - Tính toán thứ tự ưu tiên trong hàng đợi (`QueuePosition`: `#1`, `#2`...) dựa trên thời điểm tạo phiếu đặt trước (`ReservedAtUtc`).
  - Phiếu đặt trước đầu tiên trong hàng đợi khi sách có sẵn bản sao khả dụng trong kho sẽ chuyển sang trạng thái `ready` ("Sẵn sàng nhận").
  - Nếu sách chưa có bản sao khả dụng hoặc đang có người khác mượn hết, phiếu ở trạng thái `waiting` ("Chờ sách") kèm thứ tự trong hàng đợi.

- **Ràng buộc tính duy nhất & ngăn trùng lặp (Uniqueness & Conflict Prevention):**
  - Ngăn chặn độc giả tạo nhiều phiếu đặt trước mở cho cùng một đầu sách (`HasOpenReservationAsync`).
  - Ngăn chặn độc giả đặt trước cuốn sách mà chính độc giả đó hiện đang mượn (`HasActiveBorrowingAsync`).
  - Kiểm tra trạng thái hoạt động của độc giả (`MemberStatus.Active`), hiệu lực thẻ thư viện (`MembershipCard.Status == Active` và chưa hết hạn), và các hạn chế giao dịch (`MemberRestrictionType.Reservation` hoặc `AllTransactions`).

- **Quy trình hủy phiếu và chuyển giao ưu tiên (Cancellation & Queue Shift):**
  - Hỗ trợ hủy phiếu đặt trước kèm lý do (`CancelReservationRequest.Reason`).
  - Khi một phiếu đặt trước bị hủy (`cancelled`) hoặc hết hạn (`expired`), cơ hội và lượt ưu tiên tự động chuyển sang phiếu kế tiếp trong hàng đợi sách.
  - Ghi nhận `AuditLog` với hành động `reservation.cancelled` kèm snapshot dữ liệu trước và sau khi hủy.
  - Kiểm tra xung đột dữ liệu đồng thời qua `ConcurrencyToken` (trả về `409 Conflict` nếu dữ liệu đã bị thay đổi).

- **Quy trình trao sách và chuyển thành khoản mượn (Pickup / Fulfill Workflow):**
  - Endpoint `POST /api/v1/reservations/{id}/fulfill` thực hiện chuyển đổi nguyên tử (atomic transaction):
    - Đóng phiếu đặt trước (`FulfilledAtUtc = now`).
    - Tái sử dụng đầy đủ các quy tắc mượn sách: giới hạn số sách mượn tối đa của độc giả (`BorrowingLimit`, `MaxLoanBooks`), chính sách chặn khi có sách quá hạn (`BlockIfOverdue`), kiểm tra thẻ thư viện.
    - Phân bổ bản sao sách (`BookCopy`): hỗ trợ nhập/quét mã vạch cụ thể (`BookCopyBarcode`) hoặc tự động chọn bản sao khả dụng đầu tiên (`Available` hoặc `Reserved`).
    - Cập nhật trạng thái bản sao thành `CopyStatus.Borrowed`, giảm số lượng sách khả dụng trong kho (`book.Checkout`).
    - Tạo bản ghi mượn sách mới (`Borrowing.CreateWithCopy`) với hạn trả tính theo chính sách lưu thông (`CirculationPolicy`).
    - Ghi nhận đồng thời 2 nhật ký kiểm toán: `reservation.fulfilled` (cho phiếu đặt trước) và `borrowing.created_from_reservation` (cho khoản mượn mới).
    - Commit nguyên tử toàn bộ thay đổi trong một giao dịch cơ sở dữ liệu.

- **Giao diện người dùng (`/reservations`):**
  - Giao diện 100% tiếng Việt tự nhiên, không sử dụng tiếng Anh rời rạc.
  - Cột "Hàng đợi" hiển thị rõ ràng vị trí ưu tiên: huy hiệu vàng `Thứ #1`, `Thứ #2` khi chờ sách, huy hiệu xanh `Ưu tiên #1` khi đã có sách sẵn sàng nhận.
  - Nút "Chi tiết" mở hộp thoại `ReservationDetailDialog` hiển thị đầy đủ thông tin sách (Tác giả, ISBN, Thể loại, Số bản sao trong kho), độc giả (Email, Mã độc giả, Số thẻ, Nhóm độc giả), mốc thời gian, số ngày giữ chỗ và danh sách toàn bộ hàng đợi của cuốn sách đó.
  - Nút "Nhận sách" mở hộp thoại `FulfillReservationDialog` cho phép nhân viên quét/nhập mã vạch bản sao (hoặc để trống để hệ thống tự gán) và xem trước ngày hẹn trả dự kiến.
  - Nút "Hủy" mở hộp thoại `CancelReservationDialog` cho phép nhập lý do hủy.
  - Tự động bắt lỗi xung đột `409 Conflict` và cập nhật lại dữ liệu giao diện mượt mà.

---

## 2. Danh mục tệp tin đã thay đổi

### Backend
1. [`IReservationRepository.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Abstractions\Persistence\IReservationRepository.cs): Bổ sung `GetQueuePositionAsync`, `GetActiveReservationsForBookAsync`, `GetAvailableBookCopyByBarcodeAsync`, `GetFirstAvailableBookCopyAsync`, `AddAuditLogAsync`.
2. [`ReservationRepository.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Infrastructure\Persistence\Repositories\ReservationRepository.cs): Cài đặt các truy vấn LINQ/EF Core cho hàng đợi ưu tiên, bản sao sách và lưu nhật ký kiểm toán.
3. [`BookCopy.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Domain\Entities\BookCopy.cs): Cập nhật method `Checkout` cho phép mượn từ trạng thái `Available` lẫn `Reserved`.
4. [`ReservationModels.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Features\Reservations\ReservationModels.cs): Bổ sung `CancelReservationCommand`, `FulfillReservationCommand`, `ReservationDetailModel`, cập nhật `ReservationModel`.
5. [`ReservationContracts.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Api\Contracts\Reservations\ReservationContracts.cs): Bổ sung `CancelReservationRequest`, `FulfillReservationRequest`, `ReservationDetailResponse`, cập nhật `ReservationResponse`.
6. [`ReservationService.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Application\Features\Reservations\ReservationService.cs): Cài đặt `GetDetailAsync`, cập nhật `GetAsync`, `CreateAsync`, `CancelAsync` và `FulfillAsync` (xử lý concurrency, gán bản sao, tạo borrowing, ghi dual audit logs).
7. [`ReservationsController.cs`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Backend\src\UTH.Library.Api\Controllers\ReservationsController.cs): Bổ sung `GET /api/v1/reservations/{id}`, cập nhật `POST {id}/cancel` và `POST {id}/fulfill` nhận actor ID từ claims.

### Frontend
1. [`reservation-api.ts`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\reservations\reservation-api.ts): Cập nhật kiểu `LibraryReservation`, thêm `ReservationDetailResponse`, `CancelReservationInput`, `FulfillReservationInput`, thêm hàm `getReservationDetail`, cập nhật `cancelReservation` và `fulfillReservation`.
2. [`ReservationDetailDialog.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\reservations\components\ReservationDetailDialog.tsx): Modal xem chi tiết phiếu đặt trước, thông tin sách, độc giả, chính sách và danh sách hàng đợi của cuốn sách.
3. [`FulfillReservationDialog.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\reservations\components\FulfillReservationDialog.tsx): Modal xác nhận nhận sách, nhập mã vạch bản sao và xem trước ngày hẹn trả.
4. [`CancelReservationDialog.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\reservations\components\CancelReservationDialog.tsx): Modal xác nhận hủy phiếu kèm lý do.
5. [`ReservationsPage.tsx`](file:///e:/Học ĐH\Đồ án công nghệ phần mềm\cap nhat mơi\library-management-develop\Frontend\src\pages\reservations\ReservationsPage.tsx): Giao diện danh sách đặt trước hoàn chỉnh với các huy hiệu hàng đợi, tích hợp các modal và xử lý xung đột 409.

---

## 3. Quản lý trạng thái hệ thống

- Đã cập nhật `MANIFEST.md` bổ sung mã task `MS2-22` vào danh sách hoàn thành.
