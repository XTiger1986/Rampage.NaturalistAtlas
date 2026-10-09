using Rampage.Data;
using System;

namespace Rampage.NaturalistAtlas.Model.Po
{
    /// <summary>
    /// 系统设置
    /// </summary>
    [TableMapping("setting")]
    public class Setting
    {
        public static readonly string CACHE_KEY_ONE_PFX = "SETTING";
        public static readonly string CACHE_KEY_ONE_TPL = CACHE_KEY_ONE_PFX + "_{0}";


        /// <summary>
        /// 
        /// </summary>
        [FieldMapping(PrimaryKey = true, ColumnName = "id", AutoIncrement = true)]
        public int Id { get; set; }
        
        /// <summary>
        /// 名称
        /// </summary>
        [FieldMapping(ColumnName = "name")]
        public string? Name { get; set; }
        
        /// <summary>
        /// 对应的类型完整名称
        /// </summary>
        [FieldMapping(ColumnName = "type")]
        public string? Type { get; set; }
        
        /// <summary>
        /// 设置JSON(在程序代码中定义各类DTO模型) / Settings JSON (DTO models defined in code)
        /// </summary>
        [FieldMapping(ColumnName = "data", Jsonb = true)]
        public string? Data { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        [FieldMapping(ColumnName = "create_admin_id")]
        public int CreateAdminId { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        [FieldMapping(ColumnName = "update_admin_id")]
        public int UpdateAdminId { get; set; }
        
        /// <summary>
        /// 创建时间(UTC)
        /// </summary>
        [FieldMapping(ColumnName = "create_time")]
        public DateTime CreateTime { get; set; }
        
        /// <summary>
        /// 修改时间(UTC)
        /// </summary>
        [FieldMapping(ColumnName = "update_time")]
        public DateTime? UpdateTime { get; set; }
        
    }
}
