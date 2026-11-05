using System.Net.Http.Headers;
using uspto;

namespace uspto.Tests;

public class BulkFileNameSelectorTests
{
    [Test]
    public void CatalogNameTakesPrecedenceAndKeepsItsCompoundExtension()
    {
        using HttpResponseMessage response = ResponseWithDisposition("attachment; filename=server-download.zip");

        string result = BulkFileNameSelector.SelectBulkFileName("  patent data?final.xml.zip  ", response);

        Assert.That(result, Is.EqualTo("patent data_final.xml.zip"));
    }

    [Test]
    public void ExtendedDispositionNameTakesPrecedenceOverLegacyName()
    {
        using HttpResponseMessage response = ResponseWithDisposition("attachment; filename=legacy.zip; filename*=UTF-8''%E2%82%AC%20rates.xml.zip");

        string result = BulkFileNameSelector.SelectBulkFileName(null, response);

        Assert.That(result, Is.EqualTo("€ rates.xml.zip"));
    }

    [Test]
    public void LegacyQuotedDispositionNameIsUsedWhenExtendedNameIsAbsent()
    {
        using HttpResponseMessage response = ResponseWithDisposition("inline; filename=\"Patent Grant 2026.tar.gz\"");

        string result = BulkFileNameSelector.SelectBulkFileName(" ", response);

        Assert.That(result, Is.EqualTo("Patent Grant 2026.tar.gz"));
    }

    [Test]
    public void UnicodeSpacesAndLiteralPercentEscapesRemainStable()
    {
        using HttpResponseMessage response = new HttpResponseMessage();

        string unicode = BulkFileNameSelector.SelectBulkFileName("商標 data 2026.json.gz", response);
        string percent = BulkFileNameSelector.SelectBulkFileName("100%20complete.zip", response);

        Assert.Multiple(() =>
        {
            Assert.That(unicode, Is.EqualTo("商標 data 2026.json.gz"));
            Assert.That(percent, Is.EqualTo("100%20complete.zip"));
        });
    }

    [TestCase("../archive.zip")]
    [TestCase("..\\archive.zip")]
    [TestCase("/tmp/archive.zip")]
    [TestCase("C:\\archive.zip")]
    [TestCase("archive.zip:stream")]
    [TestCase("..%2Farchive.zip")]
    [TestCase("..%5carchive.zip")]
    [TestCase("C%3Aarchive.zip")]
    public void TraversalAndPathBearingNamesAreRejected(string fileName)
    {
        ValidationException exception = Assert.Throws<ValidationException>(() => BulkFileNameSelector.NormalizeBulkFileName(fileName, "catalogFileName"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("BULK_FILENAME_TRAVERSAL"));
            Assert.That(exception.FieldName, Is.EqualTo("catalogFileName"));
        });
    }

    [TestCase("CON")]
    [TestCase("con.zip")]
    [TestCase("PRN.txt")]
    [TestCase("AUX")]
    [TestCase("NUL.xml")]
    [TestCase("CLOCK$.json")]
    [TestCase("COM1.csv")]
    [TestCase("COM1 .csv")]
    [TestCase("com9.tar.gz")]
    [TestCase("COM¹.zip")]
    [TestCase("LPT1")]
    [TestCase("lpt9.zip")]
    [TestCase("CONIN$.txt")]
    [TestCase("CONOUT$.txt")]
    public void DeviceNamesAreRejectedEvenWhenTheyHaveExtensions(string fileName)
    {
        ValidationException exception = Assert.Throws<ValidationException>(() => BulkFileNameSelector.NormalizeBulkFileName(fileName, "catalogFileName"));

        Assert.That(exception.ErrorCode, Is.EqualTo("BULK_FILENAME_DEVICE"));
    }

    [TestCase(".")]
    [TestCase("..")]
    [TestCase("~")]
    [TestCase("...")]
    public void SpecialFilesystemNamesAreRejected(string fileName)
    {
        ValidationException exception = Assert.Throws<ValidationException>(() => BulkFileNameSelector.NormalizeBulkFileName(fileName, "catalogFileName"));

        Assert.That(exception.ErrorCode, Is.AnyOf("BULK_FILENAME_TRAVERSAL", "BULK_FILENAME_SPECIAL"));
    }

    [Test]
    public void OrdinaryReservedPunctuationIsSanitizedWithoutChangingTheExtension()
    {
        string result = BulkFileNameSelector.NormalizeBulkFileName("grant<>\"|?*2026.tar.gz...", "catalogFileName");

        Assert.That(result, Is.EqualTo("grant______2026.tar.gz"));
    }

    [Test]
    public void ControlCharactersAreRejected()
    {
        ValidationException exception = Assert.Throws<ValidationException>(() => BulkFileNameSelector.NormalizeBulkFileName("grant\u0000.zip", "catalogFileName"));

        Assert.That(exception.ErrorCode, Is.EqualTo("BULK_FILENAME_CONTROL_CHARACTER"));
    }

    [Test]
    public void UnicodeDirectionOverridesCannotSpoofTheExtension()
    {
        ValidationException exception = Assert.Throws<ValidationException>(() => BulkFileNameSelector.NormalizeBulkFileName("grant\u202Ecod.exe.zip", "catalogFileName"));

        Assert.That(exception.ErrorCode, Is.EqualTo("BULK_FILENAME_CONTROL_CHARACTER"));
    }

    [Test]
    public void UnsafePreferredDispositionNameIsRejectedEvenWithSafeFallbacks()
    {
        using HttpResponseMessage response = ResponseWithDisposition("attachment; filename=safe.zip; filename*=UTF-8''..%2Funsafe.zip");

        ValidationException exception = Assert.Throws<ValidationException>(() => BulkFileNameSelector.SelectBulkFileName("catalog.zip", response));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("BULK_FILENAME_TRAVERSAL"));
            Assert.That(exception.FieldName, Is.EqualTo(UsptoHeaders.ContentDisposition));
        });
    }

    [Test]
    public void MissingCatalogAndDispositionNamesAreRejected()
    {
        using HttpResponseMessage response = new HttpResponseMessage();

        ValidationException exception = Assert.Throws<ValidationException>(() => BulkFileNameSelector.SelectBulkFileName(null, response));

        Assert.That(exception.ErrorCode, Is.EqualTo("BULK_FILENAME_REQUIRED"));
    }

    private static HttpResponseMessage ResponseWithDisposition(string value)
    {
        HttpResponseMessage response = new HttpResponseMessage();
        response.Content.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(value);
        return response;
    }
}
