# # manually build the MemoryCacheSyntheticTest solution first
# FROM registry.cloud.insitehq.net/commerce-windows/runtime-base

# SHELL ["powershell"]

# WORKDIR /DataDog
# COPY ./InstallDatadog.ps1 ./
# RUN powershell ./InstallDatadog.ps1

# WORKDIR /inetpub/wwwroot
# COPY . ./

# ENTRYPOINT ["/LogMonitor/LogMonitor.exe", "powershell -C /inetpub/wwwroot/entrypoint.ps1"]

FROM registry.cloud.insitehq.net/memorycachesynthetictest
COPY bin/MemoryCacheSyntheticTest.dll bin/MemoryCacheSyntheticTest.dll
COPY Web.config UpdateNamedCacheSettings.ps1 ./

ENV DatadogAppKey=000
ENV DatadogKeyId=000
ENV DatadogKey=000

# docker run -d --cgroupns host \
#               --pid host \
#               -v /var/run/docker.sock:/var/run/docker.sock:ro \
#               -v /proc/:/host/proc/:ro \
#               -v /sys/fs/cgroup/:/host/sys/fs/cgroup:ro \
#               -e DD_API_KEY=0ef7c72f249589ca240c911f510dfa58 \
#               -e DD_DOGSTATSD_NON_LOCAL_TRAFFIC="true" \
#               -p 8125:8125/udp \
#               -p 8126:8126 \
#               gcr.io/datadoghq/agent:latest
