using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfImageResolutionTests
{
    [Test]
    public void ReadContainer_ResolvesDataUriWithOriginalAndEffectiveProvenance()
    {
        GltfContainerData container = CreateContainer(
            "{\"asset\":{\"version\":\"2.0\"},\"images\":[{\"uri\":\"DATA:image/png;base64,AQID\",\"mimeType\":\"image/png\"}]}");
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;

        GltfReadResult result = new GltfReader().ReadContainer(container, options);
        GltfImage image = result.Document!.Images[0];
        GltfResource resource = result.Resources.Resources[0];
        resource.Bytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(image.Uri, Is.EqualTo("DATA:image/png;base64,AQID"));
            Assert.That(image.MimeType, Is.EqualTo("image/png"));
            Assert.That(image.Resolved, Is.True);
            Assert.That(image.ResolvedBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(image.HasResource, Is.True);
            Assert.That(image.Resource, Is.Zero);
            Assert.That(image.EffectiveUri, Is.EqualTo("data:image/png;base64,AQID"));
            Assert.That(image.ResolvedMimeType, Is.EqualTo("image/png"));
            Assert.That(image.PendingResourceRequest, Is.Null);
            Assert.That(resource.SourceUri, Is.EqualTo(image.Uri));
            Assert.That(resource.EffectiveUri, Is.EqualTo(image.EffectiveUri));
            Assert.That(resource.DeclaredMimeType, Is.EqualTo("image/png"));
            Assert.That(resource.SourceMimeType, Is.EqualTo("image/png"));
            Assert.That(resource.Bytes, Is.EqualTo(new byte[] { 9, 2, 3 }));
            Assert.That(image.ResolvedBytes, Is.Not.SameAs(resource.Bytes));
        });
    }

    [Test]
    public void ReadContainer_ResolvesExternalImageAndRetainsResolverProvenance()
    {
        ImageResolverProbe resolver = new ImageResolverProbe();
        resolver.Bytes = [4, 5];
        resolver.EffectiveUri = "https://cdn.invalid/effective.jpg";
        resolver.SourceMimeType = "image/jpeg";
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = resolver;
        options.BaseUri = "https://origin.invalid/model.gltf";
        GltfContainerData container = CreateContainer(
            "{\"asset\":{\"version\":\"2.0\"},\"images\":[{\"uri\":\"original.jpg\"}]}");

        GltfReadResult result = new GltfReader().ReadContainer(container, options);
        GltfImage image = result.Document!.Images[0];

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.EqualTo(1));
            Assert.That(resolver.Request!.SourceUri, Is.EqualTo("original.jpg"));
            Assert.That(resolver.Request.BaseUri, Is.EqualTo(options.BaseUri));
            Assert.That(resolver.Request.Kind, Is.EqualTo(GltfResourceKind.Image));
            Assert.That(image.Uri, Is.EqualTo("original.jpg"));
            Assert.That(image.EffectiveUri, Is.EqualTo(resolver.EffectiveUri));
            Assert.That(image.ResolvedMimeType, Is.EqualTo("image/jpeg"));
            Assert.That(image.ResolvedBytes, Is.EqualTo(new byte[] { 4, 5 }));
        });
    }

    [Test]
    public void ReadContainer_ExtractsAnOwnedBufferViewImage()
    {
        GltfContainerData container = CreateContainer(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"data:application/octet-stream;base64,AAoLDA0=\",\"byteLength\":5}],\"bufferViews\":[{\"buffer\":0,\"byteOffset\":1,\"byteLength\":3}],\"images\":[{\"bufferView\":0,\"mimeType\":\"image/png\"}]}");
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;

        GltfReadResult result = new GltfReader().ReadContainer(container, options);
        GltfBuffer buffer = result.Document!.Buffers[0];
        GltfImage image = result.Document.Images[0];
        GltfResource imageResource = result.Resources.Resources[1];
        buffer.ResolvedBytes[1] = 9;
        imageResource.Bytes[1] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(image.Uri, Is.Null);
            Assert.That(image.HasBufferView, Is.True);
            Assert.That(image.BufferView, Is.Zero);
            Assert.That(image.Resolved, Is.True);
            Assert.That(image.ResolvedBytes, Is.EqualTo(new byte[] { 10, 11, 12 }));
            Assert.That(image.Resource, Is.EqualTo(1));
            Assert.That(image.EffectiveUri, Is.EqualTo("bufferView:0"));
            Assert.That(image.ResolvedMimeType, Is.EqualTo("image/png"));
            Assert.That(imageResource.SourceUri, Is.Null);
            Assert.That(imageResource.EffectiveUri, Is.EqualTo("bufferView:0"));
            Assert.That(imageResource.MimeType, Is.EqualTo("image/png"));
            Assert.That(imageResource.DeclaredMimeType, Is.EqualTo("image/png"));
            Assert.That(imageResource.SourceMimeType, Is.EqualTo("image/png"));
            Assert.That(imageResource.Bytes, Is.EqualTo(new byte[] { 10, 9, 12 }));
            Assert.That(imageResource.Embedded, Is.True);
            Assert.That(imageResource.HasDeclaredByteLength, Is.True);
            Assert.That(imageResource.DeclaredByteLength, Is.EqualTo(3));
            Assert.That(image.ResolvedBytes, Is.Not.SameAs(imageResource.Bytes));
            Assert.That(image.ResolvedBytes, Is.Not.SameAs(buffer.ResolvedBytes));
        });
    }

    [Test]
    public void ReadContainer_RetainsAnIndependentLazyUriRequest()
    {
        ImageResolverProbe resolver = new ImageResolverProbe();
        GltfReadOptions options = new GltfReadOptions();
        options.ResourceResolver = resolver;
        options.BaseUri = "file:///assets/model.gltf";
        options.ResourcePolicy.FileRoots.Add("/assets");
        GltfContainerData container = CreateContainer(
            "{\"asset\":{\"version\":\"2.0\"},\"images\":[{\"uri\":\"image.png\",\"mimeType\":\"image/png\"}]}");

        GltfReadResult result = new GltfReader().ReadContainer(container, options);
        GltfImage image = result.Document!.Images[0];
        image.PendingResourceRequest!.Policy.FileRoots[0] = "/changed";

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.Zero);
            Assert.That(result.Resources.Resources, Is.Empty);
            Assert.That(image.Resolved, Is.False);
            Assert.That(image.ResolvedBytes, Is.Empty);
            Assert.That(image.HasResource, Is.False);
            Assert.That(image.Resource, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(image.EffectiveUri, Is.Null);
            Assert.That(image.ResolvedMimeType, Is.Null);
            Assert.That(image.PendingResourceRequest.SourceUri, Is.EqualTo("image.png"));
            Assert.That(image.PendingResourceRequest.BaseUri, Is.EqualTo(options.BaseUri));
            Assert.That(image.PendingResourceRequest.Kind, Is.EqualTo(GltfResourceKind.Image));
            Assert.That(image.PendingResourceRequest.DeclaredMimeType, Is.EqualTo("image/png"));
            Assert.That(options.ResourcePolicy.FileRoots, Is.EqualTo(new[] { "/assets" }));
        });
    }

    [Test]
    public void ReadContainer_GivesDuplicateImageBindingsIndependentBytes()
    {
        ImageResolverProbe resolver = new ImageResolverProbe();
        resolver.Bytes = [6, 7];
        resolver.EffectiveUri = "https://cdn.invalid/shared.png";
        resolver.SourceMimeType = "image/png";
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.ResourceResolver = resolver;
        GltfContainerData container = CreateContainer(
            "{\"asset\":{\"version\":\"2.0\"},\"images\":[{\"uri\":\"shared.png\"},{\"uri\":\"shared.png\"}]}");

        GltfReadResult result = new GltfReader().ReadContainer(container, options);
        GltfImage first = result.Document!.Images[0];
        GltfImage second = result.Document.Images[1];
        first.ResolvedBytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(resolver.CallCount, Is.EqualTo(1));
            Assert.That(first.ResolvedBytes, Is.EqualTo(new byte[] { 9, 7 }));
            Assert.That(second.ResolvedBytes, Is.EqualTo(new byte[] { 6, 7 }));
            Assert.That(first.ResolvedBytes, Is.Not.SameAs(second.ResolvedBytes));
            Assert.That(first.Resource, Is.Zero);
            Assert.That(second.Resource, Is.EqualTo(1));
            Assert.That(first.EffectiveUri, Is.EqualTo(second.EffectiveUri));
            Assert.That(result.Resources.Cache, Has.Count.EqualTo(1));
        });
    }

    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"images\":[{}]}",
        "Image 0 does not define a URI or bufferView source.")]
    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"bufferViews\":[{\"buffer\":0,\"byteLength\":1}],\"images\":[{\"uri\":\"image.png\",\"bufferView\":0,\"mimeType\":\"image/png\"}]}",
        "Image 0 defines both URI and bufferView sources.")]
    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"bufferViews\":[{\"buffer\":0,\"byteLength\":1}],\"images\":[{\"bufferView\":0}]}",
        "Image 0 uses a bufferView and must declare a media type.")]
    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"images\":[{\"uri\":\"image.bin\",\"mimeType\":\"application/octet-stream\"}]}",
        "Image 0 media type 'application/octet-stream' is not an image media type.")]
    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"images\":[{\"bufferView\":2,\"mimeType\":\"image/png\"}]}",
        "Image 0 references bufferView 2 outside the document bufferView collection.")]
    public void ReadContainer_EnforcesImageSourceAndDeclaredMimeRules(string json, string message)
    {
        Assert.That(
            () => new GltfReader().ReadContainer(CreateContainer(json), new GltfReadOptions()),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [TestCase(
        "data:text/plain;base64,AQID",
        null,
        "Image 0 resolved media type 'text/plain' is not an image media type.")]
    [TestCase(
        "data:image/jpeg;base64,AQID",
        "image/png",
        "Image 0 declares media type 'image/png' but its URI source declares 'image/jpeg'.")]
    public void ReadContainer_EnforcesDataUriMimeRules(
        string uri,
        string? declaredMimeType,
        string message)
    {
        string mimeMember = "";
        if (declaredMimeType != null)
        {
            mimeMember = ",\"mimeType\":\"" + declaredMimeType + "\"";
        }

        string json =
            "{\"asset\":{\"version\":\"2.0\"},\"images\":[{\"uri\":\""
            + uri
            + "\""
            + mimeMember
            + "}]}";
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;

        Assert.That(
            () => new GltfReader().ReadContainer(CreateContainer(json), options),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"data:application/octet-stream;base64,AQID\",\"byteLength\":3}],\"bufferViews\":[{\"buffer\":2,\"byteLength\":1}],\"images\":[{\"bufferView\":0,\"mimeType\":\"image/png\"}]}",
        "Image 0 bufferView 0 references buffer 2 outside the document buffer collection.")]
    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"data:application/octet-stream;base64,AQID\",\"byteLength\":3}],\"bufferViews\":[{\"buffer\":0,\"byteOffset\":-1,\"byteLength\":1}],\"images\":[{\"bufferView\":0,\"mimeType\":\"image/png\"}]}",
        "Image 0 bufferView 0 has a negative byte range.")]
    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"data:application/octet-stream;base64,AQID\",\"byteLength\":3}],\"bufferViews\":[{\"buffer\":0,\"byteOffset\":2,\"byteLength\":2}],\"images\":[{\"bufferView\":0,\"mimeType\":\"image/png\"}]}",
        "Image 0 bufferView 0 range exceeds resolved buffer 0 length 3.")]
    public void ReadContainer_RejectsInvalidBufferViewImageRanges(string json, string message)
    {
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;

        Assert.That(
            () => new GltfReader().ReadContainer(CreateContainer(json), options),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(message));
    }

    private static GltfContainerData CreateContainer(string json)
    {
        GltfContainerData result = new GltfContainerData();
        result.Format = GltfContainerFormat.Json;
        result.JsonBytes = Encoding.UTF8.GetBytes(json);
        return result;
    }

    private class ImageResolverProbe : IGltfResourceResolver
    {
        internal int CallCount;
        internal byte[] Bytes = [];
        internal string? EffectiveUri;
        internal string? SourceMimeType;
        internal GltfResourceRequest? Request;

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            CallCount++;
            Request = request;
            GltfResource result = new GltfResource();
            result.Bytes = Bytes;
            result.EffectiveUri = EffectiveUri;
            result.SourceMimeType = SourceMimeType;
            return result;
        }
    }
}
