# Quản lý độc giả

## Phạm vi nghiệp vụ

Tính năng hiện thực nhóm use case “Quản lý thành viên và tiền phạt” và các quan hệ `Member`, `MembershipCard`, `MemberRestriction`, `Borrowing`, `Reservation`, `Violation`, `Payment`, `FineAdjustment` trong sơ đồ lớp.

- Tạo, tra cứu, lọc và cập nhật hồ sơ độc giả.
- Quản lý trạng thái: đang hoạt động, hết hạn, tạm đình chỉ, ngừng sử dụng.
- Thiết lập nhóm độc giả, số sách tối đa và thời hạn mượn tối đa.
- Cấp một thẻ hiện hành cho mỗi độc giả; gia hạn, tạm khóa, kích hoạt lại hoặc thu hồi thẻ.
- Ghi nhận hạn chế mượn, đặt trước hoặc toàn bộ giao dịch theo khoảng thời gian; gỡ hạn chế kèm lý do và người thực hiện.
- Tổng hợp lịch sử mượn và đặt trước theo mã độc giả.
- Tổng hợp vi phạm, tiền phạt gốc, tổng điều chỉnh, tổng đã thanh toán và số dư.
- Ghi nhận thanh toán từng phần/toàn phần theo phương thức và mã tham chiếu.
- Tăng, giảm hoặc miễn tiền phạt bằng điều chỉnh có lý do và người thực hiện.
- Lưu audit log cho các thay đổi hồ sơ, thẻ, hạn chế, thanh toán và điều chỉnh.

## Quy tắc nghiệp vụ

1. Mã độc giả và email là duy nhất; dữ liệu được chuẩn hóa trước khi lưu.
2. Một độc giả chỉ có một thẻ hiện hành; số thẻ là duy nhất và ngày hết hạn phải sau ngày cấp.
3. Chỉ độc giả `Active` có thẻ `Active`, còn hạn và không có hạn chế phù hợp mới được mượn/đặt trước.
4. Số phiếu mượn đang mở không được vượt `BorrowingLimit`; số ngày mượn không vượt `LoanPeriodDays`.
5. Thanh toán không được lớn hơn số dư; điều chỉnh không được làm số dư âm.
6. Khi thanh toán hết, vi phạm được đánh dấu `paid`; khi điều chỉnh số dư về 0, vi phạm được đánh dấu `waived`.
7. Cập nhật hồ sơ sử dụng concurrency token để phát hiện ghi đè dữ liệu cũ.

## API

| Method | Endpoint | Chức năng | Permission |
|---|---|---|---|
| GET | `/api/v1/members` | Danh sách, tìm kiếm, lọc trạng thái/nhóm, phân trang | `members.read` |
| GET | `/api/v1/members/{id}` | Chi tiết hồ sơ, thẻ, hạn chế, lịch sử và công nợ | `members.read` |
| POST | `/api/v1/members` | Tạo hồ sơ | `members.create` |
| PUT | `/api/v1/members/{id}` | Cập nhật hồ sơ, trạng thái và giới hạn | `members.update` |
| POST | `/api/v1/members/{id}/card` | Cấp thẻ | `members.manage-cards` |
| POST | `/api/v1/members/{id}/card/renew` | Gia hạn thẻ | `members.manage-cards` |
| PATCH | `/api/v1/members/{id}/card/status` | Đổi trạng thái thẻ | `members.manage-cards` |
| POST | `/api/v1/members/{id}/restrictions` | Thêm hạn chế | `members.manage-restrictions` |
| POST | `/api/v1/members/{id}/restrictions/{restrictionId}/remove` | Gỡ hạn chế | `members.manage-restrictions` |
| POST | `/api/v1/members/{id}/violations/{violationId}/payments` | Ghi nhận thanh toán | `members.manage-finances` |
| POST | `/api/v1/members/{id}/violations/{violationId}/adjustments` | Điều chỉnh tiền phạt | `members.manage-finances` |

## Dữ liệu và migration

Các migration `AddMemberManagement`, `DecoupleAuditActor` và `AddMemberFinanceForeignKeys` tạo cấu trúc dữ liệu, cho phép audit lưu actor theo snapshot và bảo đảm toàn vẹn quan hệ tài chính:

- `members`: hồ sơ, nhóm, trạng thái, giới hạn và concurrency token.
- `membership_cards`: thẻ hiện hành, ngày cấp/hết hạn và trạng thái.
- `member_restrictions`: loại hạn chế, hiệu lực, lý do tạo/gỡ và actor.
- `fine_payments`: khoản thanh toán, phương thức, tham chiếu và người thu.
- `fine_adjustments`: giá trị điều chỉnh, lý do và người thực hiện.

Các phiếu mượn, đặt trước và vi phạm tiếp tục dùng `BorrowerId`/`ReserverId`; các ID này nay tham chiếu logic tới `Member.Id` để chi tiết độc giả tổng hợp đầy đủ lịch sử.

## Frontend

Route `/members` cung cấp:

- Thẻ số liệu, tìm kiếm theo mã/tên/email/điện thoại, lọc nhóm và trạng thái.
- Bảng hồ sơ với thông tin thẻ, giới hạn, trạng thái và thao tác xem/sửa.
- Form tạo/cập nhật có validation HTML và thông báo lỗi từ Problem Details.
- Chi tiết tổng hợp hồ sơ, thẻ, hạn chế, lịch sử mượn/đặt trước và tiền phạt.
- Các luồng cấp/gia hạn/khóa thẻ, thêm/gỡ hạn chế, thanh toán và điều chỉnh.
- Biểu mẫu mượn, đặt trước và vi phạm sử dụng danh sách độc giả thay cho danh sách tài khoản hệ thống.

## Kiểm thử chấp nhận

- Không tạo được hai độc giả trùng mã hoặc email.
- Không cấp hai thẻ cho một độc giả hoặc hai thẻ trùng số.
- Không mượn/đặt trước khi hồ sơ hoặc thẻ không hoạt động, thẻ hết hạn hoặc có hạn chế tương ứng.
- Không mượn quá số lượng hoặc thời gian đã cấu hình.
- Chi tiết độc giả hiển thị giao dịch vừa tạo theo đúng `Member.Id`.
- Thanh toán từng phần làm giảm số dư; thanh toán đủ đóng vi phạm.
- Điều chỉnh âm bắt buộc có lý do và không làm số dư âm.
- Người không có permission tương ứng nhận HTTP 403.
