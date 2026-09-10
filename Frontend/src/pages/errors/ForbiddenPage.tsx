import { Link } from 'react-router-dom'
export function ForbiddenPage() { return <main className="p-8 text-center"><h1 className="text-2xl font-semibold">Không có quyền truy cập</h1><p className="my-3">Tài khoản của bạn không được cấp quyền cho trang này.</p><Link className="underline" to="/dashboard">Về trang tổng quan</Link></main> }
