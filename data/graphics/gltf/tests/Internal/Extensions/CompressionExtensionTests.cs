using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class CompressionExtensionTests
{
    [Test]
    public void DracoMetadata_ReadsWritesAndRemapsWithoutMutatingInput()
    {
        JsonObject json = new JsonObject
        {
            ["bufferView"] = 2,
            ["attributes"] = new JsonObject
            {
                ["POSITION"] = 7,
                ["NORMAL"] = 9
            },
            ["future"] = true
        };
        GltfExtensionContext context = new GltfExtensionContext
        {
            Name = GltfExtensionNames.KhrDracoMeshCompression,
            ObjectLocation = "mesh.primitive",
            ObjectIndex = 0,
            HasObjectIndex = true,
            SubObjectIndex = 1,
            HasSubObjectIndex = true,
            Json = json
        };
        GltfExtensionHandler handler = new GltfExtensionHandler();

        GltfExtensionResult read = handler.ReadExtension(context);
        GltfDracoMeshCompressionExtension value = read.TypedData!.DracoMeshCompression!;
        context.TypedData = read.TypedData;
        GltfExtensionResult written = handler.WriteExtension(context);
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.BufferViews[2] = 5;
        GltfExtensionResult remapped = handler.RemapExtensionReferences(context, remap);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(value.BufferView, Is.EqualTo(2));
            Assert.That(value.Attributes, Is.EqualTo(new Dictionary<string, int>
            {
                ["POSITION"] = 7,
                ["NORMAL"] = 9
            }));
            Assert.That(value.PropertyJson.UnknownMembers["future"]!.GetValue<bool>(), Is.True);
            Assert.That(written.Json!["future"]!.GetValue<bool>(), Is.True);
            Assert.That(remapped.Json!["bufferView"]!.GetValue<int>(), Is.EqualTo(5));
            Assert.That(remapped.References.Single().TargetCollection, Is.EqualTo("bufferViews"));
            Assert.That(value.BufferView, Is.EqualTo(2));
        });
    }

    [TestCase(GltfExtensionNames.KhrMeshoptCompression)]
    [TestCase(GltfExtensionNames.ExtMeshoptCompression)]
    public void MeshoptMetadata_ReadsWritesAndRemapsBothExtensionVersions(string extension)
    {
        JsonObject json = new JsonObject
        {
            ["buffer"] = 1,
            ["byteOffset"] = 2,
            ["byteLength"] = 6,
            ["byteStride"] = 4,
            ["count"] = 3,
            ["mode"] = "TRIANGLES",
            ["vendor"] = "kept"
        };
        GltfExtensionContext context = new GltfExtensionContext
        {
            Name = extension,
            ObjectLocation = "bufferView",
            ObjectIndex = 0,
            HasObjectIndex = true,
            Json = json
        };
        GltfExtensionHandler handler = new GltfExtensionHandler();

        GltfExtensionResult read = handler.ReadExtension(context);
        context.TypedData = read.TypedData;
        GltfExtensionResult written = handler.WriteExtension(context);
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.Buffers[1] = 4;
        GltfExtensionResult remapped = handler.RemapExtensionReferences(context, remap);

        GltfMeshoptCompressionBufferViewExtension? value =
            read.TypedData!.KhrMeshoptCompressionBufferView;
        if (value == null)
        {
            value = read.TypedData.ExtMeshoptCompressionBufferView;
        }

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(value, Is.Not.Null);
            Assert.That(value!.Buffer, Is.EqualTo(1));
            Assert.That(value.ByteOffset, Is.EqualTo(2));
            Assert.That(value.ByteLength, Is.EqualTo(6));
            Assert.That(value.ByteStride, Is.EqualTo(4));
            Assert.That(value.Count, Is.EqualTo(3));
            Assert.That(value.Mode, Is.EqualTo("TRIANGLES"));
            Assert.That(written.Json!["vendor"]!.GetValue<string>(), Is.EqualTo("kept"));
            Assert.That(remapped.Json!["buffer"]!.GetValue<int>(), Is.EqualTo(4));
            Assert.That(remapped.References.Single().TargetCollection, Is.EqualTo("buffers"));
            Assert.That(value.Buffer, Is.EqualTo(1));
        });
    }

    [Test]
    public void Validation_EnforcesMeshoptFallbackConflictAndVersionedFilterRules()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(new GltfBuffer { ByteLength = 4 });
        document.Buffers.Add(new GltfBuffer { ByteLength = 8, Uri = "compressed.bin" });
        GltfBufferView parent = new GltfBufferView { Buffer = 0, ByteLength = 4 };
        parent.PropertyJson.Extensions[GltfExtensionNames.ExtMeshoptCompression] = new JsonObject();
        document.BufferViews.Add(parent);
        GltfMeshoptCompressionBufferViewExtension value = new GltfMeshoptCompressionBufferViewExtension
        {
            Buffer = 1,
            ByteLength = 8,
            ByteStride = 4,
            Count = 1,
            Mode = "ATTRIBUTES",
            HasFilter = true,
            Filter = "COLOR"
        };
        GltfTypedExtensionData typed = new GltfTypedExtensionData
        {
            KhrMeshoptCompressionBufferView = value
        };
        GltfExtensionContext context = new GltfExtensionContext
        {
            Document = document,
            Name = GltfExtensionNames.KhrMeshoptCompression,
            ObjectLocation = "bufferView",
            HasObjectIndex = true,
            ObjectIndex = 0,
            TypedData = typed
        };

        GltfExtensionResult khr = new GltfExtensionHandler().ValidateExtension(context);
        context.Name = GltfExtensionNames.ExtMeshoptCompression;
        typed.KhrMeshoptCompressionBufferView = null;
        typed.ExtMeshoptCompressionBufferView = value;
        parent.PropertyJson.Extensions.Clear();
        GltfExtensionResult ext = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(khr.Diagnostics.Any(issue =>
                issue.Message.Contains("cannot be used on the same bufferView", StringComparison.Ordinal)),
                Is.True);
            Assert.That(khr.Diagnostics.Any(issue => issue.JsonPointer == "/extensionsRequired"),
                Is.True);
            Assert.That(khr.Diagnostics.Any(issue =>
                issue.Message.Contains("filter", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(ext.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.ValueNotInList
                && issue.JsonPointer.EndsWith("/filter", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void Validation_EnforcesRequiredDracoAccessorAndPrimitiveRules()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsRequired.Add(GltfExtensionNames.KhrDracoMeshCompression);
        document.BufferViews.Add(new GltfBufferView());
        document.Accessors.Add(new GltfAccessor { HasBufferView = true, BufferView = 0 });
        document.Accessors.Add(new GltfAccessor());
        GltfMeshPrimitive primitive = new GltfMeshPrimitive
        {
            Mode = GltfPrimitiveMode.Lines
        };
        primitive.Attributes.Add(new GltfAttribute { Semantic = "POSITION", Accessor = 0 });
        primitive.Attributes.Add(new GltfAttribute { Semantic = "NORMAL", Accessor = 1 });
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        GltfDracoMeshCompressionExtension value = new GltfDracoMeshCompressionExtension
        {
            BufferView = 0
        };
        value.Attributes["POSITION"] = 0;
        GltfExtensionContext context = new GltfExtensionContext
        {
            Document = document,
            Name = GltfExtensionNames.KhrDracoMeshCompression,
            ObjectLocation = "mesh.primitive",
            HasObjectIndex = true,
            ObjectIndex = 0,
            HasSubObjectIndex = true,
            SubObjectIndex = 0,
            TypedData = new GltfTypedExtensionData { DracoMeshCompression = value }
        };

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Diagnostics.Any(issue => issue.Code == GltfIssueCodes.ValueNotInList),
                Is.True);
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Message.Contains("must not contain fallback", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Message.Contains("every primitive attribute", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void Dispatcher_TriesUnavailableCodecsAndRejectsOversizedOutput()
    {
        ProbeCompressionCodec unavailable = new ProbeCompressionCodec
        {
            GeometryStatus = GltfOperationStatus.Unavailable
        };
        ProbeCompressionCodec success = new ProbeCompressionCodec
        {
            GeometryBytes = new byte[] { 1, 2, 3 }
        };
        GltfCompressionRequest request = new GltfCompressionRequest
        {
            Codec = GltfExtensionNames.KhrDracoMeshCompression,
            MaximumOutputByteLength = 3
        };
        CompressionCodecDispatcher dispatcher = new CompressionCodecDispatcher();

        GltfCompressionResult result = dispatcher.DecodeGeometry(
            request,
            new List<IGltfCompressionCodec> { unavailable, success });
        GltfCompressionResult encodedGeometry = dispatcher.EncodeGeometry(
            request,
            new List<IGltfCompressionCodec> { unavailable, success });
        success.MeshoptBytes = new byte[] { 4, 5 };
        GltfCompressionResult encodedMeshopt = dispatcher.EncodeMeshopt(
            request,
            new List<IGltfCompressionCodec> { unavailable, success });
        request.MaximumOutputByteLength = 2;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(unavailable.GeometryDecodeCalls, Is.EqualTo(1));
            Assert.That(success.GeometryDecodeCalls, Is.EqualTo(1));
            Assert.That(encodedGeometry.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(encodedMeshopt.Bytes, Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(unavailable.GeometryEncodeCalls, Is.EqualTo(1));
            Assert.That(success.GeometryEncodeCalls, Is.EqualTo(1));
            Assert.That(unavailable.MeshoptEncodeCalls, Is.EqualTo(1));
            Assert.That(success.MeshoptEncodeCalls, Is.EqualTo(1));
            Assert.Throws<GltfResourceException>(() => dispatcher.DecodeGeometry(
                request,
                new List<IGltfCompressionCodec> { success }));
        });
    }

    [TestCase(GltfExtensionNames.KhrMeshoptCompression)]
    [TestCase(GltfExtensionNames.ExtMeshoptCompression)]
    public void Reader_DecodesRequiredMeshoptFallbackThroughRegisteredCodec(
        string extension)
    {
        string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"" + extension + "\"],"
            + "\"extensionsRequired\":[\"" + extension + "\"],"
            + "\"buffers\":[{\"byteLength\":4},{\"byteLength\":2,"
            + "\"uri\":\"data:application/octet-stream;base64,AQI=\"}],"
            + "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4,\"extensions\":{"
            + "\"" + extension + "\":{\"buffer\":1,\"byteLength\":2,"
            + "\"byteStride\":4,\"count\":1,\"mode\":\"ATTRIBUTES\"}}}],"
            + "\"accessors\":[{\"bufferView\":0,\"componentType\":5126,"
            + "\"count\":1,\"type\":\"SCALAR\"}]}";
        ProbeCompressionCodec codec = new ProbeCompressionCodec
        {
            MeshoptBytes = BitConverter.GetBytes(1.5F)
        };
        GltfReadOptions options = new GltfReadOptions { ResolveResources = true };
        options.CompressionCodecs.Add(codec);

        GltfReadResult read = new GltfReader().ReadBytes(Encoding.UTF8.GetBytes(json), options);
        codec.MeshoptBytes[2] = 0;
        GltfAccessorData data = new GltfAccessorCodec().ReadFloatingValues(
            new GltfAccessorReadRequest { Document = read.Document, Accessor = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(codec.MeshoptDecodeCalls, Is.EqualTo(1));
            Assert.That(codec.LastRequest!.Codec, Is.EqualTo(extension));
            Assert.That(codec.LastRequest!.Meshopt, Is.Not.Null);
            Assert.That(codec.LastRequest.Bytes, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(read.Document!.Buffers[0].Resolved, Is.True);
            Assert.That(data.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(data.FloatingValues, Is.EqualTo(new[] { 1.5 }));
        });
    }

    [TestCase(GltfExtensionNames.KhrMeshoptCompression)]
    [TestCase(GltfExtensionNames.ExtMeshoptCompression)]
    public void Reader_FailedMeshoptDecodeDoesNotPublishOrMutateCodecData(
        string extension)
    {
        string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"" + extension + "\"],"
            + "\"extensionsRequired\":[\"" + extension + "\"],"
            + "\"buffers\":[{\"byteLength\":4},{\"byteLength\":2,"
            + "\"uri\":\"data:application/octet-stream;base64,AQI=\"}],"
            + "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4,\"extensions\":{"
            + "\"" + extension + "\":{\"buffer\":1,\"byteLength\":2,"
            + "\"byteStride\":4,\"count\":1,\"mode\":\"ATTRIBUTES\"}}}]}";
        GltfValidationIssue codecIssue = new GltfValidationIssue
        {
            Code = "TEST_CODEC_FAILURE",
            Message = "The test codec failed."
        };
        ProbeCompressionCodec codec = new ProbeCompressionCodec
        {
            GeometryStatus = GltfOperationStatus.Failed,
            MeshoptBytes = new byte[] { 7, 8, 9, 10 },
            MutateRequestBytes = true
        };
        codec.Diagnostics.Add(codecIssue);
        GltfReadOptions options = new GltfReadOptions { ResolveResources = true };
        options.CompressionCodecs.Add(codec);

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Failed));
            Assert.That(read.Document!.Buffers[0].Resolved, Is.False);
            Assert.That(read.Document.Buffers[0].ResolvedBytes, Is.Empty);
            Assert.That(read.Document.Buffers[1].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(codec.MeshoptBytes, Is.EqualTo(new byte[] { 7, 8, 9, 10 }));
            Assert.That(codecIssue.JsonPointer, Is.Empty);
            Assert.That(read.Diagnostics.Single(issue =>
                issue.Code == "TEST_CODEC_FAILURE").JsonPointer,
                Is.EqualTo("/bufferViews/0/extensions/" + extension));
            Assert.That(read.Diagnostics.Single(issue =>
                issue.Code == "TEST_CODEC_FAILURE"), Is.Not.SameAs(codecIssue));
        });
    }

    [Test]
    public void Reader_DecodesDracoAccessorStreamsThroughRegisteredCodec()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"KHR_draco_mesh_compression\"],"
            + "\"extensionsRequired\":[\"KHR_draco_mesh_compression\"],"
            + "\"buffers\":[{\"byteLength\":2,"
            + "\"uri\":\"data:application/octet-stream;base64,AQI=\"}],"
            + "\"bufferViews\":[{\"buffer\":0,\"byteLength\":2}],"
            + "\"accessors\":[{\"componentType\":5126,\"count\":1,\"type\":\"VEC3\"}],"
            + "\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0},"
            + "\"extensions\":{\"KHR_draco_mesh_compression\":{\"bufferView\":0,"
            + "\"attributes\":{\"POSITION\":3}}}}]}]}";
        ProbeCompressionCodec codec = new ProbeCompressionCodec();
        codec.GeometryAttributes["POSITION"] = JoinFloats(1.0F, 2.0F, 3.0F);
        GltfReadOptions options = new GltfReadOptions { ResolveResources = true };
        options.CompressionCodecs.Add(codec);

        GltfReadResult read = new GltfReader().ReadBytes(Encoding.UTF8.GetBytes(json), options);
        codec.GeometryAttributes["POSITION"][2] = 0;
        GltfAccessorData data = new GltfAccessorCodec().ReadFloatingValues(
            new GltfAccessorReadRequest { Document = read.Document, Accessor = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(codec.GeometryDecodeCalls, Is.EqualTo(1));
            Assert.That(codec.LastRequest!.HasMesh, Is.True);
            Assert.That(codec.LastRequest.Mesh, Is.Zero);
            Assert.That(codec.LastRequest.HasPrimitive, Is.True);
            Assert.That(codec.LastRequest.Primitive, Is.Zero);
            Assert.That(codec.LastRequest.Draco!.Attributes["POSITION"], Is.EqualTo(3));
            Assert.That(data.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(data.FloatingValues, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
        });
    }

    [Test]
    public void Reader_FailedDracoDecodeDoesNotPublishOrMutateCodecData()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"KHR_draco_mesh_compression\"],"
            + "\"extensionsRequired\":[\"KHR_draco_mesh_compression\"],"
            + "\"buffers\":[{\"byteLength\":2,"
            + "\"uri\":\"data:application/octet-stream;base64,AQI=\"}],"
            + "\"bufferViews\":[{\"buffer\":0,\"byteLength\":2}],"
            + "\"accessors\":[{\"componentType\":5126,\"count\":1,\"type\":\"VEC3\"}],"
            + "\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0},"
            + "\"extensions\":{\"KHR_draco_mesh_compression\":{\"bufferView\":0,"
            + "\"attributes\":{\"POSITION\":3}}}}]}]}";
        GltfValidationIssue codecIssue = new GltfValidationIssue
        {
            Code = "TEST_DRACO_FAILURE",
            Message = "The test Draco codec failed."
        };
        ProbeCompressionCodec codec = new ProbeCompressionCodec
        {
            GeometryStatus = GltfOperationStatus.Failed,
            MutateRequestBytes = true
        };
        codec.GeometryAttributes["POSITION"] = JoinFloats(1.0F, 2.0F, 3.0F);
        codec.Diagnostics.Add(codecIssue);
        GltfReadOptions options = new GltfReadOptions { ResolveResources = true };
        options.CompressionCodecs.Add(codec);

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Failed));
            Assert.That(read.Resources.Cache.ContainsKey("compression:accessor:0"), Is.False);
            Assert.That(read.Document!.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(codec.GeometryAttributes["POSITION"],
                Is.EqualTo(JoinFloats(1.0F, 2.0F, 3.0F)));
            Assert.That(codecIssue.JsonPointer, Is.Empty);
            Assert.That(read.Diagnostics.Single(issue =>
                issue.Code == "TEST_DRACO_FAILURE").JsonPointer,
                Is.EqualTo("/meshes/0/primitives/0/extensions/KHR_draco_mesh_compression"));
        });
    }

    private static byte[] JoinFloats(float first, float second, float third)
    {
        byte[] result = new byte[12];
        BitConverter.GetBytes(first).CopyTo(result, 0);
        BitConverter.GetBytes(second).CopyTo(result, 4);
        BitConverter.GetBytes(third).CopyTo(result, 8);
        return result;
    }

    private class ProbeCompressionCodec : IGltfCompressionCodec
    {
        public GltfOperationStatus GeometryStatus = GltfOperationStatus.Success;
        public byte[] GeometryBytes = [];
        public Dictionary<string, byte[]> GeometryAttributes = new Dictionary<string, byte[]>();
        public byte[] MeshoptBytes = [];
        public int GeometryDecodeCalls;
        public int GeometryEncodeCalls;
        public int MeshoptDecodeCalls;
        public int MeshoptEncodeCalls;
        public GltfCompressionRequest? LastRequest;
        public bool MutateRequestBytes;
        public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();

        public GltfCompressionResult DecodeGeometry(GltfCompressionRequest request)
        {
            GeometryDecodeCalls++;
            LastRequest = request;
            if (MutateRequestBytes && request.Bytes.Length > 0)
            {
                request.Bytes[0] = 99;
            }

            return new GltfCompressionResult
            {
                Status = GeometryStatus,
                Bytes = GeometryBytes,
                Attributes = GeometryAttributes,
                Diagnostics = Diagnostics
            };
        }

        public GltfCompressionResult EncodeGeometry(GltfCompressionRequest request)
        {
            GeometryEncodeCalls++;
            return new GltfCompressionResult
            {
                Status = GeometryStatus,
                Bytes = GeometryBytes
            };
        }

        public GltfCompressionResult DecodeMeshoptData(GltfCompressionRequest request)
        {
            MeshoptDecodeCalls++;
            LastRequest = request;
            if (MutateRequestBytes && request.Bytes.Length > 0)
            {
                request.Bytes[0] = 99;
            }

            return new GltfCompressionResult
            {
                Status = GeometryStatus,
                Bytes = MeshoptBytes,
                Diagnostics = Diagnostics
            };
        }

        public GltfCompressionResult EncodeMeshoptData(GltfCompressionRequest request)
        {
            MeshoptEncodeCalls++;
            return new GltfCompressionResult
            {
                Status = GeometryStatus,
                Bytes = MeshoptBytes
            };
        }
    }
}
