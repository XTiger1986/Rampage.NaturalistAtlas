using Rampage.Data;
using Rampage.Enums;
using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 分类单元
    /// </summary>
    [TableMapping("taxon")]
    public class Taxon
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
        /// 源站分类ID（原样字符串：数字/UUID/QID/标本号等，仅等值匹配）
        /// Source taxon id (verbatim string: numeric / UUID / QID / catalog number; equality-match only)
        /// </summary>
        [FieldMapping(ColumnName = "source_taxon_id")]
        public string SourceTaxonId { get; set; } = default!;
        
        /// <summary>
        /// 分类阶元(0=未知,1=界,2=门,3=纲,4=目,5=科,6=属,7=种,8=亚种,9=变种,10=变型,11=杂交)
        /// Taxon rank (0=unknown,1=kingdom ... 11=hybrid)
        /// </summary>
        [FieldMapping(ColumnName = "rank")]
        public TaxonRank Rank { get; set; }
        
        /// <summary>
        /// 学名
        /// </summary>
        [FieldMapping(ColumnName = "scientific_name")]
        public string? ScientificName { get; set; }
        
        /// <summary>
        /// 中文常用名
        /// </summary>
        [FieldMapping(ColumnName = "common_name_cn")]
        public string? CommonNameCn { get; set; }
        
        /// <summary>
        /// 英文常用名
        /// </summary>
        [FieldMapping(ColumnName = "common_name_en")]
        public string? CommonNameEn { get; set; }
        
        /// <summary>
        /// 类群(1=鸟,2=哺乳,3=两栖,4=爬行动物,5=辐鳍鱼,6=昆虫,7=蛛形,8=软体,9=植物,10=真菌,11=其他动物,12=原生动物,13=色藻,14=未知)
        /// Iconic taxon (1=birds, 4=reptiles ... 14=unknown)
        /// </summary>
        [FieldMapping(ColumnName = "iconic_taxon")]
        public IconicTaxon IconicTaxon { get; set; }
        
        /// <summary>
        /// 受威胁物种(源站红色名录等保守状态标记) / Threatened (conservative source red-list flag)
        /// </summary>
        [FieldMapping(ColumnName = "threatened")]
        public bool Threatened { get; set; }
        
        /// <summary>
        /// 祖先链(源站分类ID串,如48460-1-2-3)
        /// </summary>
        [FieldMapping(ColumnName = "ancestry")]
        public string? Ancestry { get; set; }
        
        /// <summary>
        /// 状态(1=正常,0=源站已弃用)
        /// </summary>
        [FieldMapping(ColumnName = "status")]
        public CommonStatus Status { get; set; }
        
        /// <summary>
        /// 创建时间(UTC)
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
