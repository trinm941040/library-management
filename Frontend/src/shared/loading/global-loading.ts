export const globalLoadingStartEvent = 'uth:global-loading-start'
export const globalLoadingEndEvent = 'uth:global-loading-end'

export function beginGlobalLoading() {
  window.dispatchEvent(new Event(globalLoadingStartEvent))
}

export function endGlobalLoading() {
  window.dispatchEvent(new Event(globalLoadingEndEvent))
}
