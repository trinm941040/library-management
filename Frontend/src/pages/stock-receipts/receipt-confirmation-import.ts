import type { StockReceipt } from './receipt-api'

export type ReceiptConfirmationImportRow = {
  rowNumber: number
  isbn: string
  barcode: string
  shelfCode: string
  condition?: 'New' | 'Good' | 'Worn' | 'Damaged'
}

const requiredHeaders = ['isbn', 'barcode', 'shelfcode'] as const
const aliases: Record<string, string> = {
  isbn: 'isbn',
  barcode: 'barcode',
  mavach: 'barcode',
  shelfcode: 'shelfcode',
  make: 'shelfcode',
  condition: 'condition',
  tinhtrang: 'condition',
}
const conditionAliases: Record<string, ReceiptConfirmationImportRow['condition']> = {
  new: 'New', moi: 'New', good: 'Good', tot: 'Good', worn: 'Worn', cu: 'Worn',
  daquasudung: 'Worn', damaged: 'Damaged', hong: 'Damaged', huhong: 'Damaged',
}

export function normalizeImportValue(value: string) {
  return value.trim().replace(/^\uFEFF/, '').normalize('NFD').replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-zA-Z0-9]/g, '').toLowerCase()
}

function parseCsv(text: string): string[][] {
  const rows: string[][] = []
  let row: string[] = []
  let field = ''
  let quoted = false

  for (let index = 0; index < text.length; index++) {
    const character = text[index]
    if (character === '"') {
      if (quoted && text[index + 1] === '"') { field += '"'; index++ }
      else if (!field || quoted) quoted = !quoted
      else throw new Error('Dấu ngoặc kép trong CSV không hợp lệ.')
    } else if (character === ',' && !quoted) {
      row.push(field); field = ''
    } else if ((character === '\n' || character === '\r') && !quoted) {
      if (character === '\r' && text[index + 1] === '\n') index++
      row.push(field)
      if (row.some(value => value.trim())) rows.push(row)
      row = []; field = ''
    } else field += character
  }

  if (quoted) throw new Error('Dòng CSV còn thiếu dấu ngoặc kép đóng.')
  row.push(field)
  if (row.some(value => value.trim())) rows.push(row)
  return rows
}

export function parseReceiptConfirmationCsv(text: string): ReceiptConfirmationImportRow[] {
  const rows = parseCsv(text)
  if (!rows.length) throw new Error('Tệp CSV không có dòng tiêu đề.')

  const indexes = new Map<string, number>()
  rows[0].forEach((header, index) => {
    const canonical = aliases[normalizeImportValue(header)]
    if (!canonical) return
    if (indexes.has(canonical)) throw new Error(`Cột "${header.trim()}" bị lặp.`)
    indexes.set(canonical, index)
  })

  const missing = requiredHeaders.filter(header => !indexes.has(header))
  if (missing.length) throw new Error(`Thiếu cột bắt buộc: ${missing.join(', ')}.`)
  if (rows.length < 2 || rows.length > 1001) throw new Error('Tệp phải có từ 1 đến 1000 bản sao.')

  const value = (cells: string[], fieldName: string) => {
    const index = indexes.get(fieldName)
    return index === undefined || index >= cells.length ? '' : cells[index].trim()
  }

  return rows.slice(1).map((cells, index) => {
    const isbn = value(cells, 'isbn')
    const barcode = value(cells, 'barcode')
    const shelfCode = value(cells, 'shelfcode')
    if (!isbn || !barcode || !shelfCode)
      throw new Error(`Dòng ${index + 2}: ISBN, Barcode và ShelfCode là bắt buộc.`)

    const rawCondition = value(cells, 'condition')
    const condition = rawCondition ? conditionAliases[normalizeImportValue(rawCondition)] : undefined
    if (rawCondition && !condition)
      throw new Error(`Dòng ${index + 2}: tình trạng phải là New, Good, Worn hoặc Damaged.`)

    return { rowNumber: index + 2, isbn, barcode, shelfCode, condition }
  })
}

function csvCell(value: string) {
  return `"${value.replaceAll('"', '""')}"`
}

export function downloadReceiptConfirmationTemplate(receipt: StockReceipt) {
  const rows = receipt.items.flatMap(item => Array.from({ length: item.receivedQuantity }, (_, index) => [
    item.isbn,
    '',
    '',
    index < item.damagedQuantity ? 'Damaged' : 'Good',
  ]))
  const content = '\uFEFFISBN,Barcode,ShelfCode,Condition\r\n' +
    rows.map(row => row.map(csvCell).join(',')).join('\r\n') + '\r\n'
  const url = URL.createObjectURL(new Blob([content], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `mau-gan-ma-vach-${receipt.receiptNumber}.csv`
  link.click()
  URL.revokeObjectURL(url)
}
