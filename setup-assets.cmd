@echo off
setlocal
cd /d "%~dp0"

git lfs version >nul 2>&1
if errorlevel 1 (
    echo Install Git LFS from https://git-lfs.com/ and run this script again.
    exit /b 1
)

set "GIT_LFS_SKIP_SMUDGE="
set "GIT_LFS_SKIP_DOWNLOAD_ERRORS="
git lfs install --local
if errorlevel 1 exit /b 1
git -c lfs.fetchinclude= -c lfs.fetchexclude= -c lfs.skipdownloaderrors=false lfs pull
if errorlevel 1 exit /b 1
git lfs fsck --objects --pointers HEAD
if errorlevel 1 exit /b 1
git lfs ls-files | findstr /R /C:"^[0-9a-f][0-9a-f]* - " >nul
if not errorlevel 1 (
    echo Some assets are still LFS pointers. Close Unity and check the download errors.
    exit /b 1
)
echo Assets downloaded and verified. Open this folder in Unity 2022.3.62f2.
exit /b 0
