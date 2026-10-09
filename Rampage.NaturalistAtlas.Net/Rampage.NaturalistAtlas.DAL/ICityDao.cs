using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 城市 Dao / City Dao
    /// </summary>
    public interface ICityDao : ISuperDao<City>
    {
        /// <summary>
        /// 公开站地点搜索：国家 / 省级行政区 / 城市三级模糊匹配（仅启用项；城市 → 省 → 国家排序）
        /// Public geo search: fuzzy match across countries, provinces and cities (enabled only; ordered city → province → country)
        /// </summary>
        /// <param name="keyword">关键字（中英文名；国家/区划代码精确匹配）/ Keyword (Chinese/English names; exact match on country/region codes)</param>
        /// <param name="limit">条数上限 / Limit</param>
        /// <returns>搜索行 / Search rows</returns>
        List<GeoSearchRowDto> SelectGeoSearch(string keyword, int limit);
    }
}
