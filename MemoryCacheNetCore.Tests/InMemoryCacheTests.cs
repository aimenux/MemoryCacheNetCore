using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemoryCacheNetCore.Tests
{
    [TestClass]
    public class InMemoryCacheTests
    {
        private const int Timeout = 10_000;
        private const int CacheDurationInSeconds = 2;
        private const int MoreThanCacheDurationInSeconds = 3;
        private static readonly Random Random = new(Guid.NewGuid().GetHashCode());

        [TestMethod, Timeout(Timeout, CooperativeCancellation = true)]
        public async Task Get_Old_Value_When_Delay_Is_Not_Expired()
        {
            var cache = new InMemoryCache(CacheDurationInSeconds);

            var xx = await cache.AddOrGetAsync("foo",
                async () =>
                {
                    await Task.Delay(RandomDelay());
                    return 10;
                });

            xx.Should().Be(10);

            var yy = await cache.AddOrGetAsync("foo",
                async () =>
                {
                    await Task.Delay(RandomDelay());
                    return 20;
                });

            yy.Should().Be(10);
        }

        [TestMethod, Timeout(Timeout, CooperativeCancellation = true)]
        public async Task Get_New_Value_When_Delay_Is_Expired()
        {
            var cache = new InMemoryCache(CacheDurationInSeconds);

            var xx = await cache.AddOrGetAsync("foo",
                async () =>
                {
                    await Task.Delay(RandomDelay());
                    return 10;
                });

            xx.Should().Be(10);

            await Task.Delay(MoreThanCacheDurationInSeconds * 1_000);

            var yy = await cache.AddOrGetAsync("foo",
                async () =>
                {
                    await Task.Delay(RandomDelay());
                    return 20;
                });

            yy.Should().Be(20);
        }

        [TestMethod, Timeout(Timeout, CooperativeCancellation = true)]
        public async Task Get_New_Value_When_Delay_Is_Not_Expired_And_Cache_Was_Flushed()
        {
            var cache = new InMemoryCache(CacheDurationInSeconds);

            var xx = await cache.AddOrGetAsync("foo",
                async () =>
                {
                    await Task.Delay(RandomDelay());
                    return 10;
                });

            xx.Should().Be(10);

            await cache.ClearCacheEntriesAsync();

            var yy = await cache.AddOrGetAsync("foo",
                async () =>
                {
                    await Task.Delay(RandomDelay());
                    return 20;
                });

            yy.Should().Be(20);
        }

        [TestMethod, Timeout(Timeout, CooperativeCancellation = true)]
        public async Task Should_Not_Throw_Exception_When_Cache_Is_Flushed_While_Is_Used_V1()
        {
            try
            {
                await Task.WhenAll(RandomCacheTasks());
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod, Timeout(Timeout, CooperativeCancellation = true)]
        public async Task Should_Not_Throw_Exception_When_Cache_Is_Flushed_While_Is_Used_V2()
        {
            var cache = new InMemoryCache(CacheDurationInSeconds);

            async Task FuncBody(int index)
            {
                var key = Guid.NewGuid().ToString();

                var xx = await cache.AddOrGetAsync(key, async () =>
                {
                    await Task.Delay(RandomDelay());
                    return 10;
                });

                xx.Should().Be(10);

                await Task.Run(async () =>
                {
                    await Task.Delay(RandomDelay());
                    await cache.ClearCacheEntriesAsync();
                });

                var yy = await cache.AddOrGetAsync(key, async () =>
                {
                    await Task.Delay(RandomDelay());
                    return 20;
                });

                yy.Should().Be(20);
            }

            var source = Enumerable.Range(1, 30);
            
            await ParallelForEachAsync(source, FuncBody, 5);

            cache.Size.Should().BeGreaterThan(0);
        }

        [TestMethod, Timeout(Timeout, CooperativeCancellation = true)]
        public async Task Should_Not_Throw_Exception_When_Cache_Is_Flushed_While_Is_Used_V3()
        {
            var cache = new InMemoryCache(CacheDurationInSeconds);

            var key = Guid.NewGuid().ToString();

            Task<int> Func1() => cache.AddOrGetAsync(key, async () =>
            {
                await Task.Delay(RandomDelay());
                return 10;
            });
            var task1 = Task.Factory.StartNew(Func1, TaskCreationOptions.LongRunning);

            Task<int> Func2() => cache.AddOrGetAsync(key, async () =>
            {
                await Task.Delay(RandomDelay());
                return 20;
            });
            var task2 = Task.Factory.StartNew(Func2, TaskCreationOptions.LongRunning);

            Task<int> Func3() => cache.AddOrGetAsync(key, async () =>
            {
                await Task.Delay(RandomDelay());
                return 30;
            });
            var task3 = Task.Factory.StartNew(Func3, TaskCreationOptions.LongRunning);

            await Task.WhenAll(task1, task2, task3);

            cache.Size.Should().Be(1);
        }

        private static Task[] RandomCacheTasks()
        {
            var cache = new InMemoryCache(CacheDurationInSeconds);

            var tasks1 = Enumerable.Range(1, 10)
                .Select(x => cache.AddOrGetAsync($"foo-{x}",
                    async () =>
                    {
                        await Task.Delay(RandomDelay());
                        return x;
                    }));

            var tasks2 = Enumerable.Range(1, 10)
                .Select(x => cache.AddOrGetAsync($"bar-{x}",
                    async () =>
                    {
                        await Task.Delay(RandomDelay());
                        return x;
                    }));

            var tasks3 = Enumerable.Range(1, 10)
                .Select(_ => Task.Run(async () => await cache.ClearCacheEntriesAsync()));

            var tasks4 = Enumerable.Range(1, 10)
                .Select(_ => Task.Delay(RandomDelay()));

            var array = tasks1.Union(tasks2).Union(tasks3).Union(tasks4);

            return array.OrderBy(_ => Random.Next()).ToArray();
        }

        private static Task ParallelForEachAsync<T>(IEnumerable<T> source, Func<T, Task> funcBody, int maxPartitions)
        {
            async Task AwaitPartition(IEnumerator<T> partition)
            {
                using (partition)
                {
                    while (partition.MoveNext())
                    {
                        await Task.Yield();
                        await funcBody(partition.Current);
                    }
                }
            }

            return Task.WhenAll(
                Partitioner
                    .Create(source)
                    .GetPartitions(maxPartitions)
                    .AsParallel()
                    .Select(AwaitPartition));
        }

        private static int RandomDelay() => Random.Next(10, 50);
    }
}
