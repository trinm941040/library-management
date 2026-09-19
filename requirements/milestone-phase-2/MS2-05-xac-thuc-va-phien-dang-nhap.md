# MS2-05 — Xác thực và quản lý phiên đăng nhập

- Phân loại: Full-stack / Feature / Security
- Mức ưu tiên: P0
- Phụ thuộc: MS2-01, MS2-02, MS2-03

## Mô tả chi tiết

Hoàn thiện đăng nhập nội bộ cho nhân viên bằng ASP.NET Core Identity, access token ngắn hạn và refresh token có thể thu hồi. Lưu refresh token dạng hash trong `RefreshTokenSession`, hỗ trợ token family, rotation, hết hạn, revoke và phát hiện reuse. Login response trả hồ sơ nhân viên cùng quyền hiệu lực, không trả bí mật.

## Acceptance criteria

### Backend

- [ ] Identity service phát hành access token và refresh token đúng hạn, lưu token hash, rotation state và token family trong `RefreshTokenSession`.
- [ ] Endpoint login/refresh/current-user trả role và permission hiệu lực đã hợp nhất từ tất cả role của account; không trả permission của role inactive hoặc assignment đã thu hồi.
- [ ] Endpoint login/refresh/revoke kiểm tra account status, permission và token reuse, trả lỗi chuẩn không tiết lộ account tồn tại.

### Frontend

- [ ] Trang login và session provider gọi đúng contract, giữ access token theo thiết kế an toàn và chỉ thực hiện một refresh khi nhiều request cùng hết hạn.
- [ ] Route guard, sidebar/menu và PermissionBoundary dùng cùng role/permission hiệu lực từ session; không tạo nhiều nguồn authorization state.
- [ ] Role/permission phải được tải xong trước khi render menu bảo vệ; UI không hiển thị tạm thời menu không được phép.

### Tích hợp

- [ ] Login kiểm tra credential, trạng thái tài khoản và liên kết nhân viên; lỗi credential dùng thông báo chung.
- [ ] Refresh token được rotate an toàn và token cũ không thể dùng lại.
- [ ] Phát hiện reuse thu hồi toàn bộ token family liên quan.
- [ ] Tài khoản khóa hoặc ngừng hoạt động không thể đăng nhập hay refresh.
- [ ] Frontend xử lý loading, credential sai, tài khoản khóa, session hết hạn và chuyển đến safe intended route.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện Identity configuration, token service, session repository, validator, handler, permission và audit.

### Frontend

- [ ] Hoàn thiện login schema/form, session bootstrap, API interceptor, query reset và route guard.

### Tích hợp

- [ ] API login, refresh, current-session và revoke được bảo vệ đúng.
- [ ] Token và mật khẩu không xuất hiện trong log hoặc response ngoài contract.
- [ ] UI login không có role selector.
- [ ] UI login không có library/organization selector.
- [ ] Permission hiệu lực được tải sau login và dùng cho route/action guard.
- [ ] Session/current-user cache có cơ chế làm mới khi role/permission thay đổi để menu và UI không dùng quyền cũ.

## Happy-case test

1. Đăng nhập bằng tài khoản active có liên kết Employee.
2. Nhận hồ sơ và quyền, mở dashboard.
3. Làm mới access token bằng refresh token và tiếp tục gọi API thành công.
4. Xác nhận menu và action hiển thị đúng theo hợp permission của tất cả role được gán.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
