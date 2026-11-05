using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class EncryptedWorkbookDetectionTests
{
    [Test]
    public void OpenBytes_DetectsEncryptedCompoundFilePackageBeforeZipParsing()
    {
        byte[] bytes = EncryptedCompoundFileHeader();

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);

        Assert.That(open.IsValid, Is.False);
        Assert.That(HasDiagnostic(open.Validation, "workbook.open.encryptedPackageUnsupported"), Is.True);
        Assert.That(HasDiagnostic(open.Validation, "zip.invalidArchive"), Is.False);
        Assert.That(open.Workbook, Is.Null);
    }

    [Test]
    public void EncryptionDetector_RecognizesCompoundFileHeader()
    {
        Assert.That(XlsxEncryptionDetector.IsEncryptedPackageBytes(EncryptedCompoundFileHeader()), Is.True);
        Assert.That(XlsxEncryptionDetector.IsEncryptedPackageBytes(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0, 0, 0, 0 }), Is.False);
    }

    [Test]
    public void ZipPackageReader_ReportsInvalidArchiveForMalformedAndTruncatedZipInput()
    {
        XlsxZipPackageReadResult malformed = new XlsxZipPackageReader().ReadBytes(new byte[] { 1, 2, 3, 4, 5, 6 }, new XlsxReaderOptions());
        XlsxZipPackageReadResult truncated = new XlsxZipPackageReader().ReadBytes(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0, 0, 0, 0 }, new XlsxReaderOptions());

        Assert.That(malformed.IsValid, Is.False);
        Assert.That(HasDiagnostic(malformed.Validation, "zip.invalidArchive"), Is.True);
        Assert.That(truncated.IsValid, Is.False);
        Assert.That(HasDiagnostic(truncated.Validation, "zip.invalidArchive"), Is.True);
    }

    static byte[] EncryptedCompoundFileHeader()
    {
        return new byte[]
        {
            0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
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
