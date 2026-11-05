using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class PackageConformanceValidatorTests
{
    [Test]
    public void AcceptsCompletePackageAndPublicIntegrityInspection()
    {
        OdfPackage package = ReadPackage(CreatePackage(
            OdfMediaTypes.Text,
            [OdfPartNames.Content],
            [OdfMediaTypes.Xml],
            [Encoding.UTF8.GetBytes("<office:document-content xmlns:office='"
                + OdfNamespaces.Office
                + "'/>")]));

        PackageValidationResult result = Validate(package);
        OdfDiagnostic[] publicDiagnostics =
            new OdfSecurityService().InspectPackageIntegrity(
                package,
                Limits());
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        index.ReplaceEntry(
            OdfPartNames.Content,
            Encoding.UTF8.GetBytes("updated content"));
        PackageApiModel.RefreshPackage(package);
        PackageValidationResult updated = Validate(package);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.SignatureContainers, Is.Empty);
            Assert.That(result.EmbeddedPackages, Is.Empty);
            Assert.That(publicDiagnostics, Is.Empty);
            Assert.That(updated.IsValid, Is.True);
            Assert.That(updated.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ReportsRequiredPartsAndBidirectionalManifestCoverage()
    {
        OdfPackage missingContent = ReadPackage(CreatePackage(
            OdfMediaTypes.Text,
            [],
            [],
            []));
        OdfPackage inconsistent = ReadPackage(CreatePackage(
            OdfMediaTypes.Text,
            [OdfPartNames.Content, "declared.bin"],
            [OdfMediaTypes.Png, OdfMediaTypes.OctetStream],
            [Encoding.UTF8.GetBytes("content"), null],
            ["orphan.bin"],
            [Encoding.UTF8.GetBytes("orphan")]));

        PackageValidationResult missingResult = Validate(missingContent);
        PackageValidationResult inconsistentResult = Validate(inconsistent);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.IsValid, Is.False);
            Assert.That(
                Codes(missingResult.Diagnostics),
                Does.Contain(OdfDiagnosticCodes.MissingRequiredPackagePart));
            Assert.That(
                Codes(missingResult.Diagnostics),
                Does.Contain(OdfDiagnosticCodes.ManifestEntryMissing));
            Assert.That(inconsistentResult.IsValid, Is.False);
            Assert.That(
                Codes(inconsistentResult.Diagnostics),
                Does.Contain(OdfDiagnosticCodes.ManifestEntryMissing));
            Assert.That(
                Codes(inconsistentResult.Diagnostics),
                Does.Contain(OdfDiagnosticCodes.ManifestMediaTypeMismatch));
            Assert.That(
                DiagnosticPaths(
                    inconsistentResult.Diagnostics,
                    OdfDiagnosticCodes.ManifestEntryMissing),
                Does.Contain("declared.bin"));
            Assert.That(
                DiagnosticPaths(
                    inconsistentResult.Diagnostics,
                    OdfDiagnosticCodes.ManifestEntryMissing),
                Does.Contain("orphan.bin"));
        });
    }

    [Test]
    public void ReportsManifestPathsDuplicatesAndMediaTypes()
    {
        OdfPackage package = ReadPackage(CreatePackage(
            OdfMediaTypes.Text,
            [
                OdfPartNames.Content,
                OdfPartNames.Content,
                "../escape.bin",
                "bad-media.bin"
            ],
            [
                OdfMediaTypes.Xml,
                OdfMediaTypes.Xml,
                OdfMediaTypes.OctetStream,
                "not a media type"
            ],
            [
                Encoding.UTF8.GetBytes("content"),
                null,
                null,
                null
            ]));

        PackageValidationResult result = Validate(package);
        string[] codes = Codes(result.Diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidManifestEntry));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.PathTraversal));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidMediaType));
        });
    }

    [Test]
    public void ReportsInvalidAndUnsupportedEncryptionStructures()
    {
        OdfPackage package = ReadPackage(CreatePackage(
            OdfMediaTypes.Text,
            [OdfPartNames.Content],
            [OdfMediaTypes.Xml],
            [Encoding.UTF8.GetBytes("encrypted bytes")]));
        OdfManifestEntry entry = FindManifestEntry(
            package.Manifest!,
            OdfPartNames.Content);
        entry.EncryptionData = new OdfManifestNode();
        entry.Encryption = new OdfEncryptionInfo
        {
            Method = OdfEncryptionMethod.Unsupported,
            ChecksumType = "unsupported-checksum",
            ChecksumIsValid = true,
            HasSize = true,
            SizeIsValid = true,
            Algorithm = new OdfEncryptionAlgorithm
            {
                IsValid = true
            },
            KeyDerivation = new OdfKeyDerivation
            {
                Name = "unsupported-derivation",
                SaltIsValid = true,
                IterationCountIsValid = true,
                KeySizeIsValid = true,
                IsValid = true
            },
            IsValid = true
        };

        PackageValidationResult result = Validate(package);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                Codes(result.Diagnostics),
                Does.Contain(OdfDiagnosticCodes.UnsupportedEncryption));
            Assert.That(
                Codes(result.Diagnostics),
                Does.Contain(OdfDiagnosticCodes.InvalidEncryptionStructure));
        });
    }

    [Test]
    public void ParsesAndReportsInvalidSignatureContainers()
    {
        string signatureXml = "<dsig:document-signatures xmlns:dsig='"
            + OdfNamespaces.DigitalSignature
            + "' dsig:version='1.4'/>";
        OdfPackage package = ReadPackage(CreatePackage(
            OdfMediaTypes.Text,
            [OdfPartNames.Content, OdfPartNames.DocumentSignatures],
            [OdfMediaTypes.Xml, OdfMediaTypes.Xml],
            [
                Encoding.UTF8.GetBytes("content"),
                Encoding.UTF8.GetBytes(signatureXml)
            ]));

        PackageValidationResult result = Validate(package);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.SignatureContainers, Has.Length.EqualTo(1));
            Assert.That(result.SignatureContainers[0].IsValid, Is.False);
            Assert.That(
                Codes(result.Diagnostics),
                Does.Contain(OdfDiagnosticCodes.SignatureInvalid));
        });
    }

    [Test]
    public void RecursivelyValidatesEmbeddedArchivePackages()
    {
        byte[] incompleteChild = CreatePackage(
            OdfMediaTypes.Spreadsheet,
            [],
            [],
            []);
        OdfPackage package = ReadPackage(CreatePackage(
            OdfMediaTypes.Text,
            [OdfPartNames.Content, "Object1.ods"],
            [OdfMediaTypes.Xml, OdfMediaTypes.Spreadsheet],
            [Encoding.UTF8.GetBytes("content"), incompleteChild]));

        PackageValidationResult result = Validate(package);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.EmbeddedPackages, Has.Length.EqualTo(1));
            Assert.That(
                Codes(result.Diagnostics),
                Does.Contain(OdfDiagnosticCodes.InvalidEmbeddedPackage));
            Assert.That(
                DiagnosticPaths(
                    result.Diagnostics,
                    OdfDiagnosticCodes.MissingRequiredPackagePart),
                Does.Contain("Object1.ods!/content.xml"));
        });
    }

    static PackageValidationResult Validate(OdfPackage package)
    {
        return new PackageConformanceValidator(Limits())
            .ValidatePackage(package);
    }

    static OdfPackage ReadPackage(byte[] data)
    {
        OdfLoadOptions options = new OdfLoadOptions();
        options.SecurityLimits = Limits();
        return new OdfPackageReader().ReadPackageBytes(data, options);
    }

    static OdfSecurityLimits Limits()
    {
        OdfSecurityLimits result = new OdfSecurityLimits();
        result.MaximumSingleEntryBytes = 10485760;
        result.MaximumTotalExpandedBytes = 52428800;
        result.MaximumCompressionRatio = 1000;
        result.MaximumEmbeddedPackageDepth = 4;
        return result;
    }

    static byte[] CreatePackage(
        string rootMediaType,
        string[] manifestPaths,
        string[] manifestMediaTypes,
        byte[]?[] manifestContents,
        string[]? extraPaths = null,
        byte[][]? extraContents = null)
    {
        string manifest = "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' m:version='1.4'>"
            + FileEntry(OdfPartNames.Root, rootMediaType);
        int manifestIndex = 0;
        while (manifestIndex < manifestPaths.Length)
        {
            manifest = manifest + FileEntry(
                manifestPaths[manifestIndex],
                manifestMediaTypes[manifestIndex]);
            manifestIndex++;
        }
        manifest = manifest + "</m:manifest>";

        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                Encoding.UTF8.GetBytes(rootMediaType),
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                Encoding.UTF8.GetBytes(manifest),
                CompressionLevel.Optimal);
            manifestIndex = 0;
            while (manifestIndex < manifestPaths.Length)
            {
                byte[]? content = manifestContents[manifestIndex];
                if (content != null)
                {
                    WriteEntry(
                        archive,
                        manifestPaths[manifestIndex],
                        content,
                        CompressionLevel.Optimal);
                }
                manifestIndex++;
            }
            if (extraPaths != null && extraContents != null)
            {
                int extraIndex = 0;
                while (extraIndex < extraPaths.Length)
                {
                    WriteEntry(
                        archive,
                        extraPaths[extraIndex],
                        extraContents[extraIndex],
                        CompressionLevel.Optimal);
                    extraIndex++;
                }
            }
        }
        return output.ToArray();
    }

    static string FileEntry(string path, string mediaType)
    {
        return "<m:file-entry m:full-path='"
            + path
            + "' m:media-type='"
            + mediaType
            + "'/>";
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        byte[] content,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream stream = entry.Open();
        stream.Write(content);
    }

    static OdfManifestEntry FindManifestEntry(
        OdfManifest manifest,
        string path)
    {
        return manifest.Entries.Single(entry => entry.FullPath == path);
    }

    static string[] Codes(OdfDiagnostic[] diagnostics)
    {
        return diagnostics.Select(diagnostic => diagnostic.Code).ToArray();
    }

    static string[] DiagnosticPaths(
        OdfDiagnostic[] diagnostics,
        string code)
    {
        return diagnostics
            .Where(diagnostic => diagnostic.Code == code)
            .Select(diagnostic => diagnostic.Location.PartPath)
            .ToArray();
    }
}
