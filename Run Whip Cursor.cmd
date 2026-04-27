@echo off
setlocal
cd /d "%~dp0"

set "PORTABLE_EXE=%~dp0dist\WhipCursor-win-x64\WhipCursor.exe"
if exist "%PORTABLE_EXE%" (
  start "" "%PORTABLE_EXE%"
  exit /b 0
)

where dotnet >nul 2>nul
if errorlevel 1 (
  echo Whip Cursor has not been built on this PC yet.
  echo.
  echo This source version needs the .NET SDK to build and run.
  echo For other PCs, use "Publish Portable Whip Cursor.cmd" first and share the dist\WhipCursor-win-x64 folder.
  echo.
  choice /C YN /M "Open the .NET SDK download page now"
  if errorlevel 2 exit /b 1
  start "" "https://dotnet.microsoft.com/download/dotnet/8.0"
  exit /b 1
)

set "APPDATA=%~dp0.appdata"
set "NUGET_PACKAGES=%~dp0.nuget\packages"
if not exist "%APPDATA%\NuGet" mkdir "%APPDATA%\NuGet"
copy /Y "%~dp0NuGet.Config" "%APPDATA%\NuGet\NuGet.Config" >nul
dotnet restore "%~dp0WhipCursor.csproj" --configfile "%~dp0NuGet.Config" >nul
if errorlevel 1 pause & exit /b 1
dotnet build "%~dp0WhipCursor.csproj" --no-restore >nul
if errorlevel 1 pause & exit /b 1
start "" "%~dp0bin\Debug\net8.0-windows\WhipCursor.exe"
