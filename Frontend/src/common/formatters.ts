export const formatDate = (value: string | Date | null | undefined) =>
  value ? new Intl.DateTimeFormat('vi-VN').format(new Date(value)) : '—'
export const formatDateTime = (value: string | Date | null | undefined) =>
  value
    ? new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(
        new Date(value),
      )
    : '—'
export const formatCurrency = (value: number, currency = 'VND') =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency }).format(value)
export const formatCode = (value: string | null | undefined) => value?.trim().toUpperCase() || '—'
export const formatStatus = <Status extends string>(
  value: Status | null | undefined,
  labels: Partial<Record<Status, string>>,
) => (value ? (labels[value] ?? value) : '—')
