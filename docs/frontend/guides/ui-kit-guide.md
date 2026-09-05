# How to use the UI kit

Project sử dụng shadcn làm UI kit duy nhất. Các component nằm tại
`src/common/components/ui` và được cấu hình bởi `components.json`.

## 1. Component hiện có

```text
src/common/components/ui/
├── badge.tsx
├── button.tsx
├── card.tsx
├── dialog.tsx
├── input.tsx
├── label.tsx
├── pagination.tsx
├── radio-group.tsx
├── select.tsx
├── switch.tsx
└── table.tsx
```

Trước khi tự viết một Button, Input, Dialog hoặc Table mới, hãy kiểm tra thư mục này.

## 2. Import component

Dùng alias `@/` thay vì đường dẫn dài:

```tsx
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
```

Không import trực tiếp Radix primitive trong page nếu shadcn component tương ứng đã
tồn tại.

## 3. Ví dụ cơ bản

### Button

```tsx
<Button onClick={handleSave}>Lưu</Button>
<Button variant="outline">Hủy</Button>
<Button variant="destructive">Xóa</Button>
<Button variant="ghost" size="icon" aria-label="Chỉnh sửa">
  <Pencil />
</Button>
```

Các variant đang có: `default`, `destructive`, `outline`, `secondary`, `ghost`,
`link`.

### Form field

`Label.htmlFor` phải trùng với `Input.id`:

```tsx
<div className="grid gap-2">
  <Label htmlFor="book-title">Tên sách</Label>
  <Input id="book-title" name="title" required />
</div>
```

### Card

```tsx
<Card>
  <CardHeader>
    <CardTitle>Thông tin sách</CardTitle>
    <CardDescription>Nội dung mô tả ngắn.</CardDescription>
  </CardHeader>
  <CardContent>Nội dung chính</CardContent>
  <CardFooter>
    <Button>Lưu</Button>
  </CardFooter>
</Card>
```

### Select

```tsx
<Select value={status} onValueChange={setStatus}>
  <SelectTrigger className="w-full">
    <SelectValue placeholder="Chọn trạng thái" />
  </SelectTrigger>
  <SelectContent>
    <SelectItem value="active">Đang hoạt động</SelectItem>
    <SelectItem value="locked">Đã khóa</SelectItem>
  </SelectContent>
</Select>
```

### Pagination

```tsx
<Pagination currentPage={currentPage} totalPages={totalPages} onPageChange={setCurrentPage} />
```

Phần cắt mảng dữ liệu vẫn thuộc page:

```ts
const firstIndex = (currentPage - 1) * PAGE_SIZE
const visibleItems = items.slice(firstIndex, firstIndex + PAGE_SIZE)
```

## 4. Thêm component shadcn mới

Chạy từ thư mục gốc của frontend:

```bash
npx shadcn@latest add tooltip
```

CLI sẽ đọc `components.json` và tạo file vào `src/common/components/ui`. Sau khi thêm:

```bash
npm run format
npm run lint
npm run build
```

Nếu CLI hỏi ghi đè một component đang có, chọn không ghi đè cho đến khi đã xem thay đổi.
Ghi đè có thể làm mất custom dùng chung của project.

## 5. Tùy chỉnh component

### Tùy chỉnh cho một lần sử dụng

Truyền Tailwind class qua `className`:

```tsx
<Button className="w-full sm:w-auto">Lưu thay đổi</Button>
<DialogContent className="sm:max-w-2xl">...</DialogContent>
```

### Tạo component kết hợp

Nếu nhiều page lặp lại cùng một cấu trúc, tạo component trong
`src/common/components`, rồi kết hợp các primitive UI:

```tsx
export function EmptyState({ message }: { message: string }) {
  return (
    <Card>
      <CardContent className="py-10 text-center text-muted-foreground">{message}</CardContent>
    </Card>
  )
}
```

### Sửa file trong `ui`

Chỉ sửa `src/common/components/ui/*` khi thay đổi phải áp dụng cho toàn project.
Style riêng của một page nên truyền qua `className`, không sửa primitive để phục vụ
một trường hợp duy nhất.

## 6. Theme và màu sắc

Dùng token thay vì màu cố định để sáng/tối hoạt động đúng:

```text
bg-background
text-foreground
bg-card
text-muted-foreground
border-border
text-destructive
```

Tránh dùng `bg-white`, `text-black` hoặc màu hex trong page nếu không có lý do thiết
kế cụ thể.

## 7. Quy tắc accessibility

- Button chỉ có icon phải có `aria-label`.
- Input phải có Label hoặc `aria-label`.
- Không dùng `<div onClick>` thay cho Button.
- Dialog phải có `DialogTitle` và `DialogDescription`.
- Giữ trạng thái `disabled` khi đang submit để tránh gửi nhiều lần.
- Không loại bỏ focus outline của component.

## Checklist

- Component cần dùng đã có trong `ui` chưa?
- Đây là primitive dùng chung hay component riêng của feature?
- `className` có sử dụng theme token không?
- Form, icon button và Dialog có đủ thông tin accessibility không?
- Đã kiểm tra light mode, dark mode và mobile chưa?
