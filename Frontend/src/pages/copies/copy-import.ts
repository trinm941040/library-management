import type { CopyCondition, CopyImportRow } from './copy-api'

const requiredHeaders = ['barcode', 'isbn', 'shelfcode'] as const
const aliases: Record<string, string> = {
  barcode: 'barcode', mavach: 'barcode', isbn: 'isbn',
  shelfcode: 'shelfcode', make: 'shelfcode',
  condition: 'condition', tinhtrang: 'condition',
}
const conditions = new Set<CopyCondition>(['New', 'Good', 'Worn', 'Damaged', 'Lost'])
const conditionAliases: Record<string, CopyCondition> = {
  new: 'New', moi: 'New', good: 'Good', tot: 'Good', worn: 'Worn', daquasudung: 'Worn',
  damaged: 'Damaged', huhong: 'Damaged', lost: 'Lost', thatlac: 'Lost',
}

function normalize(value: string) {
  return value.trim().replace(/^\uFEFF/, '').normalize('NFD').replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-zA-Z0-9]/g, '').toLowerCase()
}

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
  const rows = parseCsv(text)
  if (!rows.length) throw new Error('Tệp CSV không có dòng tiêu đề.')
  const indexes = new Map<string, number>()
  rows[0].forEach((header, index) => {
    const canonical = aliases[normalize(header)]
    if (!canonical) throw new Error(`Cột "${header.trim()}" không được hỗ trợ.`)
    if (indexes.has(canonical)) throw new Error(`Cột "${header.trim()}" bị lặp.`)
    indexes.set(canonical, index)
  })
  const missing = requiredHeaders.filter(header => !indexes.has(header))
  if (missing.length) throw new Error(`Thiếu cột bắt buộc: ${missing.join(', ')}.`)
  if (rows.length < 2 || rows.length > 1001) throw new Error('Tệp phải có từ 1 đến 1000 bản sao.')
  const value = (cells: string[], name: string) => {
    const index = indexes.get(name)
    return index === undefined || index >= cells.length ? '' : cells[index].trim()
  }
  return rows.slice(1).map((cells, index) => {
    if (cells.length > rows[0].length) throw new Error(`Dòng ${index + 2} có nhiều cột hơn dòng tiêu đề.`)
    const barcode = value(cells, 'barcode')
    const isbn = value(cells, 'isbn')
    const shelfCode = value(cells, 'shelfcode')
    if (!barcode || !isbn || !shelfCode) throw new Error(`Dòng ${index + 2}: Barcode, ISBN và ShelfCode là bắt buộc.`)
    const rawCondition = value(cells, 'condition')
    const condition = rawCondition ? conditionAliases[normalize(rawCondition)] : 'Good'
    if (!condition || !conditions.has(condition)) throw new Error(`Dòng ${index + 2}: tình trạng không hợp lệ.`)
    return { barcode, isbn, shelfCode, condition }
  })
}

export function downloadCopyTemplate() {
  const content = '\uFEFFBarcode,ISBN,ShelfCode,Condition\r\nCOPY-0001,9786044832814,A-01,Good\r\n'
  const url = URL.createObjectURL(new Blob([content], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = 'mau-nhap-ban-sao.csv'
  link.click()
  URL.revokeObjectURL(url)
}
