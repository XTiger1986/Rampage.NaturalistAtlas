using Rampage.NaturalistAtlas.Model.Dto;
using System;

namespace Rampage.NaturalistAtlas.BLL
{
    public interface IMemoryCacheOperationService
    {

        void SaveOperationQueue(MemoryCacheOperationQueue queue);

        /// <summary>
        /// 添加删除缓存键名
        /// </summary>
        /// <param name="cacheKey">键名</param>
        /// <param name="isPrefix">是否指定为键名前缀，所有该键名前缀的缓存都将被清理</param>
        void TryAddDeletionQueue(string cacheKey, bool isPrefix);

        List<MemoryCacheOperationQueue> GetValidQueueList();

        List<MemoryCacheOperationQueue> GetExpiredQueueList();

        void DeleteManyQueues(List<string> idList);

        MemoryCacheOperationRecord? GetRecord(MemoryCacheOperationType type, string queueId, string applicationId);

        List<MemoryCacheOperationRecord> GetExpiredRecordList();

        void SaveRecord(MemoryCacheOperationRecord record);

        void DeleteManyRecords(List<string> idList);

    }
}
