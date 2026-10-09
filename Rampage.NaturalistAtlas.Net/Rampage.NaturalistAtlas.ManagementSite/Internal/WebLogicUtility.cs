using Microsoft.AspNetCore.Html;
using Rampage.Data;
using Rampage.Data.Query;
using Rampage.Enums;
using Rampage.Extensions;
using Rampage.Localization;
using Rampage.Management.Dto;
using Rampage.Management.Web;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Utilities;
using Rampage.Web.Component;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.ManagementSite.Internal
{
    /// <summary>
    /// 站点级视图逻辑工具：菜单树渲染与跨页面共享的选项构建
    /// Site-level view-logic utilities: menu tree rendering and options shared across pages
    /// </summary>
    public static class WebLogicUtility
    {
        /// <summary>
        /// 全量城市下拉选项：展示名以「城市（国家）」消歧义（种子有同名城市）。
        /// 观察列表/采集任务/日志筛选与常规设置页共用；含停用城市（历史观察可能属于已停用城市）
        /// Full city dropdown options with names disambiguated as "city (country)" (seeded names
        /// collide); shared by the observation/sync/log filters and the settings page, disabled
        /// cities included (history may belong to them)
        /// </summary>
        /// <param name="cityService">城市服务 / City service</param>
        /// <param name="pageSize">拉取上限（种子 362 城，默认 500 全量）/ Fetch cap (362 seeded cities; 500 takes all)</param>
        /// <returns>下拉选项 / Dropdown options</returns>
        /// <summary>
        /// Timestamp-column end-of-day normalization: a date input yields midnight, which drops the
        /// rest of the day on timestamp columns. Used by both page controllers (list filters) and
        /// API controllers (CSV export) -- the old client-side rewrite appending 'T23:59:59' into
        /// the date input was wiped by the HTML value-sanitization rule (silently lost filters).
        /// DATE columns are unaffected; only for create_time/start_time style columns.
        /// </summary>
        public static DateTime? IncludeEndOfDay(DateTime? end)
        {
            return end.HasValue && end.Value.TimeOfDay == TimeSpan.Zero
                ? end.Value.AddDays(1).AddSeconds(-1)
                : end;
        }

        public static List<Models.CityOption> BuildCityOptions(ICityService cityService, int pageSize = 500)
        {
            (List<City> cityList, Dictionary<int, string> countryNameDict, _) =
                cityService.GetCityPageList(new CityAdminFilter(), new PagerInfo { PageIndex = 1, PageSize = pageSize });
            return cityList.ConvertAll(c => new Models.CityOption
            {
                Id = c.Id,
                DisplayName = countryNameDict.TryGetValue(c.CountryId, out string? countryName)
                    ? $"{c.Name}（{countryName}）"
                    : (c.Name ?? c.Id.ToString())
            });
        }



        public static string BuildManageMenuTableRowHtml(ManageMenuDto item, int? _, int depth, int i, LocalizationModule lang, TimeZoneInfo ctz)
        {
            string depthPadding = depth > 0 ? "|" + StringTool.LoopString("-", depth * 4) : "";

            string? actionTitle = item.ActionInfo != null ? item.ActionInfo.GetTitle() : "-";

            return @$"
<tr id=""trDataItem_{item.Id}"">
    <td>{item.Id}</td>
    <td style=""text-align:left;"">{depthPadding}<a href=""{item.Url}"" target=""_blank"">{item.Title}</a></td>
    <td>{actionTitle}</td>
    <td class=""sp-mono"">{item.Url}</td>
    <td>{item.SortOrder}</td>
    <td>
        <span class=""badge bg-success menu-status-enabled"" style=""{(item.Status == CommonStatus.Enabled ? "" : "display:none;")}"">{(EnumReader.GetText(CommonStatus.Enabled))}</span>
        <span class=""badge bg-danger menu-status-disabled"" style=""{(item.Status == CommonStatus.Disabled ? "" : "display:none;")}"">{(EnumReader.GetText(CommonStatus.Disabled))}</span>
    </td>
    <td>
        <div>{(DateTimeTool.UtcToLocal(item.CreateTime, ctz).ToCommonString())}</div>
        <div class=""text-muted small"">{(DateTimeTool.UtcToLocal(item.UpdateTime, ctz).ToCommonString())}</div>
    </td>
    <td class=""text-nowrap"">
        <button type=""button"" data-id=""{item.Id}"" data-action=""disable-menu"" class=""btn btn-sm btn-warning"" style=""{(item.Status == CommonStatus.Enabled ? "" : "display:none;")}"">
            <i class=""fa-solid fa-ban me-1""></i>{lang.Get("Disable")}
        </button>
        <button type=""button"" data-id=""{item.Id}"" data-action=""enable-menu"" class=""btn btn-sm btn-success"" style=""{(item.Status == CommonStatus.Disabled ? "" : "display:none;")}"">
            <i class=""fa-solid fa-check me-1""></i>{lang.Get("Enable")}
        </button>
        <a href=""/Admin/Menu_Modify?MenuId={item.Id}"" class=""btn btn-sm btn-info"">
            <i class=""fa-solid fa-pen-to-square me-1""></i>{lang.Get("Edit")}
        </a>
        <button type=""button"" data-id=""{item.Id}"" data-action=""delete-menu"" class=""btn btn-sm btn-danger"">
            <i class=""fa-solid fa-trash me-1""></i>{lang.Get("Delete")}
        </button>
    </td>
</tr>
";
        }

        public static IHtmlContent RenderMenuTreeTableHtml(List<ManageMenuDto> treeMenuList, LocalizationModule lang, TimeZoneInfo ctz, int? defaultId = null, int? maxDepth = null)
        {
            return ManagementLogicUtility.RenderMenuTreeHtml(treeMenuList, BuildManageMenuTableRowHtml, lang, ctz, defaultId, maxDepth);
        }

        public static IHtmlContent RenderMenuTreeSelectorHtml(List<ManageMenuDto> treeMenuList, LocalizationModule lang, TimeZoneInfo ctz, int? defaultId = null, int? maxDepth = null, int? excludeId = null)
        {
            return ManagementLogicUtility.RenderMenuTreeHtml(treeMenuList, (ManageMenuDto item, int? did, int depth, int i, LocalizationModule lang, TimeZoneInfo ctz) => {
                string text = item.Title!;
                string value = item.Id.ToString();
                string title = item.Title!;
                bool isSelected = false;
                if (did != null)
                {
                    isSelected = did.Value == item.Id;
                }
                string itemHtml = SelectorGenerator.BuildOptionHtml(text, value, title, "", isSelected, depth);
                return itemHtml;
            }, lang, ctz, defaultId, maxDepth, excludeId);
        }



    }
}
