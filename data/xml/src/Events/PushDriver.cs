using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace xml;

internal enum PushInputMode
{
    Characters = 0,
    Bytes = 1,
    Fixed = 2
}

internal class PushDriver
{
    internal XmlParserStatus Status;
    internal XmlInput? InitialInput;
    internal XmlInput? MetadataInput;
    internal XmlParserOptions? Options;
    internal IXmlEventHandler? Handler;
    internal IXmlResourceResolver? Resolver;
    internal IXmlErrorReporter? ErrorReporter;
    internal IXmlCancellation? Cancellation;
    internal ParserWorkLimiter? FeedLimiter;
    internal PushInputMode InputMode;
    internal MemoryStream ByteBuffer;
    internal StringBuilder CharacterBuffer;
    internal XmlEvent[] PendingEvents;
    internal int PendingEventIndex;
    internal bool FinalInputReceived;
    internal bool InitialInputReleased;
    internal XmlDocument? ParsedDocument;

    internal PushDriver()
    {
        Status = XmlParserStatus.Ready;
        ByteBuffer = new MemoryStream();
        CharacterBuffer = new StringBuilder();
        PendingEvents = [];
        InputMode = PushInputMode.Characters;
    }

    internal XmlParserStatus BeginXmlPushDriver(XmlInput input, XmlParserOptions options, IXmlEventHandler? handler, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
    {
        Guard.NotNull(input, "input");
        Guard.NotNull(options, "options");
        if (Status != XmlParserStatus.Ready)
        {
            throw new InvalidOperationException("The incremental XML parser is not ready to begin.");
        }

        XmlParserEngine validator = new XmlParserEngine(input, options, resolver, reporter, cancellation, options.ParseFragment);
        XmlSecurityLimits limits = options.SecurityLimits == null ? validator.CreateDefaultXmlSecurityLimits() : options.SecurityLimits;
        FeedLimiter = new ParserWorkLimiter(limits, cancellation);
        FeedLimiter.ErrorReporter = reporter;
        InitialInput = input;
        MetadataInput = CopyXmlPushMetadata(input);
        Options = options;
        Handler = handler;
        Resolver = resolver;
        ErrorReporter = reporter;
        Cancellation = cancellation;
        PendingEvents = [];
        PendingEventIndex = 0;
        FinalInputReceived = false;
        InitialInputReleased = false;
        ParsedDocument = null;
        ByteBuffer.SetLength(0);
        CharacterBuffer.Clear();
        Status = XmlParserStatus.Error;
        if (input.Kind == XmlInputKind.String)
        {
            InputMode = PushInputMode.Characters;
            AppendXmlPushCharacters((input.Text == null ? "" : input.Text).AsMemory());
        }
        else if (input.Kind == XmlInputKind.CharacterMemory)
        {
            InputMode = PushInputMode.Characters;
            AppendXmlPushCharacters(input.Characters);
        }
        else if (input.Kind == XmlInputKind.ByteMemory)
        {
            InputMode = PushInputMode.Bytes;
            AppendXmlPushBytes(input.Bytes);
        }
        else if (input.Kind == XmlInputKind.Stream || input.Kind == XmlInputKind.TextReader)
        {
            InputMode = PushInputMode.Fixed;
        }
        else
        {
            throw new ArgumentException("The XML input kind is not supported.", "input");
        }

        Status = XmlParserStatus.Running;
        return Status;
    }

    internal XmlEventBatch FeedXmlPushByteBlock(ReadOnlyMemory<byte> bytes, bool isFinal)
    {
        EnsureXmlPushCanReceiveInput(PushInputMode.Bytes);
        Status = XmlParserStatus.Error;
        AppendXmlPushBytes(bytes);
        Status = XmlParserStatus.Running;
        if (isFinal)
        {
            FinalInputReceived = true;
            return CompleteXmlPushInput();
        }

        Status = XmlParserStatus.NeedInput;
        return CreateEmptyXmlPushBatch(Status, false);
    }

    internal XmlEventBatch FeedXmlPushCharacterBlock(ReadOnlyMemory<char> characters, bool isFinal)
    {
        EnsureXmlPushCanReceiveInput(PushInputMode.Characters);
        Status = XmlParserStatus.Error;
        AppendXmlPushCharacters(characters);
        Status = XmlParserStatus.Running;
        if (isFinal)
        {
            FinalInputReceived = true;
            return CompleteXmlPushInput();
        }

        Status = XmlParserStatus.NeedInput;
        return CreateEmptyXmlPushBatch(Status, false);
    }

    internal XmlEventBatch FinishXmlPushDriver()
    {
        if (Status == XmlParserStatus.Paused)
        {
            throw new InvalidOperationException("A paused incremental XML parser must be resumed before it can finish.");
        }

        if (Status != XmlParserStatus.Running && Status != XmlParserStatus.NeedInput)
        {
            throw new InvalidOperationException("The incremental XML parser cannot finish in its current state.");
        }

        Status = XmlParserStatus.Running;
        if (FinalInputReceived)
        {
            return DispatchPendingXmlPushEvents();
        }

        FinalInputReceived = true;
        return CompleteXmlPushInput();
    }

    internal XmlParserStatus PauseXmlPushDriver()
    {
        if (Status != XmlParserStatus.Running && Status != XmlParserStatus.NeedInput)
        {
            throw new InvalidOperationException("Only an active incremental XML parser can be paused.");
        }

        Status = XmlParserStatus.Paused;
        return Status;
    }

    internal XmlParserStatus ResumeXmlPushDriver()
    {
        if (Status != XmlParserStatus.Paused)
        {
            throw new InvalidOperationException("Only a paused incremental XML parser can be resumed.");
        }

        Status = XmlParserStatus.Running;
        return Status;
    }

    internal XmlParserStatus AbortXmlPushDriver()
    {
        if (Status != XmlParserStatus.Running && Status != XmlParserStatus.NeedInput && Status != XmlParserStatus.Paused)
        {
            throw new InvalidOperationException("Only an active incremental XML parser can be aborted.");
        }

        PendingEvents = [];
        PendingEventIndex = 0;
        ReleaseInitialXmlPushInput();
        Status = XmlParserStatus.Aborted;
        return Status;
    }

    internal void ResetXmlPushDriver()
    {
        if (Status == XmlParserStatus.Closed)
        {
            throw new InvalidOperationException("A closed incremental XML parser cannot be reset.");
        }

        ReleaseInitialXmlPushInput();
        InitialInput = null;
        MetadataInput = null;
        Options = null;
        Handler = null;
        Resolver = null;
        ErrorReporter = null;
        Cancellation = null;
        FeedLimiter = null;
        ByteBuffer.SetLength(0);
        CharacterBuffer.Clear();
        PendingEvents = [];
        PendingEventIndex = 0;
        FinalInputReceived = false;
        ParsedDocument = null;
        Status = XmlParserStatus.Ready;
    }

    internal void CloseXmlPushDriver()
    {
        if (Status == XmlParserStatus.Closed)
        {
            return;
        }

        ReleaseInitialXmlPushInput();
        InitialInput = null;
        MetadataInput = null;
        Options = null;
        Handler = null;
        Resolver = null;
        ErrorReporter = null;
        Cancellation = null;
        FeedLimiter = null;
        ByteBuffer.SetLength(0);
        CharacterBuffer.Clear();
        PendingEvents = [];
        PendingEventIndex = 0;
        ParsedDocument = null;
        Status = XmlParserStatus.Closed;
    }

    internal XmlParserStatus InspectXmlPushDriverStatus()
    {
        return Status;
    }

    internal void EnsureXmlPushCanReceiveInput(PushInputMode requiredMode)
    {
        if (Status != XmlParserStatus.Running && Status != XmlParserStatus.NeedInput)
        {
            throw new InvalidOperationException("The incremental XML parser is not accepting input.");
        }

        if (FinalInputReceived)
        {
            throw new InvalidOperationException("The final incremental XML input block has already been received.");
        }

        if (InputMode != requiredMode)
        {
            throw new InvalidOperationException("Byte and character input cannot be mixed in one incremental parse.");
        }

        Status = XmlParserStatus.Running;
    }

    internal void AppendXmlPushBytes(ReadOnlyMemory<byte> bytes)
    {
        ParserWorkLimiter? limiter = FeedLimiter;
        if (limiter == null)
        {
            throw new InvalidOperationException("The incremental XML parser has no configured input limiter.");
        }

        limiter.ObserveXmlInput(0, bytes.Length);
        limiter.AdvanceXmlParserWork(bytes.Length);
        byte[] copied = bytes.ToArray();
        ByteBuffer.Write(copied, 0, copied.Length);
    }

    internal void AppendXmlPushCharacters(ReadOnlyMemory<char> characters)
    {
        ParserWorkLimiter? limiter = FeedLimiter;
        if (limiter == null)
        {
            throw new InvalidOperationException("The incremental XML parser has no configured input limiter.");
        }

        limiter.ObserveXmlInput(characters.Length, 0);
        limiter.AdvanceXmlParserWork(characters.Length);
        CharacterBuffer.Append(characters.ToArray());
    }

    internal XmlEventBatch CompleteXmlPushInput()
    {
        XmlParserOptions? optionalOptions = Options;
        if (optionalOptions == null)
        {
            throw new InvalidOperationException("The incremental XML parser has no options.");
        }

        XmlInput parseInput = CreateCompletedXmlPushInput();
        Status = XmlParserStatus.Error;
        XmlParserEngine engine = new XmlParserEngine(parseInput, optionalOptions, Resolver, ErrorReporter, Cancellation, optionalOptions.ParseFragment);
        if (InputMode == PushInputMode.Fixed)
        {
            InitialInputReleased = true;
        }

        ParsedDocument = engine.ParseXmlInput();
        PendingEvents = engine.SnapshotXmlParserEvents();
        PendingEventIndex = 0;
        Status = XmlParserStatus.Running;
        return DispatchPendingXmlPushEvents();
    }

    internal XmlEventBatch DispatchPendingXmlPushEvents()
    {
        List<XmlEvent> emitted = new List<XmlEvent>();
        while (PendingEventIndex < PendingEvents.Length)
        {
            XmlEvent item = PendingEvents[PendingEventIndex];
            PendingEventIndex = PendingEventIndex + 1;
            emitted.Add(item);
            IXmlEventHandler? optionalHandler = Handler;
            if (optionalHandler != null)
            {
                Status = XmlParserStatus.Error;
                Task<XmlHandlerAction> pending = Task.Run(() => optionalHandler.HandleXmlEvent(item));
                ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
                if (pending.Status != TaskStatus.RanToCompletion)
                {
                    PendingEvents = [];
                    PendingEventIndex = 0;
                    ReleaseInitialXmlPushInput();
                    Exception failure = ReadXmlPushHandlerFailure(pending.Exception);
                    throw CreateXmlPushHandlerError(failure.Message, item);
                }

                XmlHandlerAction action = pending.Result;
                if (action == XmlHandlerAction.Pause)
                {
                    Status = XmlParserStatus.Paused;
                    return CreateXmlPushBatch(emitted, Status, false);
                }

                if (action == XmlHandlerAction.Abort)
                {
                    PendingEvents = [];
                    PendingEventIndex = 0;
                    ReleaseInitialXmlPushInput();
                    Status = XmlParserStatus.Aborted;
                    return CreateXmlPushBatch(emitted, Status, true);
                }

                if (action != XmlHandlerAction.Continue)
                {
                    throw new ArgumentOutOfRangeException("handlerAction");
                }

                Status = XmlParserStatus.Running;
            }
        }

        PendingEvents = [];
        PendingEventIndex = 0;
        ReleaseInitialXmlPushInput();
        Status = XmlParserStatus.Completed;
        return CreateXmlPushBatch(emitted, Status, true);
    }

    internal Exception ReadXmlPushHandlerFailure(AggregateException? aggregate)
    {
        Exception failure = aggregate == null ? new InvalidOperationException("The XML event handler failed without an exception.") : aggregate;
        while (failure.GetType().FullName == "System.AggregateException")
        {
            AggregateException current = (AggregateException)failure;
            if (current.InnerExceptions.Count == 0)
            {
                break;
            }

            failure = current.InnerExceptions[0];
        }

        return failure;
    }

    internal XmlException CreateXmlPushHandlerError(string detail, XmlEvent item)
    {
        string message = "The XML event handler failed: " + detail;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.GeneralFailure;
        diagnostic.Category = XmlErrorCategory.Xml;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = item.Range;
        IXmlErrorReporter? reporter = ErrorReporter;
        if (reporter != null)
        {
            reporter.ReportXmlDiagnostic(diagnostic);
        }

        return new XmlException(message, diagnostic, item.Range == null ? null : item.Range.Start);
    }

    internal XmlInput CreateCompletedXmlPushInput()
    {
        XmlInput? initial = InitialInput;
        if (InputMode == PushInputMode.Fixed)
        {
            if (initial == null)
            {
                throw new InvalidOperationException("The incremental XML parser has no fixed input.");
            }

            return initial;
        }

        XmlInput? metadata = MetadataInput;
        if (metadata == null)
        {
            throw new InvalidOperationException("The incremental XML parser has no input metadata.");
        }

        if (InputMode == PushInputMode.Bytes)
        {
            metadata.Kind = XmlInputKind.ByteMemory;
            metadata.Bytes = ByteBuffer.ToArray();
        }
        else
        {
            metadata.Kind = XmlInputKind.String;
            metadata.Text = CharacterBuffer.ToString();
        }

        return metadata;
    }

    internal XmlInput CopyXmlPushMetadata(XmlInput input)
    {
        XmlInput copy = new XmlInput();
        copy.BaseUri = input.BaseUri;
        copy.MediaType = input.MediaType;
        copy.EncodingName = input.EncodingName;
        copy.Encoding = input.Encoding;
        copy.OwnsResource = false;
        return copy;
    }

    internal XmlEventBatch CreateEmptyXmlPushBatch(XmlParserStatus status, bool isFinal)
    {
        XmlEventBatch batch = new XmlEventBatch();
        batch.Events = [];
        batch.Offset = 0;
        batch.Count = 0;
        batch.Status = status;
        batch.IsFinal = isFinal;
        return batch;
    }

    internal XmlEventBatch CreateXmlPushBatch(List<XmlEvent> events, XmlParserStatus status, bool isFinal)
    {
        XmlEventBatch batch = new XmlEventBatch();
        batch.Events = events.ToArray();
        batch.Offset = 0;
        batch.Count = events.Count;
        batch.Status = status;
        batch.IsFinal = isFinal;
        return batch;
    }

    internal void ReleaseInitialXmlPushInput()
    {
        XmlInput? input = InitialInput;
        if (InitialInputReleased || input == null || !input.OwnsResource)
        {
            return;
        }

        if (input.Kind == XmlInputKind.Stream && input.ByteStream != null)
        {
            input.ByteStream.Dispose();
        }
        else if (input.Kind == XmlInputKind.TextReader && input.TextReader != null)
        {
            input.TextReader.Dispose();
        }

        InitialInputReleased = true;
    }
}
