# MS2-09 — Quản lý vai trò quyền và gán quyền

- Phân loại: Full-stack / Feature / Security
- Mức ưu tiên: P0
- Phụ thuộc: MS2-01, MS2-02, MS2-03

## Mô tả chi tiết

Hoàn thiện Permission, ApplicationRole, RolePermission và gán role cho account. Quyền được định danh theo thao tác nghiệp vụ, backend kiểm tra ở mọi endpoint; role hệ thống được bảo vệ khỏi chỉnh sửa nguy hiểm. Frontend sử dụng tập permission hiệu lực được hợp nhất từ các role để quyết định route, sidebar/menu, tab, nút, form field và bulk action được hiển thị hoặc cho phép thao tác.

## Acceptance criteria

### Backend

- [ ] Permission policy được áp dụng trên endpoint; thay đổi role/permission có transaction, bảo vệ administrator cuối cùng và cập nhật quyền hiệu lực đúng hạn.
- [ ] Backend tính tập permission hiệu lực bằng hợp permission của tất cả role active được gán cho account, loại bỏ permission trùng và permission từ role inactive/đã thu hồi.
- [ ] Thay đổi role, RolePermission hoặc account-role assignment phải làm mất hiệu lực authorization cache/session permission cũ theo chính sách đã chọn.

### Frontend

- [ ] Role/permission matrix và account assignment UI chỉ cho phép thao tác hợp lệ, có confirmation và xử lý `403/409`.
- [ ] Sidebar, mobile menu và user navigation lọc item theo `requiredPermissions`; menu group không có item hợp lệ không được hiển thị.
- [ ] Route, tab, button, icon action, form section/field và bulk action sử dụng `PermissionBoundary` hoặc helper dùng chung; không hard-code role name trong component nghiệp vụ.
- [ ] UI cần quyền xem nhưng thiếu quyền sửa phải hiển thị read-only hoặc ẩn/disable action theo quy ước thống nhất; backend vẫn trả `403` nếu request bị gửi trực tiếp.

### Tích hợp

- [ ] Có danh mục permission theo module và action, tên permission là duy nhất.
- [ ] Tạo, cập nhật, ngừng sử dụng role và cấu hình tập permission được hỗ trợ.
- [ ] Có thể gán nhiều role cho account và tính đúng hợp quyền hiệu lực.
- [ ] Cùng một tập permission hiệu lực được dùng cho menu, route guard, action guard và dữ liệu current-user/session.
- [ ] Không thể xóa hoặc làm rỗng role hệ thống bắt buộc nếu gây mất quyền quản trị cuối cùng.
- [ ] Thay đổi role/permission có hiệu lực theo chính sách session và được ghi AuditLog trước/sau.

## Checklist hoàn thành

### Backend

- [ ] Hoàn thiện seed, repository, command/query, authorization handler, cache invalidation và AuditLog RBAC.
- [ ] Bổ sung permission catalog cho từng module và các thao tác `view/create/update/delete/approve/export/manage` khi nghiệp vụ có sử dụng.

### Frontend

- [ ] Hoàn thiện API hooks, role form, permission matrix, account assignment và PermissionBoundary.
- [ ] Hoàn thiện navigation registry và ánh xạ menu item → permission; dùng chung cho AppSidebar và mobile navigation.
- [ ] Rà soát toàn bộ page/feature để gắn permission cho route, tab, button, form field/section, bulk action và export.

### Tích hợp

- [ ] Bảo vệ endpoint bằng permission policy thay vì chỉ kiểm tra role name.
- [ ] Trang `/roles` và `/permissions` hiển thị ma trận quyền rõ ràng.
- [ ] UI ẩn hoặc disable action không được phép nhưng vẫn xử lý `403` từ backend.
- [ ] Không dựa vào menu bị ẩn để bảo mật; URL trực tiếp và API vẫn bị route guard/backend authorization từ chối.
- [ ] Sau khi quyền thay đổi, session/current-user được làm mới hoặc yêu cầu đăng nhập lại theo chính sách; menu và action không tiếp tục dùng quyền cũ.
- [ ] Có xác nhận cho thay đổi quyền phạm vi lớn.

## Happy-case test

1. Tạo role Biên mục và gán quyền xem/tạo/cập nhật biểu ghi.
2. Gán role cho account.
3. Đăng nhập bằng account đó, thực hiện được action được cấp và không thấy action ngoài quyền.
4. Gán thêm một role, làm mới session và xác nhận menu/action phản ánh hợp permission mới.
5. Thu hồi role hoặc permission, xác nhận item menu/action biến mất và URL/API trực tiếp bị từ chối.

## Build test local

- [ ] `dotnet build` thành công.
- [ ] `pnpm build` thành công.
