# MS2-03 — Hoàn thiện nền tảng frontend và điều hướng theo quyền

- Phân loại: Frontend / Architecture / Improvement
- Mức ưu tiên: P0
- Phụ thuộc: MS2-00

## Mô tả chi tiết

Hoàn thiện cấu trúc `app → pages → widgets/features → shared`, providers, route lazy loading, session bootstrap, API client, TanStack Query và URL state. Xây dựng route guard, sidebar, menu và UI boundary theo role/permission hiệu lực; frontend chỉ dùng guard để điều khiển trải nghiệm, không thay thế kiểm tra quyền backend.

## Acceptance criteria

### Frontend

- [ ] Tất cả route trong sơ đồ frontend được khai báo, lazy load và có metadata quyền.
- [ ] Route và menu dùng chung một navigation registry có `requiredPermissions`; không khai báo quyền rời rạc hoặc hard-code role name trong từng component.
- [ ] AppSidebar, mobile navigation và menu nhóm chỉ hiển thị item người dùng có quyền xem; nhóm không còn item hợp lệ phải được ẩn hoàn toàn.
- [ ] Truy cập URL trực tiếp đến route không có quyền phải hiển thị trang forbidden và không tải dữ liệu nghiệp vụ của route đó.
- [ ] API client hỗ trợ base URL theo environment, Bearer token, refresh một lần, timeout, AbortSignal và chuẩn hóa lỗi.
- [ ] Session được khôi phục trước khi render route bảo vệ, không tạo redirect loop.
- [ ] Sidebar/menu chỉ render sau khi role và permission hiệu lực đã được khôi phục, tránh nháy menu không được phép.
- [ ] Query state, form state, URL state và global state tuân thủ đúng phạm vi sở hữu.
- [ ] Không lưu mật khẩu, access token hoặc cache API nhạy cảm vào localStorage.

## Checklist hoàn thành

### Frontend

- [ ] Cấu hình Router, QueryClient, toast và error boundary.
- [ ] Bổ sung `PermissionBoundary` và route-level forbidden page.
- [ ] Bổ sung navigation registry, hàm `can`/`canAny`/`canAll` và bộ lọc menu dùng chung cho desktop/mobile.
- [ ] Loại bỏ kiểm tra role name trực tiếp khỏi menu và feature UI; mọi quyết định dùng permission hiệu lực từ session.
- [ ] Bổ sung schema kiểm tra environment và API response boundary.
- [ ] Kiểm tra dependency boundary, không deep-import nội bộ feature khác.

## Happy-case test

1. Đăng nhập bằng tài khoản có quyền.
2. Khôi phục phiên, truy cập route được phép và tải dữ liệu thành công.
3. Điều hướng trực tiếp bằng URL vẫn giữ đúng trạng thái xác thực và quyền.
4. Đăng nhập bằng role giới hạn và xác nhận sidebar/mobile menu chỉ hiển thị module được cấp quyền; nhóm rỗng không xuất hiện.

## Build test local

- [ ] `pnpm build` thành công.
- [ ] Type check và lint frontend thành công bằng script của repository.
