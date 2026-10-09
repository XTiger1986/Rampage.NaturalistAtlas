using Rampage.Enums;
using Rampage.Validation;

namespace Rampage.NaturalistAtlas.ManagementSite.Models
{
    /// <summary>
    /// 城市启停输入（api/city/status）/ City status toggle input (api/city/status)
    /// </summary>
    public class CityStatusInput
    {
        /// <summary>
        /// 城市Id / City id
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// 目标状态 / Target status
        /// </summary>
        [Required]
        public CommonStatus Status { get; set; }
    }

    /// <summary>
    /// 国家/省份启停输入（api/country/status、api/province/status）
    /// Country / province status toggle input
    /// </summary>
    public class GeoStatusInput
    {
        /// <summary>
        /// 档案Id / Profile id
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// 目标状态 / Target status
        /// </summary>
        [Required]
        public CommonStatus Status { get; set; }
    }
}
