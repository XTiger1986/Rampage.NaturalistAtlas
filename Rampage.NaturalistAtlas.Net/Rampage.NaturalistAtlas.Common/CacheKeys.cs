using System;

namespace Rampage.NaturalistAtlas.Common
{
    /// <summary>
    /// 缓存 / 分布式锁 Key 命名规范。
    /// 格式：na:{scope}:{...}；实际 Redis 仍可能再叠配置 PrefixKey。
    /// </summary>
    public class CacheKeys
    {

        public const string Prefix = "na";

        /// <summary>写接口幂等键：na:idem:{scope}:{key}</summary>
        public static string Idempotency(string scope, string key) => $"{Prefix}:idem:{scope}:{key}";

        /// <summary>
        /// 公开站 Web 缓存键前缀（各 Atlas 服务内联键必须以此为前缀；隐藏/恢复观察后按此整体失效）
        /// Public-site web cache key prefix (inline keys in the Atlas services must start with it;
        /// hide/restore invalidates the whole prefix)
        /// </summary>
        public const string PublicPrefix = "NaturalistAtlas:Public:";

    }
}
