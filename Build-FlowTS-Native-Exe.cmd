@echo off
setlocal
cd /d "%~dp0"
set "DOTNET_ROOT=%~dp0.dotnet"
set "DOTNET_EXE=%DOTNET_ROOT%\dotnet.exe"
if exist "%DOTNET_EXE%" (
  set "PATH=%DOTNET_ROOT%;%PATH%"
) else (
  set "DOTNET_EXE=dotnet"
)
"%DOTNET_EXE%" --list-sdks >nul 2>nul
if errorlevel 1 (
  echo .NET SDK not found. Run Install-DotNet-SDK.cmd first, then run this build script again.
  pause
  exit /b 1
)
if exist "%~dp0dist\FlowTS" rmdir /s /q "%~dp0dist\FlowTS"
"%DOTNET_EXE%" publish "%~dp0FlowTS.Native\FlowTS.Native.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=false -o "%~dp0dist\FlowTS"
if errorlevel 1 pause
