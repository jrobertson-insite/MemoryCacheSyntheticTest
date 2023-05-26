using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Web.Hosting;
using Bogus;
using Datadog.Trace;
using Datadog.Trace.Configuration;
using Serilog;

namespace MemoryCacheSyntheticTest;

public class MemoryCacheModel
{
    public double MemoryInMegabytes { get; set; }
    public int CacheCount { get; set; }
}

public static class MemoryCacheHackExtensions
{
    public static long GetApproximateSize(this MemoryCache cache)
    {
        var statsField = typeof(MemoryCache).GetField("_stats", BindingFlags.NonPublic | BindingFlags.Instance);
        var statsValue = statsField.GetValue(cache);
        var monitorField = statsValue.GetType().GetField("_cacheMemoryMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var monitorValue = monitorField.GetValue(statsValue);
        var sizeField = monitorValue.GetType().GetField("_sizedRefMultiple", BindingFlags.NonPublic | BindingFlags.Instance);
        var sizeValue = sizeField.GetValue(monitorValue);
        var approxProp = sizeValue.GetType().GetProperty("ApproximateSize", BindingFlags.NonPublic | BindingFlags.Instance);
        return (long)approxProp.GetValue(sizeValue, null);
    }
}

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Bio1 { get; set; }
    public string Bio2 { get; set; }
    public string Bio3 { get; set; }
    public string Bio4 { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string Line1 { get; set; }
    public string Line2 { get; set; }
    public string PinCode { get; set; }
}

public class MemoryCacheManager
{
    private static readonly object NullValue = new NullCacheItem();

    private static readonly ConcurrentDictionary<string, object> CacheKeyLockMap = new();

    private readonly DogStatsdClient DogStatsdClient;

    private static ObjectCache Cache => MemoryCache.Default;

    public MemoryCacheManager()
    {
        // Create a settings object using the existing environment variables and config sources
        var settings = TracerSettings.FromDefaultSources();

        // Override a value
        // settings.GlobalTags.Add("SomeKey", "SomeValue");

        // Replace the tracer configuration
        Tracer.Configure(settings);

        Log.Information($"CacheMemoryLimit: {MemoryCache.Default.CacheMemoryLimit}", MemoryCache.Default.CacheMemoryLimit);

        this.DogStatsdClient = new DogStatsdClient();
        this.DogStatsdClient.Client.Gauge("memory_cache_synthetic_test.memory_cache.cacheMemoryLimitMegabytes.gauge", Convert.ToDouble(Environment.GetEnvironmentVariable("CACHEMEMORYLIMITMEGABYTES")), tags: new[] {"environment:dev"});
        this.DogStatsdClient.Client.Gauge("memory_cache_synthetic_test.memory_cache.physicalMemoryLimitPercentage.gauge", Convert.ToDouble(Environment.GetEnvironmentVariable("PHYSICALMEMORYLIMITPERCENTAGE")), tags: new[] {"environment:dev"});
    }

    public void BlowItUp()
    {
        var random = new Random();
        long itemsAdded = 0;

        while (true)
        {
            var address = new Faker<Address>()
                    .RuleFor(a => a.Line1, f => f.Address.BuildingNumber())
                    .RuleFor(a => a.Line2, f => f.Address.StreetAddress())
                    .RuleFor(a => a.PinCode, f => f.Address.ZipCode());
            var customer = new Faker<Customer>()
                    .RuleFor(c => c.Id, f => f.Random.Number(1, 100))
                    .RuleFor(c => c.FirstName, f => f.Person.FirstName)
                    .RuleFor(c => c.LastName, f => f.Person.LastName)
                    .RuleFor(c => c.Email, f => f.Person.Email)
                    .RuleFor(c => c.Bio1, f => f.Lorem.Paragraph(10000))
                    .RuleFor(c => c.Bio2, f => f.Lorem.Paragraph(10000))
                    .RuleFor(c => c.Bio3, f => f.Lorem.Paragraph(10000))
                    .RuleFor(c => c.Bio4, f => f.Lorem.Paragraph(10000))
                    .RuleFor(c => c.Address, f => address.Generate());

            this.Add((random.Next() % 3000).ToString(), customer.Generate(), TimeSpan.FromMinutes(20));
            itemsAdded++;
            var stats = GetStats();

            this.DogStatsdClient.Client.Gauge("memory_cache_synthetic_test.memory_cache.count.gauge", stats.CacheCount, tags: new[] {"environment:dev"});
            this.DogStatsdClient.Client.Gauge("memory_cache_synthetic_test.memory_cache.items_added.gauge", itemsAdded, tags: new[] {"environment:dev"});
            this.DogStatsdClient.Client.Gauge("memory_cache_synthetic_test.memory_cache.size_mb.gauge", stats.MemoryInMegabytes, tags: new[] {"environment:dev"});

            Log.Information($"Items Added: {itemsAdded} -- Cache Count: {stats.CacheCount:00000} -- Memory(MB): {stats.MemoryInMegabytes:0000.00}", stats.CacheCount, stats.MemoryInMegabytes);
            // Log.Information($"Datadog metrics actually sent: {this.DogStatsdClient.Client.TelemetryCounters.MetricsSent}");
        }

        // ReSharper disable once FunctionNeverReturns
    }

    public T Get<T>(string key)
    {
        var cachedValue = Cache.Get(key);

        if (cachedValue == NullValue)
        {
            return default;
        }

        if (cachedValue is LargeString largeString)
        {
            cachedValue = largeString.ToString();
        }

        return (T)cachedValue;
    }

    public MemoryCacheModel GetStats()
    {
        return new MemoryCacheModel
        {
            //MemoryInMegabytes = MemoryCache.Default.GetLastSize() / 8.0 / 1024.0 / 1024.0,
            MemoryInMegabytes = MemoryCache.Default.GetApproximateSize() / 8.0 / 1024.0 / 1024.0,
            CacheCount = MemoryCache.Default.Count()
        };
    }

    public T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? validFor = null)
    {
        var cachedValue = Cache.Get(key);

        if (cachedValue == null)
        {
            var cacheKeyLock = CacheKeyLockMap.GetOrAdd(key, o => new object());
            lock (cacheKeyLock)
            {
                // Check again, we may have been waiting for another thread that was computing this value already and it may now be cached
                cachedValue = Cache.Get(key);

                if (cachedValue == null)
                {
                    cachedValue = factory();
                    this.Add(key, cachedValue, validFor);
                }
            }

            CacheKeyLockMap.TryRemove(key, out var value);
        }

        if (cachedValue == NullValue)
        {
            return default(T);
        }
        else
        {
            if (cachedValue is LargeString s)
            {
                cachedValue = s.ToString();
            }
        }

        return (T)cachedValue;
    }

    public object Get(string key)
    {
        var cachedValue = Cache.Get(key);

        if (cachedValue == NullValue)
        {
            return null;
        }
        else
        {
            if (cachedValue is LargeString s)
            {
                cachedValue = s.ToString();
            }
        }

        return cachedValue;
    }

    public void Add(string key, object value, TimeSpan? validFor = null)
    {
        using var scope = Tracer.Instance.StartActive("add-cache-object");

        if (value == null)
        {
            value = NullValue;
        }
        else
        {
            if (value is string { Length: > 1024 } stringValue)
            {
                value = new LargeString(stringValue);
            }
        }

        var policy = new CacheItemPolicy();
        if (validFor.HasValue)
        {
            policy.AbsoluteExpiration = DateTime.Now.Add(validFor.Value);
        }

        Cache.Add(new CacheItem(key, value), policy);
    }

    public bool Contains(string key)
    {
        return Cache.Contains(key);
    }

    public void Remove(string key)
    {
        Cache.Remove(key);
    }

    public void Clear()
    {
        MemoryCache.Default.Trim(100);

        var stack = new StackTrace(1);
        var caller = stack.GetFrame(0).GetMethod();
        Log.Information($"Application memory cache has been cleared via {caller.DeclaringType}.{caller.Name}. Stack:{stack}");
    }

    public IEnumerable<KeyValuePair<string, object>> AsEnumerable()
    {
        return Cache.AsEnumerable();
    }

    public void Set(string key, object value, TimeSpan? validFor = null)
    {
        var policy = new CacheItemPolicy();
        if (validFor is not null)
        {
            policy.SlidingExpiration = validFor.GetValueOrDefault();
        }

        Cache.Set(new CacheItem(key, value), policy);
    }

    private class NullCacheItem
    {
        public override string ToString() => "null";
    }

    private sealed class LargeString
    {
        private readonly WeakReference<string> uncompressed;
        private readonly byte[] compressed;

        public LargeString(string value)
        {
            this.uncompressed = new WeakReference<string>(value);

            using var output = new MemoryStream();
            using var compression = new GZipStream(output, CompressionMode.Compress);
            using (var writer = new StreamWriter(compression, Encoding.UTF8))
            {
                writer.Write(value);
            }

            this.compressed = output.ToArray();
        }

        public override string ToString()
        {
            if (this.uncompressed.TryGetTarget(out var value))
            {
                return value;
            }

            using var decompression = new GZipStream(
                new MemoryStream(this.compressed),
                CompressionMode.Decompress
            );
            using var reader = new StreamReader(decompression, Encoding.UTF8);

            value = reader.ReadToEnd();

            this.uncompressed.SetTarget(value);

            return value;
        }
     }
}
