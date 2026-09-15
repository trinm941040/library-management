# Issue, branch và Pull Request guideline

Tài liệu này quy định một quy trình làm việc chung cho toàn bộ repository, từ lúc ghi
nhận công việc đến khi merge vào nhánh `develop`. Không tách quy trình theo project hoặc
tầng kỹ thuật. Mỗi Issue tương ứng với một mục tiêu rõ ràng; mỗi branch và Pull Request
chỉ nên giải quyết một Issue.

## 1. Tạo Issue

Trước khi viết code, tìm trong danh sách Issue để tránh tạo nội dung trùng lặp. Nếu chưa
có, tạo Issue với tiêu đề ngắn gọn theo cấu trúc:

```text
[<Type>] <kết quả cần đạt>
```

Ví dụ:

```text
[Feature] Quản lý hồ sơ nhân viên
[Bug] Giữ phiên đăng nhập sau khi refresh
[Refactor] Chuẩn hóa xử lý lỗi xác thực
[Docs] Bổ sung quy trình tạo Pull Request
```

`Type` nên là một trong các giá trị: `Feature`, `Bug`, `Refactor`, `Docs` hoặc `Chore`.
Không thêm tên project hoặc tầng kỹ thuật vào tiêu đề. Phạm vi file và thành phần bị ảnh
hưởng được mô tả trong nội dung Issue.

Nội dung Issue tối thiểu:

```md
## Mục tiêu

Mô tả vấn đề hoặc kết quả cần đạt.

## Phạm vi

- Những phần cần thực hiện.
- Những phần không thuộc Issue này, nếu dễ gây hiểu nhầm.

## Acceptance criteria

- [ ] Điều kiện có thể kiểm tra thứ nhất.
- [ ] Điều kiện có thể kiểm tra thứ hai.

## Ghi chú kỹ thuật

Contract, dữ liệu, thiết kế, migration, tài liệu liên quan hoặc ràng buộc cần biết.
```

Sau khi tạo Issue:

1. Gán người thực hiện và label phù hợp nếu repository đang sử dụng label.
2. Ghi rõ dependency hoặc Issue đang chặn công việc.
3. Xác định tất cả thành phần bị ảnh hưởng trong cùng một phạm vi công việc.
4. Chỉ bắt đầu triển khai khi acceptance criteria đủ rõ để kiểm tra.

Không đưa password, access token, secret hoặc dữ liệu cá nhân nhạy cảm vào Issue.

## 2. Tạo branch từ `develop`

Mọi branch công việc phải được tạo từ phiên bản mới nhất của `develop`:

```bash
git switch develop
git pull --ff-only origin develop
git switch -c feature/employee-management
```

Không commit trực tiếp lên `main` hoặc `develop`.

Tên branch sử dụng cấu trúc:

```text
<type>/<short-description>
```

| Type        | Dùng khi                          | Ví dụ                         |
| ----------- | --------------------------------- | ----------------------------- |
| `feature/`  | Thêm chức năng                    | `feature/employee-management` |
| `fix/`      | Sửa lỗi                           | `fix/login-refresh-token`     |
| `refactor/` | Đổi cấu trúc, không đổi chức năng | `refactor/error-handling`     |
| `docs/`     | Chỉ sửa tài liệu                  | `docs/pull-request-guideline` |
| `chore/`    | Tooling, dependency, cấu hình     | `chore/update-tooling`        |

Phần mô tả sau dấu `/` phải:

- Dùng tiếng Anh và `kebab-case`.
- Ngắn gọn nhưng thể hiện đúng một mục tiêu.
- Không dùng khoảng trắng, dấu tiếng Việt hoặc dấu gạch dưới.
- Không dùng tên chung chung như `update`, `changes`, `new-feature`.
- Không chứa tên project nếu mục tiêu nghiệp vụ đã đủ rõ.

Nếu Issue lớn đến mức branch phải xử lý nhiều mục tiêu độc lập, hãy tách Issue trước khi
tạo branch.

## 3. Triển khai thay đổi

Một chức năng có thể cần thay đổi nhiều project trong cùng repository. Khi các thay đổi
cùng phục vụ một acceptance criteria, hãy triển khai chúng trong cùng branch và Pull
Request để contract, logic, giao diện, dữ liệu và kiểm thử được review đồng bộ.

Trong quá trình triển khai:

1. Chỉ thay đổi file thuộc phạm vi Issue.
2. Đọc quy ước và kiến trúc hiện có trước khi tạo abstraction hoặc dependency mới.
3. Giữ contract giữa các thành phần tương thích; cập nhật bên cung cấp và bên sử dụng
   trong cùng thay đổi khi contract thay đổi.
4. Thêm hoặc cập nhật migration khi cấu trúc dữ liệu thay đổi.
5. Bổ sung validation, authorization, trạng thái lỗi và kiểm thử phù hợp.
6. Cập nhật tài liệu khi hành vi, cấu hình hoặc cách vận hành thay đổi.
7. Không commit file môi trường, secret, dependency đã cài, output build hoặc code debug.
8. Kiểm tra diff trước mỗi commit.

## 4. Commit

Commit sử dụng Conventional Commits:

```text
<type>: <short description>
```

Ví dụ:

```text
feat: add employee management
fix: handle expired access token
refactor: centralize error handling
docs: add issue and pull request guideline
```

Quy tắc commit:

- Viết ở thì hiện tại và mô tả kết quả của thay đổi.
- Một commit chỉ chứa các thay đổi có liên quan.
- Không dùng message như `update`, `done`, `fix stuff`.
- Commit lockfile cùng manifest khi dependency thay đổi.
- Không dùng tên project làm scope chỉ để phân biệt nơi chứa file.

## 5. Đồng bộ với `develop`

Trước khi tạo Pull Request, cập nhật branch công việc từ `develop`:

```bash
git fetch origin
git rebase origin/develop
```

Nếu branch đang được nhiều người cùng sử dụng hoặc team không thống nhất dùng rebase,
merge `origin/develop` vào branch thay vì tự ý rewrite history. Chỉ dùng
`git push --force-with-lease` trên branch cá nhân sau khi rebase; tuyệt đối không
force-push lên `main` hoặc `develop`.

Sau khi xử lý conflict, chạy lại toàn bộ kiểm tra liên quan.

## 6. Kiểm tra trước Pull Request

Chạy đầy đủ format, lint, build và test của tất cả thành phần bị ảnh hưởng. Các lệnh
chuẩn hiện có trong repository gồm:

```bash
npm --prefix Frontend run format
npm --prefix Frontend run lint
npm --prefix Frontend run build
dotnet build Backend/LibraryManegement.sln
dotnet test Backend/LibraryManegement.sln
```

Không bỏ qua kiểm tra của một project nếu thay đổi ở project khác có thể ảnh hưởng đến
contract hoặc luồng tích hợp của nó.

Kiểm tra thủ công tối thiểu:

- Toàn bộ luồng chính trong acceptance criteria của Issue.
- Trạng thái thành công, validation, loading, empty, error và permission có liên quan.
- Contract request/response và mã lỗi khi có giao tiếp giữa các thành phần.
- Migration và khả năng đọc dữ liệu cũ khi cấu trúc dữ liệu thay đổi.
- Không làm hỏng đăng nhập, authorization, navigation hoặc luồng liên quan.
- Không có secret, output build hoặc thay đổi ngoài phạm vi trong diff.

Sau đó push branch:

```bash
git push -u origin feature/employee-management
```

## 7. Tạo Pull Request vào `develop`

Trên GitHub, chọn:

```text
base: develop  <-  compare: feature/employee-management
```

Không chọn `main` làm base cho Pull Request phát triển thông thường.

Tiêu đề Pull Request nên mô tả kết quả và dùng cùng loại với commit chính:

```text
feat: add employee management
```

Nội dung Pull Request tối thiểu:

```md
## Summary

- Thay đổi chính thứ nhất.
- Thay đổi chính thứ hai.

## Testing

- [x] Đã chạy format và lint
- [x] Đã chạy build và test
- [x] Đã kiểm tra thủ công luồng chính

## Contract and data changes

Mô tả contract, migration, cấu hình hoặc ghi `Không có`.

## Screenshots

Thêm ảnh trước/sau khi thay đổi có ảnh hưởng trực quan; nếu không, ghi `Không áp dụng`.

## Notes

Giới hạn hoặc bước triển khai cần lưu ý.

Closes #<issue-number>
```

Dùng `Closes #<issue-number>` để GitHub tự đóng Issue sau khi Pull Request được merge.
Nếu PR chỉ liên quan nhưng không hoàn tất Issue, dùng `Refs #<issue-number>`.

Trước khi yêu cầu review:

- Chọn đúng base branch là `develop`.
- Tự review tab **Files changed** theo luồng nghiệp vụ, không chỉ theo từng thư mục.
- Đảm bảo CI thành công và không còn conflict.
- Gán reviewer phù hợp với các thành phần bị ảnh hưởng.
- Trả lời hoặc xử lý toàn bộ review comment.

## 8. Merge và dọn branch

Khi Pull Request đã được approve, không còn conflict và các kiểm tra đều thành công,
ưu tiên **Squash and merge** vào `develop`. Squash commit phải có message rõ ràng và
không dùng tiêu đề tạm thời.

Sau khi merge:

```bash
git switch develop
git pull --ff-only origin develop
git branch -d feature/employee-management
```

Xóa branch trên remote nếu GitHub chưa tự xóa. Không tiếp tục công việc mới trên branch
đã merge; tạo Issue và branch mới cho mục tiêu tiếp theo.

## Checklist nhanh

- [ ] Issue không trùng, có phạm vi và acceptance criteria rõ ràng.
- [ ] Branch được tạo từ `develop` mới nhất và đúng quy tắc đặt tên.
- [ ] Mọi thay đổi phục vụ cùng mục tiêu được triển khai và review đồng bộ.
- [ ] Commit rõ nghĩa, không chứa secret hoặc file sinh tự động.
- [ ] Format, lint, build, test và kiểm tra thủ công đều thành công.
- [ ] Contract, migration và tài liệu đã được cập nhật nếu cần.
- [ ] Pull Request có base là `develop` và liên kết Issue.
- [ ] Review comment và conflict đã được xử lý.
- [ ] Squash merge thành công và branch cũ đã được xóa.
