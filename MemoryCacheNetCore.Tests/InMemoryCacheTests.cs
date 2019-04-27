using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemoryCacheNetCore.Tests
{
    [TestClass]
    public class InMemoryCacheTests
    {
        private const int MoreThanExpiration = 6000;
        private static readonly Random Random = new Random();

        [TestMethod]
        public async Task Get_Old_Value_When_Delay_Is_Not_Expired()
        {
            var cache = new InMemoryCache();

            var xx = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(10));

            Assert.AreEqual(10, xx);

            var yy = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(20));

            Assert.AreEqual(10, yy);
        }

        [TestMethod]
        public async Task Get_New_Value_When_Delay_Is_Expired()
        {
            var cache = new InMemoryCache();

            var xx = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(10));

            Assert.AreEqual(10, xx);

            await Task.Delay(MoreThanExpiration);

            var yy = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(20));

            Assert.AreEqual(20, yy);
        }

        [TestMethod]
        public async Task Get_New_Value_When_Delay_Is_Not_Expired_And_Cache_Was_Flushed()
        {
            var cache = new InMemoryCache();

            var xx = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(10));

            Assert.AreEqual(10, xx);

            cache.ClearCacheEntries();

            var yy = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(20));

            Assert.AreEqual(20, yy);
        }

        [TestMethod]
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

        [TestMethod]
        public void Should_Not_Throw_Exception_When_Cache_Is_Flushed_While_Is_Used_V2()
        {
            var cache = new InMemoryCache();

            Parallel.For(1, 100, async index =>
            {
                await Task.Delay(Random.Next(100));

                var key = Guid.NewGuid().ToString();

                var xx = await cache.AddOrGetAsync(key,
                    async () => await Task.FromResult(10));

                Assert.AreEqual(10, xx);

                cache.ClearCacheEntries();

                var yy = await cache.AddOrGetAsync(key,
                    async () => await Task.FromResult(20));

                Assert.AreEqual(20, yy);
            });
        }

        private static Task[] RandomCacheTasks()
        {
            var cache = new InMemoryCache();

            var tasks1 = Enumerable.Range(1, 20)
                .Select(x => cache.AddOrGetAsync($"foo-{x}",
                    async () => await Task.FromResult(x)));

            var tasks2 = Enumerable.Range(1, 20)
                .Select(x => cache.AddOrGetAsync($"bar-{x}",
                    async () => await Task.FromResult(x)));

            var tasks3 = Enumerable.Range(1, 10)
                .Select(x => Task.Run(() => cache.ClearCacheEntries()));

            var array = tasks1.Union(tasks2).Union(tasks3);

            return array.OrderBy(x => Random.Next()).ToArray();
        }
    }
}
