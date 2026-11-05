using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class EmbeddedContentManagerTests
{
    [Test]
    public void LoadsSubdocumentsOleMathChartsReplacementsAndThumbnails()
    {
        OdfDocument document = ReadPackage(CreatePackage());
        OdfEmbeddedObject chartObject = document.EmbeddedObjects.Single(item =>
            item.PackagePath == "Object1/");
        OdfEmbeddedObject ole = document.EmbeddedObjects.Single(item =>
            item.Kind == OdfEmbeddedObjectKind.Ole);
        OdfEmbeddedObject inlineMath = document.EmbeddedObjects.Single(item =>
            item.Kind == OdfEmbeddedObjectKind.MathMl
            && item.SourceKind == OdfResourceSourceKind.Inline);
        OdfEmbeddedObject mathPart = document.EmbeddedObjects.Single(item =>
            item.PackagePath == "Formula/math.xml");
        OdfChart inlineChart = document.Charts.Single(item =>
            item.Node != null);
        OdfThumbnail thumbnail = document.Thumbnails.Single();

        Assert.Multiple(() =>
        {
            Assert.That(document.EmbeddedObjects, Has.Length.EqualTo(5));
            Assert.That(chartObject.Kind,
                Is.EqualTo(OdfEmbeddedObjectKind.Chart));
            Assert.That(chartObject.DocumentKind,
                Is.EqualTo(OdfDocumentKind.Chart));
            Assert.That(chartObject.IsDirectoryPackage, Is.True);
            Assert.That(chartObject.PackageEntryExists, Is.True);
            Assert.That(chartObject.IsContentAvailable, Is.True);
            Assert.That(chartObject.PackageEntries, Is.Not.Empty);
            Assert.That(chartObject.ReplacementImage, Is.Not.Null);
            Assert.That(chartObject.ReplacementImage?.PackagePath,
                Is.EqualTo("ObjectReplacements/Object1.png"));
            Assert.That(chartObject.Drawing?.EmbeddedObject,
                Is.SameAs(chartObject));
            Assert.That(ole.ClassId, Is.EqualTo("ole-class"));
            Assert.That(ole.PackagePath, Is.EqualTo("Object2.bin"));
            Assert.That(ole.Data, Is.EqualTo(OleData()));
            Assert.That(inlineMath.MathNode?.Name?.NamespaceName,
                Is.EqualTo(OdfNamespaces.Math));
            Assert.That(mathPart.MediaType, Is.EqualTo(OdfMediaTypes.MathMl));
            Assert.That(mathPart.Data, Is.Not.Empty);
            Assert.That(document.Charts, Has.Length.EqualTo(2));
            Assert.That(inlineChart.Kind, Is.EqualTo(OdfChartKind.Pie));
            Assert.That(inlineChart.EmbeddedObject, Is.Not.Null);
            Assert.That(thumbnail.Path, Is.EqualTo(OdfPartNames.Thumbnail));
            Assert.That(thumbnail.DetectedFormat,
                Is.EqualTo(OdfImageFormat.Png));
            Assert.That(thumbnail.IsValid, Is.True);
            Assert.That(thumbnail.HasMediaTypeMismatch, Is.False);
        });
    }

    [Test]
    public void WritesOleChartAndThumbnailChanges()
    {
        OdfDocument document = ReadPackage(CreatePackage());
        OdfEmbeddedObject ole = document.EmbeddedObjects.Single(item =>
            item.Kind == OdfEmbeddedObjectKind.Ole);
        OdfChart chart = document.Charts.Single(item => item.Node != null);
        OdfThumbnail thumbnail = document.Thumbnails.Single();

        ole.ClassId = "changed-class";
        ole.NotifyOnUpdateOfRanges = "Sheet1.A1:Sheet1.B2";
        ole.Data = [9, 8, 7, 6];
        chart.ClassName = "chart:line";
        chart.StyleName = "chart-style";
        chart.Width = "12cm";
        thumbnail.Data = PngData(9);

        OdfDocument reread = ReadPackage(Write(document));
        OdfEmbeddedObject changedOle = reread.EmbeddedObjects.Single(item =>
            item.Kind == OdfEmbeddedObjectKind.Ole);
        OdfChart changedChart = reread.Charts.Single(item =>
            item.Node != null);
        OdfThumbnail changedThumbnail = reread.Thumbnails.Single();

        Assert.Multiple(() =>
        {
            Assert.That(changedOle.ClassId, Is.EqualTo("changed-class"));
            Assert.That(changedOle.NotifyOnUpdateOfRanges,
                Is.EqualTo("Sheet1.A1:Sheet1.B2"));
            Assert.That(changedOle.Data, Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
            Assert.That(changedChart.Kind, Is.EqualTo(OdfChartKind.Line));
            Assert.That(changedChart.StyleName, Is.EqualTo("chart-style"));
            Assert.That(changedChart.Width, Is.EqualTo("12cm"));
            Assert.That(changedThumbnail.Data, Is.EqualTo(PngData(9)));
            Assert.That(
                EmbeddedContentManager.ExtractEmbeddedObjectData(
                    reread,
                    changedOle),
                Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
            Assert.That(
                EmbeddedContentManager.ExtractThumbnailData(changedThumbnail),
                Is.EqualTo(PngData(9)));
        });
    }

    [Test]
    public void ExtractsInlineOleAndNeverFetchesExternalObjects()
    {
        OdfDocument document = ReadFlat(
            "<draw:frame draw:name='Inline'>"
            + "<draw:object-ole draw:class-id='inline'>"
            + "<office:binary-data>AQIDBA==</office:binary-data>"
            + "</draw:object-ole></draw:frame>"
            + "<draw:frame draw:name='External'>"
            + "<draw:object-ole xlink:href='https://example.com/object' "
            + "xlink:type='simple'/></draw:frame>");
        OdfEmbeddedObject inline = document.EmbeddedObjects.Single(item =>
            item.SourceKind == OdfResourceSourceKind.Inline);
        OdfEmbeddedObject external = document.EmbeddedObjects.Single(item =>
            item.SourceKind == OdfResourceSourceKind.External);

        Assert.Multiple(() =>
        {
            Assert.That(
                EmbeddedContentManager.ExtractEmbeddedObjectData(
                    document,
                    inline),
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(external.Data, Is.Empty);
            Assert.Throws<OdfEditException>(() =>
                EmbeddedContentManager.ExtractEmbeddedObjectData(
                    document,
                    external));
        });
    }

    static OdfDocument ReadPackage(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "embedded.odp",
            new OdfLoadOptions());
    }

    static OdfDocument ReadFlat(string content)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Graphics + "'>"
            + "<office:body><office:drawing><draw:page draw:name='Page'>"
            + content
            + "</draw:page></office:drawing></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "embedded.fodg",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static byte[] CreatePackage()
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(OdfMediaTypes.Presentation),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                Encoding.UTF8.GetBytes(ContentXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(ManifestXml()),
                CompressionLevel.Optimal);
            archive.CreateEntry("Object1/", CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                "Object1/content.xml",
                Encoding.UTF8.GetBytes(ChartContentXml()),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Object2.bin",
                OleData(),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "Formula/math.xml",
                Encoding.UTF8.GetBytes(
                    "<math:math xmlns:math='" + OdfNamespaces.Math
                    + "'><math:mi>y</math:mi></math:math>"),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                "ObjectReplacements/Object1.png",
                PngData(1),
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Thumbnail,
                PngData(2),
                CompressionLevel.Optimal);
        }
        return output.ToArray();
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] data,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream stream = entry.Open();
        stream.Write(data, 0, data.Length);
    }

    static string ContentXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:math='" + OdfNamespaces.Math + "'"
            + " xmlns:chart='" + OdfNamespaces.Chart + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " office:version='1.4'>"
            + "<office:body><office:presentation>"
            + "<draw:page draw:name='Page'>"
            + "<draw:frame draw:name='Packaged Chart'>"
            + "<draw:object xlink:href='./Object1' xlink:type='simple' "
            + "draw:notify-on-update-of-ranges='Sheet1.A1'/>"
            + "<draw:image xlink:href='ObjectReplacements/Object1.png' "
            + "xlink:type='simple' draw:mime-type='image/png'/>"
            + "</draw:frame>"
            + "<draw:frame draw:name='OLE'>"
            + "<draw:object-ole xlink:href='Object2.bin' "
            + "xlink:type='simple' draw:class-id='ole-class'/>"
            + "</draw:frame>"
            + "<draw:frame draw:name='Math'>"
            + "<draw:object><math:math><math:mi>x</math:mi>"
            + "</math:math></draw:object></draw:frame>"
            + "<draw:frame draw:name='Inline Chart'>"
            + "<draw:object><office:document office:version='1.4' "
            + "office:mimetype='" + OdfMediaTypes.Chart + "'>"
            + "<office:body><office:chart>"
            + "<chart:chart chart:class='chart:circle' svg:width='8cm' "
            + "svg:height='6cm'><chart:plot-area/></chart:chart>"
            + "</office:chart></office:body></office:document>"
            + "</draw:object></draw:frame>"
            + "</draw:page></office:presentation></office:body>"
            + "</office:document-content>";
    }

    static string ChartContentXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document-content xmlns:office='"
            + OdfNamespaces.Office + "' xmlns:chart='"
            + OdfNamespaces.Chart + "' office:version='1.4'>"
            + "<office:body><office:chart>"
            + "<chart:chart chart:class='chart:bar'>"
            + "<chart:plot-area/></chart:chart>"
            + "</office:chart></office:body></office:document-content>";
    }

    static string ManifestXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<manifest:manifest xmlns:manifest='"
            + OdfNamespaces.Manifest + "' manifest:version='1.4'>"
            + Entry("/", OdfMediaTypes.Presentation)
            + Entry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + Entry("Object1/", OdfMediaTypes.Chart)
            + Entry("Object1/content.xml", OdfMediaTypes.Xml)
            + Entry("Object2.bin", OdfMediaTypes.OctetStream)
            + Entry("Formula/math.xml", OdfMediaTypes.MathMl)
            + Entry("ObjectReplacements/Object1.png", OdfMediaTypes.Png)
            + Entry(OdfPartNames.Thumbnail, OdfMediaTypes.Png)
            + "</manifest:manifest>";
    }

    static string Entry(string path, string mediaType)
    {
        return "<manifest:file-entry manifest:full-path='" + path
            + "' manifest:media-type='" + mediaType + "'/>";
    }

    static byte[] OleData()
    {
        return [208, 207, 17, 224, 161, 177, 26, 225];
    }

    static byte[] PngData(byte marker)
    {
        return [137, 80, 78, 71, 13, 10, 26, 10, marker];
    }
}
