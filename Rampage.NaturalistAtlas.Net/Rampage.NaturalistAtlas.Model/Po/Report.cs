using Rampage.Data;
using Rampage.NaturalistAtlas.Model.Enums;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 内容举报
    /// </summary>
    [TableMapping("report")]
    public class Report
    {

        /// <summary>
        /// 主键(雪花Id)
        /// </summary>
        [FieldMapping(PrimaryKey = true, ColumnName = "id")]
        public string Id { get; set; } = default!;
        
        /// <summary>
        /// observation.id(雪花Id)
        /// </summary>
        [FieldMapping(ColumnName = "observation_id")]
        public string? ObservationId { get; set; }
        
        /// <summary>
        /// 举报原因(0=其他,1=不当内容,2=错误鉴定,3=隐私问题,4=垃圾信息)
        /// Report reason (0=other, 1=inappropriate, 2=misidentification, 3=privacy, 4=spam)
        /// </summary>
        [FieldMapping(ColumnName = "reason")]
        public ReportReason Reason { get; set; }
        
        /// <summary>
        /// 补充说明
        /// </summary>
        [FieldMapping(ColumnName = "content")]
        public string? Content { get; set; }
        
        /// <summary>
        /// 联系方式(可选,匿名举报可留空)
        /// </summary>
        [FieldMapping(ColumnName = "contact")]
        public string? Contact { get; set; }
        
        /// <summary>
        /// 状态(0=待处理,1=已隐藏,2=驳回) / Status (0=pending, 1=resolved by hiding, 2=rejected)
        /// </summary>
        [FieldMapping(ColumnName = "status")]
        public ReportStatus Status { get; set; }
        
        /// <summary>
        /// 处理人admin.id(管理后台库)
        /// </summary>
        [FieldMapping(ColumnName = "handle_admin_id")]
        public int HandleAdminId { get; set; }
        
        /// <summary>
        /// 处理时间(UTC,可空:未处理)
        /// </summary>
        [FieldMapping(ColumnName = "handle_time")]
        public DateTime? HandleTime { get; set; }
        
        /// <summary>
        /// 处理备注
        /// </summary>
        [FieldMapping(ColumnName = "handle_remark")]
        public string? HandleRemark { get; set; }
        
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
