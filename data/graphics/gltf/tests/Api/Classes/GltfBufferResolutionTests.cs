using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfBufferResolutionTests
{
    [Test]
    public void ReadContainer_BindsOwnedDeclaredBytesFromGlbBinAndPreservesPadding()
    {
        GltfContainerData container = CreateContainer(
            GltfContainerFormat.Glb,
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":3}]}",
            [1, 2, 3, 0]);

        GltfReadResult result = new GltfReader().ReadContainer(container, new GltfReadOptions());
        GltfBuffer buffer = result.Document!.Buffers[0];
        GltfResource resource = result.Resources.Resources[0];
        container.BinBytes[0] = 8;
        resource.Bytes[1] = 8;
        buffer.ResolvedBytes[2] = 8;

        Assert.Multiple(() =>
        {
            Assert.That(buffer.Resolved, Is.True);
            Assert.That(buffer.HasResource, Is.True);
            Assert.That(buffer.Resource, Is.Zero);
            Assert.That(buffer.PendingResourceRequest, Is.Null);
            Assert.That(buffer.ResolvedBytes, Is.EqualTo(new byte[] { 1, 2, 8 }));
            Assert.That(resource.Kind, Is.EqualTo(GltfResourceKind.Buffer));
            Assert.That(resource.UriKind, Is.EqualTo(GltfUriKind.Absent));
            Assert.That(resource.MimeType, Is.EqualTo(GltfMimeTypes.GltfBuffer));
            Assert.That(resource.Bytes, Is.EqualTo(new byte[] { 1, 8, 3, 0 }));
            Assert.That(resource.HasDeclaredByteLength, Is.True);
            Assert.That(resource.DeclaredByteLength, Is.EqualTo(3));
            Assert.That(resource.Embedded, Is.True);
            Assert.That(resource.CacheKey, Is.EqualTo("glb:bin"));
            Assert.That(container.BinBytes, Is.EqualTo(new byte[] { 8, 2, 3, 0 }));
            Assert.That(buffer.ResolvedBytes, Is.Not.SameAs(resource.Bytes));
            Assert.That(buffer.ResolvedBytes, Is.Not.SameAs(container.BinBytes));
            Assert.That(resource.Bytes, Is.Not.SameAs(container.BinBytes));
        });
    }

    [TestCase(3, 2, "GLB buffer 0 declares byteLength 3 but the BIN chunk contains 2 bytes.")]
    [TestCase(
        3,
        7,
        "GLB BIN chunk length 7 exceeds buffer 0 declared byteLength 3 by more than 3 padding bytes.")]
    public void ReadContainer_EnforcesGlbBinDeclaredLength(
        int declaredByteLength,
        int binByteLength,
        string message)
    {
        byte[] bytes = new byte[binByteLength];
        GltfContainerData container = CreateContainer(
            GltfContainerFormat.Glb,
            $"{{\"asset\":{{\"version\":\"2.0\"}},\"buffers\":[{{\"byteLength\":{declaredByteLength}}}]}}",
            bytes);

        Assert.That(
            () => new GltfReader().ReadContainer(container, new GltfReadOptions()),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [Test]
    public void ReadContainer_RequiresTheGlbBinChunkForItsUriLessBuffer()
    {
        GltfContainerData container = CreateContainer(
            GltfContainerFormat.Glb,
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":0}]}",
            []);
        container.HasBinChunk = false;

        Assert.That(
            () => new GltfReader().ReadContainer(container, new GltfReadOptions()),
            Throws.TypeOf<GltfResourceException>()
                .With.Message.EqualTo("GLB buffer 0 has no URI and requires a BIN chunk."));
    }

    [Test]
    public void ReadContainer_EagerlyLoadsExternalBufferAndLimitsItsLogicalBytes()
    {
        BufferResolverProbe resolver = new BufferResolverProbe();
        resolver.Bytes = [1, 2, 3, 4];
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = resolver;
        options.BaseUri = "file:///assets/model.gltf";
        GltfContainerData container = CreateContainer(
            GltfContainerFormat.Json,
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"buffer.bin\",\"byteLength\":3}]}",
            []);

        GltfReadResult result = new GltfReader().ReadContainer(container, options);
        GltfBuffer buffer = result.Document!.Buffers[0];
        GltfResource resource = result.Resources.Resources[0];
        resolver.Bytes[0] = 8;
        resource.Bytes[1] = 8;

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.EqualTo(1));
            Assert.That(buffer.Resolved, Is.True);
            Assert.That(buffer.ResolvedBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(buffer.HasResource, Is.True);
            Assert.That(buffer.Resource, Is.Zero);
            Assert.That(resource.Bytes, Is.EqualTo(new byte[] { 1, 8, 3, 4 }));
            Assert.That(resource.Bytes, Is.Not.SameAs(resolver.Bytes));
            Assert.That(buffer.ResolvedBytes, Is.Not.SameAs(resource.Bytes));
        });
    }

    [Test]
    public void ReadContainer_RetainsIndependentRequestsForLazyExternalBuffers()
    {
        BufferResolverProbe resolver = new BufferResolverProbe();
        GltfReadOptions options = new GltfReadOptions();
        options.ResourceResolver = resolver;
        options.BaseUri = "file:///assets/model.gltf";
        options.ResourcePolicy.FileRoots.Add("/assets");
        GltfContainerData container = CreateContainer(
            GltfContainerFormat.Json,
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"first.bin\",\"byteLength\":2},{\"uri\":\"second.bin\",\"byteLength\":3}]}",
            []);

        GltfReadResult result = new GltfReader().ReadContainer(container, options);
        GltfBuffer first = result.Document!.Buffers[0];
        GltfBuffer second = result.Document.Buffers[1];
        first.PendingResourceRequest!.Policy.FileRoots[0] = "/changed";

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.Zero);
            Assert.That(result.Resources.Resources, Is.Empty);
            Assert.That(first.Resolved, Is.False);
            Assert.That(first.ResolvedBytes, Is.Empty);
            Assert.That(first.HasResource, Is.False);
            Assert.That(first.Resource, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(first.PendingResourceRequest, Is.Not.Null);
            Assert.That(first.PendingResourceRequest!.BaseUri, Is.EqualTo(options.BaseUri));
            Assert.That(first.PendingResourceRequest.SourceUri, Is.EqualTo("first.bin"));
            Assert.That(first.PendingResourceRequest.Kind, Is.EqualTo(GltfResourceKind.Buffer));
            Assert.That(first.PendingResourceRequest.HasDeclaredByteLength, Is.True);
            Assert.That(first.PendingResourceRequest.DeclaredByteLength, Is.EqualTo(2));
            Assert.That(second.PendingResourceRequest, Is.Not.Null);
            Assert.That(second.PendingResourceRequest!.DeclaredByteLength, Is.EqualTo(3));
            Assert.That(second.PendingResourceRequest.Policy.FileRoots, Is.EqualTo(new[] { "/assets" }));
            Assert.That(options.ResourcePolicy.FileRoots, Is.EqualTo(new[] { "/assets" }));
            Assert.That(first.PendingResourceRequest, Is.Not.SameAs(second.PendingResourceRequest));
            Assert.That(
                first.PendingResourceRequest.Policy.FileRoots,
                Is.Not.SameAs(second.PendingResourceRequest.Policy.FileRoots));
        });
    }

    [Test]
    public void ReadContainer_GivesDuplicateBufferBindingsIndependentLogicalBytes()
    {
        BufferResolverProbe resolver = new BufferResolverProbe();
        resolver.Bytes = [4, 5];
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = resolver;
        options.BaseUri = "file:///assets/model.gltf";
        GltfContainerData container = CreateContainer(
            GltfContainerFormat.Json,
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"shared.bin\",\"byteLength\":2},{\"uri\":\"shared.bin\",\"byteLength\":2}]}",
            []);

        GltfReadResult result = new GltfReader().ReadContainer(container, options);
        GltfBuffer first = result.Document!.Buffers[0];
        GltfBuffer second = result.Document.Buffers[1];
        first.ResolvedBytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.EqualTo(1));
            Assert.That(first.ResolvedBytes, Is.EqualTo(new byte[] { 9, 5 }));
            Assert.That(second.ResolvedBytes, Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(first.ResolvedBytes, Is.Not.SameAs(second.ResolvedBytes));
            Assert.That(first.Resource, Is.Zero);
            Assert.That(second.Resource, Is.EqualTo(1));
            Assert.That(result.Resources.Cache, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ReadContainer_RejectsAShortExternalBuffer()
    {
        BufferResolverProbe resolver = new BufferResolverProbe();
        resolver.Bytes = [1, 2];
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = resolver;
        GltfContainerData container = CreateContainer(
            GltfContainerFormat.Json,
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"buffer.bin\",\"byteLength\":3}]}",
            []);

        Assert.That(
            () => new GltfReader().ReadContainer(container, options),
            Throws.TypeOf<GltfResourceException>()
                .With.Message.EqualTo(
                    "Buffer 0 declares byteLength 3 but its resolved resource contains 2 bytes."));
    }

    [TestCase("data:application/octet-stream;base64,AQID", true)]
    [TestCase("data:application/gltf-buffer;base64,AQID", true)]
    [TestCase("data:;base64,AQID", false)]
    [TestCase("data:text/plain;base64,AQID", false)]
    public void ReadContainer_EnforcesDataUriBufferMediaTypes(string uri, bool accepted)
    {
        string json =
            $"{{\"asset\":{{\"version\":\"2.0\"}},\"buffers\":[{{\"uri\":\"{uri}\",\"byteLength\":3}}]}}";
        GltfContainerData container = CreateContainer(GltfContainerFormat.Json, json, []);
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;

        if (accepted)
        {
            GltfReadResult result = new GltfReader().ReadContainer(container, options);
            Assert.That(result.Document!.Buffers[0].ResolvedBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }
        else
        {
            Assert.That(
                () => new GltfReader().ReadContainer(container, options),
                Throws.TypeOf<GltfResourceException>()
                    .With.Message.Contains("is not a supported glTF buffer media type"));
        }
    }

    [TestCase(
        GltfContainerFormat.Json,
        "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":1}]}",
        "Buffer 0 has no URI and is not bound to a GLB BIN chunk.")]
    [TestCase(
        GltfContainerFormat.Json,
        "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"buffer.bin\",\"byteLength\":-1}]}",
        "Buffer 0 declares a negative byteLength -1.")]
    public void ReadContainer_RejectsUnresolvableOrNegativeBufferDeclarations(
        GltfContainerFormat format,
        string json,
        string message)
    {
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = new BufferResolverProbe();
        GltfContainerData container = CreateContainer(format, json, []);

        Assert.That(
            () => new GltfReader().ReadContainer(container, options),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    private static GltfContainerData CreateContainer(
        GltfContainerFormat format,
        string json,
        byte[] binBytes)
    {
        GltfContainerData result = new GltfContainerData();
        result.Format = format;
        result.JsonBytes = Encoding.UTF8.GetBytes(json);
        result.HasBinChunk = format == GltfContainerFormat.Glb;
        result.BinBytes = binBytes;
        return result;
    }

    private class BufferResolverProbe : IGltfResourceResolver
    {
        internal int CallCount;
        internal byte[] Bytes = [];

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            CallCount++;
            GltfResource result = new GltfResource();
            result.Bytes = Bytes;
            result.EffectiveUri = "file:///assets/" + request.SourceUri;
            return result;
        }
    }
}
