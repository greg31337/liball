using System.IO.Compression;
using System.Text;
using System.Xml;

namespace odf.Tests;

public class PackageSecurityContractTests
{
    [Test]
    public void PublicReaderEnforcesEveryZipBombLimitBeforeUnsafeExpansion()
    {
        IOdfPackageReader reader = new OdfPackageReader();
        byte[] twoEntries = CreateArchive(
            ["first.txt", "second.txt"],
            [Encoding.UTF8.GetBytes("1234"), Encoding.UTF8.GetBytes("5678")],
            [CompressionLevel.NoCompression, CompressionLevel.NoCompression]);

        OdfLoadOptions entryOptions = CreatePermissiveOptions();
        entryOptions.SecurityLimits.MaximumPackageEntries = 1;
        OdfPackage entryLimited = reader.ReadPackageBytes(twoEntries, entryOptions);

        OdfLoadOptions totalOptions = CreatePermissiveOptions();
        totalOptions.SecurityLimits.MaximumTotalExpandedBytes = 7;
        OdfPackage totalLimited = reader.ReadPackageBytes(twoEntries, totalOptions);

        OdfLoadOptions singleOptions = CreatePermissiveOptions();
        singleOptions.SecurityLimits.MaximumSingleEntryBytes = 3;
        OdfPackage singleLimited = reader.ReadPackageBytes(twoEntries, singleOptions);

        byte[] compressed = CreateArchive(
            ["compressed.txt"],
            [Encoding.UTF8.GetBytes(new string('x', 8192))],
            [CompressionLevel.SmallestSize]);
        OdfLoadOptions ratioOptions = CreatePermissiveOptions();
        ratioOptions.SecurityLimits.MaximumCompressionRatio = 2;
        OdfPackage ratioLimited = reader.ReadPackageBytes(compressed, ratioOptions);

        int centralOffset = FindSignature(compressed, 33639248);
        WriteUInt32(compressed, centralOffset + 24, 1);
        OdfLoadOptions actualOptions = CreatePermissiveOptions();
        actualOptions.SecurityLimits.MaximumSingleEntryBytes = 100;
        actualOptions.SecurityLimits.MaximumTotalExpandedBytes = 100;
        OdfPackage actualLimited = reader.ReadPackageBytes(compressed, actualOptions);

        Assert.Multiple(() =>
        {
            Assert.That(entryLimited.Entries, Is.Empty);
            Assert.That(HasDiagnostic(entryLimited, OdfDiagnosticCodes.EntryLimitExceeded), Is.True);
            Assert.That(FindEntry(totalLimited, "second.txt").HasValidCrc, Is.False);
            Assert.That(HasDiagnostic(totalLimited, OdfDiagnosticCodes.ExpandedSizeLimitExceeded), Is.True);
            Assert.That(FindEntry(singleLimited, "first.txt").HasValidCrc, Is.False);
            Assert.That(HasDiagnostic(singleLimited, OdfDiagnosticCodes.ExpandedSizeLimitExceeded), Is.True);
            Assert.That(FindEntry(ratioLimited, "compressed.txt").HasValidCrc, Is.False);
            Assert.That(HasDiagnostic(ratioLimited, OdfDiagnosticCodes.CompressionRatioExceeded), Is.True);
            Assert.That(FindEntry(actualLimited, "compressed.txt").HasValidCrc, Is.False);
            Assert.That(HasDiagnostic(actualLimited, OdfDiagnosticCodes.ExpandedSizeLimitExceeded), Is.True);
        });
    }

    [Test]
    public void PublicReaderRejectsDuplicatesCollisionsAndTraversalPaths()
    {
        IOdfPackageReader reader = new OdfPackageReader();
        OdfPackage duplicate = reader.ReadPackageBytes(
            CreateArchive(
                ["same.txt", "same.txt"],
                [Encoding.UTF8.GetBytes("one"), Encoding.UTF8.GetBytes("two")],
                [CompressionLevel.NoCompression, CompressionLevel.NoCompression]),
            CreatePermissiveOptions());
        OdfPackage collision = reader.ReadPackageBytes(
            CreateArchive(
                ["folder/item.txt", "folder/./item.txt"],
                [Encoding.UTF8.GetBytes("one"), Encoding.UTF8.GetBytes("two")],
                [CompressionLevel.NoCompression, CompressionLevel.NoCompression]),
            CreatePermissiveOptions());
        OdfPackage unicodeCollision = reader.ReadPackageBytes(
            CreateArchive(
                ["Pictures/café.txt", "Pictures/café.txt"],
                [Encoding.UTF8.GetBytes("one"), Encoding.UTF8.GetBytes("two")],
                [CompressionLevel.NoCompression, CompressionLevel.NoCompression]),
            CreatePermissiveOptions());
        OdfPackage traversal = reader.ReadPackageBytes(
            CreateArchive(
                ["../outside.txt", "%2e%2e/encoded.txt"],
                [Encoding.UTF8.GetBytes("one"), Encoding.UTF8.GetBytes("two")],
                [CompressionLevel.NoCompression, CompressionLevel.NoCompression]),
            CreatePermissiveOptions());

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.IsValid, Is.False);
            Assert.That(HasDiagnostic(duplicate, OdfDiagnosticCodes.DuplicateEntry), Is.True);
            Assert.That(collision.IsValid, Is.False);
            Assert.That(HasDiagnostic(collision, OdfDiagnosticCodes.PathNormalizationCollision), Is.True);
            Assert.That(unicodeCollision.IsValid, Is.False);
            Assert.That(HasDiagnostic(unicodeCollision, OdfDiagnosticCodes.PathNormalizationCollision), Is.True);
            Assert.That(traversal.IsValid, Is.False);
            Assert.That(HasDiagnostic(traversal, OdfDiagnosticCodes.PathTraversal), Is.True);
            Assert.That(FindEntry(traversal, "../outside.txt").HasValidCrc, Is.False);
            Assert.That(FindEntry(traversal, "%2e%2e/encoded.txt").HasValidCrc, Is.False);
        });
    }

    [Test]
    public void PublicReaderReportsCrcFailureAndKeepsCorruptContentUnavailable()
    {
        byte[] archive = CreateArchive(
            ["content.xml"],
            [Encoding.UTF8.GetBytes("content")],
            [CompressionLevel.NoCompression]);
        int centralOffset = FindSignature(archive, 33639248);
        archive[centralOffset + 16] = (byte)(archive[centralOffset + 16] ^ 255);
        IOdfPackageReader reader = new OdfPackageReader();

        OdfPackage package = reader.ReadPackageBytes(
            archive,
            CreatePermissiveOptions());

        Assert.Multiple(() =>
        {
            Assert.That(package.IsValid, Is.False);
            Assert.That(HasDiagnostic(package, OdfDiagnosticCodes.CrcMismatch), Is.True);
            Assert.That(FindEntry(package, "content.xml").HasValidCrc, Is.False);
            Assert.That(
                () => reader.OpenPackageEntry(package, "content.xml"),
                Throws.TypeOf<OdfPackageException>());
        });
    }

    [Test]
    public void RealEmbeddedPackagesStopAtTheConfiguredDepth()
    {
        byte[] leaf = CreateOdfPackage([], [], []);
        byte[] grandchild = CreateOdfPackage(
            ["leaf.odt"],
            [OdfMediaTypes.Text],
            [leaf]);
        byte[] child = CreateOdfPackage(
            ["grandchild.odt"],
            [OdfMediaTypes.Text],
            [grandchild]);
        byte[] outer = CreateOdfPackage(
            ["child.odt"],
            [OdfMediaTypes.Text],
            [child]);
        IOdfPackageReader reader = new OdfPackageReader();
        OdfPackage package = reader.ReadPackageBytes(
            outer,
            CreatePermissiveOptions());
        OdfSecurityLimits limits = CreatePermissiveOptions().SecurityLimits;
        limits.MaximumEmbeddedPackageDepth = 2;

        EmbeddedPackageTraversal traversal = package.EntryIndex!.DiscoverEmbeddedPackages(limits);
        EmbeddedPackageNode childNode = FindNode(traversal.Packages, "child.odt");
        EmbeddedPackageNode grandchildNode = FindNode(childNode.Children, "grandchild.odt");
        EmbeddedPackageNode leafNode = FindNode(grandchildNode.Children, "leaf.odt");

        Assert.Multiple(() =>
        {
            Assert.That(package.IsValid, Is.True);
            Assert.That(childNode.IsValid, Is.True);
            Assert.That(childNode.Depth, Is.EqualTo(1));
            Assert.That(grandchildNode.IsValid, Is.True);
            Assert.That(grandchildNode.Depth, Is.EqualTo(2));
            Assert.That(leafNode.IsValid, Is.False);
            Assert.That(leafNode.Depth, Is.EqualTo(3));
            Assert.That(leafNode.TraversalStopped, Is.True);
            Assert.That(leafNode.QualifiedPath, Is.EqualTo("child.odt!/grandchild.odt!/leaf.odt"));
            Assert.That(HasDiagnostic(leafNode.Diagnostics, OdfDiagnosticCodes.InvalidPackage), Is.True);
            Assert.That(traversal.IsComplete, Is.False);
        });
    }

    [Test]
    public void PublicReaderDiagnosesMalformedManifestModelsAndRejectsMalformedXml()
    {
        IOdfPackageReader reader = new OdfPackageReader();
        OdfPackage wrongRoot = reader.ReadPackageBytes(
            CreatePackageWithManifest("<wrong/>"),
            CreatePermissiveOptions());
        OdfPackage missingVersion = reader.ReadPackageBytes(
            CreatePackageWithManifest(
                "<m:manifest xmlns:m='"
                + OdfNamespaces.Manifest
                + "'><m:file-entry m:full-path='/' m:media-type='"
                + OdfMediaTypes.Text
                + "'/></m:manifest>"),
            CreatePermissiveOptions());
        OdfPackage unsupportedVersion = reader.ReadPackageBytes(
            CreatePackageWithManifest(
                "<m:manifest xmlns:m='"
                + OdfNamespaces.Manifest
                + "' m:version='9.9'><m:file-entry m:full-path='/' m:media-type='"
                + OdfMediaTypes.Text
                + "'/></m:manifest>"),
            CreatePermissiveOptions());
        byte[] malformedXml = CreatePackageWithManifest(
            "<m:manifest xmlns:m='" + OdfNamespaces.Manifest + "'>");

        Assert.Multiple(() =>
        {
            Assert.That(wrongRoot.IsValid, Is.False);
            Assert.That(wrongRoot.Manifest, Is.Not.Null);
            Assert.That(HasDiagnostic(wrongRoot, OdfDiagnosticCodes.UnexpectedElement), Is.True);
            Assert.That(missingVersion.IsValid, Is.False);
            Assert.That(HasDiagnostic(missingVersion, OdfDiagnosticCodes.MissingRequiredContent), Is.True);
            Assert.That(unsupportedVersion.IsValid, Is.False);
            Assert.That(HasDiagnostic(unsupportedVersion, OdfDiagnosticCodes.VersionUnsupported), Is.True);
            Assert.That(
                () => reader.ReadPackageBytes(malformedXml, CreatePermissiveOptions()),
                Throws.TypeOf<XmlException>());
        });
    }

    [Test]
    public void SecurityFailureHonorsInputStreamOwnership()
    {
        byte[] archive = CreateArchive(
            ["../outside.txt"],
            [Encoding.UTF8.GetBytes("content")],
            [CompressionLevel.NoCompression]);
        IOdfPackageReader reader = new OdfPackageReader();
        TrackingInputStream keptInput = new TrackingInputStream(archive);
        OdfLoadOptions keptOptions = CreatePermissiveOptions();
        keptOptions.LeaveInputOpen = true;
        OdfPackage keptPackage = reader.ReadPackageStream(keptInput, keptOptions);
        TrackingInputStream closedInput = new TrackingInputStream(archive);
        OdfLoadOptions closedOptions = CreatePermissiveOptions();
        closedOptions.LeaveInputOpen = false;
        OdfPackage closedPackage = reader.ReadPackageStream(closedInput, closedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(keptPackage.IsValid, Is.False);
            Assert.That(closedPackage.IsValid, Is.False);
            Assert.That(HasDiagnostic(keptPackage, OdfDiagnosticCodes.PathTraversal), Is.True);
            Assert.That(HasDiagnostic(closedPackage, OdfDiagnosticCodes.PathTraversal), Is.True);
            Assert.That(keptInput.WasDisposed, Is.False);
            Assert.That(keptInput.CanRead, Is.True);
            Assert.That(closedInput.WasDisposed, Is.True);
        });
        keptInput.Dispose();
    }

    static OdfLoadOptions CreatePermissiveOptions()
    {
        OdfLoadOptions options = new OdfLoadOptions();
        options.SecurityLimits.MaximumPackageEntries = 100;
        options.SecurityLimits.MaximumTotalExpandedBytes = 1000000;
        options.SecurityLimits.MaximumSingleEntryBytes = 1000000;
        options.SecurityLimits.MaximumCompressionRatio = 10000;
        options.SecurityLimits.MaximumPathLength = 1000;
        options.SecurityLimits.MaximumEmbeddedPackageDepth = 10;
        return options;
    }

    static byte[] CreateArchive(
        string[] paths,
        byte[][] contents,
        CompressionLevel[] compressionLevels)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            int index = 0;
            while (index < paths.Length)
            {
                WriteEntry(
                    archive,
                    paths[index],
                    contents[index],
                    compressionLevels[index]);
                index++;
            }
        }
        return output.ToArray();
    }

    static byte[] CreateOdfPackage(
        string[] paths,
        string[] mediaTypes,
        byte[][] contents)
    {
        string manifest = "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' m:version='1.4'><m:file-entry m:full-path='/' m:media-type='"
            + OdfMediaTypes.Text
            + "'/>";
        int manifestIndex = 0;
        while (manifestIndex < paths.Length)
        {
            manifest = manifest
                + "<m:file-entry m:full-path='"
                + paths[manifestIndex]
                + "' m:media-type='"
                + mediaTypes[manifestIndex]
                + "'/>";
            manifestIndex++;
        }
        manifest = manifest + "</m:manifest>";
        return CreatePackageWithManifestAndEntries(manifest, paths, contents);
    }

    static byte[] CreatePackageWithManifest(string manifest)
    {
        return CreatePackageWithManifestAndEntries(manifest, [], []);
    }

    static byte[] CreatePackageWithManifestAndEntries(
        string manifest,
        string[] paths,
        byte[][] contents)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(OdfMediaTypes.Text),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(manifest),
                CompressionLevel.SmallestSize);
            int index = 0;
            while (index < paths.Length)
            {
                WriteEntry(
                    archive,
                    paths[index],
                    contents[index],
                    CompressionLevel.SmallestSize);
                index++;
            }
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
        using Stream stream = entry.Open();
        stream.Write(content, 0, content.Length);
    }

    static OdfPackageEntry FindEntry(OdfPackage package, string path)
    {
        foreach (OdfPackageEntry entry in package.Entries)
        {
            if (entry.Path == path)
            {
                return entry;
            }
        }
        Assert.Fail($"Expected package entry '{path}'.");
        return new OdfPackageEntry();
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

    static bool HasDiagnostic(OdfPackage package, string code)
    {
        return HasDiagnostic(package.Diagnostics, code);
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

    static int FindSignature(byte[] data, uint signature)
    {
        int offset = 0;
        while (offset <= data.Length - 4)
        {
            uint value = (uint)data[offset]
                | ((uint)data[offset + 1] << 8)
                | ((uint)data[offset + 2] << 16)
                | ((uint)data[offset + 3] << 24);
            if (value == signature)
            {
                return offset;
            }
            offset++;
        }
        Assert.Fail($"Signature {signature} was not found.");
        return -1;
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }
}
