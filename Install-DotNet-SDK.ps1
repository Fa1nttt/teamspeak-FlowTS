$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnetDir = Join-Path $root '.dotnet'
$installScript = Join-Path $root '.dotnet-install.ps1'
New-Item -ItemType Directory -Force -Path $dotnetDir | Out-Null
Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installScript
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installScript -Channel 8.0 -InstallDir $dotnetDir -Architecture x64
& (Join-Path $dotnetDir 'dotnet.exe') --info
