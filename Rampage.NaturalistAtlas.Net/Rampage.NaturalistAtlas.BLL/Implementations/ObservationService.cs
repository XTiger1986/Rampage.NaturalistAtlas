using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 观察数据管理服务实现（管理后台） / Observation admin service implementation
    /// </summary>
    public class ObservationService : SuperService<Observation, IObservationDao>, IObservationService
    {
        [Auto]
        protected IObservationDao ObservationDao = default!;

        [Auto]
        protected ITaxonDao TaxonDao = default!;

        [Auto]
        protected ICityDao CityDao = default!;

        [Auto]
        protected IReportDao ReportDao = default!;


        /// <inheritdoc/>
        public List<ObservationRowDto> GetObservationPageList(ObservationAdminFilter filter, PagerInfo pager, string sort = "create_desc")
        {
            FilterWrapper<Observation> wrapper = new(filter);
            // 观察日期区间由筛选器 ValueScope 原生构建；排序档位由列表页下拉传入（白名单外的取值回落默认）
            // Date ranges build natively via ValueScope; the sort comes from the list dropdown
            // (values outside the whitelist fall back to the default)
            switch (sort)
            {
                case "create_asc":
                    wrapper.OrderBy(m => m.CreateTime);
                    break;
                case "observed_desc":
                    wrapper.OrderByDescending(m => m.ObservedOn);
                    wrapper.OrderByDescending(m => m.CreateTime);
                    break;
                case "observed_asc":
                    wrapper.OrderBy(m => m.ObservedOn);
                    wrapper.OrderByDescending(m => m.CreateTime);
                    break;
                default:
                    wrapper.OrderByDescending(m => m.CreateTime);
                    break;
            }

            List<Observation> list = ObservationDao.SelectPage(pager, wrapper);
            if (list.Count == 0)
            {
                return new List<ObservationRowDto>();
            }

            // 批量补齐展示名：分类与城市各一次查询，避免逐行 N+1
            Dictionary<string, Taxon> taxonDict = TaxonDao.SelectByIds(list.Where(o => !string.IsNullOrEmpty(o.TaxonId))
                .Select(o => o.TaxonId!).Distinct().ToList())
                .ToDictionary(t => t.Id);
            Dictionary<int, City> cityDict = CityDao.SelectByIds(list.Select(o => o.CityId).Distinct().ToList())
                .ToDictionary(c => c.Id);

            return list.Select(po => new ObservationRowDto
            {
                Id = po.Id,
                Source = po.Source,
                SourceObservationId = po.SourceObservationId,
                CityId = po.CityId,
                CityName = cityDict.TryGetValue(po.CityId, out City? city) ? city.Name : null,
                TaxonId = po.TaxonId,
                TaxonScientificName = po.TaxonId != null && taxonDict.TryGetValue(po.TaxonId, out Taxon? taxon) ? taxon.ScientificName : null,
                TaxonCommonNameCn = po.TaxonId != null && taxonDict.TryGetValue(po.TaxonId, out Taxon? taxonCn) ? taxonCn.CommonNameCn : null,
                IconicTaxon = po.IconicTaxon,
                QualityGrade = po.QualityGrade,
                ObservedOn = po.ObservedOn,
                Lng = po.Lng,
                Lat = po.Lat,
                PhotoSquareUrl = po.PhotoSquareUrl,
                UserLogin = po.UserLogin,
                Status = po.Status,
                CreateTime = po.CreateTime
            }).ToList();
        }

        /// <inheritdoc/>
        public ObservationDetailDto? GetObservationDetail(string observationId)
        {
            if (string.IsNullOrWhiteSpace(observationId))
            {
                return null;
            }
            Observation? po = ObservationDao.SelectById(observationId);
            if (po == null)
            {
                return null;
            }

            Taxon? taxon = string.IsNullOrEmpty(po.TaxonId) ? null : TaxonDao.SelectById(po.TaxonId!);
            City? city = CityDao.SelectById(po.CityId);

            return new ObservationDetailDto
            {
                Observation = po,
                CityName = city?.Name,
                Taxon = taxon,
                ReportList = ReportDao.SelectList(r => r.ObservationId == observationId)
                    .OrderByDescending(r => r.CreateTime)
                    .ToList()
            };
        }

        /// <inheritdoc/>
        public R UpdateObservationStatus(string observationId, ObservationStatus status)
        {
            if (string.IsNullOrWhiteSpace(observationId))
            {
                return R.Failure("参数无效");
            }
            if (status != ObservationStatus.Normal && status != ObservationStatus.Hidden)
            {
                return R.Failure("仅允许在「正常」与「隐藏」之间切换；源站已删除的记录为只读");
            }
            Observation? origin = ObservationDao.SelectById(observationId);
            if (origin == null)
            {
                return R.Failure("指定的观察不存在");
            }
            if (origin.Status == ObservationStatus.SourceDeleted)
            {
                return R.Failure("源站已删除的记录为只读，不可变更状态");
            }
            if (origin.Status == status)
            {
                return R.Success();
            }

            UpdateFields<Observation> fields = new();
            fields.Set(m => m.Status, status);
            fields.Set(m => m.UpdateTime, UtcNow);
            bool ok = ObservationDao.UpdateById(observationId, fields);
            if (ok)
            {
                InvalidatePublicCaches();
            }
            return ok ? R.Success() : R.Failure();
        }

        /// <inheritdoc/>
        public R UpdateObservationsHiddenByAuthor(string userLogin)
        {
            if (string.IsNullOrWhiteSpace(userLogin))
            {
                return R.Failure("参数无效");
            }

            // 单条批量 UPDATE（事务内）：此前列全量载入 + 逐行 UpdateById，万条 = 万次往返且部分失败留混合态
            // One bulk UPDATE in a transaction (the old per-row loop meant one round-trip per row and
            // a mixed state on partial failure)
            int affected = ObservationDao.UpdateHiddenByAuthor(userLogin, UtcNow);
            if (affected == 0)
            {
                return R.Failure($"观察者「{userLogin}」名下没有可隐藏的正常观察");
            }
            InvalidatePublicCaches();
            return R.Success(affected);
        }

        /// <summary>
        /// 隐藏/恢复后主动失效公开站缓存前缀（NaturalistAtlas:Public:*）：
        /// 榜单/详情/总览/物候缓存 10 分钟，超出产品验收"5 分钟内不可见"
        /// Invalidate the public cache prefix after hide/restore (NaturalistAtlas:Public:*):
        /// rank/detail/overview caches live 10 minutes, exceeding the acceptance "invisible within 5"
        /// </summary>
        protected void InvalidatePublicCaches()
        {
            RemoveWebCacheByPrefix(Rampage.NaturalistAtlas.Common.CacheKeys.PUBLIC_PREFIX);
        }
    }
}
