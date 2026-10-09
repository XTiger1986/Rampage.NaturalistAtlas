using Rampage.Data;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 城市集邮缺口缓存（wanted 分类快照：周边已记录、本市未记录）
    /// City collection-gap cache (wanted-taxon snapshots: recorded nearby, absent in the city)
    /// </summary>
    [TableMapping("city_wanted_taxon")]
    public class CityWantedTaxon
    {
        /// <summary>主键(雪花Id)</summary>
        [FieldMapping(PrimaryKey = true, ColumnName = "id")]
        public string Id { get; set; } = default!;

        /// <summary>city.id</summary>
        [FieldMapping(ColumnName = "city_id")]
        public int CityId { get; set; }

        /// <summary>数据源(1=iNaturalist,2=GBIF)</summary>
        [FieldMapping(ColumnName = "source")]
        public short Source { get; set; }

        /// <summary>源站分类id（快照冗余，不挂 taxon 外键）</summary>
        [FieldMapping(ColumnName = "source_taxon_id")]
        public string SourceTaxonId { get; set; } = string.Empty;

        /// <summary>分类阶元(同taxon.rank)</summary>
        [FieldMapping(ColumnName = "rank")]
        public short Rank { get; set; }

        /// <summary>学名</summary>
        [FieldMapping(ColumnName = "scientific_name")]
        public string? ScientificName { get; set; }

        /// <summary>中文常用名</summary>
        [FieldMapping(ColumnName = "common_name_cn")]
        public string? CommonNameCn { get; set; }

        /// <summary>英文常用名</summary>
        [FieldMapping(ColumnName = "common_name_en")]
        public string? CommonNameEn { get; set; }

        /// <summary>类群代码(1~14)</summary>
        [FieldMapping(ColumnName = "iconic_taxon")]
        public short IconicTaxon { get; set; }

        /// <summary>周边记录次数(源站口径)</summary>
        [FieldMapping(ColumnName = "nearby_count")]
        public int NearbyCount { get; set; }

        /// <summary>代表图方图URL</summary>
        [FieldMapping(ColumnName = "photo_square_url")]
        public string? PhotoSquareUrl { get; set; }

        /// <summary>代表图中图URL</summary>
        [FieldMapping(ColumnName = "photo_medium_url")]
        public string? PhotoMediumUrl { get; set; }

        /// <summary>代表图授权协议(0=保留所有权利,1~8同observation.license；版权署名用)</summary>
        [FieldMapping(ColumnName = "photo_license")]
        public short PhotoLicense { get; set; }

        /// <summary>创建时间(UTC)</summary>
        [FieldMapping(ColumnName = "create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>修改时间(UTC)</summary>
        [FieldMapping(ColumnName = "update_time")]
        public DateTime UpdateTime { get; set; }
    }
}
