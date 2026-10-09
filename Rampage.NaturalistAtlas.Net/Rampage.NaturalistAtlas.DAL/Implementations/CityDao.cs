using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 城市 Dao 实现 / City Dao implementation
    /// </summary>
    public class CityDao : SuperDao<City>, ICityDao
    {
        /// <inheritdoc/>
        public List<GeoSearchRowDto> GetGeoSearchList(string keyword, int limit)
        {
            // 城市中心点取 bbox 中点（city 表无独立中心点列）；省份/国家用表内定位中心点
            // A city's center is its bbox midpoint (the city table has no center column); provinces/countries use their stored centers
            return DatabaseClient.SelectList<GeoSearchRowDto>(
                @"SELECT Level, Id, Name, NameEn, ParentName, Lng, Lat, SwLng, SwLat, NeLng, NeLat, Slug FROM (
                    SELECT 'city' AS Level, c.id AS Id, c.name AS Name, c.name_en AS NameEn,
                        COALESCE(p.name, co.name) AS ParentName,
                        ((c.sw_lng + c.ne_lng) / 2)::float8 AS Lng, ((c.sw_lat + c.ne_lat) / 2)::float8 AS Lat,
                        c.sw_lng::float8 AS SwLng, c.sw_lat::float8 AS SwLat, c.ne_lng::float8 AS NeLng, c.ne_lat::float8 AS NeLat,
                        c.slug AS Slug, 1 AS Ord
                    FROM city c
                    LEFT JOIN province p ON p.id = c.province_id
                    LEFT JOIN country co ON co.id = c.country_id
                    WHERE c.status = 1 AND (c.name ILIKE @Keyword OR c.name_en ILIKE @Keyword OR c.slug ILIKE @Keyword)
                    UNION ALL
                    SELECT 'province', p.id, p.name, p.name_en, co.name,
                        p.lng::float8, p.lat::float8, NULL, NULL, NULL, NULL, NULL, 2
                    FROM province p
                    LEFT JOIN country co ON co.id = p.country_id
                    WHERE p.status = 1 AND (p.name ILIKE @Keyword OR p.name_en ILIKE @Keyword OR p.code ILIKE @Exact ESCAPE '')
                    UNION ALL
                    SELECT 'country', co.id, co.name, co.name_en, NULL,
                        co.lng::float8, co.lat::float8, NULL, NULL, NULL, NULL, NULL, 3
                    FROM country co
                    WHERE co.status = 1 AND (co.name ILIKE @Keyword OR co.name_en ILIKE @Keyword OR co.code ILIKE @Exact ESCAPE '')
                ) x
                ORDER BY Ord, length(Name), Name
                LIMIT @Limit",
                new { Keyword = AtlasFilterSql.LikePattern(keyword), Exact = AtlasFilterSql.EscapeLike(keyword), Limit = limit });
        }
    }
}
