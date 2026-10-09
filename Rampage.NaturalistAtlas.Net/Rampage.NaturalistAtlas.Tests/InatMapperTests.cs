using Rampage.NaturalistAtlas.Collection;
using Rampage.NaturalistAtlas.Model.Dto.External;
using Rampage.NaturalistAtlas.Model.Enums;
using Rampage.NaturalistAtlas.Model.Po;

namespace Rampage.NaturalistAtlas.Tests
{
    /// <summary>
    /// InatMapper 纯函数单测：字段映射与宽松解析规则（采集端最值得测的纯逻辑）
    /// InatMapper pure-function tests: field mapping and lenient parsing rules
    /// </summary>
    public class InatMapperTests
    {
        private static readonly DateTime NOW = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void MapIconic_KnownAuthorityIds()
        {
            // 权威映射（/v1/taxa/iconic 实测，勿用网传错误值）
            Assert.Equal(IconicTaxon.Insects, InatMapper.MapIconic(47158));
            Assert.Equal(IconicTaxon.Arachnids, InatMapper.MapIconic(47119));
            Assert.Equal(IconicTaxon.Protozoans, InatMapper.MapIconic(47686));
            Assert.Equal(IconicTaxon.Chromista, InatMapper.MapIconic(48222));
        }

        [Fact]
        public void MapIconic_UnknownFallsBackToUnknown()
        {
            Assert.Equal(IconicTaxon.Unknown, InatMapper.MapIconic(null));
            Assert.Equal(IconicTaxon.Unknown, InatMapper.MapIconic(999999));
        }

        [Fact]
        public void MapQualityGrade_KnownStrings()
        {
            Assert.Equal(QualityGrade.Research, InatMapper.MapQualityGrade("research"));
            Assert.Equal(QualityGrade.NeedsId, InatMapper.MapQualityGrade("needs_id"));
        }

        [Fact]
        public void MapTaxon_AncestrySlashToDash_AndCnLeftEmpty()
        {
            InatTaxonDto dto = new()
            {
                Id = 12345,
                Name = "Pica serica",
                Ancestry = "48460/1/47120/3",
                PreferredCommonName = "Oriental Magpie"
            };
            Taxon t = InatMapper.MapTaxon(dto, NOW);
            Assert.Equal("12345", t.SourceTaxonId);
            Assert.Equal("48460-1-47120-3", t.Ancestry);
            // 中文名首采留空（批次1 口径决策：英文暂存会被刷新逻辑永久固化）
            Assert.Equal(string.Empty, t.CommonNameCn);
            Assert.Equal("Oriental Magpie", t.CommonNameEn);
        }

        [Fact]
        public void MapLicense_CodeAndNull()
        {
            Assert.Equal(AtlasLicense.CcByNc, InatMapper.MapLicense("cc-by-nc"));
            Assert.Equal(AtlasLicense.AllRightsReserved, InatMapper.MapLicense(null));
        }

        [Fact]
        public void MergeGeoprivacy_TaxonPrivacyWinsWhenStricter()
        {
            Assert.Equal(GeoPrivacy.Obscured, InatMapper.MergeGeoprivacy("open", "obscured"));
            Assert.Equal(GeoPrivacy.Open, InatMapper.MergeGeoprivacy(null, null));
            Assert.Equal(GeoPrivacy.Private, InatMapper.MergeGeoprivacy("private", "open"));
        }

        [Fact]
        public void ReplaceSizeSegment_SquareToMedium()
        {
            Assert.Equal(
                "https://inaturalist-open-data.s3.amazonaws.com/photos/1/medium.jpg",
                InatMapper.ReplaceSizeSegment("https://inaturalist-open-data.s3.amazonaws.com/photos/1/square.jpg", "medium"));
        }

        [Fact]
        public void ParseLocalDate_IsoDateOnly()
        {
            Assert.Equal(new DateOnly(2026, 9, 27), InatMapper.ParseLocalDate("2026-09-27"));
            Assert.Equal(default, InatMapper.ParseLocalDate(null));
        }
    }

    /// <summary>
    /// GbifMapper 纯函数单测 / GbifMapper pure-function tests
    /// </summary>
    public class GbifMapperTests
    {
        [Fact]
        public void IsInatRepublish_KnownDatasetKey()
        {
            Assert.True(GbifMapper.IsInatRepublish("50c9509d-22c7-4a22-a47d-8c48425ef4a7"));
            Assert.False(GbifMapper.IsInatRepublish("other-key"));
            Assert.False(GbifMapper.IsInatRepublish(null));
        }

        [Fact]
        public void MapLicense_UriForm()
        {
            // GBIF license 是 URI 形态（creativecommons.org/licenses/by-nc/4.0）
            Assert.Equal(AtlasLicense.CcByNc, GbifMapper.MapLicense("http://creativecommons.org/licenses/by-nc/4.0/"));
            Assert.Equal(AtlasLicense.AllRightsReserved, GbifMapper.MapLicense(null));
        }

        [Fact]
        public void MapRecordBasis_KnownValues()
        {
            Assert.Equal(RecordBasis.HumanObservation, GbifMapper.MapRecordBasis("HUMAN_OBSERVATION"));
            Assert.Equal(RecordBasis.MachineObservation, GbifMapper.MapRecordBasis("MACHINE_OBSERVATION"));
            Assert.Equal(RecordBasis.Specimen, GbifMapper.MapRecordBasis("PRESERVED_SPECIMEN"));
        }
    }
}
