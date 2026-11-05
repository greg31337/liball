using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class EncodeAttemptParityTests
{
    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.Micro, false)]
    [TestCase(QrSymbolKind.Micro, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void TextAndSegmentAttemptsMatchSuccessfulEncoding(QrSymbolKind kind, bool segments)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = Options(kind);
        QrSegment[] input = new QrSegmenter().CreateTextSegments("123", -1, false, 100);
        QrEncodeAttemptResult attempt = segments ? encoder.TryEncodeSegments(input, options) : encoder.TryEncodeText("123", options);
        QrSymbol symbol = segments ? encoder.EncodeSegments(input, options) : encoder.EncodeText("123", options);
        AssertSuccess(attempt, symbol, "123");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CapacityFailuresMatchExactlyAndDoNotRetainPreviousSuccess(bool segments)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = Options(QrSymbolKind.Model2);
        QrSegment[] input = [new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 18, Data = new byte[18] }];
        QrEncodeAttemptResult prior = encoder.TryEncodeText("123", options);
        QrModuleValue[] before = (QrModuleValue[])prior.Matrix!.Values.Clone();
        QrEncodeAttemptResult attempt = segments ? encoder.TryEncodeSegments(input, options) : encoder.TryEncodeText(new string('a', 18), options);
        QrCapacityException error = Assert.Throws<QrCapacityException>(() =>
        {
            if (segments) encoder.EncodeSegments(input, options);
            else encoder.EncodeText(new string('a', 18), options);
        })!;
        AssertEmpty(attempt, QrFailureReason.CapacityExceeded);
        Assert.That(error.FailureReason, Is.EqualTo(attempt.FailureReason));
        Assert.That(attempt.RequiredBitCount, Is.EqualTo(156).And.EqualTo(error.RequiredBitCount));
        Assert.That(attempt.AvailableBitCount, Is.EqualTo(152).And.EqualTo(error.AvailableBitCount));
        Assert.That(prior.Matrix.Values, Is.EqualTo(before));
        AssertSuccess(encoder.TryEncodeText("123", options), encoder.EncodeText("123", options), "123");
    }

    [TestCase("", false)]
    [TestCase("", true)]
    [TestCase("123", false)]
    [TestCase("123", true)]
    [TestCase("ABC", false)]
    [TestCase("ABC", true)]
    [TestCase("漢字", false)]
    [TestCase("漢字", true)]
    public void UnsupportedTranscoderDoesNotRejectNonByteText(string text, bool optimize)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = Options(QrSymbolKind.Model2);
        options.EciAssignment = 899;
        options.OptimizeSegments = optimize;
        AssertSuccess(encoder.TryEncodeText(text, options), encoder.EncodeText(text, options), text);
    }

    [TestCase(QrSymbolKind.Model2, false, "abc")]
    [TestCase(QrSymbolKind.Model2, true, "abc")]
    [TestCase(QrSymbolKind.Model2, false, "A漢")]
    [TestCase(QrSymbolKind.Micro, false, "😀")]
    [TestCase(QrSymbolKind.Micro, true, "😀")]
    public void RepresentationFailuresHaveMatchingReasons(QrSymbolKind kind, bool optimize, string text)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = Options(kind);
        options.OptimizeSegments = optimize;
        if (kind == QrSymbolKind.Model2) options.EciAssignment = 899;
        QrEncodeAttemptResult attempt = encoder.TryEncodeText(text, options);
        AssertEmpty(attempt, QrFailureReason.UnsupportedTextEncoding);
        QrUnsupportedFeatureException error = Assert.Throws<QrUnsupportedFeatureException>(() => encoder.EncodeText(text, options))!;
        Assert.That(error.FailureReason, Is.EqualTo(attempt.FailureReason));
        Assert.That(attempt.RequiredBitCount, Is.Zero);
        Assert.That(attempt.AvailableBitCount, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SingleModeHanziDoesNotRequireTheUnavailableByteTranscoder(bool attempt)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = Options(QrSymbolKind.Model2);
        options.OptimizeSegments = false;
        options.EnableHanziCompatibility = true;
        options.EciAssignment = 899;
        const string text = "汉";
        QrSymbol symbol = attempt ? encoder.TryEncodeText(text, options).Symbol! : encoder.EncodeText(text, options);
        Assert.That(symbol, Is.Not.Null);
        Assert.That(symbol.Segments.Single().Mode, Is.EqualTo(QrEncodingMode.Hanzi));
        Assert.That(new QrDecoder().DecodeMatrix(symbol.Matrix!, new QrDecodeOptions { EnableHanziCompatibility = true }).Text, Is.EqualTo(text));
    }

    [TestCase(QrSymbolKind.Model2, "a", 0xd800)]
    [TestCase(QrSymbolKind.Model2, "😀", 0xdc00)]
    [TestCase(QrSymbolKind.Micro, "😀", 0xd800)]
    [TestCase(QrSymbolKind.Micro, "€", 0xdc00)]
    public void RepresentationFailureCannotHideMalformedTrailingText(QrSymbolKind kind, string prefix, int trailing)
    {
        string text = prefix + (char)trailing;
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = Options(kind);
        if (kind == QrSymbolKind.Model2) options.EciAssignment = 899;
        QrInputException attemptError = Assert.Throws<QrInputException>(() => encoder.TryEncodeText(text, options))!;
        QrInputException error = Assert.Throws<QrInputException>(() => encoder.EncodeText(text, options))!;
        Assert.That(attemptError.InputName, Is.EqualTo("text").And.EqualTo(error.InputName));
        Assert.That(attemptError.FailureReason, Is.EqualTo(error.FailureReason));
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    public void RepresentationFailureCannotBypassConfiguredInputLimit(QrSymbolKind kind)
    {
        IQrEncoder encoder = new QrEncoder();
        QrEncodeOptions options = Options(kind);
        options.MaximumInputBytes = 1;
        if (kind == QrSymbolKind.Model2) options.EciAssignment = 899;
        string text = kind == QrSymbolKind.Model2 ? "abc" : "😀";
        QrResourceLimitException attempted = Assert.Throws<QrResourceLimitException>(() => encoder.TryEncodeText(text, options))!;
        QrResourceLimitException thrown = Assert.Throws<QrResourceLimitException>(() => encoder.EncodeText(text, options))!;
        Assert.That(attempted.LimitName, Is.EqualTo("MaximumInputBytes").And.EqualTo(thrown.LimitName));
        Assert.That(attempted.RequestedValue, Is.EqualTo(thrown.RequestedValue));
        Assert.That(attempted.MaximumValue, Is.EqualTo(1).And.EqualTo(thrown.MaximumValue));
    }

    static QrEncodeOptions Options(QrSymbolKind kind) => new QrEncodeOptions
    {
        ForceSymbolKind = true, SymbolKind = kind, MaximumVersion = kind == QrSymbolKind.Micro ? 4 : 1,
        ErrorCorrectionLevel = kind == QrSymbolKind.rMQR ? QrErrorCorrectionLevel.Medium : QrErrorCorrectionLevel.Low,
        ForceErrorCorrectionLevel = true, BoostErrorCorrectionLevel = false, QuietZoneModules = 0
    };

    static void AssertSuccess(QrEncodeAttemptResult attempt, QrSymbol symbol, string text)
    {
        Assert.That(attempt.Success, Is.True);
        Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(attempt.Symbol, Is.Not.Null);
        Assert.That(attempt.Symbol!.Codewords, Is.EqualTo(symbol.Codewords));
        Assert.That(attempt.Matrix, Is.SameAs(attempt.Symbol.Matrix));
        Assert.That(attempt.Matrix!.Values, Is.EqualTo(symbol.Matrix!.Values));
        Assert.That(attempt.Symbol.SymbolKind, Is.EqualTo(symbol.SymbolKind));
        Assert.That(attempt.Symbol.Version, Is.EqualTo(symbol.Version));
        Assert.That(attempt.RequiredBitCount, Is.EqualTo(symbol.UsedDataBitCount));
        Assert.That(attempt.AvailableBitCount, Is.EqualTo(symbol.DataBitCapacity));
        Assert.That(attempt.Sequence, Is.Null);
        Assert.That(attempt.SequenceMatrices, Is.Empty);
        Assert.That(new QrDecoder().DecodeMatrix(attempt.Matrix, new QrDecodeOptions()).Text, Is.EqualTo(text));
    }

    static void AssertEmpty(QrEncodeAttemptResult attempt, QrFailureReason reason)
    {
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.FailureReason, Is.EqualTo(reason));
        Assert.That(attempt.Symbol, Is.Null);
        Assert.That(attempt.Matrix, Is.Null);
        Assert.That(attempt.Sequence, Is.Null);
        Assert.That(attempt.SequenceMatrices, Is.Empty);
    }
}
