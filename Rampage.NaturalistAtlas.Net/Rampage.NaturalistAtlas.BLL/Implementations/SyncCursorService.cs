using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 采集任务管理服务实现（管理后台） / Sync cursor admin service implementation
    /// </summary>
    public class SyncCursorService : SuperService<SyncCursor, ISyncCursorDao>, ISyncCursorService
    {
        [Auto]
        protected ISyncCursorDao SyncCursorDao = default!;

        [Auto]
        protected ICityDao CityDao = default!;


        /// <inheritdoc/>
        public (List<SyncCursor>, Dictionary<int, string>) GetSyncCursorPageList(SyncCursorAdminFilter filter, PagerInfo pager)
        {
            FilterWrapper<SyncCursor> wrapper = new(filter);
            wrapper.OrderBy(m => m.Source);
            wrapper.OrderBy(m => m.CityId);
            wrapper.OrderBy(m => m.Task);

            List<SyncCursor> list = SyncCursorDao.SelectPage(pager, wrapper);
            Dictionary<int, string> cityNameDict = BuildCityNameDict(list.ConvertAll(c => c.CityId));
            return (list, cityNameDict);
        }

        /// <inheritdoc/>
        public SyncTaskConfigEditDto? GetSyncCursorEdit(int id)
        {
            if (id <= 0)
            {
                return null;
            }
            SyncCursor? po = SyncCursorDao.SelectById(id);
            if (po == null)
            {
                return null;
            }
            City? city = CityDao.SelectById(po.CityId);

            JsonObject configNode = ParseConfig(po.Config);
            return new SyncTaskConfigEditDto
            {
                Id = po.Id,
                Source = po.Source,
                CityId = po.CityId,
                CityName = city?.Name,
                Task = po.Task,
                Watermark = po.Watermark,
                LastRunStatus = po.LastRunStatus,
                LastRunTime = po.LastRunTime,
                LastRunMessage = po.LastRunMessage,
                ConfigJson = po.Config,
                BatchSize = ReadInt(configNode, "BatchSize"),
                FromDate = ReadDateTime(configNode, "FromDate"),
                ToDate = ReadDateTime(configNode, "ToDate"),
                SampleSize = ReadInt(configNode, "SampleSize")
            };
        }

        /// <inheritdoc/>
        public R SaveSyncCursorConfig(SyncTaskConfigInput input)
        {
            if (input == null || input.Id <= 0)
            {
                return R.Failure("参数无效");
            }
            SyncCursor? origin = SyncCursorDao.SelectById(input.Id);
            if (origin == null)
            {
                return R.Failure("指定的采集任务不存在");
            }
            if (input.FromDate.HasValue && input.ToDate.HasValue && input.FromDate.Value > input.ToDate.Value)
            {
                return R.Failure("回补窗口起始日不能晚于截止日");
            }

            // 合并策略：在原 config 上仅覆盖表单声明的键，未列出的参数原样保留（Collector 后续扩展字段不丢失）
            JsonObject configNode = ParseConfig(origin.Config);
            bool windowChanged = false;
            if (input.BatchSize.HasValue) { configNode["BatchSize"] = input.BatchSize.Value; }
            if (input.FromDate.HasValue)
            {
                string newValue = input.FromDate.Value.ToString("yyyy-MM-dd");
                windowChanged |= ReadString(configNode, "FromDate") != newValue;
                configNode["FromDate"] = newValue;
            }
            if (input.ToDate.HasValue)
            {
                string newValue = input.ToDate.Value.ToString("yyyy-MM-dd");
                windowChanged |= ReadString(configNode, "ToDate") != newValue;
                configNode["ToDate"] = newValue;
            }
            if (input.SampleSize.HasValue) { configNode["SampleSize"] = input.SampleSize.Value; }
            if (windowChanged)
            {
                // 回补窗口被修改：清除回补进度键，让采集端按新窗口重新回补——否则 BfDone=true 的
                // 完成态会永远锁死（采集端只认 BfDone/BfNextDate，不会自行发现 ToDate 变大）
                // Backfill window edited: drop the progress keys so the collector re-runs the new
                // window — a done state (BfDone=true) would otherwise stay locked forever
                configNode.Remove("BfDone");
                configNode.Remove("BfNextDate");
                configNode.Remove("BfLastId");
                configNode.Remove("BfLastOffset");
            }

            UpdateFields<SyncCursor> fields = new();
            fields.Set(m => m.Config, configNode.ToJsonString(JsonSerializerOptions));
            fields.Set(m => m.UpdateTime, UtcNow);
            return SyncCursorDao.UpdateById(input.Id, fields) ? R.Success() : R.Failure();
        }

        /// <summary>
        /// 解析 config JSONB 为可写节点；空/非法时给空对象（保存时重建）
        /// Parse the config JSONB into a mutable node; empty or invalid yields an empty object
        /// </summary>
        private JsonObject ParseConfig(string? config)
        {
            if (!string.IsNullOrWhiteSpace(config))
            {
                try
                {
                    if (JsonNode.Parse(config) is JsonObject node)
                    {
                        return node;
                    }
                }
                catch (JsonException)
                {
                    // 非法 JSON 视为空配置
                }
            }
            return new JsonObject();
        }

        private int? ReadInt(JsonObject node, string key)
        {
            return node.TryGetPropertyValue(key, out JsonNode? value) && value != null
                ? (int?)value
                : null;
        }

        private DateTime? ReadDateTime(JsonObject node, string key)
        {
            if (node.TryGetPropertyValue(key, out JsonNode? value) && value != null
                && DateTime.TryParse(value.ToString(), out DateTime parsed))
            {
                return parsed;
            }
            return null;
        }

        private string? ReadString(JsonObject node, string key)
        {
            return node.TryGetPropertyValue(key, out JsonNode? value) && value != null ? value.ToString() : null;
        }

        private Dictionary<int, string> BuildCityNameDict(List<int> cityIdList)
        {
            Dictionary<int, string> dict = new Dictionary<int, string>();
            List<int> distinctIdList = cityIdList.Distinct().ToList();
            if (distinctIdList.Count == 0)
            {
                return dict;
            }
            foreach (City city in CityDao.SelectByIds(distinctIdList))
            {
                dict[city.Id] = city.Name ?? city.Id.ToString();
            }
            return dict;
        }

        private readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = false };
    }
}
