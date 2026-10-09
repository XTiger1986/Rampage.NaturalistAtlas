using Rampage.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    /// <summary>
    /// 单机缓存操作记录
    /// Memory Cache Operation Record
    /// </summary>
    public class MemoryCacheOperationRecord
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
        /// 队列id
        /// </summary>
        public string? QueueId { get; set; }

        /// <summary>
        /// 应用程序实例唯一ID
        /// </summary>
        public string? ApplicationId { get; set; }

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
