import { useEffect, useRef } from 'react'
import { useSearchParams } from 'react-router-dom'
import { updateSearchParams } from './table-contracts'

export const readUrlFilter = (key: string, fallback = '') =>
  new URLSearchParams(window.location.search).get(key) ?? fallback

export const readUrlPage = (key: string, fallback: number) => {
  const value = Number.parseInt(readUrlFilter(key), 10)
  return Number.isInteger(value) && value > 0 ? value : fallback
}

export function useFilterUrlSync(values: Record<string, string | number | undefined>) {
  const [, setSearchParams] = useSearchParams()
  const initialized = useRef(false)
  const serialized = JSON.stringify(values)
  useEffect(() => {
    if (!initialized.current) {
      initialized.current = true
      return
    }
    const changes = JSON.parse(serialized) as Record<string, string | number | undefined>
    setSearchParams((current) => updateSearchParams(current, changes))
  }, [serialized, setSearchParams])
  useEffect(() => {
    const restore = () => window.location.reload()
    window.addEventListener('popstate', restore)
    return () => window.removeEventListener('popstate', restore)
  }, [])
}
