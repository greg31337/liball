using System.Reflection;

namespace currencycodes.Tests;

public class DataVersionTests
{
    private static readonly string[] ExpectedSourceChecksums = new[]
    {
        "data/overrides/currency-kinds.csv\t2052\tb89611a818940f6d350317b3d2f99c32bd776c80e5af8cafb41416b4696c98d4",
        "data/overrides/fund-codes.csv\t182\t69353f99fa8132cbc25a91fa333b1bf58e56b1aaa11927146fa0ad0da6e9ea0c",
        "data/overrides/source-conflicts.csv\t75\taa02ed83a62c31e5d8c3f8fea28476a54750bc51ff6bd691a7926b7a190d0ea3",
        "data/overrides/territory-name-map.csv\t9838\t0b3e1f6c4b74e6b9bc7bba485d140e2b74cdbe11ebfeac6b690646bc4e84e626",
        "specs/cldr-48.2-license.txt\t2033\tb49d0e9f8ead51ca8b7df6fec89cc3ae6809198b4b9d43c74216da0118f23f5b",
        "specs/cldr-48.2-numberingSystems.xml\t11834\tc5b9b208c6fe7bd3ce4e1c6cbb8f3337480aca620c9cf703ef3b6c74d4258c16",
        "specs/cldr-48.2-supplementalData.xml\t399983\tcd2af39aef82fdbfba4d591c87548203350538ad2318486d104b3b38b8d62f1a",
        "specs/cldr-48.2-supplementalMetadata.xml\t180141\t36e807ce72b15304dd993f132216f408351be1c4376edaa2fe9e9547e2efcac1",
        "specs/iso-4217-amendment-180.pdf\t67969\t8b2a8a146b2dbaa4d95734b3615ef71db7c4642e75f13e38197101f082f5a8a3",
        "specs/iso-4217-amendments-overview.xlsx\t40183\td34a6a545a54f334d49d1541b8af309a1ebb661b3442e6cea50b4c99c7a2dc69",
        "specs/iso-4217-list-one.xml\t47463\t838dfb991648cf36df939edd5fe3811737962b75a32252847d239cedd1e291c9",
        "specs/iso-4217-list-three.xml\t30638\t98fde2423cdb916dd59dcf5fe96222edad8fa198d865c1c83dbc464b9cc52387",
        "specs/iso-4217-list-two.doc\t46592\t2c59a66ec930fd72332b7aebb6f5d940205497ca4ab39c6d34a076af2d70468c",
        "specs/rfc5646.txt\t208592\t5d9515f053163c80e7294a84d18217dc83471acc7caa9773da2ac40deeec8228",
        "specs/six-iso-4217-maintenance.html\t213173\tf45e164ed2f9dcd68d4f95711b7d09b49cb2d69adf27e9e9fbb05057161d1584",
        "specs/un-m49-overview.html\t1721575\tb9048114f6e7f2abda83bf03d4263c9d7cd1bd7230e3d0461025ee7839a7a1fb",
        "specs/unicode-tr35-numbers.html\t177642\t43aed410d67e349c0550b0d5fb36a36636e60ee7324efa57f068d8c668d2860a",
        "specs/unicode-tr35.html\t392315\tc04d92e36279b1b353cf4e6964c28383567709026c9fa69420e093961fe774dc"
    };

    [Test]
    public void EveryPublicStaticVersionFieldMatchesThePinnedSnapshot()
    {
        FieldInfo[] fields = typeof(CurrencyDataVersions).GetFields(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        string[] names = Array.ConvertAll(fields, field => field.Name);
        Array.Sort(names, StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EqualTo(new[]
            {
                "CldrVersion",
                "GeneratorSchemaVersion",
                "IsoPublicationDate",
                "SnapshotIdentifier"
            }));
            Assert.That(CurrencyDataVersions.IsoPublicationDate, Is.EqualTo("2026-01-01"));
            Assert.That(CurrencyDataVersions.CldrVersion, Is.EqualTo("48.2"));
            Assert.That(CurrencyDataVersions.GeneratorSchemaVersion, Is.EqualTo("1"));
            Assert.That(
                CurrencyDataVersions.SnapshotIdentifier,
                Is.EqualTo("sha256:45c22d81e49767588f1509e6028b964f988a2c83f813184c00994bae30204a92"));
        });
    }

    [Test]
    public void PublicDataVersionResultContainsEveryPinnedFieldAndSourceChecksum()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        DataVersionInfo result = catalog.GetCurrencyDataVersion();
        FieldInfo[] fields = typeof(DataVersionInfo).GetFields(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        string[] names = Array.ConvertAll(fields, field => field.Name);
        Array.Sort(names, StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(names, Is.EqualTo(new[]
            {
                "CldrVersion",
                "GeneratorSchemaVersion",
                "IsoPublicationDate",
                "SnapshotIdentifier",
                "SourceChecksums"
            }));
            Assert.That(result.IsoPublicationDate, Is.EqualTo("2026-01-01"));
            Assert.That(result.CldrVersion, Is.EqualTo("48.2"));
            Assert.That(result.GeneratorSchemaVersion, Is.EqualTo("1"));
            Assert.That(
                result.SnapshotIdentifier,
                Is.EqualTo("sha256:45c22d81e49767588f1509e6028b964f988a2c83f813184c00994bae30204a92"));
            Assert.That(result.SourceChecksums, Is.EqualTo(ExpectedSourceChecksums));
        });
    }

    [Test]
    public void PublicAndGeneratedVersionValuesStayInSync()
    {
        DataVersionInfo result = new CurrencyCodeService().GetCurrencyDataVersion();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsoPublicationDate, Is.EqualTo(CurrencyDataVersions.IsoPublicationDate));
            Assert.That(result.CldrVersion, Is.EqualTo(CurrencyDataVersions.CldrVersion));
            Assert.That(result.GeneratorSchemaVersion, Is.EqualTo(CurrencyDataVersions.GeneratorSchemaVersion));
            Assert.That(result.SnapshotIdentifier, Is.EqualTo(CurrencyDataVersions.SnapshotIdentifier));
            Assert.That(result.IsoPublicationDate, Is.EqualTo(GeneratedDataVersion.IsoPublicationDate));
            Assert.That(result.CldrVersion, Is.EqualTo(GeneratedDataVersion.CldrVersion));
            Assert.That(result.GeneratorSchemaVersion, Is.EqualTo(GeneratedDataVersion.GeneratorSchemaVersion));
            Assert.That(result.SnapshotIdentifier, Is.EqualTo(GeneratedDataVersion.SnapshotIdentifier));
            Assert.That(result.SourceChecksums, Is.EqualTo(GeneratedDataVersion.SourceChecksums));
        });
    }

    [Test]
    public void PublicDataVersionResultsOwnIndependentChecksumArrays()
    {
        CurrencyCodeService service = new CurrencyCodeService();
        DataVersionInfo first = service.GetCurrencyDataVersion();
        DataVersionInfo second = service.GetCurrencyDataVersion();

        first.SourceChecksums[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.SourceChecksums, Is.Not.SameAs(second.SourceChecksums));
            Assert.That(first.SourceChecksums, Is.Not.SameAs(GeneratedDataVersion.SourceChecksums));
            Assert.That(second.SourceChecksums, Is.EqualTo(ExpectedSourceChecksums));
            Assert.That(GeneratedDataVersion.SourceChecksums, Is.EqualTo(ExpectedSourceChecksums));
        });
    }
}
