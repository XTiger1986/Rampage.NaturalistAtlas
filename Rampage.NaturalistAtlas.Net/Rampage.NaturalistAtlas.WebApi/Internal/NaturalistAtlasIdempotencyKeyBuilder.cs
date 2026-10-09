using Rampage.Idempotency;
using Rampage.NaturalistAtlas.Common;

namespace Rampage.NaturalistAtlas.WebApi.Internal
{
    /// <summary>
    /// 幂等键
    /// </summary>
    public sealed class NaturalistAtlasIdempotencyKeyBuilder : IIdempotencyKeyBuilder
    {
        public string Build(string requestPath, string clientKey)
        {
            string scope = string.IsNullOrWhiteSpace(requestPath) ? "api" : requestPath;
            return CacheKeys.Idempotency(scope, clientKey);
        }
    }
}
