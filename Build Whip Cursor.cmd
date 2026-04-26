@echo off
setlocal
cd /d "%~dp0"
set "APPDATA=%~dp0.appdata"
set "NUGET_PACKAGES=%~dp0.nuget\packages"
if not exist "%APPDATA%\NuGet" mkdir "%APPDATA%\NuGet"
copy /Y "%~dp0NuGet.Config" "%APPDATA%\NuGet\NuGet.Config" >nul
dotnet restore "%~dp0WhipCursor.csproj" --configfile "%~dp0NuGet.Config"
if errorlevel 1 pause & exit /b 1
dotnet build "%~dp0WhipCursor.csproj" --no-restore
pause
