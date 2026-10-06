#!/bin/zsh
# Builds the mobile store packages into releases/:
#   releases/android/*.aab  (upload to Google Play)
#   releases/android/*.apk  (for side-loading / testing)
#   releases/ios/*.ipa      (upload to App Store Connect)
# Android signing uses ~/keys/secrettomb-upload.jks; its password is read from the macOS Keychain
# (service "SecretTomb Android upload keystore"). iOS signing uses "Apple Distribution: Paul Johnson
# (3UH7BE38T3)" and the rel-secrettomb profile (~/Downloads/relsecrettomb.mobileprovision, installed by this script).
#
#   tools/build_release.sh        Android and iOS
#   tools/build_release.sh ios    iOS only (no Android upload key needed)
set -euo pipefail
cd "$(dirname "$0")/.."

# Xcode's tools find provisioning profiles by name in these folders.
for f in relsecrettomb develsecrettomb; do
  [[ -f ~/Downloads/$f.mobileprovision ]] || continue
  uuid=$(security cms -D -i ~/Downloads/$f.mobileprovision | plutil -extract UUID raw -)
  for dir in "$HOME/Library/MobileDevice/Provisioning Profiles" "$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles"; do
    mkdir -p "$dir" && cp ~/Downloads/$f.mobileprovision "$dir/$uuid.mobileprovision"
  done
done

rm -rf releases/ios
dotnet publish SecretTomb.iOS/SecretTomb.iOS.csproj -c Release -f net10.0-ios -o releases/ios
if [[ ${1:-} == ios ]]; then
  ls -la releases/ios/*.ipa
  exit 0
fi

export SECRETTOMB_KEYSTORE_PASS="$(security find-generic-password -a secrettomb-upload -s 'SecretTomb Android upload keystore' -w)"
SDK="${ANDROID_HOME:-$HOME/Library/Android/sdk}"

rm -rf releases/android
for format in aab apk; do
  dotnet publish SecretTomb.Android/SecretTomb.Android.csproj -c Release -f net10.0-android \
    -p:AndroidPackageFormat=$format -p:AndroidSdkDirectory="$SDK" -o releases/android/$format
done
mkdir -p releases/android/out
mv releases/android/aab/*-Signed.aab releases/android/apk/*-Signed.apk releases/android/out/
rm -rf releases/android/aab releases/android/apk
mv releases/android/out/* releases/android/ && rmdir releases/android/out

ls -la releases/android releases/ios/*.ipa
