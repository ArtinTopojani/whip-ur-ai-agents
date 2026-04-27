@echo off
setlocal
cd /d "%~dp0"

set "SOURCE=%~dp0app"
set "INSTALL_DIR=%LOCALAPPDATA%\WhipCursor"
set "EXE=%INSTALL_DIR%\WhipCursor.exe"

if not exist "%SOURCE%\WhipCursor.exe" (
  echo Whip Cursor app files were not found.
  echo.
  echo Make sure this installer still has its app folder next to it.
  pause
  exit /b 1
)

echo Installing Whip Cursor...
echo.

taskkill /IM WhipCursor.exe /F >nul 2>nul

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
robocopy "%SOURCE%" "%INSTALL_DIR%" /MIR >nul
if %ERRORLEVEL% GEQ 8 (
  echo Install failed while copying files.
  pause
  exit /b 1
)

if exist "%~dp0Uninstall Whip Cursor.cmd" copy /Y "%~dp0Uninstall Whip Cursor.cmd" "%INSTALL_DIR%\Uninstall Whip Cursor.cmd" >nul

set "WHIP_EXE=%EXE%"
set "WHIP_DIR=%INSTALL_DIR%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$shell=New-Object -ComObject WScript.Shell; $start=Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\Whip Cursor'; New-Item -ItemType Directory -Force -Path $start | Out-Null; $app=$shell.CreateShortcut((Join-Path $start 'Whip Cursor.lnk')); $app.TargetPath=$env:WHIP_EXE; $app.WorkingDirectory=$env:WHIP_DIR; $app.Save(); $uninstaller=Join-Path $env:WHIP_DIR 'Uninstall Whip Cursor.cmd'; if (Test-Path $uninstaller) { $un=$shell.CreateShortcut((Join-Path $start 'Uninstall Whip Cursor.lnk')); $un.TargetPath=$uninstaller; $un.WorkingDirectory=$env:WHIP_DIR; $un.Save() }; $desktop=Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'Whip Cursor.lnk'; $desk=$shell.CreateShortcut($desktop); $desk.TargetPath=$env:WHIP_EXE; $desk.WorkingDirectory=$env:WHIP_DIR; $desk.Save()"
if errorlevel 1 (
  echo Installed, but shortcut creation failed.
  echo You can still run:
  echo %EXE%
  pause
  exit /b 1
)

start "" "%EXE%"

echo Installed.
echo.
echo Whip Cursor is now running from:
echo %INSTALL_DIR%
pause
