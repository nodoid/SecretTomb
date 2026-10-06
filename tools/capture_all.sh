#!/bin/zsh
# Records every store capture with the macOS (DesktopGL) build into <dir> (default: a temporary
# folder), then builds the store assets in stores/ and the app preview videos (also copied to ~/Movies).
#
#   tools/capture_all.sh [dir] [stills|videos]     ("stills" skips the videos, "videos" skips the stills)
#
# Stills: desktop and mobile prompts, English and French, at 2400 x 2240.
# Videos: one language and layout at a time (1200 x 1120 frames are large), encoded and deleted.
# Windows stills come from tools/build_windows.sh capture (releases/windows/capture), if present.
set -euo pipefail
cd "$(dirname "$0")/.."

CAP=${1:-$(mktemp -d)/capture}
mkdir -p "$CAP"
dotnet build SecretTomb.DesktopGL -c Release -v q -o "$CAP/bin" >/dev/null
GAME=("dotnet" "$CAP/bin/SecretTomb.dll")

if [[ ${2:-} != videos ]]; then
for lang in en fr; do
  "${GAME[@]}" --capture "$CAP/$lang/desktop" stills --lang $lang
  "${GAME[@]}" --capture "$CAP/$lang/mobile" stills --mobile --lang $lang
  if [[ -d releases/windows/capture/$lang/stills ]]; then
    mkdir -p "$CAP/$lang/windows" && cp -R releases/windows/capture/$lang/stills "$CAP/$lang/windows/"
  fi
done
python3 tools/make_store_assets.py stills "$CAP"
fi
[[ ${2:-} == stills ]] && exit 0

for lang in en fr; do
  for layout in mobile desktop; do
    flags=(--lang $lang)
    [[ $layout == mobile ]] && flags+=(--mobile)
    "${GAME[@]}" --capture "$CAP/$lang/video-$layout" videos "${flags[@]}"
    python3 tools/make_store_assets.py video "$CAP" $lang $layout
    rm -rf "$CAP/$lang/video-$layout"
  done
done
echo "captures in $CAP"
