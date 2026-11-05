using System;
using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfDocumentCompositionTests
{
    [Test]
    public void ImportPage_DeepCopiesPageGraphAndReconcilesNames()
    {
        PdfDocument target = new PdfDocument();
        PdfPage targetPage = target.AddPage(200, 200);
        target.AddImage(1, 1, new byte[] { 1, 2, 3 });
        target.AddNamedDestination("chapter", targetPage, 0, 200);
        target.AddTextField("customer", targetPage, 1, 1, 20, 10);
        target.AddAttachment("notes.txt", new byte[] { 1 });

        PdfDocument source = new PdfDocument();
        PdfPage sourcePage = source.AddPage(300, 400);
        sourcePage.SetMargins(10, 20, 30, 40);
        sourcePage.SetCropBox(new PdfRectangle(5, 6, 280, 380));
        sourcePage.SetTrimBox(new PdfRectangle(10, 12, 260, 350));
        PdfImage image = source.AddImage(1, 1, new byte[] { 9, 8, 7 });
        sourcePage.GetCanvas().DrawImage(image, 10, 20, 30, 40);
        source.AddNamedDestination("chapter", sourcePage, 12, 350);
        sourcePage.AddInternalLink("chapter", 1, 2, 3, 4);
        sourcePage.AddExternalLink("https://example.test", 5, 6, 7, 8);
        PdfAnnotation annotation = sourcePage.AddTextAnnotation("review", 9, 10, 11, 12);
        annotation.SetColor(new PdfColor("RGB", 0.1, 0.2, 0.3, 0));
        source.AddOutline("Imported chapter", sourcePage, 12, 350);
        PdfAcroFormField field = source.AddTextField("customer", sourcePage, 20, 30, 100, 20);
        field.SetValue("Ada");
        source.AddAttachmentWithMetadata("notes.txt", new byte[] { 2 }, "text/plain", "source");
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle("Source title");
        source.SetMetadata(metadata);

        PdfPage imported = target.ImportPage(source, 0);

        Assert.That(target.GetPageCount(), Is.EqualTo(2));
        Assert.That(imported, Is.Not.SameAs(sourcePage));
        Assert.That(imported.GetDimensions().Width, Is.EqualTo(300));
        AssertRectangle(imported.GetCropBox(), 5, 6, 280, 380);
        AssertRectangle(imported.GetTrimBox(), 10, 12, 260, 350);
        Assert.That(imported.Margins.Left, Is.EqualTo(10));
        Assert.That(imported.GetCanvas().ToPdfString(), Does.Contain("/Im2 Do"));
        Assert.That(sourcePage.GetCanvas().ToPdfString(), Does.Contain("/Im1 Do"));
        Assert.That(target.Images[1], Is.Not.SameAs(source.Images[0]));
        Assert.That(imported.Annotations[0], Is.Not.SameAs(sourcePage.Annotations[0]));
        Assert.That(imported.ExternalLinks.Count, Is.EqualTo(1));
        Assert.That(imported.InternalLinks[0].DestinationName, Is.EqualTo("chapter (2)"));
        Assert.That(target.NamedDestinations[1].Page, Is.SameAs(imported));
        Assert.That(target.Outlines[0].Page, Is.SameAs(imported));
        Assert.That(target.AcroForm.GetField(1).GetFieldName(), Is.EqualTo("customer (2)"));
        Assert.That(target.AcroForm.GetField(1).GetValue(), Is.EqualTo("Ada"));
        Assert.That(target.Attachments[1].FileName, Is.EqualTo("notes (2).txt"));
        Assert.That(target.Metadata.Title, Is.EqualTo("Source title"));
    }

    [Test]
    public void StampPages_ComposeInLayerOrderAndReconcileResources()
    {
        PdfDocument source = new PdfDocument();
        PdfPage stamp = source.AddPage(100, 100);
        PdfImage stampImage = source.AddImage(1, 1, new byte[] { 200, 100, 50 });
        stamp.GetCanvas().DrawText("STAMP", 2, 3, 8);
        stamp.GetCanvas().DrawImage(stampImage, 4, 5, 6, 7);

        PdfDocument target = new PdfDocument();
        PdfPage foregroundPage = target.AddPage(100, 100);
        PdfPage backgroundPage = target.AddPage(100, 100);
        PdfImage targetImage = target.AddImage(1, 1, new byte[] { 1, 2, 3 });
        foregroundPage.GetCanvas().DrawImage(targetImage, 20, 20, 10, 10);
        foregroundPage.GetCanvas().DrawText("BODY", 10, 10, 8);
        backgroundPage.GetCanvas().DrawText("BODY", 10, 10, 8);

        target.StampPageForeground(source, 0, 0);
        target.StampPageBackground(source, 0, 1);

        string foreground = foregroundPage.GetCanvas().ToPdfString();
        string background = backgroundPage.GetCanvas().ToPdfString();
        Assert.That(foreground.IndexOf("BODY", StringComparison.Ordinal), Is.LessThan(foreground.IndexOf("STAMP", StringComparison.Ordinal)));
        Assert.That(background.IndexOf("STAMP", StringComparison.Ordinal), Is.LessThan(background.IndexOf("BODY", StringComparison.Ordinal)));
        Assert.That(foreground, Does.Contain("/Im2 Do"));
        Assert.That(background, Does.Contain("/Im3 Do"));
        Assert.That(target.Images.Count, Is.EqualTo(3));
        Assert.That(Encoding.ASCII.GetString(target.ToBytes()), Does.Contain("/Im2"));
    }

    [Test]
    public void SplitDocument_PreservesDocumentAndPageObjectsInSourceOrder()
    {
        PdfDocument source = new PdfDocument();
        PdfPage first = source.AddPage(100, 110);
        PdfPage second = source.AddPage(120, 130);
        PdfPage third = source.AddPage(140, 150);
        first.GetCanvas().DrawText("one", 1, 1, 8);
        second.GetCanvas().DrawText("two", 1, 1, 8);
        third.GetCanvas().DrawText("three", 1, 1, 8);
        source.AddNamedDestination("second", second, 1, 120);
        source.AddOutline("Second", second, 1, 120);
        source.AddTextField("first-field", first, 1, 1, 20, 10);
        source.AddCheckbox("third-field", third, 1, 1, 10, 10);
        source.AddAttachment("data.bin", new byte[] { 4, 5, 6 });
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetAuthor("Writer");
        source.SetMetadata(metadata);

        PdfDocument[] parts = source.SplitDocument(2);

        Assert.That(parts.Length, Is.EqualTo(2));
        Assert.That(parts[0].GetPageCount(), Is.EqualTo(2));
        Assert.That(parts[1].GetPageCount(), Is.EqualTo(1));
        Assert.That(parts[0].GetPage(0).GetCanvas().ToPdfString(), Does.Contain("one"));
        Assert.That(parts[0].GetPage(1).GetCanvas().ToPdfString(), Does.Contain("two"));
        Assert.That(parts[1].GetPage(0).GetCanvas().ToPdfString(), Does.Contain("three"));
        Assert.That(parts[0].NamedDestinations[0].Page, Is.SameAs(parts[0].GetPage(1)));
        Assert.That(parts[0].Outlines[0].Page, Is.SameAs(parts[0].GetPage(1)));
        Assert.That(parts[0].AcroForm.GetFieldCount(), Is.EqualTo(1));
        Assert.That(parts[1].AcroForm.GetFieldCount(), Is.EqualTo(1));
        Assert.That(parts[0].Attachments[0].Data, Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(parts[1].Metadata.Author, Is.EqualTo("Writer"));
        Assert.That(parts[0].ToBytes(), Is.Not.Empty);
        Assert.That(parts[1].ToBytes(), Is.Not.Empty);
    }

    [Test]
    public void MergeDocument_AppendsPagesAndKeepsTargetMetadata()
    {
        PdfDocument target = new PdfDocument();
        PdfPage targetPage = target.AddPage(100, 100);
        targetPage.GetCanvas().DrawText("target", 1, 1, 8);
        target.AddNamedDestination("top", targetPage, 0, 100);
        target.AddAttachment("same.bin", new byte[] { 1 });
        PdfMetadata targetMetadata = new PdfMetadata();
        targetMetadata.SetTitle("Target title");
        target.SetMetadata(targetMetadata);

        PdfDocument source = new PdfDocument();
        PdfPage firstSourcePage = source.AddPage(200, 200);
        PdfPage secondSourcePage = source.AddPage(300, 300);
        firstSourcePage.GetCanvas().DrawText("source one", 1, 1, 8);
        secondSourcePage.GetCanvas().DrawText("source two", 1, 1, 8);
        source.AddNamedDestination("top", secondSourcePage, 0, 300);
        source.AddAttachment("same.bin", new byte[] { 2 });
        PdfMetadata sourceMetadata = new PdfMetadata();
        sourceMetadata.SetTitle("Source title");
        source.SetMetadata(sourceMetadata);

        target.MergeDocument(source);

        Assert.That(target.GetPageCount(), Is.EqualTo(3));
        Assert.That(target.GetPage(1).GetCanvas().ToPdfString(), Does.Contain("source one"));
        Assert.That(target.GetPage(2).GetCanvas().ToPdfString(), Does.Contain("source two"));
        Assert.That(target.NamedDestinations[1].Name, Is.EqualTo("top (2)"));
        Assert.That(target.NamedDestinations[1].Page, Is.SameAs(target.GetPage(2)));
        Assert.That(target.Attachments[1].FileName, Is.EqualTo("same (2).bin"));
        Assert.That(target.Metadata.Title, Is.EqualTo("Target title"));
        byte[] firstOutput = target.ToBytes();
        byte[] secondOutput = target.ToBytes();
        Assert.That(firstOutput, Is.EqualTo(secondOutput));
    }

    [Test]
    public void CompositionApis_ValidateRangesAndSelfMerge()
    {
        PdfDocument document = new PdfDocument();
        document.AddPage(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => document.SplitDocument(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.ImportPage(new PdfDocument(), 0));
        Assert.Throws<ArgumentException>(() => document.MergeDocument(document));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.StampPageForeground(document, 2, 0));
    }

    [Test]
    public void InsertAndLargeSplitLimitsPreservePageOrderWithoutOverflow()
    {
        PdfDocument source = new PdfDocument();
        PdfPage sourcePage = source.AddPage(120, 130);
        sourcePage.GetCanvas().DrawText("inserted", 1, 1, 8);

        PdfDocument target = new PdfDocument();
        target.AddPage(100, 100).GetCanvas().DrawText("first", 1, 1, 8);
        target.AddPage(140, 150).GetCanvas().DrawText("last", 1, 1, 8);

        PdfPage inserted = target.InsertImportedPage(source, 0, 1);
        PdfDocument[] parts = target.SplitDocument(int.MaxValue);

        Assert.That(target.GetPage(1), Is.SameAs(inserted));
        Assert.That(target.GetPage(0).GetCanvas().ToPdfString(), Does.Contain("first"));
        Assert.That(target.GetPage(1).GetCanvas().ToPdfString(), Does.Contain("inserted"));
        Assert.That(target.GetPage(2).GetCanvas().ToPdfString(), Does.Contain("last"));
        Assert.That(parts.Length, Is.EqualTo(1));
        Assert.That(parts[0].GetPageCount(), Is.EqualTo(3));
        Assert.That(parts[0].GetPage(1).GetCanvas().ToPdfString(), Does.Contain("inserted"));
    }

    static void AssertRectangle(PdfRectangle rectangle, double x, double y, double width, double height)
    {
        Assert.That(rectangle.X, Is.EqualTo(x));
        Assert.That(rectangle.Y, Is.EqualTo(y));
        Assert.That(rectangle.Width, Is.EqualTo(width));
        Assert.That(rectangle.Height, Is.EqualTo(height));
    }
}
