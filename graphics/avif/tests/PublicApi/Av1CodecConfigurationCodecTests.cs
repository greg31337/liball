namespace avif.Tests;

public class Av1CodecConfigurationCodecTests
{
    [Test]
    public void ConfigurationRecordParsingAndGenerationRoundTripAllHeaderFields()
    {
        byte[] source = [129, 77, 238, 21];
        Av1CodecConfigurationCodec codec = new Av1CodecConfigurationCodec();
        Av1CodecConfiguration parsed = codec.ParseConfigurationRecord(source);
        AvifEncodeResult generated = codec.GenerateConfigurationRecord(parsed);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Profile, Is.EqualTo(2));
            Assert.That(parsed.Level, Is.EqualTo(13));
            Assert.That(parsed.Tier, Is.EqualTo(1));
            Assert.That(parsed.BitDepth, Is.EqualTo(12));
            Assert.That(parsed.Monochrome, Is.False);
            Assert.That(parsed.ChromaSubsamplingX, Is.True);
            Assert.That(parsed.ChromaSubsamplingY, Is.True);
            Assert.That(parsed.ChromaSamplePosition,
                Is.EqualTo(AvifChromaSamplePosition.Colocated));
            Assert.That(parsed.InitialPresentationDelayPresent, Is.True);
            Assert.That(parsed.InitialPresentationDelayMinusOne, Is.EqualTo(5));
            Assert.That(parsed.ConfigObus, Is.Empty);
            Assert.That(generated.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(generated.GetData(), Is.EqualTo(source));
            Assert.That(codec.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void CodecStringsParseAndGenerateAbbreviatedAndExpandedForms()
    {
        Av1CodecConfigurationCodec codec = new Av1CodecConfigurationCodec();
        string expanded = "av01.0.04M.10.0.112.09.16.09.0";
        Av1CodecParameters parsedExpanded = codec.ParseCodecString(expanded);
        string regeneratedExpanded = codec.GenerateCodecString(parsedExpanded);
        Av1CodecParameters abbreviated = codec.ParseCodecString(
            "av01.0.01M.08");
        string regeneratedAbbreviated = codec.GenerateCodecString(abbreviated);

        Assert.Multiple(() =>
        {
            Assert.That(parsedExpanded.Code,
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsedExpanded.SampleEntry, Is.EqualTo("av01"));
            Assert.That(parsedExpanded.Profile, Is.Zero);
            Assert.That(parsedExpanded.Level, Is.EqualTo(4));
            Assert.That(parsedExpanded.Tier, Is.Zero);
            Assert.That(parsedExpanded.BitDepth, Is.EqualTo(10));
            Assert.That(parsedExpanded.Monochrome, Is.False);
            Assert.That(parsedExpanded.ChromaSubsamplingX, Is.True);
            Assert.That(parsedExpanded.ChromaSubsamplingY, Is.True);
            Assert.That(parsedExpanded.ChromaSamplePosition,
                Is.EqualTo(AvifChromaSamplePosition.Colocated));
            Assert.That(parsedExpanded.ColorPrimaries, Is.EqualTo(9));
            Assert.That(parsedExpanded.TransferCharacteristics, Is.EqualTo(16));
            Assert.That(parsedExpanded.MatrixCoefficients, Is.EqualTo(9));
            Assert.That(parsedExpanded.Range, Is.EqualTo(AvifRange.Limited));
            Assert.That(parsedExpanded.HasOptionalFields, Is.True);
            Assert.That(regeneratedExpanded, Is.EqualTo(expanded));
            Assert.That(abbreviated.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(abbreviated.HasOptionalFields, Is.False);
            Assert.That(abbreviated.ChromaSubsamplingX, Is.True);
            Assert.That(abbreviated.ChromaSubsamplingY, Is.True);
            Assert.That(abbreviated.ColorPrimaries, Is.EqualTo(1));
            Assert.That(regeneratedAbbreviated,
                Is.EqualTo("av01.0.01M.08"));
        });
    }

    [Test]
    public void SequenceConfigurationAndCodecStringReconcileWithoutPrivateModels()
    {
        AvifImage image = CreateImage();
        Av1Encoder encoder = new Av1Encoder();
        Assert.That(encoder.AddFrame(image, 1),
            Is.EqualTo(AvifResultCode.Success));
        byte[] obuStream = encoder.Finish().GetData();
        Av1CodecConfigurationCodec codec = new Av1CodecConfigurationCodec();
        Av1CodecParameters sequence =
            codec.ParseSequenceHeaderParameters(obuStream);
        Av1CodecConfiguration configuration = CreateConfiguration(sequence);
        AvifEncodeResult configurationBytes =
            codec.GenerateConfigurationRecord(configuration);
        string codecString = codec.GenerateCodecString(sequence);
        Av1CodecParameters reconciled =
            codec.ReconcileConfigurationAndSequenceHeader(
                configuration,
                obuStream);
        Av1CodecParameters allSources = codec.ReconcileCodecSources(
            codecString,
            configurationBytes.GetData(),
            obuStream);

        Av1CodecConfiguration wrongConfiguration = CreateConfiguration(sequence);
        wrongConfiguration.Level = sequence.Level == 4 ? 5 : 4;
        Av1CodecParameters configurationMismatch =
            codec.ReconcileConfigurationAndSequenceHeader(
                wrongConfiguration,
                obuStream);
        Av1CodecParameters wrongStringParameters =
            codec.ParseCodecString(codecString);
        wrongStringParameters.Level = sequence.Level == 4 ? 5 : 4;
        string wrongString = codec.GenerateCodecString(wrongStringParameters);
        Av1CodecParameters stringMismatch = codec.ReconcileCodecSources(
            wrongString,
            configurationBytes.GetData(),
            obuStream);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(configurationBytes.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(codecString, Does.StartWith("av01."));
            Assert.That(reconciled.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reconciled.Profile, Is.EqualTo(sequence.Profile));
            Assert.That(reconciled.Level, Is.EqualTo(sequence.Level));
            Assert.That(reconciled.BitDepth, Is.EqualTo(sequence.BitDepth));
            Assert.That(reconciled.Range, Is.EqualTo(sequence.Range));
            Assert.That(allSources.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(configurationMismatch.Code,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(stringMismatch.Code,
                Is.EqualTo(AvifResultCode.NonConformingData));
        });

        encoder.Close();
        image.Close();
    }

    [Test]
    public void MalformedAndSemanticallyInvalidSignalingReturnsStableCodes()
    {
        Av1CodecConfigurationCodec codec = new Av1CodecConfigurationCodec();
        Av1CodecConfiguration badMarker =
            codec.ParseConfigurationRecord(new byte[] { 1, 0, 12, 0 });
        Av1CodecParameters partialOptional = codec.ParseCodecString(
            "av01.0.04M.10.0");
        Av1CodecParameters nonBinaryFlag = codec.ParseCodecString(
            "av01.0.04M.10.2.112.09.16.09.0");
        Av1CodecParameters badTier = codec.ParseCodecString(
            "av01.0.04X.10");
        Av1CodecConfiguration impossibleProfile = new Av1CodecConfiguration();
        impossibleProfile.Profile = 1;
        AvifEncodeResult impossible =
            codec.GenerateConfigurationRecord(impossibleProfile);
        Av1CodecParameters missingSequence =
            codec.ParseSequenceHeaderParameters(new byte[] { 18, 0 });

        Assert.Multiple(() =>
        {
            Assert.That(badMarker.Code,
                Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(partialOptional.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(nonBinaryFlag.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(badTier.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(impossible.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(missingSequence.Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
        });
    }

    static Av1CodecConfiguration CreateConfiguration(
        Av1CodecParameters parameters)
    {
        Av1CodecConfiguration result = new Av1CodecConfiguration();
        result.Profile = parameters.Profile;
        result.Level = parameters.Level;
        result.Tier = parameters.Tier;
        result.BitDepth = parameters.BitDepth;
        result.Monochrome = parameters.Monochrome;
        result.ChromaSubsamplingX = parameters.ChromaSubsamplingX;
        result.ChromaSubsamplingY = parameters.ChromaSubsamplingY;
        result.ChromaSamplePosition = parameters.ChromaSamplePosition;
        return result;
    }

    static AvifImage CreateImage()
    {
        AvifImage image = AvifImage.Create(
            3,
            2,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane plane = image.GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Invalid internal state.");
        int row = 0;
        while (row < plane.GetHeight())
        {
            plane.GetRowSpan(row).Fill(79);
            row++;
        }
        return image;
    }
}
