export function safeIntendedDestination(value: unknown): string {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//'))
    return '/dashboard'
  try {
    const decoded = decodeURIComponent(value)
    if (
      [...decoded].some((character) => character === '\\' || character.charCodeAt(0) <= 32) ||
      decoded.startsWith('//')
    )
      return '/dashboard'
    const url = new URL(value, 'https://library.invalid')
    if (
      url.origin !== 'https://library.invalid' ||
      /^\/(login|api)(\/|$)/i.test(decodeURIComponent(url.pathname))
    )
      return '/dashboard'
    return url.pathname + url.search + url.hash
  } catch {
    return '/dashboard'
  }
}
