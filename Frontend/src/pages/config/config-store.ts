import { recordActivityLog } from '../activity-logs/activity-log-store'

export type CirculationConfig = {
  maxDays: number
  maxBooks: number
  maxRenewals: number
  holdDays: number
  finePerDay: number
  blockOverdue: boolean
  lostBookPenaltyRatio: number
}

export type LibraryInfoConfig = {
  libName: string
  libEmail: string
  libAddress: string
  libPhone: string
  libHours: string
}

export type OtherSettingsConfig = {
  smtpHost: string
  smtpPort: number
  smtpUser: string
  autoBackup: boolean
  backupRetention: number
}

export type SystemConfig = {
  version: string
  lastUpdated: string
  circulation: CirculationConfig
  libraryInfo: LibraryInfoConfig
  otherSettings: OtherSettingsConfig
}

const STORAGE_KEYS = {
  CIRCULATION: 'config_circulation',
  LIBRARY_INFO: 'config_library_info',
  OTHER_SETTINGS: 'config_other_settings',
}

export const defaultCirculationConfig: CirculationConfig = {
  maxDays: 14,
  maxBooks: 5,
  maxRenewals: 2,
  holdDays: 3,
  finePerDay: 5000,
  blockOverdue: true,
  lostBookPenaltyRatio: 200,
}

export const defaultLibraryInfo: LibraryInfoConfig = {
  libName: 'Northstar Library',
  libEmail: 'contact@northstarlibrary.com',
  libAddress: '123 Đường Điện Biên Phủ, Quận Bình Thạnh, TP.HCM',
  libPhone: '028 3812 3456',
  libHours: '07:30 - 20:30 (Thứ 2 - Thứ 7)',
}

export const defaultOtherSettings: OtherSettingsConfig = {
  smtpHost: 'smtp.gmail.com',
  smtpPort: 587,
  smtpUser: 'no-reply@northstarlibrary.com',
  autoBackup: true,
  backupRetention: 30,
}

// ==================== CIRCULATION CONFIG ====================

export function loadCirculationConfig(): CirculationConfig {
  const saved = localStorage.getItem(STORAGE_KEYS.CIRCULATION)
  if (!saved) {
    // Migration fallback from old keys if any
    const oldMaxDays = localStorage.getItem('config_maxDays')
    const oldMaxBooks = localStorage.getItem('config_maxBooks')
    const oldFine = localStorage.getItem('config_finePerDay')
    const oldBlock = localStorage.getItem('config_blockOverdue')

    if (oldMaxDays || oldMaxBooks || oldFine || oldBlock) {
      const migrated: CirculationConfig = {
        ...defaultCirculationConfig,
        maxDays: oldMaxDays ? Number(oldMaxDays) : defaultCirculationConfig.maxDays,
        maxBooks: oldMaxBooks ? Number(oldMaxBooks) : defaultCirculationConfig.maxBooks,
        finePerDay: oldFine ? Number(oldFine) : defaultCirculationConfig.finePerDay,
        blockOverdue: oldBlock !== null ? oldBlock === 'true' : defaultCirculationConfig.blockOverdue,
      }
      saveCirculationConfig(migrated, 'Hệ thống (Migrate)', false)
      return migrated
    }

    localStorage.setItem(STORAGE_KEYS.CIRCULATION, JSON.stringify(defaultCirculationConfig))
    return defaultCirculationConfig
  }

  try {
    return { ...defaultCirculationConfig, ...JSON.parse(saved) }
  } catch {
    return defaultCirculationConfig
  }
}

export function saveCirculationConfig(
  config: CirculationConfig,
  adminName = 'System Administrator',
  shouldLog = true,
): void {
  const previous = loadCirculationConfig()
  localStorage.setItem(STORAGE_KEYS.CIRCULATION, JSON.stringify(config))

  // Keep old keys in sync for backward compatibility with existing code
  localStorage.setItem('config_maxDays', config.maxDays.toString())
  localStorage.setItem('config_maxBooks', config.maxBooks.toString())
  localStorage.setItem('config_finePerDay', config.finePerDay.toString())
  localStorage.setItem('config_blockOverdue', config.blockOverdue.toString())

  if (shouldLog) {
    recordActivityLog({
      admin: adminName,
      email: 'admin@example.com',
      action: 'Cập nhật quy tắc lưu thông & tiền phạt',
      module: 'Cấu hình',
      ip: '192.168.1.15',
      status: 'Thành công',
      details: {
        note: 'Cập nhật các thông số mượn/trả, hạn mượn và chính sách phạt vi phạm',
        before: previous as unknown as Record<string, unknown>,
        after: config as unknown as Record<string, unknown>,
      },
    })
  }
}

export function resetCirculationConfig(adminName = 'System Administrator'): CirculationConfig {
  const previous = loadCirculationConfig()
  localStorage.setItem(STORAGE_KEYS.CIRCULATION, JSON.stringify(defaultCirculationConfig))

  localStorage.setItem('config_maxDays', defaultCirculationConfig.maxDays.toString())
  localStorage.setItem('config_maxBooks', defaultCirculationConfig.maxBooks.toString())
  localStorage.setItem('config_finePerDay', defaultCirculationConfig.finePerDay.toString())
  localStorage.setItem('config_blockOverdue', defaultCirculationConfig.blockOverdue.toString())

  recordActivityLog({
    admin: adminName,
    email: 'admin@example.com',
    action: 'Khôi phục quy tắc lưu thông về mặc định',
    module: 'Cấu hình',
    ip: '192.168.1.15',
    status: 'Cảnh báo',
    details: {
      note: 'Khôi phục toàn bộ thông số mượn/trả về thiết lập ban đầu của hệ thống',
      before: previous as unknown as Record<string, unknown>,
      after: defaultCirculationConfig as unknown as Record<string, unknown>,
    },
  })

  return defaultCirculationConfig
}

// ==================== LIBRARY INFO ====================

export function loadLibraryInfo(): LibraryInfoConfig {
  const saved = localStorage.getItem(STORAGE_KEYS.LIBRARY_INFO)
  if (!saved) {
    // Check old individual keys
    const oldName = localStorage.getItem('info_libName')
    if (oldName) {
      const migrated: LibraryInfoConfig = {
        libName: oldName,
        libEmail: localStorage.getItem('info_libEmail') || defaultLibraryInfo.libEmail,
        libAddress: localStorage.getItem('info_libAddress') || defaultLibraryInfo.libAddress,
        libPhone: localStorage.getItem('info_libPhone') || defaultLibraryInfo.libPhone,
        libHours: localStorage.getItem('info_libHours') || defaultLibraryInfo.libHours,
      }
      saveLibraryInfo(migrated, 'Hệ thống (Migrate)', false)
      return migrated
    }
    localStorage.setItem(STORAGE_KEYS.LIBRARY_INFO, JSON.stringify(defaultLibraryInfo))
    return defaultLibraryInfo
  }
  try {
    return { ...defaultLibraryInfo, ...JSON.parse(saved) }
  } catch {
    return defaultLibraryInfo
  }
}

export function saveLibraryInfo(
  info: LibraryInfoConfig,
  adminName = 'System Administrator',
  shouldLog = true,
): void {
  const previous = loadLibraryInfo()
  localStorage.setItem(STORAGE_KEYS.LIBRARY_INFO, JSON.stringify(info))

  localStorage.setItem('info_libName', info.libName)
  localStorage.setItem('info_libEmail', info.libEmail)
  localStorage.setItem('info_libAddress', info.libAddress)
  localStorage.setItem('info_libPhone', info.libPhone)
  localStorage.setItem('info_libHours', info.libHours)

  if (shouldLog) {
    recordActivityLog({
      admin: adminName,
      email: 'admin@example.com',
      action: 'Cập nhật thông tin thư viện',
      module: 'Cấu hình',
      ip: '192.168.1.15',
      status: 'Thành công',
      details: {
        note: 'Cập nhật thông tin liên hệ và giờ hoạt động thư viện',
        before: previous as unknown as Record<string, unknown>,
        after: info as unknown as Record<string, unknown>,
      },
    })
  }
}

// ==================== OTHER SETTINGS ====================

export function loadOtherSettings(): OtherSettingsConfig {
  const saved = localStorage.getItem(STORAGE_KEYS.OTHER_SETTINGS)
  if (!saved) {
    const oldHost = localStorage.getItem('other_smtpHost')
    if (oldHost) {
      const migrated: OtherSettingsConfig = {
        smtpHost: oldHost,
        smtpPort: Number(localStorage.getItem('other_smtpPort') || defaultOtherSettings.smtpPort),
        smtpUser: localStorage.getItem('other_smtpUser') || defaultOtherSettings.smtpUser,
        autoBackup: localStorage.getItem('other_autoBackup') === 'true',
        backupRetention: Number(localStorage.getItem('other_backupRetention') || defaultOtherSettings.backupRetention),
      }
      saveOtherSettings(migrated, 'Hệ thống (Migrate)', false)
      return migrated
    }
    localStorage.setItem(STORAGE_KEYS.OTHER_SETTINGS, JSON.stringify(defaultOtherSettings))
    return defaultOtherSettings
  }
  try {
    return { ...defaultOtherSettings, ...JSON.parse(saved) }
  } catch {
    return defaultOtherSettings
  }
}

export function saveOtherSettings(
  settings: OtherSettingsConfig,
  adminName = 'System Administrator',
  shouldLog = true,
): void {
  const previous = loadOtherSettings()
  localStorage.setItem(STORAGE_KEYS.OTHER_SETTINGS, JSON.stringify(settings))

  localStorage.setItem('other_smtpHost', settings.smtpHost)
  localStorage.setItem('other_smtpPort', settings.smtpPort.toString())
  localStorage.setItem('other_smtpUser', settings.smtpUser)
  localStorage.setItem('other_autoBackup', settings.autoBackup.toString())
  localStorage.setItem('other_backupRetention', settings.backupRetention.toString())

  if (shouldLog) {
    recordActivityLog({
      admin: adminName,
      email: 'admin@example.com',
      action: 'Cập nhật thiết lập nâng cao (SMTP & Sao lưu)',
      module: 'Cấu hình',
      ip: '192.168.1.15',
      status: 'Thành công',
      details: {
        note: 'Cập nhật máy chủ thư điện tử và chu kỳ sao lưu hệ thống',
        before: previous as unknown as Record<string, unknown>,
        after: settings as unknown as Record<string, unknown>,
      },
    })
  }
}

// ==================== IMPORT / EXPORT PACKAGE ====================

export function exportFullConfigPackage(): string {
  const fullConfig: SystemConfig = {
    version: '1.0.0',
    lastUpdated: new Date().toISOString(),
    circulation: loadCirculationConfig(),
    libraryInfo: loadLibraryInfo(),
    otherSettings: loadOtherSettings(),
  }
  return JSON.stringify(fullConfig, null, 2)
}

export function importFullConfigPackage(
  jsonText: string,
  adminName = 'System Administrator',
): { success: boolean; message: string } {
  try {
    const parsed = JSON.parse(jsonText) as Partial<SystemConfig>
    if (!parsed.circulation || !parsed.libraryInfo) {
      return { success: false, message: 'Tệp cấu hình không đúng định dạng chuẩn của Northstar Library Portal.' }
    }

    const previousCirculation = loadCirculationConfig()
    saveCirculationConfig(parsed.circulation, adminName, false)
    saveLibraryInfo(parsed.libraryInfo, adminName, false)
    if (parsed.otherSettings) {
      saveOtherSettings(parsed.otherSettings, adminName, false)
    }

    recordActivityLog({
      admin: adminName,
      email: 'admin@example.com',
      action: 'Nhập gói cấu hình hệ thống từ tệp JSON',
      module: 'Cấu hình',
      ip: '192.168.1.15',
      status: 'Thành công',
      details: {
        note: 'Nhập toàn bộ thiết lập từ tệp cấu hình bên ngoài',
        before: previousCirculation as unknown as Record<string, unknown>,
        after: parsed.circulation as unknown as Record<string, unknown>,
      },
    })

    return { success: true, message: 'Nhập gói cấu hình thành công! Các thông số đã được áp dụng toàn hệ thống.' }
  } catch (e) {
    return { success: false, message: 'Không thể đọc tệp cấu hình: ' + (e instanceof Error ? e.message : 'Lỗi định dạng JSON') }
  }
}
