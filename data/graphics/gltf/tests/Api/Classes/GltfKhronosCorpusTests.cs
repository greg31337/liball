using System.Security.Cryptography;
using System.Text;
using gltf.Tests.Support;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfKhronosCorpusTests
{
    [Test]
    public void LoadablePinnedAssetsPassReaderWriterValidatorAndConverterPipelines()
    {
        using TemporaryResourceWorkspace workspace = new TemporaryResourceWorkspace();
        List<KhronosAssetCase> assets = KhronosAssetCorpus.EnumeratePinnedAssets();
        int assetIndex = 0;
        while (assetIndex < assets.Count)
        {
            KhronosAssetCase asset = assets[assetIndex];
            if (asset.Expectation != KhronosAssetExpectation.Unavailable)
            {
                string inputPath = KhronosAssetCorpus.MaterializeAsset(workspace, asset);
                AssertPinnedFiles(workspace, asset);
                GltfReadOptions readOptions = new GltfReadOptions();
                readOptions.ResolveResources = true;
                readOptions.Validate = true;

                GltfReadResult read = new GltfReader().ReadFile(inputPath, readOptions);
                Assert.That(
                    read.Status,
                    Is.EqualTo(GltfOperationStatus.Success),
                    asset.Id);
                Assert.That(read.Container!.Format, Is.EqualTo(asset.Format), asset.Id);
                Assert.That(read.Document, Is.Not.Null, asset.Id);
                Assert.That(read.Validation.IsValid, Is.True, asset.Id);

                GltfValidationReport validation = new GltfValidator().ValidateDocument(
                    read.Document!,
                    new GltfValidationOptions());
                Assert.That(validation.IsValid, Is.True, asset.Id);
                if (asset.ValidationIssueCode != null)
                {
                    AssertIssue(validation, asset);
                }

                GltfWriteOptions writeOptions = new GltfWriteOptions();
                writeOptions.Format = GltfContainerFormat.Json;
                writeOptions.ResourceMode = GltfResourceMode.DataUri;
                GltfWriteResult write = new GltfWriter().WriteBytes(
                    read.Document!,
                    writeOptions);
                Assert.That(write.Status, Is.EqualTo(GltfOperationStatus.Success), asset.Id);
                Assert.That(write.PrimaryBytes, Is.Not.Empty, asset.Id);

                GltfReadOptions roundTripOptions = new GltfReadOptions();
                roundTripOptions.Validate = true;
                GltfReadResult roundTrip = new GltfReader().ReadBytes(
                    write.PrimaryBytes,
                    roundTripOptions);
                Assert.That(
                    roundTrip.Status,
                    Is.EqualTo(GltfOperationStatus.Success),
                    asset.Id);
                Assert.That(roundTrip.Validation.IsValid, Is.True, asset.Id);

                string outputPath = workspace.GetPath(asset.Id + "/converted.glb");
                GltfConversionOptions conversionOptions = new GltfConversionOptions();
                conversionOptions.OutputFormat = GltfContainerFormat.Glb;
                conversionOptions.ResourceMode = GltfResourceMode.GlbBin;
                conversionOptions.ReadOptions.ResolveResources = true;
                GltfConversionResult conversion = new GltfConverter().ConvertFile(
                    inputPath,
                    outputPath,
                    conversionOptions);
                Assert.That(
                    conversion.Status,
                    Is.EqualTo(GltfOperationStatus.Success),
                    asset.Id);
                Assert.That(File.Exists(outputPath), Is.True, asset.Id);

                GltfValidationReport convertedValidation = new GltfValidator().ValidateFile(
                    outputPath,
                    new GltfValidationOptions());
                Assert.That(convertedValidation.IsValid, Is.True, asset.Id);
            }

            assetIndex++;
        }
    }

    [Test]
    public void NonLoadablePinnedAssetsHaveStableReadValidationAndConversionCategories()
    {
        using TemporaryResourceWorkspace workspace = new TemporaryResourceWorkspace();
        List<KhronosAssetCase> assets = KhronosAssetCorpus.EnumeratePinnedAssets();
        int assetIndex = 0;
        while (assetIndex < assets.Count)
        {
            KhronosAssetCase asset = assets[assetIndex];
            if (asset.Expectation == KhronosAssetExpectation.Unavailable)
            {
                string inputPath = KhronosAssetCorpus.MaterializeAsset(workspace, asset);
                AssertPinnedFiles(workspace, asset);
                GltfReadOptions readOptions = new GltfReadOptions();
                readOptions.ResolveResources = true;
                readOptions.Validate = true;

                GltfReadResult read = new GltfReader().ReadFile(inputPath, readOptions);
                Assert.That(
                    read.Status,
                    Is.EqualTo(GltfOperationStatus.Unavailable),
                    asset.Id);
                Assert.That(read.Document, Is.Not.Null, asset.Id);
                Assert.That(
                    read.Diagnostics.Any(issue => issue.Code == asset.ReadIssueCode),
                    Is.True,
                    asset.Id);

                GltfValidationReport validation = new GltfValidator().ValidateFile(
                    inputPath,
                    new GltfValidationOptions());
                AssertIssue(validation, asset);

                string outputPath = workspace.GetPath(asset.Id + "/rejected.glb");
                GltfConversionOptions conversionOptions = new GltfConversionOptions();
                conversionOptions.OutputFormat = GltfContainerFormat.Glb;
                conversionOptions.ResourceMode = GltfResourceMode.GlbBin;
                conversionOptions.ReadOptions.ResolveResources = true;
                GltfConversionResult conversion = new GltfConverter().ConvertFile(
                    inputPath,
                    outputPath,
                    conversionOptions);
                Assert.That(
                    conversion.Status,
                    Is.EqualTo(GltfOperationStatus.Unavailable),
                    asset.Id);
                Assert.That(
                    conversion.Diagnostics.Any(issue => issue.Code == asset.ReadIssueCode),
                    Is.True,
                    asset.Id);
                Assert.That(File.Exists(outputPath), Is.False, asset.Id);
            }

            assetIndex++;
        }
    }

    [Test]
    public void ReaderAcceptsFutureMinorVersionOnlyWhenMinimumVersionRemainsSupported()
    {
        byte[] compatible = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.1\"}}");
        byte[] incompatible = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.1\",\"minVersion\":\"2.1\"}}");

        GltfReadResult compatibleResult = new GltfReader().ReadBytes(
            compatible,
            new GltfReadOptions());
        GltfReadResult incompatibleResult = new GltfReader().ReadBytes(
            incompatible,
            new GltfReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(
                compatibleResult.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(
                incompatibleResult.Status,
                Is.EqualTo(GltfOperationStatus.Unavailable));
            Assert.That(
                incompatibleResult.Diagnostics[0].Code,
                Is.EqualTo(GltfIssueCodes.UnknownAssetMinorVersion));
            Assert.That(
                incompatibleResult.Diagnostics[0].JsonPointer,
                Is.EqualTo("/asset/minVersion"));
        });
    }

    private static void AssertPinnedFiles(
        TemporaryResourceWorkspace workspace,
        KhronosAssetCase asset)
    {
        Assert.That(asset.Revision, Has.Length.EqualTo(40), asset.Id);
        Assert.That(
            asset.Repository,
            Does.StartWith("https://github.com/KhronosGroup/"),
            asset.Id);
        int fileIndex = 0;
        while (fileIndex < asset.Files.Count)
        {
            KhronosAssetFile file = asset.Files[fileIndex];
            byte[] bytes = File.ReadAllBytes(
                workspace.GetPath(asset.Id + "/" + file.RelativePath));
            string hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            Assert.That(hash, Is.EqualTo(file.Sha256), asset.Id + "/" + file.RelativePath);
            fileIndex++;
        }

        byte[] entryBytes = File.ReadAllBytes(
            workspace.GetPath(asset.Id + "/" + asset.EntryFile));
        string entryHash = Convert.ToHexString(
            SHA256.HashData(entryBytes)).ToLowerInvariant();
        Assert.That(entryHash, Is.EqualTo(asset.EntrySha256), asset.Id);
    }

    private static void AssertIssue(
        GltfValidationReport report,
        KhronosAssetCase asset)
    {
        Assert.That(asset.ValidationIssueCode, Is.Not.Null, asset.Id);
        Assert.That(
            report.Issues.Any(issue =>
                issue.Code == asset.ValidationIssueCode
                && issue.Severity == asset.ValidationIssueSeverity),
            Is.True,
            asset.Id);
    }
}
