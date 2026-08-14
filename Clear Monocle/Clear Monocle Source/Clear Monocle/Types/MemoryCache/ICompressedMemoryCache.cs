using ThaleTheGreat.ClearMonocle.Tasking;
using System.Threading;
using System.Threading.Tasks;

namespace ThaleTheGreat.ClearMonocle.Types.MemoryCache;

internal interface ICompressedMemoryCache : ICache {
    private static readonly ThreadedTaskScheduler CompressedMemoryCacheScheduler = new(
        concurrencyLevel: null,
        threadNameFunction: i => $"CompressedMemoryCache Compression Thread {i}",
        useBackgroundThreads: true,
        threadPriority: ThreadPriority.Lowest
    );
    protected static readonly TaskFactory TaskFactory = new(CompressedMemoryCacheScheduler);
}
