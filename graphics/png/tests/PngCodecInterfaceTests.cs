using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngCodecInterfaceTests
{
    [Test]
    public void Codec_ImplementsDecoderAndEncoderRoles()
    {
        IPngEncoder encoder = new PngCodec();
        IPngDecoder decoder = new PngCodec();
        PngImage source = Png.CreateImage(1, 1, PngPixelFormat.Rgba8, new byte[] { 10, 20, 30, 255 });

        byte[] encoded = encoder.EncodeToBytes(source, new PngEncoderOptions());
        PngImage decoded = decoder.DecodeBytes(encoded, new PngDecoderOptions());

        Assert.That(decoded.Pixels.Width, Is.EqualTo(1));
        Assert.That(decoded.Pixels.Height, Is.EqualTo(1));
        Assert.That(decoded.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(source.Pixels.Data));

        MemoryStream stream = new MemoryStream();
        encoder.EncodeToStream(stream, source, new PngEncoderOptions());
        stream.Position = 0;
        PngImage streamDecoded = decoder.DecodeStream(stream, new PngDecoderOptions());
        Assert.That(streamDecoded.Pixels.Data, Is.EqualTo(source.Pixels.Data));
    }

    [Test]
    public void CodecInterfaces_KeepTheInjectableContractSmall()
    {
        Assert.That(typeof(IPngDecoder).GetMethods().Select(value => value.Name), Is.EquivalentTo(new string[]
        {
            "DecodeBytes",
            "DecodeBytesAtOffset",
            "DecodeSpan",
            "DecodeStream"
        }));
        Assert.That(typeof(IPngEncoder).GetMethods(), Has.Length.EqualTo(2));
        Assert.That(typeof(IPngDecoder).GetFields(), Is.Empty);
        Assert.That(typeof(IPngEncoder).GetFields(), Is.Empty);
        Assert.That(typeof(IPngDecoder).GetInterfaces(), Is.Empty);
        Assert.That(typeof(IPngEncoder).GetInterfaces(), Is.Empty);
    }

    [Test]
    public void CompressionTrials_UseInjectedEncoderWithoutRunningRealCompression()
    {
        RecordingEncoder encoder = new RecordingEncoder();
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 2;
        options.MaximumCompressionLevel = 4;
        options.UseEarlyBailout = false;
        PngImage image = new PngImage();

        PngCompressionTrialResult result = PngCompressionLevelTrialRunner.Run(image, options, encoder);

        Assert.That(encoder.Images, Has.Count.EqualTo(3));
        Assert.That(encoder.Images, Has.All.SameAs(image));
        Assert.That(encoder.Options.Select(value => value.CompressionLevel), Is.EqualTo(new int[] { 2, 3, 4 }));
        Assert.That(result.BestTrial.CompressionLevel, Is.EqualTo(4));
        Assert.That(result.BestTrial.IsBest, Is.True);
    }

    [Test]
    public void CompressionTrials_PropagateExplicitRowFilterStrategy()
    {
        RecordingEncoder encoder = new RecordingEncoder();
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 2;
        options.MaximumCompressionLevel = 3;
        options.RowFilterStrategy = PngRowFilterStrategy.Fast;
        options.UseEarlyBailout = false;

        PngCompressionLevelTrialRunner.Run(new PngImage(), options, encoder);

        Assert.That(encoder.Options.Select(value => value.RowFilterStrategy), Is.All.EqualTo(PngRowFilterStrategy.Fast));
    }

    [Test]
    public void FilterTrials_UseInjectedEncoderAndExposeEachStrategy()
    {
        RecordingEncoder encoder = new RecordingEncoder();
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 9;
        options.UseEarlyBailout = false;

        PngFilterStrategyTrialResult result = PngFilterStrategyTrialRunner.Run(new PngImage(), options, encoder);

        Assert.That(encoder.Options, Has.Count.EqualTo(7));
        Assert.That(encoder.Options[0].RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Adaptive));
        Assert.That(encoder.Options[0].UseFixedFilter, Is.False);
        Assert.That(encoder.Options[1].RowFilterStrategy, Is.EqualTo(PngRowFilterStrategy.Fast));
        Assert.That(encoder.Options.Skip(2).Select(value => value.FixedFilterType), Is.EqualTo(new PngFilterType[]
        {
            PngFilterType.None,
            PngFilterType.Sub,
            PngFilterType.Up,
            PngFilterType.Average,
            PngFilterType.Paeth
        }));
        Assert.That(result.BestTrial.UseFixedFilter, Is.True);
        Assert.That(result.BestTrial.FilterType, Is.EqualTo(PngFilterType.Paeth));
    }

    [Test]
    public void TrialRunners_RejectNullInjectedEncoder()
    {
        PngImage image = new PngImage();
        PngOptimizerOptions options = new PngOptimizerOptions();

        Assert.Throws<ArgumentNullException>(() => PngCompressionLevelTrialRunner.Run(image, options, null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterStrategyTrialRunner.Run(image, options, null!));
    }

    class RecordingEncoder : IPngEncoder
    {
        public List<PngImage> Images = new List<PngImage>();
        public List<PngEncoderOptions> Options = new List<PngEncoderOptions>();

        public byte[] EncodeToBytes(PngImage image, PngEncoderOptions options)
        {
            Images.Add(image);
            Options.Add(options);

            int byteCount = 100 - options.CompressionLevel;
            if (options.UseFixedFilter)
            {
                byteCount -= ((int)options.FixedFilterType + 1) * 5;
            }

            return new byte[byteCount];
        }

        public void EncodeToStream(Stream stream, PngImage image, PngEncoderOptions options)
        {
            byte[] encoded = EncodeToBytes(image, options);
            stream.Write(encoded, 0, encoded.Length);
        }
    }
}
