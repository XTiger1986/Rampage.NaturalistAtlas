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
    /// 国家管理服务实现（管理后台） / Country admin service implementation
    /// </summary>
    public class CountryService : SuperService<Country, ICountryDao>, ICountryService
    {
        /// <summary>
        /// 国家代码合法格式：2 位小写字母（ISO 3166-1 alpha-2）
        /// Legal country code: 2 lowercase letters (ISO 3166-1 alpha-2)
        /// </summary>
        private static readonly Regex CountryCodePattern = new("^[a-z]{2}$", RegexOptions.Compiled);

        [Auto]
        protected ICountryDao CountryDao = default!;

        [Auto]
        protected IProvinceDao ProvinceDao = default!;

        [Auto]
        protected ICityDao CityDao = default!;


        /// <inheritdoc/>
        public List<Country> GetCountryPageList(CountryAdminFilter filter, PagerInfo pager)
        {
            FilterWrapper<Country> wrapper = new(filter);
            // 中文名 A→Z（中文按拼音序近似由 PG 排序规则处理）
            wrapper.OrderBy(m => m.Name);
            return CountryDao.SelectPage(pager, wrapper);
        }

        /// <inheritdoc/>
        public Country? GetCountry(int countryId)
        {
            if (countryId <= 0)
            {
                return null;
            }
            return CountryDao.SelectById(countryId);
        }

        /// <inheritdoc/>
        public R SaveCountry(CountryInput input)
        {
            if (input == null)
            {
                return R.Failure("提交数据无效");
            }
            if (!Enum.IsDefined(input.Status))
            {
                return R.Failure("状态取值无效");
            }
            if (!CountryCodePattern.IsMatch(input.Code ?? string.Empty))
            {
                return R.Failure("国家代码须为 2 位小写字母（ISO 3166-1 alpha-2，如 cn）");
            }
            if (input.Lng < -180 || input.Lng > 180 || input.Lat < -90 || input.Lat > 90)
            {
                return R.Failure("定位坐标取值非法：经度 [-180,180]、纬度 [-90,90]");
            }

            // code 唯一性：新增查重；修改排除自身
            Country? sameCode = CountryDao.SelectOne(c => c.Code == input.Code);
            if (sameCode != null && sameCode.Id != input.Id)
            {
                return R.Failure($"国家代码 {input.Code} 已被「{sameCode.Name}」占用");
            }

            if (input.Id <= 0)
            {
                Country po = new Country
                {
                    Code = input.Code!,
                    Name = input.Name,
                    NameEn = input.NameEn,
                    Lng = input.Lng,
                    Lat = input.Lat,
                    Status = input.Status,
                    CreateTime = UtcNow,
                    UpdateTime = UtcNow
                };
                if (!CountryDao.Insert(po))
                {
                    return R.Failure();
                }
                return R.Success(po.Id);
            }

            Country? origin = CountryDao.SelectById(input.Id);
            if (origin == null)
            {
                return R.Failure("指定的国家不存在");
            }
            origin.Code = input.Code!;
            origin.Name = input.Name;
            origin.NameEn = input.NameEn;
            origin.Lng = input.Lng;
            origin.Lat = input.Lat;
            origin.Status = input.Status;
            origin.UpdateTime = UtcNow;
            return CountryDao.UpdateById(origin) ? R.Success(origin.Id) : R.Failure();
        }

        /// <inheritdoc/>
        public R ChangeCountryStatus(int countryId, CommonStatus status)
        {
            if (countryId <= 0 || !Enum.IsDefined(status))
            {
                return R.Failure("参数无效");
            }
            Country? origin = CountryDao.SelectById(countryId);
            if (origin == null)
            {
                return R.Failure("指定的国家不存在");
            }
            if (origin.Status == status)
            {
                return R.Success();
            }
            origin.Status = status;
            origin.UpdateTime = UtcNow;
            return CountryDao.UpdateById(origin) ? R.Success() : R.Failure();
        }

        /// <inheritdoc/>
        public R DeleteCountry(int countryId)
        {
            if (countryId <= 0 || CountryDao.SelectById(countryId) == null)
            {
                return R.Failure("指定的国家不存在");
            }
            // 引用校验：省份或城市任一存在即拒绝（无外键，业务层约束）
            if (ProvinceDao.Count(p => p.CountryId == countryId) > 0)
            {
                return R.Failure("该国家下已有一级区划，不能删除（可改为停用）");
            }
            if (CityDao.Count(c => c.CountryId == countryId) > 0)
            {
                return R.Failure("该国家下已有城市，不能删除（可改为停用）");
            }
            return CountryDao.DeleteById(countryId) ? R.Success() : R.Failure();
        }
    }

    /// <summary>
    /// 省份管理服务实现（管理后台） / Province admin service implementation
    /// </summary>
    public class ProvinceService : SuperService<Province, IProvinceDao>, IProvinceService
    {
        /// <summary>
        /// 区划码合法格式：ISO 3166-2 风格（大写字母与数字、连字符，如 CN-BJ / JP-13）
        /// Legal subdivision code: ISO 3166-2 style (uppercase letters, digits, hyphen)
        /// </summary>
        private static readonly Regex ProvinceCodePattern = new("^[A-Z]{2}-[A-Z0-9]{1,3}$", RegexOptions.Compiled);

        [Auto]
        protected IProvinceDao ProvinceDao = default!;

        [Auto]
        protected ICountryDao CountryDao = default!;

        [Auto]
        protected ICityDao CityDao = default!;


        /// <inheritdoc/>
        public (List<Province>, Dictionary<int, string>) GetProvincePageList(ProvinceAdminFilter filter, PagerInfo pager)
        {
            FilterWrapper<Province> wrapper = new(filter);
            // 国家序 + 中文名序
            wrapper.OrderBy(m => m.CountryId);
            wrapper.OrderBy(m => m.Name);

            List<Province> list = ProvinceDao.SelectPage(pager, wrapper);

            Dictionary<int, string> countryNameDict = new Dictionary<int, string>();
            List<int> distinctCountryIdList = list.ConvertAll(p => p.CountryId).Distinct().ToList();
            if (distinctCountryIdList.Count > 0)
            {
                foreach (Country country in CountryDao.SelectByIds(distinctCountryIdList))
                {
                    countryNameDict[country.Id] = country.Name;
                }
            }
            return (list, countryNameDict);
        }

        /// <inheritdoc/>
        public Province? GetProvince(int provinceId)
        {
            if (provinceId <= 0)
            {
                return null;
            }
            return ProvinceDao.SelectById(provinceId);
        }

        /// <inheritdoc/>
        public R SaveProvince(ProvinceInput input)
        {
            if (input == null)
            {
                return R.Failure("提交数据无效");
            }
            if (!Enum.IsDefined(input.Status))
            {
                return R.Failure("状态取值无效");
            }
            if (!string.IsNullOrWhiteSpace(input.Code) && !ProvinceCodePattern.IsMatch(input.Code))
            {
                return R.Failure("区划码须为 ISO 3166-2 风格（如 CN-BJ / JP-13），或留空");
            }
            if (input.Lng < -180 || input.Lng > 180 || input.Lat < -90 || input.Lat > 90)
            {
                return R.Failure("定位坐标取值非法：经度 [-180,180]、纬度 [-90,90]");
            }
            if (input.CountryId <= 0 || CountryDao.SelectById(input.CountryId) == null)
            {
                return R.Failure("指定的国家不存在");
            }

            // (国家, 区划码) 唯一：新增查重；修改排除自身（空码不参与唯一约束）
            if (!string.IsNullOrWhiteSpace(input.Code))
            {
                string code = input.Code;
                Province? sameCode = ProvinceDao.SelectOne(p => p.CountryId == input.CountryId && p.Code == code);
                if (sameCode != null && sameCode.Id != input.Id)
                {
                    return R.Failure($"区划码 {code} 在该国家下已存在");
                }
            }
            // 同国名称查重：避免同国同名区划
            Province? sameName = ProvinceDao.SelectOne(p => p.CountryId == input.CountryId && p.Name == input.Name);
            if (sameName != null && sameName.Id != input.Id)
            {
                return R.Failure($"该国家下已存在同名区划「{input.Name}」");
            }

            if (input.Id <= 0)
            {
                Province po = new Province
                {
                    CountryId = input.CountryId,
                    Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code,
                    Name = input.Name,
                    NameEn = input.NameEn,
                    Lng = input.Lng,
                    Lat = input.Lat,
                    Status = input.Status,
                    CreateTime = UtcNow,
                    UpdateTime = UtcNow
                };
                if (!ProvinceDao.Insert(po))
                {
                    return R.Failure();
                }
                return R.Success(po.Id);
            }

            Province? origin = ProvinceDao.SelectById(input.Id);
            if (origin == null)
            {
                return R.Failure("指定的省份不存在");
            }
            origin.CountryId = input.CountryId;
            origin.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code;
            origin.Name = input.Name;
            origin.NameEn = input.NameEn;
            origin.Lng = input.Lng;
            origin.Lat = input.Lat;
            origin.Status = input.Status;
            origin.UpdateTime = UtcNow;
            return ProvinceDao.UpdateById(origin) ? R.Success(origin.Id) : R.Failure();
        }

        /// <inheritdoc/>
        public R ChangeProvinceStatus(int provinceId, CommonStatus status)
        {
            if (provinceId <= 0 || !Enum.IsDefined(status))
            {
                return R.Failure("参数无效");
            }
            Province? origin = ProvinceDao.SelectById(provinceId);
            if (origin == null)
            {
                return R.Failure("指定的省份不存在");
            }
            if (origin.Status == status)
            {
                return R.Success();
            }
            origin.Status = status;
            origin.UpdateTime = UtcNow;
            return ProvinceDao.UpdateById(origin) ? R.Success() : R.Failure();
        }

        /// <inheritdoc/>
        public R DeleteProvince(int provinceId)
        {
            if (provinceId <= 0 || ProvinceDao.SelectById(provinceId) == null)
            {
                return R.Failure("指定的省份不存在");
            }
            if (CityDao.Count(c => c.ProvinceId == provinceId) > 0)
            {
                return R.Failure("该省份下已有城市，不能删除（可改为停用）");
            }
            return ProvinceDao.DeleteById(provinceId) ? R.Success() : R.Failure();
        }

        /// <inheritdoc/>
        public List<Province> GetProvinceListByCountry(int countryId)
        {
            if (countryId <= 0)
            {
                return new List<Province>();
            }
            // PG 堆内更新行会漂移：内存排序兜底保证下拉稳定
            List<Province> list = ProvinceDao.SelectList(p => p.CountryId == countryId && p.Status == CommonStatus.Enabled);
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCulture));
            return list;
        }
    }
}
