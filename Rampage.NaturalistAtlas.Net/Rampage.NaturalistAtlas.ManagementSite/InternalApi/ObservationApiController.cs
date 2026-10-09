using Microsoft.AspNetCore.Mvc;
using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Localization;
using Rampage.Management;
using Rampage.Management.Mvc;
using Rampage.Management.Permission;
using Rampage.Model;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.ManagementSite.Internal;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.Validation;
using System;

namespace Rampage.NaturalistAtlas.ManagementSite.InternalApi
{
    /// <summary>
    /// 观察数据管理接口：隐藏/恢复、按作者批量隐藏、CSV 导出
    /// Observation admin API: hide / restore, hide-by-author, CSV export
    /// </summary>
    [Route("api/observation")]
    public class ObservationApiController : NaturalistAtlasManagementApiController
    {
        /// <summary>导出上限（运营归档场景足够；更大规模走数据库工具）/ Export cap (enough for ops archiving; larger sets belong to db tooling)</summary>
        private const int ExportLimit = 5000;

        [Auto]
        protected IObservationService ObservationService = default!;


        /// <summary>
        /// 隐藏/恢复观察（仅在正常与隐藏之间切换；源站已删除只读）
        /// Hide or restore an observation (normal ↔ hidden only; source-deleted is read-only)
        /// </summary>
        [PermissionAction(Title = "隐藏/恢复观察", Resource = "ObservationOperation")]
        [RecordOperationFilter]
        [HttpPost("status")]
        public R QueryObservationStatus([FromForm][ValidAttribute] ObservationStatusInput input)
        {
            return Execute(() => ObservationService.ChangeObservationStatus(input.ObservationId, input.Status));
        }

        /// <summary>
        /// 按作者批量隐藏：将该观察者名下全部正常观察置为隐藏
        /// Hide by author: hides every normal observation of the given observer
        /// </summary>
        [PermissionAction(Title = "按作者隐藏观察", Resource = "ObservationOperation")]
        [RecordOperationFilter]
        [HttpPost("hide-by-author")]
        public R QueryHideByAuthor([FromForm][ValidAttribute] HideByAuthorInput input)
        {
            return Execute(() => ObservationService.HideObservationsByAuthor(input.UserLogin));
        }

        /// <summary>
        /// CSV 导出：按当前列表筛选（含排序档位）导出最多 5000 行，UTF-8 BOM 供 Excel 直开
        /// CSV export: up to 5,000 rows under the current list filter (sort included); UTF-8 BOM for Excel
        /// </summary>
        [PermissionAction(Title = "导出观察数据", Type = PermissionActionType.Query, Resource = "ObservationManagement")]
        [HttpGet("export")]
        public IActionResult ExportList([FromQuery] ObservationAdminFilter filter, [FromQuery] string sort = "create_desc")
        {
            try
            {
                var rows = ObservationService.GetObservationPageList(
                    filter, new PagerInfo { PageIndex = 1, PageSize = ExportLimit }, sort);
                string csv = CsvTool.Build(
                    new[] { "观察Id", "数据源", "源站观察ID", "城市", "学名", "中文名", "类群", "鉴定级别", "观察日期", "经度", "纬度", "观察者", "状态", "入库时间(UTC)" },
                    rows.ConvertAll(r => new object?[]
                    {
                        r.Id, EnumReader.GetText(r.Source), r.SourceObservationId, r.CityName,
                        r.TaxonScientificName, r.TaxonCommonNameCn, EnumReader.GetText(r.IconicTaxon),
                        EnumReader.GetText(r.QualityGrade), r.ObservedOn.ToString("yyyy-MM-dd"),
                        r.Lng, r.Lat, r.UserLogin, EnumReader.GetText(r.Status), r.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
                    }));
                return File(CsvTool.ToDownloadBytes(csv), "text/csv; charset=utf-8",
                    $"observations-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
            }
            catch (Exception ex)
            {
                LogError(ex);
                return StatusCode(500);
            }
        }
    }
}
