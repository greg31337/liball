namespace gltf.Tests.Internal.Conversion;

[TestFixture]
public class ResourceLayoutPlannerTests
{
    [TestCase(GltfResourceMode.Preserve, false, "image.png")]
    [TestCase(GltfResourceMode.External, false, "image.png")]
    [TestCase(GltfResourceMode.DataUri, true, "data:image/png;base64,AQID")]
    [TestCase(GltfResourceMode.BufferView, true, null)]
    public void PlanLayout_ProducesDeterministicPlansForNonGlbModes(
        GltfResourceMode mode,
        bool embedded,
        string? expectedUri)
    {
        GltfDocument document = new GltfDocument();
        GltfResource image = CreateResource(
            GltfResourceKind.Image,
            "image.png",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [1, 2, 3]);
        document.Resources.Resources.Add(image);

        ResourceLayoutPlan first = new ResourceLayoutPlanner().PlanLayout(
            document,
            GltfContainerFormat.Json,
            mode,
            false,
            false);
        ResourceLayoutPlan second = new ResourceLayoutPlanner().PlanLayout(
            document,
            GltfContainerFormat.Json,
            mode,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(first.Format, Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(first.Mode, Is.EqualTo(mode));
            Assert.That(first.Placements, Has.Count.EqualTo(1));
            Assert.That(first.Placements[0].SourceResource, Is.Zero);
            Assert.That(first.Placements[0].OutputResource, Is.Zero);
            Assert.That(first.Placements[0].Mode, Is.EqualTo(mode));
            Assert.That(first.Placements[0].Embedded, Is.EqualTo(embedded));
            Assert.That(first.Placements[0].Uri, Is.EqualTo(expectedUri));
            Assert.That(first.WritePlan.Manifest.Select(value => value.Uri),
                Is.EqualTo(second.WritePlan.Manifest.Select(value => value.Uri)));
            Assert.That(image.EffectiveUri, Is.Null);
            Assert.That(image.Embedded, Is.False);
            Assert.That(image.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });

        if (mode == GltfResourceMode.BufferView)
        {
            Assert.That(first.Placements[0].BufferView, Is.Zero);
        }
    }

    [Test]
    public void PlanLayout_GlbBinPacksBuffersAndRequestedImagesWithAlignment()
    {
        GltfDocument document = new GltfDocument();
        GltfResource bufferResource = CreateResource(
            GltfResourceKind.Buffer,
            "geometry.bin",
            GltfMimeTypes.GltfBuffer,
            GltfImageFormat.Unknown,
            [1, 2, 3]);
        GltfResource imageResource = CreateResource(
            GltfResourceKind.Image,
            "image.png",
            null,
            GltfImageFormat.Png,
            [4, 5]);
        GltfResource otherResource = CreateResource(
            GltfResourceKind.Other,
            "metadata.bin",
            GltfMimeTypes.OctetStream,
            GltfImageFormat.Unknown,
            [9]);
        document.Resources.Resources.Add(bufferResource);
        document.Resources.Resources.Add(imageResource);
        document.Resources.Resources.Add(otherResource);
        document.Buffers.Add(new GltfBuffer
        {
            Uri = "geometry.bin",
            ByteLength = 3,
            HasResource = true,
            Resource = 0
        });
        document.Images.Add(new GltfImage
        {
            Uri = "image.png",
            HasResource = true,
            Resource = 1
        });

        ResourceLayoutPlan result = new ResourceLayoutPlanner().PlanLayout(
            document,
            GltfContainerFormat.Glb,
            GltfResourceMode.GlbBin,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasBinChunk, Is.True);
            Assert.That(result.BinBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 0, 4, 5 }));
            Assert.That(result.WritePlan.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.WritePlan.Resources.Resources[0].Bytes,
                Is.EqualTo(result.BinBytes));
            Assert.That(result.WritePlan.Resources.Resources[0].Bytes,
                Is.Not.SameAs(result.BinBytes));
            Assert.That(result.WritePlan.SourceResourceMap,
                Is.EqualTo(new[] { 0, 0, 1 }));
            Assert.That(result.WritePlan.SourceResourceUris,
                Is.EqualTo(new[] { null, null, "metadata.bin" }));
            Assert.That(result.Placements[0].BinByteOffset, Is.Zero);
            Assert.That(result.Placements[0].BufferView,
                Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(result.Placements[1].BinByteOffset, Is.EqualTo(4));
            Assert.That(result.Placements[1].BufferView, Is.Zero);
            Assert.That(result.Placements[1].MimeType,
                Is.EqualTo(GltfMimeTypes.Png));
            Assert.That(result.Placements[2].Mode,
                Is.EqualTo(GltfResourceMode.Preserve));
            Assert.That(result.WritePlan.Manifest.Select(value => value.Mode),
                Is.EqualTo(new[]
                {
                    GltfResourceMode.GlbBin,
                    GltfResourceMode.Preserve
                }));
            Assert.That(bufferResource.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(imageResource.Bytes, Is.EqualTo(new byte[] { 4, 5 }));
        });
    }

    [Test]
    public void PlanLayout_GlbBinPreservesImagesWhenEmbeddingIsDisabled()
    {
        GltfDocument document = new GltfDocument();
        document.Resources.Resources.Add(CreateResource(
            GltfResourceKind.Buffer,
            "geometry.bin",
            GltfMimeTypes.GltfBuffer,
            GltfImageFormat.Unknown,
            [1]));
        document.Resources.Resources.Add(CreateResource(
            GltfResourceKind.Image,
            "image.png",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [2]));
        document.Buffers.Add(new GltfBuffer
        {
            Uri = "geometry.bin",
            ByteLength = 1,
            HasResource = true,
            Resource = 0
        });
        document.Images.Add(new GltfImage
        {
            Uri = "image.png",
            HasResource = true,
            Resource = 1
        });

        ResourceLayoutPlan result = new ResourceLayoutPlanner().PlanLayout(
            document,
            GltfContainerFormat.Auto,
            GltfResourceMode.GlbBin,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(result.BinBytes, Is.EqualTo(new byte[] { 1 }));
            Assert.That(result.Placements[1].Mode,
                Is.EqualTo(GltfResourceMode.Preserve));
            Assert.That(result.Placements[1].Uri, Is.EqualTo("image.png"));
            Assert.That(result.Placements[1].BufferView,
                Is.EqualTo(GltfConstants.AbsentIndex));
        });
    }

    [Test]
    public void PlanLayout_PreservesOwnedGlbBinChunk()
    {
        GltfDocument document = new GltfDocument();
        document.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Glb,
            HasBinChunk = true,
            BinBytes = [7, 8]
        };
        document.Buffers.Add(new GltfBuffer
        {
            ByteLength = 2
        });

        ResourceLayoutPlan result = new ResourceLayoutPlanner().PlanLayout(
            document,
            GltfContainerFormat.Glb,
            GltfResourceMode.Preserve,
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasBinChunk, Is.True);
            Assert.That(result.BinBytes, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(result.BinBytes, Is.Not.SameAs(document.Container.BinBytes));
        });
    }

    [TestCase(GltfContainerFormat.Json, GltfResourceMode.GlbBin, false,
        "GLB BIN resource layout requires GLB output.")]
    [TestCase(GltfContainerFormat.Json, GltfResourceMode.External, true,
        "Image embedding requires BufferView or GlbBin resource mode.")]
    public void PlanLayout_RejectsContradictoryOptionsBeforePlanning(
        GltfContainerFormat format,
        GltfResourceMode mode,
        bool embedImages,
        string message)
    {
        GltfDocument document = new GltfDocument();
        GltfResource source = CreateResource(
            GltfResourceKind.Image,
            "image.png",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [1]);
        document.Resources.Resources.Add(source);

        Assert.That(
            () => new ResourceLayoutPlanner().PlanLayout(
                document,
                format,
                mode,
                embedImages,
                false),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(message));
        Assert.Multiple(() =>
        {
            Assert.That(source.EffectiveUri, Is.Null);
            Assert.That(source.Embedded, Is.False);
            Assert.That(source.Bytes, Is.EqualTo(new byte[] { 1 }));
        });
    }

    [Test]
    public void PlanLayout_RejectsUriLessBufferWhenPreservingJson()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(new GltfBuffer
        {
            ByteLength = 1
        });

        Assert.That(
            () => new ResourceLayoutPlanner().PlanLayout(
                document,
                GltfContainerFormat.Json,
                GltfResourceMode.Preserve,
                false,
                false),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "JSON output cannot preserve URI-less buffer 0."));
    }

    [Test]
    public void PlanLayout_RejectsUnresolvedBufferRequiredByGlbBin()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(new GltfBuffer
        {
            Uri = "geometry.bin",
            ByteLength = 1
        });

        Assert.That(
            () => new ResourceLayoutPlanner().PlanLayout(
                document,
                GltfContainerFormat.Glb,
                GltfResourceMode.GlbBin,
                false,
                false),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Buffer 0 has no resolved resource for GLB BIN layout."));
    }

    [Test]
    public void PlanLayout_ValidatesAllResourcesBeforeAllocatingBinOutput()
    {
        GltfDocument document = new GltfDocument();
        GltfResource first = CreateResource(
            GltfResourceKind.Buffer,
            "first.bin",
            GltfMimeTypes.GltfBuffer,
            GltfImageFormat.Unknown,
            [1]);
        GltfResource second = CreateResource(
            GltfResourceKind.Buffer,
            "second.bin",
            GltfMimeTypes.GltfBuffer,
            GltfImageFormat.Unknown,
            [2]);
        second.Bytes = null!;
        document.Resources.Resources.Add(first);
        document.Resources.Resources.Add(second);

        Assert.That(
            () => new ResourceLayoutPlanner().PlanLayout(
                document,
                GltfContainerFormat.Glb,
                GltfResourceMode.GlbBin,
                false,
                false),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "Document resource 1 bytes are null."));
        Assert.That(first.Bytes, Is.EqualTo(new byte[] { 1 }));
    }

    private static GltfResource CreateResource(
        GltfResourceKind kind,
        string sourceUri,
        string? mimeType,
        GltfImageFormat imageFormat,
        byte[] bytes)
    {
        GltfResource result = new GltfResource();
        result.Kind = kind;
        result.UriKind = GltfUriKind.Relative;
        result.SourceUri = sourceUri;
        result.MimeType = mimeType;
        result.ImageFormat = imageFormat;
        result.Bytes = bytes;
        return result;
    }
}
