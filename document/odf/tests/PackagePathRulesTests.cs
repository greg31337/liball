using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class PackagePathRulesTests
{
    [TestCase("content.xml")]
    [TestCase("META-INF/manifest.xml")]
    [TestCase("Pictures/café.png")]
    [TestCase("Configurations2/")]
    public void CanonicalPackagePathsRemainUnchanged(string path)
    {
        PackagePathValue result = PackagePathRules.CanonicalizeEntryPath(path);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.True);
            Assert.That(result.IsTraversal, Is.False);
            Assert.That(result.CanonicalPath, Is.EqualTo(path));
        });
    }

    [TestCase("a/./b.txt", "a/b.txt")]
    [TestCase("a/c/../b.txt", "a/b.txt")]
    [TestCase("a/%7efile.txt", "a/~file.txt")]
    [TestCase("a/%2e/b.txt", "a/b.txt")]
    [TestCase("a/value%2fpart", "a/value%2Fpart")]
    [TestCase("Pictures/caf%C3%A9.png", "Pictures/café.png")]
    [TestCase("Pictures/café.png", "Pictures/café.png")]
    public void NonCanonicalSafePathsProduceStableCanonicalPaths(string path, string expected)
    {
        PackagePathValue result = PackagePathRules.CanonicalizeEntryPath(path);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.IsTraversal, Is.False);
            Assert.That(result.CanonicalPath, Is.EqualTo(expected));
        });
    }

    [TestCase("../outside.txt")]
    [TestCase("a/../../outside.txt")]
    [TestCase("/absolute.txt")]
    [TestCase("..\\outside.txt")]
    [TestCase("%2e%2e/outside.txt")]
    public void TraversalPathsAreRejected(string path)
    {
        PackagePathValue result = PackagePathRules.CanonicalizeEntryPath(path);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsTraversal, Is.True);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(".")]
    [TestCase("a//b.txt")]
    [TestCase("a?query")]
    [TestCase("a#fragment")]
    [TestCase("scheme:path")]
    [TestCase("bad%2")]
    public void MalformedPackagePathsAreRejectedWithoutTraversalClassification(string? path)
    {
        PackagePathValue result = PackagePathRules.CanonicalizeEntryPath(path);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsTraversal, Is.False);
        });
    }

    [TestCase("Object/content.xml", "../Pictures/%69mage.png", "Pictures/image.png")]
    [TestCase("Object/./content.xml", "styles.xml", "Object/styles.xml")]
    [TestCase("Object/content.xml", "../Pictures/café.png#preview", "Pictures/café.png#preview")]
    [TestCase("Object/Sub/content.xml", "../../styles.xml?mode=read", "styles.xml?mode=read")]
    [TestCase("Object/content.xml", "../../outside.xml", "")]
    public void RelativeReferencesResolveThroughCanonicalPackagePaths(
        string basePart,
        string reference,
        string expected)
    {
        Assert.That(PackagePathRules.ResolveRelativeReference(basePart, reference), Is.EqualTo(expected));
        Assert.That(OdfIriResolver.ResolvePackageReference(basePart, reference), Is.EqualTo(expected));
    }

    [Test]
    public void ZipInspectionReportsDotSegmentNormalizationCollision()
    {
        byte[] archive = CreateArchive(["a/b.txt", "a/./b.txt"]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);
        OdfDiagnostic collision = GetDiagnostic(inspection, OdfDiagnosticCodes.PathNormalizationCollision);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries[0].CanonicalPath, Is.EqualTo("a/b.txt"));
            Assert.That(inspection.Entries[0].IsCanonicalPath, Is.True);
            Assert.That(inspection.Entries[1].CanonicalPath, Is.EqualTo("a/b.txt"));
            Assert.That(inspection.Entries[1].IsCanonicalPath, Is.False);
            Assert.That(inspection.Entries[1].HasValidCrc, Is.True);
            Assert.That(collision.Location.PartPath, Is.EqualTo("a/./b.txt"));
            Assert.That(collision.HasRelatedLocation, Is.True);
            Assert.That(collision.RelatedLocation.PartPath, Is.EqualTo("a/b.txt"));
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.InvalidEntryPath), Is.True);
        });
    }

    [Test]
    public void ZipInspectionDetectsUnicodeNormalizationCollision()
    {
        byte[] archive = CreateArchive(["Pictures/café.txt", "Pictures/café.txt"]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries[0].CanonicalPath, Is.EqualTo("Pictures/café.txt"));
            Assert.That(inspection.Entries[1].CanonicalPath, Is.EqualTo("Pictures/café.txt"));
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.PathNormalizationCollision), Is.True);
        });
    }

    [Test]
    public void ZipInspectionDetectsPercentEncodedUnicodeCollision()
    {
        byte[] archive = CreateArchive(["Pictures/café.txt", "Pictures/caf%C3%A9.txt"]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.Entries[0].CanonicalPath, Is.EqualTo("Pictures/café.txt"));
            Assert.That(inspection.Entries[1].CanonicalPath, Is.EqualTo("Pictures/café.txt"));
            Assert.That(HasDiagnostic(inspection, OdfDiagnosticCodes.PathNormalizationCollision), Is.True);
        });
    }

    [TestCase("../outside.txt")]
    [TestCase("%2e%2e/outside.txt")]
    public void ZipInspectionRejectsTraversalBeforeEntryExpansion(string path)
    {
        byte[] archive = CreateArchive([path]);

        ZipPackageInspection inspection = ZipPackageReader.InspectBytes(archive);
        OdfDiagnostic diagnostic = GetDiagnostic(inspection, OdfDiagnosticCodes.PathTraversal);

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsValid, Is.False);
            Assert.That(inspection.Entries[0].HasValidCrc, Is.False);
            Assert.That(diagnostic.Severity, Is.EqualTo(OdfValidationSeverity.Error));
            Assert.That(diagnostic.Location.PartPath, Is.EqualTo(path));
        });
    }

    static byte[] CreateArchive(string[] paths)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                int index = 0;
                while (index < paths.Length)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(paths[index], CompressionLevel.NoCompression);
                    using (Stream output = entry.Open())
                    using (StreamWriter writer = new StreamWriter(output, new UTF8Encoding(false)))
                    {
                        writer.Write("content");
                    }
                    index++;
                }
            }
            return stream.ToArray();
        }
    }

    static bool HasDiagnostic(ZipPackageInspection inspection, string code)
    {
        foreach (OdfDiagnostic diagnostic in inspection.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }
        return false;
    }

    static OdfDiagnostic GetDiagnostic(ZipPackageInspection inspection, string code)
    {
        foreach (OdfDiagnostic diagnostic in inspection.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return diagnostic;
            }
        }
        Assert.Fail($"Expected diagnostic {code}.");
        return new OdfDiagnostic();
    }
}
