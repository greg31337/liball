using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class BinaryMediaOwnershipCompletionTests
{
    [Test]
    public void ImageReaderAndWorkbookReplacement_OwnCallerProvidedBytes()
    {
        byte[] readerBytes = PngBytes();
        XlsxImagePart image = new XlsxImagePartReader().ReadBytesWithContentType("/xl/media/image1.png", "image/png", readerBytes);
        readerBytes[0] = 0;
        XlsxImagePartWriteResult imageWrite = new XlsxImagePartWriter().WriteBytes(image);

        XlsxWorkbook workbook = XlsxWorkbook.Create();
        byte[] initialBytes = SvgBytes("red");
        XlsxRawImageInsertResult added = workbook.AddRawImage(initialBytes, "image/svg+xml");
        initialBytes[0] = 0;
        byte[] replacementBytes = SvgBytes("blue");
        XlsxRawImageInsertResult replaced = workbook.ReplaceRawImagePart(added.PartPath, replacementBytes, "image/svg+xml");
        replacementBytes[0] = 0;
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(image.IsValid, Is.True);
        Assert.That(image.Data, Is.Not.SameAs(readerBytes));
        Assert.That(imageWrite.IsValid, Is.True);
        Assert.That(imageWrite.Bytes, Is.EqualTo(PngBytes()));
        Assert.That(added.IsValid, Is.True);
        Assert.That(replaced.IsValid, Is.True);
        Assert.That(replaced.ImagePart!.Data, Is.EqualTo(SvgBytes("blue")));
        Assert.That(save.IsValid, Is.True);
        Assert.That(package.GetEntry(added.PartPath)!.Data, Is.EqualTo(SvgBytes("blue")));
    }

    [Test]
    public void EmbeddedObjectAndVbaReaders_OwnCallerProvidedBytes()
    {
        byte[] embeddedBytes = EmbeddedBytes();
        byte[] vbaBytes = VbaBytes();
        XlsxEmbeddedObjectPart embedded = new XlsxEmbeddedObjectPartReader().ReadBytes("/xl/embeddings/embeddedPackage1.xlsx", XlsxContentTypes.EmbeddedSpreadsheetPackage, embeddedBytes);
        XlsxVbaProjectPart vba = new XlsxVbaProjectPartReader().ReadBytes(XlsxPackagePaths.VbaProject, vbaBytes);
        embeddedBytes[0] = 0;
        vbaBytes[0] = 0;
        XlsxEmbeddedObjectPartWriteResult embeddedWrite = new XlsxEmbeddedObjectPartWriter().WriteBytes(embedded);
        XlsxVbaProjectPartWriteResult vbaWrite = new XlsxVbaProjectPartWriter().WriteBytes(vba);

        Assert.That(embedded.IsValid, Is.True);
        Assert.That(embedded.Data, Is.Not.SameAs(embeddedBytes));
        Assert.That(embeddedWrite.IsValid, Is.True);
        Assert.That(embeddedWrite.Bytes, Is.EqualTo(EmbeddedBytes()));
        Assert.That(vba.IsValid, Is.True);
        Assert.That(vba.Data, Is.Not.SameAs(vbaBytes));
        Assert.That(vbaWrite.IsValid, Is.True);
        Assert.That(vbaWrite.Bytes, Is.EqualTo(VbaBytes()));
    }

    [Test]
    public void InCellPictureMetadataReader_OwnsCallerProvidedBytes()
    {
        string xml = "<rvData xmlns=\"http://schemas.microsoft.com/office/spreadsheetml/2017/richdata\" count=\"1\"><rv s=\"0\"><v>0</v></rv></rvData>";
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        XlsxInCellPictureMetadataPart part = new XlsxInCellPictureMetadataPartReader().ReadBytes("/xl/richData/rdrichvalue.xml", XlsxContentTypes.RichValue, bytes);
        bytes[1] = 0;
        XlsxInCellPictureMetadataPartWriteResult write = new XlsxInCellPictureMetadataPartWriter().WriteString(part);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Data, Is.Not.SameAs(bytes));
        Assert.That(part.RawXml, Is.EqualTo(xml));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Is.EqualTo(xml));
    }

    static byte[] PngBytes()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
    }

    static byte[] SvgBytes(string color)
    {
        return Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect fill=\"" + color + "\"/></svg>");
    }

    static byte[] EmbeddedBytes()
    {
        return new byte[] { 0x50, 0x4B, 0x03, 0x04, 0xAA, 0xBB, 0xCC, 0xDD };
    }

    static byte[] VbaBytes()
    {
        return new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0x01, 0x02, 0x03, 0x04 };
    }
}
