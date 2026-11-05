using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfCoreWriterCompletionTests
{
    [Test]
    public void DocumentConstructorAndWriterOptionsExposeStableDefaults()
    {
        PdfDocument first = new PdfDocument();
        PdfDocument second = new PdfDocument();
        PdfWriterOptions options = new PdfWriterOptions();

        Assert.That(first.GetPageCount(), Is.EqualTo(0));
        Assert.That(first.GetFontRegistry(), Is.SameAs(first.FontRegistry));
        Assert.That(second.GetFontRegistry(), Is.Not.SameAs(first.GetFontRegistry()));
        Assert.That(options.Version, Is.EqualTo(PdfVersion.Pdf14));
        Assert.That(options.Deterministic, Is.False);
        Assert.That(options.DeterministicDocumentId, Is.EqualTo("00000000000000000000000000000000"));
        Assert.That(options.CompressContentStreams, Is.False);
    }

    [Test]
    public void PageCreationAndInsertionVariantsPreserveRequestedOrderAndDimensions()
    {
        PdfDocument document = new PdfDocument();
        PdfPage last = document.AddPageWithDimensions(new PdfPageDimensions(300, 400));
        PdfPage first = document.InsertLetterPage(0);
        PdfPage middle = document.InsertPageWithDimensions(1, new PdfPageDimensions(200, 250));

        Assert.That(document.GetPageCount(), Is.EqualTo(3));
        Assert.That(document.GetPage(0), Is.SameAs(first));
        Assert.That(document.GetPage(1), Is.SameAs(middle));
        Assert.That(document.GetPage(2), Is.SameAs(last));
        Assert.That(first.GetDimensions().Width, Is.EqualTo(PdfPageSize.LetterWidth));
        Assert.That(middle.GetDimensions().Height, Is.EqualTo(250));
        Assert.That(last.GetDimensions().Width, Is.EqualTo(300));
    }

    [Test]
    public void PageCreationRejectsNullNonFiniteAndNonPositiveDimensions()
    {
        PdfDocument document = new PdfDocument();

        Assert.Throws<ArgumentNullException>(() => document.AddPageWithDimensions(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddPage(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddPage(100, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.InsertPage(0, double.NaN, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.InsertPage(0, 100, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfPage(double.NegativeInfinity, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.InsertLetterPage(1));
        Assert.That(document.GetPageCount(), Is.EqualTo(0));
    }

    [Test]
    public void WriterOptionsApplyVersionCompressionAndDeterministicIdentifierTogether()
    {
        PdfWriterOptions options = new PdfWriterOptions();
        options.Version = PdfVersion.Pdf17;
        options.Deterministic = true;
        options.DeterministicDocumentId = "abcdef0123456789abcdef0123456789";
        options.CompressContentStreams = true;
        PdfDocument document = CreateDocument("combined options");

        document.SetWriterOptions(options);
        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.StartWith("%PDF-1.7\n"));
        Assert.That(pdf, Does.Contain("/Filter /FlateDecode"));
        Assert.That(pdf, Does.Contain("/ID [<abcdef0123456789abcdef0123456789><abcdef0123456789abcdef0123456789>]"));
    }

    [Test]
    public void WriterOptionsRejectInvalidDeterministicIdentifierAtConfigurationAndSerialization()
    {
        PdfWriterOptions invalid = new PdfWriterOptions();
        invalid.Deterministic = true;
        invalid.DeterministicDocumentId = "short";
        PdfDocument document = CreateDocument("invalid options");

        Assert.Throws<ArgumentException>(() => document.SetWriterOptions(invalid));

        PdfWriterOptions mutable = new PdfWriterOptions();
        mutable.Deterministic = true;
        mutable.DeterministicDocumentId = "11111111111111111111111111111111";
        document.SetWriterOptions(mutable);
        mutable.DeterministicDocumentId = null!;

        Assert.Throws<ArgumentException>(() => document.ToBytes());
    }

    [Test]
    public void OutputMethodsValidateDestinationsCreateDirectoriesAndLeaveStreamsOpen()
    {
        PdfDocument document = CreateDocument("destinations");
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "core-writer", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "nested", "document.pdf");

        Assert.Throws<ArgumentNullException>(() => document.Save(null!));
        Assert.Throws<ArgumentException>(() => document.Save(string.Empty));
        Assert.Throws<ArgumentNullException>(() => document.SaveToStream(null!));
        using (MemoryStream readOnly = new MemoryStream(new byte[8], false))
        {
            Assert.Throws<ArgumentException>(() => document.SaveToStream(readOnly));
        }

        document.Save(path);
        Assert.That(File.Exists(path), Is.True);

        using (MemoryStream writable = new MemoryStream())
        {
            document.SaveToStream(writable);
            Assert.That(writable.CanWrite, Is.True);
            Assert.That(writable.Length, Is.GreaterThan(0));
        }
    }

    [Test]
    public void ToBytesReturnsFreshArraysWithStableDeterministicContent()
    {
        PdfDocument document = CreateDocument("fresh arrays");
        document.UseDeterministicWriterModeWithId("22222222222222222222222222222222");

        byte[] first = document.ToBytes();
        byte[] second = document.ToBytes();
        byte original = second[0];
        first[0] = 0;

        Assert.That(second[0], Is.EqualTo(original));
        Assert.That(document.ToBytes(), Is.EqualTo(second));
    }

    [Test]
    public void IndependentStructureScanFindsContinuousObjectsAndValidStartXref()
    {
        PdfDocument document = CreateDocument("structure scan");
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle("Core writer");
        document.SetMetadata(metadata);
        document.AddImage(1, 1, new byte[] { 1, 2, 3 });
        document.AddAttachment("notes.txt", Encoding.ASCII.GetBytes("notes"));

        byte[] bytes = document.ToBytes();
        string pdf = Encoding.ASCII.GetString(bytes);
        MatchCollection objects = Regex.Matches(pdf, @"(?m)^(\d+) 0 obj$");
        Match trailer = Regex.Match(pdf, @"trailer\n<< /Size (\d+) /Root 1 0 R /Info \d+ 0 R >>\nstartxref\n(\d+)\n%%EOF\n$");

        Assert.That(trailer.Success, Is.True);
        Assert.That(objects.Count + 1, Is.EqualTo(int.Parse(trailer.Groups[1].Value)));
        for (int index = 0; index < objects.Count; index++)
        {
            Assert.That(int.Parse(objects[index].Groups[1].Value), Is.EqualTo(index + 1));
        }

        int startXref = int.Parse(trailer.Groups[2].Value);
        Assert.That(Encoding.ASCII.GetString(bytes, startXref, 5), Is.EqualTo("xref\n"));
    }

    static PdfDocument CreateDocument(string text)
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        page.GetCanvas().DrawText(text, 10, 20, 10);
        return document;
    }
}
