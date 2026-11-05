using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class EmbeddedPackageWalkerTests
{
    [Test]
    public void TraversesDirectoryAndArchiveSubdocumentsRecursively()
    {
        byte[] outer = CreateNestedPackageGraph();
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(outer);

        EmbeddedPackageTraversal traversal = index.DiscoverEmbeddedPackages(
            new OdfSecurityLimits());
        EmbeddedPackageNode directory = FindNode(traversal.Packages, "Object1/");
        EmbeddedPackageNode subdirectory = FindNode(directory.Children, "Object1/Sub/");
        EmbeddedPackageNode archive = FindNode(
            traversal.Packages,
            "Embedded/child.odt");
        EmbeddedPackageNode grandchild = FindNode(
            archive.Children,
            "grandchild.ods");

        Assert.Multiple(() =>
        {
            Assert.That(traversal.IsComplete, Is.True);
            Assert.That(traversal.Diagnostics, Is.Empty);
            Assert.That(traversal.Packages, Has.Length.EqualTo(2));
            Assert.That(directory.IsDirectoryPackage, Is.True);
            Assert.That(directory.IsArchivePackage, Is.False);
            Assert.That(directory.IsValid, Is.True);
            Assert.That(directory.Depth, Is.EqualTo(1));
            Assert.That(directory.MediaType, Is.EqualTo(OdfMediaTypes.Spreadsheet));
            Assert.That(directory.Entries, Has.Length.EqualTo(6));
            Assert.That(subdirectory.Depth, Is.EqualTo(2));
            Assert.That(subdirectory.MediaType, Is.EqualTo(OdfMediaTypes.Formula));
            Assert.That(subdirectory.QualifiedPath, Is.EqualTo("Object1/Sub/"));
            Assert.That(archive.IsArchivePackage, Is.True);
            Assert.That(archive.IsValid, Is.True);
            Assert.That(archive.Depth, Is.EqualTo(1));
            Assert.That(archive.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(grandchild.IsValid, Is.True);
            Assert.That(grandchild.Depth, Is.EqualTo(2));
            Assert.That(grandchild.MediaType, Is.EqualTo(OdfMediaTypes.Spreadsheet));
            Assert.That(grandchild.QualifiedPath, Is.EqualTo("Embedded/child.odt!/grandchild.ods"));
        });
    }

    [Test]
    public void DepthLimitIncludesButDoesNotTraverseBlockedPackages()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateNestedPackageGraph());
        OdfSecurityLimits limits = new OdfSecurityLimits();
        limits.MaximumEmbeddedPackageDepth = 1;

        EmbeddedPackageTraversal traversal = index.DiscoverEmbeddedPackages(limits);
        EmbeddedPackageNode directory = FindNode(traversal.Packages, "Object1/");
        EmbeddedPackageNode blockedDirectory = FindNode(
            directory.Children,
            "Object1/Sub/");
        EmbeddedPackageNode archive = FindNode(
            traversal.Packages,
            "Embedded/child.odt");
        EmbeddedPackageNode blockedArchive = FindNode(
            archive.Children,
            "grandchild.ods");

        Assert.Multiple(() =>
        {
            Assert.That(traversal.IsComplete, Is.False);
            Assert.That(blockedDirectory.Depth, Is.EqualTo(2));
            Assert.That(blockedDirectory.TraversalStopped, Is.True);
            Assert.That(blockedDirectory.IsValid, Is.False);
            Assert.That(blockedDirectory.Children, Is.Empty);
            Assert.That(blockedArchive.Depth, Is.EqualTo(2));
            Assert.That(blockedArchive.TraversalStopped, Is.True);
            Assert.That(blockedArchive.IsValid, Is.False);
            Assert.That(blockedArchive.Children, Is.Empty);
            Assert.That(blockedArchive.Diagnostics[0].Code, Is.EqualTo(OdfDiagnosticCodes.InvalidPackage));
            Assert.That(blockedArchive.Diagnostics[0].Location.PartPath, Is.EqualTo("Embedded/child.odt!/grandchild.ods"));
            Assert.That(traversal.Diagnostics, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void NestedArchivesUseTheConfiguredPackageSecurityLimits()
    {
        byte[] child = CreateOdfPackage(
            OdfMediaTypes.Text,
            [OdfPartNames.Content],
            [OdfMediaTypes.Xml],
            [Encoding.UTF8.GetBytes("content")]);
        byte[] outer = CreateOdfPackage(
            OdfMediaTypes.Text,
            ["child.odt"],
            [OdfMediaTypes.Text],
            [child]);
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(outer);
        OdfSecurityLimits limits = new OdfSecurityLimits();
        limits.MaximumPackageEntries = 2;

        EmbeddedPackageTraversal traversal = index.DiscoverEmbeddedPackages(limits);
        EmbeddedPackageNode childNode = FindNode(traversal.Packages, "child.odt");

        Assert.Multiple(() =>
        {
            Assert.That(childNode.IsValid, Is.False);
            Assert.That(childNode.TraversalStopped, Is.True);
            Assert.That(childNode.Entries, Is.Empty);
            Assert.That(HasDiagnostic(childNode.Diagnostics, OdfDiagnosticCodes.EntryLimitExceeded), Is.True);
            Assert.That(traversal.IsComplete, Is.False);
            Assert.That(HasDiagnostic(traversal.Diagnostics, OdfDiagnosticCodes.EntryLimitExceeded), Is.True);
        });
    }

    [Test]
    public void MalformedDeclaredPackageDoesNotHideValidSiblingAndOrdinaryZipIsIgnored()
    {
        byte[] valid = CreateOdfPackage(
            OdfMediaTypes.Presentation,
            [],
            [],
            []);
        byte[] ordinaryZip = CreateOrdinaryZip();
        byte[] outer = CreateOdfPackage(
            OdfMediaTypes.Text,
            ["broken.odt", "valid.odp", "ordinary.zip"],
            [OdfMediaTypes.Text, OdfMediaTypes.Presentation, OdfMediaTypes.Zip],
            [Encoding.UTF8.GetBytes("not a zip"), valid, ordinaryZip]);
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(outer);

        EmbeddedPackageTraversal traversal = index.DiscoverEmbeddedPackages(
            new OdfSecurityLimits());
        EmbeddedPackageNode broken = FindNode(traversal.Packages, "broken.odt");
        EmbeddedPackageNode validNode = FindNode(traversal.Packages, "valid.odp");

        Assert.Multiple(() =>
        {
            Assert.That(traversal.Packages, Has.Length.EqualTo(2));
            Assert.That(broken.IsValid, Is.False);
            Assert.That(broken.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(broken.Diagnostics[0].Location.PartPath, Is.EqualTo("broken.odt"));
            Assert.That(validNode.IsValid, Is.True);
            Assert.That(HasNode(traversal.Packages, "ordinary.zip"), Is.False);
        });
    }

    [Test]
    public void NegativeDepthLimitStopsBeforeDiscovery()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateNestedPackageGraph());
        OdfSecurityLimits limits = new OdfSecurityLimits();
        limits.MaximumEmbeddedPackageDepth = -1;

        EmbeddedPackageTraversal traversal = index.DiscoverEmbeddedPackages(limits);

        Assert.Multiple(() =>
        {
            Assert.That(traversal.IsComplete, Is.False);
            Assert.That(traversal.Packages, Is.Empty);
            Assert.That(traversal.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(traversal.Diagnostics[0].Code, Is.EqualTo(OdfDiagnosticCodes.InvalidPackage));
        });
    }

    static byte[] CreateNestedPackageGraph()
    {
        byte[] grandchild = CreateOdfPackage(
            OdfMediaTypes.Spreadsheet,
            [],
            [],
            []);
        byte[] child = CreateOdfPackage(
            OdfMediaTypes.Text,
            [OdfPartNames.Content, "grandchild.ods"],
            [OdfMediaTypes.Xml, OdfMediaTypes.Spreadsheet],
            [Encoding.UTF8.GetBytes("content"), grandchild]);
        return CreateOdfPackage(
            OdfMediaTypes.Text,
            [
                "Object1/",
                "Object1/mimetype",
                "Object1/content.xml",
                "Object1/Sub/",
                "Object1/Sub/mimetype",
                "Object1/Sub/content.xml",
                "Embedded/child.odt"
            ],
            [
                OdfMediaTypes.Spreadsheet,
                "text/plain",
                OdfMediaTypes.Xml,
                OdfMediaTypes.Formula,
                "text/plain",
                OdfMediaTypes.Xml,
                OdfMediaTypes.Text
            ],
            [
                [],
                Encoding.UTF8.GetBytes(OdfMediaTypes.Spreadsheet),
                Encoding.UTF8.GetBytes("content"),
                [],
                Encoding.UTF8.GetBytes(OdfMediaTypes.Formula),
                Encoding.UTF8.GetBytes("content"),
                child
            ]);
    }

    static byte[] CreateOdfPackage(
        string mediaType,
        string[] paths,
        string[] mediaTypes,
        byte[][] contents)
    {
        string manifest = "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' m:version='1.4'>"
            + FileEntry(OdfPartNames.Root, mediaType);
        int manifestIndex = 0;
        while (manifestIndex < paths.Length)
        {
            manifest = manifest + FileEntry(
                paths[manifestIndex],
                mediaTypes[manifestIndex]);
            manifestIndex++;
        }
        manifest = manifest + "</m:manifest>";

        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(mediaType),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(manifest),
                CompressionLevel.SmallestSize);
            int index = 0;
            while (index < paths.Length)
            {
                if (paths[index].EndsWith("/", StringComparison.Ordinal))
                {
                    archive.CreateEntry(paths[index], CompressionLevel.NoCompression);
                }
                else
                {
                    WriteEntry(
                        archive,
                        paths[index],
                        contents[index],
                        CompressionLevel.SmallestSize);
                }
                index++;
            }
        }
        return output.ToArray();
    }

    static byte[] CreateOrdinaryZip()
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(
                archive,
                "data.txt",
                Encoding.UTF8.GetBytes("data"),
                CompressionLevel.SmallestSize);
        }
        return output.ToArray();
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] content,
        CompressionLevel compressionLevel)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compressionLevel);
        using Stream output = entry.Open();
        output.Write(content, 0, content.Length);
    }

    static string FileEntry(string path, string mediaType)
    {
        return "<m:file-entry m:full-path='"
            + path
            + "' m:media-type='"
            + mediaType
            + "'/>";
    }

    static EmbeddedPackageNode FindNode(
        EmbeddedPackageNode[] nodes,
        string path)
    {
        foreach (EmbeddedPackageNode node in nodes)
        {
            if (node.Path == path)
            {
                return node;
            }
        }
        Assert.Fail($"Expected embedded package '{path}'.");
        return new EmbeddedPackageNode();
    }

    static bool HasNode(EmbeddedPackageNode[] nodes, string path)
    {
        foreach (EmbeddedPackageNode node in nodes)
        {
            if (node.Path == path)
            {
                return true;
            }
        }
        return false;
    }

    static bool HasDiagnostic(OdfDiagnostic[] diagnostics, string code)
    {
        foreach (OdfDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }
        return false;
    }
}
