using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;
using System;

namespace Rampage.NaturalistAtlas.SchedulerJob
{
    public class MemoryCacheOperationJob : SchedulerJobBase
    {

        [Auto]
        protected IMemoryCacheOperationService MemoryCacheOperationService = default!;


        protected override void ExecuteJob()
        {
            try
            {
                string applicationId = RampageBooter.Application.UniqueId;
                List<MemoryCacheOperationQueue> queueList = MemoryCacheOperationService.GetValidQueueList();
                if (queueList.Count > 0)
                {
                    foreach (MemoryCacheOperationQueue queue in queueList)
                    {
                        MemoryCacheOperationRecord? record = MemoryCacheOperationService.GetRecord(queue.Type, queue.Id, applicationId);
                        if (record == null)
                        {
                            switch (queue.Type)
                            {
                                case MemoryCacheOperationType.Delete:
                                    string cacheKey = queue.CacheKey!;
                                    if (queue.IsPrefix)
                                    {
                                        MemoryCache.RemoveBySearch(cacheKey);
                                    }
                                    else
                                    {
                                        MemoryCache.Remove(cacheKey);
                                    }

                                    record = new MemoryCacheOperationRecord
                                    {
                                        Id = Guid.NewGuid().ToString("N"),
                                        Type = queue.Type,
                                        QueueId = queue.Id,
                                        ApplicationId = applicationId,
                                        ExpireTime = queue.ExpireTime,
                                        CreateTime = DateTime.UtcNow,
                                        UpdateTime = DateTime.UtcNow
                                    };
                                    MemoryCacheOperationService.SaveRecord(record);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("MemoryCacheOperationJob.ExecuteJob error", ex);
            }
        }
    }
}
