# Library Management Frontend

Dự án React + TypeScript + Vite cho hệ thống quản lý thư viện.

## Tài liệu làm việc

- [Quy tắc đặt tên](guides/naming-conventions.md)
- [Chiến lược Git branching](guides/git-branching-strategy.md)
- [Workflow phát triển frontend](guides/development-workflow.md)
- [Quy trình tạo Issue, branch và Pull Request](../issue-and-pull-request-guideline.md)
- [Cách sử dụng UI kit](guides/ui-kit-guide.md)
- [Cách tùy chỉnh nội dung Dialog](guides/dialog-customization.md)

## Cấu trúc source code

```text
src/
├── common/
│   └── components/
│       └── ui/              # Component do shadcn CLI quản lý
├── layouts/
│   ├── components/          # Header và Sidebar dùng chung
│   └── AppLayout.tsx        # Layout của các page sau đăng nhập
├── pages/
│   ├── dashboard/
│   │   ├── components/      # Component chỉ dùng trong dashboard
│   │   │   └── MetricCard.tsx
│   │   ├── DashboardPage.tsx
│   └── users/
│       ├── components/
│       │   └── UserFormDialog.tsx
│       ├── UserPage.tsx     # Màn hình quản lý tài khoản
│       └── user-store.ts    # Nơi đọc/ghi dữ liệu user tạm thời
├── routes/
│   └── AppRoutes.tsx        # Khai báo URL của các page
├── search/
│   ├── GlobalSearch.tsx     # Thanh tìm kiếm trong Header
│   └── search-data.ts       # Dữ liệu và thuật toán tìm kiếm local
├── styles/
│   └── app-shell.css        # CSS cho layout và vùng sau đăng nhập
├── utils/                   # Hàm hỗ trợ dùng ở nhiều nơi
├── App.tsx                  # Chọn page cần hiển thị
├── index.css                # CSS dùng cho toàn dự án
└── main.tsx                 # Điểm bắt đầu của ứng dụng
```

### Page đặt ở đâu?

Mỗi màn hình nằm trong một thư mục riêng:

```text
src/pages/login/LoginPage.tsx
src/pages/users/UserPage.tsx
src/pages/books/BooksPage.tsx
src/pages/members/MembersPage.tsx
src/pages/borrowings/BorrowingsPage.tsx
```

Quy tắc đơn giản:

- `pages`: một màn hình hoàn chỉnh.
- `layouts`: khung giao diện dùng chung cho nhiều page, như Header và Sidebar.
- `common/components/ui`: component UI do shadcn sinh ra.
- Component chỉ dùng cho một page thì đặt ngay trong thư mục của page đó.
- Không đặt code xử lý riêng của login vào `common`.

## UI kit shadcn

Project chỉ sử dụng một UI kit là shadcn. Cấu hình CLI nằm trong
`components.json`; theme và Tailwind token nằm trong `src/index.css`.

Component hiện có:

```text
src/common/components/ui/
├── button.tsx
├── badge.tsx
├── card.tsx
├── dialog.tsx
├── input.tsx
├── label.tsx
├── pagination.tsx
├── radio-group.tsx
├── select.tsx
├── table.tsx
└── switch.tsx
```

Thêm component mới bằng CLI:

```bash
npx shadcn@latest add dialog
```

Sử dụng component:

```tsx
import { Button } from '@/common/components/ui/button'

;<Button>Lưu thay đổi</Button>
```

Không tự tạo thêm `Button`, `Input`, `Dialog` ở thư mục khác. Nếu cần
primitive mới, hãy thêm bằng shadcn CLI để mọi page dùng chung một chuẩn.

Các URL hiện có được khai báo tập trung trong `src/routes/AppRoutes.tsx`:

- `/login`: trang đăng nhập.
- `/dashboard`: trang tổng quan.
- `/users`: thêm, sửa, xoá, khoá hoặc mở khoá tài khoản.
- `/settings`: cố định sidebar và chọn giao diện.
- `/`: chuyển đến `/dashboard`.
- URL không tồn tại: chuyển về `/dashboard`.

Khi thêm page mới, tạo page trong `src/pages` rồi thêm một `<Route>` tương ứng vào
`AppRoutes.tsx`.

## Tìm kiếm toàn hệ thống

Thanh tìm kiếm nằm trong Header và hiện dùng dữ liệu local tại
`src/search/search-data.ts`. Nó tìm được page, chức năng và tài khoản đang lưu trong
`localStorage`. Tìm kiếm không phân biệt chữ hoa, dấu tiếng Việt và chấp nhận một số
lỗi gõ nhẹ. Khi backend có API tìm kiếm, thay hàm `searchLocalContent` bằng request API.

## Đăng nhập

Frontend kết nối đến API `http://localhost:5191`. Khi chạy `npm run dev`, Vite chuyển
các request `/api` đến API này theo cấu hình trong `vite.config.ts`.

Luồng đăng nhập:

1. Khi mở ứng dụng, `AuthProvider` gọi `/api/v1/auth/refresh` để kiểm tra refresh cookie.
2. Nếu phiên còn hiệu lực, frontend gọi `/api/v1/me` và cho phép vào dashboard.
3. Nếu chưa đăng nhập, route bảo vệ chuyển user đến `/login`.
4. Login page gửi email và password đến `/api/v1/auth/login`.
5. Khi đăng xuất, frontend gọi `/api/v1/auth/logout` và chuyển về `/login`.

Access token chỉ được giữ trong bộ nhớ. Refresh token do API lưu bằng cookie HttpOnly.
Backend cần được chạy tại cổng `5191` trước khi thử đăng nhập.

Trang `/profile` và menu tài khoản trên Header dùng chung dữ liệu current-user từ
`AuthProvider`. Trang cho phép cập nhật thông tin cá nhân, xem dữ liệu công việc/quyền
truy cập. Trang `/profile/change-password` xử lý đổi mật khẩu riêng; mật khẩu không
được lưu vào storage hoặc state toàn cục.

## Quản lý user

Màn hình quản lý user nằm tại `src/pages/users/UserPage.tsx`. Form thêm và sửa
nằm trong `src/pages/users/components/UserFormDialog.tsx` để file page dễ đọc hơn.

Backend hiện chưa có API dành cho quản trị user, vì vậy dữ liệu của màn hình này
đang được lưu tạm trong `localStorage` bởi `src/pages/users/user-store.ts`. Mật khẩu
tạm thời không được lưu vào trình duyệt và user mới chưa thể dùng để đăng nhập thật.
Khi backend có API user, chỉ cần thay phần đọc/ghi trong store bằng các request API.

## Cài đặt giao diện

Nút đổi giao diện sáng/tối luôn nằm ở góc trên bên phải của mọi page, kể cả
trang đăng nhập. Lựa chọn được lưu trong `localStorage` nên vẫn được giữ sau
khi tải lại trang. Trang Cài đặt ở cuối sidebar dùng để cấu hình sidebar.

## Chạy dự án

```bash
npm install
npm run dev
```

Các lệnh còn lại:

```bash
npm run build
npm run lint
npm run format
```

dsadasdasdas
