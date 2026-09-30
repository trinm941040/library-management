# Phân tích chi tiết Frontend và các thư viện

> Phạm vi: kiến trúc frontend hiện tại, vai trò của Zod, TanStack Query và các thư viện chính, kèm ví dụ lấy từ source code.

## 1. Tổng quan frontend

Frontend là một SPA sử dụng:

```text
React 19
TypeScript 6
Vite 8
React Router 7
Tailwind CSS 4
Zod 4
TanStack React Query 5
React Hook Form 7
Radix UI
SignalR client
```

Entry point:

```text
Frontend/src/main.tsx
  -> AppProviders
     -> App
        -> AppRoutes
```

Provider tree hiện tại:

```text
AppErrorBoundary
  -> QueryClientProvider
     -> GlobalLoadingProvider
        -> BrowserRouter
           -> SettingsProvider
              -> ToastProvider
                 -> AuthProvider
                    -> NotificationRealtimeProvider
                       -> AppRoutes
```

File thể hiện composition này:

```text
Frontend/src/app/AppProviders.tsx
```

## 2. Cách tổ chức source code

```text
Frontend/src/
├── app/                 composition, navigation, error boundary
├── auth/                session state và HTTP client wrapper
├── common/components/   component tái sử dụng
│   ├── atoms/
│   ├── molecules/
│   ├── organisms/
│   └── ui/              wrapper trên Radix/native controls
├── layouts/             AppLayout, Header, Sidebar
├── pages/               feature/page theo business module
├── routes/              route table và route guard
├── search/              global navigation search
├── settings/            UI preferences context
├── shared/              auth helpers, URL/table state, loading
├── styles/              layout CSS
└── utils/               helper dùng chung
```

Mỗi feature thường có dạng:

```text
pages/books/
├── BooksPage.tsx              page state và orchestration
├── CatalogDetailPage.tsx
├── book-api.ts                API contracts/functions
└── components/                dialog và feature components
```

Đây là cấu trúc **feature-oriented kết hợp shared component layers**. Page và API của cùng nghiệp vụ nằm gần nhau; component dùng chung được đẩy vào `common`.

## 3. Luồng từ UI đến backend

Ví dụ tải danh sách sách:

```text
BooksPage
  -> useEffect
  -> getBooks(filters, AbortSignal)
  -> authenticatedFetch(...)
  -> gắn Authorization: Bearer <access token>
  -> native fetch
  -> readResponse(response, bookPageSchema)
  -> Zod kiểm tra JSON runtime
  -> setPage(response)
  -> DataTable render rows
```

Các file chính:

```text
Frontend/src/pages/books/BooksPage.tsx
Frontend/src/pages/books/book-api.ts
Frontend/src/auth/auth-api.ts
Frontend/src/common/components/organisms/DataTable.tsx
```

Project **không dùng Axios**. Toàn bộ request đi qua native `fetch`, chủ yếu bằng:

```text
authenticatedFetch()  request cần JWT
publicFetch()         request công khai
readResponse()        parse success/Problem Details
```

## 4. Zod làm gì?

Zod là thư viện định nghĩa schema và validation chạy ở runtime. TypeScript chỉ kiểm tra khi compile; nó không thể bảo đảm JSON backend trả về lúc chạy đúng shape. Zod lấp khoảng trống này.

Project đang dùng Zod cho bốn mục đích chính.

### 4.1 Validate API response

Ví dụ tại:

```text
Frontend/src/pages/books/book-api.ts
```

```ts
const bookSchema = z.object({
  id: guidSchema,
  title: z.string(),
  isbn: z.string(),
  status: z.enum(['Active', 'Inactive']),
  availableCopyCount: z.number().int().nonnegative().nullish(),
  concurrencyToken: guidSchema,
})

const bookPageSchema = z.object({
  items: z.array(bookSchema),
  pageNumber: z.number().int().positive(),
  pageSize: z.number().int().positive().max(100),
  totalCount: z.number().int().nonnegative(),
  totalPages: z.number().int().nonnegative(),
})
```

Response được kiểm tra tại runtime:

```ts
return readResponse(
  await authenticatedFetch('/api/v1/books?...'),
  bookPageSchema,
)
```

Trong `auth-api.ts`:

```ts
const parsed = schema.safeParse(data)
if (!parsed.success)
  throw new ApiError('Phản hồi máy chủ không hợp lệ.', 502)
return parsed.data
```

Nếu backend vô tình đổi `totalCount` thành chuỗi, thiếu `items`, trả GUID sai hoặc trả status ngoài enum, frontend không âm thầm sử dụng dữ liệu lỗi mà tạo `ApiError` 502 phía client.

### 4.2 Sinh TypeScript type từ schema

```ts
export type LibraryBook = z.infer<typeof bookSchema>
export type BookPageResponse = z.infer<typeof bookPageSchema>
```

Lợi ích:

- schema runtime và TypeScript type dùng cùng một nguồn;
- tránh viết một interface và một validator riêng rồi để chúng lệch nhau;
- IDE autocomplete đúng theo response đã validate.

### 4.3 Validate form

Ví dụ profile:

```text
Frontend/src/pages/profile/profile-api.ts
Frontend/src/pages/profile/ProfilePage.tsx
```

```ts
export const profileFormSchema = z.object({
  fullName: z.string().trim().min(2).max(150),
  phoneNumber: z.string().trim().max(30).refine(...),
  dateOfBirth: z.string()
    .refine(..., 'Ngày sinh không hợp lệ.')
    .refine(..., 'Ngày sinh không được ở tương lai.'),
  address: z.string().trim().max(500),
  rowVersion: guidSchema,
})
```

Schema được nối với React Hook Form:

```ts
const profileForm = useForm<ProfileFormValues>({
  resolver: zodResolver(profileFormSchema),
  values: ...,
})
```

Khi submit:

```text
input
  -> React Hook Form thu thập values
  -> zodResolver chạy profileFormSchema
  -> lỗi gắn vào formState.errors
  -> hợp lệ mới gọi updateProfile()
```

Ví dụ validate hai mật khẩu trùng nhau:

```ts
const passwordFormSchema = z.object({
  currentPassword: z.string().min(1),
  newPassword: z.string().min(8),
  confirmPassword: z.string().min(1),
}).refine(
  value => value.newPassword === value.confirmPassword,
  { path: ['confirmPassword'], message: 'Mật khẩu xác nhận không khớp.' },
)
```

### 4.4 Validate environment và URL state

Environment:

```ts
const environmentSchema = z.object({
  VITE_API_BASE_URL: z.string().trim().url().or(z.literal('')).default(''),
  VITE_API_TIMEOUT_MS: z.coerce.number().int().positive().default(15000),
})

const environment = environmentSchema.parse(import.meta.env)
```

Nếu timeout/env sai, app fail sớm thay vì tạo lỗi request khó chẩn đoán.

URL table state:

```text
Frontend/src/shared/data/table-contracts.ts
```

```ts
const tableUrlStateSchema = z.object({
  search: z.string().trim().max(200).default(''),
  pageSize: z.coerce.number()
    .pipe(z.union([z.literal(10), z.literal(20), z.literal(50), z.literal(100)]))
    .catch(20),
  sortDirection: z.enum(['asc', 'desc']),
})
```

URL như `?pageSize=abc` được coerce/fallback về giá trị an toàn thay vì làm hỏng page.

### 4.5 Zod đang được dùng ở đâu?

Zod xuất hiện trong 19 source files, nổi bật:

```text
auth/auth-api.ts
shared/data/table-contracts.ts
pages/books/book-api.ts
pages/copies/copy-api.ts
pages/members/member-api.ts
pages/employee/employee-api.ts
pages/access-accounts/access-account-api.ts
pages/inventory-audits/inventory-audit-api.ts
pages/stock-receipts/receipt-api.ts
pages/roles/role-permission-api.ts
pages/settings/settings-api.ts
pages/profile/profile-api.ts
pages/kiosk/kiosk-api.ts
```

### 4.6 Điểm hạn chế hiện tại của Zod

Zod chưa được áp dụng nhất quán. Một số module như:

```text
borrowing-api.ts
dashboard-api.ts
notifications-api.ts
reports-api.ts
reservation-api.ts
user-api.ts
violation-api.ts
payment-api.ts
```

chủ yếu dùng TypeScript type assertion:

```ts
return response.json() as Promise<BorrowingPageResponse>
```

Đoạn này không validate runtime. Nếu backend đổi contract, TypeScript vẫn tin dữ liệu và lỗi chỉ xuất hiện muộn trong UI.

Kết luận về Zod:

```text
Zod được sử dụng rộng nhưng chưa toàn diện.
Nó mạnh nhất ở auth, catalog, inventory và profile.
Các API module còn lại vẫn có contract safety không đồng đều.
```

## 5. TanStack React Query làm gì?

TanStack Query quản lý **server state**:

- gọi query và giữ kết quả trong cache;
- cung cấp `isLoading`, `error`, `refetch`;
- deduplicate request cùng `queryKey`;
- tự hủy query bằng `AbortSignal`;
- invalidate/refetch sau mutation;
- cập nhật cache trực tiếp;
- polling/refetch on focus.

Nó không thay thế toàn bộ application state. Theme, dialog đang mở hoặc input form vẫn nên là local/context state.

### 5.1 Cấu hình QueryClient

Tại `Frontend/src/app/AppProviders.tsx`:

```ts
new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
    mutations: { retry: 0 },
  },
})
```

Ý nghĩa:

- query được coi là fresh trong 30 giây;
- query lỗi được retry một lần;
- mặc định không refetch khi focus window;
- mutation không tự retry để tránh tạo/cập nhật dữ liệu hai lần.

### 5.2 Query key

Tại `notification-queries.ts`:

```ts
export const notificationKeys = {
  all: ['notifications'],
  unread: ['notifications', 'unread-count'],
  lists: ['notifications', 'my'],
  list: params => ['notifications', 'my', params],
}
```

Query key là định danh của cache. Hai request có cùng key dùng chung cached server state.

### 5.3 `useQuery` — đọc dữ liệu

```ts
export function useUnreadNotificationCount() {
  return useQuery({
    queryKey: notificationKeys.unread,
    queryFn: ({ signal }) => fetchUnreadCount(signal),
    refetchInterval: 30_000,
    refetchOnWindowFocus: true,
  })
}
```

Luồng:

```text
Component mount
  -> TanStack kiểm tra cache theo queryKey
  -> thiếu/stale thì gọi fetchUnreadCount
  -> truyền AbortSignal
  -> cache số unread
  -> rerender component
  -> tự poll lại mỗi 30 giây
```

### 5.4 `useMutation` — thay đổi dữ liệu

```ts
return useMutation({
  mutationFn: id => markNotificationRead(id),
  onSuccess: (_, id) => {
    client.setQueriesData(... cập nhật item isRead ...)
    client.invalidateQueries({ queryKey: notificationKeys.unread })
  },
})
```

Sau khi đánh dấu đã đọc:

- `setQueriesData` cập nhật ngay các notification list đã cache;
- `invalidateQueries` đánh dấu unread count là stale và tải lại;
- UI không cần tự giữ thêm `reloadKey`.

Mark-all dùng:

```ts
client.setQueryData(notificationKeys.unread, 0)
```

để số unread về 0 ngay lập tức.

### 5.5 TanStack Query kết hợp SignalR

Tại `NotificationRealtimeProvider.tsx`:

```ts
connection.on('NotificationReceived', notification => {
  if (!notification.isRead)
    client.setQueryData<number>(notificationKeys.unread, current => (current ?? 0) + 1)
  client.invalidateQueries({ queryKey: notificationKeys.lists })
})
```

SignalR nhận event realtime, còn TanStack Query chịu trách nhiệm đồng bộ cache/UI.

```text
Backend SignalR event
  -> NotificationRealtimeProvider
  -> tăng cached unread count
  -> invalidate cached inbox lists
  -> component dùng query tự rerender/refetch
```

### 5.6 Xóa cache khi quyền/session đổi

Trong `AuthProvider`:

```ts
if (authorizationKeyChanged) {
  client.cancelQueries()
  client.clear()
}
```

Điều này ngăn dữ liệu của user/quyền cũ tồn tại trong cache sau logout hoặc sau khi role/permission đổi.

### 5.7 Mức độ áp dụng TanStack hiện tại

TanStack Query chỉ xuất hiện trực tiếp trong bốn file:

```text
app/AppProviders.tsx
auth/AuthProvider.tsx
pages/notifications/notification-queries.ts
pages/notifications/NotificationRealtimeProvider.tsx
```

Nghĩa là **TanStack Query hiện chỉ quản lý server state của notification/inbox**, chưa phải data layer chung của frontend.

Ví dụ Books vẫn làm thủ công:

```ts
const [page, setPage] = useState(null)
const [isLoading, setIsLoading] = useState(true)
const [reloadKey, setReloadKey] = useState(0)

useEffect(() => {
  const controller = new AbortController()
  getBooks(..., controller.signal).then(setPage)
  return () => controller.abort()
}, [filters, reloadKey])
```

Cách này chạy được nhưng phải tự quản lý:

- loading/error/data;
- cancellation;
- retry/refetch;
- stale data;
- cache;
- invalidation sau mutation.

Kết luận về TanStack:

```text
Project đã cài và dùng đúng TanStack Query,
nhưng phạm vi chỉ là notification.
Phần lớn page vẫn dùng manual server-state management.
```

## 6. React Hook Form và `@hookform/resolvers`

React Hook Form giúp quản lý form state mà không cần tạo một `useState` cho từng field:

- register input;
- dirty/touched state;
- error theo field;
- submit state;
- reset form;
- giảm rerender.

`@hookform/resolvers/zod` là cầu nối giữa React Hook Form và Zod.

Hiện chỉ hai page dùng React Hook Form:

```text
Frontend/src/pages/profile/ProfilePage.tsx
Frontend/src/pages/profile/ChangePasswordPage.tsx
```

Ví dụ:

```ts
const form = useForm<PasswordFormValues>({
  resolver: zodResolver(passwordFormSchema),
  defaultValues: {
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  },
})

const submit = form.handleSubmit(async values => {
  await changePassword(values)
})
```

Nhiều form khác vẫn dùng `useState` và validation thủ công, vì vậy form architecture cũng chưa thống nhất.

## 7. React Router làm gì?

Files:

```text
Frontend/src/routes/AppRoutes.tsx
Frontend/src/routes/ProtectedRoute.tsx
Frontend/src/app/navigation.ts
```

Vai trò:

- map URL → page;
- nested route trong `AppLayout`;
- redirect alias;
- route params/query params;
- giữ intended destination khi bị đưa về login;
- lazy-load page bundle;
- route-level authentication/permission guard.

Ví dụ lazy loading:

```ts
const BooksPage = lazy(() =>
  import('../pages/books/BooksPage')
    .then(module => ({ default: module.BooksPage })),
)
```

Vite chỉ tải chunk Books khi route cần nó.

Ví dụ bảo vệ route:

```text
ProtectedRoute
  -> auth loading: hiển thị loading state
  -> unauthenticated: redirect /login và lưu URL cũ
  -> thiếu permission: ForbiddenPage
  -> hợp lệ: render page
```

`PermissionBoundary` còn ẩn button/action trong page:

```tsx
<PermissionBoundary requiredPermissions={['books.create']}>
  <Button>Thêm sách</Button>
</PermissionBoundary>
```

Đây chỉ là UX enforcement; backend authorization vẫn là lớp bảo vệ thực sự.

## 8. Authentication state ở frontend

Files:

```text
Frontend/src/auth/auth-api.ts
Frontend/src/auth/AuthProvider.tsx
```

### Access token

Access token chỉ nằm trong biến module:

```ts
let accessToken: string | null = null
```

Nó không được ghi vào `localStorage` hoặc `sessionStorage`, giúp giảm khả năng token bị lấy từ persistent browser storage.

### Refresh token

Frontend không đọc được raw refresh token. Browser tự gửi HttpOnly cookie với:

```ts
credentials: 'include'
```

### Auto refresh khi API trả 401

```text
authenticatedFetch
  -> gửi Bearer token
  -> nếu 401
     -> refreshAccessToken()
     -> retry request đúng một lần
  -> vẫn 401
     -> clearLocalSession()
```

`refreshPromise` deduplicate nhiều request cùng lúc cần refresh. `navigator.locks` tuần tự hóa rotation giữa các tab dùng chung cookie.

### Đồng bộ authorization

`AuthProvider` tải lại profile:

- mỗi 30 giây;
- khi window focus;
- khi tab visible;
- sau response `403`;
- qua `BroadcastChannel` khi tab khác báo authorization changed/logout.

## 9. SignalR client

Thư viện:

```text
@microsoft/signalr
```

File:

```text
Frontend/src/pages/notifications/NotificationRealtimeProvider.tsx
```

```ts
new HubConnectionBuilder()
  .withUrl('/api/v1/notifications/hub', {
    accessTokenFactory: getRealtimeAccessToken,
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
```

Nếu SignalR không kết nối được, polling unread count của TanStack Query vẫn là fallback.

## 10. Radix UI

`radix-ui` cung cấp headless accessible primitives:

- Dialog;
- Select;
- Tabs;
- Radio Group;
- Switch;
- Slot/composition.

Các wrapper nằm tại:

```text
Frontend/src/common/components/ui/dialog.tsx
Frontend/src/common/components/ui/select.tsx
Frontend/src/common/components/ui/tabs.tsx
Frontend/src/common/components/ui/radio-group.tsx
Frontend/src/common/components/ui/switch.tsx
```

Radix xử lý keyboard navigation, focus trapping, portal, ARIA và open/close state. Project bổ sung Tailwind styling và layout behavior.

Ví dụ `Button asChild` dùng `Slot.Root` để truyền behavior/style của Button sang một `Link` mà không tạo button lồng link.

## 11. Tailwind CSS

Project dùng Tailwind CSS 4 qua:

```text
@tailwindcss/vite
@import 'tailwindcss';
```

File chính:

```text
Frontend/src/index.css
Frontend/src/styles/app-shell.css
```

`index.css` định nghĩa design tokens bằng CSS variables:

```text
--background
--foreground
--primary
--destructive
--border
--radius
```

Dark mode dùng:

```css
@custom-variant dark (&:where([data-theme='dark'], [data-theme='dark'] *));
```

`SettingsProvider` đặt `data-theme` trên `<html>` và lưu preference trong `localStorage`.

## 12. `class-variance-authority`, `clsx` và `tailwind-merge`

### CVA

`class-variance-authority` định nghĩa variant có type cho component:

```ts
const buttonVariants = cva(baseClasses, {
  variants: {
    variant: {
      default: '...',
      destructive: '...',
      outline: '...',
      ghost: '...',
    },
    size: {
      sm: '...',
      lg: '...',
      icon: '...',
    },
  },
})
```

Caller dùng:

```tsx
<Button variant="destructive" size="sm">Xóa</Button>
```

### clsx

`clsx` ghép class conditionally:

```ts
clsx('base', loading && 'opacity-50', className)
```

### tailwind-merge

`tailwind-merge` giải quyết class Tailwind xung đột, ví dụ `px-2` và `px-4`, giữ class có precedence đúng.

Project gộp hai thư viện thành:

```ts
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
```

File:

```text
Frontend/src/utils/cn.ts
```

## 13. Lucide React

`lucide-react` cung cấp SVG icon dạng React component:

```tsx
import { Search, Plus, Trash2 } from 'lucide-react'

<Search aria-hidden="true" />
```

Nó được dùng rộng khắp navigation, button, table state, dashboard và dialog. Vì import theo tên, bundler có thể tree-shake icon không dùng.

## 14. React Context và local state

Project không dùng Redux hoặc Zustand. Client state được chia thành:

| Loại state | Cơ chế |
|---|---|
| User/session/permission | `AuthProvider` context |
| Theme/sidebar/AI preference | `SettingsProvider` context + localStorage |
| Toast | `ToastProvider` context |
| Global request spinner | `GlobalLoadingProvider` + browser events |
| Notification server cache | TanStack Query |
| Form profile/password | React Hook Form |
| Page/dialog/filter state | `useState`, `useEffect`, URL search params |

`GlobalLoadingProvider` đếm request foreground. Nó chờ 120 ms trước khi hiện spinner để tránh flicker với request rất nhanh.

## 15. Component system

Các component dùng lại quan trọng:

- `DataTable`: sort, select rows, bulk actions, loading/error/empty state, pagination và mobile list layout.
- `PageShell`: layout tiêu đề/action.
- `ScreenState` và `LoadingBoundary`: loading/error/empty/forbidden presentation.
- `ConfirmDialog`: xác nhận hành động nguy hiểm.
- `ImportPreviewDialog`: preview CSV/import errors.
- `StatusBadge`: hiển thị trạng thái nhất quán.
- `ToastProvider`: feedback thành công/lỗi.

`DataTable<Row>` dùng generic TypeScript nên một component phục vụ books, users, copies... mà vẫn giữ type của row.

## 16. Vite, TypeScript, ESLint và Prettier

### Vite

- dev server;
- build bundle;
- xử lý `import.meta.env`;
- code splitting từ `React.lazy`;
- tích hợp Tailwind plugin.

### TypeScript

- compile-time types cho props, DTO và generic components;
- không thay thế runtime validation, do đó Zod vẫn cần thiết ở boundary.

### ESLint

- kiểm tra lỗi code/static rules;
- React Hooks rules;
- accessibility rules qua `eslint-plugin-jsx-a11y`.

### Prettier

- format code thống nhất;
- không kiểm tra business correctness.

## 17. Đánh giá điểm mạnh

1. Auth/session design tốt: access token chỉ ở memory, refresh cookie HttpOnly, refresh deduplication và cross-tab lock.
2. API wrapper xử lý timeout, abort, Problem Details, global loading và auto-refresh tập trung.
3. Zod bảo vệ nhiều API boundary quan trọng.
4. Route được lazy-load và có auth/permission guard.
5. UI primitives tái sử dụng, có accessibility foundation từ Radix.
6. Notification kết hợp SignalR và TanStack Query cache hợp lý.
7. URL giữ filter/page/sort giúp reload/share URL không mất context.
8. Responsive table có mobile behavior riêng thay vì chỉ thu nhỏ desktop table.

## 18. Điểm chưa nhất quán và rủi ro

### 18.1 Hai cách quản lý server state

Notification dùng TanStack Query, còn đa số page dùng manual `useEffect/useState/reloadKey`. Điều này làm:

- pattern loading/error/refetch khác nhau;
- code page dài;
- không chia sẻ cache;
- dễ request trùng;
- invalidation sau mutation phải làm thủ công.

### 18.2 Hai cách xử lý API response

Một số module dùng central:

```text
auth-api.readResponse + Zod schema
```

Một số module tự định nghĩa `ApiError` và `readResponse<T>` rồi type-cast JSON. Kết quả là behavior/message và contract safety không đồng đều.

### 18.3 Hai cách quản lý form

Profile/password dùng React Hook Form + Zod. Nhiều dialog khác dùng local state + validation thủ công. Khi form lớn, cách thủ công dễ sinh boilerplate và lỗi mapping server field errors.

### 18.4 Frontend permission chỉ là UX

`ProtectedRoute` và `PermissionBoundary` giúp ẩn UI, nhưng không phải security boundary. API backend bắt buộc tiếp tục kiểm tra policy như hiện tại.

## 19. Hướng chuẩn hóa phù hợp

Không cần rewrite toàn frontend. Có thể chuyển dần theo feature:

```text
1. Mỗi API response có Zod schema.
2. Dùng chung auth-api.readResponse và ApiError.
3. Mỗi module có queryKeys + query hooks + mutation hooks.
4. Mutation thành công invalidate đúng key thay vì reloadKey.
5. Form mới dùng React Hook Form + Zod khi có nhiều field/rule.
6. Giữ useState cho pure UI state: dialog, selected tab, local toggle.
```

Ví dụ Books nếu chuyển sang TanStack:

```ts
const booksQuery = useQuery({
  queryKey: ['books', filters],
  queryFn: ({ signal }) => getBooks(filters, signal),
})

const deleteMutation = useMutation({
  mutationFn: deleteBook,
  onSuccess: () => queryClient.invalidateQueries({ queryKey: ['books'] }),
})
```

Khi đó có thể loại phần lớn `page`, `isLoading`, `pageError`, `reloadKey` và effect fetch thủ công.

## 20. Kết luận

```text
Zod:
Runtime contract validation + type inference + form/env/URL validation.
Được dùng khá rộng nhưng chưa phủ toàn bộ API.

TanStack Query:
Server-state cache, query/mutation, polling và invalidation.
Hiện chỉ áp dụng đáng kể cho notification/inbox.

React Hook Form:
Form state + Zod integration.
Hiện chỉ dùng ở profile và change-password.

React Router:
Routing, lazy loading, auth/permission guard.

Radix + Tailwind + CVA + clsx/tailwind-merge:
Nền component accessible, styling và typed variants.

SignalR:
Realtime notifications, với TanStack polling làm fallback.
```

Frontend có nền tảng tốt nhưng đang ở trạng thái chuyển tiếp: các feature mới như notification/profile dùng library abstraction đầy đủ hơn, còn nhiều feature cũ vẫn dùng manual fetch/form state. Điểm cải tiến có giá trị nhất là chuẩn hóa API boundary bằng Zod và chuyển server state từng module sang TanStack Query.
