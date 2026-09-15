# MS2-06 — Triển khai trang thông tin cá nhân và user dropdown

- Phân loại: Full-stack / Feature / Security / UI
- Môi trường: Frontend, Backend
- Trạng thái: Đang triển khai
- Mức ưu tiên: P0
- Phụ thuộc: MS2-03, MS2-04, MS2-05

## Mô tả chi tiết

Triển khai trang `/profile` để nhân viên đang đăng nhập xem thông tin tài khoản, hồ sơ Employee, chi nhánh, vai trò và quyền hiệu lực; chỉnh sửa các trường thông tin cá nhân được phép; đổi mật khẩu và đăng xuất.

Tại vị trí tên người dùng ở góc trên bên phải AppHeader, triển khai user dropdown gồm thông tin tóm tắt, liên kết đến trang thông tin cá nhân, thao tác đổi mật khẩu và đăng xuất. Dropdown phải đồng bộ với session hiện tại và sử dụng được bằng chuột, bàn phím và thiết bị màn hình nhỏ.

Thông tin phải tuân thủ ranh giới dữ liệu:

- `ApplicationUser`: định danh đăng nhập, trạng thái tài khoản, display name, lần đăng nhập gần nhất, vai trò, quyền và session.
- `Employee`: mã nhân viên, họ tên, thông tin liên hệ, ngày sinh, địa chỉ, chức danh, phòng ban, chi nhánh và trạng thái việc làm.
- Người dùng chỉ được tự sửa họ tên, số điện thoại, ngày sinh và địa chỉ.
- `Employee.FullName` là nguồn dữ liệu chuẩn cho tên cá nhân; `ApplicationUser.DisplayName` là giá trị hiển thị được đồng bộ từ `Employee.FullName` trong cùng transaction khi hồ sơ được cập nhật.
- Email hoặc định danh đăng nhập, mã nhân viên, chức danh, phòng ban, chi nhánh, trạng thái việc làm, vai trò và quyền là chỉ đọc. Thay đổi các trường này phải đi qua chức năng quản trị riêng.
- Member/độc giả không tham gia luồng đăng nhập hoặc trang hồ sơ nội bộ này.

## API contract

### `GET /api/v1/me`

Trả thông tin người dùng hiện tại:

- `userId`, `employeeId`, `displayName`, `loginIdentifier`, `lastLoginAtUtc`.
- `employeeCode`, `fullName`, `phoneNumber`, `dateOfBirth`, `address`.
- `position`, `department`, `employmentStatus`.
- `branch` gồm `id`, `code`, `name`.
- `roles`, `permissions` và `rowVersion`.

Không trả password hash, refresh token, token hash, security stamp, claim nội bộ không cần thiết hoặc dữ liệu của người dùng khác.

### `PATCH /api/v1/me/profile`

- Nhận `fullName`, `phoneNumber`, `dateOfBirth`, `address`, `rowVersion`.
- Chỉ cập nhật whitelist trường tự phục vụ.
- Trả hồ sơ mới nhất sau khi cập nhật.
- Trả `422` khi dữ liệu không hợp lệ và `409` khi `rowVersion` đã cũ.

### `POST /api/v1/me/change-password`

- Nhận `currentPassword` và `newPassword`.
- Kiểm tra mật khẩu hiện tại và password policy của Identity.
- Sau khi thành công, thu hồi toàn bộ `RefreshTokenSession` của tài khoản và yêu cầu đăng nhập lại.
- Không ghi giá trị mật khẩu hoặc token vào log/AuditLog.

### `POST /api/v1/auth/logout`

- Thu hồi phiên đăng nhập hiện tại.
- Có tính idempotent; gọi lại không tạo lỗi nghiệp vụ hoặc khôi phục phiên đã thu hồi.

## Acceptance criteria

### Backend

- [ ] `GET /api/v1/me` yêu cầu xác thực và chỉ trả hồ sơ của tài khoản hiện tại cùng các trường được phép.
- [ ] Dữ liệu được lấy đúng quan hệ `ApplicationUser → Employee → Branch`; role và permission là quyền hiệu lực của session hiện tại.
- [ ] `PATCH /api/v1/me/profile` chỉ cập nhật `fullName`, `phoneNumber`, `dateOfBirth`, `address`; payload chứa trường khác bị bỏ qua hoặc từ chối theo contract thống nhất.
- [ ] Khi `Employee.FullName` thay đổi, backend đồng bộ `ApplicationUser.DisplayName` trong cùng transaction; nếu một bước lỗi thì không entity nào được cập nhật.
- [ ] Backend validate họ tên bắt buộc, độ dài trường, định dạng số điện thoại, ngày sinh hợp lệ và không nằm trong tương lai.
- [ ] Cập nhật hồ sơ sử dụng optimistic concurrency; dữ liệu cũ trả `409 Conflict` và không ghi đè thay đổi mới hơn.
- [ ] Đổi mật khẩu xác minh đúng mật khẩu hiện tại, áp dụng password policy và trả lỗi chung không tiết lộ dữ liệu bảo mật.
- [ ] Đổi mật khẩu thành công thu hồi toàn bộ refresh session của tài khoản; access token/refresh token cũ không thể tạo phiên mới.
- [ ] Logout thu hồi đúng phiên hiện tại và không ảnh hưởng phiên của tài khoản khác.
- [ ] Cập nhật hồ sơ, đổi mật khẩu và logout được ghi AuditLog với actor, action, timestamp và correlation ID; password/token/PII không cần thiết được loại bỏ.
- [ ] Các endpoint hỗ trợ cancellation token, Problem Details và mã lỗi thống nhất với kiến trúc backend.

### Frontend

- [ ] Tên người dùng ở góc trên bên phải AppHeader là trigger của dropdown và có accessible name rõ ràng.
- [ ] Trigger hiển thị avatar dạng chữ cái đầu hoặc avatar hiện có, display name và trạng thái mở/đóng; không dùng icon đơn lẻ để truyền đạt thông tin.
- [ ] Dropdown hiển thị thông tin tóm tắt chỉ đọc và ba thao tác: `Thông tin cá nhân`, `Đổi mật khẩu`, `Đăng xuất`.
- [ ] Dropdown mở bằng click hoặc `Enter`/`Space`, di chuyển bằng bàn phím, đóng bằng `Escape`, click bên ngoài hoặc sau khi chọn; focus được trả về trigger.
- [ ] Chọn `Thông tin cá nhân` điều hướng đến `/profile`; chọn `Đổi mật khẩu` điều hướng đến section bảo mật hoặc mở dialog có URL/state rõ ràng.
- [ ] Trang `/profile` hiển thị riêng nhóm thông tin cá nhân, thông tin công việc/chi nhánh và quyền truy cập.
- [ ] Các trường không cho tự sửa được hiển thị read-only và không được gửi trong mutation cập nhật hồ sơ.
- [ ] Form chỉnh sửa dùng React Hook Form và Zod; hiển thị lỗi field, chặn submit lặp, giữ dữ liệu khi lỗi và cảnh báo khi rời trang còn thay đổi chưa lưu.
- [ ] Form đổi mật khẩu có mật khẩu hiện tại, mật khẩu mới, xác nhận mật khẩu mới, nút hiện/ẩn an toàn và mô tả password policy.
- [ ] Giá trị mật khẩu không được lưu vào Zustand, localStorage, sessionStorage, query cache hoặc log; form được xóa sau submit hoặc unmount.
- [ ] Khi cập nhật họ tên thành công, tên trong AppHeader, dropdown và cache hồ sơ được đồng bộ ngay mà không cần tải lại trang.
- [ ] Frontend luôn hiển thị `displayName` do current-profile/session contract trả về, không tự suy luận từ nhiều nguồn dữ liệu khác nhau.
- [ ] Khi đổi mật khẩu thành công, frontend xóa session/query cache nhạy cảm, hiển thị thông báo và chuyển về `/login`.
- [ ] Khi logout, frontend chặn submit lặp, gọi API revoke, luôn xóa session local và chuyển về `/login`; lỗi mạng được thông báo nhưng không giữ UI ở trạng thái đăng nhập sai lệch.
- [ ] Trang và dropdown có đủ loading, error/retry, unauthorized/expired-session, conflict và success states.
- [ ] Giao diện responsive trên desktop/tablet/mobile, không bị tràn và không che nội dung chính.

### Tích hợp

- [ ] Session bootstrap, AppHeader, user dropdown và trang `/profile` sử dụng cùng một nguồn dữ liệu người dùng hiện tại; không gọi hoặc lưu các bản sao hồ sơ không đồng bộ.
- [ ] Sau khi cập nhật hồ sơ, response backend được validate ở biên API và cập nhật đồng thời profile query, session metadata và display name trên header.
- [ ] Người dùng không thể thay đổi field chỉ đọc bằng cách sửa request trực tiếp; backend là lớp thực thi quyền cuối cùng.
- [ ] Khi session hết hạn tại trang profile, hệ thống chỉ refresh một lần; nếu refresh thất bại thì xóa session và chuyển về login không tạo redirect loop.
- [ ] Khi đổi mật khẩu hoặc logout, refresh session bị thu hồi và mọi request bảo vệ tiếp theo nhận `401` cho đến khi đăng nhập lại.
- [ ] Các thao tác xem, sửa hồ sơ, đổi mật khẩu và đăng xuất hoạt động với tài khoản có một hoặc nhiều role mà không yêu cầu chọn role trên UI.

## Checklist hoàn thành

### Backend

- [ ] Kiểm tra model và quan hệ hiện có của `ApplicationUser`, `Employee`, `Branch`, `RefreshTokenSession` và `AuditLog`; chỉ bổ sung migration khi schema thực sự thiếu.
- [ ] Tạo request/response contract cho current profile, update profile và change password.
- [ ] Tạo query lấy current profile theo user ID từ claim đã xác thực; không nhận user ID từ client.
- [ ] Tạo command/handler cập nhật hồ sơ với whitelist field, validation và transaction boundary.
- [ ] Bổ sung mapping và transaction đồng bộ `Employee.FullName → ApplicationUser.DisplayName`.
- [ ] Bổ sung `RowVersion` hoặc concurrency token cho dữ liệu hồ sơ được phép chỉnh sửa nếu chưa có.
- [ ] Tạo command/handler đổi mật khẩu bằng ASP.NET Core Identity và thu hồi toàn bộ refresh session sau thành công.
- [ ] Hoàn thiện logout/revoke current session theo cơ chế token hiện tại.
- [ ] Áp dụng authorization, cancellation token, Problem Details và correlation ID cho các endpoint.
- [ ] Ghi AuditLog cho update profile, change password và logout; kiểm tra redaction password, token và PII.
- [ ] Cập nhật OpenAPI với request, response, mã `200/204/401/409/422` và mô tả lỗi.

### Frontend

- [ ] Tạo hoặc hoàn thiện feature `profile` gồm API functions, Zod schemas, query keys, hooks, model, form và public exports.
- [ ] Tạo route lazy-loaded `/profile` trong authenticated layout và bảo vệ bằng session guard.
- [ ] Tạo Profile page có view mode, edit mode, security/change-password section và các trạng thái chuẩn.
- [ ] Tạo form chỉnh sửa hồ sơ bằng React Hook Form + Zod, map đúng field editable/read-only và gửi `rowVersion`.
- [ ] Tạo form đổi mật khẩu, kiểm tra confirm password ở frontend và xóa toàn bộ password field sau xử lý.
- [ ] Tạo hoặc hoàn thiện `UserMenu` tại AppHeader, dùng dropdown primitive truy cập được thay vì tự quản lý focus thủ công nếu UI kit đã có primitive phù hợp.
- [ ] Bổ sung menu item, icon từ cùng icon family, nhãn truy cập và hành vi keyboard/focus.
- [ ] Kết nối logout mutation, chống click lặp, xóa session/query cache và điều hướng về `/login`.
- [ ] Đồng bộ display name sau update profile vào AppHeader/UserMenu mà không reload toàn trang.
- [ ] Chuẩn hóa selector/current-user model để AppHeader, UserMenu và Profile dùng cùng trường `displayName`.
- [ ] Xử lý lỗi `401`, `409`, `422`, network và server error bằng message có thể hành động; không che lỗi conflict.
- [ ] Kiểm tra responsive, focus-visible, contrast, reduced motion và accessible error announcement.

### Tích hợp

- [ ] Thống nhất contract và tên field giữa DTO backend, Zod schema frontend và form model.
- [ ] Kiểm tra dropdown và profile dùng dữ liệu từ current-user query/session hiện có, không tạo store toàn cục trùng lặp.
- [ ] Kiểm tra update profile làm mới đúng cache nhưng không làm mất role, permission hoặc session metadata.
- [ ] Kiểm tra change-password và logout thu hồi session đúng với cơ chế refresh token backend.
- [ ] Kiểm tra AuditLog sinh đúng actor/action và không chứa password/token.
- [ ] Cập nhật tài liệu route/API nếu repository có README hoặc API contract documentation.

## Happy-case test

### HC-01 Xem và cập nhật thông tin cá nhân

1. Đăng nhập bằng tài khoản active đã liên kết Employee và Branch.
2. Mở user dropdown, chọn `Thông tin cá nhân`.
3. Xác nhận `/profile` hiển thị đúng thông tin tài khoản, nhân viên, chi nhánh, role và permission.
4. Chuyển sang edit, cập nhật họ tên, số điện thoại, ngày sinh hoặc địa chỉ rồi lưu.
5. Xác nhận dữ liệu được lưu, toast thành công xuất hiện và display name trên AppHeader/UserMenu được cập nhật ngay.
6. Tải lại current profile và xác nhận `Employee.FullName` cùng `ApplicationUser.DisplayName` đã đồng bộ.

### HC-02 Đổi mật khẩu

1. Từ user dropdown chọn `Đổi mật khẩu`.
2. Nhập đúng mật khẩu hiện tại, mật khẩu mới đáp ứng policy và xác nhận khớp.
3. Gửi form và nhận thông báo thành công.
4. Xác nhận ứng dụng xóa session, chuyển về `/login` và refresh token cũ không dùng lại được.
5. Đăng nhập bằng mật khẩu mới thành công.

### HC-03 Sử dụng user dropdown bằng bàn phím

1. Dùng `Tab` đưa focus đến tên người dùng trong AppHeader.
2. Nhấn `Enter` hoặc `Space` để mở dropdown.
3. Dùng phím mũi tên hoặc `Tab` đến từng menu item và chọn trang profile.
4. Mở lại menu, nhấn `Escape` và xác nhận focus trở về trigger.

### HC-04 Đăng xuất

1. Đăng nhập và mở user dropdown.
2. Chọn `Đăng xuất` một lần.
3. Xác nhận API thu hồi session, frontend xóa session/query cache và điều hướng về `/login`.
4. Truy cập lại `/profile` và xác nhận bị chuyển về login.

## Build test local

- [ ] Backend build thành công bằng `dotnet build`.
- [ ] Frontend type-check/lint thành công bằng script hiện có của repository.
- [ ] Frontend production build thành công bằng `pnpm build`.
- [ ] Chạy được HC-01 đến HC-04 trên frontend và backend local với database local.
- [ ] Không yêu cầu Docker.
- [ ] Không chạy `dotnet test`; backend chỉ cần `dotnet build` thành công và không có lỗi.
- [ ] Frontend component/E2E smoke test có thể bỏ qua; happy case được kiểm tra trực tiếp trên local.
