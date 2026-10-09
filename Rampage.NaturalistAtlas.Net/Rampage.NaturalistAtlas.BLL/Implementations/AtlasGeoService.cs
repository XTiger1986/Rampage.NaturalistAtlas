using Rampage.DependencyInjection;
using Rampage.Enums;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 公开站地理服务实现 / Public geo service implementation
    /// </summary>
    public class AtlasGeoService : SuperService<City, ICityDao>, IAtlasGeoService
    {
        /// <summary>城市列表缓存键 / City list cache key</summary>
        private const string CACHE_KEY_CITY_LIST = "NaturalistAtlas:Public:CityList";

        /// <summary>城市列表缓存秒数 / City list cache lifetime in seconds</summary>
        private const double CITY_LIST_CACHE_SECONDS = 600;

        [Auto]
        protected ICityDao CityDao = default!;

        [Auto]
        protected ICountryDao CountryDao = default!;

        [Auto]
        protected IProvinceDao ProvinceDao = default!;


        /// <inheritdoc/>
        public List<CityItemDto> GetCityList()
        {
            return HandleWebCache(() => {
                // 国家/省份为小表，整表载入内存做名称映射，避免逐行查询
                // Countries/provinces are small tables: load them whole for name lookup instead of per-row queries
                Dictionary<int, string> countryDict = CountryDao.SelectList(c => true).ToDictionary(c => c.Id, c => c.Name);
                Dictionary<int, string> provinceDict = ProvinceDao.SelectList(p => true).ToDictionary(p => p.Id, p => p.Name);

                return CityDao.SelectList(c => c.Status == CommonStatus.Enabled)
                    .OrderBy(c => c.CountryId).ThenBy(c => c.Id)
                    .Select(c => new CityItemDto
                    {
                        Id = c.Id,
                        Name = c.Name ?? string.Empty,
                        NameEn = c.NameEn,
                        Slug = c.Slug ?? c.Id.ToString(),
                        CountryId = c.CountryId,
                        CountryName = countryDict.TryGetValue(c.CountryId, out string? countryName) ? countryName : null,
                        ProvinceName = c.ProvinceId.HasValue && provinceDict.TryGetValue(c.ProvinceId.Value, out string? provinceName) ? provinceName : null,
                        Bbox = new[] { (double)c.SwLng, (double)c.SwLat, (double)c.NeLng, (double)c.NeLat },
                        Center = new[] { (double)(c.SwLng + c.NeLng) / 2, (double)(c.SwLat + c.NeLat) / 2 }
                    })
                    .ToList();
            }, CACHE_KEY_CITY_LIST, CITY_LIST_CACHE_SECONDS) ?? new List<CityItemDto>();
        }

        /// <inheritdoc/>
        public List<GeoSearchItemDto> Search(string? keyword, int limit)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<GeoSearchItemDto>();
            }
            // 关键字截断到 50 字，防止超长模式串拖慢 ILIKE
            // Truncate the keyword to 50 chars so an oversized pattern cannot slow down ILIKE
            string trimmed = keyword.Trim();
            if (trimmed.Length > 50)
            {
                trimmed = trimmed.Substring(0, 50);
            }
            limit = Math.Clamp(limit, 1, 30);

            return CityDao.SelectGeoSearch(trimmed, limit).Select(row => new GeoSearchItemDto
            {
                Level = row.Level,
                Id = row.Id,
                Name = row.Name,
                NameEn = row.NameEn,
                ParentName = row.ParentName,
                Center = new[] { row.Lng, row.Lat },
                Bbox = row.SwLng.HasValue && row.SwLat.HasValue && row.NeLng.HasValue && row.NeLat.HasValue
                    ? new[] { row.SwLng.Value, row.SwLat.Value, row.NeLng.Value, row.NeLat.Value }
                    : null,
                Slug = row.Slug
            }).ToList();
        }
    }
}
