using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Validation;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    /// <summary>
    /// 观察列表行（管理后台）：观察主档 + 批量补齐的分类/城市展示名
    /// Observation list row: the observation record plus batch-resolved taxon / city display names
    /// </summary>
    public class ObservationRowDto
    {
        /// <summary>主键(雪花Id) / Primary key (snowflake id)</summary>
        public string Id { get; set; } = default!;

        /// <summary>数据源 / Data source</summary>
        public AtlasSource Source { get; set; }

        /// <summary>源站观察ID（原样字符串） / Source observation id (verbatim string)</summary>
        public string SourceObservationId { get; set; } = default!;

        /// <summary>城市Id / City id</summary>
        public int CityId { get; set; }

        /// <summary>城市名称（批量补齐） / City name (batch resolved)</summary>
        public string? CityName { get; set; }

        /// <summary>分类单元Id / Taxon id</summary>
        public string? TaxonId { get; set; }

        /// <summary>分类学名（批量补齐，缺分类时为空） / Scientific name (batch resolved; empty when taxon missing)</summary>
        public string? TaxonScientificName { get; set; }

        /// <summary>中文常用名（批量补齐） / Chinese common name (batch resolved)</summary>
        public string? TaxonCommonNameCn { get; set; }

        /// <summary>类群 / Iconic taxon</summary>
        public IconicTaxon IconicTaxon { get; set; }

        /// <summary>鉴定级别 / Quality grade</summary>
        public QualityGrade QualityGrade { get; set; }

        /// <summary>观察日期（观察者本地日期，DATE 列映射 DateOnly） / Observed-on (observer-local date; DATE column mapped to DateOnly)</summary>
        public DateOnly ObservedOn { get; set; }

        /// <summary>公开坐标经度 / Public longitude</summary>
        public decimal Lng { get; set; }

        /// <summary>公开坐标纬度 / Public latitude</summary>
        public decimal Lat { get; set; }

        /// <summary>照片方图URL / Square photo url</summary>
        public string? PhotoSquareUrl { get; set; }

        /// <summary>观察者公开昵称 / Observer public login</summary>
        public string? UserLogin { get; set; }

        /// <summary>状态 / Status</summary>
        public ObservationStatus Status { get; set; }

        /// <summary>入库时间(UTC) / Create time (UTC)</summary>
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 观察详情（管理后台）：主档 + 分类/城市展示名 + 该观察的举报记录
    /// Observation detail: the record plus taxon / city names and its reports
    /// </summary>
    public class ObservationDetailDto
    {
        /// <summary>观察主档 / The observation record</summary>
        public Observation Observation { get; set; } = default!;

        /// <summary>城市名称 / City name</summary>
        public string? CityName { get; set; }

        /// <summary>分类档案（可空：观察可无分类） / Taxon profile (null when the observation has none)</summary>
        public Taxon? Taxon { get; set; }

        /// <summary>该观察的举报记录（新→旧） / Reports of this observation (newest first)</summary>
        public List<Report> ReportList { get; set; } = new List<Report>();
    }

    /// <summary>
    /// 举报列表行（管理后台）：举报主档 + 批量补齐的观察摘要
    /// Report list row: the report plus a batch-resolved observation summary
    /// </summary>
    public class ReportRowDto
    {
        /// <summary>主键(雪花Id) / Primary key (snowflake id)</summary>
        public string Id { get; set; } = default!;

        /// <summary>观察Id / Observation id</summary>
        public string? ObservationId { get; set; }

        /// <summary>举报原因 / Report reason</summary>
        public ReportReason Reason { get; set; }

        /// <summary>补充说明 / Extra content</summary>
        public string? Content { get; set; }

        /// <summary>联系方式 / Contact</summary>
        public string? Contact { get; set; }

        /// <summary>状态 / Status</summary>
        public ReportStatus Status { get; set; }

        /// <summary>提交时间(UTC) / Submitted time (UTC)</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>处理人admin.id / Handler admin id</summary>
        public int HandleAdminId { get; set; }

        /// <summary>处理时间(UTC) / Handled time (UTC)</summary>
        public DateTime? HandleTime { get; set; }

        /// <summary>处理备注 / Handler remark</summary>
        public string? HandleRemark { get; set; }

        /// <summary>观察方图URL（批量补齐） / Observation square photo url (batch resolved)</summary>
        public string? ObservationPhotoSquareUrl { get; set; }

        /// <summary>源站观察ID（批量补齐） / Source observation id (batch resolved)</summary>
        public string? ObservationSourceId { get; set; }

        /// <summary>观察当前状态（批量补齐；观察不存在时为空） / Observation status (batch resolved; null when the observation is gone)</summary>
        public ObservationStatus? ObservationStatus { get; set; }

        /// <summary>观察所属类群（批量补齐） / Observation iconic taxon (batch resolved)</summary>
        public IconicTaxon? ObservationIconicTaxon { get; set; }

        /// <summary>观察者昵称（批量补齐） / Observer login (batch resolved)</summary>
        public string? ObservationUserLogin { get; set; }
    }

    /// <summary>
    /// 举报处理输入（api/report/handle）：Hide=隐藏对应观察并结单，Reject=驳回结单
    /// Report handle input: Hide hides the observation and closes the report; Reject closes it without hiding
    /// </summary>
    public class HandleReportInput
    {
        /// <summary>
        /// 举报Id（雪花） / Report id (snowflake)
        /// </summary>
        [Required]
        public string ReportId { get; set; } = default!;

        /// <summary>
        /// 处理动作：true=隐藏观察，false=驳回 / Action: true=hide the observation, false=reject
        /// </summary>
        public bool Hide { get; set; }

        /// <summary>
        /// 处理备注（可选） / Handler remark (optional)
        /// </summary>
        [StringLength(Min = 0, Max = 500)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 观察状态切换输入（api/observation/status）：仅在 0=正常 与 1=隐藏 之间切换
    /// Observation status toggle input: only switches between 0=normal and 1=hidden
    /// </summary>
    public class ObservationStatusInput
    {
        /// <summary>
        /// 观察Id（雪花） / Observation id (snowflake)
        /// </summary>
        [Required]
        public string ObservationId { get; set; } = default!;

        /// <summary>
        /// 目标状态（0=正常恢复，1=隐藏） / Target status (0=restore, 1=hide)
        /// </summary>
        [Required]
        public ObservationStatus Status { get; set; }
    }

    /// <summary>
    /// 按作者隐藏输入（api/observation/hide-by-author）：将该观察者名下全部正常观察置为隐藏
    /// Hide-by-author input: hides all normal observations of the given observer login
    /// </summary>
    public class HideByAuthorInput
    {
        /// <summary>
        /// 观察者公开昵称 / Observer public login
        /// </summary>
        [NotWhiteSpaceString]
        [StringLength(Min = 1, Max = 100)]
        public string UserLogin { get; set; } = default!;
    }
}
