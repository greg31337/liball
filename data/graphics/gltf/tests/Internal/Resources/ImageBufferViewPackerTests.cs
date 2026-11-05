namespace gltf.Tests.Internal.Resources;

[TestFixture]
public class ImageBufferViewPackerTests
{
    [Test]
    public void PackImages_AlignsOffsetsPreservesExactLengthsAndDeduplicatesBytes()
    {
        GltfResourceSet resources = new GltfResourceSet();
        resources.Resources.Add(CreateImage([1, 2, 3], GltfMimeTypes.Png));
        resources.Resources.Add(CreateImage([4, 5], GltfMimeTypes.Jpeg));
        resources.Resources.Add(CreateImage([1, 2, 3], GltfMimeTypes.Png));

        ImageBufferViewPackPlan result = new ImageBufferViewPacker().PackImages(resources);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResourceBufferViews, Is.EqualTo(new[] { 0, 1, 0 }));
            Assert.That(result.ByteOffsets, Is.EqualTo(new[] { 0, 4 }));
            Assert.That(result.ByteLengths, Is.EqualTo(new[] { 3, 2 }));
            Assert.That(result.MimeTypes,
                Is.EqualTo(new[] { GltfMimeTypes.Png, GltfMimeTypes.Jpeg }));
            Assert.That(result.BufferBytes, Is.EqualTo(new byte[] { 1, 2, 3, 0, 4, 5 }));
        });
    }

    [Test]
    public void PackImages_LeavesNonImageResourcesOutsideThePackedViews()
    {
        GltfResourceSet resources = new GltfResourceSet();
        GltfResource buffer = new GltfResource();
        buffer.Kind = GltfResourceKind.Buffer;
        buffer.Bytes = [9, 8, 7];
        resources.Resources.Add(buffer);
        resources.Resources.Add(CreateImage([1], GltfMimeTypes.Png));

        ImageBufferViewPackPlan result = new ImageBufferViewPacker().PackImages(resources);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResourceBufferViews, Is.EqualTo(new[] { -1, 0 }));
            Assert.That(result.BufferBytes, Is.EqualTo(new byte[] { 1 }));
            Assert.That(buffer.Bytes, Is.EqualTo(new byte[] { 9, 8, 7 }));
        });
    }

    [Test]
    public void PackImages_InfersKnownMimeTypeWithoutChangingTheSource()
    {
        GltfResource image = CreateImage([1], null);
        image.ImageFormat = GltfImageFormat.WebP;
        GltfResourceSet resources = new GltfResourceSet();
        resources.Resources.Add(image);

        ImageBufferViewPackPlan result = new ImageBufferViewPacker().PackImages(resources);

        Assert.Multiple(() =>
        {
            Assert.That(result.MimeTypes, Is.EqualTo(new[] { GltfMimeTypes.WebP }));
            Assert.That(image.MimeType, Is.Null);
        });
    }

    [Test]
    public void PackImages_RejectsImageWithoutKnownMimeType()
    {
        GltfResourceSet resources = new GltfResourceSet();
        resources.Resources.Add(CreateImage([1], null));

        Assert.That(
            () => new ImageBufferViewPacker().PackImages(resources),
            Throws.TypeOf<GltfWriteException>().With.Message.EqualTo(
                "A packed image resource must have a known media type."));
    }

    private static GltfResource CreateImage(byte[] bytes, string? mimeType)
    {
        GltfResource resource = new GltfResource();
        resource.Kind = GltfResourceKind.Image;
        resource.MimeType = mimeType;
        resource.Bytes = bytes;
        return resource;
    }
}
