import { ScanBarcode } from 'lucide-react'
import { forwardRef, type ComponentProps } from 'react'
import { Input } from '@/common/components/ui/input'
import { cn } from '@/utils/cn'

type BarcodeInputProps = Omit<ComponentProps<typeof Input>, 'onChange'> & {
  value: string
  onChange: (value: string) => void
  onScan?: (value: string) => void
}

export const BarcodeInput = forwardRef<HTMLInputElement, BarcodeInputProps>(function BarcodeInput(
  { value, onChange, onScan, className, onKeyDown, ...props },
  ref,
) {
  return (
    <div className="relative">
      <ScanBarcode
        className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden="true"
      />
      <Input
        ref={ref}
        value={value}
        className={cn('pl-9 font-mono tracking-wide', className)}
        inputMode="numeric"
        autoCapitalize="off"
        spellCheck={false}
        onChange={(event) => onChange(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' && value.trim() && onScan) {
            event.preventDefault()
            onScan(value.trim())
          }
          onKeyDown?.(event)
        }}
        {...props}
      />
    </div>
  )
})
