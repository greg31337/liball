using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfWriterTests
{
    private static readonly byte[] MinimalJson = Encoding.UTF8.GetBytes(
        "{\"asset\":{\"version\":\"2.0\"}}");

    [Test]
    public void WriteBytesAndJsonText_UseTheDeterministicJsonOutput()
    {
        GltfDocument document = new GltfDocument();
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Json);
        GltfWriter writer = new GltfWriter();

        GltfWriteResult bytesResult = writer.WriteBytes(document, options);
        GltfWriteResult textResult = writer.WriteJsonText(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(bytesResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(bytesResult.PrimaryBytes, Is.EqualTo(MinimalJson));
            Assert.That(bytesResult.JsonText, Is.Null);
            Assert.That(textResult.PrimaryBytes, Is.EqualTo(MinimalJson));
            Assert.That(textResult.JsonText, Is.EqualTo(Encoding.UTF8.GetString(MinimalJson)));
        });

        bytesResult.PrimaryBytes[0] = 0;
        Assert.That(textResult.PrimaryBytes[0], Is.EqualTo((byte)'{'));
    }

    [Test]
    public void WriteBytes_AutoPreservesGlbChunksFromContainerProvenance()
    {
        GltfDocument document = new GltfDocument();
        document.Container = new GltfContainerData();
        document.Container.Format = GltfContainerFormat.Glb;
        document.Container.HasBinChunk = true;
        document.Container.BinBytes = [1, 2, 3];
        GltfGlbChunk chunk = new GltfGlbChunk();
        chunk.Type = 0x12345678;
        chunk.Data = [4, 5, 6, 7];
        document.Container.UnknownChunks.Add(chunk);
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Auto);

        GltfWriteResult result = new GltfWriter().WriteBytes(document, options);
        GlbReader reader = new GlbReader();
        GltfContainerData parsed = reader.ReadChunks(
            result.PrimaryBytes,
            reader.ReadHeader(result.PrimaryBytes, result.PrimaryBytes.Length),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.JsonBytes, Is.EqualTo(new byte[]
            {
                0x7B, 0x22, 0x61, 0x73, 0x73, 0x65, 0x74, 0x22,
                0x3A, 0x7B, 0x22, 0x76, 0x65, 0x72, 0x73, 0x69,
                0x6F, 0x6E, 0x22, 0x3A, 0x22, 0x32, 0x2E, 0x30,
                0x22, 0x7D, 0x7D, 0x20
            }));
            Assert.That(parsed.HasBinChunk, Is.True);
            Assert.That(parsed.BinBytes, Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
            Assert.That(parsed.UnknownChunks, Has.Count.EqualTo(1));
            Assert.That(parsed.UnknownChunks[0].Type, Is.EqualTo(0x12345678));
            Assert.That(parsed.UnknownChunks[0].Data, Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(document.Container.BinBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void WriteBytes_CopiesPreservedResourcesAndBuildsManifest()
    {
        GltfDocument document = new GltfDocument();
        GltfResource resource = new GltfResource();
        resource.Kind = GltfResourceKind.Image;
        resource.UriKind = GltfUriKind.Relative;
        resource.SourceUri = "source.png";
        resource.EffectiveUri = "images/output.png";
        resource.MimeType = "image/png";
        resource.Bytes = [1, 2, 3];
        resource.CacheKey = "image-key";
        resource.Embedded = false;
        document.Resources.Resources.Add(resource);
        document.Resources.Cache[resource.CacheKey] = resource;

        GltfWriteResult result = new GltfWriter().WriteBytes(
            document,
            CreateOptions(GltfContainerFormat.Json));

        Assert.Multiple(() =>
        {
            Assert.That(result.Resources, Is.Not.SameAs(document.Resources));
            Assert.That(result.Resources.Resources, Has.Count.EqualTo(1));
            Assert.That(result.Resources.Resources[0], Is.Not.SameAs(resource));
            Assert.That(result.Resources.Resources[0].Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.Resources.Cache["image-key"], Is.SameAs(result.Resources.Resources[0]));
            Assert.That(result.Manifest, Has.Count.EqualTo(1));
            Assert.That(result.Manifest[0].Kind, Is.EqualTo(GltfResourceKind.Image));
            Assert.That(result.Manifest[0].Uri, Is.EqualTo("images/output.png"));
            Assert.That(result.Manifest[0].MimeType, Is.EqualTo("image/png"));
            Assert.That(result.Manifest[0].ByteLength, Is.EqualTo(3));
            Assert.That(result.Manifest[0].Mode, Is.EqualTo(GltfResourceMode.Preserve));
        });

        resource.Bytes[0] = 9;
        Assert.That(result.Resources.Resources[0].Bytes[0], Is.EqualTo(1));
    }

    [Test]
    public void WriteBytes_RejectsResourceRelayoutReservedForGlbPacking()
    {
        GltfDocument document = new GltfDocument();
        document.Resources.Resources.Add(new GltfResource());
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Json);
        options.ResourceMode = GltfResourceMode.GlbBin;

        Assert.That(
            () => new GltfWriter().WriteBytes(document, options),
            Throws.TypeOf<GltfUnsupportedFeatureException>()
                .With.Message.EqualTo("Resource layout mode GlbBin is not implemented."));
    }

    [Test]
    public void WriteBytes_UsesOptionalValidationAndReturnsInvalidWithoutOutput()
    {
        WriterRecordingValidator validator = new WriterRecordingValidator();
        validator.Report.IsValid = false;
        GltfWriter writer = new GltfWriter();
        writer.Validator = validator;
        GltfDocument document = new GltfDocument();
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Json);
        options.Validate = true;

        GltfWriteResult result = writer.WriteBytes(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(validator.DocumentCallCount, Is.EqualTo(1));
            Assert.That(validator.Document, Is.SameAs(document));
            Assert.That(validator.Options, Is.SameAs(options.ValidationOptions));
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.PrimaryBytes, Is.Empty);
            Assert.That(result.Validation, Is.SameAs(validator.Report));
        });

        options.Validate = false;
        Assert.That(writer.WriteBytes(document, options).Status, Is.EqualTo(GltfOperationStatus.Success));
        Assert.That(validator.DocumentCallCount, Is.EqualTo(1));

        validator.Report = new GltfValidationReport();
        options.Validate = true;
        GltfWriteResult validResult = writer.WriteBytes(document, options);
        Assert.Multiple(() =>
        {
            Assert.That(validResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(validResult.PrimaryBytes, Is.EqualTo(MinimalJson));
            Assert.That(validResult.Validation, Is.SameAs(validator.Report));
            Assert.That(validator.DocumentCallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void WriteStream_DoesNotModifyOutputWhenValidationFailsAndAppliesOwnership()
    {
        WriterRecordingValidator validator = new WriterRecordingValidator();
        validator.Report.IsValid = false;
        GltfWriter writer = new GltfWriter();
        writer.Validator = validator;
        TrackingNonSeekableStream stream = new TrackingNonSeekableStream([9, 8], false, true, 1);
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Json);
        options.Validate = true;
        options.LeaveStreamOpen = false;

        GltfWriteResult result = writer.WriteStream(new GltfDocument(), stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(stream.GetBytes(), Is.EqualTo(new byte[] { 9, 8 }));
            Assert.That(stream.Disposed, Is.True);
        });
    }

    [Test]
    public void WriteFile_InfersFormatFromExtensionWithoutMutatingOptions()
    {
        string jsonPath = Path.Combine(Path.GetTempPath(), $"gltf-writer-{Guid.NewGuid():N}.gltf");
        string glbPath = Path.Combine(Path.GetTempPath(), $"gltf-writer-{Guid.NewGuid():N}.glb");
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Auto);
        GltfWriter writer = new GltfWriter();

        try
        {
            GltfWriteResult jsonResult = writer.WriteFile(new GltfDocument(), jsonPath, options);
            GltfWriteResult glbResult = writer.WriteFile(new GltfDocument(), glbPath, options);
            byte[] glbBytes = File.ReadAllBytes(glbPath);

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllBytes(jsonPath), Is.EqualTo(MinimalJson));
                Assert.That(glbBytes[0], Is.EqualTo((byte)'g'));
                Assert.That(glbBytes[1], Is.EqualTo((byte)'l'));
                Assert.That(glbBytes[2], Is.EqualTo((byte)'T'));
                Assert.That(glbBytes[3], Is.EqualTo((byte)'F'));
                Assert.That(jsonResult.PrimaryBytes, Is.Empty);
                Assert.That(glbResult.PrimaryBytes, Is.Empty);
                Assert.That(options.Format, Is.EqualTo(GltfContainerFormat.Auto));
            });
        }
        finally
        {
            File.Delete(jsonPath);
            File.Delete(glbPath);
        }
    }

    [Test]
    public void WriteStream_FinitenessPreflightLeavesTargetUnmodified()
    {
        GltfDocument document = new GltfDocument();
        GltfNode node = new GltfNode();
        node.HasTranslation = true;
        node.Translation = [0.0, double.NaN, 0.0];
        document.Nodes.Add(node);
        using MemoryStream stream = new MemoryStream();
        stream.WriteByte(9);
        stream.WriteByte(8);

        Assert.That(
            () => new GltfWriter().WriteStream(
                document,
                stream,
                CreateOptions(GltfContainerFormat.Json)),
            Throws.TypeOf<GltfWriteException>()
                .With.Message.EqualTo("Numeric value /nodes/0/translation/1 must be finite."));
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 9, 8 }));
    }

    [Test]
    public void WriteFile_ReferencePreflightLeavesExistingTargetUnmodified()
    {
        string path = Path.Combine(Path.GetTempPath(), $"gltf-writer-preflight-{Guid.NewGuid():N}.gltf");
        byte[] original = [9, 8, 7];
        File.WriteAllBytes(path, original);
        GltfDocument document = new GltfDocument();
        document.HasDefaultScene = true;
        document.DefaultScene = 0;

        try
        {
            Assert.That(
                () => new GltfWriter().WriteFile(
                    document,
                    path,
                    CreateOptions(GltfContainerFormat.Json)),
                Throws.TypeOf<GltfWriteException>()
                    .With.Message.EqualTo(
                        "Reference /scene index 0 is outside the target collection of length 0."));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Writer_OverflowPreflightRejectsRangesBeforeModifyingAStream()
    {
        GltfDocument bufferViewDocument = new GltfDocument();
        bufferViewDocument.Buffers.Add(new GltfBuffer());
        GltfBufferView overflowingView = new GltfBufferView();
        overflowingView.Buffer = 0;
        overflowingView.HasByteOffset = true;
        overflowingView.ByteOffset = int.MaxValue;
        overflowingView.ByteLength = 1;
        bufferViewDocument.BufferViews.Add(overflowingView);

        GltfDocument accessorDocument = new GltfDocument();
        GltfAccessor overflowingAccessor = new GltfAccessor();
        overflowingAccessor.Count = int.MaxValue;
        overflowingAccessor.Type = GltfAccessorType.Vector4;
        overflowingAccessor.ComponentType = GltfComponentType.Float;
        accessorDocument.Accessors.Add(overflowingAccessor);
        using MemoryStream stream = new MemoryStream();
        stream.WriteByte(6);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GltfWriter().WriteBytes(
                    bufferViewDocument,
                    CreateOptions(GltfContainerFormat.Json)),
                Throws.TypeOf<GltfWriteException>()
                    .With.Message.EqualTo(
                        "Byte range /bufferViews/0 exceeds the supported signed byte length."));
            Assert.That(
                () => new GltfWriter().WriteStream(
                    accessorDocument,
                    stream,
                    CreateOptions(GltfContainerFormat.Json)),
                Throws.TypeOf<GltfWriteException>()
                    .With.Message.EqualTo(
                        "Byte range /accessors/0 exceeds the supported signed byte length."));
        });
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 6 }));
    }

    [Test]
    public void WriteBytes_ReferencePreflightChecksNestedAndLocalIndices()
    {
        GltfDocument document = new GltfDocument();
        GltfAnimation animation = new GltfAnimation();
        GltfAnimationChannel channel = new GltfAnimationChannel();
        channel.Sampler = 0;
        animation.Channels.Add(channel);
        document.Animations.Add(animation);
        WriterRecordingValidator validator = new WriterRecordingValidator();
        GltfWriter writer = new GltfWriter();
        writer.Validator = validator;
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Json);
        options.Validate = true;

        Assert.That(
            () => writer.WriteBytes(document, options),
            Throws.TypeOf<GltfWriteException>()
                .With.Message.EqualTo(
                    "Reference /animations/0/channels/0/sampler index 0 is outside the target collection of length 0."));
        Assert.That(validator.DocumentCallCount, Is.Zero);
    }

    [Test]
    public void WriteBytes_PreflightAcceptsConsistentCoreReferences()
    {
        GltfDocument document = CreateReferencedDocument();

        GltfWriteResult result = new GltfWriter().WriteBytes(
            document,
            CreateOptions(GltfContainerFormat.Json));

        string json = Encoding.UTF8.GetString(result.PrimaryBytes);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(json, Does.Contain("\"scene\":0"));
            Assert.That(json, Does.Contain("\"inverseBindMatrices\":0"));
        });
    }

    [Test]
    public void WriteJsonText_ReturnsJsonEvenWhenContainerFormatIsGlb()
    {
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Glb);

        GltfWriteResult result = new GltfWriter().WriteJsonText(new GltfDocument(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.PrimaryBytes, Is.EqualTo(MinimalJson));
            Assert.That(result.JsonText, Is.EqualTo(Encoding.UTF8.GetString(MinimalJson)));
        });
    }

    private static GltfWriteOptions CreateOptions(GltfContainerFormat format)
    {
        GltfWriteOptions options = new GltfWriteOptions();
        options.Format = format;
        options.Validate = false;
        return options;
    }

    private static GltfDocument CreateReferencedDocument()
    {
        GltfDocument document = new GltfDocument();
        GltfBuffer buffer = new GltfBuffer();
        buffer.ByteLength = 64;
        document.Buffers.Add(buffer);
        GltfBufferView bufferView = new GltfBufferView();
        bufferView.Buffer = 0;
        bufferView.ByteLength = 64;
        document.BufferViews.Add(bufferView);
        GltfAccessor accessor = new GltfAccessor();
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        accessor.Count = 1;
        accessor.Type = GltfAccessorType.Vector3;
        document.Accessors.Add(accessor);

        GltfImage image = new GltfImage();
        image.HasBufferView = true;
        image.BufferView = 0;
        document.Images.Add(image);
        GltfTexture texture = new GltfTexture();
        texture.HasSource = true;
        texture.Source = 0;
        document.Textures.Add(texture);
        GltfMaterial material = new GltfMaterial();
        material.EmissiveTexture = new GltfTextureInfo();
        material.EmissiveTexture.Index = 0;
        document.Materials.Add(material);

        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfAttribute attribute = new GltfAttribute();
        attribute.Semantic = "POSITION";
        attribute.Accessor = 0;
        primitive.Attributes.Add(attribute);
        primitive.HasIndices = true;
        primitive.Indices = 0;
        primitive.HasMaterial = true;
        primitive.Material = 0;
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfCamera camera = new GltfCamera();
        camera.Perspective = new GltfPerspectiveCamera();
        document.Cameras.Add(camera);
        GltfNode firstNode = new GltfNode();
        firstNode.Children.Add(1);
        firstNode.HasCamera = true;
        firstNode.Camera = 0;
        firstNode.HasSkin = true;
        firstNode.Skin = 0;
        firstNode.HasMesh = true;
        firstNode.Mesh = 0;
        document.Nodes.Add(firstNode);
        document.Nodes.Add(new GltfNode());

        GltfSkin skin = new GltfSkin();
        skin.HasInverseBindMatrices = true;
        skin.InverseBindMatrices = 0;
        skin.HasSkeleton = true;
        skin.Skeleton = 0;
        skin.Joints.Add(1);
        document.Skins.Add(skin);
        GltfScene scene = new GltfScene();
        scene.Nodes.Add(0);
        document.Scenes.Add(scene);
        document.HasDefaultScene = true;
        document.DefaultScene = 0;

        GltfAnimationSampler animationSampler = new GltfAnimationSampler();
        animationSampler.Input = 0;
        animationSampler.Output = 0;
        GltfAnimationChannel animationChannel = new GltfAnimationChannel();
        animationChannel.Sampler = 0;
        animationChannel.Target.HasNode = true;
        animationChannel.Target.Node = 0;
        GltfAnimation animation = new GltfAnimation();
        animation.Samplers.Add(animationSampler);
        animation.Channels.Add(animationChannel);
        document.Animations.Add(animation);
        return document;
    }
}

internal sealed class WriterRecordingValidator : IGltfValidator
{
    internal GltfValidationReport Report = new GltfValidationReport();
    internal int DocumentCallCount;
    internal GltfDocument? Document;
    internal GltfValidationOptions? Options;

    public GltfValidationReport ValidateDocument(GltfDocument document, GltfValidationOptions options)
    {
        DocumentCallCount++;
        Document = document;
        Options = options;
        return Report;
    }

    public GltfValidationReport ValidateFile(string path, GltfValidationOptions options)
    {
        throw new NotSupportedException();
    }

    public GltfValidationReport ValidateBytes(byte[] bytes, GltfValidationOptions options)
    {
        throw new NotSupportedException();
    }

    public GltfValidationReport ValidateContainer(GltfContainerData container, GltfValidationOptions options)
    {
        throw new NotSupportedException();
    }
}
