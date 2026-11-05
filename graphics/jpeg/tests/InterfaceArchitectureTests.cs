using System.Reflection;

namespace jpeg.Tests;

internal class InterfaceArchitectureTests
{
    [Test]
    public void JpegFactories_ExposeFunctionalReaderAndWriterContracts()
    {
        global::jpeg.IJpegIdentifier identifier = global::jpeg.Jpeg.CreateIdentifier();
        global::jpeg.IJpegReader reader = global::jpeg.Jpeg.CreateReader();
        global::jpeg.IJpegWriter writer = global::jpeg.Jpeg.CreateWriter();
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);

        byte[] data = writer.Write(source);
        global::jpeg.JpegInfo info = identifier.Identify(data);
        global::jpeg.JpegImage decoded = reader.Read(data);

        Assert.That(identifier, Is.InstanceOf<global::jpeg.JpegIdentificationService>());
        Assert.That(reader, Is.InstanceOf<global::jpeg.JpegImageReader>());
        Assert.That(writer, Is.InstanceOf<global::jpeg.JpegImageWriter>());
        Assert.That(identifier.GetType().IsPublic, Is.False);
        Assert.That(reader.GetType().IsPublic, Is.False);
        Assert.That(writer.GetType().IsPublic, Is.False);
        Assert.That(info.Width, Is.EqualTo(8));
        Assert.That(info.Height, Is.EqualTo(8));
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
    }

    [Test]
    public void JpegInterfaces_FollowRestrictedInterfacePolicy()
    {
        Type[] interfaces = typeof(global::jpeg.Jpeg).Assembly.GetTypes()
            .Where(type => type.IsInterface && type.Namespace == "jpeg")
            .ToArray();

        Assert.That(interfaces, Is.Not.Empty);
        foreach (Type interfaceType in interfaces)
        {
            Assert.That(interfaceType.Name.Length, Is.GreaterThan(1));
            Assert.That(interfaceType.Name[0], Is.EqualTo('I'));
            Assert.That(char.IsUpper(interfaceType.Name[1]), Is.True, interfaceType.FullName);
            Assert.That(interfaceType.GetInterfaces(), Is.Empty, interfaceType.FullName);
            Assert.That(interfaceType.GetFields(), Is.Empty, interfaceType.FullName);

            foreach (MethodInfo method in interfaceType.GetMethods())
            {
                Assert.That(method.IsStatic, Is.False, method.Name);
                Assert.That(method.IsGenericMethod, Is.False, method.Name);
                Assert.That(method.GetMethodBody(), Is.Null, method.Name);
            }
        }
    }

    [Test]
    public void InverseDctTransformer_UsesInjectableStrategyContract()
    {
        TrackingInverseDct accurate = new TrackingInverseDct(17, 117);
        TrackingInverseDct fast = new TrackingInverseDct(23, 123);
        global::jpeg.JpegInverseDctTransformer transformer = new global::jpeg.JpegInverseDctTransformer();
        transformer.Accurate = accurate;
        transformer.Fast = fast;
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        int[] accurateResult = transformer.InverseTransformBlock(coefficients, options);
        options.InverseDctMethod = global::jpeg.JpegInverseDctMethod.Fast;
        byte[] fastResult = transformer.InverseTransformBlockToSamples(coefficients, options);

        Assert.That(accurateResult[0], Is.EqualTo(17));
        Assert.That(fastResult[0], Is.EqualTo(123));
        Assert.That(accurate.TransformCallCount, Is.EqualTo(1));
        Assert.That(fast.SampleCallCount, Is.EqualTo(1));
    }

    [Test]
    public void ImageReader_UsesInjectableChromaUpsamplerContract()
    {
        TrackingChromaUpsampler upsampler = new TrackingChromaUpsampler();
        global::jpeg.JpegImageReader reader = new global::jpeg.JpegImageReader();
        reader.ChromaUpsampler = upsampler;
        global::jpeg.JpegPlanarImageBuffer source = new global::jpeg.JpegPlanarImageBuffer();
        source.Width = 2;
        source.Height = 2;
        source.ComponentCount = 3;
        source.Components.Add(CreateComponent(1, 2, 2));
        source.Components.Add(CreateComponent(2, 1, 1));
        source.Components.Add(CreateComponent(3, 1, 1));

        global::jpeg.JpegPlanarImageBuffer result = reader.UpsampleChroma(source);

        Assert.That(upsampler.CallCount, Is.EqualTo(2));
        Assert.That(result.Components[0], Is.SameAs(source.Components[0]));
        Assert.That(result.Components[1].Width, Is.EqualTo(2));
        Assert.That(result.Components[2].Height, Is.EqualTo(2));
    }

    [Test]
    public void AlgorithmImplementations_ImplementTheirStrategyContracts()
    {
        Assert.That(new global::jpeg.JpegAccurateIntegerInverseDct(), Is.InstanceOf<global::jpeg.IJpegInverseDct>());
        Assert.That(new global::jpeg.JpegFastIntegerInverseDct(), Is.InstanceOf<global::jpeg.IJpegInverseDct>());
        Assert.That(new global::jpeg.JpegFastBoxChromaUpsampler(), Is.InstanceOf<global::jpeg.IJpegChromaUpsampler>());
        Assert.That(new global::jpeg.JpegFancyChromaUpsampler(), Is.InstanceOf<global::jpeg.IJpegChromaUpsampler>());
    }

    static global::jpeg.JpegPlanarComponentBuffer CreateComponent(int identifier, int width, int height)
    {
        global::jpeg.JpegPlanarComponentBuffer component = new global::jpeg.JpegPlanarComponentBuffer();
        component.Identifier = identifier;
        component.Width = width;
        component.Height = height;
        component.PaddedWidth = width;
        component.PaddedHeight = height;
        component.Stride = width;
        component.Samples = new byte[width * height];
        return component;
    }
}

internal class TrackingInverseDct : global::jpeg.IJpegInverseDct
{
    internal int TransformValue;
    internal int SampleValue;
    internal int TransformCallCount;
    internal int SampleCallCount;

    internal TrackingInverseDct(int transformValue, int sampleValue)
    {
        TransformValue = transformValue;
        SampleValue = sampleValue;
    }

    public int[] InverseTransformBlock(int[] coefficients)
    {
        TransformCallCount = TransformCallCount + 1;
        int[] result = new int[global::jpeg.JpegConstants.BlockSampleCount];
        result[0] = TransformValue;
        return result;
    }

    public byte[] InverseTransformBlockToSamples(int[] coefficients)
    {
        SampleCallCount = SampleCallCount + 1;
        byte[] result = new byte[global::jpeg.JpegConstants.BlockSampleCount];
        result[0] = (byte)SampleValue;
        return result;
    }

    public void CopyInverseTransformBlock(int[] coefficients, int[] destination)
    {
        TransformCallCount = TransformCallCount + 1;
        destination[0] = TransformValue;
    }

    public void CopyInverseTransformBlockToSamples(int[] coefficients, byte[] destination)
    {
        SampleCallCount = SampleCallCount + 1;
        destination[0] = (byte)SampleValue;
    }
}

internal class TrackingChromaUpsampler : global::jpeg.IJpegChromaUpsampler
{
    internal int CallCount;

    public global::jpeg.JpegPlanarComponentBuffer UpsampleComponent(
        global::jpeg.JpegPlanarComponentBuffer source,
        int outputWidth,
        int outputHeight)
    {
        CallCount = CallCount + 1;
        global::jpeg.JpegPlanarComponentBuffer result = new global::jpeg.JpegPlanarComponentBuffer();
        result.Identifier = source.Identifier;
        result.Width = outputWidth;
        result.Height = outputHeight;
        result.PaddedWidth = outputWidth;
        result.PaddedHeight = outputHeight;
        result.Stride = outputWidth;
        result.Samples = new byte[outputWidth * outputHeight];
        return result;
    }
}
