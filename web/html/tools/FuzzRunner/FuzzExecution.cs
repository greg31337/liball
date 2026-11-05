using System.Diagnostics;
using html;

namespace UniversalHtml.FuzzRunner;

internal sealed class FuzzEvaluation
{
    public FuzzEvaluation(bool failedValue, string failureKindValue,
        string exceptionTypeValue, string messageValue,
        TimeSpan elapsedValue)
    {
        Failed = failedValue;
        FailureKind = failureKindValue;
        ExceptionType = exceptionTypeValue;
        Message = messageValue;
        Elapsed = elapsedValue;
    }

    public bool Failed { get; }
    public string FailureKind { get; }
    public string ExceptionType { get; }
    public string Message { get; }
    public TimeSpan Elapsed { get; }

    public static FuzzEvaluation Success(TimeSpan elapsed)
    {
        return new FuzzEvaluation(false, "", "", "", elapsed);
    }
}

internal sealed class FuzzExecutor
{
    readonly int caseTimeoutMilliseconds;
    readonly int maximumOutputCharacters;
    readonly HtmlCompiledSanitizerPolicy cssPolicy;
    readonly HtmlCompiledSanitizerPolicy sanitizerPolicy;
    readonly HtmlDocument selectorDocument;

    public FuzzExecutor(int caseTimeoutMillisecondsValue,
        int maximumInputLength)
    {
        caseTimeoutMilliseconds = caseTimeoutMillisecondsValue;
        maximumOutputCharacters = checked(Math.Max(4096,
            maximumInputLength * 16 + 1024));
        HtmlSanitizerPolicy policy = HtmlSanitizerPolicy.CreateRichText();
        policy.AllowCssProperty("background-image");
        policy.AllowCssProperty("color");
        policy.AllowCssProperty("display");
        policy.AllowCssProperty("font-size");
        policy.AllowCssProperty("margin");
        policy.AllowCssProperty("opacity");
        policy.AllowCssProperty("width");
        cssPolicy = policy.Compile();
        sanitizerPolicy = HtmlSanitizerPolicy.CreateRichText().Compile();
        selectorDocument = Html.Parse(
            "<!doctype html><main id=root><article class='item active'>"
            + "<a href='https://example.test/a' lang=en-US>link</a>"
            + "<ol><li data-x='a b'>one</li><li>two</li></ol>"
            + "<svg><circle id=shape></circle></svg></article></main>");
    }

    public FuzzEvaluation Evaluate(FuzzTarget target, string input)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        using CancellationTokenSource cancellation =
            new CancellationTokenSource(caseTimeoutMilliseconds);
        try
        {
            switch (target)
            {
                case FuzzTarget.Html:
                    EvaluateHtml(input, cancellation.Token);
                    break;
                case FuzzTarget.Selector:
                    EvaluateSelector(input, cancellation.Token);
                    break;
                case FuzzTarget.Css:
                    EvaluateCss(input);
                    break;
                case FuzzTarget.Url:
                    EvaluateUrl(input, cancellation.Token);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target));
            }

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > caseTimeoutMilliseconds)
            {
                return new FuzzEvaluation(true, "timeout", "",
                    "The case exceeded its wall-time budget.",
                    stopwatch.Elapsed);
            }

            return FuzzEvaluation.Success(stopwatch.Elapsed);
        }
        catch (OperationCanceledException exception)
            when (cancellation.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new FuzzEvaluation(true, "timeout",
                exception.GetType().FullName,
                "The case exceeded its cancellation deadline.",
                stopwatch.Elapsed);
        }
        catch (FuzzInvariantException exception)
        {
            stopwatch.Stop();
            return new FuzzEvaluation(true,
                "invariant:" + exception.Code,
                exception.GetType().FullName, exception.Message,
                stopwatch.Elapsed);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new FuzzEvaluation(true,
                "exception:" + exception.GetType().FullName,
                exception.GetType().FullName, exception.Message,
                stopwatch.Elapsed);
        }
    }

    void EvaluateHtml(string input, CancellationToken cancellationToken)
    {
        HtmlParseResult result = Html.Parse(input, CreateParseOptions(),
            cancellationToken);
        result.GetDiagnostics();
        FuzzInvariants.Require(result.IsComplete() && !result.WasTruncated()
            && result.GetDocument() != null, "html-parse-incomplete",
            "Bounded generated HTML did not produce a complete document.");
        HtmlDocument document = result.GetDocument();
        FuzzInvariants.AssertTree(document, document.GetRoot(),
            maximumOutputCharacters);

        HtmlSerializerOptions serializerOptions = CreateSerializerOptions();
        string first = HtmlSerializer.Serialize(document.GetRoot(),
            serializerOptions, cancellationToken);
        string second = HtmlSerializer.Serialize(document.GetRoot(),
            serializerOptions, cancellationToken);
        FuzzInvariants.Require(first == second,
            "serialization-nondeterministic",
            "Serializing the same tree twice produced different output.");
        FuzzInvariants.Require(first.Length <= maximumOutputCharacters,
            "serialization-output-bound",
            "Serialized HTML exceeded the configured output bound.");

        HtmlParseResult repeated = Html.Parse(input, CreateParseOptions(),
            cancellationToken);
        FuzzInvariants.Require(repeated.IsComplete()
            && repeated.GetDocument() != null,
            "html-repeat-incomplete",
            "Repeating the same parse did not complete.");
        string repeatedOutput = HtmlSerializer.Serialize(
            repeated.GetDocument().GetRoot(), serializerOptions,
            cancellationToken);
        FuzzInvariants.Require(first == repeatedOutput,
            "parse-serialization-nondeterministic",
            "The same HTML input produced different canonical trees.");

        AssertSanitizerInvariants(input, cancellationToken);
    }

    void EvaluateSelector(string input,
        CancellationToken cancellationToken)
    {
        HtmlSelectorCompileResult compilation = HtmlSelectors.Compile(input);
        compilation.GetDiagnostics();
        HtmlSelectorCompileResult repeated = HtmlSelectors.Compile(input);
        FuzzInvariants.Require(compilation.IsSuccess() == repeated.IsSuccess(),
            "selector-nondeterministic",
            "Compiling the same selector changed its success result.");
        if (compilation.IsSuccess())
        {
            HtmlNode[] first = compilation.GetSelector().FindAllWithCancellation(
                selectorDocument.GetRoot(), cancellationToken);
            HtmlNode[] second = repeated.GetSelector().FindAllWithCancellation(
                selectorDocument.GetRoot(), cancellationToken);
            FuzzInvariants.Require(first.Length == second.Length
                && first.SequenceEqual(second,
                    ReferenceEqualityComparer.Instance),
                "selector-results-nondeterministic",
                "The same selector produced different result order.");
            FuzzInvariants.Require(first.Length <= maximumOutputCharacters,
                "selector-results-bound",
                "Selector results exceeded the configured bound.");
            compilation.GetSelector().FindAllWithCancellation(
                selectorDocument.GetRoot(), cancellationToken);
        }
    }

    void EvaluateCss(string input)
    {
        HtmlSanitizerStyleResult result = HtmlSanitizerStyles.Sanitize(input,
            cssPolicy);
        result.GetChanges();
        string value = result.GetValue();
        HtmlSanitizerStyleResult repeated =
            HtmlSanitizerStyles.Sanitize(input, cssPolicy);
        FuzzInvariants.Require(value == repeated.GetValue()
            && result.IsSuccess() == repeated.IsSuccess()
            && result.WasTruncated() == repeated.WasTruncated(),
            "css-nondeterministic",
            "Sanitizing the same CSS declarations changed the result.");
        FuzzInvariants.Require(value.Length <= maximumOutputCharacters,
            "css-output-bound",
            "Sanitized CSS exceeded the configured output bound.");
    }

    void EvaluateUrl(string input, CancellationToken cancellationToken)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlUrlSchemeResult scheme = HtmlUrls.ExtractScheme(input, limits,
            cancellationToken);
        scheme.GetDeobfuscatedValue();
        HtmlUrlResult result = HtmlUrls.Resolve(input,
            "https://example.test/base/index.html", limits,
            cancellationToken);
        result.GetUrl();
        result.GetDiagnostics();
        HtmlUrlResult repeated = HtmlUrls.Resolve(input,
            "https://example.test/base/index.html", limits,
            cancellationToken);
        FuzzInvariants.Require(result.IsSuccess() == repeated.IsSuccess()
            && result.GetUrl() == repeated.GetUrl()
            && result.GetScheme() == repeated.GetScheme(),
            "url-nondeterministic",
            "Resolving the same URL changed the result.");
        FuzzInvariants.Require(result.GetUrl().Length
            <= maximumOutputCharacters, "url-output-bound",
            "Resolved URL output exceeded the configured bound.");
    }

    void AssertSanitizerInvariants(string input,
        CancellationToken cancellationToken)
    {
        HtmlSanitizeResult first = HtmlSanitizer.Sanitize(input,
            sanitizerPolicy, CreateParseOptions(), cancellationToken);
        FuzzInvariants.Require(first.IsSuccess() && !first.WasTruncated(),
            "sanitizer-incomplete",
            "Bounded input did not produce a complete sanitizer result.");
        string output = first.GetHtml();
        FuzzInvariants.Require(output.Length <= maximumOutputCharacters,
            "sanitizer-output-bound",
            "Sanitized HTML exceeded the configured output bound.");

        HtmlSanitizeResult second = HtmlSanitizer.Sanitize(output,
            sanitizerPolicy, CreateParseOptions(), cancellationToken);
        FuzzInvariants.Require(second.IsSuccess()
            && !second.WasTruncated() && second.GetHtml() == output,
            "sanitizer-not-idempotent",
            "Sanitizing canonical output changed it.");

        HtmlParseResult contextParse = Html.Parse("", CreateParseOptions(),
            cancellationToken);
        FuzzInvariants.Require(contextParse.IsSuccess()
            && contextParse.IsComplete(), "context-parse-incomplete",
            "The empty context document did not parse completely.");
        HtmlDocument contextDocument = contextParse.GetDocument();
        HtmlNode context = contextDocument.CreateElement("body");
        HtmlParseResult reparsed = Html.ParseFragment(output,
            new HtmlFragmentContext(context), CreateParseOptions(),
            cancellationToken);
        FuzzInvariants.Require(reparsed.IsSuccess() && reparsed.IsComplete()
            && !reparsed.WasTruncated() && reparsed.GetFragment() != null,
            "sanitizer-reparse-incomplete",
            "Sanitized output did not reparse completely.");
        FuzzInvariants.AssertTree(reparsed.GetDocument(),
            reparsed.GetFragment(), maximumOutputCharacters);
        FuzzInvariants.AssertSafeSanitizedTree(reparsed.GetFragment(),
            sanitizerPolicy);
        string reparsedOutput = HtmlSerializer.Serialize(
            reparsed.GetFragment(), CreateSanitizerSerializerOptions(),
            cancellationToken);
        FuzzInvariants.Require(reparsedOutput == output,
            "sanitizer-reparse-unstable",
            "Sanitized output changed after safe reparsing.");
    }

    HtmlParseOptions CreateParseOptions()
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetLimits(CreateLimits());
        return options;
    }

    HtmlSerializerOptions CreateSerializerOptions()
    {
        HtmlSerializerOptions options =
            HtmlSerializerOptions.CreateCanonical();
        options.SetSortAttributes(true);
        options.SetLimits(CreateLimits());
        return options;
    }

    HtmlSerializerOptions CreateSanitizerSerializerOptions()
    {
        HtmlSerializerOptions options =
            HtmlSerializerOptions.CreateCanonical();
        options.SetLimits(CreateLimits());
        return options;
    }

    HtmlLimits CreateLimits()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(maximumOutputCharacters);
        return limits;
    }
}
