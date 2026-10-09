using Rampage.NaturalistAtlas.Model.Dto.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rampage.NaturalistAtlas.Collection
{
    /// <summary>
    /// iNaturalist v2 API 客户端：只拼端点与参数，不做字段映射
    /// iNaturalist v2 API client: endpoint/parameter assembly only, no field mapping
    /// </summary>
    public class InatApiClient : SourceApiClient
    {
        /// <summary>合规 UA（源站建议附联系渠道）/ Compliance UA (the source suggests a contact channel)</summary>
        public const string ClientUserAgent = "NaturalistAtlas-Collector/1.0 (+https://1986z.com)";

        /// <summary>
        /// v2 显式字段清单（v2 默认最小响应必须显式要字段；不用 fields=all——all 与 order_by+d1 组合会触发源站 500，
        /// 且显式清单响应更轻，嵌套字段用点路径选取）
        /// v2 explicit field list (v2 defaults to a minimal response; fields=all is avoided — it triggers a
        /// source-side 500 combined with order_by + d1 — and the explicit list is lighter; nested fields use dot paths)
        /// </summary>
        public const string FieldsList =
            "id,uuid,observed_on,time_observed_at,time_observed_at_utc,created_at,updated_at,quality_grade,geojson," +
            "latitude,longitude,positional_accuracy,public_positional_accuracy,geoprivacy,taxon_geoprivacy," +
            "identifications_count,user.login,taxon.id,taxon.name,taxon.rank,taxon.iconic_taxon_id,taxon.ancestry," +
            "taxon.preferred_common_name,taxon.is_active,taxon.threatened,photos.url,photos.license_code";

        /// <summary>
        /// 构造 iNat 客户端 / Construct the iNat client
        /// </summary>
        /// <param name="baseUrl">v2 基址 / v2 base url</param>
        /// <param name="ratePerMinute">节流上限（次/分钟）/ Throttle cap (requests/minute)</param>
        public InatApiClient(string baseUrl, int ratePerMinute)
            : base("iNaturalist", baseUrl, ClientUserAgent, ratePerMinute)
        {
        }

        /// <summary>
        /// 观察搜索（参数由编排层组装：place_id/bbox、updated_since、d1/d2、id_above 等）
        /// Observation search (parameters assembled by the orchestrator: place_id/bbox, updated_since, d1/d2, id_above etc.)
        /// </summary>
        /// <param name="query">除 fields 外的查询参数 / Query params other than fields</param>
        /// <returns>响应 / The response</returns>
        public Task<InatObservationsResponse?> FetchObservationsAsync(Dictionary<string, object?> query)
        {
            Dictionary<string, object?> full = new Dictionary<string, object?>(query)
            {
                ["fields"] = FieldsList
            };
            return GetJsonAsync<InatObservationsResponse>("/observations", full);
        }

        /// <summary>
        /// 按源站ID批量取观察（删除对账；缺席即源站已删）。必须显式 per_page=200：源站默认 30/页，
        /// 不带该参数时响应被截断，缺席的 id 会被误判为已删除
        /// Fetch observations by source ids in batch (deletion check; ids missing from the response are
        /// deleted on the source). per_page=200 is mandatory: the source defaults to 30 per page, and a
        /// truncated response would make the missing ids look deleted
        /// </summary>
        /// <param name="sourceObservationIds">源站观察ID（单批须 ≤200）/ Source observation ids (≤200 per batch)</param>
        /// <returns>响应（仅含仍存在的记录）/ The response (only rows still present)</returns>
        public Task<InatObservationsResponse?> FetchObservationsByIdsAsync(List<string> sourceObservationIds)
        {
            return FetchObservationsAsync(new Dictionary<string, object?>
            {
                ["id"] = string.Join(",", sourceObservationIds),
                ["per_page"] = 200
            });
        }

        /// <summary>
        /// 取单个分类（分类刷新）/ Fetch one taxon (taxon refresh)
        /// </summary>
        /// <param name="sourceTaxonId">源站分类ID / Source taxon id</param>
        /// <returns>响应 / The response</returns>
        public Task<InatTaxaResponse?> FetchTaxaAsync(string sourceTaxonId)
        {
            return GetJsonAsync<InatTaxaResponse>($"/taxa/{sourceTaxonId}");
        }

        /// <summary>
        /// 集邮缺口：祖先分类下"周边已记录、指定地点未记录"的分类清单（仅 v1 提供 wanted 端点，
        /// 调用方须以 v1 基址构造客户端）。显式 per_page + page 翻页：默认 30/页会把缺口清单截短
        /// Collection gaps: taxa recorded nearby but not at the place (the wanted endpoint exists on v1
        /// only; callers must construct the client with the v1 base url). Explicit per_page + page
        /// paging — the 30-per-page default would truncate the gap list
        /// </summary>
        /// <param name="ancestorTaxonId">祖先源站分类ID（通常为 iconic 类群）/ Ancestor source taxon id (usually an iconic taxon)</param>
        /// <param name="placeId">地点ID / Place id</param>
        /// <param name="page">页码（1 起）/ Page number (1-based)</param>
        /// <returns>响应（字段形态宽松解析）/ The response (parsed leniently)</returns>
        public Task<InatWantedResponse?> FetchWantedTaxaAsync(string ancestorTaxonId, int placeId, int page = 1)
        {
            return GetJsonAsync<InatWantedResponse>($"/taxa/{ancestorTaxonId}/wanted",
                new Dictionary<string, object?>
                {
                    ["place_id"] = placeId,
                    ["per_page"] = 100,
                    ["page"] = page
                });
        }

        /// <summary>
        /// 全历史逐月观察直方图（仅 v1 提供；调用方须以 v1 基址构造客户端）。
        /// interval=month 时源站把全部年份聚合为 12 个月桶——一次请求即得该分类在该地点的完整物候年谱
        /// Full-history monthly observation histogram (v1 only; construct the client with the v1
        /// base url). With interval=month the source folds all years into 12 month buckets — one
        /// request yields the taxon's complete phenological year at the place
        /// </summary>
        /// <param name="taxonId">源站分类ID / Source taxon id</param>
        /// <param name="placeId">地点ID / Place id</param>
        /// <returns>直方图响应（宽松形态）/ The histogram response (lenient shape)</returns>
        public Task<InatHistogramResponse?> FetchObservationHistogramAsync(string taxonId, int placeId)
        {
            return GetJsonAsync<InatHistogramResponse>("/observations/histogram",
                new Dictionary<string, object?>
                {
                    ["taxon_id"] = taxonId,
                    ["place_id"] = placeId,
                    ["date_field"] = "observed",
                    ["interval"] = "month"
                });
        }
    }
}
