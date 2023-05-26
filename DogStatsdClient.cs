using System;
using Serilog;
using StatsdClient;

namespace MemoryCacheSyntheticTest;

public class DogStatsdClient
{
    public readonly DogStatsdService Client;

    public DogStatsdClient()
    {
        var statsdConfig = new StatsdConfig
        {
            StatsdServerName = Environment.GetEnvironmentVariable("DD_DOGSTATSD_HOST"),
            StatsdPort = Convert.ToInt32(Environment.GetEnvironmentVariable("DD_DOGSTATSD_PORT")),
        };

        Log.Information($"Setting up DogStatsD with Server: {statsdConfig.StatsdServerName} Port: {statsdConfig.StatsdPort}");

        this.Client = new DogStatsdService();
        if (!this.Client.Configure(statsdConfig))
        {
            var ex = new InvalidOperationException("Cannot initialize DogstatsD. Set optionalExceptionHandler argument in the `Configure` method for more information.");
            Log.Error(ex, "Failed to setup dogstatsd.");
        }
    }
}
