using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemoryCacheNetCore.Tests
{
    [TestClass]
    public class InMemoryCacheTests
    {
        private const int LessThanExpiration = 1000;
        private const int MoreThanExpiration = 6000;

        [TestMethod]
        public async Task Get_Old_Value_When_Delay_Is_Not_Expired()
        {
            var cache = new InMemoryCache($"{DateTime.UtcNow}");

            var xx = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(10))
                .ConfigureAwait(false);

            Assert.AreEqual(10, xx);

            await Task.Delay(LessThanExpiration);

            var yy = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(20))
                .ConfigureAwait(false);

            Assert.AreEqual(10, yy);
        }

        [TestMethod]
        public async Task Get_New_Value_When_Delay_Is_Expired()
        {
            var cache = new InMemoryCache($"{DateTime.UtcNow}");

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
            var cache = new InMemoryCache($"{DateTime.UtcNow}");

            var xx = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(10))
                .ConfigureAwait(false);

            Assert.AreEqual(10, xx);

            cache.ClearCacheEntries();

            var yy = await cache.AddOrGetAsync("foo",
                async () => await Task.FromResult(20))
                .ConfigureAwait(false);

            Assert.AreEqual(20, yy);
        }

        [TestMethod]
        public async Task Should_Not_Throw_Exception_When_Get_Or_Add_Or_Flush_Cache()
        {
            var cache = new InMemoryCache($"{DateTime.UtcNow}");

            var task1 = cache.AddOrGetAsync("toto",
                async () => await Task.FromResult(10));

            var task2 = cache.AddOrGetAsync("toto",
                async () => await Task.FromResult(20));

            var task3 = Task.Run(() => cache.ClearCacheEntries());

            try
            {
                await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}
