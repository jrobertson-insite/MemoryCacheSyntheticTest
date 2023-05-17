# MemoryCacheSyntheticTest
Try to test things related to dotnet memory cache manager

Lots to learn, this is a problem that has many layers. The two main variables we
want to work with cacheMemoryLimitMegabytes and physicalMemoryLimitPercentage are
only two of the many factors that go into how dotnet deals with memorycache.

This program as it stands with me setting cacheMemoryLimitMegabytes to 1024 and
disabling physicalMemoryLimitPercentage (set to 0) will trim the cache and ~150 MB.
This is on a system with 64GB of memory and 20+GB of free memory.

## TODO:
* Output console.log to a csv to open in excel and graph what's happening
* Find a way to see "memory pressure"
  * https://referencesource.microsoft.com/#System.Runtime.Caching/System/Caching/PhysicalMemoryMonitor.cs,109
  * https://referencesource.microsoft.com/#System.Runtime.Caching/System/Caching/PhysicalMemoryMonitor.cs,26
* Look at Microsoft's source code more to understand the levers that control memory cache

## Thoughts and random links:
* Investigate perf counters.
  * https://www.stefangeiger.ch/2019/11/25/dotnet-tip-memorycache-perfcounter.html
  * This seems like it doesn't work well if we use memorycache.default since it's not named, maybe it's fine, just uses weird names likely tied to app pool id or similar
  * perf counters on a server with many websites using memorycache.default
    * PS C:\Users\jrobertson> typeperf -qx ".NET Memory Cache 4.0"
    * \.NET Memory Cache 4.0(_lm_w3svc_6_root:default)\Cache Hits
    * \.NET Memory Cache 4.0(_lm_w3svc_97_root:default)\Cache Hits
    * ...
* https://stackoverflow.com/questions/41968043/dot-net-memorycache-eviction
