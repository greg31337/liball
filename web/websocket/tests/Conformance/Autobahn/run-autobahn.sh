#!/usr/bin/env bash
set -euo pipefail

mode="${1:-all}"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_dir="$(cd "${script_dir}/../../.." && pwd)"
project="${script_dir}/websocket.Autobahn.csproj"
config_dir="${script_dir}/config"
reports_dir="${repo_dir}/artifacts/autobahn"
image="crossbario/autobahn-testsuite:25.10.1"
container_name="libs11-websocket-autobahn"
server_pid=""

cleanup() {
  if [[ -n "${server_pid}" ]]; then
    kill "${server_pid}" 2>/dev/null || true
    wait "${server_pid}" 2>/dev/null || true
  fi
  docker rm -f "${container_name}" >/dev/null 2>&1 || true
}
trap cleanup EXIT

verify_report() {
  dotnet run --project "${project}" --no-build -- verify "$1"
}

run_client_role() {
  mkdir -p "${reports_dir}/clients"
  docker run --rm --name "${container_name}" \
    -v "${config_dir}:/config:ro" \
    -v "${reports_dir}:/reports" \
    -p 9001:9001 \
    "${image}" \
    wstest -m fuzzingserver -s /config/fuzzingserver.json &
  container_pid=$!
  dotnet run --project "${project}" --no-build -- client ws://127.0.0.1:9001
  docker rm -f "${container_name}" >/dev/null
  wait "${container_pid}" 2>/dev/null || true
  verify_report "${reports_dir}/clients"
}

run_server_role() {
  mkdir -p "${reports_dir}/servers"
  dotnet run --project "${project}" --no-build -- server 0.0.0.0 9001 &
  server_pid=$!
  docker run --rm --name "${container_name}" \
    --add-host host.docker.internal:host-gateway \
    -v "${config_dir}:/config:ro" \
    -v "${reports_dir}:/reports" \
    "${image}" \
    wstest -m fuzzingclient -s /config/fuzzingclient.json
  kill "${server_pid}"
  wait "${server_pid}" 2>/dev/null || true
  server_pid=""
  verify_report "${reports_dir}/servers"
}

cd "${repo_dir}"
dotnet build "${project}"

case "${mode}" in
  client)
    run_client_role
    ;;
  server)
    run_server_role
    ;;
  all)
    run_client_role
    docker rm -f "${container_name}" >/dev/null 2>&1 || true
    run_server_role
    ;;
  verify)
    verify_report "${reports_dir}/clients"
    verify_report "${reports_dir}/servers"
    ;;
  *)
    echo "Usage: ${0##*/} [client|server|all|verify]" >&2
    exit 2
    ;;
esac
