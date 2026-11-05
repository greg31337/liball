namespace uri.Tests;

public class PercentCodecTests
{
    [Test]
    public void DefaultOptionsSelectUnreservedEncodingAndStrictUtf8Decoding()
    {
        PercentCodecOptions options = new PercentCodecOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.EncodeSet, Is.EqualTo(PercentEncodeSet.Unreserved));
            Assert.That(options.DecodeMode, Is.EqualTo(PercentDecodeMode.StrictUtf8));
            Assert.That(options.Limits, Is.Not.Null);
        });
    }

    [Test]
    public void ValidatePercentEncodingReturnsNullOrTheExactFirstDiagnostic()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();

        UriDiagnostic? valid = codec.ValidatePercentEncoding("a%20b%2F", options);
        UriDiagnostic? invalid = codec.ValidatePercentEncoding("a%20b%G0", options);

        Assert.That(valid, Is.Null);
        AssertDiagnostic(
            invalid,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.Input,
            6,
            1,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [Test]
    public void ValidatePercentEncodingAppliesTheInputLimitBeforeScanning()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        options.Limits.MaxInputCharacters = 2;

        UriDiagnostic? diagnostic = codec.ValidatePercentEncoding("%GG", options);

        AssertDiagnostic(
            diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input,
            2,
            1,
            LimitGuard.InputLimitMessage);
    }

    [Test]
    public void EncodeTextUsesTheSelectedSetAndStrictUtf8()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        options.EncodeSet = PercentEncodeSet.Query;

        string result = codec.EncodeText("a/b? \u00e4", options);

        Assert.That(result, Is.EqualTo("a/b?%20%C3%A4"));
    }

    [Test]
    public void EncodeBytesPreservesArbitraryOctetsWithoutTextInterpretation()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();

        string result = codec.EncodeBytes(new byte[] { 0, 65, 128, 255 }, options);

        Assert.That(result, Is.EqualTo("%00A%80%FF"));
    }

    [Test]
    public void EncodeSpanMatchesTextEncoding()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        string text = "path/\u00e4";

        string fromText = codec.EncodeText(text, options);
        string fromSpan = codec.EncodeSpan(text.AsSpan(), options);

        Assert.That(fromSpan, Is.EqualTo(fromText));
    }

    [Test]
    public void EncodingFailureThrowsDiagnosticCarryingPercentException()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        string malformed = new string(new char[] { 'a', (char)55296 });

        PercentEncodingException? exception = Assert.Throws<PercentEncodingException>(
            () => codec.EncodeText(malformed, options));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Is.EqualTo(DiagnosticFactory.InvalidUnicodeMessage));
        AssertDiagnostic(
            exception.Diagnostic,
            UriDiagnosticCode.InvalidUnicode,
            UriComponent.Input,
            1,
            1,
            DiagnosticFactory.InvalidUnicodeMessage);
    }

    [Test]
    public void EncodeMethodsEnforceInputAndOutputLimits()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions inputOptions = new PercentCodecOptions();
        inputOptions.Limits.MaxInputCharacters = 1;
        PercentCodecOptions outputOptions = new PercentCodecOptions();
        outputOptions.Limits.MaxOutputCharacters = 2;

        PercentEncodingException? input = Assert.Throws<PercentEncodingException>(
            () => codec.EncodeBytes(new byte[] { 65, 66 }, inputOptions));
        PercentEncodingException? output = Assert.Throws<PercentEncodingException>(
            () => codec.EncodeSpan(" ".AsSpan(), outputOptions));

        Assert.That(input!.Diagnostic.Component, Is.EqualTo(UriComponent.Input));
        Assert.That(input.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
        Assert.That(output!.Diagnostic.Component, Is.EqualTo(UriComponent.Output));
        Assert.That(output.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
    }

    [Test]
    public void DecodeBytesReturnsOnlyTheByteOutcomeOnSuccess()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();

        PercentDecodeResult result = codec.DecodeBytes("A%00%80%FF", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 65, 0, 128, 255 }));
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.UsedReplacement, Is.False);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void DecodeBytesReturnsFailureWithoutPartialOutcomes()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();

        PercentDecodeResult result = codec.DecodeBytes("valid%20then%GG", options);

        AssertFailureResult(result, UriDiagnosticCode.InvalidPercentEncoding);
        Assert.That(result.Diagnostic!.Range.Offset, Is.EqualTo(13));
    }

    [Test]
    public void DecodeTextStrictModeRejectsInvalidUtf8()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();

        PercentDecodeResult result = codec.DecodeText("a%FF", options);

        AssertFailureResult(result, UriDiagnosticCode.InvalidUtf8);
        Assert.That(result.Diagnostic!.Range.Offset, Is.EqualTo(1));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(3));
    }

    [Test]
    public void DecodeTextReplacementModeReportsReplacementInSuccessfulResult()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        options.DecodeMode = PercentDecodeMode.ReplaceInvalidUtf8;

        PercentDecodeResult result = codec.DecodeText("a%FF%E2%82", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(result.Text, Is.EqualTo("a\uFFFD\uFFFD"));
            Assert.That(result.UsedReplacement, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void DecodeTextStrictModeReturnsLegalUnicodeText()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();

        PercentDecodeResult result = codec.DecodeText("A%C2%A2%E2%82%AC%F0%9F%98%80", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("A\u00a2\u20ac\ud83d\ude00"));
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(result.UsedReplacement, Is.False);
        });
    }

    [Test]
    public void DecodeMethodsReturnLimitFailuresWithoutPartialOutcomes()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions inputOptions = new PercentCodecOptions();
        inputOptions.Limits.MaxInputCharacters = 2;
        PercentCodecOptions outputOptions = new PercentCodecOptions();
        outputOptions.Limits.MaxOutputCharacters = 1;

        PercentDecodeResult input = codec.DecodeText("%41", inputOptions);
        PercentDecodeResult output = codec.DecodeBytes("AB", outputOptions);

        AssertFailureResult(input, UriDiagnosticCode.LimitExceeded);
        Assert.That(input.Diagnostic!.Component, Is.EqualTo(UriComponent.Input));
        AssertFailureResult(output, UriDiagnosticCode.LimitExceeded);
        Assert.That(output.Diagnostic!.Component, Is.EqualTo(UriComponent.Output));
    }

    [Test]
    public void NormalizePercentEncodingCombinesHexCaseAndUnreservedNormalization()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();

        string result = codec.NormalizePercentEncoding(
            "a%2fb%7ec%41%25%ff%252F",
            options);

        Assert.That(result, Is.EqualTo("a%2Fb~cA%25%FF%252F"));
    }

    [Test]
    public void NormalizePercentEncodingUsesFinalLengthForOutputLimit()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        options.Limits.MaxOutputCharacters = 1;

        string result = codec.NormalizePercentEncoding("%41", options);

        Assert.That(result, Is.EqualTo("A"));
    }

    [Test]
    public void NormalizeFailureThrowsTheSameDiagnosticContextAsValidation()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        UriDiagnostic? validation = codec.ValidatePercentEncoding("a%G0", options);

        PercentEncodingException? exception = Assert.Throws<PercentEncodingException>(
            () => codec.NormalizePercentEncoding("a%G0", options));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Diagnostic.Code, Is.EqualTo(validation!.Code));
        Assert.That(exception.Diagnostic.Component, Is.EqualTo(validation.Component));
        Assert.That(exception.Diagnostic.Range.Offset, Is.EqualTo(validation.Range.Offset));
        Assert.That(exception.Diagnostic.Range.Length, Is.EqualTo(validation.Range.Length));
        Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
    }

    [Test]
    public void InvalidOptionValuesFailDeterministically()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions encodeOptions = new PercentCodecOptions();
        encodeOptions.EncodeSet = (PercentEncodeSet)999;
        PercentCodecOptions decodeOptions = new PercentCodecOptions();
        decodeOptions.DecodeMode = (PercentDecodeMode)999;

        PercentEncodingException? encode = Assert.Throws<PercentEncodingException>(
            () => codec.EncodeText("a", encodeOptions));
        PercentDecodeResult decode = codec.DecodeText("a", decodeOptions);

        Assert.That(encode!.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        AssertFailureResult(decode, UriDiagnosticCode.InvalidInput);
    }

    [Test]
    public void NullReferenceInputsAndOptionsProduceDomainFailures()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        PercentCodecOptions missingLimits = new PercentCodecOptions();
        missingLimits.Limits = null!;

        UriDiagnostic? validation = codec.ValidatePercentEncoding(null!, options);
        PercentDecodeResult byteResult = codec.DecodeBytes(null!, options);
        PercentDecodeResult textResult = codec.DecodeText("a", missingLimits);
        PercentEncodingException? encode = Assert.Throws<PercentEncodingException>(
            () => codec.EncodeBytes(null!, options));
        PercentEncodingException? normalize = Assert.Throws<PercentEncodingException>(
            () => codec.NormalizePercentEncoding("a", null!));

        Assert.That(validation!.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        AssertFailureResult(byteResult, UriDiagnosticCode.InvalidInput);
        AssertFailureResult(textResult, UriDiagnosticCode.InvalidInput);
        Assert.That(encode!.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        Assert.That(normalize!.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
    }

    [Test]
    public void EmptyInputsProduceEmptySuccessfulOutcomes()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();

        Assert.That(codec.ValidatePercentEncoding("", options), Is.Null);
        Assert.That(codec.EncodeText("", options), Is.Empty);
        Assert.That(codec.EncodeBytes(new byte[0], options), Is.Empty);
        Assert.That(codec.EncodeSpan("".AsSpan(), options), Is.Empty);
        Assert.That(codec.DecodeBytes("", options).Success, Is.True);
        Assert.That(codec.DecodeBytes("", options).Bytes, Is.Empty);
        Assert.That(codec.DecodeText("", options).Success, Is.True);
        Assert.That(codec.DecodeText("", options).Text, Is.Empty);
        Assert.That(codec.NormalizePercentEncoding("", options), Is.Empty);
    }

    private static void AssertFailureResult(PercentDecodeResult result, UriDiagnosticCode code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Empty);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.UsedReplacement, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.PercentEncoding));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
    }

    private static void AssertDiagnostic(
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.PercentEncoding));
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostic.Component, Is.EqualTo(component));
        Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(diagnostic.Message, Is.EqualTo(message));
    }
}
