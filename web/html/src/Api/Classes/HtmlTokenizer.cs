using System;
using System.IO;

namespace html;
/// <summary>Incrementally converts preprocessed HTML characters into bounded tokenizer results.</summary>
public class HtmlTokenizer : IHtmlTokenizer
{
    internal const int ReaderBufferCharacters = 4096;
    internal HtmlTokenizerOptions options;
    internal HtmlTokenizerContext context;
    internal HtmlBudget? sharedBudget;
    internal TextReader? reader;
    internal bool readerLeaveOpen;
    internal bool readerEnded;
    internal bool readerFailed;
    internal bool cancelled;
    internal bool started;
    internal int readerCharacterCount;
    internal int deliveredTokenCount;
    internal char[] readerBuffer;
    internal bool fragmentInitialized;
    internal HtmlTokenizerState fragmentState;
    internal string fragmentLastStartTagName;
    internal bool fragmentCdataAllowed;
    /// <summary>Copies the supplied settings and creates an incremental tokenizer in the HTML data state.</summary>
    /// <param name="options">The settings copied for use by this tokenizer.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>options</c> is null.</exception>
    public HtmlTokenizer(HtmlTokenizerOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        this.options = options.Copy();
        this.sharedBudget = null;
        this.context = new HtmlTokenizerContext(this.options, null, false);
        this.reader = null;
        this.readerLeaveOpen = true;
        this.readerEnded = false;
        this.readerFailed = false;
        this.cancelled = false;
        this.started = false;
        this.readerCharacterCount = 0;
        this.deliveredTokenCount = 0;
        this.readerBuffer = new char[ReaderBufferCharacters];
        this.fragmentInitialized = false;
        this.fragmentState = HtmlTokenizerState.Data;
        this.fragmentLastStartTagName = "";
        this.fragmentCdataAllowed = false;
    }

    /// <summary>Copies the tokenizer settings and creates an incremental tokenizer in the HTML data state.</summary>
    /// <param name = "options">The settings copied for use by this operation.</param>
    /// <returns>A tokenizer ready to accept character input.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static HtmlTokenizer Create(HtmlTokenizerOptions options)
    {
        return new HtmlTokenizer(options);
    }

    internal static HtmlTokenizer Create(HtmlTokenizerOptions options, HtmlBudget budget, bool inputAlreadyCounted)
    {
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        HtmlTokenizer tokenizer = new HtmlTokenizer(options);
        tokenizer.sharedBudget = budget;
        tokenizer.context = new HtmlTokenizerContext(tokenizer.options, budget,
            inputAlreadyCounted);
        return tokenizer;
    }

    /// <summary>Adds the next input segment and records whether it is the final segment.</summary>
    /// <param name = "input">The input data to process.</param>
    /// <param name = "isFinal">Whether no more input will be supplied after this chunk.</param>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    public void Feed(ReadOnlySpan<char> input, bool isFinal)
    {
        if (this.reader != null)
        {
            throw new InvalidOperationException(
                "Caller-fed input cannot be mixed with a text reader.");
        }

        if (this.options.GetCancellationToken().IsCancellationRequested)
        {
            this.cancelled = true;
            return;
        }

        if (this.cancelled || this.readerFailed)
        {
            throw new InvalidOperationException(
                "The tokenizer cannot accept input after it has stopped.");
        }

        this.started = true;
        this.context.Feed(input, isFinal);
    }

    /// <summary>Attaches a decoded character source that <see cref = "Read"/> pulls from until a token or terminal status is available.</summary>
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

        if (this.started || this.reader != null || this.cancelled
            || this.readerFailed)
        {
            throw new InvalidOperationException(
                "A text reader must be attached before input processing starts.");
        }

        this.reader = readerValue;
        this.readerLeaveOpen = leaveOpenValue;
    }

    /// <summary>Initializes context-sensitive tokenizer state before input processing starts.</summary>
    /// <param name = "contextValue">The fragment context whose namespace and local name control the initial state.</param>
    /// <param name = "scriptingEnabled"><c>true</c> to use scripting-enabled HTML fragment state selection; no script is executed.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when input processing has already started.</exception>
    public void InitializeFragment(HtmlFragmentContext contextValue,
        bool scriptingEnabled)
    {
        if (contextValue == null)
        {
            throw new ArgumentNullException("contextValue");
        }

        if (this.started || this.cancelled || this.readerFailed)
        {
            throw new InvalidOperationException(
                "Fragment state must be initialized before input processing starts.");
        }

        if (this.options.GetCancellationToken().IsCancellationRequested)
        {
            this.cancelled = true;
            return;
        }

        HtmlNode element = contextValue.GetElement();
        HtmlBudget budget = new HtmlBudget(this.options.GetLimits(),
            this.options.GetCancellationToken());
        HtmlFragmentParser initializer = HtmlFragmentParser.Create(element,
            scriptingEnabled, budget);
        this.fragmentState = initializer.GetTokenizerState();
        this.fragmentLastStartTagName = initializer.GetContextElement()
            .GetLocalNameStorage();
        this.fragmentCdataAllowed = initializer.GetTreeBuilder()
            .IsCdataAllowed();
        this.fragmentInitialized = true;
        this.ApplyFragmentInitialization();
    }

    /// <summary>Reads the next available item or reports why one is not yet available.</summary>
    /// <returns>The next token-read status, token when available, and diagnostics.</returns>
    public HtmlTokenReadResult Read()
    {
        if (this.cancelled
            || this.options.GetCancellationToken().IsCancellationRequested)
        {
            this.cancelled = true;
            this.CloseReader();
            return HtmlTokenReadResult.CreateStatus(
                HtmlTokenReadStatus.Cancelled,
                new HtmlDiagnostic[] { CreateCancellationDiagnostic() },
                this.deliveredTokenCount > 0, false, 0);
        }

        if (this.readerFailed)
        {
            return HtmlTokenReadResult.CreateStatus(
                HtmlTokenReadStatus.Failed,
                new HtmlDiagnostic[] { CreateReaderFailureDiagnostic() },
                this.deliveredTokenCount > 0, false, 0);
        }

        bool reading = true;
        while (reading)
        {
            this.started = true;
            HtmlTokenReadResult result = this.context.Read();
            HtmlTokenReadStatus status = result.GetStatus();
            if (status == HtmlTokenReadStatus.TokenAvailable)
            {
                this.deliveredTokenCount = this.deliveredTokenCount + 1;
                return result;
            }

            if (status == HtmlTokenReadStatus.NeedMoreInput
                && this.reader != null && !this.readerEnded)
            {
                this.PullReaderInput();
                if (this.cancelled)
                {
                    return HtmlTokenReadResult.CreateStatus(
                        HtmlTokenReadStatus.Cancelled,
                        new HtmlDiagnostic[] { CreateCancellationDiagnostic() },
                        this.deliveredTokenCount > 0, false, 0);
                }

                if (this.readerFailed)
                {
                    return HtmlTokenReadResult.CreateStatus(
                        HtmlTokenReadStatus.Failed,
                        new HtmlDiagnostic[] { CreateReaderFailureDiagnostic() },
                        this.deliveredTokenCount > 0, false, 0);
                }
            }
            else
            {
                if (status == HtmlTokenReadStatus.Complete
                    || status == HtmlTokenReadStatus.LimitExceeded)
                {
                    this.CloseReader();
                }

                return result;
            }
        }

        return HtmlTokenReadResult.CreateStatus(HtmlTokenReadStatus.Failed);
    }

    /// <summary>Clears accumulated state so the object can process a new input.</summary>
    public void Reset()
    {
        this.CloseReader();
        this.context = new HtmlTokenizerContext(this.options, this.sharedBudget, false);
        this.reader = null;
        this.readerLeaveOpen = true;
        this.readerEnded = false;
        this.readerFailed = false;
        this.cancelled = false;
        this.started = false;
        this.readerCharacterCount = 0;
        this.deliveredTokenCount = 0;
        this.ApplyFragmentInitialization();
    }

    /// <summary>Controls whether the tokenizer recognizes CDATA sections in the current foreign-content context.</summary>
    /// <param name = "value">Whether CDATA syntax is currently allowed.</param>
    public void SetCdataAllowed(bool value)
    {
        this.context.SetCdataAllowed(value);
    }

    internal void InitializeForFragment(HtmlTokenizerState state, string lastStartTagName, bool cdataAllowed)
    {
        this.context.SetState(state);
        this.context.SetLastStartTagName(lastStartTagName);
        this.context.SetCdataAllowed(cdataAllowed);
    }

    internal void ApplyTreeBuilderState(HtmlTokenizerState state)
    {
        this.context.SetState(state);
    }

    internal void EnableByteOffsets(HtmlDecodedInput input)
    {
        this.context.EnableByteOffsets(input);
    }

    internal void ApplyFragmentInitialization()
    {
        if (this.fragmentInitialized)
        {
            this.InitializeForFragment(this.fragmentState,
                this.fragmentLastStartTagName,
                this.fragmentCdataAllowed);
        }
    }

    internal void PullReaderInput()
    {
        TextReader? source = this.reader;
        if (source == null || this.readerEnded)
        {
            return;
        }

        if (this.options.GetCancellationToken().IsCancellationRequested)
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

        if (this.options.GetCancellationToken().IsCancellationRequested)
        {
            this.cancelled = true;
            this.CloseReader();
            return;
        }

        this.readerCharacterCount = this.readerCharacterCount + read;
        this.readerEnded = read == 0;
        if (read > remaining)
        {
            if (remaining > 0)
            {
                this.context.Feed(new ReadOnlySpan<char>(this.readerBuffer,
                    0, remaining), false);
            }

            this.context.Feed(new ReadOnlySpan<char>(this.readerBuffer,
                remaining, read - remaining), false);
        }
        else
        {
            this.context.Feed(new ReadOnlySpan<char>(this.readerBuffer, 0,
                read), this.readerEnded);
        }

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

    internal static HtmlDiagnostic CreateCancellationDiagnostic()
    {
        return HtmlDiagnostic.Create(HtmlDiagnosticCode.OperationCancelled,
            HtmlDiagnosticSeverity.Error,
            "Cancellation stopped tokenization before completion.",
            HtmlSourceSpan.CreateUnknown());
    }

    internal static HtmlDiagnostic CreateReaderFailureDiagnostic()
    {
        return HtmlDiagnostic.Create(HtmlDiagnosticCode.InputReadFailed,
            HtmlDiagnosticSeverity.Fatal,
            "The text reader returned an invalid character count.",
            HtmlSourceSpan.CreateUnknown());
    }
}
