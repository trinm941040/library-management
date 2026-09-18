import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { parseTableUrlState, updateSearchParams } from './table-contracts'

export function useUrlListState(defaultSort = 'createdAtUtc', allowedSortFields: readonly string[] = [defaultSort]) {
  const [params, setParams] = useSearchParams()
  const table = useMemo(
    () => parseTableUrlState(params, allowedSortFields, defaultSort),
    [allowedSortFields, defaultSort, params],
  )
  const status = params.get('status') ?? 'all'
  const update = useCallback((changes: Record<string, string | number | undefined>) => {
    setParams((current) => updateSearchParams(current, changes))
  }, [setParams])
  return { ...table, status, update }
}
