using Rampage.Data;
using Rampage.Data.Query;
using Rampage.DependencyInjection;
using Rampage.NaturalistAtlas.DAL;
using Rampage.NaturalistAtlas.Model.Dto;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Filter;
using Rampage.NaturalistAtlas.Model.Po;
using Rampage.Web;
using System;

namespace Rampage.NaturalistAtlas.BLL
{
    /// <summary>
    /// 分类单元管理服务实现（管理后台）：数据由同步任务维护，后台只读
    /// Taxon admin service implementation: data is maintained by sync jobs; the admin side is read-only
    /// </summary>
    public class TaxonService : SuperService<Taxon, ITaxonDao>, ITaxonService
    {
        [Auto]
        protected ITaxonDao TaxonDao = default!;

        [Auto]
        protected IObservationDao ObservationDao = default!;


        /// <inheritdoc/>
        public List<Taxon> GetTaxonPageList(TaxonAdminFilter filter, PagerInfo pager)
        {
            FilterWrapper<Taxon> wrapper = new(filter);
            // 学名 A→Z 排序便于档案检索；无漂移风险，直接库序返回
            wrapper.OrderBy(i => i.ScientificName);
            return TaxonDao.SelectPage(pager, wrapper);
        }

        /// <inheritdoc/>
        public TaxonDetailDto? GetTaxonDetail(string taxonId)
        {
            if (string.IsNullOrWhiteSpace(taxonId))
            {
                return null;
            }
            Taxon? po = TaxonDao.SelectById(taxonId);
            if (po == null)
            {
                return null;
            }

            return new TaxonDetailDto
            {
                Id = po.Id,
                Source = po.Source,
                SourceTaxonId = po.SourceTaxonId,
                Rank = po.Rank,
                ScientificName = po.ScientificName,
                CommonNameCn = po.CommonNameCn,
                CommonNameEn = po.CommonNameEn,
                IconicTaxon = po.IconicTaxon,
                Threatened = po.Threatened,
                Ancestry = po.Ancestry,
                Status = po.Status,
                CreateTime = po.CreateTime,
                UpdateTime = po.UpdateTime,
                ObservationCount = ObservationDao.Count(o => o.TaxonId == taxonId),
                AncestorList = ResolveAncestors(po)
            };
        }

        /// <summary>
        /// 解析祖先链：ancestry 为源站分类ID串（如 48460-1-2-3），按链序换取本地分类档案
        /// Resolve the ancestry chain: ancestry is a source-id string (e.g. 48460-1-2-3); swap ids for local profiles in chain order
        /// </summary>
        /// <param name="po">当前分类单元 / The current taxon</param>
        /// <returns>按链序排列的祖先列表（本地未同步到的成员跳过）/ Ancestors in chain order (unsynced members skipped)</returns>
        private List<Taxon> ResolveAncestors(Taxon po)
        {
            List<Taxon> result = new List<Taxon>();
            if (string.IsNullOrWhiteSpace(po.Ancestry))
            {
                return result;
            }

            string[] segments = po.Ancestry.Split('-', StringSplitOptions.RemoveEmptyEntries);
            // 源站ID为原样字符串：链段原样收集（数字/字母码均兼容）
            List<string> sourceIdList = segments.Where(s => s.Length > 0).ToList();
            if (sourceIdList.Count == 0)
            {
                return result;
            }

            // 逐链点精确解析（链长度有限，避免内存拼接大 IN 条件）：同源下按 source_taxon_id 精确匹配
            AtlasSource source = po.Source;
            foreach (string sourceId in sourceIdList)
            {
                string currentId = sourceId;
                Taxon? ancestor = TaxonDao.SelectOne(t => t.Source == source && t.SourceTaxonId == currentId);
                if (ancestor != null)
                {
                    result.Add(ancestor);
                }
            }
            return result;
        }
    }
}
