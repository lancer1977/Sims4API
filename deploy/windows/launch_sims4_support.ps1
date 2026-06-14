param(
    [string]$BasePath = "C:\Users\lancer1977\game_servers\sims4-support",
    [string]$WebKey = "sims4-support-stream-box",
    [string]$HubUrl = "http://127.0.0.1:5230/signalr"
)

$ErrorActionPreference = "Stop"

Set-Location $BasePath
$env:ASPNETCORE_URLS = "http://+:5230"
$env:SIMS4_SUPPORT_WEB_KEY = $WebKey
$env:SIMS4_SUPPORT_HUB_URL = $HubUrl

$stdout = Join-Path $BasePath "stdout.log"
$stderr = Join-Path $BasePath "stderr.log"
$app = Join-Path $BasePath "PolyhydraGames.Sims4.Bridge.exe"
Start-Process -FilePath $app -WorkingDirectory $BasePath -RedirectStandardOutput $stdout -RedirectStandardError $stderr
