using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfStorageTransformTests
{
    [Test]
    public void DenseToSparse_OptimizesOnlySmallerSparseRepresentationsAndRoundTrips()
    {
        GltfDocument source = new GltfDocument();
        int sparseCandidate = CreateUnsignedAccessor(
            source,
            new uint[] { 0, 7, 0, 9, 0 });
        int denseCandidate = CreateUnsignedAccessor(
            source,
            new uint[] { 1, 2, 3, 4, 5 });

        GltfTransformReport sparseReport = Transform(
            source,
            GltfTransformKind.DenseToSparse,
            new JsonObject());
        GltfDocument sparse = sparseReport.Document!;

        Assert.Multiple(() =>
        {
            Assert.That(sparse.Accessors[sparseCandidate].Sparse, Is.Not.Null);
            Assert.That(sparse.Accessors[sparseCandidate].Sparse!.Count, Is.EqualTo(2));
            Assert.That(sparse.Accessors[sparseCandidate].HasBufferView, Is.False);
            Assert.That(sparse.Accessors[denseCandidate].Sparse, Is.Null);
            Assert.That(sparse.Accessors[denseCandidate].HasBufferView, Is.True);
            Assert.That(sparseReport.Steps[0].ChangedObjects,
                Is.EqualTo(new[] { "accessors/0" }));
            Assert.That(source.Accessors[sparseCandidate].Sparse, Is.Null);
        });

        GltfDocument dense = Transform(
            sparse,
            GltfTransformKind.SparseToDense,
            new JsonObject()).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(dense.Accessors[sparseCandidate].Sparse, Is.Null);
            Assert.That(dense.Accessors[sparseCandidate].HasBufferView, Is.True);
            Assert.That(ReadUnsigned(dense, sparseCandidate),
                Is.EqualTo(new uint[] { 0, 7, 0, 9, 0 }));
        });
    }

    [Test]
    public void DenseToSparse_AppliesConfiguredZeroTolerance()
    {
        GltfDocument source = new GltfDocument();
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = source,
            ComponentType = GltfComponentType.Float,
            Type = GltfAccessorType.Scalar,
            FloatingValues = new double[] { 0.001, 0.0, 2.0, 0.0 }
        };
        int accessor = new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;

        GltfDocument result = Transform(
            source,
            GltfTransformKind.DenseToSparse,
            new JsonObject { ["zeroTolerance"] = 0.01 }).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Accessors[accessor].Sparse, Is.Not.Null);
            Assert.That(result.Accessors[accessor].Sparse!.Count, Is.EqualTo(1));
            Assert.That(ReadFloating(result, accessor),
                Is.EqualTo(new double[] { 0.0, 0.0, 2.0, 0.0 }));
        });
    }

    [Test]
    public void ConsolidateBuffers_PreservesBytesAndReportsReferenceRemaps()
    {
        GltfDocument source = CreateTwoBufferDocument();

        GltfTransformReport report = Transform(
            source,
            GltfTransformKind.ConsolidateBuffers,
            new JsonObject());
        GltfDocument result = report.Document!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Buffers, Has.Count.EqualTo(1));
            Assert.That(result.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(result.BufferViews[0].Buffer, Is.Zero);
            Assert.That(result.BufferViews[1].Buffer, Is.Zero);
            Assert.That(result.BufferViews[1].ByteOffset, Is.EqualTo(4));
            Assert.That(report.Steps[0].Remap.BufferViews[0], Is.Zero);
            Assert.That(report.Steps[0].Remap.BufferViews[1], Is.EqualTo(1));
            Assert.That(report.Steps[0].Remap.Buffers[0], Is.Zero);
            Assert.That(report.Steps[0].Remap.Buffers[1], Is.Zero);
            Assert.That(source.Buffers, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void PartitionBuffers_UsesWholeViewBoundariesAndConfiguredMaximum()
    {
        GltfDocument source = CreateThreeBufferDocument();

        GltfDocument result = Transform(
            source,
            GltfTransformKind.PartitionBuffers,
            new JsonObject { ["maximumBufferByteLength"] = 8 }).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Buffers, Has.Count.EqualTo(2));
            Assert.That(result.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(result.Buffers[1].ResolvedBytes,
                Is.EqualTo(new byte[] { 9, 10, 11, 12 }));
            Assert.That(result.BufferViews[0].Buffer, Is.Zero);
            Assert.That(result.BufferViews[1].Buffer, Is.Zero);
            Assert.That(result.BufferViews[2].Buffer, Is.EqualTo(1));
        });
    }

    [Test]
    public void PackBuffers_ProducesDeterministicLayout()
    {
        GltfDocument source = CreateTwoBufferDocument();

        GltfDocument first = Transform(
            source,
            GltfTransformKind.PackBuffers,
            new JsonObject()).Document!;
        GltfDocument second = Transform(
            source,
            GltfTransformKind.PackBuffers,
            new JsonObject()).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(second.Buffers, Has.Count.EqualTo(first.Buffers.Count));
            Assert.That(second.Buffers[0].ResolvedBytes,
                Is.EqualTo(first.Buffers[0].ResolvedBytes));
            Assert.That(second.BufferViews[0].ByteOffset,
                Is.EqualTo(first.BufferViews[0].ByteOffset));
            Assert.That(second.BufferViews[1].ByteOffset,
                Is.EqualTo(first.BufferViews[1].ByteOffset));
        });
    }

    [Test]
    public void EmbedAndExtractResources_UpdateBindingsAndPreserveOwnedBytes()
    {
        GltfDocument source = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer
        {
            Uri = "geometry.bin",
            ByteLength = 3,
            Resolved = true,
            ResolvedBytes = new byte[] { 1, 2, 3 }
        };
        source.Buffers.Add(buffer);
        GltfImage image = new GltfImage
        {
            Uri = "texture.png",
            MimeType = GltfMimeTypes.Png,
            ResolvedMimeType = GltfMimeTypes.Png,
            Resolved = true,
            ResolvedBytes = new byte[] { 4, 5, 6 }
        };
        source.Images.Add(image);

        GltfTransformReport embeddedReport = Transform(
            source,
            GltfTransformKind.EmbedResources,
            new JsonObject());
        GltfDocument embedded = embeddedReport.Document!;

        Assert.Multiple(() =>
        {
            Assert.That(embedded.Buffers[0].Uri,
                Is.EqualTo("data:application/gltf-buffer;base64,AQID"));
            Assert.That(embedded.Images[0].Uri,
                Is.EqualTo("data:image/png;base64,BAUG"));
            Assert.That(embedded.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(embedded.Resources.Resources[0].Embedded, Is.True);
            Assert.That(embedded.Resources.Resources[1].Embedded, Is.True);
            Assert.That(embedded.Buffers[0].HasResource, Is.True);
            Assert.That(embedded.Images[0].HasResource, Is.True);
            Assert.That(source.Buffers[0].HasResource, Is.False);
        });

        GltfDocument extracted = Transform(
            embedded,
            GltfTransformKind.ExtractResources,
            new JsonObject()).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(extracted.Buffers[0].Uri, Is.EqualTo("geometry.bin"));
            Assert.That(extracted.Images[0].Uri, Is.EqualTo("texture.png"));
            Assert.That(extracted.Resources.Resources[0].Embedded, Is.False);
            Assert.That(extracted.Resources.Resources[1].Embedded, Is.False);
            Assert.That(extracted.Resources.Resources[
                extracted.Buffers[0].Resource].Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(extracted.Resources.Resources[
                extracted.Images[0].Resource].Bytes,
                Is.EqualTo(new byte[] { 4, 5, 6 }));
        });
    }

    [Test]
    public void ExtractResources_ConvertsBufferViewImagesToExternalResources()
    {
        GltfDocument source = new GltfDocument();
        AddBufferAndView(source, new byte[] { 9, 8, 7, 6 });
        source.Images.Add(new GltfImage
        {
            HasBufferView = true,
            BufferView = 0,
            MimeType = GltfMimeTypes.Png
        });

        GltfDocument result = Transform(
            source,
            GltfTransformKind.ExtractResources,
            new JsonObject()).Document!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Images[0].HasBufferView, Is.False);
            Assert.That(result.Images[0].Uri, Is.EqualTo("image.png"));
            Assert.That(result.Images[0].HasResource, Is.True);
            Assert.That(result.Resources.Resources[
                result.Images[0].Resource].Bytes,
                Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
        });
    }

    [Test]
    public void StorageTransforms_RejectInvalidSettingsTransactionally()
    {
        GltfDocument source = CreateTwoBufferDocument();

        Assert.That(
            () => Transform(
                source,
                GltfTransformKind.PartitionBuffers,
                new JsonObject { ["maximumBufferByteLength"] = 0 }),
            Throws.ArgumentException.With.Message.Contains(
                "maximumBufferByteLength"));
        Assert.That(source.Buffers, Has.Count.EqualTo(2));
    }

    private static int CreateUnsignedAccessor(
        GltfDocument document,
        uint[] values)
    {
        GltfAccessorWriteRequest request = new GltfAccessorWriteRequest
        {
            Document = document,
            ComponentType = GltfComponentType.UnsignedShort,
            Type = GltfAccessorType.Scalar,
            UnsignedValues = values
        };
        return new GltfAccessorCodec().CreateDenseAccessor(request).Accessor;
    }

    private static uint[] ReadUnsigned(GltfDocument document, int accessor)
    {
        return new GltfAccessorCodec().ReadUnsignedValues(
            new GltfAccessorReadRequest
            {
                Document = document,
                Accessor = accessor
            }).UnsignedValues;
    }

    private static double[] ReadFloating(GltfDocument document, int accessor)
    {
        return new GltfAccessorCodec().ReadFloatingValues(
            new GltfAccessorReadRequest
            {
                Document = document,
                Accessor = accessor
            }).FloatingValues;
    }

    private static GltfDocument CreateTwoBufferDocument()
    {
        GltfDocument result = new GltfDocument();
        AddBufferAndView(result, new byte[] { 1, 2, 3, 4 });
        AddBufferAndView(result, new byte[] { 5, 6, 7, 8 });
        return result;
    }

    private static GltfDocument CreateThreeBufferDocument()
    {
        GltfDocument result = CreateTwoBufferDocument();
        AddBufferAndView(result, new byte[] { 9, 10, 11, 12 });
        return result;
    }

    private static void AddBufferAndView(GltfDocument document, byte[] bytes)
    {
        int bufferIndex = document.Buffers.Count;
        document.Buffers.Add(new GltfBuffer
        {
            ByteLength = bytes.Length,
            Resolved = true,
            ResolvedBytes = bytes
        });
        document.BufferViews.Add(new GltfBufferView
        {
            Buffer = bufferIndex,
            ByteLength = bytes.Length
        });
    }

    private static GltfTransformReport Transform(
        GltfDocument document,
        GltfTransformKind kind,
        JsonObject settings)
    {
        return new GltfTransformer().TransformDocument(
            document,
            new GltfTransformPlan
            {
                Steps = [new GltfTransformStep
                {
                    Kind = kind,
                    Settings = settings
                }]
            },
            new GltfTransformOptions
            {
                ValidateBefore = false,
                ValidateAfter = false
            });
    }
}
