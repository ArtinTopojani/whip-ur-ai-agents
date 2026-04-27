@echo off
setlocal

set "INSTALL_DIR=%LOCALAPPDATA%\WhipCursor"
set "INSTALLED_EXE=%INSTALL_DIR%\WhipCursor.exe"

if exist "%INSTALLED_EXE%" (
  start "" "%INSTALLED_EXE%"
  exit /b 0
)

echo Whip Cursor is not installed yet.
echo.
choice /C YN /M "Run the installer now"
if errorlevel 2 exit /b 1

call "%~dp0Install Whip Cursor.cmd"
