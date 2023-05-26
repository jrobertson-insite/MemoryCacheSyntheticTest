Push-Location -Path C:\inetpub\wwwroot\

if (-not (Test-Path "C:/inetpub/wwwroot/App_Log")) {
  New-Item "C:/inetpub/wwwroot/App_Log" -Type Directory
}

icacls "C:/inetpub/wwwroot/App_Log" /t /grant 'IIS_IUSRS:(F)'

./UpdateNamedCacheSettings.ps1

C:\ServiceMonitor.exe w3svc
