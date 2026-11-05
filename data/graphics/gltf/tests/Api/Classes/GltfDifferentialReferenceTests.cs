using System.Numerics;
using System.Text.Json;
using gltf.Tests.Support;
using SharpModelRoot = SharpGLTF.Schema2.ModelRoot;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfDifferentialReferenceTests
{
    [Test]
    public void SharpGltfAndLibraryRoundTripsPreserveCoreModelCounts()
    {
        using TemporaryResourceWorkspace workspace = new TemporaryResourceWorkspace();
        KhronosAssetCase asset = KhronosAssetCorpus.CreateSampleBox();
        string inputPath = KhronosAssetCorpus.MaterializeAsset(workspace, asset);
        GltfReadOptions readOptions = new GltfReadOptions();
        readOptions.ResolveResources = true;
        readOptions.Validate = true;

        GltfReadResult source = new GltfReader().ReadFile(inputPath, readOptions);
        SharpModelRoot sharpSource = SharpModelRoot.Load(inputPath);
        Assert.That(source.Status, Is.EqualTo(GltfOperationStatus.Success));
        AssertCoreCounts(source.Document!, sharpSource, "pinned source");

        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Format = GltfContainerFormat.Json;
        writeOptions.ResourceMode = GltfResourceMode.DataUri;
        GltfWriteResult libraryWrite = new GltfWriter().WriteBytes(
            source.Document!,
            writeOptions);
        string libraryPath = workspace.WriteBytes(
            "round-trips/library.gltf",
            libraryWrite.PrimaryBytes);
        SharpModelRoot sharpFromLibrary = SharpModelRoot.Load(libraryPath);

        string sharpPath = workspace.GetPath("round-trips/sharp.glb");
        sharpSource.SaveGLB(sharpPath);
        GltfReadResult libraryFromSharp = new GltfReader().ReadFile(
            sharpPath,
            readOptions);

        Assert.Multiple(() =>
        {
            Assert.That(
                libraryWrite.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(
                libraryFromSharp.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(
                libraryFromSharp.Validation.IsValid,
                Is.True);
        });
        AssertCoreCounts(source.Document!, sharpFromLibrary, "library output");
        AssertCoreCounts(libraryFromSharp.Document!, sharpSource, "SharpGLTF output");
    }

    [Test]
    public void SharpGltfAndLibraryDecodeTheSamePositionsAndBounds()
    {
        using TemporaryResourceWorkspace workspace = new TemporaryResourceWorkspace();
        KhronosAssetCase asset = KhronosAssetCorpus.CreateSampleBox();
        string inputPath = KhronosAssetCorpus.MaterializeAsset(workspace, asset);
        GltfReadOptions readOptions = new GltfReadOptions();
        readOptions.ResolveResources = true;
        GltfReadResult read = new GltfReader().ReadFile(inputPath, readOptions);
        GltfDocument document = read.Document!;
        int positionAccessor = document.Meshes[0].Primitives[0].Attributes
            .Single(attribute => attribute.Semantic == "POSITION")
            .Accessor;

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = positionAccessor;
        GltfAccessorData libraryValues = new GltfAccessorCodec()
            .ReadFloatingValues(request);
        AccessorBoundsResult libraryBounds = new AccessorBoundsCalculator()
            .CalculateAccessorBounds(document, positionAccessor);

        SharpModelRoot sharp = SharpModelRoot.Load(inputPath);
        SharpGLTF.Schema2.Accessor sharpAccessor = sharp.LogicalMeshes[0]
            .Primitives[0]
            .GetVertexAccessor("POSITION");
        IReadOnlyList<Vector3> sharpValues = sharpAccessor.AsVector3Array();
        sharpAccessor.UpdateBounds();
        (IReadOnlyList<double> Min, IReadOnlyList<double> Max) sharpBounds =
            sharpAccessor.Bounds;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(
                libraryValues.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(libraryValues.Count, Is.EqualTo(sharpValues.Count));
            Assert.That(libraryValues.ComponentCount, Is.EqualTo(3));
            Assert.That(libraryBounds.Available, Is.True);
            Assert.That(libraryBounds.HasValues, Is.True);
        });

        int valueIndex = 0;
        while (valueIndex < sharpValues.Count)
        {
            Vector3 expected = sharpValues[valueIndex];
            int componentOffset = valueIndex * 3;
            Assert.Multiple(() =>
            {
                Assert.That(
                    libraryValues.FloatingValues[componentOffset],
                    Is.EqualTo(expected.X).Within(0.000001),
                    "position X at element " + valueIndex);
                Assert.That(
                    libraryValues.FloatingValues[componentOffset + 1],
                    Is.EqualTo(expected.Y).Within(0.000001),
                    "position Y at element " + valueIndex);
                Assert.That(
                    libraryValues.FloatingValues[componentOffset + 2],
                    Is.EqualTo(expected.Z).Within(0.000001),
                    "position Z at element " + valueIndex);
            });
            valueIndex++;
        }

        int boundIndex = 0;
        while (boundIndex < 3)
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    libraryBounds.Minimum[boundIndex],
                    Is.EqualTo(sharpBounds.Min[boundIndex]).Within(0.000001),
                    "minimum component " + boundIndex);
                Assert.That(
                    libraryBounds.Maximum[boundIndex],
                    Is.EqualTo(sharpBounds.Max[boundIndex]).Within(0.000001),
                    "maximum component " + boundIndex);
            });
            boundIndex++;
        }
    }

    [Test]
    public void PinnedKhronosValidatorCategoriesMatchSharedLibraryCategories()
    {
        using TemporaryResourceWorkspace workspace = new TemporaryResourceWorkspace();
        List<KhronosAssetCase> assets = KhronosAssetCorpus.EnumeratePinnedAssets()
            .Where(asset => asset.ValidatorResultFile != null)
            .ToList();
        int assetIndex = 0;
        while (assetIndex < assets.Count)
        {
            KhronosAssetCase asset = assets[assetIndex];
            string inputPath = KhronosAssetCorpus.MaterializeAsset(workspace, asset);
            string resultFile = asset.ValidatorResultFile
                ?? throw new InvalidOperationException("Missing validator result fixture.");
            string resultPath = workspace.GetPath(asset.Id + "/" + resultFile);
            using JsonDocument oracle = JsonDocument.Parse(File.ReadAllBytes(resultPath));
            Assert.That(
                oracle.RootElement.GetProperty("validatorVersion").GetString(),
                Is.EqualTo("2.0.0-dev.3.2"),
                asset.Id);

            GltfReadOptions readOptions = new GltfReadOptions();
            readOptions.ResolveResources = true;
            GltfReadResult read = new GltfReader().ReadFile(inputPath, readOptions);
            Assert.That(read.Document, Is.Not.Null, asset.Id);
            GltfValidationReport actual = new GltfValidator().ValidateDocument(
                read.Document!,
                new GltfValidationOptions());
            AssertNormativeExpectation(actual, asset);
            AssertSharedCategories(oracle, actual, asset.Id);
            assetIndex++;
        }
    }

    private static void AssertCoreCounts(
        GltfDocument expected,
        SharpModelRoot actual,
        string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.LogicalAccessors, Has.Count.EqualTo(expected.Accessors.Count), context);
            Assert.That(actual.LogicalAnimations, Has.Count.EqualTo(expected.Animations.Count), context);
            Assert.That(actual.LogicalBuffers, Has.Count.EqualTo(expected.Buffers.Count), context);
            Assert.That(actual.LogicalBufferViews, Has.Count.EqualTo(expected.BufferViews.Count), context);
            Assert.That(actual.LogicalCameras, Has.Count.EqualTo(expected.Cameras.Count), context);
            Assert.That(actual.LogicalImages, Has.Count.EqualTo(expected.Images.Count), context);
            Assert.That(actual.LogicalMaterials, Has.Count.EqualTo(expected.Materials.Count), context);
            Assert.That(actual.LogicalMeshes, Has.Count.EqualTo(expected.Meshes.Count), context);
            Assert.That(actual.LogicalNodes, Has.Count.EqualTo(expected.Nodes.Count), context);
            Assert.That(actual.LogicalScenes, Has.Count.EqualTo(expected.Scenes.Count), context);
            Assert.That(actual.LogicalSkins, Has.Count.EqualTo(expected.Skins.Count), context);
            Assert.That(actual.LogicalTextures, Has.Count.EqualTo(expected.Textures.Count), context);
        });
    }

    private static void AssertNormativeExpectation(
        GltfValidationReport report,
        KhronosAssetCase asset)
    {
        if (asset.ValidationIssueCode != null)
        {
            Assert.That(
                report.Issues.Any(issue =>
                    issue.Code == asset.ValidationIssueCode
                    && issue.Severity == asset.ValidationIssueSeverity),
                Is.True,
                asset.Id);
        }
        else
        {
            Assert.That(
                report.Issues.Any(issue => issue.Severity == GltfValidationSeverity.Error),
                Is.False,
                asset.Id);
        }
    }

    private static void AssertSharedCategories(
        JsonDocument oracle,
        GltfValidationReport actual,
        string assetId)
    {
        JsonElement messages = oracle.RootElement
            .GetProperty("issues")
            .GetProperty("messages");
        foreach (JsonElement message in messages.EnumerateArray())
        {
            string code = message.GetProperty("code").GetString() ?? "";
            if (IsSharedIssueCode(code))
            {
                GltfValidationSeverity severity = ConvertSeverity(
                    message.GetProperty("severity").GetInt32());
                Assert.That(
                    actual.Issues.Any(issue =>
                        issue.Code == code && issue.Severity == severity),
                    Is.True,
                    assetId + ": " + code + " / " + severity);
            }
        }
    }

    private static bool IsSharedIssueCode(string code)
    {
        return code == GltfIssueCodes.UnknownAssetMinorVersion
            || code == GltfIssueCodes.UnsupportedExtension
            || code == GltfIssueCodes.MeshPrimitiveNoPosition;
    }

    private static GltfValidationSeverity ConvertSeverity(int severity)
    {
        if (severity == 0)
        {
            return GltfValidationSeverity.Error;
        }

        if (severity == 1)
        {
            return GltfValidationSeverity.Warning;
        }

        return GltfValidationSeverity.Information;
    }
}
