namespace Rampage.NaturalistAtlas.ManagementSite.Models
{
    /// <summary>
    /// 城市下拉选项（展示层）：重名城市以「城市（国家）」消歧义
    /// City dropdown option (presentation): disambiguated as "city (country)"
    /// </summary>
    public class CityOption
    {
        /// <summary>
        /// 城市Id / City id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 展示名：城市（国家） / Display label: city (country)
        /// </summary>
        public string DisplayName { get; set; } = default!;
    }
}
