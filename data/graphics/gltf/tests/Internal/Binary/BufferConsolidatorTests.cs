using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Binary;

[TestFixture]
public class BufferConsolidatorTests
{
    [Test]
    public void ConsolidateBuffers_RemapsCoreAndCompressionReferences()
    {
        GltfDocument document = CreateReferenceDocument();
        GltfBufferView originalArrayView = document.BufferViews[2];

        BufferConsolidationResult result = new BufferConsolidator()
            .ConsolidateBuffers(document, new BufferConsolidationOptions());

        JsonObject draco = (JsonObject)document.Meshes[0].Primitives[0]
            .PropertyJson.Extensions[GltfExtensionNames.KhrDracoMeshCompression]!;
        JsonObject khrMeshopt = (JsonObject)originalArrayView
            .PropertyJson.Extensions[GltfExtensionNames.KhrMeshoptCompression]!;
        JsonObject extMeshopt = (JsonObject)originalArrayView
            .PropertyJson.Extensions[GltfExtensionNames.ExtMeshoptCompression]!;
        JsonObject lights = (JsonObject)document.PropertyJson.Extensions[
            GltfExtensionNames.ExtLightsIes]!;
        JsonObject light = (JsonObject)((JsonArray)lights["lights"]!)[0]!;
        JsonObject mdl = (JsonObject)document.PropertyJson.Extensions[
            GltfExtensionNames.NvMaterialsMdl]!;
        JsonObject mdlModule = (JsonObject)((JsonArray)mdl["modules"]!)[0]!;
        JsonObject mdlMeasurement = (JsonObject)((JsonArray)mdl[
            "bsdfMeasurements"]!)[0]!;
        JsonObject techniques = (JsonObject)document.PropertyJson.Extensions[
            GltfExtensionNames.KhrTechniquesWebgl]!;
        JsonObject shader = (JsonObject)((JsonArray)techniques["shaders"]!)[0]!;
        JsonObject timed = (JsonObject)document.Accessors[0].PropertyJson.Extensions[
            GltfExtensionNames.MpegAccessorTimed]!;
        JsonObject bim = (JsonObject)document.Nodes[0].PropertyJson.Extensions[
            GltfExtensionNames.GriffelBimData]!;
        Assert.Multiple(() =>
        {
            Assert.That(result.OriginalBufferCount, Is.EqualTo(2));
            Assert.That(result.ResultBufferCount, Is.EqualTo(1));
            Assert.That(result.BufferViews, Is.EqualTo(new int[] { 2, 0, 1 }));
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(3));
            Assert.That(document.BufferViews[1], Is.SameAs(originalArrayView));
            Assert.That(document.BufferViews[1].HasByteStride, Is.True);
            Assert.That(document.BufferViews[1].ByteStride, Is.EqualTo(4));
            Assert.That(document.Accessors[0].BufferView, Is.EqualTo(1));
            Assert.That(document.Accessors[1].Sparse!.Indices.BufferView, Is.EqualTo(2));
            Assert.That(document.Accessors[1].Sparse!.Values.BufferView, Is.EqualTo(1));
            Assert.That(document.Images[0].BufferView, Is.EqualTo(0));
            Assert.That(draco["bufferView"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(khrMeshopt["buffer"]!.GetValue<int>(), Is.EqualTo(0));
            Assert.That(khrMeshopt["byteOffset"]!.GetValue<int>(), Is.EqualTo(4));
            Assert.That(extMeshopt["buffer"]!.GetValue<int>(), Is.EqualTo(0));
            Assert.That(extMeshopt["byteOffset"]!.GetValue<int>(), Is.EqualTo(8));
            Assert.That(light["bufferView"]!.GetValue<int>(), Is.EqualTo(0));
            Assert.That(mdlModule["bufferView"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(mdlMeasurement["bufferView"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(shader["bufferView"]!.GetValue<int>(), Is.EqualTo(0));
            Assert.That(timed["bufferView"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(bim["bufferView"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(
                document.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[]
                {
                    10, 11, 12, 0,
                    40, 41, 42, 0,
                    50, 51, 0, 0,
                    20, 21, 22, 23, 24, 25, 26, 27,
                    90, 91
                }));
        });
    }

    [Test]
    public void ConsolidateBuffers_PartitionsAtPackedViewBoundaries()
    {
        GltfDocument document = new GltfDocument();
        AddBuffer(document, new byte[] { 1, 2, 3, 4 });
        AddBuffer(document, new byte[] { 5, 6, 7, 8 });
        AddBuffer(document, new byte[] { 9, 10, 11, 12 });
        AddView(document, 0, 0, 4, GltfBufferViewTarget.None);
        AddView(document, 1, 0, 4, GltfBufferViewTarget.None);
        AddView(document, 2, 0, 4, GltfBufferViewTarget.None);
        BufferConsolidationOptions options = new BufferConsolidationOptions();
        options.MaximumBufferByteLength = 8;
        options.Partition = true;

        BufferConsolidationResult result = new BufferConsolidator()
            .ConsolidateBuffers(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResultBufferCount, Is.EqualTo(2));
            Assert.That(document.Buffers[0].ResolvedBytes, Is.EqualTo(new byte[]
            {
                1, 2, 3, 4, 5, 6, 7, 8
            }));
            Assert.That(document.Buffers[1].ResolvedBytes, Is.EqualTo(new byte[]
            {
                9, 10, 11, 12
            }));
            Assert.That(document.BufferViews[0].Buffer, Is.EqualTo(0));
            Assert.That(document.BufferViews[1].Buffer, Is.EqualTo(0));
            Assert.That(document.BufferViews[1].ByteOffset, Is.EqualTo(4));
            Assert.That(document.BufferViews[2].Buffer, Is.EqualTo(1));
            Assert.That(document.BufferViews[2].ByteOffset, Is.EqualTo(0));
        });
    }

    [Test]
    public void ConsolidateBuffers_RemovesResolvedBuffersWithoutViews()
    {
        GltfDocument document = new GltfDocument();
        AddBuffer(document, new byte[] { 1, 2, 3 });

        BufferConsolidationResult result = new BufferConsolidator()
            .ConsolidateBuffers(document, new BufferConsolidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.OriginalBufferCount, Is.EqualTo(1));
            Assert.That(result.ResultBufferCount, Is.EqualTo(0));
            Assert.That(document.Buffers, Is.Empty);
            Assert.That(document.BufferViews, Is.Empty);
        });
    }

    [Test]
    public void ConsolidateBuffers_PreservesDocumentWhenPackingLimitFails()
    {
        GltfDocument document = new GltfDocument();
        AddBuffer(document, new byte[] { 1, 2, 3, 4 });
        AddBuffer(document, new byte[] { 5, 6, 7, 8 });
        AddView(document, 0, 0, 4, GltfBufferViewTarget.None);
        AddView(document, 1, 0, 4, GltfBufferViewTarget.None);
        List<GltfBuffer> originalBuffers = document.Buffers;
        List<GltfBufferView> originalViews = document.BufferViews;
        BufferConsolidationOptions options = new BufferConsolidationOptions();
        options.MaximumBufferByteLength = 7;

        Assert.That(
            () => new BufferConsolidator().ConsolidateBuffers(document, options),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
        Assert.Multiple(() =>
        {
            Assert.That(document.Buffers, Is.SameAs(originalBuffers));
            Assert.That(document.BufferViews, Is.SameAs(originalViews));
            Assert.That(document.BufferViews[1].Buffer, Is.EqualTo(1));
            Assert.That(document.BufferViews[1].ByteOffset, Is.EqualTo(0));
        });
    }

    [Test]
    public void ConsolidateBuffers_PreservesDocumentWhenAReferenceIsInvalid()
    {
        GltfDocument document = new GltfDocument();
        AddBuffer(document, new byte[] { 1, 2, 3, 4 });
        AddView(document, 0, 0, 4, GltfBufferViewTarget.None);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 4;
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Count = 1;
        accessor.Type = GltfAccessorType.Scalar;
        document.Accessors.Add(accessor);
        List<GltfBuffer> originalBuffers = document.Buffers;
        List<GltfBufferView> originalViews = document.BufferViews;

        Assert.That(
            () => new BufferConsolidator().ConsolidateBuffers(
                document,
                new BufferConsolidationOptions()),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("document"));
        Assert.Multiple(() =>
        {
            Assert.That(document.Buffers, Is.SameAs(originalBuffers));
            Assert.That(document.BufferViews, Is.SameAs(originalViews));
            Assert.That(accessor.BufferView, Is.EqualTo(4));
        });
    }

    [Test]
    public void ConsolidateBuffers_RejectsUnresolvedOrOutOfRangePayloads()
    {
        GltfDocument unresolved = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 4;
        unresolved.Buffers.Add(buffer);
        AddView(unresolved, 0, 0, 4, GltfBufferViewTarget.None);

        GltfDocument outOfRange = new GltfDocument();
        AddBuffer(outOfRange, new byte[] { 1, 2, 3, 4 });
        AddView(outOfRange, 0, 2, 4, GltfBufferViewTarget.None);

        GltfDocument invalidExtension = new GltfDocument();
        AddBuffer(invalidExtension, new byte[] { 1, 2, 3, 4 });
        AddView(invalidExtension, 0, 0, 4, GltfBufferViewTarget.None);
        GltfNode node = new GltfNode();
        JsonObject bim = new JsonObject();
        bim["bufferView"] = -1;
        node.PropertyJson.Extensions[GltfExtensionNames.GriffelBimData] = bim;
        invalidExtension.Nodes.Add(node);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new BufferConsolidator().ConsolidateBuffers(
                    unresolved,
                    new BufferConsolidationOptions()),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => new BufferConsolidator().ConsolidateBuffers(
                    outOfRange,
                    new BufferConsolidationOptions()),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("document"));
            Assert.That(
                () => new BufferConsolidator().ConsolidateBuffers(
                    invalidExtension,
                    new BufferConsolidationOptions()),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("document"));
        });
    }

    [Test]
    public void ConsolidateBuffers_RejectsPartitioningThatBreaksTimedAccessorColocation()
    {
        GltfDocument document = new GltfDocument();
        AddBuffer(document, new byte[]
        {
            1, 2, 3, 4,
            5, 6, 7, 8
        });
        AddView(document, 0, 0, 4, GltfBufferViewTarget.None);
        AddView(document, 0, 4, 4, GltfBufferViewTarget.ArrayBuffer);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 1;
        accessor.ComponentType = GltfComponentType.Float;
        accessor.Count = 1;
        accessor.Type = GltfAccessorType.Scalar;
        JsonObject timed = new JsonObject();
        timed["bufferView"] = 0;
        accessor.PropertyJson.Extensions[GltfExtensionNames.MpegAccessorTimed] = timed;
        document.Accessors.Add(accessor);
        List<GltfBuffer> originalBuffers = document.Buffers;
        List<GltfBufferView> originalViews = document.BufferViews;
        BufferConsolidationOptions options = new BufferConsolidationOptions();
        options.MaximumBufferByteLength = 4;
        options.Partition = true;

        Assert.That(
            () => new BufferConsolidator().ConsolidateBuffers(document, options),
            Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
        Assert.Multiple(() =>
        {
            Assert.That(document.Buffers, Is.SameAs(originalBuffers));
            Assert.That(document.BufferViews, Is.SameAs(originalViews));
            Assert.That(accessor.BufferView, Is.EqualTo(1));
            Assert.That(timed["bufferView"]!.GetValue<int>(), Is.EqualTo(0));
        });
    }

    private static GltfDocument CreateReferenceDocument()
    {
        GltfDocument document = new GltfDocument();
        byte[] firstBuffer = new byte[16];
        firstBuffer[0] = 90;
        firstBuffer[1] = 91;
        firstBuffer[4] = 40;
        firstBuffer[5] = 41;
        firstBuffer[6] = 42;
        firstBuffer[8] = 50;
        firstBuffer[9] = 51;
        AddBuffer(document, firstBuffer);
        byte[] secondBuffer = new byte[16];
        secondBuffer[4] = 10;
        secondBuffer[5] = 11;
        secondBuffer[6] = 12;
        secondBuffer[8] = 20;
        secondBuffer[9] = 21;
        secondBuffer[10] = 22;
        secondBuffer[11] = 23;
        secondBuffer[12] = 24;
        secondBuffer[13] = 25;
        secondBuffer[14] = 26;
        secondBuffer[15] = 27;
        AddBuffer(document, secondBuffer);

        AddView(document, 0, 0, 2, GltfBufferViewTarget.ElementArrayBuffer);
        AddView(document, 1, 4, 3, GltfBufferViewTarget.None);
        AddView(document, 1, 8, 8, GltfBufferViewTarget.ArrayBuffer);
        document.BufferViews[2].HasByteStride = true;
        document.BufferViews[2].ByteStride = 4;

        JsonObject khrMeshopt = new JsonObject();
        khrMeshopt["buffer"] = 0;
        khrMeshopt["byteOffset"] = 4;
        khrMeshopt["byteLength"] = 3;
        document.BufferViews[2].PropertyJson.Extensions[
            GltfExtensionNames.KhrMeshoptCompression] = khrMeshopt;
        JsonObject extMeshopt = new JsonObject();
        extMeshopt["buffer"] = 0;
        extMeshopt["byteOffset"] = 8;
        extMeshopt["byteLength"] = 2;
        document.BufferViews[2].PropertyJson.Extensions[
            GltfExtensionNames.ExtMeshoptCompression] = extMeshopt;

        JsonObject lights = new JsonObject();
        JsonArray lightProfiles = new JsonArray();
        JsonObject light = new JsonObject();
        light["bufferView"] = 1;
        lightProfiles.Add(light);
        lights["lights"] = lightProfiles;
        document.PropertyJson.Extensions[GltfExtensionNames.ExtLightsIes] = lights;
        JsonObject mdl = new JsonObject();
        JsonArray modules = new JsonArray();
        JsonObject module = new JsonObject();
        module["bufferView"] = 2;
        modules.Add(module);
        mdl["modules"] = modules;
        JsonArray measurements = new JsonArray();
        JsonObject measurement = new JsonObject();
        measurement["bufferView"] = 0;
        measurements.Add(measurement);
        mdl["bsdfMeasurements"] = measurements;
        document.PropertyJson.Extensions[GltfExtensionNames.NvMaterialsMdl] = mdl;
        JsonObject techniques = new JsonObject();
        JsonArray shaders = new JsonArray();
        JsonObject shader = new JsonObject();
        shader["bufferView"] = 1;
        shaders.Add(shader);
        techniques["shaders"] = shaders;
        document.PropertyJson.Extensions[
            GltfExtensionNames.KhrTechniquesWebgl] = techniques;

        GltfAccessor dense = new GltfAccessor();
        dense.HasBufferView = true;
        dense.BufferView = 2;
        dense.ComponentType = GltfComponentType.Float;
        dense.Count = 2;
        dense.Type = GltfAccessorType.Scalar;
        JsonObject timed = new JsonObject();
        timed["bufferView"] = 0;
        dense.PropertyJson.Extensions[GltfExtensionNames.MpegAccessorTimed] = timed;
        document.Accessors.Add(dense);
        GltfAccessor sparse = new GltfAccessor();
        sparse.ComponentType = GltfComponentType.UnsignedByte;
        sparse.Count = 2;
        sparse.Type = GltfAccessorType.Scalar;
        sparse.Sparse = new GltfAccessorSparse();
        sparse.Sparse.Count = 2;
        sparse.Sparse.Indices.BufferView = 0;
        sparse.Sparse.Indices.ComponentType = GltfComponentType.UnsignedByte;
        sparse.Sparse.Values.BufferView = 2;
        document.Accessors.Add(sparse);
        GltfImage image = new GltfImage();
        image.HasBufferView = true;
        image.BufferView = 1;
        image.MimeType = GltfMimeTypes.Png;
        document.Images.Add(image);

        GltfNode node = new GltfNode();
        JsonObject bim = new JsonObject();
        bim["bufferView"] = 2;
        node.PropertyJson.Extensions[GltfExtensionNames.GriffelBimData] = bim;
        document.Nodes.Add(node);

        GltfMesh mesh = new GltfMesh();
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        JsonObject draco = new JsonObject();
        draco["bufferView"] = 0;
        draco["attributes"] = new JsonObject();
        primitive.PropertyJson.Extensions[
            GltfExtensionNames.KhrDracoMeshCompression] = draco;
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        return document;
    }

    private static void AddBuffer(GltfDocument document, byte[] bytes)
    {
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = bytes.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = bytes;
        document.Buffers.Add(buffer);
    }

    private static void AddView(
        GltfDocument document,
        int buffer,
        int byteOffset,
        int byteLength,
        GltfBufferViewTarget target)
    {
        GltfBufferView view = new GltfBufferView();
        view.Buffer = buffer;
        if (byteOffset != 0)
        {
            view.HasByteOffset = true;
            view.ByteOffset = byteOffset;
        }

        view.ByteLength = byteLength;
        if (target != GltfBufferViewTarget.None)
        {
            view.HasTarget = true;
            view.Target = target;
        }

        document.BufferViews.Add(view);
    }
}
