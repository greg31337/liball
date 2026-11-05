using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class EncryptionInfoTests
{
    [Test]
    public void EncryptionInfoReader_ClassifiesAgileAndStandardVersions()
    {
        XlsxEncryptionInfo agile = XlsxEncryptionDetector.ReadEncryptionInfo(new byte[] { 0x04, 0x00, 0x04, 0x00 });
        XlsxEncryptionInfo standard = XlsxEncryptionDetector.ReadEncryptionInfo(new byte[] { 0x03, 0x00, 0x02, 0x00 });

        Assert.That(agile.IsValid, Is.True);
        Assert.That(agile.IsAgile, Is.True);
        Assert.That(agile.EncryptionKind, Is.EqualTo("Agile"));
        Assert.That(standard.IsValid, Is.True);
        Assert.That(standard.IsStandard, Is.True);
        Assert.That(standard.EncryptionKind, Is.EqualTo("Standard"));
    }

    [Test]
    public void EncryptionInfoReader_ReportsShortStreamsAndUnknownVersions()
    {
        XlsxEncryptionInfo tooShort = XlsxEncryptionDetector.ReadEncryptionInfo(new byte[] { 0x04 });
        XlsxEncryptionInfo unknown = XlsxEncryptionDetector.ReadEncryptionInfo(new byte[] { 0x09, 0x00, 0x09, 0x00 });

        Assert.That(tooShort.IsValid, Is.False);
        Assert.That(HasDiagnostic(tooShort.Validation, "encryptionInfo.tooShort"), Is.True);
        Assert.That(unknown.IsValid, Is.True);
        Assert.That(unknown.EncryptionKind, Is.EqualTo("Unknown"));
        Assert.That(HasDiagnostic(unknown.Validation, "encryptionInfo.versionUnknown"), Is.True);
    }

    [Test]
    public void OpenBytes_WithPasswordReportsInvalidCompoundFileForMalformedEncryptedPackage()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.Password = "secret";

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(EncryptedCompoundFileHeader(), options);

        Assert.That(open.IsValid, Is.False);
        Assert.That(HasDiagnostic(open.Validation, "cfb.invalidHeader"), Is.True);
        Assert.That(HasDiagnostic(open.Validation, "workbook.open.encryptedPackageUnsupported"), Is.False);
    }

    static byte[] EncryptedCompoundFileHeader()
    {
        return new byte[]
        {
            0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1,
            0x00, 0x00, 0x00, 0x00
        };
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
