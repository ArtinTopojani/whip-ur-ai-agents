@echo off
setlocal
cd /d "%~dp0"

set "APP_NAME=Whip Cursor"
set "INSTALL_DIR=%LOCALAPPDATA%\WhipCursor"
set "INSTALLED_EXE=%INSTALL_DIR%\WhipCursor.exe"
set "PUBLISH_DIR=%~dp0dist\publish\WhipCursor-win-x64"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo Whip Cursor needs the .NET SDK once to build itself from the GitHub ZIP.
  echo.
  echo After it builds, the installed app can run from your Desktop shortcut.
  echo.
  choice /C YN /M "Open the official .NET SDK download page now"
  if errorlevel 2 exit /b 1
  start "" "https://dotnet.microsoft.com/download/dotnet/8.0"
  exit /b 1
)

echo This will build and install Whip Cursor for this Windows user.
echo.
echo It may download Microsoft runtime files once from NuGet.org.
echo After install, you can start Whip Cursor from the Desktop shortcut.
echo.
choice /C YN /M "Continue"
if errorlevel 2 exit /b 1

set "APPDATA=%~dp0.appdata"
set "NUGET_PACKAGES=%~dp0.nuget\packages"
if not exist "%APPDATA%\NuGet" mkdir "%APPDATA%\NuGet"
copy /Y "%~dp0NuGet.Portable.Config" "%APPDATA%\NuGet\NuGet.Config" >nul

echo.
echo Building Whip Cursor...
dotnet restore "%~dp0WhipCursor.csproj" --runtime win-x64 --configfile "%~dp0NuGet.Portable.Config"
if errorlevel 1 pause & exit /b 1

dotnet publish "%~dp0WhipCursor.csproj" --configuration Release --runtime win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false --output "%PUBLISH_DIR%"
if errorlevel 1 pause & exit /b 1

echo.
echo Installing Whip Cursor...
taskkill /IM WhipCursor.exe /F >nul 2>nul

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
robocopy "%PUBLISH_DIR%" "%INSTALL_DIR%" /MIR >nul
if %ERRORLEVEL% GEQ 8 (
  echo Install failed while copying files.
  pause
  exit /b 1
)

if exist "%~dp0installer\Uninstall Whip Cursor.cmd" copy /Y "%~dp0installer\Uninstall Whip Cursor.cmd" "%INSTALL_DIR%\Uninstall Whip Cursor.cmd" >nul

set "WHIP_EXE=%INSTALLED_EXE%"
set "WHIP_DIR=%INSTALL_DIR%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$shell=New-Object -ComObject WScript.Shell; $start=Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\Whip Cursor'; New-Item -ItemType Directory -Force -Path $start | Out-Null; $app=$shell.CreateShortcut((Join-Path $start 'Whip Cursor.lnk')); $app.TargetPath=$env:WHIP_EXE; $app.WorkingDirectory=$env:WHIP_DIR; $app.Save(); $uninstaller=Join-Path $env:WHIP_DIR 'Uninstall Whip Cursor.cmd'; if (Test-Path $uninstaller) { $un=$shell.CreateShortcut((Join-Path $start 'Uninstall Whip Cursor.lnk')); $un.TargetPath=$uninstaller; $un.WorkingDirectory=$env:WHIP_DIR; $un.Save() }; $desktop=Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'Whip Cursor.lnk'; $desk=$shell.CreateShortcut($desktop); $desk.TargetPath=$env:WHIP_EXE; $desk.WorkingDirectory=$env:WHIP_DIR; $desk.Save()"
if errorlevel 1 (
  echo Installed, but shortcut creation failed.
  echo You can still run:
  echo %INSTALLED_EXE%
  pause
  exit /b 1
)

start "" "%INSTALLED_EXE%"

echo.
echo Done. Whip Cursor is installed and running.
echo.
echo Desktop shortcut created:
echo Whip Cursor
pause
