using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PackageRoundTripTests
{
    [Test]
    public void OpenSaveOpen_PreservesContentTypeManifestLogically()
    {
        byte[] originalBytes = CreateFixturePackageBytes();
        PptxPackage opened = PptxPackage.OpenBytes(originalBytes);

        PptxPackage reopened = PptxPackage.OpenBytes(opened.SaveBytes());

        Assert.That(opened.Succeeded(), Is.True, opened.Diagnostics.FormatDiagnostics());
        Assert.That(reopened.Succeeded(), Is.True, reopened.Diagnostics.FormatDiagnostics());
        AssertManifestEquivalent(opened.ContentTypes, reopened.ContentTypes);
    }

    [Test]
    public void OpenSaveOpen_PreservesUnknownPartBytes()
    {
        byte[] originalBytes = CreateFixturePackageBytes();
        PptxPackage opened = PptxPackage.OpenBytes(originalBytes);
        byte[] expectedCustomBytes = opened.GetPart("/custom/data/unknown.bin").GetBytes();
        byte[] expectedExtensionBytes = opened.GetPart("/ppt/extensions/opaque.dat").GetBytes();

        PptxPackage reopened = PptxPackage.OpenBytes(opened.SaveBytes());

        Assert.That(reopened.GetPart("/custom/data/unknown.bin").Bytes, Is.EqualTo(expectedCustomBytes));
        Assert.That(reopened.GetPart("/ppt/extensions/opaque.dat").Bytes, Is.EqualTo(expectedExtensionBytes));
    }

    [Test]
    public void OpenSaveOpen_PreservesUnknownRelationships()
    {
        byte[] originalBytes = CreateFixturePackageBytes();
        PptxPackage reopened = PptxPackage.OpenBytes(PptxPackage.OpenBytes(originalBytes).SaveBytes());

        Assert.That(reopened.RootRelationships.Count(), Is.EqualTo(2));
        Assert.That(reopened.RootRelationships.Relationships[1].Type, Is.EqualTo("http://example.com/relationships/custom-root"));
        Assert.That(reopened.RootRelationships.Relationships[1].TargetPartName, Is.EqualTo("/custom/data/unknown.bin"));
        Assert.That(reopened.GetPart("/custom/data/unknown.bin").Relationships.Count(), Is.EqualTo(1));
        Assert.That(reopened.GetPart("/custom/data/unknown.bin").Relationships.Relationships[0].Type, Is.EqualTo("http://example.com/relationships/external-link"));
        Assert.That(reopened.GetPart("/custom/data/unknown.bin").Relationships.Relationships[0].TargetMode, Is.EqualTo(PptxRelationshipTargetMode.External));
        Assert.That(reopened.GetPart("/custom/data/unknown.bin").Relationships.Relationships[0].Target, Is.EqualTo("https://example.com/blob"));
    }

    [Test]
    public void OpenSaveOpen_PreservesManifestAndUnknownBytesAcrossRepeatedSaves()
    {
        PptxPackage first = PptxPackage.OpenBytes(CreateFixturePackageBytes());
        PptxPackage second = PptxPackage.OpenBytes(first.SaveBytes());
        PptxPackage third = PptxPackage.OpenBytes(second.SaveBytes());

        AssertManifestEquivalent(first.ContentTypes, third.ContentTypes);
        Assert.That(third.GetPart("/custom/data/unknown.bin").Bytes, Is.EqualTo(first.GetPart("/custom/data/unknown.bin").Bytes));
        Assert.That(third.GetPart("/ppt/extensions/opaque.dat").Bytes, Is.EqualTo(first.GetPart("/ppt/extensions/opaque.dat").Bytes));
    }

    static void AssertManifestEquivalent(PptxContentTypeManifest expected, PptxContentTypeManifest actual)
    {
        Assert.That(actual.DefaultCount(), Is.EqualTo(expected.DefaultCount()));
        Assert.That(actual.OverrideCount(), Is.EqualTo(expected.OverrideCount()));

        for (int index = 0; index < expected.Defaults.Count; index++)
        {
            PptxContentTypeDefault item = expected.Defaults[index];
            Assert.That(actual.GetDefaultContentType(item.Extension), Is.EqualTo(item.ContentType));
        }

        for (int index = 0; index < expected.Overrides.Count; index++)
        {
            PptxContentTypeOverride item = expected.Overrides[index];
            Assert.That(actual.GetOverrideContentType(item.PartName), Is.EqualTo(item.ContentType));
        }
    }

    static byte[] CreateFixturePackageBytes()
    {
        List<PptxZipEntry> entries = new List<PptxZipEntry>();
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        manifest.AddDefault("xml", PptxContentTypes.Xml);
        manifest.AddDefault("rels", PptxContentTypes.Relationships);
        manifest.AddDefault("bin", PptxContentTypes.Binary);
        manifest.AddDefault("dat", "application/vnd.example.opaque");
        manifest.AddOverride("/ppt/presentation.xml", PptxContentTypes.Presentation);
        manifest.AddOverride("/custom/data/unknown.bin", "application/vnd.example.unknown");
        manifest.AddOverride("/ppt/extensions/opaque.dat", "application/vnd.example.opaque");

        entries.Add(new PptxZipEntry(PptxPartNames.ContentTypesEntry, "", PptxContentTypesWriter.WriteBytes(manifest), 0, 0));

        PptxRelationshipCollection rootRelationships = new PptxRelationshipCollection("");
        rootRelationships.Add(new PptxRelationship(
            "",
            "rId2",
            "http://example.com/relationships/custom-root",
            "custom/data/unknown.bin",
            PptxRelationshipTargetMode.Internal,
            "/custom/data/unknown.bin",
            ""));
        rootRelationships.Add(new PptxRelationship(
            "",
            "rId1",
            PptxRelationshipTypes.OfficeDocument,
            "ppt/presentation.xml",
            PptxRelationshipTargetMode.Internal,
            "/ppt/presentation.xml",
            ""));
        entries.Add(new PptxZipEntry("_rels/.rels", "/_rels/.rels", PptxRelationshipsWriter.WriteBytes(rootRelationships), 0, 0));

        PptxRelationshipCollection customRelationships = new PptxRelationshipCollection("/custom/data/unknown.bin");
        customRelationships.Add(new PptxRelationship(
            "/custom/data/unknown.bin",
            "rId1",
            "http://example.com/relationships/external-link",
            "https://example.com/blob",
            PptxRelationshipTargetMode.External,
            "",
            ""));
        entries.Add(new PptxZipEntry("custom/data/_rels/unknown.bin.rels", "/custom/data/_rels/unknown.bin.rels", PptxRelationshipsWriter.WriteBytes(customRelationships), 0, 0));

        entries.Add(CreateTextEntry("ppt/presentation.xml", "<p:presentation/>"));
        entries.Add(new PptxZipEntry("custom/data/unknown.bin", "/custom/data/unknown.bin", new byte[] { 0, 1, 2, 3, 254, 255 }, 6, 0));
        entries.Add(new PptxZipEntry("ppt/extensions/opaque.dat", "/ppt/extensions/opaque.dat", Encoding.UTF8.GetBytes("opaque-extension-data"), 21, 0));

        using MemoryStream stream = new MemoryStream();
        PptxResult result = new PptxZipWriter().Write(stream, entries, PptxZipWriteOptions.CreateDefault());
        Assert.That(result.Succeeded(), Is.True, result.FormatDiagnostics());
        return stream.ToArray();
    }

    static PptxZipEntry CreateTextEntry(string name, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return new PptxZipEntry(name, PptxZipReader.GetPartNameForEntry(name), bytes, bytes.Length, 0);
    }
}
