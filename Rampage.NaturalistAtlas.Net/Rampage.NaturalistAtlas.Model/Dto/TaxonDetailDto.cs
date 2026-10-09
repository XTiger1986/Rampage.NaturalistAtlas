using Rampage.Enums;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    /// <summary>
    /// 分类单元详情（管理后台）：基础档案 + 关联观察数 + 按祖先链解析的上级分类列表
    /// Taxon detail (admin site): base profile + observation count + ancestor list resolved from the ancestry chain
    /// </summary>
    public class TaxonDetailDto
    {
        /// <summary>
        /// 主键(雪花Id) / Primary key (snowflake id)
        /// </summary>
        public string Id { get; set; } = default!;

        /// <summary>
        /// 数据源 / Data source
        /// </summary>
        public AtlasSource Source { get; set; }

        /// <summary>
        /// 源站分类ID（原样字符串） / Source taxon id (verbatim string)
        /// </summary>
        public string SourceTaxonId { get; set; } = default!;

        /// <summary>
        /// 分类阶元 / Rank
        /// </summary>
        public TaxonRank Rank { get; set; }

        /// <summary>
        /// 学名 / Scientific name
        /// </summary>
        public string? ScientificName { get; set; }

        /// <summary>
        /// 中文常用名 / Chinese common name
        /// </summary>
        public string? CommonNameCn { get; set; }

        /// <summary>
        /// 英文常用名 / English common name
        /// </summary>
        public string? CommonNameEn { get; set; }

        /// <summary>
        /// 类群 / Iconic taxon
        /// </summary>
        public IconicTaxon IconicTaxon { get; set; }

        /// <summary>
        /// 受威胁物种 / Threatened
        /// </summary>
        public bool Threatened { get; set; }

        /// <summary>
        /// 祖先链原文（源站分类ID串） / Raw ancestry chain (source taxon id string)
        /// </summary>
        public string? Ancestry { get; set; }

        /// <summary>
        /// 状态 / Status
        /// </summary>
        public CommonStatus Status { get; set; }

        /// <summary>
        /// 创建时间(UTC) / Create time (UTC)
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间(UTC) / Update time (UTC)
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 关联观察数（主库 observation 按 taxon_id 计数）/ Observation count (observation rows by taxon_id)
        /// </summary>
        public long ObservationCount { get; set; }

        /// <summary>
        /// 祖先链解析结果（按链序排列；源站未同步到的成员缺省）
        /// Resolved ancestors in chain order; members missing from the local table are skipped
        /// </summary>
        public List<Taxon> AncestorList { get; set; } = new List<Taxon>();
    }
}
