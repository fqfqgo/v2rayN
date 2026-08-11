#!/usr/bin/env bash
# Build a per-user Windows SFX installer from an unpacked v2rayN publish folder.
# Usage: build-windows-sfx.sh <payload_dir> <output_exe>
set -euo pipefail

payload_dir=${1:-}
output_exe=${2:-}
if [[ -z "${payload_dir}" || -z "${output_exe}" ]]; then
  echo "Usage: $0 <payload_dir> <output_exe>" >&2
  exit 1
fi
if [[ ! -d "${payload_dir}" ]]; then
  echo "payload_dir not found: ${payload_dir}" >&2
  exit 1
fi
if [[ ! -f "${payload_dir}/v2rayN.exe" ]]; then
  echo "v2rayN.exe not found in ${payload_dir}" >&2
  exit 1
fi

root_dir=$(cd "$(dirname "$0")/.." && pwd)
sfx_dir="${root_dir}/scripts/windows-sfx"
sfx_module="${sfx_dir}/7zSD-v2rayN.sfx"
if [[ ! -f "${sfx_module}" ]]; then
  echo "missing branded SFX module: ${sfx_module}" >&2
  exit 1
fi

work_dir=$(mktemp -d)
trap 'rm -rf "${work_dir}"' EXIT

mkdir -p "${work_dir}/payload"
cp -a "${payload_dir}/." "${work_dir}/payload/"
cp -f "${sfx_dir}/install.ps1" "${work_dir}/payload/install.ps1"
cp -f "${sfx_dir}/install.cmd" "${work_dir}/payload/install.cmd"
cp -f "${sfx_dir}/uninstall.ps1" "${work_dir}/payload/uninstall.ps1"
cp -f "${sfx_dir}/config.txt" "${work_dir}/config.txt"

(
  cd "${work_dir}/payload"
  7z a -t7z -mx=7 -m0=lzma2 "${work_dir}/payload.7z" . >/dev/null
)

mkdir -p "$(dirname "${output_exe}")"
cat "${sfx_module}" "${work_dir}/config.txt" "${work_dir}/payload.7z" > "${output_exe}"
chmod 0644 "${output_exe}"
echo "Created ${output_exe} ($(wc -c < "${output_exe}") bytes)"
