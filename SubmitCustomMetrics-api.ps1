function unixTime() {
  Return (Get-Date -date ((get-date).ToUniversalTime()) -UFormat %s) -Replace("[,\.]\d*", "")
}

function postMetric($metric,$tags) {
  $currenttime = unixTime
  $host_name = $env:COMPUTERNAME #optional parameter .

  # Construct JSON
  $points = ,@($currenttime, $metric.amount)
  $post_obj = [pscustomobject]@{"series" = ,@{"metric" = $metric.name;
      "points" = $points;
      "type" = "gauge";
      "host" = $host_name;
      "tags" = $tags}}
  $post_json = $post_obj | ConvertTo-Json -Depth 5 -Compress
  # POST to DD API
  $response = Invoke-RestMethod -Method Post -Uri $url -Body $post_json -ContentType "application/json"
}

# Datadog account, API information and optional parameters
$app_key = "42f6d1d9a2b5ec11ccf45e1065b72aeba364dfff" #provide your valid app key
$api_key = "0ef7c72f249589ca240c911f510dfa58" #provide your valid api key
$url_base = "https://app.datadoghq.com/"
$url_signature = "api/v1/series"
$url = $url_base + $url_signature + "?api_key=$api_key" + "&" + "application_key=$app_key"
$tags = "[env:test]" #optional parameter

# Select what to send to Datadog. In this example, the number of handles opened by process "mmc" is being sent
$metric_ns = "ps1." # your desired metric namespace
$temp = Get-Process w3wp
$metric = @{"name"=$metric_ns + $temp.Name; "amount"=$temp.Handles}
postMetric($metric)($tags) # pass your metric as a parameter to postMetric()
