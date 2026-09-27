import { useEffect } from 'react'
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
  const serialized = JSON.stringify(values)
  useEffect(() => {
    const changes = JSON.parse(serialized) as Record<string, string | number | undefined>
    setSearchParams((current) => {
      const next = updateSearchParams(current, changes)
      return next.toString() === current.toString() ? current : next
    }, { replace: true })
  }, [serialized, setSearchParams])
  useEffect(() => {
    const restore = () => window.location.reload()
    window.addEventListener('popstate', restore)
    return () => window.removeEventListener('popstate', restore)
  }, [])
}
