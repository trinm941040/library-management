# MS2-20 — Kết quả Trả sách quá hạn, mất và hỏng

Hoàn thành toàn diện phân hệ Trả sách (Return workflow) bằng mã vạch cho cả Backend .NET 9 Clean Architecture và Frontend React TypeScript:

---

## 1. Hành vi và nghiệp vụ sau hoàn thiện

- **Quy trình Return Workflow:**
  - **Quét mã vạch bản sao sách (BookCopy Barcode):**
    - Tra cứu bản sao qua `Barcode`, xác định khoản mượn đang hoạt động (`GetActiveBorrowingByCopyIdAsync`).
    - Lấy thông tin tác phẩm, độc giả, ngày mượn, hạn trả và chính sách lưu thông hiệu lực (`CirculationPolicy`).
    - Tính toán trước hạn quá hạn và các khoản tiền phạt ước tính:
      - Quá hạn: số ngày quá hạn theo UTC, tiền phạt mỗi ngày (`FinePerDay`), tiền phạt tối đa (`MaxFineAmount`).
      - Mất/hỏng: tiền phạt cố định theo chính sách (`FixedFineAmount`, `LostBookPenaltyRatio`).
  - **Lựa chọn tình trạng sách và ghi chú:**
    - Hỗ trợ 4 tình trạng: `Good` (Tốt / Bình thường), `Worn` (Hao mòn tự nhiên), `Damaged` (Hư hỏng), `Lost` (Báo mất sách).
    - Khi chọn `Damaged` hoặc `Lost`: bắt buộc nhập ghi chú mô tả chi tiết hư hỏng hoặc lý do mất. Cho phép điều chỉnh mức tiền phạt theo thực tế hoặc giữ mặc định theo chính sách.
  - **Cam kết giao dịch nguyên tử & Idempotency:**
    - Đóng `Borrowing`: ghi nhận `ReturnedAtUtc = now` và tăng `ConcurrencyToken`.
    - Cập nhật bản sao `BookCopy`:
      - Nếu `Damaged`: chuyển trạng thái `CopyStatus.Damaged`, `CopyCondition.Damaged`, không đưa về kho khả dụng (`book.Quantity` không tăng).
      - Nếu `Lost`: chuyển trạng thái `CopyStatus.Lost`, `CopyCondition.Lost`, không đưa về kho khả dụng (`book.Quantity` không tăng).
      - Nếu `Good` hoặc `Worn`: hoàn trả vào kho khả dụng (`book.CheckIn(now)`), nếu có người đang đặt trước (`Reservation` waiting) thì chuyển sang `CopyStatus.Reserved`, ngược lại chuyển về `CopyStatus.Available`.
    - Tự động tạo bản ghi vi phạm `Violation` (loại `overdue`, `damage`, `lost`) kèm tiền phạt và gắn với độc giả / tác phẩm.
    - Toàn bộ giao dịch commit nguyên tử trong một transaction cùng với `AuditSaveChangesInterceptor` (ghi nhận nhật ký kiểm toán cho tất cả các thực thể thay đổi).
    - Idempotency & Concurrency: Nếu một khoản mượn đã được hoàn trả trước đó hoặc có xung đột cập nhật, request bị từ chối với lỗi `409 Conflict`.
  
- **Giao diện người dùng (`/circulation/return`):**
  - Tối ưu cho thiết bị đọc mã vạch: tự động focus ô quét, nhấn Enter để tra cứu tức thì.
  - Hỗ trợ quét trả liên tiếp: sau khi xác nhận trả thành công, hệ thống hiển thị tóm tắt và tự động xóa ô nhập, đưa con trỏ chuột về ô quét để thủ thư quét ngay cuốn tiếp theo.
  - Lưu và hiển thị danh sách lịch sử các cuốn đã trả trong ca trực hiện tại.
  - Hiển thị cảnh báo trực quan nếu sách trả là sách đang có độc giả đặt trước (`Reservation`).
  - Không làm mất dữ liệu đã quét khi gặp xung đột / lỗi mạng.

---

## 2. Danh mục tệp tin đã thay đổi

### Backend
1. [`BookCopy.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Domain/Entities/BookCopy.cs): Bổ sung method `ReturnWithCondition(condition, status, now)`.
2. [`IBorrowingRepository.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Application/Abstractions/Persistence/IBorrowingRepository.cs): Bổ sung `GetActiveBorrowingByCopyIdAsync(bookCopyId, cancellationToken)`.
3. [`BorrowingRepository.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Infrastructure/Persistence/Repositories/BorrowingRepository.cs): Cài đặt `GetActiveBorrowingByCopyIdAsync`.
4. [`IReservationRepository.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Application/Abstractions/Persistence/IReservationRepository.cs): Bổ sung `GetFirstWaitingReservationForBookAsync(bookId, utcNow, cancellationToken)`.
5. [`ReservationRepository.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Infrastructure/Persistence/Repositories/ReservationRepository.cs): Cài đặt `GetFirstWaitingReservationForBookAsync`.
6. [`BorrowingModels.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Application/Features/Borrowings/BorrowingModels.cs): Bổ sung `BookCopyReturnLookupResult`, `ConfirmReturnCommand`, `ViolationSummaryModel`, `ReturnExecutionResult`, `ReturnResult`.
7. [`BorrowingService.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Application/Features/Borrowings/BorrowingService.cs): Cài đặt `LookupCopyForReturnAsync`, `ConfirmReturnAsync` (tính tiền phạt, cập nhật trạng thái bản sao, tạo violation và kiểm tra reservation queue).
8. [`BorrowingContracts.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Api/Contracts/Borrowings/BorrowingContracts.cs): Bổ sung `BookCopyReturnLookupResponse`, `ConfirmReturnRequest`, `ViolationSummaryResponse`, `ReturnExecutionResponse`.
9. [`BorrowingsController.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Api/Controllers/BorrowingsController.cs): Bổ sung các endpoints:
   - `GET /api/v1/borrowings/return/lookup-copy` (phân quyền: `borrowings.return`)
   - `POST /api/v1/borrowings/return/confirm` (phân quyền: `borrowings.return`)

### Frontend
1. [`borrowing-api.ts`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/pages/borrowings/borrowing-api.ts): Thêm client functions `lookupBookCopyForReturn`, `confirmReturn` và các kiểu dữ liệu liên quan.
2. [`ReturnPage.tsx`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/pages/borrowings/ReturnPage.tsx): Giao diện chuyên dụng quét mã vạch trả sách, tính phạt dự kiến, chọn tình trạng và lịch sử ca trực.
3. [`AppRoutes.tsx`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/routes/AppRoutes.tsx): Đăng ký route `/circulation/return` với `ProtectedRoute` yêu cầu quyền `borrowings.return`.
4. [`navigation.ts`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/app/navigation.ts): Bổ sung mục "Trả sách & Xử lý" vào nhóm Quản lý tác vụ với biểu tượng `RotateCcw`.
5. [`BorrowingsPage.tsx`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/pages/borrowings/BorrowingsPage.tsx): Bổ sung nút bấm "Quét mã trả sách" dẫn trực tiếp đến `/circulation/return`.
6. [`MANIFEST.md`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/MANIFEST.md): Cập nhật trạng thái `MS2-20` sang "Đã hoàn thành".

---

## 3. Đối chiếu Acceptance Criteria

- [x] **Backend:** Return handler xác định Borrowing active, tính overdue/lost/damaged và commit Borrowing/BookCopy/Violation/AuditLog nguyên tử, idempotent.
- [x] **Frontend:** Return UI hỗ trợ scan liên tiếp, preview khoản mượn/phạt, chọn condition, nhập ghi chú bắt buộc và hiển thị kết quả từng lần trả.
- [x] **Tích hợp:**
  - Barcode xác định đúng BookCopy và Borrowing active cần trả.
  - Trả bình thường đóng Borrowing và đưa BookCopy về available nếu không có hold khác; nếu có hold chuyển `Reserved`.
  - Quá hạn tính số ngày theo quy tắc UTC và tạo `Violation` tương ứng.
  - Mất/hỏng cập nhật condition/status, ghi chú bắt buộc và không đưa bản sao về available.
  - Borrowing, BookCopy, Violation và AuditLog commit nguyên tử; request lặp không xử lý trả hai lần (idempotency, conflict 409).
