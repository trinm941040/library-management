# MS2-19 — Kết quả Lập phiếu mượn bằng mã thẻ và mã vạch

Hoàn thành toàn diện phân hệ Checkout (lập phiếu mượn bằng mã thẻ và mã vạch) cho cả Backend .NET 9 Clean Architecture và Frontend React TypeScript:

---

## 1. Hành vi và nghiệp vụ sau hoàn thiện

- **Quy trình Checkout Workflow:**
  - **Quét thẻ độc giả:**
    - Tra cứu độc giả qua số thẻ (`CardNumber`) hoặc mã hội viên (`MemberCode`).
    - Kiểm tra điều kiện mượn: trạng thái tài khoản `Active`, thẻ thư viện `Active` và chưa hết hạn (`ExpiresOn >= today`), không có lệnh hạn chế mượn sách (`MemberRestriction`), chưa vượt giới hạn mượn (`ActiveLoans < BorrowingLimit`), và không có sách quá hạn khi chính sách yêu cầu chặn (`BlockIfOverdue`).
  - **Quét mã vạch bản sao sách (BookCopy):**
    - Tra cứu bản sao qua `Barcode`, kiểm tra tính khả dụng (`CopyStatus.Available` và không có `Borrowing` đang hoạt động).
    - Cảnh báo rõ ràng nếu bản sao đang được mượn, bị hư hỏng, bị mất hoặc thanh lý.
    - Áp dụng chính sách lưu thông hiệu lực (`CirculationPolicy`) theo nhóm độc giả (`MemberGroup`) và thể loại sách (`Category`), tính toán hạn trả dự kiến (`DueAtUtc`).
  - **Cam kết giao dịch nguyên tử & Concurrency Control:**
    - Cập nhật trạng thái `BookCopy` sang `Borrowed` (`bookCopy.Checkout(now)`) và tăng `ConcurrencyToken`.
    - Tạo `Borrowing` gắn `BookId`, `BookCopyId`, `BorrowerId`, `ProcessedByEmployeeId`, `DueAtUtc`, `AppliedPolicyId`, `AppliedPolicySnapshot`.
    - Lưu vết tự động trong cùng một transaction thông qua `AuditSaveChangesInterceptor` (ghi nhật ký kiểm toán cho cả `BookCopy` và `Borrowing`, tự động che các trường PII/nhạy cảm).
    - Xử lý tương tranh lạc quan: Nếu 2 yêu cầu đồng thời mượn cùng 1 bản sao sách, 1 yêu cầu thành công, yêu cầu còn lại phát sinh xung đột và nhận mã `409 Conflict`.
  - **Hoàn trả bản sao:**
    - Khi gọi `ReturnAsync`, hệ thống tự động cập nhật trạng thái bản sao sách `BookCopy` trở lại `Available`.

- **Giao diện người dùng (`/circulation/checkout`):**
  - Tối ưu thao tác với máy quét mã vạch và bàn phím (tự động focus, nhấn Enter để tra cứu / gửi form).
  - Giao diện 3 bước rõ ràng: Quét thẻ $\rightarrow$ Quét sách $\rightarrow$ Xem trước chính sách & Hạn trả $\rightarrow$ Hoàn tất.
  - Khi phát sinh lỗi hoặc conflict, không làm mất dữ liệu độc giả/sách đã quét.
  - Sau khi mượn thành công, cung cấp 2 nút thao tác nhanh: "Quét tiếp sách cho độc giả này" và "Lập phiếu cho độc giả mới".

---

## 2. Danh mục tệp tin đã thay đổi

### Backend
1. [`BookCopy.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Domain/Entities/BookCopy.cs): Bổ sung `Checkout()` và `Return()` kèm cập nhật `ConcurrencyToken`.
2. [`Borrowing.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Domain/Entities/Borrowing.cs): Bổ sung `BookCopyId`, `ProcessedByEmployeeId` và factory method `CreateWithCopy()`.
3. [`LibraryDbContext.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Infrastructure/Persistence/LibraryDbContext.cs): Cấu hình mapping `BookCopyId`, `ProcessedByEmployeeId`, khóa ngoại và index `IX_borrowings_BookCopyId_ReturnedAtUtc`.
4. [`20260915130000_AddBookCopyAndEmployeeToBorrowings.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Infrastructure/Persistence/Migrations/20260915130000_AddBookCopyAndEmployeeToBorrowings.cs): Migration cơ sở dữ liệu có khả năng rollback.
5. [`IBorrowingRepository.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Application/Abstractions/Persistence/IBorrowingRepository.cs) & [`BorrowingRepository.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Infrastructure/Persistence/Repositories/BorrowingRepository.cs): Bổ sung `HasActiveBorrowingForCopyAsync`, `GetBookCopyByBarcodeAsync`, `GetBookCopyByIdAsync`.
6. [`IMemberRepository.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Application/Abstractions/Persistence/IMemberRepository.cs) & [`MemberRepository.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Infrastructure/Persistence/Repositories/MemberRepository.cs): Bổ sung `GetByCardOrCodeAsync`.
7. [`BorrowingModels.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Application/Features/Borrowings/BorrowingModels.cs): Thêm `MemberCheckoutLookupResult`, `BookCopyCheckoutLookupResult`, `CheckoutWithBarcodeCommand` và mở rộng `BorrowingModel`.
8. [`BorrowingService.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Application/Features/Borrowings/BorrowingService.cs): Cài đặt `LookupMemberForCheckoutAsync`, `LookupBookCopyForCheckoutAsync`, `CheckoutWithBarcodeAsync` và hoàn trả bản sao trong `ReturnAsync`.
9. [`BorrowingContracts.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Api/Contracts/Borrowings/BorrowingContracts.cs): Thêm DTO request/response tra cứu và checkout.
10. [`BorrowingsController.cs`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Backend/src/UTH.Library.Api/Controllers/BorrowingsController.cs): Thêm các endpoints:
   - `GET /api/v1/borrowings/checkout/lookup-member`
   - `GET /api/v1/borrowings/checkout/lookup-copy`
   - `POST /api/v1/borrowings/checkout`

### Frontend
1. [`borrowing-api.ts`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/pages/borrowings/borrowing-api.ts): Thêm client functions `lookupMemberForCheckout`, `lookupBookCopyForCheckout`, `checkoutWithBarcode`.
2. [`CheckoutPage.tsx`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/pages/borrowings/CheckoutPage.tsx): Màn hình lập phiếu mượn chuyên dụng theo quy trình quét 3 bước.
3. [`AppRoutes.tsx`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/routes/AppRoutes.tsx): Đăng ký route `/circulation/checkout` với `ProtectedRoute` yêu cầu quyền `borrowings.create`.
4. [`navigation.ts`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/app/navigation.ts): Bổ sung mục "Lập phiếu mượn" vào nhóm Quản lý tác vụ với biểu tượng `Barcode`.
5. [`BorrowingsPage.tsx`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/Frontend/src/pages/borrowings/BorrowingsPage.tsx): Bổ sung nút bấm "Quét mã lập phiếu" dẫn trực tiếp đến `/circulation/checkout`.
6. [`MANIFEST.md`](file:///e:/Học ĐH/Đồ án công nghệ phần mềm/cap nhat mơi/library-management-develop/MANIFEST.md): Cập nhật trạng thái `MS2-19` sang "Đã hoàn thành".

---

## 3. Đối chiếu Acceptance Criteria

- [x] **Backend:** Checkout handler enforce eligibility, policy, BookCopy uniqueness và commit Borrowing/BookCopy/AuditLog nguyên tử với concurrency control.
- [x] **Frontend:** Checkout UI tối ưu scan card/barcode, hiển thị member/copy/policy preview và xử lý validation/forbidden/conflict không mất dữ liệu đã quét.
- [x] **Tích hợp:**
  - Từ chối thẻ hết hạn/khóa, Member bị hạn chế, vượt giới hạn, còn nghĩa vụ chặn mượn hoặc BookCopy không available.
  - Due date được tính từ policy hiệu lực và lưu cùng giao dịch.
  - Checkout thành công tạo Borrowing gắn Member, BookCopy và Employee xử lý; BookCopy chuyển borrowed.
  - Hai request đồng thời cho cùng BookCopy chỉ một request thành công, request còn lại trả `409 Conflict`.
  - Frontend hiển thị từng bước quét, điều kiện mượn, hạn trả và kết quả rõ ràng.
