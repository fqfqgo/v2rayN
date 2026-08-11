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
work_dir=$(mktemp -d)
trap 'rm -rf "${work_dir}"' EXIT

# Official installer SFX lives in LZMA SDK (7-Zip Extra no longer ships it).
sdk_ver="2501"
sdk_url="https://www.7-zip.org/a/lzma${sdk_ver}.7z"
sdk_archive="${work_dir}/lzma-sdk.7z"

echo "Downloading LZMA SDK ${sdk_ver}..."
wget -nv -O "${sdk_archive}" "${sdk_url}"
7z e -y "-o${work_dir}" "${sdk_archive}" bin/7zSD.sfx >/dev/null

mkdir -p "${work_dir}/payload"
cp -a "${payload_dir}/." "${work_dir}/payload/"
cp -f "${sfx_dir}/install.ps1" "${work_dir}/payload/install.ps1"
cp -f "${sfx_dir}/install.cmd" "${work_dir}/payload/install.cmd"
cp -f "${sfx_dir}/config.txt" "${work_dir}/config.txt"

# Compress payload; publish folder files are not pre-compressed.
(
  cd "${work_dir}/payload"
  7z a -t7z -mx=7 -m0=lzma2 "${work_dir}/payload.7z" . >/dev/null
)

mkdir -p "$(dirname "${output_exe}")"
cat "${work_dir}/7zSD.sfx" "${work_dir}/config.txt" "${work_dir}/payload.7z" > "${output_exe}"
chmod 0644 "${output_exe}"
echo "Created ${output_exe} ($(wc -c < "${output_exe}") bytes)"
