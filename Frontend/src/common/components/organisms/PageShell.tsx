import type { ReactNode } from 'react'
import { cn } from '@/utils/cn'

export function PageShell({
  eyebrow,
  title,
  description,
  actions,
  children,
  className,
}: {
  eyebrow?: string
  title: string
  description?: string
  actions?: ReactNode
  children: ReactNode
  className?: string
}) {
  return (
    <main className={cn('mx-auto w-full min-w-0 max-w-7xl px-4 py-6 sm:px-6 md:px-10 md:py-10 xl:px-12', className)}>
      <header className="mb-6 flex min-w-0 flex-col justify-between gap-4 sm:mb-8 xl:flex-row xl:items-end">
        <div className="min-w-0">
          {eyebrow ? (
            <p className="mb-2 text-xs font-bold tracking-widest text-primary uppercase">
              {eyebrow}
            </p>
          ) : null}
          <h1 className="break-words text-2xl font-bold tracking-tight sm:text-3xl">{title}</h1>
          {description ? (
            <p className="mt-2 line-clamp-2 max-w-2xl text-sm text-muted-foreground sm:line-clamp-none">{description}</p>
          ) : null}
        </div>
        {actions ? (
          <div className="page-shell-actions grid w-full min-w-0 grid-cols-2 gap-2 sm:flex sm:flex-wrap xl:w-auto [&>*]:max-w-full [&_[data-slot=button]]:w-full sm:[&_[data-slot=button]]:w-auto">
            {actions}
          </div>
        ) : null}
      </header>
      {children}
    </main>
  )
}
