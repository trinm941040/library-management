import * as React from 'react'
import { XIcon } from 'lucide-react'
import { Dialog as DialogPrimitive } from 'radix-ui'

import { cn } from '@/utils/cn'
import { Button } from '@/common/components/ui/button'

function Dialog({ ...props }: React.ComponentProps<typeof DialogPrimitive.Root>) {
  return <DialogPrimitive.Root data-slot="dialog" {...props} />
}

function DialogTrigger({ ...props }: React.ComponentProps<typeof DialogPrimitive.Trigger>) {
  return <DialogPrimitive.Trigger data-slot="dialog-trigger" {...props} />
}

function DialogPortal({ ...props }: React.ComponentProps<typeof DialogPrimitive.Portal>) {
  return <DialogPrimitive.Portal data-slot="dialog-portal" {...props} />
}

function DialogClose({ ...props }: React.ComponentProps<typeof DialogPrimitive.Close>) {
  return <DialogPrimitive.Close data-slot="dialog-close" {...props} />
}

function DialogOverlay({
  className,
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Overlay>) {
  return (
    <DialogPrimitive.Overlay
      data-slot="dialog-overlay"
      className={cn(
        'fixed inset-0 z-50 bg-black/50 data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:animate-in data-[state=open]:fade-in-0',
        className,
      )}
      {...props}
    />
  )
}

function flattenDialogChildren(children: React.ReactNode): React.ReactNode[] {
  return React.Children.toArray(children).flatMap((child) => {
    if (
      React.isValidElement<{ children?: React.ReactNode }>(child) &&
      child.type === React.Fragment
    ) {
      return flattenDialogChildren(child.props.children)
    }
    return [child]
  })
}

function isDialogSlot(
  child: React.ReactNode,
  slot: typeof DialogHeader | typeof DialogFooter,
): child is React.ReactElement {
  return React.isValidElement(child) && child.type === slot
}

function arrangeDialogChildren(children: React.ReactNode, viewportClassName?: string) {
  const outerChildren = flattenDialogChildren(children)
  const form = outerChildren.find(
    (child): child is React.ReactElement<React.ComponentProps<'form'>> =>
      React.isValidElement<React.ComponentProps<'form'>>(child) && child.type === 'form',
  )
  const formChildren = form ? flattenDialogChildren(form.props.children) : []
  const allChildren = [...outerChildren.filter((child) => child !== form), ...formChildren]
  const header = allChildren.find((child) => isDialogSlot(child, DialogHeader))
  const footer = allChildren.find((child) => isDialogSlot(child, DialogFooter))

  if (!header || !footer) {
    return (
      <div
        data-slot="dialog-scroll-viewport"
        className={cn(
          'grid min-h-0 max-h-[calc(100dvh-4rem)] gap-4 overflow-x-hidden overflow-y-auto overscroll-contain sm:max-h-[calc(100dvh-5rem)]',
          viewportClassName,
        )}
      >
        {children}
      </div>
    )
  }

  const body = allChildren.filter(
    (child) => !isDialogSlot(child, DialogHeader) && !isDialogSlot(child, DialogFooter),
  )
  const bodyClassName = cn(
    'min-h-0 overflow-x-hidden overflow-y-auto overscroll-contain',
    form?.props.className,
    viewportClassName,
  )
  const sections = [
    header,
    <div key="dialog-body" data-slot="dialog-body" className={bodyClassName}>
      {body}
    </div>,
    footer,
  ]
  const layoutClassName =
    'grid min-h-0 max-h-[calc(100dvh-4rem)] grid-rows-[auto_minmax(0,1fr)_auto] gap-4 overflow-hidden sm:max-h-[calc(100dvh-5rem)]'

  if (form) {
    return React.cloneElement(form, { className: layoutClassName }, ...sections)
  }

  return (
    <div data-slot="dialog-layout" className={layoutClassName}>
      {sections}
    </div>
  )
}

function DialogContent({
  className,
  children,
  showCloseButton = true,
  viewportClassName,
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Content> & {
  showCloseButton?: boolean
  viewportClassName?: string
}) {
  return (
    <DialogPortal data-slot="dialog-portal">
      <DialogOverlay />
      <DialogPrimitive.Content
        data-slot="dialog-content"
        className={cn(
          'fixed top-[50%] left-[50%] z-50 grid min-w-0 max-h-[calc(100dvh-2rem)] w-[calc(100vw-1rem)] max-w-[calc(100vw-1rem)] translate-x-[-50%] translate-y-[-50%] rounded-lg border bg-background p-4 shadow-lg duration-200 outline-none [&>*]:min-w-0 [&_code]:break-all [&_dd]:break-words [&_p]:break-words data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=closed]:zoom-out-95 data-[state=open]:animate-in data-[state=open]:fade-in-0 data-[state=open]:zoom-in-95 sm:w-full sm:max-w-lg sm:p-6',
          className,
          'max-h-none overflow-hidden',
        )}
        {...props}
      >
        {arrangeDialogChildren(children, viewportClassName)}
        {showCloseButton && (
          <DialogPrimitive.Close
            data-slot="dialog-close"
            className="absolute top-4 right-4 z-30 rounded-xs opacity-70 ring-offset-background transition-opacity hover:opacity-100 focus:ring-2 focus:ring-ring focus:ring-offset-2 focus:outline-hidden disabled:pointer-events-none data-[state=open]:bg-accent data-[state=open]:text-muted-foreground [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4"
          >
            <XIcon />
            <span className="sr-only">Đóng</span>
          </DialogPrimitive.Close>
        )}
      </DialogPrimitive.Content>
    </DialogPortal>
  )
}

function DialogHeader({ className, ...props }: React.ComponentProps<'div'>) {
  return (
    <div
      data-slot="dialog-header"
      className={cn(
        'flex min-w-0 flex-col gap-2 bg-background pr-10 pb-2 text-center sm:text-left',
        className,
      )}
      {...props}
    />
  )
}

function DialogFooter({
  className,
  showCloseButton = false,
  children,
  ...props
}: React.ComponentProps<'div'> & {
  showCloseButton?: boolean
}) {
  return (
    <div
      data-slot="dialog-footer"
      className={cn(
        'mb-3 flex flex-col-reverse gap-2 bg-background pt-2 pb-2 sm:flex-row sm:justify-end max-sm:[&>*]:w-full',
        className,
      )}
      {...props}
    >
      {children}
      {showCloseButton && (
        <DialogPrimitive.Close asChild>
          <Button type="button" variant="outline">
            Đóng
          </Button>
        </DialogPrimitive.Close>
      )}
    </div>
  )
}

function DialogTitle({ className, ...props }: React.ComponentProps<typeof DialogPrimitive.Title>) {
  return (
    <DialogPrimitive.Title
      data-slot="dialog-title"
      className={cn('text-lg leading-none font-semibold', className)}
      {...props}
    />
  )
}

function DialogDescription({
  className,
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Description>) {
  return (
    <DialogPrimitive.Description
      data-slot="dialog-description"
      className={cn('text-sm text-muted-foreground', className)}
      {...props}
    />
  )
}

export {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogOverlay,
  DialogPortal,
  DialogTitle,
  DialogTrigger,
}
