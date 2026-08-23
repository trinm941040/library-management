# Development workflow

Đây là workflow dành riêng cho frontend Library Management hiện tại.

## 1. Chuẩn bị môi trường

Yêu cầu Node.js và npm. Cài dependency và chạy frontend:

```bash
npm install
npm run dev
```

API đăng nhập cần chạy tại `http://localhost:5191`. Vite proxy request `/api` tới
host này theo `vite.config.ts`.

## 2. Bắt đầu một task

1. Xác định kết quả và điều kiện hoàn thành của task.
2. Kiểm tra code liên quan trước khi tạo file mới.
3. Tạo branch theo [Git branching strategy](./git-branching-strategy.md).
4. Chỉ sửa những phần thuộc phạm vi task.

Ví dụ task “thêm trang quản lý sách” cần xác định tối thiểu:

- URL của page.
- Dữ liệu lấy từ API hay local.
- Các thao tác người dùng cần thực hiện.
- Trạng thái loading, rỗng và lỗi.
- Ai được phép truy cập.

## 3. Đặt code đúng vị trí

| Nội dung                       | Vị trí                                  |
| ------------------------------ | --------------------------------------- |
| Page hoàn chỉnh                | `src/pages/<feature>/<Feature>Page.tsx` |
| Component chỉ dùng cho feature | `src/pages/<feature>/components`        |
| Layout chung sau đăng nhập     | `src/layouts`                           |
| Component dùng lại             | `src/common/components`                 |
| Primitive shadcn               | `src/common/components/ui`              |
| Route                          | `src/routes/AppRoutes.tsx`              |
| Auth                           | `src/auth`                              |
| Tìm kiếm toàn hệ thống         | `src/search`                            |
| Setting toàn ứng dụng          | `src/settings`                          |

Không đưa component riêng của một page vào `common` chỉ để làm file page ngắn hơn.
Chỉ chuyển vào `common` khi ít nhất hai feature thực sự dùng lại nó.

## 4. Thêm một page mới

1. Tạo page trong `src/pages`.
2. Thêm route con bên trong `AppLayout` tại `src/routes/AppRoutes.tsx`.
3. Thêm link vào `src/layouts/components/Sidebar.tsx` nếu cần menu cố định.
4. Thêm nội dung tìm kiếm vào `src/search/search-data.ts`.
5. Cập nhật README nếu thêm URL hoặc thay đổi cấu trúc quan trọng.

Ví dụ route:

```tsx
<Route path="/books" element={<BooksPage />} />
```

Không bọc `BooksPage` bằng Header hoặc Sidebar. `AppLayout` đã cung cấp layout và
`ProtectedRoute` đã kiểm tra đăng nhập.

## 5. Làm giao diện

- Kiểm tra `src/common/components/ui` trước khi tự tạo UI primitive.
- Nếu thiếu primitive, thêm bằng shadcn CLI.
- Tham khảo [How to use the UI kit](./ui-kit-guide.md) và
  [How to customize Dialog content](./dialog-customization.md).
- Dùng màu token như `bg-background`, `text-foreground`, `text-muted-foreground` để
  dark mode hoạt động đúng.
- Kiểm tra giao diện sáng, tối, desktop và mobile.
- Button chỉ có icon phải có `aria-label`.
- Form phải có `Label`, validation và thông báo lỗi dễ hiểu.

## 6. Làm việc với dữ liệu

- API của một domain đặt gần domain đó, tương tự `src/auth/auth-api.ts`.
- Không gọi API trực tiếp rải rác trong nhiều component.
- Access token chỉ giữ trong memory; không lưu token vào `localStorage`.
- Dữ liệu user và tìm kiếm hiện là local tạm thời. Phải ghi rõ giới hạn này trong PR.
- Không lưu password hoặc dữ liệu nhạy cảm trong frontend.

Khi backend có endpoint mới, thay lớp đọc/ghi local bằng API nhưng giữ UI component
độc lập với chi tiết request nếu có thể.

## 7. Kiểm tra trong lúc làm

Kiểm tra thủ công đúng luồng liên quan tới task. Với một page có dữ liệu, tối thiểu
kiểm tra:

- Trạng thái có dữ liệu và không có dữ liệu.
- Tìm kiếm hoặc lọc.
- Thêm, sửa, xóa nếu có.
- Loading và API error nếu dùng backend.
- Refresh trình duyệt.
- Truy cập khi chưa đăng nhập.
- Dark/light mode và màn hình nhỏ.

Project hiện không duy trì test tự động, vì vậy kiểm tra thủ công và `lint/build` là
bắt buộc trước khi bàn giao.

## 8. Hoàn thành task

Chạy theo thứ tự:

```bash
npm run format
npm run lint
npm run build
```

Sau đó:

1. Xem lại diff và loại bỏ code debug, import thừa, text mẫu không cần thiết.
2. Kiểm tra không có secret hoặc file build trong commit.
3. Commit với message rõ ràng.
4. Push branch và tạo Pull Request.
5. Mô tả cách kiểm tra và giới hạn còn lại.
6. Sửa review, chạy lại các lệnh kiểm tra, rồi squash merge vào `main`.

## Definition of Done

Một task chỉ được xem là hoàn thành khi:

- Đúng yêu cầu và không phá vỡ luồng đăng nhập/navigation hiện có.
- Code nằm đúng thư mục và tuân theo
  [Naming conventions](./naming-conventions.md).
- UI dùng được trên desktop/mobile và sáng/tối.
- Search index và README được cập nhật nếu có page/chức năng mới.
- `npm run lint` và `npm run build` thành công.
- Pull Request mô tả đủ cách kiểm tra và giới hạn còn lại.
