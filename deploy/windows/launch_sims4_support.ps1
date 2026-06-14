param(
    [string]$BasePath = "C:\Users\lancer1977\game_servers\sims4-support",
    [string]$WebKey = "sims4-support-stream-box",
    [string]$HubUrl = "http://127.0.0.1:5230/signalr",
    [int]$Port = 5230
)

$ErrorActionPreference = "Stop"

Set-Location $BasePath
$env:ASPNETCORE_URLS = "http://+:{0}" -f $Port
$env:SIMS4_SUPPORT_WEB_KEY = $WebKey
$env:SIMS4_SUPPORT_HUB_URL = $HubUrl

$stdout = Join-Path $BasePath "stdout.log"
$stderr = Join-Path $BasePath "stderr.log"
$app = Join-Path $BasePath "PolyhydraGames.Sims4.Bridge.exe"
$wrapper = Join-Path $BasePath "launch_sims4_support.cmd"

$wrapperBody = @"
@echo off
set ASPNETCORE_URLS=http://+:$Port
set SIMS4_SUPPORT_WEB_KEY=$WebKey
set SIMS4_SUPPORT_HUB_URL=$HubUrl
cd /d "$BasePath"
"$app" 1>>"$stdout" 2>>"$stderr"
"@
Set-Content -Path $wrapper -Value $wrapperBody -Encoding Ascii

cmd /c "taskkill /F /IM PolyhydraGames.Sims4.Bridge.exe /T >NUL 2>NUL" | Out-Null

for ($attempt = 0; $attempt -lt 15; $attempt++) {
    $existingListeners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    if ($existingListeners.Count -eq 0) {
        break
    }

    foreach ($listener in $existingListeners) {
        Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
    }

    Start-Sleep -Seconds 1
}

Write-Output "Prepared scheduled-task wrapper at $wrapper"
