using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfValidatorTests
{
    [Test]
    public void ValidateDocument_AggregatesRulesAndPopulatesStatistics()
    {
        GltfDocument document = new GltfDocument();
        document.Asset.Version = "3.0";
        document.Nodes.Add(new GltfNode());

        GltfValidationReport report = new GltfValidator().ValidateDocument(
            document,
            new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(
                report.Issues.Any(issue => issue.Code == GltfIssueCodes.UnknownAssetMajorVersion),
                Is.True);
            Assert.That(report.Statistics.NodeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ValidateBytes_ValidatesRawJsonBeforeConstructingAModel()
    {
        GltfValidator validator = new GltfValidator();

        GltfValidationReport malformed = validator.ValidateBytes(
            Encoding.UTF8.GetBytes("{\"asset\":{"),
            new GltfValidationOptions());
        GltfValidationReport structurallyInvalid = validator.ValidateBytes(
            Encoding.UTF8.GetBytes("{\"asset\":{}}"),
            new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(malformed.IsValid, Is.False);
            Assert.That(malformed.Issues[0].Code, Is.EqualTo(GltfIssueCodes.InvalidJson));
            Assert.That(malformed.Issues[0].HasByteOffset, Is.True);
            Assert.That(structurallyInvalid.IsValid, Is.False);
            Assert.That(
                structurallyInvalid.Issues.Any(issue =>
                    issue.Code == GltfIssueCodes.UndefinedProperty
                    && issue.JsonPointer == "/asset/version"),
                Is.True);
        });
    }

    [Test]
    public void ValidateBytes_ValidatesGlbContainerBeforeJson()
    {
        byte[] glb = new byte[12];
        WriteUInt32(glb, 0, GltfConstants.GlbMagic);
        WriteUInt32(glb, 4, (uint)GltfConstants.GlbVersion);
        WriteUInt32(glb, 8, 12U);

        GltfValidationReport report = new GltfValidator().ValidateBytes(
            glb,
            new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues[0].Code, Is.EqualTo(GltfIssueCodes.GlbLengthTooSmall));
            Assert.That(report.Issues[0].ByteOffset, Is.EqualTo(8));
        });
    }

    [Test]
    public void ValidateBytes_AcceptsAValidWrittenGlb()
    {
        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Format = GltfContainerFormat.Glb;
        writeOptions.Validate = false;
        byte[] bytes = new GltfWriter().WriteBytes(
            new GltfDocument(),
            writeOptions).PrimaryBytes;

        GltfValidationReport report = new GltfValidator().ValidateBytes(
            bytes,
            new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidateBytes_AggregatesDuplicateMembersUsingStableLocations()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"asset\":{\"version\":\"2.0\"}}");

        GltfValidationReport report = new GltfValidator().ValidateBytes(
            bytes,
            new GltfValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues, Has.Count.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(GltfIssueCodes.JsonDuplicateMember));
            Assert.That(report.Issues[0].JsonPointer, Is.EqualTo("/asset"));
            Assert.That(report.Issues[0].HasByteOffset, Is.True);
        });
    }

    [Test]
    public void ValidateContainerAndFile_UseTheSameValidationPipeline()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}");
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Json;
        container.JsonBytes = (byte[])bytes.Clone();
        string path = Path.Combine(Path.GetTempPath(), $"gltf-validator-{Guid.NewGuid():N}.gltf");
        File.WriteAllBytes(path, bytes);

        try
        {
            GltfValidator validator = new GltfValidator();
            GltfValidationReport containerReport = validator.ValidateContainer(
                container,
                new GltfValidationOptions());
            GltfValidationReport fileReport = validator.ValidateFile(
                path,
                new GltfValidationOptions());

            Assert.Multiple(() =>
            {
                Assert.That(containerReport.IsValid, Is.True);
                Assert.That(fileReport.IsValid, Is.True);
                Assert.That(fileReport.Issues, Is.Empty);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void StrictMode_ConvertsOnlyInvalidReportsToValidationExceptions()
    {
        GltfValidator validator = new GltfValidator();
        byte[] invalid = Encoding.UTF8.GetBytes("{\"asset\":{}}");
        GltfValidationOptions collecting = new GltfValidationOptions();
        GltfValidationOptions strict = new GltfValidationOptions();
        strict.Strict = true;
        GltfDocument warningDocument = new GltfDocument();
        warningDocument.Asset.Version = "2.1";

        GltfValidationReport collectingReport = validator.ValidateBytes(invalid, collecting);
        GltfValidationReport warningReport = validator.ValidateDocument(warningDocument, strict);

        Assert.Multiple(() =>
        {
            Assert.That(collectingReport.IsValid, Is.False);
            Assert.That(warningReport.IsValid, Is.True);
            Assert.That(
                () => validator.ValidateBytes(invalid, strict),
                Throws.TypeOf<GltfValidationException>()
                    .With.Message.Contains(GltfIssueCodes.UndefinedProperty));
        });
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }
}
