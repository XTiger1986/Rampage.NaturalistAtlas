using Rampage.Hierarchy;
using Rampage.NaturalistAtlas.Model.Po;

namespace Rampage.NaturalistAtlas.DAL
{
    /// <summary>
    /// 国家 Dao 实现（CRUD 由 SuperDao 提供） / Country Dao implementation (CRUD from SuperDao)
    /// </summary>
    public class CountryDao : SuperDao<Country>, ICountryDao
    {
    }

    /// <summary>
    /// 省级行政区 Dao 实现（CRUD 由 SuperDao 提供） / Province Dao implementation (CRUD from SuperDao)
    /// </summary>
    public class ProvinceDao : SuperDao<Province>, IProvinceDao
    {
    }
}
