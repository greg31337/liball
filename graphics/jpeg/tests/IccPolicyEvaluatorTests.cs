namespace jpeg.Tests;

internal class IccPolicyEvaluatorTests
{
    [Test]
    public void Policy_IgnoreSkipsMalformedProfileWithoutPreserving()
    {
        byte[] profile = new byte[] { 1, 2, 3 };
        global::jpeg.JpegIccPolicyEvaluator evaluator = new global::jpeg.JpegIccPolicyEvaluator();

        global::jpeg.JpegIccPolicyResult result = evaluator.Apply(
            profile,
            global::jpeg.JpegIccBehaviorPolicy.Ignore);

        Assert.That(result.HasProfile, Is.True);
        Assert.That(result.Ignored, Is.True);
        Assert.That(result.Preserved, Is.False);
        Assert.That(result.ConversionAttempted, Is.False);
        Assert.That(result.PreservedProfile.Length, Is.EqualTo(0));
    }

    [Test]
    public void Policy_PreserveCopiesProfileWithoutParsing()
    {
        byte[] profile = new byte[] { 1, 2, 3 };
        global::jpeg.JpegIccPolicyEvaluator evaluator = new global::jpeg.JpegIccPolicyEvaluator();

        global::jpeg.JpegIccPolicyResult result = evaluator.Apply(
            profile,
            global::jpeg.JpegIccBehaviorPolicy.Preserve);
        profile[0] = 9;

        Assert.That(result.HasProfile, Is.True);
        Assert.That(result.Preserved, Is.True);
        Assert.That(result.ConversionAttempted, Is.False);
        Assert.That(result.PreservedProfile, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Policy_ConvertWhenSupportedReturnsMatrixTrcProfile()
    {
        byte[] profile = IccMatrixTrcConverterTests.CreateRgbMatrixTrcProfile();
        global::jpeg.JpegIccPolicyEvaluator evaluator = new global::jpeg.JpegIccPolicyEvaluator();

        global::jpeg.JpegIccPolicyResult result = evaluator.Apply(
            profile,
            global::jpeg.JpegIccBehaviorPolicy.ConvertWhenSupported);

        Assert.That(result.HasProfile, Is.True);
        Assert.That(result.ConversionAttempted, Is.True);
        Assert.That(result.ConversionSupported, Is.True);
        Assert.That(result.Preserved, Is.False);
        Assert.That(result.Icc.ColorSpace, Is.EqualTo("RGB "));
        Assert.That(result.MatrixTrcProfile.IsRgb, Is.True);
    }

    [Test]
    public void Policy_ConvertWhenSupportedPreservesUnsupportedProfile()
    {
        byte[] profile = IccProfileParserTests.CreateIccProfileWithOneTag();
        global::jpeg.JpegIccPolicyEvaluator evaluator = new global::jpeg.JpegIccPolicyEvaluator();

        global::jpeg.JpegIccPolicyResult result = evaluator.Apply(
            profile,
            global::jpeg.JpegIccBehaviorPolicy.ConvertWhenSupported);

        Assert.That(result.HasProfile, Is.True);
        Assert.That(result.ConversionAttempted, Is.True);
        Assert.That(result.ConversionSupported, Is.False);
        Assert.That(result.Unsupported, Is.True);
        Assert.That(result.Preserved, Is.True);
        Assert.That(result.PreservedProfile, Is.EqualTo(profile));
        Assert.That(result.Message, Is.Not.Empty);
    }

    [Test]
    public void Policy_FailIfUnsupportedThrowsForUnsupportedProfile()
    {
        byte[] profile = IccProfileParserTests.CreateIccProfileWithOneTag();
        global::jpeg.JpegIccPolicyEvaluator evaluator = new global::jpeg.JpegIccPolicyEvaluator();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                evaluator.Apply(profile, global::jpeg.JpegIccBehaviorPolicy.FailIfUnsupported);
            });
    }

    [Test]
    public void Converter_CanConvertRecognizesSupportedAndUnsupportedProfiles()
    {
        global::jpeg.JpegIccMatrixTrcConverter converter = new global::jpeg.JpegIccMatrixTrcConverter();

        Assert.That(converter.CanConvert(IccMatrixTrcConverterTests.CreateRgbMatrixTrcProfile()), Is.True);
        Assert.That(converter.CanConvert(IccMatrixTrcConverterTests.CreateGrayMatrixTrcProfile()), Is.True);
        Assert.That(converter.CanConvert(IccMatrixTrcConverterTests.CreateCmykMatrixTrcProfile()), Is.True);
        Assert.That(converter.CanConvert(IccProfileParserTests.CreateIccProfileWithOneTag()), Is.False);
    }
}
