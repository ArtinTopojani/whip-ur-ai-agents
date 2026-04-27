@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo The .NET SDK is needed once to build the Whip Cursor release installer.
  echo.
  echo After the release is built, other PCs can install and run Whip Cursor without installing .NET.
  echo.
  choice /C YN /M "Open the .NET SDK download page now"
  if errorlevel 2 exit /b 1
  start "" "https://dotnet.microsoft.com/download/dotnet/8.0"
  exit /b 1
)

echo This will create a release-style installer folder.
echo.
echo It may download Microsoft runtime files once from NuGet.org.
echo The finished installer can be sent to other Windows PCs.
echo.
choice /C YN /M "Continue and allow downloads if needed"
if errorlevel 2 exit /b 1

set "APPDATA=%~dp0.appdata"
set "NUGET_PACKAGES=%~dp0.nuget\packages"
set "PUBLISH=%~dp0dist\publish\WhipCursor-win-x64"
set "RELEASE=%~dp0dist\WhipCursor-Installer"
set "ZIP=%~dp0dist\WhipCursor-Installer.zip"

if not exist "%APPDATA%\NuGet" mkdir "%APPDATA%\NuGet"
copy /Y "%~dp0NuGet.Portable.Config" "%APPDATA%\NuGet\NuGet.Config" >nul

dotnet restore "%~dp0WhipCursor.csproj" --runtime win-x64 --configfile "%~dp0NuGet.Portable.Config"
if errorlevel 1 pause & exit /b 1

dotnet publish "%~dp0WhipCursor.csproj" --configuration Release --runtime win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false --output "%PUBLISH%"
if errorlevel 1 pause & exit /b 1

if exist "%RELEASE%" rmdir /S /Q "%RELEASE%"
mkdir "%RELEASE%\app"

robocopy "%PUBLISH%" "%RELEASE%\app" /E >nul
if %ERRORLEVEL% GEQ 8 (
  echo Failed to prepare installer app files.
  pause
  exit /b 1
)

copy /Y "%~dp0installer\Install Whip Cursor.cmd" "%RELEASE%\Install Whip Cursor.cmd" >nul
copy /Y "%~dp0installer\Uninstall Whip Cursor.cmd" "%RELEASE%\Uninstall Whip Cursor.cmd" >nul

if exist "%ZIP%" del /Q "%ZIP%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%RELEASE%\*' -DestinationPath '%ZIP%' -Force"

echo.
echo Release installer created:
echo %RELEASE%
echo.
echo Zip created:
echo %ZIP%
echo.
echo Send the zip or folder to another Windows PC.
echo The user should run "Install Whip Cursor.cmd".
pause
