import { Bookmark, Check, Trash2 } from 'lucide-react'
import { Badge } from '@/common/components/ui/badge'
import { Button } from '@/common/components/ui/button'
import type { SavedFilter } from '../reports-api'

type SavedFiltersBarProps = {
  savedFilters: SavedFilter[]
  activeFilterId?: string | null
  onApply: (filter: SavedFilter) => void
  onDelete: (id: string) => void
}

export function SavedFiltersBar({
  savedFilters,
  activeFilterId,
  onApply,
  onDelete,
}: SavedFiltersBarProps) {
  if (!savedFilters || savedFilters.length === 0) {
    return null
  }

  return (
    <div className="flex items-center gap-2 flex-wrap text-xs py-1">
      <span className="text-muted-foreground flex items-center gap-1 font-medium mr-1">
        <Bookmark className="w-3.5 h-3.5 text-primary" /> Bộ lọc đã lưu:
      </span>

      {savedFilters.map((sf) => {
        const isActive = sf.id === activeFilterId

        return (
          <div
            key={sf.id}
            className={`group inline-flex items-center rounded-full border transition-all pl-2.5 pr-1 py-0.5 gap-1 ${
              isActive
                ? 'bg-primary/10 border-primary text-primary font-semibold'
                : 'bg-muted/40 border-border text-foreground hover:bg-muted'
            }`}
          >
            <button
              type="button"
              className="cursor-pointer text-xs focus:outline-none flex items-center gap-1"
              onClick={() => onApply(sf)}
              title={`Áp dụng bộ lọc: ${sf.name}`}
            >
              {isActive && <Check className="w-3 h-3 text-primary" />}
              <span>{sf.name}</span>
            </button>

            <button
              type="button"
              className="p-1 rounded-full text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors opacity-60 group-hover:opacity-100"
              onClick={(e) => {
                e.stopPropagation()
                onDelete(sf.id)
              }}
              title="Xóa bộ lọc này"
              aria-label={`Xóa bộ lọc ${sf.name}`}
            >
              <Trash2 className="w-3 h-3" />
            </button>
          </div>
        )
      })}
    </div>
  )
}
