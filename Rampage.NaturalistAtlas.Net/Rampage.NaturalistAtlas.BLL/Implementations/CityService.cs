using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.Model;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Text.RegularExpressions;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 城市管理服务实现（管理后台，全球城市三级档案） / City admin service implementation (worldwide hierarchy)
    /// </summary>
    public class CityService : SuperService<City, ICityDao>, ICityService
    {
        /// <summary>
        /// slug 合法格式：小写字母/数字段以连字符相连（如 beijing、xi-an、new-york）
        /// Legal slug format: lowercase alphanumeric segments joined by hyphens
        /// </summary>
        private static readonly Regex SLUG_PATTERN = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

        [Auto]
        protected ICityDao CityDao = default!;

        [Auto]
        protected ICountryDao CountryDao = default!;

        [Auto]
        protected IProvinceDao ProvinceDao = default!;

        [Auto]
        protected IObservationDao ObservationDao = default!;

        [Auto]
        protected ISyncCursorDao SyncCursorDao = default!;

        [Auto]
        protected IEmergenceStatDao EmergenceStatDao = default!;

        [Auto]
        protected IEmergenceHistoryDao EmergenceHistoryDao = default!;


        /// <inheritdoc/>
        public (List<City>, Dictionary<int, string>, Dictionary<int, string>) GetCityPageList(CityAdminFilter filter, PagerInfo pager)
        {
            FilterWrapper<City> wrapper = new(filter);
            // 国家序 → 省份序 → 城市名序，同域城市相邻展示；id 为 SERIAL 不漂移
            wrapper.OrderBy(m => m.CountryId);
            wrapper.OrderBy(m => m.ProvinceId);
            wrapper.OrderBy(m => m.Name);
            List<City> list = CityDao.SelectPage(pager, wrapper);

            Dictionary<int, string> countryNameDict = new Dictionary<int, string>();
            Dictionary<int, string> provinceNameDict = new Dictionary<int, string>();
            List<int> countryIdList = list.ConvertAll(c => c.CountryId).Distinct().ToList();
            List<int> provinceIdList = list.Where(c => c.ProvinceId.HasValue).Select(c => c.ProvinceId!.Value).Distinct().ToList();
            if (countryIdList.Count > 0)
            {
                foreach (Country country in CountryDao.SelectByIds(countryIdList))
                {
                    countryNameDict[country.Id] = country.Name;
                }
            }
            if (provinceIdList.Count > 0)
            {
                foreach (Province province in ProvinceDao.SelectByIds(provinceIdList))
                {
                    provinceNameDict[province.Id] = province.Name;
                }
            }
            return (list, countryNameDict, provinceNameDict);
        }

        /// <inheritdoc/>
        public City? GetCity(int cityId)
        {
            if (cityId <= 0)
            {
                return null;
            }
            return CityDao.SelectById(cityId);
        }

        /// <inheritdoc/>
        public R SaveCity(CityInput input)
        {
            if (input == null)
            {
                return R.Failure("提交数据无效");
            }
            if (!Enum.IsDefined(input.Status))
            {
                return R.Failure("状态取值无效");
            }
            if (!SLUG_PATTERN.IsMatch(input.Slug ?? string.Empty))
            {
                return R.Failure("URL标识仅允许小写字母、数字与连字符（如 beijing）");
            }
            if (input.SwLng < -180 || input.NeLng > 180 || input.SwLat < -90 || input.NeLat > 90
                || input.SwLng >= input.NeLng || input.SwLat >= input.NeLat)
            {
                return R.Failure("bbox 取值非法：经度 [-180,180]、纬度 [-90,90]，且西南角须小于东北角");
            }

            // 国家归属校验
            Country? country = CountryDao.SelectById(input.CountryId);
            if (country == null)
            {
                return R.Failure("指定的国家不存在");
            }
            // 省份归属校验：可空；填写时必须存在且属于所选国家
            if (input.ProvinceId.HasValue)
            {
                Province? province = ProvinceDao.SelectById(input.ProvinceId.Value);
                if (province == null)
                {
                    return R.Failure("指定的省份不存在");
                }
                if (province.CountryId != input.CountryId)
                {
                    return R.Failure($"省份「{province.Name}」不属于国家「{country.Name}」");
                }
            }

            // slug 唯一性：新增查重；修改排除自身
            City? sameSlug = CityDao.SelectOne(c => c.Slug == input.Slug);
            if (sameSlug != null && sameSlug.Id != input.Id)
            {
                return R.Failure($"URL标识 {input.Slug} 已被城市「{sameSlug.Name}（{sameSlug.Id}）」占用");
            }

            if (input.Id <= 0)
            {
                City po = new City
                {
                    CountryId = input.CountryId,
                    ProvinceId = input.ProvinceId,
                    Name = input.Name,
                    NameEn = string.IsNullOrWhiteSpace(input.NameEn) ? null : input.NameEn,
                    Slug = input.Slug,
                    SwLng = input.SwLng,
                    SwLat = input.SwLat,
                    NeLng = input.NeLng,
                    NeLat = input.NeLat,
                    Status = input.Status,
                    InatPlaceId = input.InatPlaceId,
                    CreateTime = UtcNow,
                    UpdateTime = UtcNow
                };
                if (!CityDao.Insert(po))
                {
                    return R.Failure();
                }
                return R.Success(po.Id);
            }

            City? origin = CityDao.SelectById(input.Id);
            if (origin == null)
            {
                return R.Failure("指定的城市不存在");
            }
            origin.CountryId = input.CountryId;
            origin.ProvinceId = input.ProvinceId;
            origin.Name = input.Name;
            origin.NameEn = string.IsNullOrWhiteSpace(input.NameEn) ? null : input.NameEn;
            origin.Slug = input.Slug;
            origin.SwLng = input.SwLng;
            origin.SwLat = input.SwLat;
            origin.NeLng = input.NeLng;
            origin.NeLat = input.NeLat;
            origin.Status = input.Status;
            // 源站档案：place id 可编辑；boundary_wkt 由采集端/种子维护，编辑页只读展示
            origin.InatPlaceId = input.InatPlaceId;
            origin.UpdateTime = UtcNow;
            return CityDao.UpdateById(origin) ? R.Success(origin.Id) : R.Failure();
        }

        /// <inheritdoc/>
        public R ChangeCityStatus(int cityId, CommonStatus status)
        {
            if (cityId <= 0 || !Enum.IsDefined(status))
            {
                return R.Failure("参数无效");
            }
            City? origin = CityDao.SelectById(cityId);
            if (origin == null)
            {
                return R.Failure("指定的城市不存在");
            }
            if (origin.Status == status)
            {
                return R.Success();
            }
            origin.Status = status;
            origin.UpdateTime = UtcNow;
            return CityDao.UpdateById(origin) ? R.Success() : R.Failure();
        }

        /// <inheritdoc/>
        public R DeleteCity(int cityId)
        {
            if (cityId <= 0 || CityDao.SelectById(cityId) == null)
            {
                return R.Failure("指定的城市不存在");
            }
            // 引用校验：观察、同步游标、物候统计任一存在即拒绝删除（无外键，靠业务层约束）
            if (ObservationDao.Count(o => o.CityId == cityId) > 0)
            {
                return R.Failure("该城市下已有观察数据，不能删除（可改为停用）");
            }
            if (SyncCursorDao.Count(s => s.CityId == cityId) > 0)
            {
                return R.Failure("该城市已配置采集任务，请先在采集模块清理任务");
            }
            if (EmergenceStatDao.Count(e => e.CityId == cityId) > 0
                || EmergenceHistoryDao.Count(e => e.CityId == cityId) > 0)
            {
                return R.Failure("该城市已有物候统计数据，不能删除");
            }
            return CityDao.DeleteById(cityId) ? R.Success() : R.Failure();
        }
    }
}
