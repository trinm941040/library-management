import { useId } from 'react'
import { cn } from '@/utils/cn'

export function AiSparklesIcon({ className }: { className?: string }) {
  const gradientId = useId().replace(/:/g, '')
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
      className={cn('size-5 shrink-0', className)}
    >
      <defs>
        <linearGradient
          id={gradientId}
          x1="3"
          y1="21"
          x2="21"
          y2="3"
          gradientUnits="userSpaceOnUse"
        >
          <stop stopColor="#2563eb" />
          <stop offset="0.5" stopColor="#6366f1" />
          <stop offset="1" stopColor="#a855f7" />
        </linearGradient>
      </defs>
      <path
        d="M9.8 3.7c.35-1.25 2.05-1.25 2.4 0l.62 2.18a5 5 0 0 0 3.45 3.45l2.18.62c1.25.35 1.25 2.05 0 2.4l-2.18.62a5 5 0 0 0-3.45 3.45l-.62 2.18c-.35 1.25-2.05 1.25-2.4 0l-.62-2.18a5 5 0 0 0-3.45-3.45l-2.18-.62c-1.25-.35-1.25-2.05 0-2.4l2.18-.62a5 5 0 0 0 3.45-3.45L9.8 3.7Z"
        stroke={`url(#${gradientId})`}
        strokeWidth="1.9"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M18.5 2v4M16.5 4h4M4 18.5v2M3 19.5h2"
        stroke={`url(#${gradientId})`}
        strokeWidth="1.9"
        strokeLinecap="round"
      />
    </svg>
  )
}
