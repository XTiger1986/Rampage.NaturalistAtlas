using System;

namespace Rampage.NaturalistAtlas.Model.Dto
{
    /// <summary>
    /// 枚举类信息
    /// </summary>
    public class EnumTypeDto
    {
        /// <summary>
        /// 枚举值
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// 枚举码
        /// </summary>
        public string? TypeCode { get; set; }
        /// <summary>
        /// 枚举名称
        /// </summary>
        public string? TypeName { get; set; }

    }
}
