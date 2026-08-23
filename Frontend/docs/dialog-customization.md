# How to customize Dialog content

Dialog của project nằm tại `src/common/components/ui/dialog.tsx`. Page nên kết hợp
các phần có sẵn thay vì tự tạo overlay và modal mới.

## 1. Cấu trúc chuẩn

```tsx
import { Button } from '@/common/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/common/components/ui/dialog'

export function BookDetailsDialog() {
  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button variant="outline">Xem chi tiết</Button>
      </DialogTrigger>

      <DialogContent>
        <DialogHeader>
          <DialogTitle>Thông tin sách</DialogTitle>
          <DialogDescription>Xem thông tin chi tiết của đầu sách.</DialogDescription>
        </DialogHeader>

        <div className="grid gap-3 py-4">Nội dung tùy chỉnh đặt ở đây.</div>

        <DialogFooter showCloseButton />
      </DialogContent>
    </Dialog>
  )
}
```

Vai trò từng phần:

- `Dialog`: quản lý trạng thái đóng/mở.
- `DialogTrigger`: phần tử mở dialog.
- `DialogContent`: khung nội dung và overlay.
- `DialogHeader`: nhóm tiêu đề và mô tả.
- `DialogTitle`: tiêu đề bắt buộc cho accessibility.
- `DialogDescription`: giải thích ngắn mục đích dialog.
- `DialogFooter`: khu vực các nút hành động.

## 2. Dialog được điều khiển bởi page

Dùng controlled dialog khi page cần mở dialog từ Table, cần biết item đang sửa hoặc
cần đóng dialog sau khi API thành công.

```tsx
const [editingBook, setEditingBook] = useState<Book | null>(null)

return (
  <>
    <Button onClick={() => setEditingBook(book)}>Chỉnh sửa</Button>

    <Dialog open={editingBook !== null} onOpenChange={(open) => !open && setEditingBook(null)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Chỉnh sửa sách</DialogTitle>
          <DialogDescription>Cập nhật thông tin của {editingBook?.title}.</DialogDescription>
        </DialogHeader>

        <div>Nội dung form</div>
      </DialogContent>
    </Dialog>
  </>
)
```

Quy ước props cho một dialog tách thành component riêng:

```ts
type BookFormDialogProps = {
  open: boolean
  book: Book | null
  onOpenChange: (open: boolean) => void
  onSave: (data: BookFormData) => Promise<void>
}
```

## 3. Custom nội dung bằng form

Đặt `<form>` bên trong `DialogContent`. Nút lưu dùng `type="submit"`, nút hủy dùng
`type="button"` để không submit nhầm.

```tsx
<Dialog open={open} onOpenChange={onOpenChange}>
  <DialogContent className="sm:max-w-xl">
    <form onSubmit={handleSubmit}>
      <DialogHeader>
        <DialogTitle>Thêm sách</DialogTitle>
        <DialogDescription>Nhập thông tin để tạo một đầu sách mới.</DialogDescription>
      </DialogHeader>

      <div className="grid gap-5 py-6">
        <div className="grid gap-2">
          <Label htmlFor="book-title">Tên sách</Label>
          <Input
            id="book-title"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            required
          />
        </div>

        {error ? (
          <p role="alert" className="text-sm text-destructive">
            {error}
          </p>
        ) : null}
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
          Hủy
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Đang lưu...' : 'Lưu'}
        </Button>
      </DialogFooter>
    </form>
  </DialogContent>
</Dialog>
```

Chỉ đóng dialog sau khi lưu thành công. Nếu API lỗi, giữ dialog mở và hiển thị lỗi
ngay trong form.

## 4. Dialog xác nhận

```tsx
<Dialog open={userToDelete !== null} onOpenChange={(open) => !open && setUserToDelete(null)}>
  <DialogContent>
    <DialogHeader>
      <DialogTitle>Xóa user?</DialogTitle>
      <DialogDescription>
        Tài khoản {userToDelete?.name} sẽ bị xóa. Thao tác này không thể hoàn tác.
      </DialogDescription>
    </DialogHeader>

    <DialogFooter>
      <Button variant="outline" onClick={() => setUserToDelete(null)}>
        Hủy
      </Button>
      <Button variant="destructive" disabled={isDeleting} onClick={handleDelete}>
        {isDeleting ? 'Đang xóa...' : 'Xóa user'}
      </Button>
    </DialogFooter>
  </DialogContent>
</Dialog>
```

Nút xác nhận hành động nguy hiểm phải dùng `variant="destructive"` và mô tả rõ đối
tượng bị ảnh hưởng.

## 5. Thay đổi kích thước và scroll

Dialog mặc định có `sm:max-w-lg`. Ghi đè bằng `className`:

```tsx
<DialogContent className="sm:max-w-2xl">...</DialogContent>
```

Với nội dung dài, giới hạn chiều cao và chỉ scroll phần giữa:

```tsx
<DialogContent className="max-h-[90vh] sm:max-w-2xl">
  <DialogHeader>...</DialogHeader>
  <div className="overflow-y-auto py-4">Nội dung dài</div>
  <DialogFooter>...</DialogFooter>
</DialogContent>
```

Không dùng kích thước cố định quá lớn vì dialog phải hoạt động trên mobile.

## 6. Tùy chỉnh nút đóng

Mặc định `DialogContent` có nút `X`. Có thể ẩn khi cần:

```tsx
<DialogContent showCloseButton={false}>...</DialogContent>
```

Chỉ ẩn khi dialog đã có cách đóng rõ ràng khác. Có thể dùng `DialogClose` để một nút
tự đóng dialog:

```tsx
<DialogClose asChild>
  <Button variant="outline">Đóng</Button>
</DialogClose>
```

Không dùng `DialogClose` cho nút lưu async vì dialog sẽ đóng trước khi biết API thành
công hay thất bại. Với API, dùng controlled dialog và tự gọi `onOpenChange(false)` sau
khi request thành công.

## 7. Nên đặt custom Dialog ở đâu?

- Chỉ một page dùng: `src/pages/<feature>/components`.
- Nhiều feature dùng thật sự: `src/common/components`.
- Không thêm nội dung nghiệp vụ trực tiếp vào `src/common/components/ui/dialog.tsx`.

Ví dụ hiện có:

```text
src/pages/users/components/UserFormDialog.tsx
```

## Checklist Dialog

- Có `DialogTitle` và `DialogDescription` không?
- Label có liên kết đúng với Input không?
- Nút hủy trong form có `type="button"` không?
- Có disable nút khi request đang chạy không?
- API lỗi có giữ dialog mở và hiển thị lỗi không?
- Nội dung dài có scroll và dùng được trên mobile không?
- Hành động nguy hiểm có xác nhận và dùng màu destructive không?
