using Rampage.Data;
using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 观察记录主档表
    /// </summary>
    [TableMapping("observation")]
    public class Observation
    {

        /// <summary>
        /// 主键(雪花Id)
        /// </summary>
        [FieldMapping(PrimaryKey = true, ColumnName = "id")]
        public string Id { get; set; } = default!;
        
        /// <summary>
        /// 数据源(1=iNaturalist,2=GBIF) / Data source (1=iNaturalist, 2=GBIF)
        /// </summary>
        [FieldMapping(ColumnName = "source")]
        public AtlasSource Source { get; set; }
        
        /// <summary>
        /// 源站观察ID（原样字符串：数字/UUID/URN 等，仅等值匹配）
        /// Source observation id (verbatim string: numeric / UUID / URN; equality-match only)
        /// </summary>
        [FieldMapping(ColumnName = "source_observation_id")]
        public string SourceObservationId { get; set; } = default!;
        
        /// <summary>
        /// city.id
        /// </summary>
        [FieldMapping(ColumnName = "city_id")]
        public int CityId { get; set; }
        
        /// <summary>
        /// taxon.id(雪花Id)
        /// </summary>
        [FieldMapping(ColumnName = "taxon_id")]
        public string? TaxonId { get; set; }
        
        /// <summary>
        /// 类群(冗余自taxon,取值同taxon.iconic_taxon) / Iconic taxon (redundant copy of taxon.iconic_taxon)
        /// </summary>
        [FieldMapping(ColumnName = "iconic_taxon")]
        public IconicTaxon IconicTaxon { get; set; }
        
        /// <summary>
        /// 鉴定级别(1=research,2=needs_id,3=casual预留) / Quality grade (1=research, 2=needs_id, 3=casual reserved)
        /// </summary>
        [FieldMapping(ColumnName = "quality_grade")]
        public QualityGrade QualityGrade { get; set; }
        
        /// <summary>
        /// 观察日期(观察者本地日期,物候聚合口径；列类型 DATE，Npgsql 6+ 映射 DateOnly)
        /// Observed-on (observer-local date, phenology basis; DATE column, mapped to DateOnly by Npgsql 6+)
        /// </summary>
        [FieldMapping(ColumnName = "observed_on")]
        public DateOnly ObservedOn { get; set; }
        
        /// <summary>
        /// 观察时刻(UTC,可空:部分记录仅有日期)
        /// </summary>
        [FieldMapping(ColumnName = "observed_at")]
        public DateTime? ObservedAt { get; set; }
        
        /// <summary>
        /// 公开坐标经度
        /// </summary>
        [FieldMapping(ColumnName = "lng")]
        public decimal Lng { get; set; }
        
        /// <summary>
        /// 公开坐标纬度
        /// </summary>
        [FieldMapping(ColumnName = "lat")]
        public decimal Lat { get; set; }
        
        /// <summary>
        /// 位置精度(米,源站值,可空)
        /// </summary>
        [FieldMapping(ColumnName = "positional_accuracy")]
        public int? PositionalAccuracy { get; set; }
        
        /// <summary>
        /// 坐标保护(0=开放,1=模糊,2=私密) / Geoprivacy (0=open, 1=obscured, 2=private)
        /// </summary>
        [FieldMapping(ColumnName = "geoprivacy")]
        public GeoPrivacy Geoprivacy { get; set; }
        
        /// <summary>
        /// 照片方图URL(75,卡片缩略)
        /// </summary>
        [FieldMapping(ColumnName = "photo_square_url")]
        public string? PhotoSquareUrl { get; set; }
        
        /// <summary>
        /// 照片中图URL(500,卡片)
        /// </summary>
        [FieldMapping(ColumnName = "photo_medium_url")]
        public string? PhotoMediumUrl { get; set; }
        
        /// <summary>
        /// 照片大图URL(1024,详情)
        /// </summary>
        [FieldMapping(ColumnName = "photo_large_url")]
        public string? PhotoLargeUrl { get; set; }
        
        /// <summary>
        /// 首图授权(0=保留所有权利,1=CC0,2=CC-BY,3=CC-BY-SA,4=CC-BY-NC,5=CC-BY-NC-SA,6=CC-BY-ND,7=CC-BY-NC-ND)
        /// Photo license (0=all rights reserved, 1=CC0 ... 7=CC-BY-NC-ND)
        /// </summary>
        [FieldMapping(ColumnName = "license")]
        public AtlasLicense License { get; set; }
        
        /// <summary>
        /// 观察者公开昵称(GBIF为recordedBy,可含多人) / Observer public login (GBIF recordedBy, may list several)
        /// </summary>
        [FieldMapping(ColumnName = "user_login")]
        public string? UserLogin { get; set; }
        
        /// <summary>
        /// 记录类型(可空:0=人类观察,1=机器观察,2=标本) / Record basis (nullable: 0=human, 1=machine, 2=specimen)
        /// </summary>
        [FieldMapping(ColumnName = "record_basis")]
        public RecordBasis? RecordBasis { get; set; }
        
        /// <summary>
        /// GBIF数据集UUID(iNat来源留空;跳过iNat转发记录的对账锚点) / GBIF dataset UUID (null for iNat; anchor for skipping iNat republishes)
        /// </summary>
        [FieldMapping(ColumnName = "dataset_key")]
        public string? DatasetKey { get; set; }
        
        /// <summary>
        /// GBIF数据集名称(卡片署名数据发布机构用,iNat留空) / GBIF dataset title (publisher attribution; null for iNat)
        /// </summary>
        [FieldMapping(ColumnName = "dataset_title")]
        public string? DatasetTitle { get; set; }
        
        /// <summary>
        /// 鉴定次数
        /// </summary>
        [FieldMapping(ColumnName = "identifications_count")]
        public int IdentificationsCount { get; set; }
        
        /// <summary>
        /// 源站发布时间(UTC,新鲜度度量用)
        /// </summary>
        [FieldMapping(ColumnName = "source_created_at")]
        public DateTime? SourceCreatedAt { get; set; }
        
        /// <summary>
        /// 源站最后更新时间(UTC,增量水位与对账用)
        /// </summary>
        [FieldMapping(ColumnName = "source_updated_at")]
        public DateTime? SourceUpdatedAt { get; set; }
        
        /// <summary>
        /// 状态(0=正常,1=隐藏(运营处置),2=源站已删除(对账下线))
        /// Status (0=normal, 1=hidden by operator, 2=deleted on source site)
        /// </summary>
        [FieldMapping(ColumnName = "status")]
        public ObservationStatus Status { get; set; }
        
        /// <summary>
        /// 创建时间(本站入库时间,UTC)
        /// </summary>
        [FieldMapping(ColumnName = "create_time")]
        public DateTime CreateTime { get; set; }
        
        /// <summary>
        /// 修改时间(UTC)
        /// </summary>
        [FieldMapping(ColumnName = "update_time")]
        public DateTime UpdateTime { get; set; }
        
    }
}
