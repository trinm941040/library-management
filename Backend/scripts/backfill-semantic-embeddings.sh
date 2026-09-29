#!/usr/bin/env bash

set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:5191}"
BATCH_DELAY_SECONDS="${BATCH_DELAY_SECONDS:-1}"
REQUEST_TIMEOUT_SECONDS="${REQUEST_TIMEOUT_SECONDS:-1800}"
BACKFILL_URL="${API_BASE_URL%/}/api/v1/books/semantic-embeddings/backfill"
MAX_RETRIES=5
RETRY_DELAYS=(2 4 8 16 30)

response_file="$(mktemp -t semantic-backfill.XXXXXX)"
started_at="$(date +%s)"
total_processed=0
batch_count=0
last_remaining=-1

cleanup() {
  rm -f "$response_file"
}

handle_interrupt() {
  printf '\nBackfill interrupted. Run the same command again to resume safely.\n' >&2
  exit 130
}

trap cleanup EXIT
trap handle_interrupt INT TERM

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    printf 'Required command not found: %s\n' "$1" >&2
    exit 1
  fi
}

print_response_error() {
  if jq -e . "$response_file" >/dev/null 2>&1; then
    jq -c . "$response_file" >&2
  else
    head -c 2000 "$response_file" >&2 || true
    printf '\n' >&2
  fi
}

validate_response() {
  jq -e '
    (.processedCount | type == "number" and . >= 0 and floor == .) and
    (.remainingCount | type == "number" and . >= 0 and floor == .) and
    (.hasMore | type == "boolean")
  ' "$response_file" >/dev/null 2>&1
}

call_backfill() {
  local batch_size="$1"
  local operation_name="$2"
  local attempt=0
  local curl_exit=0
  local http_status=""
  local retry_delay=0

  while :; do
    : >"$response_file"
    curl_exit=0
    http_status="$(curl --silent --show-error \
      --connect-timeout 10 \
      --max-time "$REQUEST_TIMEOUT_SECONDS" \
      --output "$response_file" \
      --write-out '%{http_code}' \
      --request POST \
      --header 'Content-Type: application/json' \
      --data "{\"batchSize\":${batch_size}}" \
      "$BACKFILL_URL")" || curl_exit=$?

    if [ "$curl_exit" -eq 0 ] && [[ "$http_status" =~ ^2[0-9][0-9]$ ]] && validate_response; then
      RESPONSE_PROCESSED="$(jq -r '.processedCount' "$response_file")"
      RESPONSE_REMAINING="$(jq -r '.remainingCount' "$response_file")"
      RESPONSE_HAS_MORE="$(jq -r '.hasMore' "$response_file")"
      return 0
    fi

    printf '%s failed (attempt %d/%d).\n' "$operation_name" "$((attempt + 1))" "$((MAX_RETRIES + 1))" >&2
    if [ "$curl_exit" -ne 0 ]; then
      printf 'Connection/curl error code: %d\n' "$curl_exit" >&2
    elif ! [[ "$http_status" =~ ^2[0-9][0-9]$ ]]; then
      printf 'HTTP status: %s\n' "${http_status:-unknown}" >&2
      print_response_error
    else
      printf 'Response is not valid JSON or is missing processedCount, remainingCount, or hasMore.\n' >&2
      print_response_error
    fi

    if [ "$attempt" -ge "$MAX_RETRIES" ]; then
      printf 'Maximum retries exhausted. Backfill stopped; rerun the same command to resume.\n' >&2
      return 1
    fi

    retry_delay="${RETRY_DELAYS[$attempt]}"
    printf 'Retrying the same operation in %s seconds...\n' "$retry_delay" >&2
    sleep "$retry_delay"
    attempt=$((attempt + 1))
  done
}

print_summary() {
  local finished_at elapsed minutes seconds
  finished_at="$(date +%s)"
  elapsed=$((finished_at - started_at))
  minutes=$((elapsed / 60))
  seconds=$((elapsed % 60))

  printf '%s\n' '========================================'
  printf '%s\n' 'Semantic embedding backfill completed'
  printf '%s\n' '========================================'
  printf 'Processed this run: %d\n' "$total_processed"
  printf 'Remaining: %d\n' "$last_remaining"
  printf 'Batches: %d\n' "$batch_count"
  printf 'Elapsed: %dm %ds\n' "$minutes" "$seconds"
  printf '%s\n' '========================================'
}

require_command curl
require_command jq

printf '%s\n' 'Semantic embedding backfill'
printf 'Endpoint: %s\n\n' "$BACKFILL_URL"

printf '%s\n' 'Running pre-flight check with batchSize=1...'
call_backfill 1 'Pre-flight check'
total_processed=$((total_processed + RESPONSE_PROCESSED))
last_remaining="$RESPONSE_REMAINING"
printf '%s\n' 'Pre-flight check successful.'
printf 'Processed during pre-flight: %d\n' "$RESPONSE_PROCESSED"
printf 'Remaining: %d\n\n' "$last_remaining"

if [ "$RESPONSE_HAS_MORE" = "false" ] || [ "$last_remaining" -eq 0 ]; then
  print_summary
  exit 0
fi

while :; do
  batch_count=$((batch_count + 1))
  call_backfill 100 "Batch ${batch_count}"

  total_processed=$((total_processed + RESPONSE_PROCESSED))
  last_remaining="$RESPONSE_REMAINING"

  printf 'Batch %d\n' "$batch_count"
  printf 'Processed this batch: %d\n' "$RESPONSE_PROCESSED"
  printf 'Total processed this run: %d\n' "$total_processed"
  printf 'Remaining: %d\n\n' "$last_remaining"

  if [ "$RESPONSE_HAS_MORE" = "false" ] || [ "$last_remaining" -eq 0 ]; then
    break
  fi

  if [ "$RESPONSE_PROCESSED" -eq 0 ]; then
    printf 'Backend reported remaining work but processed zero books. Stopping to avoid an infinite loop.\n' >&2
    exit 1
  fi

  sleep "$BATCH_DELAY_SECONDS"
done

print_summary
