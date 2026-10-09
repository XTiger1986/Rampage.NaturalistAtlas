using Rampage.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Dto
{

    public enum MemoryCacheOperationType
    {
        Delete = 0
    }

    /// <summary>
    /// 单机缓存操作队列
    /// Memory Cache Operation Queue
    /// </summary>
    public class MemoryCacheOperationQueue
    {

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; } = default!;

        /// <summary>
        /// 操作类别
        /// </summary>
        public MemoryCacheOperationType Type { get; set; }

        /// <summary>
        /// 缓存键名
        /// </summary>
        public string? CacheKey { get; set; }

        /// <summary>
        /// 是否键名前缀
        /// </summary>
        public bool IsPrefix { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public CommonStatus Status { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime ExpireTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

    }
}
