using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfReadLimitTests
{
    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"data:application/octet-stream;base64,AQID\",\"byteLength\":3}]}")]
    [TestCase(
        "{\"asset\":{\"version\":\"2.0\"},\"images\":[{\"uri\":\"data:image/png;base64,AQID\"}]}")]
    public void ReadBytes_AppliesTheDecodedDataUriLimitToBuffersAndImages(string json)
    {
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.Limits.MaximumDataUriByteLength = 2;
        options.Limits.MaximumResourceByteLength = 10;

        Assert.That(
            () => new GltfReader().ReadBytes(Encoding.UTF8.GetBytes(json), options),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "Base64 decoded length 3 exceeds the configured maximum 2."));
    }

    [Test]
    public void ReadContainer_UsesStoredBytesWhenOriginalLengthIsUnavailable()
    {
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Json;
        container.JsonBytes = Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}");
        GltfReadOptions options = new GltfReadOptions();
        options.Limits.MaximumInputByteLength = container.JsonBytes.Length - 1;

        Assert.That(
            () => new GltfReader().ReadContainer(container, options),
            Throws.TypeOf<GltfParseException>().With.Message.EqualTo(
                $"Input length {container.JsonBytes.Length} exceeds the configured maximum {container.JsonBytes.Length - 1} at byte {container.JsonBytes.Length - 1}."));
    }

    [Test]
    public void ReadContainer_CountsStoredGlbChunksAgainstThePrimaryInputLimit()
    {
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Glb;
        container.JsonBytes = Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}");
        container.HasBinChunk = true;
        container.BinBytes = [1, 2, 3, 0];
        int storedByteLength = 20 + container.JsonBytes.Length + 8 + container.BinBytes.Length;
        GltfReadOptions options = new GltfReadOptions();
        options.Limits.MaximumInputByteLength = storedByteLength - 1;

        Assert.That(
            () => new GltfReader().ReadContainer(container, options),
            Throws.TypeOf<GltfParseException>().With.Message.EqualTo(
                $"Input length {storedByteLength} exceeds the configured maximum {storedByteLength - 1} at byte {storedByteLength - 1}."));
    }

    [Test]
    public void ReadBytes_AppliesTheResourceLimitToLazyBufferDeclarations()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"large.bin\",\"byteLength\":3}]}");
        GltfReadOptions options = new GltfReadOptions();
        options.Limits.MaximumResourceByteLength = 2;

        Assert.That(
            () => new GltfReader().ReadBytes(json, options),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "Buffer 0 declared byteLength 3 exceeds the configured maximum resource length 2."));
    }

    [Test]
    public void ReadBytes_AppliesTheResourceLimitToLazyBufferViewImages()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"buffer.bin\",\"byteLength\":3}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":4}],\"images\":[{\"bufferView\":0,\"mimeType\":\"image/png\"}]}");
        GltfReadOptions options = new GltfReadOptions();
        options.Limits.MaximumResourceByteLength = 3;

        Assert.That(
            () => new GltfReader().ReadBytes(json, options),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "Image 0 bufferView 0 byteLength 4 exceeds the configured maximum resource length 3."));
    }

    [Test]
    public void ReadBytes_BoundsTheCombinedResolvedResourceCount()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"data:application/octet-stream;base64,AQ==\",\"byteLength\":1}],\"images\":[{\"uri\":\"data:image/png;base64,Ag==\"}]}");
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.Limits.MaximumCollectionCount = 1;

        Assert.That(
            () => new GltfReader().ReadBytes(json, options),
            Throws.TypeOf<GltfResourceException>().With.Message.EqualTo(
                "Resolved resource count 2 exceeds the configured maximum 1."));
    }

    [Test]
    public void ReadBytes_CountsRepeatedBufferViewImagesOnceInTheCacheTotal()
    {
        byte[] json = Encoding.UTF8.GetBytes(
            "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"uri\":\"data:application/octet-stream;base64,AQID\",\"byteLength\":3}],\"bufferViews\":[{\"buffer\":0,\"byteOffset\":1,\"byteLength\":2}],\"images\":[{\"bufferView\":0,\"mimeType\":\"image/png\"},{\"bufferView\":0,\"mimeType\":\"image/png\"}]}");
        GltfReadOptions options = new GltfReadOptions();
        options.ResolveResources = true;
        options.Limits.MaximumTotalResourceByteLength = 5;

        GltfReadResult result = new GltfReader().ReadBytes(json, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(3));
            Assert.That(result.Resources.Cache, Has.Count.EqualTo(2));
            Assert.That(result.Document!.Images[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 2, 3 }));
            Assert.That(result.Document.Images[1].ResolvedBytes,
                Is.EqualTo(new byte[] { 2, 3 }));
        });
    }
}
