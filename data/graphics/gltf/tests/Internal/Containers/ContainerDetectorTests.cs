namespace gltf.Tests.Internal.Containers;

[TestFixture]
public class ContainerDetectorTests
{
    [Test]
    public void Detect_AutomaticallyRecognizesJsonObjectPrefixes()
    {
        ContainerDetector detector = new ContainerDetector();

        Assert.Multiple(() =>
        {
            Assert.That(
                detector.Detect([0x7B, 0x7D], GltfContainerFormat.Auto, null),
                Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(
                detector.Detect([0x20, 0x09, 0x0A, 0x0D, 0x7B], GltfContainerFormat.Auto, null),
                Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(
                detector.Detect([0xEF, 0xBB, 0xBF, 0x20, 0x7B], GltfContainerFormat.Auto, null),
                Is.EqualTo(GltfContainerFormat.Json));
        });
    }

    [Test]
    public void Detect_AutomaticallyRecognizesExactGlbMagicAtByteZero()
    {
        ContainerDetector detector = new ContainerDetector();

        GltfContainerFormat format = detector.Detect(
            [0x67, 0x6C, 0x54, 0x46],
            GltfContainerFormat.Auto,
            null);

        Assert.That(format, Is.EqualTo(GltfContainerFormat.Glb));
    }

    [Test]
    public void Detect_ExplicitFormatOverridesContentAndEmptyInput()
    {
        ContainerDetector detector = new ContainerDetector();

        Assert.Multiple(() =>
        {
            Assert.That(
                detector.Detect([], GltfContainerFormat.Json, null),
                Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(
                detector.Detect([0x67, 0x6C, 0x54, 0x46], GltfContainerFormat.Json, null),
                Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(
                detector.Detect([], GltfContainerFormat.Glb, null),
                Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(
                detector.Detect([0x7B], GltfContainerFormat.Glb, null),
                Is.EqualTo(GltfContainerFormat.Glb));
        });
    }

    [Test]
    public void Detect_IgnoresMismatchedFileExtensions()
    {
        ContainerDetector detector = new ContainerDetector();

        Assert.Multiple(() =>
        {
            Assert.That(
                detector.Detect([0x7B], GltfContainerFormat.Auto, "model.glb"),
                Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(
                detector.Detect([0x67, 0x6C, 0x54, 0x46], GltfContainerFormat.Auto, "model.gltf"),
                Is.EqualTo(GltfContainerFormat.Glb));
        });
    }

    [Test]
    public void Detect_RejectsEmptyWhitespaceAndBomOnlyInput()
    {
        ContainerDetector detector = new ContainerDetector();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => detector.Detect([], GltfContainerFormat.Auto, null),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("The input does not contain a glTF document."));
            Assert.That(
                () => detector.Detect([0x20, 0x09, 0x0A, 0x0D], GltfContainerFormat.Auto, null),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("The input does not contain a glTF document."));
            Assert.That(
                () => detector.Detect([0xEF, 0xBB, 0xBF], GltfContainerFormat.Auto, "empty.gltf"),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("The input 'empty.gltf' does not contain a glTF document."));
        });
    }

    [Test]
    public void Detect_RejectsMalformedMagicAndUnknownPrefixes()
    {
        ContainerDetector detector = new ContainerDetector();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => detector.Detect([0x67, 0x6C, 0x54, 0x58], GltfContainerFormat.Auto, "bad.glb"),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("The input 'bad.glb' begins with malformed GLB magic."));
            Assert.That(
                () => detector.Detect([0xEF, 0xBB, 0xBF, 0x67, 0x6C, 0x54, 0x46], GltfContainerFormat.Auto, null),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("The input is neither JSON glTF nor binary GLB."));
            Assert.That(
                () => detector.Detect([0x5B, 0x5D], GltfContainerFormat.Auto, null),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("The input is neither JSON glTF nor binary GLB."));
        });
    }

    [Test]
    public void Detect_RejectsInvalidArguments()
    {
        ContainerDetector detector = new ContainerDetector();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => detector.Detect(null!, GltfContainerFormat.Auto, null),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("prefix"));
            Assert.That(
                () => detector.Detect([], (GltfContainerFormat)99, null),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("requestedFormat"));
        });
    }

    [Test]
    public void ReaderBytes_UsesDetectionBeforeDocumentParsing()
    {
        GltfReader reader = new GltfReader();
        GltfReadOptions options = new GltfReadOptions();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadBytes([], options),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("The input does not contain a glTF document."));
            Assert.That(
                reader.ReadBytes(
                    System.Text.Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}}"),
                    options).Document,
                Is.Not.Null);
        });
    }
}
