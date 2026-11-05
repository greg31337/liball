
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerTests
{
    [Test]
    public void FeedReadAndFinalizationSuspendAndEmitEofExactlyOnce()
    {
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());

        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.NeedMoreInput);
        tokenizer.Feed("ab".AsSpan(), false);
        HtmlTokenReadResult first = tokenizer.Read();
        AssertToken(first, HtmlTokenKind.Character, "ab");
        Assert.That(first.GetToken().GetSourceSpan().GetNormalizedCharacterLength(), Is.EqualTo(2));
        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.NeedMoreInput);

        tokenizer.Feed("c".AsSpan(), true);
        AssertToken(tokenizer.Read(), HtmlTokenKind.Character, "c");
        HtmlTokenReadResult eof = tokenizer.Read();
        AssertToken(eof, HtmlTokenKind.EndOfFile, "");
        Assert.That(eof.GetToken().GetSourceSpan().GetNormalizedCharacterLength(), Is.EqualTo(0));
        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.Complete);
        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.Complete);
    }

    [Test]
    public void CoordinatorCompactsOwnedInputAfterTokenEmission()
    {
        HtmlTokenizerContext context = new HtmlTokenizerContext(
            HtmlTokenizerOptions.CreateDefault(), null, false);

        context.Feed("first".AsSpan(), false);
        AssertToken(context.Read(), HtmlTokenKind.Character, "first");
        Assert.That(context.GetBufferedInputLength(), Is.EqualTo(0));
        Assert.That(context.GetInputPosition(), Is.EqualTo(5));

        context.Feed("second".AsSpan(), true);
        AssertToken(context.Read(), HtmlTokenKind.Character, "second");
        Assert.That(context.GetBufferedInputLength(), Is.EqualTo(0));
        Assert.That(context.GetInputPosition(), Is.EqualTo(11));
    }

    [Test]
    public void CoordinatorSupportsLookaheadAndReconsumeBeforeSafeCompaction()
    {
        HtmlTokenizerContext context = new HtmlTokenizerContext(
            HtmlTokenizerOptions.CreateDefault(), null, false);
        context.Feed("abc".AsSpan(), false);

        Assert.That(context.PeekInput(), Is.EqualTo('a'));
        Assert.That(context.PeekInputAhead(2), Is.EqualTo('c'));
        Assert.That(context.ConsumeInput(), Is.EqualTo('a'));
        Assert.That(context.ConsumeInput(), Is.EqualTo('b'));
        context.ReconsumeInput();
        Assert.That(context.ConsumeInput(), Is.EqualTo('b'));
        context.CompactInput();

        Assert.That(context.GetBufferedInputLength(), Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(context.ReconsumeInput);
        Assert.That(context.ConsumeInput(), Is.EqualTo('c'));
    }

    [Test]
    public void ResetClearsFinalAndBufferedStateButRetainsSnapshottedOptions()
    {
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetCoalesceCharacterTokens(false);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        options.SetCoalesceCharacterTokens(true);

        tokenizer.Feed("ab".AsSpan(), true);
        AssertToken(tokenizer.Read(), HtmlTokenKind.Character, "a");
        tokenizer.Reset();
        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.NeedMoreInput);
        tokenizer.Feed("cd".AsSpan(), true);
        AssertToken(tokenizer.Read(), HtmlTokenKind.Character, "c");
        AssertToken(tokenizer.Read(), HtmlTokenKind.Character, "d");
        AssertToken(tokenizer.Read(), HtmlTokenKind.EndOfFile, "");
        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.Complete);
    }

    [Test]
    public void SuspensionDoesNotSplitAUnicodeScalarAcrossChunks()
    {
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetCoalesceCharacterTokens(false);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        char[] high = ['\uD83D'];
        char[] low = ['\uDE00'];

        tokenizer.Feed(high.AsSpan(), false);
        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.NeedMoreInput);
        tokenizer.Feed(low.AsSpan(), true);

        AssertToken(tokenizer.Read(), HtmlTokenKind.Character, "\U0001F600");
        AssertToken(tokenizer.Read(), HtmlTokenKind.EndOfFile, "");
    }

    [Test]
    public void CharacterCoalescingHonorsTheMaximumTokenLength()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(2);
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);

        tokenizer.Feed("abcde".AsSpan(), true);

        AssertToken(tokenizer.Read(), HtmlTokenKind.Character, "ab");
        AssertToken(tokenizer.Read(), HtmlTokenKind.Character, "cd");
        AssertToken(tokenizer.Read(), HtmlTokenKind.Character, "e");
        AssertToken(tokenizer.Read(), HtmlTokenKind.EndOfFile, "");
    }

    [Test]
    public void DisabledSourceTrackingReturnsUnknownTokenSpans()
    {
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetTrackSourceLocations(false);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);

        tokenizer.Feed("text".AsSpan(), true);

        Assert.That(tokenizer.Read().GetToken().GetSourceSpan().IsKnown(), Is.False);
        Assert.That(tokenizer.Read().GetToken().GetSourceSpan().IsKnown(), Is.False);
    }

    [Test]
    public void TokenLimitIncludesEofAndStopsDeterministically()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokens(1);
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);

        tokenizer.Feed("x".AsSpan(), true);

        HtmlTokenReadResult token = tokenizer.Read();
        HtmlTokenReadResult limited = tokenizer.Read();
        HtmlTokenReadResult repeated = tokenizer.Read();

        AssertToken(token, HtmlTokenKind.Character, "x");
        Assert.That(token.IsPartial(), Is.False);
        AssertStatus(limited, HtmlTokenReadStatus.LimitExceeded);
        Assert.That(limited.IsPartial(), Is.True);
        Assert.That(limited.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.ResourceLimitExceeded }));
        AssertStatus(repeated, HtmlTokenReadStatus.LimitExceeded);
        Assert.That(repeated.IsPartial(), Is.True);
        Assert.That(repeated.GetDiagnostics(), Is.Empty);
        Assert.Throws<InvalidOperationException>(() => tokenizer.Feed("y".AsSpan(), true));
    }

    [Test]
    public void InputLimitDrainsPreviouslyAcceptedInputBeforeReportingFailure()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(2);
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);

        tokenizer.Feed("ab".AsSpan(), false);
        tokenizer.Feed("c".AsSpan(), false);

        HtmlTokenReadResult token = tokenizer.Read();
        HtmlTokenReadResult limited = tokenizer.Read();

        AssertToken(token, HtmlTokenKind.Character, "ab");
        Assert.That(token.IsPartial(), Is.True);
        Assert.That(token.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.InputLimitExceeded }));
        AssertStatus(limited, HtmlTokenReadStatus.LimitExceeded);
        Assert.That(limited.IsPartial(), Is.True);
        Assert.Throws<InvalidOperationException>(() => tokenizer.Feed("d".AsSpan(), true));
    }

    [Test]
    public void LimitBeforeAnyTokenReportsFailureWithoutPartialOutput()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(1);
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        tokenizer.Feed("\U0001F600".AsSpan(), true);

        HtmlTokenReadResult result = tokenizer.Read();

        AssertStatus(result, HtmlTokenReadStatus.LimitExceeded);
        Assert.That(result.IsPartial(), Is.False);
        Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.ResourceLimitExceeded }));
    }

    [Test]
    public void DiagnosticTruncationIsReportedWithoutFailingTokenization()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumDiagnostics(2);
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        tokenizer.Feed("\0\0\0".AsSpan(), true);

        HtmlTokenReadResult text = tokenizer.Read();
        HtmlTokenReadResult eof = tokenizer.Read();
        HtmlTokenReadResult complete = tokenizer.Read();

        AssertToken(text, HtmlTokenKind.Character, "\uFFFD\uFFFD\uFFFD");
        Assert.That(text.GetDiagnostics(), Has.Length.EqualTo(2));
        Assert.That(text.WereDiagnosticsTruncated(), Is.True);
        Assert.That(text.GetDroppedDiagnosticCount(), Is.EqualTo(1));
        AssertToken(eof, HtmlTokenKind.EndOfFile, "");
        Assert.That(eof.WereDiagnosticsTruncated(), Is.True);
        Assert.That(eof.GetDroppedDiagnosticCount(), Is.EqualTo(1));
        AssertStatus(complete, HtmlTokenReadStatus.Complete);
        Assert.That(complete.WereDiagnosticsTruncated(), Is.True);
        Assert.That(complete.GetDroppedDiagnosticCount(), Is.EqualTo(1));
    }

    [Test]
    public void NormalizationDiagnosticsAreDeliveredOnceWithTheNextRead()
    {
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
        tokenizer.Feed("a\0".AsSpan(), true);

        HtmlTokenReadResult text = tokenizer.Read();
        AssertToken(text, HtmlTokenKind.Character, "a\uFFFD");
        Assert.That(text.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.InputNullCharacter }));
        Assert.That(tokenizer.Read().GetDiagnostics(), Is.Empty);
        Assert.That(tokenizer.Read().GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CancellationIsObservableAndResetRetainsTheConfiguredToken()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetCancellationToken(source.Token);
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(options);
        source.Cancel();

        Assert.DoesNotThrow(() => tokenizer.Feed("x".AsSpan(), false));
        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.Cancelled);
        Assert.DoesNotThrow(tokenizer.Reset);
        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.Cancelled);
    }

    [Test]
    public void FinalFeedAndConstructorEnforceLifecycleArguments()
    {
        Assert.Throws<ArgumentNullException>(() => HtmlTokenizer.Create(null));
        HtmlTokenizer tokenizer = HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
        tokenizer.Feed(ReadOnlySpan<char>.Empty, true);

        Assert.Throws<InvalidOperationException>(() => tokenizer.Feed("x".AsSpan(), false));
        AssertToken(tokenizer.Read(), HtmlTokenKind.EndOfFile, "");
        AssertStatus(tokenizer.Read(), HtmlTokenReadStatus.Complete);
    }

    static void AssertToken(HtmlTokenReadResult result, HtmlTokenKind kind, string data)
    {
        Assert.That(result.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.TokenAvailable));
        Assert.That(result.HasToken(), Is.True);
        Assert.That(result.GetToken().GetKind(), Is.EqualTo(kind));
        Assert.That(result.GetToken().GetData(), Is.EqualTo(data));
    }

    static void AssertStatus(HtmlTokenReadResult result, HtmlTokenReadStatus status)
    {
        Assert.That(result.GetStatus(), Is.EqualTo(status));
        Assert.That(result.HasToken(), Is.False);
    }
}
