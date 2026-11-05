namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class ResourceNameAllocatorTests
{
    [TestCase(GltfImageFormat.Png, ".png")]
    [TestCase(GltfImageFormat.Jpeg, ".jpg")]
    [TestCase(GltfImageFormat.Ktx2, ".ktx2")]
    [TestCase(GltfImageFormat.WebP, ".webp")]
    [TestCase(GltfImageFormat.Avif, ".avif")]
    [TestCase(GltfImageFormat.Dds, ".dds")]
    [TestCase(GltfImageFormat.Astc, ".astc")]
    public void AllocateResourceName_SelectsCanonicalImageFormatExtension(
        GltfImageFormat format,
        string extension)
    {
        GltfResource resource = CreateResource(GltfResourceKind.Image);
        resource.ImageFormat = format;

        string result = new ResourceNameAllocator().AllocateResourceName(
            resource,
            "textures/source.WRONG",
            false);

        Assert.That(result, Is.EqualTo("textures/source" + extension));
    }

    [TestCase(GltfMimeTypes.Png, "asset.png")]
    [TestCase(GltfMimeTypes.Jpeg, "asset.jpg")]
    [TestCase(GltfMimeTypes.Ktx2, "asset.ktx2")]
    [TestCase(GltfMimeTypes.WebP, "asset.webp")]
    [TestCase(GltfMimeTypes.Avif, "asset.avif")]
    [TestCase(GltfMimeTypes.Dds, "asset.dds")]
    [TestCase(GltfMimeTypes.GltfJson, "asset.gltf")]
    [TestCase(GltfMimeTypes.GltfBinary, "asset.glb")]
    [TestCase(GltfMimeTypes.GltfBuffer, "asset.bin")]
    public void AllocateResourceName_SelectsCanonicalMimeTypeExtension(
        string mimeType,
        string expected)
    {
        GltfResource resource = CreateResource(GltfResourceKind.Other);
        resource.MimeType = mimeType.ToUpperInvariant();

        string result = new ResourceNameAllocator().AllocateResourceName(
            resource,
            "asset.unknown",
            false);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void AllocateResourceName_NormalizesRelativeSegmentsSeparatorsAndUriText()
    {
        GltfResource resource = CreateResource(GltfResourceKind.Buffer);

        string result = new ResourceNameAllocator().AllocateResourceName(
            resource,
            @"./assets\models/../mesh data.dat?version=1#part",
            false);

        Assert.That(result, Is.EqualTo("assets/mesh%20data.bin"));
    }

    [Test]
    public void AllocateResourceName_UsesThePathFromAnAbsoluteSourceUri()
    {
        GltfResource resource = CreateResource(GltfResourceKind.Image);
        resource.ImageFormat = GltfImageFormat.Png;
        resource.SourceUri = "https://cdn.example.test/models/textures/albedo.jpeg?v=2";

        string result = new ResourceNameAllocator().AllocateResourceName(
            resource,
            null,
            false);

        Assert.That(result, Is.EqualTo("models/textures/albedo.png"));
    }

    [Test]
    public void AllocateResourceName_RemovesAuthorityFromNetworkPathReference()
    {
        GltfResource resource = CreateResource(GltfResourceKind.Image);
        resource.ImageFormat = GltfImageFormat.WebP;

        string result = new ResourceNameAllocator().AllocateResourceName(
            resource,
            "//cdn.example.test/models/preview.jpeg",
            false);

        Assert.That(result, Is.EqualTo("models/preview.webp"));
    }

    [Test]
    public void AllocateResourceName_RemovesTraversalBeyondTheOutputRoot()
    {
        GltfResource resource = CreateResource(GltfResourceKind.Buffer);

        string result = new ResourceNameAllocator().AllocateResourceName(
            resource,
            "../../outside/../buffer.data",
            false);

        Assert.That(result, Is.EqualTo("buffer.bin"));
    }

    [Test]
    public void AllocateResourceName_UsesKindFallbackForADataUri()
    {
        GltfResource resource = CreateResource(GltfResourceKind.Image);
        resource.MimeType = GltfMimeTypes.Png;
        resource.SourceUri = "data:image/png;base64,iVBORw0KGgo=";

        string result = new ResourceNameAllocator().AllocateResourceName(
            resource,
            null,
            false);

        Assert.That(result, Is.EqualTo("image.png"));
    }

    [Test]
    public void AllocateResourceName_PreservesAndNormalizesUnknownExtension()
    {
        GltfResource resource = CreateResource(GltfResourceKind.Extension);

        string result = new ResourceNameAllocator().AllocateResourceName(
            resource,
            "vendor/payload.CUSTOM",
            false);

        Assert.That(result, Is.EqualTo("vendor/payload.custom"));
    }

    [Test]
    public void AllocateResourceName_AddsStableSuffixesBeforeTheExtension()
    {
        ResourceNameAllocator allocator = new ResourceNameAllocator();
        allocator.ReserveResourceName("Textures/Image.png");
        allocator.ReserveResourceName("textures/image-1.png");
        GltfResource resource = CreateResource(GltfResourceKind.Image);
        resource.ImageFormat = GltfImageFormat.Png;

        string first = allocator.AllocateResourceName(resource, "textures/image.jpeg", false);
        string second = allocator.AllocateResourceName(resource, "textures/image.jpeg", false);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("textures/image-2.png"));
            Assert.That(second, Is.EqualTo("textures/image-3.png"));
        });
    }

    [Test]
    public void AllocateResourceName_UsesLowercaseSha256ContentNameWhenRequested()
    {
        GltfResource resource = CreateResource(GltfResourceKind.Image);
        resource.ImageFormat = GltfImageFormat.Jpeg;
        resource.Bytes = [1, 2, 3];

        string result = new ResourceNameAllocator().AllocateResourceName(
            resource,
            "textures/original.jpeg",
            true);

        Assert.That(
            result,
            Is.EqualTo(
                "textures/039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81.jpg"));
    }

    [Test]
    public void AllocateResourceName_SuffixesRepeatedContentHashNames()
    {
        ResourceNameAllocator allocator = new ResourceNameAllocator();
        GltfResource resource = CreateResource(GltfResourceKind.Buffer);
        resource.Bytes = [1, 2, 3];

        string first = allocator.AllocateResourceName(resource, "buffers/a.data", true);
        string second = allocator.AllocateResourceName(resource, "buffers/b.data", true);

        Assert.Multiple(() =>
        {
            Assert.That(first, Does.EndWith("1a2011cfb81.bin"));
            Assert.That(second, Does.EndWith("1a2011cfb81-1.bin"));
            Assert.That(first, Does.StartWith("buffers/"));
            Assert.That(second, Does.StartWith("buffers/"));
        });
    }

    [Test]
    public void AllocateResourceName_RejectsMalformedPercentEncoding()
    {
        GltfResource resource = CreateResource(GltfResourceKind.Other);

        Assert.That(
            () => new ResourceNameAllocator().AllocateResourceName(
                resource,
                "resources/bad%2.bin",
                false),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "Malformed percent escape at character 13."));
    }

    [Test]
    public void ReserveResourceName_RejectsAnEmptyNormalizedName()
    {
        Assert.That(
            () => new ResourceNameAllocator().ReserveResourceName("../"),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "A reserved resource name must not be empty."));
    }

    private static GltfResource CreateResource(GltfResourceKind kind)
    {
        GltfResource resource = new GltfResource();
        resource.Kind = kind;
        resource.Bytes = [0];
        return resource;
    }
}
