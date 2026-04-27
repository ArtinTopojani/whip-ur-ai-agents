@echo off
setlocal
cd /d "%~dp0"
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
