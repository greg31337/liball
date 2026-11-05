namespace gltf.Tests.Api.Static;

[TestFixture]
public class GltfMimeTypesTests
{
    [Test]
    public void ContainerAndBufferTypes_MatchRegisteredValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GltfMimeTypes.GltfJson, Is.EqualTo("model/gltf+json"));
            Assert.That(GltfMimeTypes.GltfBinary, Is.EqualTo("model/gltf-binary"));
            Assert.That(GltfMimeTypes.GltfBuffer, Is.EqualTo("application/gltf-buffer"));
            Assert.That(GltfMimeTypes.OctetStream, Is.EqualTo("application/octet-stream"));
        });
    }

    [Test]
    public void ImageTypes_MatchKnownValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GltfMimeTypes.Png, Is.EqualTo("image/png"));
            Assert.That(GltfMimeTypes.Jpeg, Is.EqualTo("image/jpeg"));
            Assert.That(GltfMimeTypes.Ktx2, Is.EqualTo("image/ktx2"));
            Assert.That(GltfMimeTypes.WebP, Is.EqualTo("image/webp"));
            Assert.That(GltfMimeTypes.Avif, Is.EqualTo("image/avif"));
            Assert.That(GltfMimeTypes.Dds, Is.EqualTo("image/vnd-ms.dds"));
        });
    }
}
