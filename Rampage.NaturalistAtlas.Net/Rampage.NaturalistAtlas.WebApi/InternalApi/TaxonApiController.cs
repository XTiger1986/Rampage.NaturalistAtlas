using Microsoft.AspNetCore.Mvc;
using Rampage.DependencyInjection;
using Rampage.Model;
using Rampage.NaturalistAtlas.BLL;
using Rampage.NaturalistAtlas.Model.Dto.Public;
using Rampage.NaturalistAtlas.WebApi.Internal;
using System;
using System.Collections.Generic;

namespace Rampage.NaturalistAtlas.WebApi.InternalApi
{
    /// <summary>
    /// 分类接口：祖先链面包屑 / Taxon API: ancestor chain breadcrumb
    /// </summary>
    [Route("api/taxon")]
    public class TaxonApiController : NaturalistAtlasApiController
    {
        [Auto]
        protected IAtlasSpeciesService AtlasSpeciesService = default!;


        /// <summary>
        /// 分类祖先链（界›门›纲›目›科›属，按链序）/ Taxon ancestors in path order
        /// </summary>
        /// <param name="id">taxon.id</param>
        [HttpGet("{id}/ancestors")]
        public R<List<TaxonAncestorDto>> QueryAncestors(string id)
        {
            try
            {
                return R<List<TaxonAncestorDto>>.Success(AtlasSpeciesService.GetTaxonAncestors(id));
            }
            catch (Exception ex)
            {
                LogError(ex);
                return R<List<TaxonAncestorDto>>.Failure(UNEXPECTED_FAILURE_MESSAGE);
            }
        }
    }
}
