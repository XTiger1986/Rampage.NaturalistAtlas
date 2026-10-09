using Rampage.Data;
using Rampage.Data.Query;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 分类单元管理服务（管理后台，只读检索 + 详情） / Taxon admin service (read-only search + detail)
    /// </summary>
    public interface ITaxonService : ISuperService<Taxon, ITaxonDao>
    {
        /// <summary>
        /// 分页查询分类单元列表 / Query the paged taxon list
        /// </summary>
        /// <param name="filter">筛选条件 / Filter</param>
        /// <param name="pager">分页参数 / Pager</param>
        /// <returns>分类单元列表 / The taxon list</returns>
        List<Taxon> GetTaxonPageList(TaxonAdminFilter filter, PagerInfo pager);

        /// <summary>
        /// 分类单元详情：基础档案 + 关联观察数 + 祖先链解析
        /// Taxon detail: base profile + observation count + resolved ancestry chain
        /// </summary>
        /// <param name="taxonId">分类单元Id（雪花）/ Taxon id (snowflake)</param>
        /// <returns>详情，不存在返回 null / The detail, or null when missing</returns>
        TaxonDetailDto? GetTaxonDetail(string taxonId);
    }
}
