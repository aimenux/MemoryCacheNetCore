﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace MemoryCacheNetCore
{
    public sealed class InMemoryCache : IInMemoryCache, IDisposable
    {
        private readonly MemoryCache _cache;
        private readonly int _cacheDurationInSeconds;
        private static CancellationTokenSource _flushCacheToken = new();

        public int Size => _cache.Count;

        public InMemoryCache(int cacheDurationInSeconds)
        {
            _cacheDurationInSeconds = cacheDurationInSeconds;
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<T> AddOrGetAsync<T>(string key, Func<Task<T>> func)
        {
            var newValue = new Lazy<Task<T>>(func);
            var oldValue = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_cacheDurationInSeconds);
                entry.AddExpirationToken(new CancellationChangeToken(_flushCacheToken.Token));
                entry.RegisterPostEvictionCallback(EvictionCallback);
                return newValue;
            });
            try
            {
                return await (oldValue ?? newValue).Value;
            }
            catch
            {
                _cache.Remove(key);
                throw;
            }
        }

        public Task ClearCacheEntriesAsync()
        {
            if (CanCancelToken(_flushCacheToken))
            {
                _flushCacheToken.Cancel();
                _flushCacheToken.Dispose();
                _flushCacheToken = new CancellationTokenSource();
            }

            Console.WriteLine("Flushing cache");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cache?.Dispose();
        }

        private static void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            Console.WriteLine($"Entry {key} was evicted because: {reason}.");
        }

        private static bool CanCancelToken(CancellationTokenSource tokenSource)
        {
            return !tokenSource.IsCancellationRequested && tokenSource.Token.CanBeCanceled;
        }
    }
}
