using Rampage.AppLog;
using Rampage.Enums;
using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.RedisLibrary;
using Rampage.Utilities;
using System;

namespace Rampage.NaturalistAtlas.BLL.Implementations
{
    public class MemoryCacheOperationService : BaseService, IMemoryCacheOperationService
    {

        protected RedisHelper RedisClient;

        protected static readonly object OPERATION_LOCKER = new object();
        protected static readonly string REDIS_KEY_MEMORY_CACHE_OPERATION = "MEMORY_CACHE_OPERATION";
        protected static readonly string REDIS_FIELD_KEY_OPERATION_QUEUE = "QUEUE_LIST";
        protected static readonly string REDIS_FIELD_KEY_OPERATION_RECORD = "RECORD_LIST";


        public MemoryCacheOperationService()
        {
            RedisClient = RedisFactory.GetClient()!;
        }

        public void SaveOperationQueue(MemoryCacheOperationQueue queue)
        {
            try
            {
                List<MemoryCacheOperationQueue>? list = RedisClient.HashGet<List<MemoryCacheOperationQueue>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_QUEUE);
                MemoryCacheOperationQueue? existsQueue = null;
                if (list != null)
                {
                    existsQueue = list.Find(x => x.Type == queue.Type && StringTool.Equals(x.CacheKey, queue.CacheKey) && x.IsPrefix == queue.IsPrefix);
                }
                if (existsQueue == null)
                {
                    lock (OPERATION_LOCKER)
                    {
                        list = RedisClient.HashGet<List<MemoryCacheOperationQueue>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_QUEUE);
                        if (list != null)
                        {
                            existsQueue = list.Find(x => x.Type == queue.Type && StringTool.Equals(x.CacheKey, queue.CacheKey) && x.IsPrefix == queue.IsPrefix);
                        }

                        if (existsQueue == null)
                        {
                            if (string.IsNullOrEmpty(queue.Id))
                            {
                                queue.Id = Guid.NewGuid().ToString("N");
                            }
                            queue.Status = CommonStatus.Enabled;
                            queue.CreateTime = DateTime.UtcNow;

                            if (list == null)
                            {
                                list = new List<MemoryCacheOperationQueue>();
                            }
                            list.Add(queue);
                            RedisClient.HashSet(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_QUEUE, list);
                        }
                    }
                }
                else
                {
                    existsQueue.UpdateTime = DateTime.UtcNow;
                    existsQueue.ExpireTime = queue.ExpireTime;
                    RedisClient.HashSet(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_QUEUE, list);
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }

        public void TryAddDeletionQueue(string cacheKey, bool isPrefix)
        {
            try
            {
                MemoryCacheOperationQueue queue = new MemoryCacheOperationQueue
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Type = MemoryCacheOperationType.Delete,
                    CacheKey = cacheKey,
                    IsPrefix = isPrefix,
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow,
                    ExpireTime = DateTime.UtcNow.AddDays(1)
                };
                SaveOperationQueue(queue);
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }

        public List<MemoryCacheOperationQueue> GetValidQueueList()
        {
            List<MemoryCacheOperationQueue>? list = RedisClient.HashGet<List<MemoryCacheOperationQueue>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_QUEUE);
            if (list != null && list.Count > 0)
            {
                return list.FindAll(q => q.Status == CommonStatus.Enabled && q.ExpireTime > DateTime.UtcNow);
            }
            return new List<MemoryCacheOperationQueue>();
        }

        public List<MemoryCacheOperationQueue> GetExpiredQueueList()
        {
            List<MemoryCacheOperationQueue>? list = RedisClient.HashGet<List<MemoryCacheOperationQueue>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_QUEUE);
            if (list != null && list.Count > 0)
            {
                return list.FindAll(q => q.Status == CommonStatus.Disabled || q.ExpireTime <= DateTime.UtcNow);
            }
            return new List<MemoryCacheOperationQueue>();
        }

        public void DeleteManyQueues(List<string> idList)
        {
            try
            {
                List<MemoryCacheOperationQueue>? list = RedisClient.HashGet<List<MemoryCacheOperationQueue>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_QUEUE);
                bool exists = false;
                if (list != null)
                {
                    exists = list.Any(d => idList.Contains(d.Id));
                }
                if (exists)
                {
                    lock (OPERATION_LOCKER)
                    {
                        list = RedisClient.HashGet<List<MemoryCacheOperationQueue>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_QUEUE);
                        if (list != null && list.Any(d => idList.Contains(d.Id)))
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                MemoryCacheOperationQueue item = list[i];
                                if (idList.Contains(item.Id))
                                {
                                    list.Remove(item);
                                }
                            }
                            RedisClient.HashSet(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_QUEUE, list);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }

        public MemoryCacheOperationRecord? GetRecord(MemoryCacheOperationType type, string queueId, string applicationId)
        {
            List<MemoryCacheOperationRecord>? list = RedisClient.HashGet<List<MemoryCacheOperationRecord>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_RECORD);
            if (list != null && list.Count > 0)
            {
                return list.Find(q => q.Type == type && q.QueueId == queueId && q.ApplicationId == applicationId);
            }
            return null;
        }

        public List<MemoryCacheOperationRecord> GetExpiredRecordList()
        {
            List<MemoryCacheOperationRecord>? list = RedisClient.HashGet<List<MemoryCacheOperationRecord>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_RECORD);
            if (list != null && list.Count > 0)
            {
                return list.FindAll(q => q.Status == CommonStatus.Disabled || q.ExpireTime <= DateTime.UtcNow);
            }
            return new List<MemoryCacheOperationRecord>();
        }

        public void SaveRecord(MemoryCacheOperationRecord record)
        {
            try
            {
                List<MemoryCacheOperationRecord>? list = RedisClient.HashGet<List<MemoryCacheOperationRecord>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_RECORD);
                MemoryCacheOperationRecord? existsRecord = null;
                if (list != null)
                {
                    existsRecord = list.Find(x => x.Type == record.Type && x.QueueId == record.QueueId && x.ApplicationId == record.ApplicationId);
                }
                if (existsRecord == null)
                {
                    lock (OPERATION_LOCKER)
                    {
                        list = RedisClient.HashGet<List<MemoryCacheOperationRecord>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_RECORD);
                        if (list != null)
                        {
                            existsRecord = list.Find(x => x.Type == record.Type && x.QueueId == record.QueueId && x.ApplicationId == record.ApplicationId);
                        }

                        if (existsRecord == null)
                        {
                            if (string.IsNullOrEmpty(record.Id))
                            {
                                record.Id = Guid.NewGuid().ToString("N");
                            }
                            record.Status = CommonStatus.Enabled;
                            record.CreateTime = DateTime.UtcNow;

                            if (list == null)
                            {
                                list = new List<MemoryCacheOperationRecord>();
                            }
                            list.Add(record);
                            RedisClient.HashSet(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_RECORD, list);
                        }
                    }
                }
                else
                {
                    existsRecord.UpdateTime = DateTime.UtcNow;
                    existsRecord.ExpireTime = record.ExpireTime;
                    RedisClient.HashSet(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_RECORD, list);
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }

        public void DeleteManyRecords(List<string> idList)
        {
            try
            {
                List<MemoryCacheOperationRecord>? list = RedisClient.HashGet<List<MemoryCacheOperationRecord>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_RECORD);
                bool exists = false;
                if (list != null)
                {
                    exists = list.Any(d => idList.Contains(d.Id));
                }
                if (exists)
                {
                    lock (OPERATION_LOCKER)
                    {
                        list = RedisClient.HashGet<List<MemoryCacheOperationRecord>>(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_RECORD);
                        if (list != null && list.Any(d => idList.Contains(d.Id)))
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                MemoryCacheOperationRecord item = list[i];
                                if (idList.Contains(item.Id))
                                {
                                    list.Remove(item);
                                }
                            }
                            RedisClient.HashSet(REDIS_KEY_MEMORY_CACHE_OPERATION, REDIS_FIELD_KEY_OPERATION_RECORD, list);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RampageLogger.Error(ex);
            }
        }


    }
}
