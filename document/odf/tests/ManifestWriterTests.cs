using System.Text;

namespace odf.Tests;

public class ManifestWriterTests
{
    [Test]
    public void PreservationModeReturnsTheExactOriginalXmlAndBytes()
    {
        string xml = "<?xml version='1.0' encoding='utf-8'?>\n"
            + "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' xmlns:ext='urn:extension' ext:flag='keep' m:version='1.4'>\n"
            + "  <!--retain this comment--><m:file-entry m:media-type='text/xml'"
            + " m:size='0007' m:full-path='content.xml'/>\n"
            + "</m:manifest>";
        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfSaveOptions options = new OdfSaveOptions();

        string written = ManifestWriter.WriteText(manifest, options);
        byte[] bytes = ManifestWriter.WriteBytes(manifest, options);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(xml));
            Assert.That(bytes, Is.EqualTo(new UTF8Encoding(false).GetBytes(xml)));
        });
    }

    [Test]
    public void StructuredWritingUsesCurrentKnownValuesAndRoundTripsThem()
    {
        string xml = ManifestStart("1.4")
            + "<m:file-entry m:full-path='content.xml' m:media-type='text/xml' m:size='0007'/>"
            + "</m:manifest>";
        OdfManifest manifest = ManifestParser.ParseText(xml);
        manifest.Entries[0].MediaType = "application/xml";
        manifest.Entries[0].Size = 9;
        manifest.Entries[0].SizeLexical = "0009";
        OdfSaveOptions options = new OdfSaveOptions();

        string written = ManifestWriter.WriteText(manifest, options);
        OdfManifest reparsed = ManifestParser.ParseText(written);

        Assert.Multiple(() =>
        {
            Assert.That(reparsed.IsValid, Is.True);
            Assert.That(reparsed.Version, Is.EqualTo("1.4"));
            Assert.That(reparsed.Entries[0].MediaType, Is.EqualTo("application/xml"));
            Assert.That(reparsed.Entries[0].Size, Is.EqualTo(9));
            Assert.That(reparsed.Entries[0].SizeLexical, Is.EqualTo("0009"));
        });
    }

    [Test]
    public void StructuredWritingCanPreserveOrCanonicalizeSizeLexicalValues()
    {
        string xml = ManifestStart("1.4")
            + "<m:file-entry m:full-path='content.xml' m:media-type='text/xml' m:size='0007'/>"
            + "</m:manifest>";
        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfSaveOptions preserving = new OdfSaveOptions();
        preserving.IndentXml = true;
        OdfSaveOptions canonical = new OdfSaveOptions();
        canonical.PreserveLexicalValues = false;

        string preserved = ManifestWriter.WriteText(manifest, preserving);
        string normalized = ManifestWriter.WriteText(manifest, canonical);

        Assert.Multiple(() =>
        {
            Assert.That(preserved, Does.Contain("manifest:size=\"0007\""));
            Assert.That(normalized, Does.Contain("manifest:size=\"7\""));
            Assert.That(preserved, Does.Contain("\n"));
        });
    }

    [Test]
    public void EntrySortingIsExplicitAndUsesOrdinalPackagePaths()
    {
        string xml = ManifestStart("1.4")
            + "<m:file-entry m:full-path='z.xml' m:media-type='text/xml'/>"
            + "<m:file-entry m:full-path='A.xml' m:media-type='text/xml'/>"
            + "<m:file-entry m:full-path='a.xml' m:media-type='text/xml'/>"
            + "</m:manifest>";
        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfSaveOptions sourceOrder = new OdfSaveOptions();
        sourceOrder.PreserveLexicalValues = false;
        OdfSaveOptions sorted = new OdfSaveOptions();
        sorted.SortManifestEntries = true;

        string unsortedXml = ManifestWriter.WriteText(manifest, sourceOrder);
        string sortedXml = ManifestWriter.WriteText(manifest, sorted);

        Assert.Multiple(() =>
        {
            Assert.That(unsortedXml.IndexOf("z.xml"), Is.LessThan(unsortedXml.IndexOf("A.xml")));
            Assert.That(sortedXml.IndexOf("A.xml"), Is.LessThan(sortedXml.IndexOf("a.xml")));
            Assert.That(sortedXml.IndexOf("a.xml"), Is.LessThan(sortedXml.IndexOf("z.xml")));
        });
    }

    [Test]
    public void DeterministicWritingSortsNamespacesAttributesAndEntriesStably()
    {
        string xml = "<m:manifest xmlns:z='urn:z' xmlns:a='urn:a' xmlns:m='"
            + OdfNamespaces.Manifest
            + "' z:last='z' a:first='a' m:version='1.4'>"
            + "<m:file-entry m:full-path='z.xml' m:media-type='text/xml'/>"
            + "<m:file-entry m:full-path='a.xml' m:media-type='text/xml'/>"
            + "</m:manifest>";
        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfSaveOptions options = new OdfSaveOptions();
        options.Deterministic = true;

        string first = ManifestWriter.WriteText(manifest, options);
        string second = ManifestWriter.WriteText(manifest, options);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.IndexOf("xmlns:a"), Is.LessThan(first.IndexOf("xmlns:z")));
            Assert.That(first.IndexOf("a:first"), Is.LessThan(first.IndexOf("z:last")));
            Assert.That(first.IndexOf("a.xml"), Is.LessThan(first.IndexOf("z.xml")));
        });
    }

    [Test]
    public void StructuredWritingRetainsForeignMixedContentAndEncryptionTrees()
    {
        string xml = ManifestStart("1.4")
            + "<ext:before>alpha<ext:nested ext:value='one'/>omega</ext:before>"
            + "<m:file-entry m:full-path='secret.bin' m:media-type='application/octet-stream'>"
            + "<m:encryption-data m:checksum-type='SHA256' m:checksum='Y2hlY2s='>"
            + "<m:algorithm m:algorithm-name='urn:algorithm' m:initialisation-vector='aXY='/>"
            + "</m:encryption-data><ext:payload>AAEC</ext:payload>"
            + "</m:file-entry><ext:after/>"
            + "</m:manifest>";
        OdfManifest manifest = ManifestParser.ParseText(xml);
        OdfSaveOptions options = new OdfSaveOptions();
        options.PreserveLexicalValues = false;

        string written = ManifestWriter.WriteText(manifest, options);
        OdfManifest reparsed = ManifestParser.ParseText(written);
        int alpha = written.IndexOf("alpha");
        int nested = written.IndexOf("ext:nested");
        int omega = written.IndexOf("omega");

        Assert.Multiple(() =>
        {
            Assert.That(alpha, Is.LessThan(nested));
            Assert.That(nested, Is.LessThan(omega));
            Assert.That(written.IndexOf("ext:before"), Is.LessThan(written.IndexOf("secret.bin")));
            Assert.That(written.IndexOf("secret.bin"), Is.LessThan(written.IndexOf("ext:after")));
            Assert.That(reparsed.IsValid, Is.True);
            Assert.That(reparsed.ForeignElements, Has.Length.EqualTo(2));
            Assert.That(reparsed.ForeignElements[0].TextContent, Is.EqualTo("alphaomega"));
            Assert.That(reparsed.Entries[0].EncryptionData, Is.Not.Null);
            Assert.That(reparsed.Entries[0].ForeignElements[0].TextContent, Is.EqualTo("AAEC"));
        });
    }

    static string ManifestStart(string version)
    {
        return "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' xmlns:ext='urn:extension' m:version='"
            + version
            + "'>";
    }
}
