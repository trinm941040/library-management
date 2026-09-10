import { Link } from 'react-router-dom'
export function NotFoundPage() { return <main className="p-8 text-center"><h1 className="text-2xl font-semibold">Không tìm thấy trang</h1><Link className="underline" to="/dashboard">Về trang tổng quan</Link></main> }
