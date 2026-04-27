@echo off
setlocal
cd /d "%~dp0.."
set "ROOT=%CD%"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo The .NET SDK is needed to build Whip Cursor from source.
  echo.
  choice /C YN /M "Open the .NET SDK download page now"
  if errorlevel 2 exit /b 1
  start "" "https://dotnet.microsoft.com/download/dotnet/8.0"
  exit /b 1
)

set "APPDATA=%ROOT%\.appdata"
set "NUGET_PACKAGES=%ROOT%\.nuget\packages"
if not exist "%APPDATA%\NuGet" mkdir "%APPDATA%\NuGet"
copy /Y "%ROOT%\NuGet.Config" "%APPDATA%\NuGet\NuGet.Config" >nul
dotnet restore "%ROOT%\WhipCursor.csproj" --configfile "%ROOT%\NuGet.Config"
if errorlevel 1 pause & exit /b 1
dotnet build "%ROOT%\WhipCursor.csproj" --no-restore
pause
