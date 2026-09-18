import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { LockKeyhole, Mail, ShieldCheck } from 'lucide-react'
import { useAuth } from '@/auth/AuthProvider'
import { loginSchema } from '@/auth/auth-api'
import { safeIntendedDestination } from '@/shared/auth/intended-destination'
import { BrandLogo } from '@/common/components/BrandLogo'
import { Button } from '@/common/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/common/components/ui/card'
import { Input } from '@/common/components/ui/input'
import { Label } from '@/common/components/ui/label'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (isSubmitting) return
    const parsed = loginSchema.safeParse({ email, password })
    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message ?? 'Thông tin đăng nhập không hợp lệ.')
      return
    }
    setError('')
    setIsSubmitting(true)

    try {
      await login(parsed.data.email, parsed.data.password)
      setPassword('')
      navigate(safeIntendedDestination(location.state?.from), { replace: true })
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : 'Đăng nhập không thành công.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="relative min-h-screen overflow-hidden bg-slate-950">
      <img
        src="/assets/bg.jpg"
        alt=""
        aria-hidden="true"
        className="absolute inset-0 h-full w-full object-cover object-center"
      />
      <div className="absolute inset-0 bg-[linear-gradient(90deg,rgba(5,15,30,0.92)_0%,rgba(8,22,38,0.72)_44%,rgba(8,16,28,0.48)_100%)]" />
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_76%_50%,transparent_0%,rgba(2,8,18,0.24)_75%)]" />

      <div className="relative z-10 mx-auto grid min-h-screen w-full max-w-7xl items-center gap-12 px-5 py-10 md:px-10 lg:grid-cols-[1.15fr_0.85fr] lg:px-14">
        <section className="hidden max-w-xl text-white lg:block">
          <div className="mb-8 inline-flex items-center gap-3 rounded-full border border-white/20 bg-white/10 px-4 py-2 text-sm font-medium shadow-lg backdrop-blur-md">
            <BrandLogo variant="app-icon" alt="" className="size-5" />
            Hệ thống quản lý thư viện
          </div>
          <h1 className="text-5xl leading-[1.08] font-semibold tracking-tight xl:text-6xl">
            Kết nối tri thức,
            <span className="mt-2 block text-amber-300">vận hành hiệu quả.</span>
          </h1>
          <p className="mt-6 max-w-lg text-lg leading-8 text-slate-200">
            Quản lý kho sách, độc giả và hoạt động lưu thông tập trung trong một không gian an toàn,
            trực quan.
          </p>
          <div className="mt-10 flex items-center gap-3 text-sm text-slate-200">
            <span className="grid size-10 place-items-center rounded-full border border-white/20 bg-white/10 backdrop-blur">
              <ShieldCheck className="size-5 text-emerald-300" />
            </span>
            Truy cập an toàn dành cho nhân viên thư viện
          </div>
        </section>

        <div className="mx-auto w-full max-w-md">
          <Card className="overflow-hidden border-white/40 bg-white/92 shadow-[0_28px_80px_rgba(0,0,0,0.38)] backdrop-blur-xl dark:border-white/10 dark:bg-slate-950/90">
            <CardHeader className="space-y-4 px-6 pt-7 text-center sm:px-9 sm:pt-9">
              <BrandLogo
                variant="login"
                tone="auto"
                className="mx-auto h-auto w-56 max-w-full sm:w-64"
              />
              <div className="space-y-2">
                <CardTitle className="text-3xl tracking-tight">Đăng nhập</CardTitle>
                <p className="text-sm text-muted-foreground">
                  Nhập thông tin tài khoản để tiếp tục.
                </p>
              </div>
            </CardHeader>
            <CardContent className="px-6 pb-7 sm:px-9 sm:pb-9">
              {(location.state as { notice?: string } | null)?.notice ? (
                <p
                  className="mb-5 rounded-md border border-green-600/20 bg-green-600/10 p-3 text-sm text-green-700"
                  role="status"
                >
                  {(location.state as { notice?: string }).notice}
                </p>
              ) : null}
              <form className="grid gap-6" onSubmit={handleSubmit}>
                <div className="grid gap-2.5">
                  <Label htmlFor="email">Email</Label>
                  <div className="relative">
                    <Mail className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      className="h-11 pl-10"
                      type="email"
                      id="email"
                      name="email"
                      placeholder="ban@example.com"
                      required
                      autoComplete="email"
                      value={email}
                      onChange={(event) => setEmail(event.target.value)}
                    />
                  </div>
                </div>

                <div className="grid gap-2.5">
                  <Label htmlFor="password">Mật khẩu</Label>
                  <div className="relative">
                    <LockKeyhole className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      className="h-11 pl-10"
                      type="password"
                      id="password"
                      name="password"
                      placeholder="••••••••"
                      required
                      autoComplete="current-password"
                      value={password}
                      onChange={(event) => setPassword(event.target.value)}
                    />
                  </div>
                </div>

                {error ? (
                  <p role="alert" className="text-sm text-destructive">
                    {error}
                  </p>
                ) : null}

                <Button
                  type="submit"
                  loading={isSubmitting}
                  loadingLabel="Đang đăng nhập"
                  className="h-11 w-full font-semibold shadow-md shadow-primary/20"
                >
                  Đăng nhập
                </Button>
              </form>
              <p className="mt-6 text-center text-xs leading-5 text-muted-foreground">
                Việc truy cập hệ thống được ghi nhận nhằm bảo đảm an toàn dữ liệu.
              </p>
            </CardContent>
          </Card>
          <p className="mt-5 text-center text-xs text-white/70 lg:hidden">
            Hệ thống Quản lý Thư viện
          </p>
        </div>
      </div>
    </main>
  )
}
