using ttf;

namespace ttf.Tests;

public class FontKindsAndDiagnosticsTests
{
    [Test]
    public void ContainerKindValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)FontContainerKind.Unknown, Is.EqualTo(0));
            Assert.That((int)FontContainerKind.Sfnt, Is.EqualTo(1));
            Assert.That((int)FontContainerKind.Collection, Is.EqualTo(2));
            Assert.That((int)FontContainerKind.Woff1, Is.EqualTo(3));
            Assert.That((int)FontContainerKind.Woff2, Is.EqualTo(4));
        });
    }

    [Test]
    public void OutlineKindValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)FontOutlineKind.Unknown, Is.EqualTo(0));
            Assert.That((int)FontOutlineKind.TrueType, Is.EqualTo(1));
            Assert.That((int)FontOutlineKind.Cff1, Is.EqualTo(2));
            Assert.That((int)FontOutlineKind.Cff2, Is.EqualTo(3));
            Assert.That((int)FontOutlineKind.BitmapOnly, Is.EqualTo(4));
        });
    }

    [Test]
    public void DiagnosticSeverityValuesAreStableAndOrdered()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)FontDiagnosticSeverity.Information, Is.EqualTo(0));
            Assert.That((int)FontDiagnosticSeverity.Warning, Is.EqualTo(1));
            Assert.That((int)FontDiagnosticSeverity.Error, Is.EqualTo(2));
            Assert.That((int)FontDiagnosticSeverity.Fatal, Is.EqualTo(3));
        });
    }

    [TestCase(FontDiagnosticCode.None, 0)]
    [TestCase(FontDiagnosticCode.UnknownContainer, 1000)]
    [TestCase(FontDiagnosticCode.UnsupportedContainer, 1001)]
    [TestCase(FontDiagnosticCode.TruncatedData, 1002)]
    [TestCase(FontDiagnosticCode.InvalidOffset, 1003)]
    [TestCase(FontDiagnosticCode.InvalidLength, 1004)]
    [TestCase(FontDiagnosticCode.ArithmeticOverflow, 1005)]
    [TestCase(FontDiagnosticCode.LimitExceeded, 1006)]
    [TestCase(FontDiagnosticCode.UnsupportedVersion, 1007)]
    [TestCase(FontDiagnosticCode.InvalidReservedValue, 1008)]
    [TestCase(FontDiagnosticCode.InvalidSfntHeader, 2000)]
    [TestCase(FontDiagnosticCode.InvalidSearchFields, 2001)]
    [TestCase(FontDiagnosticCode.TooManyTables, 2002)]
    [TestCase(FontDiagnosticCode.UnsortedTableDirectory, 2003)]
    [TestCase(FontDiagnosticCode.DuplicateTable, 2004)]
    [TestCase(FontDiagnosticCode.MisalignedTable, 2005)]
    [TestCase(FontDiagnosticCode.OverlappingTables, 2006)]
    [TestCase(FontDiagnosticCode.TableOutOfBounds, 2007)]
    [TestCase(FontDiagnosticCode.NonZeroPadding, 2008)]
    [TestCase(FontDiagnosticCode.TableChecksumMismatch, 2009)]
    [TestCase(FontDiagnosticCode.FontChecksumMismatch, 2010)]
    [TestCase(FontDiagnosticCode.MissingRequiredTable, 2011)]
    [TestCase(FontDiagnosticCode.InvalidTableCombination, 2012)]
    [TestCase(FontDiagnosticCode.InvalidCollection, 2013)]
    [TestCase(FontDiagnosticCode.InvalidWoff1, 2014)]
    [TestCase(FontDiagnosticCode.InvalidWoff2, 2015)]
    [TestCase(FontDiagnosticCode.DecompressionFailed, 2016)]
    [TestCase(FontDiagnosticCode.DecompressionLimitExceeded, 2017)]
    [TestCase(FontDiagnosticCode.InvalidWoff2Integer, 2018)]
    [TestCase(FontDiagnosticCode.UnsupportedWoff2Transform, 2019)]
    [TestCase(FontDiagnosticCode.UnsupportedTable, 3000)]
    [TestCase(FontDiagnosticCode.UnsupportedTableVersion, 3001)]
    [TestCase(FontDiagnosticCode.MalformedTable, 3002)]
    [TestCase(FontDiagnosticCode.InvalidGlyphId, 3003)]
    [TestCase(FontDiagnosticCode.MissingGlyph, 3004)]
    [TestCase(FontDiagnosticCode.InvalidCharacterMapping, 3005)]
    [TestCase(FontDiagnosticCode.InvalidNameEncoding, 3006)]
    [TestCase(FontDiagnosticCode.InvalidMetrics, 3007)]
    [TestCase(FontDiagnosticCode.InvalidOutline, 3008)]
    [TestCase(FontDiagnosticCode.CompositeCycle, 3009)]
    [TestCase(FontDiagnosticCode.CompositeDepthExceeded, 3010)]
    [TestCase(FontDiagnosticCode.InvalidLayout, 3011)]
    [TestCase(FontDiagnosticCode.LayoutLimitExceeded, 3012)]
    [TestCase(FontDiagnosticCode.InvalidVariation, 3013)]
    [TestCase(FontDiagnosticCode.InvalidColorGlyph, 3014)]
    [TestCase(FontDiagnosticCode.UnsupportedShapingTechnology, 3015)]
    [TestCase(FontDiagnosticCode.RepairApplied, 4000)]
    [TestCase(FontDiagnosticCode.TablePreservedAsRaw, 4001)]
    [TestCase(FontDiagnosticCode.TableDropped, 4002)]
    [TestCase(FontDiagnosticCode.StringDecodingReplacement, 4003)]
    [TestCase(FontDiagnosticCode.StoredBoundsMismatch, 4004)]
    [TestCase(FontDiagnosticCode.SignatureNotVerified, 4005)]
    [TestCase(FontDiagnosticCode.EmbeddingRestricted, 5000)]
    [TestCase(FontDiagnosticCode.SubsettingRestricted, 5001)]
    [TestCase(FontDiagnosticCode.BitmapEmbeddingOnly, 5002)]
    [TestCase(FontDiagnosticCode.AmbiguousUnicodeMapping, 5003)]
    [TestCase(FontDiagnosticCode.PdfVersionRequired, 5004)]
    public void DiagnosticCodeValuesAreStable(FontDiagnosticCode code, int value)
    {
        Assert.That((int)code, Is.EqualTo(value));
    }

    [Test]
    public void DiagnosticCodesAreUnique()
    {
        int[] values = Enum.GetValues<FontDiagnosticCode>().Select(value => (int)value).ToArray();

        Assert.That(values.Distinct().Count(), Is.EqualTo(values.Length));
    }
}
