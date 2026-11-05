using System;
using System.IO;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Incrementally parses HTML tokens into a browser-compatible document or fragment tree.</summary>
public class HtmlParser : IHtmlParser
{
    internal const int ReaderBufferCharacters = 4096;
    internal static readonly HtmlParseOptions constructorOptions = HtmlParseOptions.CreateDefault();
    internal static readonly HtmlDocument constructorDocument = new HtmlDocument("", "", HtmlDocumentMode.NoQuirks, Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    internal static readonly IHtmlTokenizerControl constructorTokenizer = new HtmlTokenizerControl(HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault()));
    internal static readonly HtmlParseDiagnosticBuffer constructorDiagnostics = new HtmlParseDiagnosticBuffer(0);
    internal HtmlParseOptions options = constructorOptions;
    internal HtmlDocument document = constructorDocument;
    internal HtmlNode? fragment;
    internal IHtmlTokenizerControl tokenizer = constructorTokenizer;
    internal HtmlTreeBuilder? treeBuilder;
    internal HtmlParseDiagnosticBuffer diagnostics = constructorDiagnostics;
    internal bool finalInputFed;
    internal bool tokenizerComplete;
    internal bool limitExceeded;
    internal bool terminalLimit;
    internal bool failed;
    internal bool diagnosticsTruncated;
    internal bool treeDiagnosticsCollected;
    internal long externalDroppedDiagnosticCount;
    internal long tokenizerDroppedDiagnosticCount;
    internal int fedCharacterCount;
    internal int processedTokenCount;
    internal Encoding? detectedEncoding;
    internal string detectedEncodingLabel = "";
    internal HtmlEncodingDetectionResult? encodingDetection;
    internal CancellationToken cancellationToken;
    internal HtmlNode? fragmentTarget;
    internal TextReader? reader;
    internal bool readerLeaveOpen;
    internal bool readerEnded;
    internal bool readerFailed;
    internal bool cancelled;
    internal bool terminalDiagnosticReported;
    internal int readerCharacterCount;
    internal char[] readerBuffer = new char[ReaderBufferCharacters];
    /// <summary>Copies the supplied settings and creates an incremental document parser.</summary>
    /// <param name="optionsValue">The settings copied for use by this parser.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>optionsValue</c> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the supplied options contain an invalid setting.</exception>
    public HtmlParser(IHtmlParseConfiguration optionsValue)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        this.Initialize(optionsValue.Compile().CreateOptions(), null,
            CancellationToken.None, null, false);
    }

    /// <summary>Copies the parse settings and creates an incremental parser ready to accept character chunks through <see cref = "Feed"/>.</summary>
    /// <param name = "options">The settings copied for use by this operation.</param>
    /// <returns>A parser ready to accept character input.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when <c>options</c> is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the supplied options contain an invalid setting.</exception>
    internal static HtmlParser Create(HtmlParseOptions options)
    {
        return new HtmlParser(options);
    }

    internal static HtmlParser Create(HtmlParseOptions options, CancellationToken cancellationToken)
    {
        HtmlParser parser = new HtmlParser(options);
        parser.Initialize(options, null, cancellationToken, null, false);
        return parser;
    }

    internal static HtmlParser Create(HtmlParseOptions options, CancellationToken cancellationToken, HtmlBudget budget)
    {
        HtmlParser parser = new HtmlParser(options);
        parser.Initialize(options, null, cancellationToken, budget, false);
        return parser;
    }

    internal static HtmlParser Create(HtmlParseOptions options, CancellationToken cancellationToken, HtmlBudget budget, bool inputAlreadyCounted)
    {
        HtmlParser parser = new HtmlParser(options);
        parser.Initialize(options, null, cancellationToken, budget,
            inputAlreadyCounted);
        return parser;
    }

    internal static HtmlParser Create(HtmlParseOptions options, IHtmlTokenizerControl tokenizer)
    {
        if (tokenizer == null)
        {
            throw new ArgumentNullException("tokenizer");
        }

        HtmlParser parser = new HtmlParser(options);
        parser.tokenizer = tokenizer;
        return parser;
    }

    internal void Initialize(HtmlParseOptions optionsValue, HtmlNode? fragmentTarget, CancellationToken cancellationToken, HtmlBudget? budgetValue, bool inputAlreadyCounted)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("options");
        }

        optionsValue.Validate();
        this.options = optionsValue.Copy();
        this.cancellationToken = cancellationToken;
        this.fragmentTarget = fragmentTarget;
        this.reader = null;
        this.readerLeaveOpen = true;
        this.readerEnded = false;
        this.readerFailed = false;
        this.cancelled = false;
        this.terminalDiagnosticReported = false;
        this.readerCharacterCount = 0;
        this.readerBuffer = new char[ReaderBufferCharacters];
        HtmlBudget treeBudget = budgetValue == null ? new HtmlBudget(this.options.GetLimits(), cancellationToken) : budgetValue;
        this.diagnostics = new HtmlParseDiagnosticBuffer(this.options.GetLimits().GetMaximumDiagnostics());
        this.fragment = null;
        if (fragmentTarget == null)
        {
            this.document = new HtmlDocument(this.options.GetResponseUrl(),
                this.options.GetFallbackBaseUrl(), HtmlDocumentMode.NoQuirks,
                new HtmlDiagnostic[0], HtmlSourceSpan.CreateUnknown());
            this.document.GetRootStorage().SetSourceBackedStorage(true);
            this.document.SetContentLanguageStorage(
                this.options.GetContentLanguage());
            this.treeBuilder = new HtmlTreeBuilder(this.document, treeBudget, null, null);
        }
        else
        {
            HtmlNode? context = GetContextElement(fragmentTarget);
            if (context == null)
            {
                throw new ArgumentException("A fragment target must be an element or template content.", "fragmentTarget");
            }

            this.document = context.GetOwnerDocumentStorage();
            this.treeBuilder = null;
        }

        HtmlTokenizerOptions tokenizerOptions = HtmlTokenizerOptions.CreateDefault();
        tokenizerOptions.SetLimits(this.options.GetLimits());
        tokenizerOptions.SetTrackSourceLocations(this.options.GetTrackSourceLocations());
        tokenizerOptions.SetCancellationToken(cancellationToken);
        HtmlTokenizer tokenizerValue = budgetValue == null ? HtmlTokenizer.Create(tokenizerOptions) : HtmlTokenizer.Create(tokenizerOptions, treeBudget, inputAlreadyCounted);
        this.tokenizer = new HtmlTokenizerControl(tokenizerValue);
        if (fragmentTarget != null)
        {
            HtmlFragmentParser initializer = HtmlFragmentParser.Create(fragmentTarget, this.options.GetScriptingEnabled(), treeBudget);
            this.fragment = initializer.GetFragment();
            this.treeBuilder = initializer.GetTreeBuilder();
            initializer.InitializeTokenizerControl(this.tokenizer);
        }

        this.GetTreeBuilder().SetScriptingEnabled(this.options.GetScriptingEnabled());
        this.finalInputFed = false;
        this.tokenizerComplete = false;
        this.limitExceeded = false;
        this.terminalLimit = false;
        this.failed = false;
        this.diagnosticsTruncated = false;
        this.treeDiagnosticsCollected = false;
        this.externalDroppedDiagnosticCount = 0;
        this.tokenizerDroppedDiagnosticCount = 0;
        this.fedCharacterCount = 0;
        this.processedTokenCount = 0;
        this.detectedEncoding = null;
        this.detectedEncodingLabel = "";
        this.encodingDetection = null;
    }

    internal static HtmlParser CreateFragment(HtmlParseOptions options, HtmlNode target, CancellationToken cancellationToken)
    {
        HtmlParser parser = new HtmlParser(options);
        parser.Initialize(options, target, cancellationToken, null, false);
        return parser;
    }

    internal static HtmlParser CreateFragmentWithBudget(
        HtmlParseOptions options, HtmlNode target,
        CancellationToken cancellationToken, HtmlBudget budget,
        bool inputAlreadyCounted)
    {
        HtmlParser parser = new HtmlParser(options);
        parser.Initialize(options, target, cancellationToken, budget,
            inputAlreadyCounted);
        return parser;
    }

    /// <summary>Adds the next input segment and records whether it is the final segment.</summary>
    /// <param name = "input">The input data to process.</param>
    /// <param name = "isFinal">Whether no more input will be supplied after this chunk.</param>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    /// <exception cref = "InvalidOperationException">Thrown when the object state does not permit this operation.</exception>
    public void Feed(ReadOnlySpan<char> input, bool isFinal)
    {
        if (this.reader != null)
        {
            throw new InvalidOperationException(
                "Caller-fed input cannot be mixed with a text reader.");
        }

        this.FeedCore(input, isFinal);
    }

    internal void FeedCore(ReadOnlySpan<char> input, bool isFinal)
    {
        if (this.cancellationToken.IsCancellationRequested)
        {
            this.cancelled = true;
            return;
        }

        if (this.cancelled || this.readerFailed)
        {
            throw new InvalidOperationException(
                "The parser cannot accept input after it has stopped.");
        }

        if (this.finalInputFed || this.tokenizerComplete || this.limitExceeded)
        {
            throw new InvalidOperationException("The parser cannot accept more input.");
        }

        int maximum = this.options.GetLimits().GetMaximumInputCharacters();
        int remaining = maximum - this.fedCharacterCount;
        if (input.Length > remaining)
        {
            if (remaining > 0)
            {
                this.tokenizer.Feed(input.Slice(0, remaining), true);
                this.fedCharacterCount = this.fedCharacterCount + remaining;
            }
            else
            {
                this.tokenizer.Feed(ReadOnlySpan<char>.Empty, true);
            }

            this.finalInputFed = true;
            this.limitExceeded = true;
            this.diagnostics.AddDetails(HtmlDiagnosticCode.InputLimitExceeded, HtmlDiagnosticSeverity.Error, "The parser input character limit was exceeded.", HtmlSourceSpan.CreateUnknown());
            return;
        }

        this.tokenizer.Feed(input, isFinal);
        this.fedCharacterCount = this.fedCharacterCount + input.Length;
        this.finalInputFed = isFinal;
    }

    /// <summary>Attaches a decoded character source that <see cref = "Read"/> and <see cref = "Complete"/> pull from as needed.</summary>
    /// <param name = "readerValue">The text reader to consume.</param>
    /// <param name = "leaveOpenValue"><c>true</c> to leave the reader open after reset or terminal completion; otherwise, <c>false</c>.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when input processing has already started.</exception>
    public void SetReader(TextReader readerValue, bool leaveOpenValue)
    {
        if (readerValue == null)
        {
            throw new ArgumentNullException("readerValue");
        }

        this.EnsurePristine();
        if (this.reader != null)
        {
            throw new InvalidOperationException(
                "A text reader is already attached.");
        }

        this.reader = readerValue;
        this.readerLeaveOpen = leaveOpenValue;
    }

    /// <summary>Changes the cancellation token before input processing starts.</summary>
    /// <param name = "cancellationTokenValue">The token checked between bounded parser work units.</param>
    /// <exception cref = "InvalidOperationException">Thrown when input processing has already started.</exception>
    public void SetCancellationToken(CancellationToken cancellationTokenValue)
    {
        this.EnsurePristine();
        if (cancellationTokenValue.IsCancellationRequested)
        {
            this.cancellationToken = cancellationTokenValue;
            this.cancelled = true;
            return;
        }

        HtmlParseOptions saved = this.options.Copy();
        HtmlNode? savedTarget = this.fragmentTarget;
        TextReader? savedReader = this.reader;
        bool savedReaderLeaveOpen = this.readerLeaveOpen;
        this.Initialize(saved, savedTarget, cancellationTokenValue, null,
            false);
        this.reader = savedReader;
        this.readerLeaveOpen = savedReaderLeaveOpen;
    }

    /// <summary>Reinitializes a fresh parser for the supplied fragment context before input processing starts.</summary>
    /// <param name = "context">The element or template-content context whose tokenizer and tree-construction states apply.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when input processing has already started.</exception>
    public void InitializeFragment(HtmlFragmentContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        this.EnsurePristine();
        if (this.cancellationToken.IsCancellationRequested)
        {
            this.cancelled = true;
            return;
        }

        HtmlParseOptions saved = this.options.Copy();
        TextReader? savedReader = this.reader;
        bool savedReaderLeaveOpen = this.readerLeaveOpen;
        this.Initialize(saved, context.GetElement(), this.cancellationToken,
            null, false);
        this.reader = savedReader;
        this.readerLeaveOpen = savedReaderLeaveOpen;
    }

    internal void FeedDecoded(HtmlDecodedInput? input, bool isFinal)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        if (this.options.GetTrackByteOffsets())
        {
            this.tokenizer.EnableByteOffsets(input);
        }

        ReadOnlySpan<char> text = input.GetString().AsSpan();
        int maximum = this.options.GetLimits().GetMaximumInputCharacters();
        if (text.Length > maximum)
        {
            this.tokenizer.Feed(text.Slice(0, maximum), true);
            this.fedCharacterCount = maximum;
            this.finalInputFed = true;
            this.limitExceeded = true;
            return;
        }

        this.Feed(text, isFinal);
    }

    /// <summary>Reads the next available item or reports why one is not yet available.</summary>
    /// <returns>The parser progress status and any token made available by this step.</returns>
    public HtmlParseProgress Read()
    {
        if (this.cancelled || this.cancellationToken.IsCancellationRequested)
        {
            this.cancelled = true;
            this.failed = true;
            this.ReportCancellationDiagnostic();
            this.CloseReader();
            return new HtmlParseProgress(HtmlParseProgressStatus.Cancelled,
                this.processedTokenCount);
        }

        if (this.readerFailed)
        {
            this.failed = true;
            return new HtmlParseProgress(HtmlParseProgressStatus.Failed,
                this.processedTokenCount);
        }

        bool reading = true;
        while (reading)
        {
            HtmlParseProgress progress = this.ReadStep();
            HtmlParseProgressStatus status = progress.GetStatus();
            if (status == HtmlParseProgressStatus.NeedMoreInput
                && this.reader != null && !this.readerEnded)
            {
                this.PullReaderInput();
                if (this.cancelled)
                {
                    this.failed = true;
                    this.ReportCancellationDiagnostic();
                    return new HtmlParseProgress(
                        HtmlParseProgressStatus.Cancelled,
                        this.processedTokenCount);
                }

                if (this.readerFailed)
                {
                    this.failed = true;
                    this.ReportReaderFailureDiagnostic();
                    return new HtmlParseProgress(
                        HtmlParseProgressStatus.Failed,
                        this.processedTokenCount);
                }
            }
            else
            {
                if (status == HtmlParseProgressStatus.Complete
                    || status == HtmlParseProgressStatus.LimitExceeded
                    || status == HtmlParseProgressStatus.Cancelled
                    || status == HtmlParseProgressStatus.Failed)
                {
                    this.CloseReader();
                }

                return progress;
            }
        }

        this.failed = true;
        return new HtmlParseProgress(HtmlParseProgressStatus.Failed,
            this.processedTokenCount);
    }

    internal HtmlParseProgress ReadStep()
    {
        if (this.tokenizerComplete)
        {
            return new HtmlParseProgress(HtmlParseProgressStatus.Complete, this.processedTokenCount);
        }

        HtmlTreeBuilder treeBuilder = this.GetTreeBuilder();
        this.tokenizer.SetCdataAllowed(treeBuilder.IsCdataAllowed());
        HtmlTokenReadResult read = this.tokenizer.Read();
        this.diagnostics.AddRange(read.GetDiagnostics());
        long tokenizerDropped = read.GetDroppedDiagnosticCount();
        if (tokenizerDropped > this.tokenizerDroppedDiagnosticCount)
        {
            this.externalDroppedDiagnosticCount =
                this.externalDroppedDiagnosticCount + tokenizerDropped
                - this.tokenizerDroppedDiagnosticCount;
            this.tokenizerDroppedDiagnosticCount = tokenizerDropped;
        }

        if (read.WereDiagnosticsTruncated())
        {
            this.diagnosticsTruncated = true;
        }

        HtmlTokenReadStatus status = read.GetStatus();
        if (status == HtmlTokenReadStatus.NeedMoreInput)
        {
            return new HtmlParseProgress(HtmlParseProgressStatus.NeedMoreInput, this.processedTokenCount);
        }

        if (status == HtmlTokenReadStatus.LimitExceeded)
        {
            this.limitExceeded = true;
            this.terminalLimit = true;
            return new HtmlParseProgress(HtmlParseProgressStatus.LimitExceeded, this.processedTokenCount);
        }

        if (status == HtmlTokenReadStatus.Complete)
        {
            if (this.limitExceeded)
            {
                this.terminalLimit = true;
                return new HtmlParseProgress(
                    HtmlParseProgressStatus.LimitExceeded,
                    this.processedTokenCount);
            }

            this.tokenizerComplete = true;
            return new HtmlParseProgress(HtmlParseProgressStatus.Complete, this.processedTokenCount);
        }

        if (status == HtmlTokenReadStatus.Cancelled)
        {
            this.cancelled = true;
            this.failed = true;
            this.terminalDiagnosticReported = true;
            return new HtmlParseProgress(HtmlParseProgressStatus.Cancelled,
                this.processedTokenCount);
        }

        if (status == HtmlTokenReadStatus.Failed)
        {
            this.readerFailed = true;
            this.failed = true;
            this.terminalDiagnosticReported = true;
            return new HtmlParseProgress(HtmlParseProgressStatus.Failed,
                this.processedTokenCount);
        }

        HtmlToken? token = read.GetToken();
        if (token == null)
        {
            throw new InvalidOperationException("A token read status must contain a token.");
        }

        bool accepted = treeBuilder.ProcessToken(token);
        this.processedTokenCount = this.processedTokenCount + 1;
        if (!accepted || treeBuilder.HasTerminalLimitExceeded())
        {
            this.limitExceeded = true;
            this.terminalLimit = true;
            return new HtmlParseProgress(HtmlParseProgressStatus.LimitExceeded, this.processedTokenCount);
        }

        if (treeBuilder.HasTokenizerStateRequest())
        {
            this.tokenizer.ApplyTreeBuilderState(treeBuilder.GetRequestedTokenizerState());
            treeBuilder.ClearTokenizerStateRequest();
        }

        return new HtmlParseProgress(HtmlParseProgressStatus.TokenProcessed, this.processedTokenCount);
    }

    /// <summary>Finishes processing and returns the final bounded result.</summary>
    /// <returns>The completed document or fragment, parsing status, encoding information, and diagnostics.</returns>
    public HtmlParseResult Complete()
    {
        if (this.reader == null && !this.readerEnded && !this.finalInputFed
            && !this.limitExceeded && !this.cancelled
            && !this.readerFailed)
        {
            this.FeedCore(ReadOnlySpan<char>.Empty, true);
        }

        while (!this.tokenizerComplete && !this.terminalLimit
            && !this.cancelled && !this.readerFailed)
        {
            HtmlParseProgress progress = this.Read();
            HtmlParseProgressStatus status = progress.GetStatus();
            if (status == HtmlParseProgressStatus.NeedMoreInput)
            {
                this.failed = true;
                break;
            }

            if (status == HtmlParseProgressStatus.Cancelled
                || status == HtmlParseProgressStatus.Failed)
            {
                break;
            }
        }

        this.CloseReader();

        if (this.cancelled)
        {
            this.ReportCancellationDiagnostic();
        }

        if (this.readerFailed)
        {
            this.ReportReaderFailureDiagnostic();
        }

        this.CollectTreeDiagnostics();
        HtmlDiagnostic[] found = this.diagnostics.GetDiagnostics();
        bool truncated = this.limitExceeded || this.diagnosticsTruncated || this.diagnostics.WasTruncated() || this.GetTreeBuilder().WereDiagnosticsTruncated();
        bool complete = this.tokenizerComplete && !this.limitExceeded;
        bool success = complete && !this.failed;
        if (this.fragment == null)
        {
            this.document.SetDiagnosticsStorage(found);
            this.document.SetEncodingMetadataStorage(this.detectedEncoding,
                this.detectedEncodingLabel, this.encodingDetection);
        }

        long droppedDiagnosticCount = this.diagnostics.dropped
            + this.externalDroppedDiagnosticCount;
        long diagnosticCount = (long)found.Length + droppedDiagnosticCount;
        return new HtmlParseResult(success, complete, truncated,
            this.cancelled, this.document, this.fragment, found,
            diagnosticCount, droppedDiagnosticCount,
            this.detectedEncoding, this.detectedEncodingLabel,
            this.encodingDetection);
    }

    /// <summary>Clears accumulated state so the object can process a new input.</summary>
    /// <exception cref = "InvalidOperationException">Thrown when the object state does not permit this operation.</exception>
    public void Reset()
    {
        this.CloseReader();
        HtmlParseOptions saved = this.options.Copy();
        HtmlNode? savedTarget = this.fragmentTarget;
        CancellationToken savedCancellationToken = this.cancellationToken;
        CancellationToken initializationToken = savedCancellationToken
            .IsCancellationRequested
            ? CancellationToken.None : savedCancellationToken;
        this.Initialize(saved, savedTarget, initializationToken, null, false);
        this.cancellationToken = savedCancellationToken;
    }

    internal void AddInputDiagnostics(HtmlDiagnostic[] values)
    {
        this.diagnostics.AddRange(values);
    }

    internal void SetEncodingMetadata(Encoding? encoding, string label, HtmlEncodingDetectionResult detection)
    {
        this.detectedEncoding = encoding == null ? null : (Encoding)encoding.Clone();
        this.detectedEncodingLabel = label;
        this.encodingDetection = detection;
    }

    internal void SetInputLimitExceeded()
    {
        this.limitExceeded = true;
    }

    internal void SetInputFailed()
    {
        this.failed = true;
    }

    internal void SetInputDiagnosticsTruncated(long droppedCount)
    {
        if (droppedCount <= 0)
        {
            throw new ArgumentOutOfRangeException("droppedCount");
        }

        this.diagnosticsTruncated = true;
        this.externalDroppedDiagnosticCount =
            this.externalDroppedDiagnosticCount + droppedCount;
    }

    internal bool CanAcceptInput()
    {
        return !this.finalInputFed && !this.tokenizerComplete && !this.limitExceeded;
    }

    internal HtmlInsertionMode[] GetVisitedInsertionModes()
    {
        return this.GetTreeBuilder().GetVisitedInsertionModes();
    }

    internal HtmlNamespace[] GetVisitedNamespaces()
    {
        return this.GetTreeBuilder().GetVisitedNamespaces();
    }

    internal void CollectTreeDiagnostics()
    {
        if (this.treeDiagnosticsCollected)
        {
            return;
        }

        HtmlTreeBuilder builder = this.GetTreeBuilder();
        this.diagnostics.AddRange(builder.GetDiagnostics());
        this.externalDroppedDiagnosticCount =
            this.externalDroppedDiagnosticCount
            + builder.GetDroppedDiagnosticCount();
        this.treeDiagnosticsCollected = true;
    }

    internal HtmlTreeBuilder GetTreeBuilder()
    {
        HtmlTreeBuilder? result = this.treeBuilder;
        if (result == null)
        {
            throw new InvalidOperationException("An initialized parser requires a tree builder.");
        }

        return result;
    }

    internal static HtmlNode? GetContextElement(HtmlNode target)
    {
        if (target.GetKindStorage() == HtmlNodeKind.Element)
        {
            return target;
        }

        return target.GetTemplateHostStorage();
    }

    internal void EnsurePristine()
    {
        if (this.finalInputFed || this.tokenizerComplete
            || this.limitExceeded || this.terminalLimit || this.failed
            || this.cancelled || this.readerFailed
            || this.fedCharacterCount != 0 || this.processedTokenCount != 0)
        {
            throw new InvalidOperationException(
                "Parser initialization must occur before input processing starts.");
        }
    }

    internal void PullReaderInput()
    {
        TextReader? source = this.reader;
        if (source == null || this.readerEnded)
        {
            return;
        }

        if (this.cancellationToken.IsCancellationRequested)
        {
            this.cancelled = true;
            this.CloseReader();
            return;
        }

        int maximum = this.options.GetLimits().GetMaximumInputCharacters();
        int remaining = maximum - this.readerCharacterCount;
        int requested = remaining >= this.readerBuffer.Length
            ? this.readerBuffer.Length : remaining + 1;
        int read = source.Read(this.readerBuffer, 0, requested);
        if (read < 0 || read > requested)
        {
            this.readerFailed = true;
            this.CloseReader();
            return;
        }

        if (this.cancellationToken.IsCancellationRequested)
        {
            this.cancelled = true;
            this.CloseReader();
            return;
        }

        this.readerCharacterCount = this.readerCharacterCount + read;
        this.readerEnded = read == 0;
        this.FeedCore(new ReadOnlySpan<char>(this.readerBuffer, 0, read),
            this.readerEnded);
        if (this.readerEnded)
        {
            this.CloseReader();
        }
    }

    internal void CloseReader()
    {
        TextReader? source = this.reader;
        if (source != null && !this.readerLeaveOpen)
        {
            source.Dispose();
        }

        this.reader = null;
    }

    internal void ReportCancellationDiagnostic()
    {
        if (this.terminalDiagnosticReported)
        {
            return;
        }

        this.terminalDiagnosticReported = true;
        this.diagnostics.AddDetails(HtmlDiagnosticCode.OperationCancelled,
            HtmlDiagnosticSeverity.Error,
            "Cancellation stopped parsing before completion.",
            HtmlSourceSpan.CreateUnknown());
    }

    internal void ReportReaderFailureDiagnostic()
    {
        if (this.terminalDiagnosticReported)
        {
            return;
        }

        this.terminalDiagnosticReported = true;
        this.diagnostics.AddDetails(HtmlDiagnosticCode.InputReadFailed,
            HtmlDiagnosticSeverity.Fatal,
            "The text reader returned an invalid character count.",
            HtmlSourceSpan.CreateUnknown());
    }
}
