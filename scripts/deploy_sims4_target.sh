#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
TARGET="${1:-${SIMS4_SUPPORT_TARGET:-stream-box}}"
COMPOSE_FILE="deploy/compose/docker-compose.yml"
HEALTH_URL="${SIMS4_SUPPORT_HEALTH_URL:-http://127.0.0.1:5230/healthz}"
STATE_URL="${SIMS4_SUPPORT_STATE_URL:-http://127.0.0.1:5230/state}"
VERSION_URL="${SIMS4_SUPPORT_VERSION_URL:-http://127.0.0.1:5230/version}"
CONTAINER_NAME="${SIMS4_SUPPORT_CONTAINER:-sims4-support}"

case "${TARGET}" in
  stream-box)
    TARGET_LABEL="stream-box"
  TARGET_KIND="windows"
  HOST="${SIMS4_SUPPORT_HOST:-192.168.0.178}"
  REMOTE_ROOT_UNIX="${SIMS4_SUPPORT_ROOT:-C:/Users/lancer1977/game_servers/sims4-support}"
  REMOTE_ROOT_CMD="${SIMS4_SUPPORT_ROOT_CMD:-C:\\Users\\lancer1977\\game_servers\\sims4-support}"
  SUPPORT_WEB_KEY="${SIMS4_SUPPORT_WEB_KEY:-sims4-support-stream-box}"
  REMOTE_LAUNCHER="${REPO_ROOT}/deploy/windows/launch_sims4_support.ps1"
  PUBLISH_DIR=$(mktemp -d "${TMPDIR:-/tmp}/sims4-stream-box.XXXXXX")
  ARCHIVE_PATH="${TMPDIR:-/tmp}/sims4-stream-box-$$.zip"
  ;;
  alienware-252|252)
    TARGET_LABEL="alienware-252"
    TARGET_KIND="linux-docker"
    HOST="${SIMS4_SUPPORT_HOST:-192.168.0.252}"
    REMOTE_ROOT="${SIMS4_SUPPORT_ROOT:-/home/lancer1977/game_servers/sims4-support}"
    SUPPORT_WEB_KEY="${SIMS4_SUPPORT_WEB_KEY:-sims4-support-252}"
    ;;
  *)
    echo "Unknown Sims4 deploy target: ${TARGET}" >&2
    echo "Supported targets: stream-box, alienware-252" >&2
    exit 1
    ;;
esac

if [ "${TARGET_KIND}" = "windows" ]; then
  wait_for_health() {
    local description="$1"
    local url="$2"
    local filter="$3"

    for attempt in $(seq 1 60); do
      local body
      if body=$(ssh "${HOST}" "powershell -NoProfile -Command \"(Invoke-WebRequest -UseBasicParsing -Uri '${url}').Content\"" 2>/dev/null) && printf '%s' "$body" | jq -e "$filter" >/dev/null; then
        return 0
      fi
      sleep 2
    done

    echo "Timed out waiting for ${description} on ${HOST}" >&2
    ssh "${HOST}" "powershell -NoProfile -Command \"(Invoke-WebRequest -UseBasicParsing -Uri '${url}').Content\"" >&2 || true
    exit 1
  }
else
  wait_for_health() {
    local description="$1"
    local url="$2"
    local filter="$3"

    for attempt in $(seq 1 60); do
      local body
      if body=$(ssh "${HOST}" "curl -fsS '${url}'" 2>/dev/null) && printf '%s' "$body" | jq -e "$filter" >/dev/null; then
        return 0
      fi
      sleep 2
    done

    echo "Timed out waiting for ${description} on ${HOST}" >&2
    ssh "${HOST}" "curl -fsS '${url}'" >&2 || true
    exit 1
  }
fi

wait_for_container() {
  local description="$1"
  local container="$2"
  local expected_state="$3"

  for attempt in $(seq 1 60); do
    local state
    state=$(ssh "${HOST}" "cd '${REMOTE_ROOT}' && docker inspect --format '{{.State.Status}}' '${container}'" 2>/dev/null || true)
    if [ "${state}" = "${expected_state}" ]; then
      return 0
    fi
    sleep 2
  done

  echo "Timed out waiting for ${description} on ${HOST}" >&2
  ssh "${HOST}" "cd '${REMOTE_ROOT}' && docker inspect --format 'name={{.Name}} image={{.Config.Image}} status={{.State.Status}} started={{.State.StartedAt}}' '${container}'" >&2 || true
  ssh "${HOST}" "cd '${REMOTE_ROOT}' && docker logs --tail 100 '${container}'" >&2 || true
  exit 1
}

  echo "[1/5] Verifying the deploy path for ${TARGET_LABEL}"
  if [ "${TARGET_KIND}" = "windows" ]; then
    dotnet publish "${REPO_ROOT}/Sims4.SignalR/PolyhydraGames.Sims4.Bridge.csproj" \
      --configuration Release \
      --runtime win-x64 \
      --self-contained true \
      --output "${PUBLISH_DIR}" \
    /p:UseAppHost=true >/dev/null
    cp "${REMOTE_LAUNCHER}" "${PUBLISH_DIR}/launch_sims4_support.ps1"
    rm -f "${ARCHIVE_PATH}"
    (cd "${PUBLISH_DIR}" && zip -qr "${ARCHIVE_PATH}" .)
  else
    SIMS4_SUPPORT_WEB_KEY="${SUPPORT_WEB_KEY}" \
      SIMS4_SUPPORT_HUB_URL="${SIMS4_SUPPORT_HUB_URL:-http://127.0.0.1:5230/signalr}" \
      docker compose -f "${REPO_ROOT}/${COMPOSE_FILE}" config >/dev/null
  fi

  if [ "${TARGET_KIND}" = "windows" ]; then
  echo "[2/5] Syncing the published bridge archive to ${HOST}:${REMOTE_ROOT_UNIX}"
  ssh "${HOST}" "powershell -NoProfile -Command \"New-Item -ItemType Directory -Force -Path '${REMOTE_ROOT_CMD}' | Out-Null\""
  ssh "${HOST}" "powershell -NoProfile -Command \"Get-Process PolyhydraGames.Sims4.Bridge -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id \$_ -Force }; exit 0\""
  scp "${ARCHIVE_PATH}" "${HOST}:${REMOTE_ROOT_UNIX}/sims4-support.zip"
  ssh "${HOST}" "powershell -NoProfile -Command \"Expand-Archive -Force -Path '${REMOTE_ROOT_CMD}\\sims4-support.zip' -DestinationPath '${REMOTE_ROOT_CMD}'\""

  echo "[3/5] Starting the resident app on ${HOST}"
  ssh "${HOST}" "powershell -NoProfile -ExecutionPolicy Bypass -File ${REMOTE_ROOT_CMD}\\launch_sims4_support.ps1 -BasePath ${REMOTE_ROOT_CMD} -WebKey ${SUPPORT_WEB_KEY} -HubUrl ${SIMS4_SUPPORT_HUB_URL:-http://127.0.0.1:5230/signalr}"
else
  echo "[2/5] Syncing the repo to ${HOST}:${REMOTE_ROOT}"
  ssh "${HOST}" "mkdir -p '${REMOTE_ROOT}'"
  rsync -az --delete --exclude '.git' --exclude 'bin' --exclude 'obj' --exclude '.vs' "${REPO_ROOT}/" "${HOST}:${REMOTE_ROOT}/"

  echo "[3/5] Bringing the support home up on ${HOST} for ${TARGET_LABEL}"
  ssh "${HOST}" "cd '${REMOTE_ROOT}' && SIMS4_SUPPORT_WEB_KEY='${SUPPORT_WEB_KEY}' SIMS4_SUPPORT_HUB_URL='${SIMS4_SUPPORT_HUB_URL:-http://127.0.0.1:5230/signalr}' docker compose -f '${COMPOSE_FILE}' up -d --build"
fi

echo "[4/5] Waiting for the Sims4 bridge container and health endpoint"
if [ "${TARGET_KIND}" = "windows" ]; then
  wait_for_health "bridge health" "${HEALTH_URL}" '.status == "ok" and .surface == "Sims4.SignalR" and .bridge == "connected"'
  wait_for_health "bridge state" "${STATE_URL}" '.surface == "Sims4.SignalR" and .hubUrl == "http://127.0.0.1:5230/signalr" and .connectedAt != null and .lastError == null'
  wait_for_health "bridge version" "${VERSION_URL}" '.status == "ok" and .surface == "Sims4.SignalR" and (.version | type == "string")'
else
  wait_for_container "Sims4 bridge container" "${CONTAINER_NAME}" "running"
  wait_for_health "bridge health" "${HEALTH_URL}" '.status == "ok" and .surface == "Sims4.SignalR" and .bridge == "connected"'
  wait_for_health "bridge state" "${STATE_URL}" '.surface == "Sims4.SignalR" and .hubUrl == "http://127.0.0.1:5230/signalr" and .connectedAt != null and .lastError == null'
  wait_for_health "bridge version" "${VERSION_URL}" '.status == "ok" and .surface == "Sims4.SignalR" and (.version | type == "string")'
fi

echo "[5/5] Recording the deployed lane"
if [ "${TARGET_KIND}" = "windows" ]; then
  ssh "${HOST}" "powershell -NoProfile -Command \"Get-NetTCPConnection -LocalPort 5230 -State Listen | Select-Object -First 1 | Format-Table -AutoSize LocalAddress,LocalPort,OwningProcess\""
  ssh "${HOST}" "powershell -NoProfile -Command \"Get-Process dotnet -ErrorAction SilentlyContinue | Select-Object -First 5 | Format-Table -AutoSize Id,ProcessName,Path\""
  ssh "${HOST}" "powershell -NoProfile -Command \"(Invoke-WebRequest -UseBasicParsing -Uri '${HEALTH_URL}').Content | ConvertFrom-Json | Select-Object status,bridge,surface | ConvertTo-Json -Compress\""
  ssh "${HOST}" "powershell -NoProfile -Command \"(Invoke-WebRequest -UseBasicParsing -Uri '${STATE_URL}').Content | ConvertFrom-Json | Select-Object surface,lastEventType,connectedAt | ConvertTo-Json -Compress\""
  ssh "${HOST}" "powershell -NoProfile -Command \"(Invoke-WebRequest -UseBasicParsing -Uri '${VERSION_URL}').Content | ConvertFrom-Json | Select-Object version,surface | ConvertTo-Json -Compress\""
else
  ssh "${HOST}" "cd '${REMOTE_ROOT}' && docker inspect --format 'name={{.Name}} image={{.Config.Image}} status={{.State.Status}} started={{.State.StartedAt}}' '${CONTAINER_NAME}'"
  ssh "${HOST}" "cd '${REMOTE_ROOT}' && docker logs --tail 100 '${CONTAINER_NAME}'"
  ssh "${HOST}" "cd '${REMOTE_ROOT}' && docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | grep -E '^(sims4-support)'"
  ssh "${HOST}" "curl -fsS '${HEALTH_URL}' | jq -r '.status + \" \" + .bridge + \" \" + .surface'"
  ssh "${HOST}" "curl -fsS '${STATE_URL}' | jq -r '.surface + \" \" + (.lastEventType // \"none\") + \" \" + (.connectedAt // \"none\")'"
  ssh "${HOST}" "curl -fsS '${VERSION_URL}' | jq -r '.version + \" \" + .surface'"
fi

echo "Sims4 ${TARGET_LABEL} deploy complete"
