using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;
using System;

namespace Rampage.NaturalistAtlas.SchedulerJob
{
    public class MemoryCacheQueueCleanJob : SchedulerJobBase
    {

        [Auto]
        protected IMemoryCacheOperationService MemoryCacheOperationService = default!;


        protected override void ExecuteJob()
        {
            try
            {
                string applicationId = RampageBooter.Application.UniqueId;
                List<MemoryCacheOperationQueue> queueList = MemoryCacheOperationService.GetExpiredQueueList();
                if (queueList.Count > 0)
                {
                    List<string> expiredQueueIdList = queueList.Select(d => d.Id).ToList();
                    MemoryCacheOperationService.DeleteManyQueues(expiredQueueIdList);
                }
            }
            catch (Exception ex)
            {
                LogError("MemoryCacheOperationJob.ExecuteJob error", ex);
            }
        }
    }
}
