@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo The .NET SDK is needed once to create the portable build.
  echo.
  echo After the portable build is created, other PCs can run WhipCursor.exe without installing .NET.
  echo.
  choice /C YN /M "Open the .NET SDK download page now"
  if errorlevel 2 exit /b 1
  start "" "https://dotnet.microsoft.com/download/dotnet/8.0"
  exit /b 1
)

echo This will create a portable Windows build.
echo.
echo It may download Microsoft runtime files once from NuGet.org.
echo After that, the finished dist folder should run on other PCs without installing .NET.
echo.
choice /C YN /M "Continue and allow downloads if needed"
if errorlevel 2 exit /b 1

set "APPDATA=%~dp0.appdata"
set "NUGET_PACKAGES=%~dp0.nuget\packages"
set "OUT=%~dp0dist\WhipCursor-win-x64"
if not exist "%APPDATA%\NuGet" mkdir "%APPDATA%\NuGet"
copy /Y "%~dp0NuGet.Portable.Config" "%APPDATA%\NuGet\NuGet.Config" >nul
dotnet restore "%~dp0WhipCursor.csproj" --runtime win-x64 --configfile "%~dp0NuGet.Portable.Config"
if errorlevel 1 pause & exit /b 1
dotnet publish "%~dp0WhipCursor.csproj" --configuration Release --runtime win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false --output "%OUT%"
if errorlevel 1 pause & exit /b 1
echo.
echo Portable build created:
echo %OUT%
echo.
echo Share that folder with another Windows PC and run WhipCursor.exe.
pause
