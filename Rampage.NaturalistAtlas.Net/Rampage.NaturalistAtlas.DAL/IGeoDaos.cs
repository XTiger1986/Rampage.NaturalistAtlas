using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Po;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 国家 Dao / Country Dao
    /// </summary>
    public interface ICountryDao : ISuperDao<Country>
    {
    }

    /// <summary>
    /// 省级行政区 Dao / Province Dao
    /// </summary>
    public interface IProvinceDao : ISuperDao<Province>
    {
    }
}
