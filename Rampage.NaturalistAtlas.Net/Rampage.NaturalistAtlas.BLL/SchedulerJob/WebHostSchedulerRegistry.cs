using FluentScheduler;

namespace Rampage.NaturalistAtlas.SchedulerJob
{
    /// <summary>
    /// 各 Web 进程内存缓存清理（每实例各自跑，分布式下清理本机缓存）。
    /// </summary>
    public class WebHostSchedulerRegistry : Registry
    {
        public WebHostSchedulerRegistry()
        {
            Schedule<MemoryCacheOperationJob>().NonReentrant().ToRunEvery(10).Seconds();
            Schedule<MemoryCacheQueueCleanJob>().NonReentrant().ToRunEvery(10).Seconds();
            Schedule<MemoryCacheRecordCleanJob>().NonReentrant().ToRunEvery(10).Seconds();
        }
    }


}
