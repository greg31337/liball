using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfWriterResourcePlanningTests
{
    [Test]
    public void WriteBytes_ExternalizesBoundResourcesAndRewritesOnlyOutputJson()
    {
        GltfDocument document = new GltfDocument();
        GltfResource bufferResource = CreateResource(
            GltfResourceKind.Buffer,
            "buffers/original.data",
            GltfMimeTypes.OctetStream,
            GltfImageFormat.Unknown,
            [1, 2, 3]);
        GltfResource imageResource = CreateResource(
            GltfResourceKind.Image,
            "textures/picture.jpeg",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [4, 5]);
        document.Resources.Resources.Add(bufferResource);
        document.Resources.Resources.Add(imageResource);

        GltfBuffer buffer = new GltfBuffer();
        buffer.Uri = "old-buffer.bin";
        buffer.ByteLength = 3;
        buffer.HasResource = true;
        buffer.Resource = 0;
        document.Buffers.Add(buffer);
        GltfImage image = new GltfImage();
        image.Uri = "old-image.png";
        image.MimeType = GltfMimeTypes.Png;
        image.HasResource = true;
        image.Resource = 1;
        document.Images.Add(image);
        GltfWriteOptions options = CreateOptions(GltfResourceMode.External);

        GltfWriteResult result = new GltfWriter().WriteBytes(document, options);
        string json = Encoding.UTF8.GetString(result.PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"uri\":\"buffers/original.bin\""));
            Assert.That(json, Does.Contain("\"uri\":\"textures/picture.png\""));
            Assert.That(json, Does.Not.Contain("old-buffer.bin"));
            Assert.That(json, Does.Not.Contain("old-image.png"));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.Resources.Resources[0].Bytes,
                Is.Not.SameAs(bufferResource.Bytes));
            Assert.That(result.Manifest, Has.Count.EqualTo(2));
            Assert.That(result.Manifest[0].ContentHash, Has.Length.EqualTo(64));
            Assert.That(result.Manifest[0].Mode, Is.EqualTo(GltfResourceMode.External));
            Assert.That(buffer.Uri, Is.EqualTo("old-buffer.bin"));
            Assert.That(image.Uri, Is.EqualTo("old-image.png"));
            Assert.That(bufferResource.EffectiveUri, Is.Null);
            Assert.That(imageResource.EffectiveUri, Is.Null);
        });
    }

    [Test]
    public void WriteBytes_EmbedsAndDeduplicatesBoundImageResources()
    {
        GltfDocument document = new GltfDocument();
        GltfResource firstResource = CreateResource(
            GltfResourceKind.Image,
            "first.png",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [1, 2, 3]);
        GltfResource secondResource = CreateResource(
            GltfResourceKind.Image,
            "second.png",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [1, 2, 3]);
        document.Resources.Resources.Add(firstResource);
        document.Resources.Resources.Add(secondResource);
        document.Images.Add(CreateBoundImage("first.png", 0));
        document.Images.Add(CreateBoundImage("second.png", 1));

        GltfWriteResult result = new GltfWriter().WriteBytes(
            document,
            CreateOptions(GltfResourceMode.DataUri));
        string json = Encoding.UTF8.GetString(result.PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(CountOccurrences(json, "data:image/png;base64,AQID"), Is.EqualTo(2));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(result.Resources.Resources[0].Embedded, Is.True);
            Assert.That(result.Manifest, Has.Count.EqualTo(1));
            Assert.That(result.Manifest[0].Embedded, Is.True);
            Assert.That(result.Manifest[0].Mode, Is.EqualTo(GltfResourceMode.DataUri));
            Assert.That(document.Images[0].Uri, Is.EqualTo("first.png"));
            Assert.That(document.Images[1].Uri, Is.EqualTo("second.png"));
            Assert.That(firstResource.Embedded, Is.False);
            Assert.That(secondResource.Embedded, Is.False);
        });
    }

    [Test]
    public void WriteBytes_EmitsCanonicalDataUrisWithRfc4648Padding()
    {
        GltfDocument document = new GltfDocument();
        document.Resources.Resources.Add(CreateResource(
            GltfResourceKind.Image,
            "first.png",
            "IMAGE/PNG",
            GltfImageFormat.Png,
            [102]));
        document.Resources.Resources.Add(CreateResource(
            GltfResourceKind.Image,
            "second.jpg",
            GltfMimeTypes.Jpeg,
            GltfImageFormat.Jpeg,
            [102, 111]));
        document.Images.Add(CreateBoundImage("first.png", 0));
        GltfImage second = CreateBoundImage("second.jpg", 1);
        second.MimeType = GltfMimeTypes.Jpeg;
        document.Images.Add(second);

        GltfWriteResult result = new GltfWriter().WriteBytes(
            document,
            CreateOptions(GltfResourceMode.DataUri));
        JsonObject json = JsonNode.Parse(Encoding.UTF8.GetString(result.PrimaryBytes))!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(
                json["images"]![0]!["uri"]!.GetValue<string>(),
                Is.EqualTo("data:image/png;base64,Zg=="));
            Assert.That(
                json["images"]![1]!["uri"]!.GetValue<string>(),
                Is.EqualTo("data:image/jpeg;base64,Zm8="));
            Assert.That(
                result.Manifest.Select(entry => entry.Uri),
                Is.EqualTo(new[]
                {
                    "data:image/png;base64,Zg==",
                    "data:image/jpeg;base64,Zm8="
                }));
        });
    }

    [Test]
    public void WriteBytes_UsesContentHashExternalNamesWhenEnabled()
    {
        GltfDocument document = new GltfDocument();
        GltfResource resource = CreateResource(
            GltfResourceKind.Image,
            "textures/source.png",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [1, 2, 3]);
        document.Resources.Resources.Add(resource);
        document.Images.Add(CreateBoundImage("textures/source.png", 0));
        GltfWriteOptions options = CreateOptions(GltfResourceMode.External);
        options.UseContentHashResourceNames = true;

        GltfWriteResult result = new GltfWriter().WriteBytes(document, options);

        Assert.That(
            result.Resources.Resources[0].EffectiveUri,
            Is.EqualTo(
                "textures/039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81.png"));
    }

    [Test]
    public void WriteBytes_RepeatedPlansAllocateIdenticalExternalNames()
    {
        GltfDocument document = new GltfDocument();
        GltfResource firstResource = CreateResource(
            GltfResourceKind.Image,
            "textures/color.png",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [1]);
        GltfResource secondResource = CreateResource(
            GltfResourceKind.Image,
            "textures/color.jpeg",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [2]);
        document.Resources.Resources.Add(firstResource);
        document.Resources.Resources.Add(secondResource);
        document.Images.Add(CreateBoundImage("first.png", 0));
        document.Images.Add(CreateBoundImage("second.png", 1));
        GltfWriter writer = new GltfWriter();
        GltfWriteOptions options = CreateOptions(GltfResourceMode.External);

        GltfWriteResult first = writer.WriteBytes(document, options);
        GltfWriteResult second = writer.WriteBytes(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.PrimaryBytes, Is.EqualTo(second.PrimaryBytes));
            Assert.That(first.Manifest.Select(entry => entry.Uri),
                Is.EqualTo(second.Manifest.Select(entry => entry.Uri)));
            Assert.That(first.Manifest.Select(entry => entry.Uri),
                Is.EqualTo(new[] { "textures/color.png", "textures/color-1.png" }));
        });
    }

    [Test]
    public void WriteBytes_ExternalGlbOmitsTheObsoleteEmbeddedBinChunk()
    {
        GltfDocument document = new GltfDocument();
        document.Container = new GltfContainerData();
        document.Container.Format = GltfContainerFormat.Glb;
        document.Container.HasBinChunk = true;
        document.Container.BinBytes = [1, 2, 3];
        GltfResource resource = CreateResource(
            GltfResourceKind.Buffer,
            "buffer.bin",
            GltfMimeTypes.GltfBuffer,
            GltfImageFormat.Unknown,
            [1, 2, 3]);
        resource.Embedded = true;
        document.Resources.Resources.Add(resource);
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 3;
        buffer.HasResource = true;
        buffer.Resource = 0;
        document.Buffers.Add(buffer);
        GltfWriteOptions options = CreateOptions(GltfResourceMode.External);
        options.Format = GltfContainerFormat.Glb;

        GltfWriteResult result = new GltfWriter().WriteBytes(document, options);
        GlbReader reader = new GlbReader();
        GltfContainerData parsed = reader.ReadChunks(
            result.PrimaryBytes,
            reader.ReadHeader(result.PrimaryBytes, result.PrimaryBytes.Length),
            false);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.HasBinChunk, Is.False);
            Assert.That(Encoding.UTF8.GetString(parsed.JsonBytes),
                Does.Contain("\"uri\":\"buffer.bin\""));
            Assert.That(document.Container.HasBinChunk, Is.True);
            Assert.That(document.Container.BinBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void WriteBytes_ExternalizesABufferViewImageWithoutChangingItsSourceFields()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 4;
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.ByteLength = 4;
        document.BufferViews.Add(view);
        GltfImage image = new GltfImage();
        image.HasBufferView = true;
        image.BufferView = 0;
        image.MimeType = GltfMimeTypes.Png;
        image.HasResource = true;
        image.Resource = 0;
        document.Images.Add(image);
        document.Resources.Resources.Add(CreateResource(
            GltfResourceKind.Image,
            "buffer-view:0",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [1, 2, 3, 4]));

        GltfWriteResult result = new GltfWriter().WriteBytes(
            document,
            CreateOptions(GltfResourceMode.External));
        string json = Encoding.UTF8.GetString(result.PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\"uri\":\"0.png\""));
            Assert.That(json, Does.Not.Contain("\"bufferView\":0"));
            Assert.That(image.HasBufferView, Is.True);
            Assert.That(image.BufferView, Is.Zero);
            Assert.That(image.Uri, Is.Null);
        });
    }

    [Test]
    public void WriteBytes_PacksImagesIntoAppendedViewsWithoutChangingAccessorLayout()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer geometryBuffer = new GltfBuffer();
        geometryBuffer.Uri = "geometry.bin";
        geometryBuffer.ByteLength = 8;
        geometryBuffer.HasResource = true;
        geometryBuffer.Resource = 0;
        document.Buffers.Add(geometryBuffer);
        GltfBufferView geometryView = new GltfBufferView();
        geometryView.Buffer = 0;
        geometryView.ByteLength = 8;
        document.BufferViews.Add(geometryView);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.ComponentType = GltfComponentType.UnsignedByte;
        accessor.Count = 1;
        accessor.Type = GltfAccessorType.Scalar;
        document.Accessors.Add(accessor);
        document.Resources.Resources.Add(CreateResource(
            GltfResourceKind.Buffer,
            "geometry.bin",
            GltfMimeTypes.GltfBuffer,
            GltfImageFormat.Unknown,
            [9, 8, 7, 6, 5, 4, 3, 2]));
        document.Resources.Resources.Add(CreateResource(
            GltfResourceKind.Image,
            "first.png",
            GltfMimeTypes.Png,
            GltfImageFormat.Png,
            [1, 2, 3]));
        document.Resources.Resources.Add(CreateResource(
            GltfResourceKind.Image,
            "second.jpg",
            GltfMimeTypes.Jpeg,
            GltfImageFormat.Jpeg,
            [4, 5]));
        document.Images.Add(CreateBoundImage("first.png", 1));
        GltfImage secondImage = CreateBoundImage("second.jpg", 2);
        secondImage.MimeType = GltfMimeTypes.Jpeg;
        document.Images.Add(secondImage);

        GltfWriteResult result = new GltfWriter().WriteBytes(
            document,
            CreateOptions(GltfResourceMode.BufferView));
        JsonObject json = JsonNode.Parse(Encoding.UTF8.GetString(result.PrimaryBytes))!.AsObject();
        JsonArray buffers = json["buffers"]!.AsArray();
        JsonArray views = json["bufferViews"]!.AsArray();
        JsonArray images = json["images"]!.AsArray();
        string packedUri = buffers[1]!["uri"]!.GetValue<string>();
        DataUriData packedData = new DataUriCodec().ParseDataUri(packedUri);

        Assert.Multiple(() =>
        {
            Assert.That(buffers, Has.Count.EqualTo(2));
            Assert.That(buffers[0]!["uri"]!.GetValue<string>(), Is.EqualTo("geometry.bin"));
            Assert.That(buffers[1]!["byteLength"]!.GetValue<int>(), Is.EqualTo(6));
            Assert.That(packedData.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 0, 4, 5 }));
            Assert.That(views, Has.Count.EqualTo(3));
            Assert.That(views[0]!["buffer"]!.GetValue<int>(), Is.Zero);
            Assert.That(views[0]!["byteLength"]!.GetValue<int>(), Is.EqualTo(8));
            Assert.That(views[1]!["buffer"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(views[1]!["byteLength"]!.GetValue<int>(), Is.EqualTo(3));
            Assert.That(views[2]!["buffer"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(views[2]!["byteOffset"]!.GetValue<int>(), Is.EqualTo(4));
            Assert.That(views[2]!["byteLength"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(images[0]!["uri"], Is.Null);
            Assert.That(images[0]!["bufferView"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(images[0]!["mimeType"]!.GetValue<string>(), Is.EqualTo(GltfMimeTypes.Png));
            Assert.That(images[1]!["bufferView"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(json["accessors"]![0]!["bufferView"]!.GetValue<int>(), Is.Zero);
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(2));
            Assert.That(result.Manifest, Has.Count.EqualTo(2));
            Assert.That(document.Buffers, Has.Count.EqualTo(1));
            Assert.That(document.BufferViews, Has.Count.EqualTo(1));
            Assert.That(document.Images[0].Uri, Is.EqualTo("first.png"));
            Assert.That(document.Images[0].HasBufferView, Is.False);
        });
    }

    private static GltfResource CreateResource(
        GltfResourceKind kind,
        string sourceUri,
        string mimeType,
        GltfImageFormat imageFormat,
        byte[] bytes)
    {
        GltfResource resource = new GltfResource();
        resource.Kind = kind;
        resource.UriKind = GltfUriKind.Relative;
        resource.SourceUri = sourceUri;
        resource.MimeType = mimeType;
        resource.ImageFormat = imageFormat;
        resource.Bytes = bytes;
        return resource;
    }

    private static GltfImage CreateBoundImage(string uri, int resourceIndex)
    {
        GltfImage image = new GltfImage();
        image.Uri = uri;
        image.MimeType = GltfMimeTypes.Png;
        image.HasResource = true;
        image.Resource = resourceIndex;
        return image;
    }

    private static GltfWriteOptions CreateOptions(GltfResourceMode mode)
    {
        GltfWriteOptions options = new GltfWriteOptions();
        options.Format = GltfContainerFormat.Json;
        options.ResourceMode = mode;
        options.Validate = false;
        return options;
    }

    private static int CountOccurrences(string value, string target)
    {
        int count = 0;
        int index = 0;
        while (index < value.Length)
        {
            int matchIndex = value.IndexOf(target, index, StringComparison.Ordinal);
            if (matchIndex < 0)
            {
                return count;
            }

            count++;
            index = matchIndex + target.Length;
        }

        return count;
    }
}
