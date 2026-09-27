import { useEffect, useState, type FormEvent } from 'react'
import { EntityForm, EntityFormField } from '@/common/components'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/common/components/ui/dialog'
import { Input } from '@/common/components/ui/input'
import type { LocationInput, LocationNode, LocationType } from './branch-api'

type Props = {
  open: boolean
  type: LocationType
  editing: LocationNode | null
  defaultParentId: string | null
  parentOptions: Array<{ id: string; label: string }>
  onOpenChange: (open: boolean) => void
  onSave: (input: LocationInput) => Promise<string | null>
}

const labels: Record<LocationType, string> = { Branch: 'chi nhánh', Area: 'khu vực', Shelf: 'kệ' }

export function LocationFormDialog({
  open,
  type,
  editing,
  defaultParentId,
  parentOptions,
  onOpenChange,
  onSave,
}: Props) {
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [address, setAddress] = useState('')
  const [parentId, setParentId] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    setCode(editing?.code ?? '')
    setName(editing?.name ?? '')
    setAddress(editing?.address ?? '')
    setParentId(editing?.parentId ?? defaultParentId ?? parentOptions[0]?.id ?? '')
    setError('')
  }, [defaultParentId, editing, open, parentOptions])

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!code.trim() || !name.trim()) {
      setError('Vui lòng nhập đầy đủ mã và tên.')
      return
    }
    if (type !== 'Branch' && !parentId) {
      setError(`Vui lòng chọn ${type === 'Area' ? 'chi nhánh' : 'khu vực'}.`)
      return
    }
    setSubmitting(true)
    setError('')
    try {
      const saveError = await onSave({
        code: code.trim(),
        name: name.trim(),
        address: address.trim() || null,
        parentId: type === 'Branch' ? null : parentId,
        concurrencyToken: editing?.concurrencyToken,
      })
      if (saveError) setError(saveError)
      else onOpenChange(false)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(next) => !submitting && onOpenChange(next)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{editing ? 'Cập nhật' : 'Tạo'} {labels[type]}</DialogTitle>
          <DialogDescription>
            Mã được chuẩn hóa thành chữ hoa và phải duy nhất trong đúng phạm vi.
          </DialogDescription>
        </DialogHeader>
        <EntityForm
          isSubmitting={submitting}
          submitLabel={editing ? 'Lưu thay đổi' : 'Tạo mới'}
          serverError={error}
          isConflict={error.toLowerCase().includes('thay đổi') || error.toLowerCase().includes('tồn tại')}
          onSubmit={submit}
          onCancel={() => onOpenChange(false)}
        >
          <EntityFormField id="location-code" label="Mã *">
            <Input id="location-code" maxLength={30} required value={code} onChange={(event) => setCode(event.target.value)} />
          </EntityFormField>
          <EntityFormField id="location-name" label={type === 'Shelf' ? 'Nhãn kệ *' : 'Tên *'}>
            <Input id="location-name" maxLength={150} required value={name} onChange={(event) => setName(event.target.value)} />
          </EntityFormField>
          {type === 'Branch' ? (
            <EntityFormField id="location-address" label="Địa chỉ">
              <Input id="location-address" maxLength={500} value={address} onChange={(event) => setAddress(event.target.value)} />
            </EntityFormField>
          ) : (
            <EntityFormField id="location-parent" label={type === 'Area' ? 'Chi nhánh' : 'Khu vực'}>
              <select
                id="location-parent"
                className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
                value={parentId}
                disabled={Boolean(editing)}
                onChange={(event) => setParentId(event.target.value)}
              >
                <option value="">Chọn vị trí cha</option>
                {parentOptions.map((option) => <option key={option.id} value={option.id}>{option.label}</option>)}
              </select>
            </EntityFormField>
          )}
        </EntityForm>
      </DialogContent>
    </Dialog>
  )
}
