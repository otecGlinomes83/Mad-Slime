#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")"

if ! git lfs version >/dev/null 2>&1; then
    printf >&2 'Install Git LFS from https://git-lfs.com/ and run this script again.\n'
    exit 1
fi

unset GIT_LFS_SKIP_SMUDGE GIT_LFS_SKIP_DOWNLOAD_ERRORS
git lfs install --local
# Download everything required by the current checkout, even if this machine
# has global include/exclude rules or previously disabled automatic downloads.
git -c lfs.fetchinclude= -c lfs.fetchexclude= -c lfs.skipdownloaderrors=false lfs pull
git lfs fsck --objects --pointers HEAD
files=$(git lfs ls-files)
if printf '%s\n' "$files" | grep -Eq '^[0-9a-f]+ - '; then
    printf >&2 'Some assets are still LFS pointers. Close Unity and check the download errors.\n'
    exit 1
fi
printf 'Assets downloaded and verified. Open this folder in Unity 2022.3.62f2.\n'
