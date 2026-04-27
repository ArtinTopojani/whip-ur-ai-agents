@echo off
setlocal

set "INSTALL_DIR=%LOCALAPPDATA%\WhipCursor"
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Whip Cursor"
set "DESKTOP_SHORTCUT=%USERPROFILE%\Desktop\Whip Cursor.lnk"

echo Uninstalling Whip Cursor...
echo.

taskkill /IM WhipCursor.exe /F >nul 2>nul

if exist "%DESKTOP_SHORTCUT%" del /Q "%DESKTOP_SHORTCUT%" >nul 2>nul
if exist "%START_MENU%" rmdir /S /Q "%START_MENU%" >nul 2>nul

cd /d "%TEMP%"
if exist "%INSTALL_DIR%" rmdir /S /Q "%INSTALL_DIR%" >nul 2>nul

echo Uninstalled.
pause
