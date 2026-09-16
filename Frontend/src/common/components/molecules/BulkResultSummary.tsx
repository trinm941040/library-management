import type { BulkResult } from '@/shared/data/table-contracts'

export function BulkResultSummary({ result }: { result: BulkResult }) {
  return (
    <div className="grid gap-2 rounded-md border bg-muted/30 p-3 text-sm" role="status">
      <p><strong>{result.succeededCount}</strong> thành công · <strong>{result.failedCount}</strong> thất bại</p>
      {result.failedCount > 0 ? (
        <ul className="max-h-32 list-disc overflow-auto pl-5 text-destructive">
          {result.items.filter((item) => !item.succeeded).map((item) => (
            <li key={item.id}><span className="font-mono">{item.id}</span>: {item.error}</li>
          ))}
        </ul>
      ) : null}
      <small className="break-all text-muted-foreground">Correlation ID: {result.correlationId}</small>
    </div>
  )
}
