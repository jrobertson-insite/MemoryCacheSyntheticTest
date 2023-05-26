$webConfigFilePath = "/inetpub/wwwroot/web.config"

if ($null -eq $env:CACHEMEMORYLIMITMEGABYTES) {
  Write-Error "Missing environment variable 'CACHEMEMORYLIMITMEGABYTES', can not update named caches configuration."
  Exit 1
}
$cacheMemoryLimitMegabytes = $env:CACHEMEMORYLIMITMEGABYTES

if ($null -eq $env:PHYSICALMEMORYLIMITPERCENTAGE) {
  Write-Error "Missing environment variable 'PHYSICALMEMORYLIMITPERCENTAGE', can not update named caches configuration."
  Exit 1
}
$physicalMemoryLimitPercentage = $env:PHYSICALMEMORYLIMITPERCENTAGE

# this will only update the node if it exists, we don't want to risk adding it when
# it wasn't there, or if there are more than one element. This is intended only to update
# the single default named cache child element

# expected xml that this is intended to update/modify
# <configuration>
# ...
#   <system.runtime.caching>
#     <memoryCache>
#       <namedCaches>
#         <add name="Default" cacheMemoryLimitMegabytes="1024" physicalMemoryLimitPercentage="25" pollingInterval="00:01:00" />
#       </namedCaches>
#     </memoryCache>
#   </system.runtime.caching>
# ...
# </configuration>

[xml]$webConfig = Get-Content $webConfigFilePath

$namedCachesElement = $webConfig.configuration.'system.runtime.caching'.memoryCache.namedCaches
if ($null -eq $namedCachesElement) {
  Write-Error "WebConfig namedCaches element not found, not updating cache settings."
  Exit 1
}

if ($null -eq $namedCachesElement.add -and $namedCachesElement.add.Count -ne 1) {
  Write-Error "WebConfig namedCaches element does not have the 1 (and only 1) expected child elements."
  Exit 1
}

$memoryCacheNode = $namedCachesElement.add
$memoryCacheNode.SetAttribute("cacheMemoryLimitMegabytes", $cacheMemoryLimitMegabytes)
$memoryCacheNode.SetAttribute("physicalMemoryLimitPercentage", $physicalMemoryLimitPercentage)

$webConfig.Save($webConfigFilePath)
