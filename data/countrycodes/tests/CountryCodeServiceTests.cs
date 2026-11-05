using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class CountryCodeServiceTests
{
    [Test]
    public void DataVersionProviderReturnsDetachedCompiledMetadata()
    {
        CountryCodeService service = new CountryCodeService();
        IDataVersionProvider provider = service;

        DataVersion first = provider.GetDataVersion();
        first.SourceRevisions[0] = "changed";
        first.SchemaVersion = "changed";

        DataVersion second = provider.GetDataVersion();

        Assert.That(second.SchemaVersion, Is.EqualTo(CountryCodeConstants.SchemaVersion));
        Assert.That(second.DatasetVersion, Is.EqualTo(CountryDataMetadata.DatasetVersion));
        Assert.That(second.EffectiveDate, Is.EqualTo(CountryDataMetadata.EffectiveDate));
        Assert.That(second.GeneratedDate, Is.EqualTo(CountryDataMetadata.EffectiveDate));
        Assert.That(second.GeneratorVersion, Is.EqualTo(CountryDataMetadata.GeneratorVersion));
        Assert.That(second.SourceRevisions, Has.Length.EqualTo(6));
        Assert.That(
            second.SourceRevisions[0],
            Is.EqualTo(CountryDataMetadata.CldrSourceRevision));
        Assert.That(second.ContentChecksum, Is.EqualTo(CountryDataMetadata.ContentChecksum));
    }
}
