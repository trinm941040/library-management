import { useEffect, useRef } from 'react'
import { Bold, Italic, Link, List, Redo2, Undo2 } from 'lucide-react'
import { Button } from '@/common/components/ui/button'

export function EmailTemplateEditor({
  id,
  value,
  onChange,
  variables,
  disabled = false,
}: {
  id?: string
  value: string
  onChange: (value: string) => void
  variables: string[]
  disabled?: boolean
}) {
  const editorRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const editor = editorRef.current
    if (editor && document.activeElement !== editor && editor.innerHTML !== value)
      editor.innerHTML = value
  }, [value])

  const execute = (command: string, commandValue?: string) => {
    if (disabled) return
    editorRef.current?.focus()
    document.execCommand(command, false, commandValue)
    onChange(editorRef.current?.innerHTML ?? '')
  }

  const insertLink = () => {
    const url = window.prompt('Nhập liên kết bắt đầu bằng https:// hoặc mailto:')?.trim()
    if (!url || (!url.startsWith('https://') && !url.startsWith('mailto:'))) return
    execute('createLink', url)
  }

  return (
    <div className="overflow-hidden rounded-md border bg-background focus-within:ring-2 focus-within:ring-ring">
      <div
        className="flex flex-wrap items-center gap-1 border-b bg-muted/40 p-2"
        role="toolbar"
        aria-label="Công cụ soạn email"
      >
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={disabled}
          aria-label="Hoàn tác"
          onClick={() => execute('undo')}
        >
          <Undo2 />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={disabled}
          aria-label="Làm lại"
          onClick={() => execute('redo')}
        >
          <Redo2 />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={disabled}
          aria-label="In đậm"
          onClick={() => execute('bold')}
        >
          <Bold />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={disabled}
          aria-label="In nghiêng"
          onClick={() => execute('italic')}
        >
          <Italic />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={disabled}
          aria-label="Danh sách"
          onClick={() => execute('insertUnorderedList')}
        >
          <List />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={disabled}
          aria-label="Chèn liên kết"
          onClick={insertLink}
        >
          <Link />
        </Button>
        <span className="mx-1 h-6 border-l" aria-hidden="true" />
        {variables.map((variable) => (
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={disabled}
            key={variable}
            onClick={() => execute('insertText', `{{${variable}}}`)}
          >{`{{${variable}}}`}</Button>
        ))}
      </div>
      <div
        id={id}
        ref={editorRef}
        contentEditable={!disabled}
        suppressContentEditableWarning
        role="textbox"
        aria-label="Nội dung mẫu thông báo"
        aria-multiline="true"
        aria-required="true"
        className="min-h-64 p-4 text-sm leading-6 outline-none [&_a]:text-primary [&_a]:underline [&_ol]:list-decimal [&_ol]:pl-6 [&_ul]:list-disc [&_ul]:pl-6"
        onInput={(event) => onChange(event.currentTarget.innerHTML)}
      />
    </div>
  )
}
