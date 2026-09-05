# Git branching strategy

Project sử dụng `main` làm nhánh phát hành ổn định, `develop` làm nhánh tích hợp và
các nhánh công việc ngắn hạn. Quy trình đầy đủ từ Issue đến Pull Request nằm tại
[Issue, branch và Pull Request guideline](../../docs/issue-and-pull-request-guideline.md).

## 1. Nhánh chính

- `main` luôn phải build được và có thể bàn giao. Chỉ cập nhật `main` theo quy trình
  phát hành đã được team thống nhất.
- `develop` là nhánh tích hợp cho công việc phát triển. Pull Request của feature, fix,
  refactor, docs và chore phải merge vào `develop`.
- Không commit trực tiếp lên `main` hoặc `develop`. Mọi thay đổi đi qua branch riêng và
  Pull Request.

## 2. Tên branch

Cấu trúc:

```text
<type>/<short-description>
```

| Type        | Dùng khi                          | Ví dụ                        |
| ----------- | --------------------------------- | ---------------------------- |
| `feature/`  | Thêm chức năng                    | `feature/user-management`    |
| `fix/`      | Sửa lỗi                           | `fix/login-refresh-token`    |
| `refactor/` | Đổi cấu trúc, không đổi chức năng | `refactor/shared-app-layout` |
| `docs/`     | Chỉ sửa tài liệu                  | `docs/project-workflow`      |
| `chore/`    | Tooling, dependency, cấu hình     | `chore/update-eslint`        |

Tên sau dấu `/` dùng `kebab-case`, ngắn và mô tả một mục tiêu. Mỗi branch chỉ xử lý
một task. Nên merge hoặc đóng branch trong vòng vài ngày.

## 3. Tạo branch

```bash
git switch develop
git pull --ff-only origin develop
git switch -c feature/user-management
```

Không tiếp tục feature mới trên một branch đã được merge.

## 4. Commit message

Dùng Conventional Commits ở mức đơn giản:

```text
<type>: <short description>
```

Ví dụ:

```text
feat: add local user management page
fix: keep sidebar visible when pinned
refactor: move shared layout out of dashboard
docs: add frontend workflow
chore: update lint configuration
```

Quy tắc:

- Viết ở thì hiện tại, ngắn và mô tả kết quả.
- Một commit nên chứa một thay đổi có liên quan.
- Không dùng message như `update`, `fix stuff`, `done`.
- Không commit `.env`, token, password, `node_modules` hoặc `dist`.
- Commit `package-lock.json` cùng `package.json` khi dependency thay đổi.

## 5. Pull Request

Trước khi mở Pull Request:

```bash
npm run format
npm run lint
npm run build
```

Pull Request cần ghi:

- Task hoặc vấn đề cần giải quyết.
- Những phần đã thay đổi.
- Cách kiểm tra thủ công.
- Ảnh chụp nếu giao diện thay đổi đáng kể.
- Giới hạn còn lại, ví dụ dữ liệu đang dùng `localStorage` vì backend chưa có API.

Ít nhất một thành viên khác nên review. Người review tập trung vào tính đúng, cấu trúc
dễ hiểu, khả năng truy cập, responsive và ảnh hưởng tới luồng đăng nhập.

## 6. Merge và dọn branch

Ưu tiên **Squash and merge** để lịch sử `develop` gọn. Sau khi merge:

```bash
git switch develop
git pull --ff-only origin develop
git branch -d feature/user-management
```

Xóa branch trên remote nếu nền tảng Git không tự xóa.

## 7. Khi branch bị chậm hơn `develop`

Cập nhật branch trước khi merge:

```bash
git switch develop
git pull --ff-only origin develop
git switch feature/user-management
git rebase develop
```

Nếu chưa quen rebase hoặc branch đang được nhiều người dùng, merge `develop` vào branch
thay vì tự ý force-push. Không force-push lên `main` hoặc `develop`.
