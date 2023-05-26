$env:DD_AGENT_HOST="datadog-windows-dev.datadog-dev.svc.cluster.local"
function dogstatsd($metric) {
  $udpClient = New-Object System.Net.Sockets.UdpClient
  $udpClient.Connect($env:DD_AGENT_HOST, $env:DD_DOGSTATSD_PORT)
  $encodedData=[System.Text.Encoding]::ASCII.GetBytes($metric)
  $bytesSent=$udpClient.Send($encodedData,$encodedData.Length)
  $udpClient.Close()
}
$tags = "|#env:test" # Datadog tag
$temp = Get-Process w3wp
$metric = "dogstatsd.ps1." + $temp.Name + ":" + $temp.Handles + "|g" + $tags # metric
dogstatsd($metric)
