import { useEffect, useRef, useState, type ReactNode } from 'react'
import { globalLoadingEndEvent, globalLoadingStartEvent } from '@/shared/loading/global-loading'
import { FullScreenSpinner } from './FullScreenSpinner'

export function GlobalLoadingProvider({ children }: { children: ReactNode }) {
  const pendingCount = useRef(0)
  const showTimer = useRef<number | null>(null)
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const start = () => {
      pendingCount.current += 1
      if (pendingCount.current === 1 && showTimer.current === null) {
        showTimer.current = window.setTimeout(() => {
          showTimer.current = null
          if (pendingCount.current > 0) setVisible(true)
        }, 120)
      }
    }
    const end = () => {
      pendingCount.current = Math.max(0, pendingCount.current - 1)
      if (pendingCount.current !== 0) return
      if (showTimer.current !== null) {
        window.clearTimeout(showTimer.current)
        showTimer.current = null
      }
      setVisible(false)
    }

    window.addEventListener(globalLoadingStartEvent, start)
    window.addEventListener(globalLoadingEndEvent, end)
    return () => {
      window.removeEventListener(globalLoadingStartEvent, start)
      window.removeEventListener(globalLoadingEndEvent, end)
      if (showTimer.current !== null) window.clearTimeout(showTimer.current)
    }
  }, [])

  return (
    <>
      <div inert={visible || undefined}>{children}</div>
      {visible ? <FullScreenSpinner /> : null}
    </>
  )
}
