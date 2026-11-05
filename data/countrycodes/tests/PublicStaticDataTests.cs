using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class PublicStaticDataTests
{
    [Test]
    public void CountryCodeConstantsDescribeCanonicalCodeShapesAndRanges()
    {
        Assert.That(CountryCodeConstants.Alpha2Length, Is.EqualTo(2));
        Assert.That(CountryCodeConstants.Alpha3Length, Is.EqualTo(3));
        Assert.That(CountryCodeConstants.Numeric3Length, Is.EqualTo(3));
        Assert.That(CountryCodeConstants.Alpha4Length, Is.EqualTo(4));
        Assert.That(CountryCodeConstants.SubdivisionCountryCodeLength, Is.EqualTo(2));
        Assert.That(CountryCodeConstants.SubdivisionMinimumSuffixLength, Is.EqualTo(1));
        Assert.That(CountryCodeConstants.SubdivisionMaximumSuffixLength, Is.EqualTo(3));
        Assert.That(CountryCodeConstants.SubdivisionSeparator, Is.EqualTo('-'));
        Assert.That(CountryCodeConstants.MinimumNumericValue, Is.EqualTo(0));
        Assert.That(CountryCodeConstants.MaximumNumericValue, Is.EqualTo(999));
        Assert.That(CountryCodeConstants.UserAssignedNumericMinimum, Is.EqualTo(900));
        Assert.That(CountryCodeConstants.UserAssignedNumericMaximum, Is.EqualTo(999));
        Assert.That(CountryCodeConstants.SchemaVersion, Is.EqualTo("1.6.0"));
    }

    [Test]
    public void CountryDataMetadataMatchesTheCompiledGeneratedSnapshot()
    {
        Assert.That(
            CountryCodeConstants.SchemaVersion,
            Is.EqualTo(GeneratedDataMetadata.SchemaVersion));
        Assert.That(
            CountryDataMetadata.DatasetVersion,
            Is.EqualTo(GeneratedDataMetadata.DataVersion));
        Assert.That(
            CountryDataMetadata.EffectiveDate,
            Is.EqualTo(GeneratedDataMetadata.EffectiveDate));
        Assert.That(
            CountryDataMetadata.GeneratorVersion,
            Is.EqualTo(GeneratedDataMetadata.GeneratorVersion));
        Assert.That(
            CountryDataMetadata.ContentChecksumAlgorithm,
            Is.EqualTo(GeneratedDataMetadata.ContentChecksumAlgorithm));
        Assert.That(
            CountryDataMetadata.ContentChecksum,
            Is.EqualTo(GeneratedDataMetadata.AggregateContentChecksum));
    }

    [Test]
    public void PublicSourceRevisionsMatchEveryPinnedGeneratedRevision()
    {
        Assert.That(
            CountryDataMetadata.CldrSourceRevision,
            Is.EqualTo(GeneratedDataMetadata.SourceRevisions[0].Revision));
        Assert.That(
            CountryDataMetadata.DebianIsoCodesSourceRevision,
            Is.EqualTo(GeneratedDataMetadata.SourceRevisions[1].Revision));
        Assert.That(
            CountryDataMetadata.IanaLanguageSubtagRegistrySourceRevision,
            Is.EqualTo(GeneratedDataMetadata.SourceRevisions[2].Revision));
        Assert.That(
            CountryDataMetadata.IetfBcp47SourceRevision,
            Is.EqualTo(GeneratedDataMetadata.SourceRevisions[3].Revision));
        Assert.That(
            CountryDataMetadata.UnM49SourceRevision,
            Is.EqualTo(GeneratedDataMetadata.SourceRevisions[4].Revision));
        Assert.That(
            CountryDataMetadata.UnicodeSpecificationsSourceRevision,
            Is.EqualTo(GeneratedDataMetadata.SourceRevisions[5].Revision));

        Assert.That(
            CountryDataMetadata.CldrSourceRevision,
            Is.EqualTo("release-48-2@11299982335beb974c1c63c45265184e759c0f41"));
        Assert.That(
            CountryDataMetadata.DebianIsoCodesSourceRevision,
            Is.EqualTo("abb091ac802dc34ff0375bdf153c4e633a2f44d3"));
        Assert.That(
            CountryDataMetadata.IanaLanguageSubtagRegistrySourceRevision,
            Is.EqualTo("File-Date:2026-08-08"));
    }
}
