namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class AttributeSemanticTests
{
    [TestCase("POSITION", 1)]
    [TestCase("NORMAL", 2)]
    [TestCase("TANGENT", 3)]
    public void ParseAttributeSemantic_RecognizesUnnumberedCoreSemantics(
        string text,
        int kind)
    {
        AttributeSemanticParser parser = new AttributeSemanticParser();

        AttributeSemantic result = parser.ParseAttributeSemantic(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Kind, Is.EqualTo((AttributeSemanticKind)kind));
            Assert.That(result.HasSetIndex, Is.False);
            Assert.That(parser.FormatParsedSemantic(result), Is.EqualTo(text));
        });
    }

    [TestCase("TEXCOORD_0", 4, 0)]
    [TestCase("COLOR_2", 5, 2)]
    [TestCase("JOINTS_12", 6, 12)]
    [TestCase("WEIGHTS_2147483647", 7, int.MaxValue)]
    public void ParseAttributeSemantic_RecognizesCanonicalNumberedSemantics(
        string text,
        int kind,
        int setIndex)
    {
        AttributeSemanticParser parser = new AttributeSemanticParser();

        AttributeSemantic result = parser.ParseAttributeSemantic(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Kind, Is.EqualTo((AttributeSemanticKind)kind));
            Assert.That(result.HasSetIndex, Is.True);
            Assert.That(result.SetIndex, Is.EqualTo(setIndex));
            Assert.That(parser.FormatParsedSemantic(result), Is.EqualTo(text));
        });
    }

    [TestCase("")]
    [TestCase("position")]
    [TestCase("TEXCOORD")]
    [TestCase("TEXCOORD_")]
    [TestCase("TEXCOORD_01")]
    [TestCase("COLOR_-1")]
    [TestCase("JOINTS_2147483648")]
    [TestCase("WEIGHTS_1x")]
    [TestCase("UNKNOWN")]
    public void ParseAttributeSemantic_RejectsMalformedOrUnknownCoreForms(string text)
    {
        AttributeSemantic result = new AttributeSemanticParser().ParseAttributeSemantic(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Kind, Is.EqualTo(AttributeSemanticKind.Invalid));
        });
    }

    [Test]
    public void ParseAndFormatAttributeSemantic_PreserveCustomSemantics()
    {
        AttributeSemanticParser parser = new AttributeSemanticParser();

        AttributeSemantic result = parser.ParseAttributeSemantic("_TEMPERATURE_DELTA");

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(AttributeSemanticKind.Custom));
            Assert.That(result.Name, Is.EqualTo("TEMPERATURE_DELTA"));
            Assert.That(parser.FormatParsedSemantic(result), Is.EqualTo("_TEMPERATURE_DELTA"));
            Assert.That(parser.FormatCustomSemantic("APPLICATION_ID"), Is.EqualTo("_APPLICATION_ID"));
            Assert.That(parser.ParseAttributeSemantic("_").Valid, Is.False);
        });
    }

    [Test]
    public void ParseAndFormatAttributeSemantic_SupportNamespacedExtensionSemantics()
    {
        AttributeSemanticParser parser = new AttributeSemanticParser();
        string text = "KHR_gaussian_splatting:SH_DEGREE_2_COEF_4";

        AttributeSemantic result = parser.ParseAttributeSemantic(text);
        AttributeSemantic future = parser.ParseAttributeSemantic("VENDOR_future:DATA_0");

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(AttributeSemanticKind.Extension));
            Assert.That(result.ExtensionName, Is.EqualTo(GltfExtensionNames.KhrGaussianSplatting));
            Assert.That(result.Name, Is.EqualTo("SH_DEGREE_2_COEF_4"));
            Assert.That(parser.FormatParsedSemantic(result), Is.EqualTo(text));
            Assert.That(future.Valid, Is.True);
            Assert.That(parser.FormatParsedSemantic(future), Is.EqualTo("VENDOR_future:DATA_0"));
        });
    }

    [TestCase("TRANSLATION")]
    [TestCase("ROTATION")]
    [TestCase("SCALE")]
    public void ParseAndFormatAttributeSemantic_SupportGpuInstancingSemantics(string text)
    {
        AttributeSemanticParser parser = new AttributeSemanticParser();

        AttributeSemantic result = parser.ParseAttributeSemantic(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(AttributeSemanticKind.Extension));
            Assert.That(result.ExtensionName, Is.EqualTo(GltfExtensionNames.ExtMeshGpuInstancing));
            Assert.That(result.Name, Is.EqualTo(text));
            Assert.That(parser.FormatParsedSemantic(result), Is.EqualTo(text));
        });
    }

    [TestCase(":ROTATION")]
    [TestCase("KHR_gaussian_splatting:")]
    [TestCase("KHR_gaussian_splatting:ROTATION:EXTRA")]
    [TestCase("KHR-gaussian-splatting:ROTATION")]
    [TestCase("KHR_gaussian_splatting:BAD VALUE")]
    public void ParseAttributeSemantic_RejectsMalformedExtensionForms(string text)
    {
        AttributeSemantic result = new AttributeSemanticParser().ParseAttributeSemantic(text);

        Assert.That(result.Valid, Is.False);
    }

    [Test]
    public void FormatHelpers_ProduceCanonicalCoreSemanticsAndRejectInvalidInputs()
    {
        AttributeSemanticParser parser = new AttributeSemanticParser();

        Assert.Multiple(() =>
        {
            Assert.That(
                parser.FormatUnnumberedCoreSemantic(AttributeSemanticKind.Position),
                Is.EqualTo("POSITION"));
            Assert.That(
                parser.FormatNumberedCoreSemantic(AttributeSemanticKind.TextureCoordinate, 7),
                Is.EqualTo("TEXCOORD_7"));
            Assert.That(
                parser.FormatExtensionSemantic(
                    GltfExtensionNames.KhrGaussianSplatting,
                    "OPACITY"),
                Is.EqualTo("KHR_gaussian_splatting:OPACITY"));
            Assert.That(
                () => parser.FormatNumberedCoreSemantic(AttributeSemanticKind.Color, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => parser.FormatNumberedCoreSemantic(AttributeSemanticKind.Position, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => parser.FormatCustomSemantic(""),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => parser.FormatExtensionSemantic("BAD-NAME", "VALUE"),
                Throws.TypeOf<ArgumentException>());
        });
    }
}
