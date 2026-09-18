# MS2-01 — Hoàn thiện mô hình dữ liệu và migration

- Phân loại: Backend / Database / Improvement
- Mức ưu tiên: P0
- Phụ thuộc: MS2-00

## Mô tả chi tiết

Hoàn thiện `LibraryDbContext`, entity configuration và migration cho các lớp còn thiếu trong sơ đồ lớp: phiên đăng nhập, nhân sự, phân quyền, biên mục chuẩn hóa, bản sao, vị trí, kiểm kê, nhập kho, thành viên, lưu thông, tiền phạt, báo cáo, thông báo và cấu hình. Giữ dữ liệu hiện có khi chuyển các trường chuỗi hoặc tổng số lượng sang quan hệ chuẩn hóa. Giữ các bảng hỗ trợ Identity Framework theo mapping chuẩn và không biến `TodoItem` thành nghiệp vụ thư viện cốt lõi.

## Acceptance criteria

### Backend

- [ ] Tất cả entity và quan hệ trong sơ đồ lớp được ánh xạ bằng EF Core với khóa, foreign key và delete behavior rõ ràng.
- [ ] `IdentityUserRole<Guid>`, `IdentityUserClaim<Guid>`, `IdentityUserLogin<Guid>`, `IdentityUserToken<Guid>`, `IdentityUserPasskey<Guid>` và `IdentityRoleClaim<Guid>` tiếp tục do ASP.NET Core Identity/IdentityDbContext quản lý với khóa và quan hệ chuẩn.
- [ ] `RefreshTokenSession` là session nghiệp vụ riêng của dự án, không bị gộp hoặc thay thế bởi `IdentityUserToken<Guid>`.
- [ ] `TodoItem` được giữ trong phạm vi kỹ thuật/thử nghiệm hoặc loại khỏi production API/navigation nếu không còn dùng; không entity nghiệp vụ mới được phụ thuộc vào `TodoItem`.
- [ ] Có unique index cho ISBN, barcode, mã thành viên, số thẻ, mã nhân viên, mã chi nhánh và khóa cấu hình phù hợp.
- [ ] Các entity cần chống ghi đồng thời có concurrency token hoặc `RowVersion`.
- [ ] Migration nâng cấp và rollback được tạo, không làm mất dữ liệu hợp lệ hiện có.
- [ ] Seed quyền và vai trò hệ thống có tính lặp lại, không tạo dữ liệu trùng.

## Checklist hoàn thành

### Backend

- [ ] Cấu hình enum, độ dài chuỗi, precision tiền tệ và timestamp UTC.
- [ ] Rà soát mapping Identity mặc định trước khi tạo migration để tránh đổi tên/xóa nhầm bảng, khóa hoặc index của framework.
- [ ] Ghi quyết định xử lý `TodoItem` và xác nhận không xuất hiện trong module nghiệp vụ cốt lõi.
- [ ] Bổ sung index phục vụ tra cứu và báo cáo thường dùng.
- [ ] Viết tài liệu mapping dữ liệu cũ sang model mới.
- [ ] Áp dụng migration lên database local sạch và database local có dữ liệu mẫu.

## Happy-case test

1. Tạo database local từ toàn bộ migration.
2. Nạp dữ liệu mẫu có quan hệ Book–BookCopy, Member–Borrowing và Role–Permission.
3. Truy vấn lại dữ liệu và xác nhận đủ quan hệ, index và trạng thái.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] Lệnh tạo hoặc cập nhật database local bằng EF Core thành công.
