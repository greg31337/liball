using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class SafeOpeningTests
{
    [Test]
    public void DocumentFactory_OpensPathsStreamsByteArraysAndSpansWithOptions()
    {
        PptxDocumentFactory factory = new PptxDocumentFactory();
        byte[] bytes = factory.CreatePresentation().SaveBytes();
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "safe-document-opening.pptx");
        File.WriteAllBytes(path, bytes);
        PptxZipReadOptions options = new PptxZipReadOptions();

        PptxDocument fromPath = factory.OpenFile(path);
        PptxDocument fromPathWithOptions = factory.OpenFileWithOptions(path, options);
        using MemoryStream stream = new MemoryStream(bytes);
        PptxDocument fromStream = factory.OpenStreamWithOptions(stream, options);
        PptxDocument fromBytes = factory.OpenBytesWithOptions(bytes, options);
        ReadOnlySpan<byte> span = bytes.AsSpan();
        PptxDocument fromSpan = factory.OpenSpan(span);
        PptxDocument fromSpanWithOptions = factory.OpenSpanWithOptions(span, options);

        AssertValidDocument(fromPath);
        AssertValidDocument(fromPathWithOptions);
        AssertValidDocument(fromStream);
        AssertValidDocument(fromBytes);
        AssertValidDocument(fromSpan);
        AssertValidDocument(fromSpanWithOptions);
    }

    [Test]
    public void PackageOperations_OpensPathsAndSpansAndAppliesSafetyLimits()
    {
        PptxDocumentFactory documents = new PptxDocumentFactory();
        PptxPackageOperations packages = new PptxPackageOperations();
        byte[] bytes = documents.CreatePresentation().SaveBytes();
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "safe-package-opening.pptx");
        File.WriteAllBytes(path, bytes);

        PptxPackage fromPath = packages.OpenFile(path);
        PptxPackage fromSpan = packages.OpenSpan(bytes.AsSpan());
        PptxZipReadOptions options = new PptxZipReadOptions();
        options.MaxEntryCount = 0;
        PptxPackage limitedPath = packages.OpenFileWithOptions(path, options);
        PptxPackage limitedSpan = packages.OpenSpanWithOptions(bytes.AsSpan(), options);

        Assert.That(packages.HasPart(fromPath, PptxPartNames.Presentation), Is.True);
        Assert.That(packages.HasPart(fromSpan, PptxPartNames.Presentation), Is.True);
        Assert.That(packages.Failed(limitedPath), Is.True);
        Assert.That(limitedPath.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP001"));
        Assert.That(packages.Failed(limitedSpan), Is.True);
        Assert.That(limitedSpan.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP001"));
    }

    [Test]
    public void StreamOpening_LeavesCallerStreamOpenAfterSuccessAndFailure()
    {
        PptxDocumentFactory factory = new PptxDocumentFactory();
        byte[] bytes = factory.CreatePresentation().SaveBytes();
        MemoryStream validStream = new MemoryStream(bytes);

        PptxDocument opened = factory.OpenStream(validStream);

        AssertValidDocument(opened);
        Assert.That(validStream.CanRead, Is.True);

        MemoryStream invalidStream = new MemoryStream(new byte[] { 1, 2, 3 });
        Assert.Throws<InvalidDataException>(() => factory.Open(invalidStream));
        Assert.That(invalidStream.CanRead, Is.True);

        validStream.Dispose();
        invalidStream.Dispose();
    }

    [Test]
    public void StreamOpening_RejectsUnreadableStreamsExplicitly()
    {
        PptxPackageOperations packages = new PptxPackageOperations();
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "write-only-opening.tmp");
        using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => packages.Open(stream))!;

        Assert.That(exception.Message, Does.Contain("readable"));
        Assert.That(stream.CanWrite, Is.True);
    }

    [Test]
    public void ZipReadOptions_RejectNegativeAndNonFiniteLimits()
    {
        PptxDocumentFactory documents = new PptxDocumentFactory();
        PptxPackageOperations packages = new PptxPackageOperations();
        byte[] bytes = documents.CreatePresentation().SaveBytes();
        PptxZipReadOptions options = new PptxZipReadOptions();

        options.MaxEntryCount = -1;
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => packages.OpenBytesWithOptions(bytes, options))!.ParamName,
            Is.EqualTo("MaxEntryCount"));

        options.MaxEntryCount = 1;
        options.MaxEntryBytes = -1L;
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => packages.OpenBytesWithOptions(bytes, options))!.ParamName,
            Is.EqualTo("MaxEntryBytes"));

        options.MaxEntryBytes = 1L;
        options.MaxTotalBytes = -1L;
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => packages.OpenBytesWithOptions(bytes, options))!.ParamName,
            Is.EqualTo("MaxTotalBytes"));

        options.MaxTotalBytes = 1L;
        options.MaxCompressionRatio = double.NaN;
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => packages.OpenBytesWithOptions(bytes, options))!.ParamName,
            Is.EqualTo("MaxCompressionRatio"));

        options.MaxCompressionRatio = double.PositiveInfinity;
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => packages.OpenBytesWithOptions(bytes, options))!.ParamName,
            Is.EqualTo("MaxCompressionRatio"));
    }

    static void AssertValidDocument(PptxDocument document)
    {
        PptxResult validation = document.Validate();
        Assert.That(validation.Succeeded(), Is.True, validation.FormatDiagnostics());
        Assert.That(document.LoadPresentation().SlideCount(), Is.EqualTo(1));
    }
}
