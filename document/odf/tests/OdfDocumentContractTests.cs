using System.Text;

namespace odf.Tests;

public class OdfDocumentContractTests
{
    [TestCase(OdfPackageKind.Package)]
    [TestCase(OdfPackageKind.Flat)]
    public void StreamMethodsRoundTripBothRepresentationsAndHonorOwnership(
        OdfPackageKind packageKind)
    {
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        OdfDocument source = packageKind == OdfPackageKind.Package
            ? factory.CreatePackageDocument(OdfDocumentKind.TextTemplate)
            : factory.CreateFlatDocument(OdfDocumentKind.TextTemplate);
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        TrackingOutputStream output = new TrackingOutputStream();
        writer.WriteDocumentStream(
            source,
            output,
            new OdfSaveOptions
            {
                LeaveOutputOpen = true,
                Deterministic = true
            });
        TrackingInputStream input = new TrackingInputStream(output.ToArray());
        IOdfDocumentReader reader = new OdfDocumentReader();

        OdfDocument roundTrip = reader.ReadDocumentStream(
            input,
            packageKind == OdfPackageKind.Package
                ? "template.ott"
                : "template.fodt",
            new OdfLoadOptions
            {
                LeaveInputOpen = false
            });

        Assert.Multiple(() =>
        {
            Assert.That(output.WasDisposed, Is.False);
            Assert.That(output.CanWrite, Is.True);
            Assert.That(input.WasDisposed, Is.True);
            Assert.That(roundTrip.PackageKind, Is.EqualTo(packageKind));
            Assert.That(roundTrip.Kind,
                Is.EqualTo(OdfDocumentKind.TextTemplate));
            Assert.That(roundTrip.MediaType,
                Is.EqualTo(OdfMediaTypes.TextTemplate));
            Assert.That(roundTrip.Parts, Has.Length.EqualTo(1));
        });
    }

    [TestCase(OdfPackageKind.Package)]
    [TestCase(OdfPackageKind.Flat)]
    public void PathMethodsRoundTripBothRepresentations(
        OdfPackageKind packageKind)
    {
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        OdfDocument source = packageKind == OdfPackageKind.Package
            ? factory.CreatePackageDocument(OdfDocumentKind.DrawingTemplate)
            : factory.CreateFlatDocument(OdfDocumentKind.DrawingTemplate);
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        IOdfDocumentReader reader = new OdfDocumentReader();
        string path = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N")
                + (packageKind == OdfPackageKind.Package ? ".otg" : ".fodg"));
        try
        {
            writer.WriteDocumentPath(
                source,
                path,
                new OdfSaveOptions
                {
                    Deterministic = true
                });
            OdfDocument roundTrip = reader.ReadDocumentPath(
                path,
                new OdfLoadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(new FileInfo(path).Length, Is.GreaterThan(0));
                Assert.That(roundTrip.PackageKind, Is.EqualTo(packageKind));
                Assert.That(roundTrip.Kind,
                    Is.EqualTo(OdfDocumentKind.DrawingTemplate));
                Assert.That(roundTrip.MediaType,
                    Is.EqualTo(OdfMediaTypes.GraphicsTemplate));
                Assert.That(BodyChildName(roundTrip), Is.EqualTo("drawing"));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestCase("sample.fodt", "text", OdfMediaTypes.Text, OdfDocumentKind.Text)]
    [TestCase("sample.fods", "spreadsheet", OdfMediaTypes.Spreadsheet, OdfDocumentKind.Spreadsheet)]
    [TestCase("sample.fodp", "presentation", OdfMediaTypes.Presentation, OdfDocumentKind.Presentation)]
    [TestCase("sample.fodg", "drawing", OdfMediaTypes.Graphics, OdfDocumentKind.Drawing)]
    public void ByteReaderInfersFlatMediaTypeFromSourceName(
        string sourceName,
        string bodyName,
        string mediaType,
        OdfDocumentKind kind)
    {
        string xml = "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' office:version='1.4'><office:body><office:"
            + bodyName
            + "/></office:body></office:document>";
        IOdfDocumentReader reader = new OdfDocumentReader();

        OdfDocument document = reader.ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            sourceName,
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.PackageKind, Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(document.Kind, Is.EqualTo(kind));
            Assert.That(document.MediaType, Is.EqualTo(mediaType));
            Assert.That(document.Parts[0].MediaType, Is.EqualTo(mediaType));
            Assert.That(BodyChildName(document), Is.EqualTo(bodyName));
        });
    }

    [Test]
    public void DatabaseFactoryDocumentsRoundTripThroughBytes()
    {
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        IOdfDocumentReader reader = new OdfDocumentReader();
        OdfDocument package = factory.CreatePackageDocument(
            OdfDocumentKind.Database);
        OdfDocument flat = factory.CreateFlatDocument(
            OdfDocumentKind.Database);

        OdfDocument packageRoundTrip = reader.ReadDocumentBytes(
            writer.WriteDocumentBytes(package, new OdfSaveOptions()),
            "database.odb",
            new OdfLoadOptions());
        OdfDocument flatRoundTrip = reader.ReadDocumentBytes(
            writer.WriteDocumentBytes(flat, new OdfSaveOptions()),
            "database.xml",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            AssertDatabaseDocument(package, OdfPackageKind.Package);
            AssertDatabaseDocument(flat, OdfPackageKind.Flat);
            AssertDatabaseDocument(packageRoundTrip, OdfPackageKind.Package);
            AssertDatabaseDocument(flatRoundTrip, OdfPackageKind.Flat);
        });
    }

    [Test]
    public void StreamWriterCanOwnBothRepresentationOutputs()
    {
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        IOdfDocumentWriter writer = new OdfDocumentWriter();
        TrackingOutputStream packageOutput = new TrackingOutputStream();
        TrackingOutputStream flatOutput = new TrackingOutputStream();

        writer.WriteDocumentStream(
            factory.CreatePackageDocument(OdfDocumentKind.Spreadsheet),
            packageOutput,
            new OdfSaveOptions
            {
                LeaveOutputOpen = false
            });
        writer.WriteDocumentStream(
            factory.CreateFlatDocument(OdfDocumentKind.Spreadsheet),
            flatOutput,
            new OdfSaveOptions
            {
                LeaveOutputOpen = false
            });

        Assert.Multiple(() =>
        {
            Assert.That(packageOutput.WasDisposed, Is.True);
            Assert.That(flatOutput.WasDisposed, Is.True);
            Assert.That(packageOutput.ToArray(), Is.Not.Empty);
            Assert.That(flatOutput.ToArray(), Is.Not.Empty);
        });
    }

    static void AssertDatabaseDocument(
        OdfDocument document,
        OdfPackageKind packageKind)
    {
        Assert.That(document.PackageKind, Is.EqualTo(packageKind));
        Assert.That(document.Kind, Is.EqualTo(OdfDocumentKind.Database));
        Assert.That(document.MediaType, Is.EqualTo(OdfMediaTypes.Base));
        Assert.That(BodyChildName(document), Is.EqualTo("database"));
        Assert.That(document.Database, Is.Not.Null);
    }

    static string BodyChildName(OdfDocument document)
    {
        if (document.Body == null)
        {
            return "";
        }
        int index = 0;
        while (index < document.Body.Children.Length)
        {
            OdfNode node = document.Body.Children[index];
            if (node.Kind == OdfNodeKind.Element && node.Name != null)
            {
                return node.Name.LocalName;
            }
            index++;
        }
        return "";
    }
}
