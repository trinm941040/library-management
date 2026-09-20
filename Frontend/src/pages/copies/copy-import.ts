import type { CopyCondition, CopyImportRow } from './copy-api'

const header = ['Barcode', 'BookId', 'ShelfId', 'Condition']
const conditions = new Set<CopyCondition>(['New', 'Good', 'Worn', 'Damaged', 'Lost'])

function parseCsv(text: string): string[][] {
  const rows: string[][] = []
  let row: string[] = []
  let field = ''
  let quoted = false
  for (let index = 0; index < text.length; index++) {
    const char = text[index]
    if (char === '"') {
      if (quoted && text[index + 1] === '"') { field += '"'; index++ }
      else if (!field || quoted) quoted = !quoted
      else throw new Error('Dấu ngoặc kép trong CSV không hợp lệ.')
    } else if (char === ',' && !quoted) { row.push(field); field = '' }
    else if ((char === '\n' || char === '\r') && !quoted) {
      if (char === '\r' && text[index + 1] === '\n') index++
      row.push(field)
      if (row.some(value => value.trim())) rows.push(row)
      row = []; field = ''
    } else field += char
  }
  if (quoted) throw new Error('Dòng CSV còn thiếu dấu ngoặc kép đóng.')
  row.push(field)
  if (row.some(value => value.trim())) rows.push(row)
  return rows
}

export function parseCopyImport(text: string): CopyImportRow[] {
  const rows = parseCsv(text.replace(/^\uFEFF/, ''))
  if (!rows.length || rows[0].length !== 4 || rows[0].some((value, index) => value.trim() !== header[index]))
    throw new Error('Header CSV phải là Barcode,BookId,ShelfId,Condition.')
  if (rows.length < 2 || rows.length > 1001) throw new Error('Tệp phải có từ 1 đến 1000 bản sao.')
  return rows.slice(1).map((cells, index) => {
    if (cells.length !== 4) throw new Error(`Dòng ${index + 2} phải có 4 cột.`)
    const condition = cells[3].trim() as CopyCondition
    if (!conditions.has(condition)) throw new Error(`Dòng ${index + 2}: tình trạng không hợp lệ.`)
    return { barcode: cells[0].trim(), bookId: cells[1].trim(), shelfId: cells[2].trim(), condition }
  })
}

export function downloadCopyTemplate() {
  const url = URL.createObjectURL(new Blob(['Barcode,BookId,ShelfId,Condition\r\n'], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = 'book-copies-template.csv'
  link.click()
  URL.revokeObjectURL(url)
}
