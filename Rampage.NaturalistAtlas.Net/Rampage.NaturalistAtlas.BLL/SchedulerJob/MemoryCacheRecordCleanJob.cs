using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto;
using System;

namespace Rampage.NaturalistAtlas.SchedulerJob
{
    public class MemoryCacheRecordCleanJob : SchedulerJobBase
    {

        [Auto]
        protected IMemoryCacheOperationService MemoryCacheOperationService = default!;


        protected override void ExecuteJob()
        {
            try
            {
                string applicationId = RampageBooter.Application.UniqueId;
                List<MemoryCacheOperationRecord> queueList = MemoryCacheOperationService.GetExpiredRecordList();
                if (queueList.Count > 0)
                {
                    List<string> expiredRecordIdList = queueList.Select(d => d.Id).ToList();
                    MemoryCacheOperationService.DeleteManyRecords(expiredRecordIdList);
                }
            }
            catch (Exception ex)
            {
                LogError("MemoryCacheRecordCleanJob.ExecuteJob error", ex);
            }
        }
    }
}
