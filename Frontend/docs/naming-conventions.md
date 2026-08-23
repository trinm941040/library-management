# Naming conventions

Tài liệu này là quy ước đặt tên thống nhất cho frontend Library Management.
Mục tiêu là để người mới có thể đoán được một file dùng để làm gì chỉ bằng tên của nó.

## 1. Ngôn ngữ

- Tên file, biến, hàm, type và comment kỹ thuật viết bằng tiếng Anh.
- Nội dung hiển thị cho người dùng có thể viết bằng tiếng Việt.
- Không viết tắt nếu từ đầy đủ vẫn ngắn và dễ hiểu.
- Dùng một thuật ngữ thống nhất. Ví dụ dùng `user`, không trộn `user`, `accountUser`
  và `systemPerson` cho cùng một đối tượng.

## 2. File và thư mục

| Loại                | Quy tắc                                 | Ví dụ                          |
| ------------------- | --------------------------------------- | ------------------------------ |
| Thư mục             | `kebab-case`                            | `pages/user-management`        |
| React component     | `PascalCase.tsx`                        | `UserFormDialog.tsx`           |
| Page                | kết thúc bằng `Page.tsx`                | `UserPage.tsx`                 |
| Layout              | kết thúc bằng `Layout.tsx`              | `AppLayout.tsx`                |
| Provider            | kết thúc bằng `Provider.tsx`            | `AuthProvider.tsx`             |
| Hook                | bắt đầu bằng `use`, file `camelCase.ts` | `useUsers.ts`                  |
| Utility, API, store | `kebab-case.ts`                         | `auth-api.ts`, `user-store.ts` |
| CSS                 | `kebab-case.css`                        | `app-shell.css`                |

Không tạo các tên chung chung như `helper.ts`, `data.ts`, `component.tsx` hoặc
`new-page.tsx`. Tên phải mô tả đúng trách nhiệm của file.

## 3. Component React

- Component dùng `PascalCase`: `GlobalSearch`, `UserPage`.
- Props type dùng tên component cộng với `Props`: `HeaderProps`.
- Mỗi file chỉ nên có một component chính được export.
- Component page đặt trong `src/pages/<feature>`.
- Component chỉ dùng cho một page đặt trong `src/pages/<feature>/components`.
- Component dùng lại giữa nhiều feature đặt trong `src/common/components`.
- Primitive UI của shadcn đặt trong `src/common/components/ui` và giữ cách đặt tên
  do shadcn tạo.

```tsx
type UserCardProps = {
  userName: string
  isLocked: boolean
  onEdit: () => void
}

export function UserCard({ userName, isLocked, onEdit }: UserCardProps) {
  // ...
}
```

## 4. Biến và hàm

- Biến và hàm dùng `camelCase`: `currentPage`, `loadUsers`.
- Boolean bắt đầu bằng `is`, `has`, `can` hoặc `should`: `isOpen`, `hasError`.
- Hàm xử lý sự kiện bên trong component bắt đầu bằng `handle`: `handleSubmit`.
- Callback truyền qua props bắt đầu bằng `on`: `onSave`, `onPageChange`.
- Hàm đọc dữ liệu dùng động từ rõ ràng: `loadUsers`, `fetchCurrentUser`.
- Hàm thay đổi dữ liệu dùng động từ hành động: `saveUsers`, `deleteUser`, `lockUser`.
- Hằng số dùng `UPPER_SNAKE_CASE`: `USERS_PER_PAGE`, `STORAGE_KEY`.
- Tránh tên một ký tự, ngoại trừ biến vòng lặp rất ngắn.

## 5. TypeScript

- Type và interface dùng `PascalCase`: `SystemUser`, `SearchItem`.
- Không thêm tiền tố `I` hoặc `T`: dùng `User`, không dùng `IUser` hay `TUser`.
- Union type dùng giá trị dễ hiểu:

```ts
type UserStatus = 'active' | 'locked'
type ThemePreference = 'light' | 'dark' | 'system'
```

- Không dùng `any`. Nếu chưa biết kiểu dữ liệu, dùng `unknown` rồi kiểm tra kiểu.
- Type chỉ dùng trong một file thì không cần export.

## 6. Route, CSS và storage

- Route dùng chữ thường và danh từ số nhiều: `/users`, `/books`, `/borrowings`.
- Route parameter có tên rõ ràng: `/users/:userId`.
- CSS class tự viết dùng `kebab-case`: `.top-actions`, `.sidebar-bottom`.
- Ưu tiên Tailwind cho style nhỏ nằm ngay trong component.
- Key trong `localStorage` bắt đầu bằng `library-`: `library-theme`.

## 7. Import

- Import qua alias `@/` khi đi sang thư mục khác.
- Chỉ dùng relative import cho file ở cùng feature hoặc cùng thư mục.
- Thứ tự: thư viện ngoài, alias nội bộ, relative import.

```tsx
import { useState } from 'react'
import { Button } from '@/common/components/ui/button'
import { UserFormDialog } from './components/UserFormDialog'
```

## Checklist khi đặt tên

- Người khác có hiểu trách nhiệm của file mà chưa cần mở file không?
- Tên có khớp với các file cùng loại đang có không?
- Boolean và event handler có đúng tiền tố không?
- Có đang tạo thêm một tên khác cho khái niệm đã tồn tại không?
