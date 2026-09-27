import { useEffect, useRef, type FormEvent, type ReactNode } from 'react'
import { Button } from '@/common/components/ui/button'
import { Label } from '@/common/components/ui/label'

export function EntityForm({
  children,
  isSubmitting,
  submitLabel = 'Lưu',
  serverError,
  isConflict,
  onSubmit,
  onCancel,
}: {
  children: ReactNode
  isSubmitting: boolean
  submitLabel?: string
  serverError?: string
  isConflict?: boolean
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onCancel: () => void
}) {
  const serverErrorRef = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (serverError) serverErrorRef.current?.focus()
  }, [serverError])

  return (
    <form onSubmit={onSubmit} aria-busy={isSubmitting} noValidate>
      <fieldset disabled={isSubmitting} className="grid gap-5 border-0 p-0">
        {children}
      </fieldset>
      {serverError ? (
        <div
          ref={serverErrorRef}
          className="mt-5 rounded-md border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive"
          role="alert"
          tabIndex={-1}
        >
          <strong>{isConflict ? 'Dữ liệu đã thay đổi' : 'Không thể lưu dữ liệu'}</strong>
          <p className="mt-1">{serverError}</p>
          {isConflict ? (
            <p className="mt-1 text-muted-foreground">Hãy tải lại dữ liệu rồi thử lại.</p>
          ) : null}
        </div>
      ) : null}
      <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Button type="button" variant="outline" disabled={isSubmitting} onClick={onCancel}>
          Hủy
        </Button>
        <Button type="submit" loading={isSubmitting} loadingLabel="Đang lưu dữ liệu">
          {submitLabel}
        </Button>
      </div>
    </form>
  )
}

export function EntityFormField({
  id,
  label,
  error,
  hint,
  required = false,
  children,
}: {
  id: string
  label: string
  error?: string
  hint?: string
  required?: boolean
  children: ReactNode
}) {
  const messageId = `${id}-${error ? 'error' : 'hint'}`
  return (
    <div className="grid gap-2">
      <Label htmlFor={id}>
        {label}{required ? <span className="text-destructive" aria-hidden="true"> *</span> : null}
      </Label>
      {children}
      {error ? (
        <p id={messageId} className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : hint ? (
        <p id={messageId} className="text-xs text-muted-foreground">
          {hint}
        </p>
      ) : null}
    </div>
  )
}
