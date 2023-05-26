Invoke-WebRequest -UseBasicParsing https://github.com/DataDog/dd-trace-dotnet/releases/download/v2.30.0/datadog-dotnet-apm-2.30.0-x64.msi -OutFile c:\datadog\datadog.msi

$datadogInstaller = "c:\datadog\datadog.msi"
if (Test-Path $datadogInstaller) {
  Start-Process -Wait msiexec -ArgumentList "/i $datadogInstaller /quiet /qn /norestart /log datadog-apm-msi-installer.log"
}
