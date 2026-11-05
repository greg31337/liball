using System;
using System.Threading.Tasks;

namespace xml;

internal class XmlParserEngine
{
    internal XmlInput Input;
    internal XmlParserOptions Options;
    internal IXmlResourceResolver? Resolver;
    internal IXmlErrorReporter? ErrorReporter;
    internal IXmlCancellation? Cancellation;
    internal bool Fragment;
    internal XmlEvent[] ParsedEvents;

    internal XmlParserEngine(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? errorReporter, IXmlCancellation? cancellation, bool fragment)
    {
        Guard.NotNull(input, "input");
        Guard.NotNull(options, "options");
        ValidateXmlParserOptions(options);
        Input = input;
        Options = options;
        Resolver = resolver;
        ErrorReporter = errorReporter;
        Cancellation = cancellation;
        Fragment = fragment;
        ParsedEvents = [];
    }

    internal XmlDocument ParseXmlInput()
    {
        Task<XmlDocument> pending = Task.Run(() => ParseXmlInputCore());
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return pending.Result;
        }

        Exception failure = ReadXmlParserEngineFailure(pending.Exception);
        string? optionalType = failure.GetType().FullName;
        string type = optionalType == null ? "" : optionalType;
        if (type == "System.FormatException")
        {
            throw new XmlParser().CreateXmlParseBoundaryError(failure.Message);
        }

        if (type == "System.Text.DecoderFallbackException" || type == "System.NotSupportedException" || type == "System.ArgumentOutOfRangeException")
        {
            throw CreateXmlParserWellFormednessError(failure.Message, 0);
        }

        if (type == "System.IO.IOException" || type == "System.ObjectDisposedException")
        {
            throw CreateXmlParserResourceError(failure.Message);
        }

        throw failure;
    }

    internal XmlDocument ParseXmlInputCore()
    {
        XmlEncodingEntityKind entityKind = Fragment ? XmlEncodingEntityKind.ExternalParsedEntity : XmlEncodingEntityKind.Document;
        XmlSecurityLimits limits = Options.SecurityLimits == null ? CreateDefaultXmlSecurityLimits() : Options.SecurityLimits;
        ParserWorkLimiter limiter = new ParserWorkLimiter(limits, Cancellation);
        limiter.ErrorReporter = ErrorReporter;
        int decoderBufferSize = limits.CancellationPollingInterval < 4096 ? limits.CancellationPollingInterval : 4096;
        XmlInputDecoder decoder = new XmlInputDecoder(Input, entityKind, EncodingPolicyKind.BuiltInText, decoderBufferSize);
        decoder.ConfigureXmlInputWork(limiter);
        using (decoder)
        {
            EnsureXmlParserNotCanceled();
            limiter.PollXmlCancellation();
            XmlDecodedInput decoded = decoder.DecodeXmlInput();
            XmlVersion version = DetermineXmlParserVersion(decoded.Text, entityKind);
            XmlLineNormalizationResult normalized = LineNormalizer.NormalizeXmlLineEndings(decoded, version, entityKind);
            SourceTracker tracker = new SourceTracker(Input.BaseUri, normalized);

            XmlScanner grammarScanner = new XmlScanner(normalized.Text.AsMemory(), version, entityKind);
            grammarScanner.ConfigureXmlScannerErrors(tracker, ErrorReporter);
            grammarScanner.ConfigureXmlScannerWork(limiter);
            ParserEventQueue eventQueue = new ParserEventQueue(Options);
            eventQueue.ConfigureXmlEventLocations(tracker);
            XmlParserState state = new XmlParserState(grammarScanner, eventQueue, ErrorReporter);
            state.BeginXmlParsing();
            if (!Fragment)
            {
                eventQueue.EnqueueXmlDocumentBoundary(XmlEventKind.StartDocument, 0);
            }

            XmlDocument document;
            XmlDiagnostic[] validationDiagnostics;
            if (Fragment)
            {
                XmlNamespaceBinding[] inheritedBindings = Options.FragmentNamespaceBindings == null ? [] : Options.FragmentNamespaceBindings;
                XmlFragmentContext context = new XmlFragmentContext(inheritedBindings, Input.BaseUri, null, XmlSpaceMode.Default);
                XmlFragmentGrammarParser grammar = new XmlFragmentGrammarParser(grammarScanner, context, true, null);
                grammar.ElementParser.ConfigureXmlElementWork(limiter);
                XmlFragmentStructure structure = grammar.ParseXmlFragmentStructure();
                XmlTreeAssembler assembler = new XmlTreeAssembler(normalized, tracker, Options, limiter, version, ErrorReporter, true, structure.InheritedContext, null, null, null, null, null, null, XmlStandalone.Unspecified, null, false, eventQueue, null, []);
                document = assembler.AssembleXmlTree();
                validationDiagnostics = assembler.SnapshotDtdValidationDiagnostics();
                document.Version = structure.Version;
                document.EncodingName = decoded.EffectiveEncodingName == null ? structure.EncodingName : decoded.EffectiveEncodingName;
            }
            else
            {
                XmlDocumentGrammarParser grammar = new XmlDocumentGrammarParser(grammarScanner, true, null);
                grammar.ElementParser.ConfigureXmlElementWork(limiter);
                XmlDocumentStructure structure = grammar.ParseXmlDocumentStructure();
                ValidateXmlDocumentTypePolicy(structure);
                DtdParser? dtdParser = null;
                DtdSubset? dtdSubset = null;
                if (structure.HasDocumentType && Options.DtdProcessing == XmlDtdProcessing.Parse)
                {
                    dtdParser = new DtdParser(version, Resolver, Options.ResolverPolicy);
                    dtdParser.ConfigureDtdParserWork(limiter);
                    dtdParser.ConfigureDtdDocumentEntityVersion(structure.HasXmlDeclaration ? structure.Version : XmlVersion.Xml10);
                    dtdSubset = dtdParser.LoadDtdSubsets(structure.DocumentTypeInternalSubset, structure.DocumentTypePublicIdentifier, structure.DocumentTypeSystemIdentifier, Input.BaseUri);
                }

                XmlDocumentType? eventDocumentType = CreateXmlDocumentType(structure, tracker, dtdSubset);
                XmlEvent[] declarationEvents = dtdSubset == null ? [] : dtdSubset.DeclarationEvents;
                bool hasParameterEntityReference = dtdParser != null && dtdParser.SawParameterEntityReference;
                bool skipUnresolvedEntities = structure.Standalone != XmlStandalone.Yes && (structure.DocumentTypeSystemIdentifier != null || hasParameterEntityReference);
                XmlTreeAssembler assembler = new XmlTreeAssembler(normalized, tracker, Options, limiter, version, ErrorReporter, false, null, dtdParser, dtdParser, dtdParser, dtdParser, dtdParser, dtdParser, structure.Standalone, structure.DocumentTypeName, skipUnresolvedEntities, eventQueue, eventDocumentType, declarationEvents);
                document = assembler.AssembleXmlTree();
                validationDiagnostics = assembler.SnapshotDtdValidationDiagnostics();
                document.Version = structure.Version;
                document.Standalone = structure.Standalone;
                document.EncodingName = decoded.EffectiveEncodingName == null ? structure.EncodingName : decoded.EffectiveEncodingName;
                document.DocumentType = Options.DtdProcessing == XmlDtdProcessing.Ignore ? null : eventDocumentType;
            }

            document.DocumentUri = Input.BaseUri;
            document.BaseUri = Input.BaseUri;
            document.Diagnostics = CombineXmlParserDiagnostics(state.SnapshotXmlParserDiagnostics(), validationDiagnostics);
            state.CompleteXmlParsing();
            int diagnosticIndex = 0;
            while (diagnosticIndex < document.Diagnostics.Length)
            {
                eventQueue.EnqueueXmlDiagnosticEvent(document.Diagnostics[diagnosticIndex]);
                diagnosticIndex = diagnosticIndex + 1;
            }

            if (!Fragment)
            {
                eventQueue.EnqueueXmlDocumentBoundary(XmlEventKind.EndDocument, normalized.Text.Length);
            }

            ParsedEvents = eventQueue.SnapshotXmlEvents();
            TreeBuilder treeBuilder = new TreeBuilder(document, Fragment);
            document = treeBuilder.BuildXmlEventTree(ParsedEvents);
            state.CloseXmlParser();
            limiter.PollXmlCancellation();
            return document;
        }
    }

    internal Exception ReadXmlParserEngineFailure(AggregateException? aggregate)
    {
        Exception failure = aggregate == null ? new InvalidOperationException("XML parsing failed without an exception.") : aggregate;
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

    internal XmlEvent[] SnapshotXmlParserEvents()
    {
        XmlEvent[] copy = new XmlEvent[ParsedEvents.Length];
        Array.Copy(ParsedEvents, copy, ParsedEvents.Length);
        return copy;
    }

    internal XmlDiagnostic[] CombineXmlParserDiagnostics(XmlDiagnostic[] parserDiagnostics, XmlDiagnostic[] validationDiagnostics)
    {
        XmlDiagnostic[] diagnostics = new XmlDiagnostic[parserDiagnostics.Length + validationDiagnostics.Length];
        int index = 0;
        while (index < parserDiagnostics.Length)
        {
            diagnostics[index] = parserDiagnostics[index];
            index = index + 1;
        }

        int validationIndex = 0;
        while (validationIndex < validationDiagnostics.Length)
        {
            diagnostics[index] = validationDiagnostics[validationIndex];
            index = index + 1;
            validationIndex = validationIndex + 1;
        }

        return diagnostics;
    }

    internal void EnsureXmlParserNotCanceled()
    {
        if (Cancellation != null && Cancellation.IsXmlCancellationRequested())
        {
            throw new OperationCanceledException("XML processing was canceled.");
        }
    }

    internal XmlVersion DetermineXmlParserVersion(string text, XmlEncodingEntityKind entityKind)
    {
        XmlDeclarationParser declarationParser = new XmlDeclarationParser(text.AsMemory(), entityKind, 0);
        declarationParser.ConfigureXmlDeclarationErrors(Input.BaseUri, ErrorReporter);
        XmlDeclarationInfo declaration = declarationParser.ParseXmlOrTextDeclaration();
        if (declaration.HasDeclaration && declaration.HasVersion)
        {
            return declaration.Version;
        }

        return Options.Version;
    }

    internal XmlDocumentType? CreateXmlDocumentType(XmlDocumentStructure structure, SourceTracker tracker, DtdSubset? dtdSubset)
    {
        Guard.NotNull(structure, "structure");
        Guard.NotNull(tracker, "tracker");
        if (!structure.HasDocumentType)
        {
            return null;
        }

        XmlDocumentType documentType = new XmlDocumentType();
        documentType.Name = CreateXmlParserName(structure.DocumentTypeName == null ? "" : structure.DocumentTypeName);
        documentType.PublicIdentifier = structure.DocumentTypePublicIdentifier;
        documentType.SystemIdentifier = structure.DocumentTypeSystemIdentifier;
        documentType.InternalSubset = structure.DocumentTypeInternalSubset;
        documentType.BaseUri = Input.BaseUri;
        documentType.ElementDeclarations = dtdSubset == null ? [] : dtdSubset.ElementDeclarations;
        documentType.AttributeDeclarations = dtdSubset == null ? [] : dtdSubset.AttributeDeclarations;
        documentType.EntityDeclarations = dtdSubset == null ? [] : dtdSubset.EntityDeclarations;
        documentType.NotationDeclarations = dtdSubset == null ? [] : dtdSubset.NotationDeclarations;
        if (Options.PreserveSourceLocations)
        {
            int start = structure.DocumentTypeStartUtf16Offset;
            documentType.Range = tracker.CreateTrackedRange(start, start + structure.DocumentTypeUtf16Length);
        }

        return documentType;
    }

    internal XmlName CreateXmlParserName(string qualifiedName)
    {
        XmlName name = new XmlName();
        name.QualifiedName = qualifiedName;
        int colon = qualifiedName.IndexOf(':');
        if (colon > 0 && colon < qualifiedName.Length - 1)
        {
            name.LocalName = qualifiedName.Substring(colon + 1);
            name.Prefix = Options.PreservePrefixes ? qualifiedName.Substring(0, colon) : null;
        }
        else
        {
            name.LocalName = qualifiedName;
            name.Prefix = null;
        }

        return name;
    }

    internal void ValidateXmlDocumentTypePolicy(XmlDocumentStructure structure)
    {
        if (structure.HasDocumentType && Options.DtdProcessing == XmlDtdProcessing.Prohibit)
        {
            throw CreateXmlParserWellFormednessError("A document type definition is prohibited by the parser options", 0);
        }
    }

    internal XmlWellFormednessException CreateXmlParserWellFormednessError(string message, int offset)
    {
        SourceLocation location = new SourceLocation();
        location.SourceUri = Input.BaseUri;
        location.CharacterOffset = offset;
        location.ByteOffset = -1;
        location.Line = 1;
        location.Column = offset + 1;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.MalformedMarkup;
        diagnostic.Category = XmlErrorCategory.Xml;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        SourceRange range = new SourceRange();
        range.Start = location;
        range.End = location;
        diagnostic.Range = range;
        if (ErrorReporter != null)
        {
            ErrorReporter.ReportXmlDiagnostic(diagnostic);
        }

        return new XmlWellFormednessException(message, diagnostic, location);
    }

    internal XmlResourceException CreateXmlParserResourceError(string message)
    {
        SourceLocation location = new SourceLocation();
        location.SourceUri = Input.BaseUri;
        location.CharacterOffset = 0;
        location.ByteOffset = -1;
        location.Line = 1;
        location.Column = 1;
        SourceRange range = new SourceRange();
        range.Start = location;
        range.End = location;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.GeneralFailure;
        diagnostic.Category = XmlErrorCategory.Resource;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = range;
        if (ErrorReporter != null)
        {
            ErrorReporter.ReportXmlDiagnostic(diagnostic);
        }

        return new XmlResourceException(message, diagnostic, location);
    }

    internal XmlSecurityLimits CreateDefaultXmlSecurityLimits()
    {
        XmlSecurityLimits limits = new XmlSecurityLimits();
        limits.MaximumDepth = 256;
        limits.MaximumNodeCount = 1000000;
        limits.MaximumAttributesPerElement = 10000;
        limits.MaximumNameLength = 16384;
        limits.MaximumTextLength = 67108864;
        limits.MaximumTokenLength = 67108864;
        limits.MaximumInputCharacters = 67108864;
        limits.MaximumInputBytes = 134217728;
        limits.MaximumDtdCharacters = 16777216;
        limits.MaximumEntityDepth = 32;
        limits.MaximumEntityCount = 100000;
        limits.MaximumEntityCharacters = 67108864;
        limits.MaximumEntityAmplification = 100;
        limits.MaximumExternalResources = 64;
        limits.MaximumResourceDepth = 16;
        limits.MaximumResourceBytes = 67108864;
        limits.CancellationPollingInterval = 1024;
        return limits;
    }

    internal void ValidateXmlParserOptions(XmlParserOptions options)
    {
        if (options.Version != XmlVersion.Xml10 && options.Version != XmlVersion.Xml11)
        {
            throw new ArgumentOutOfRangeException("options.Version");
        }

        if (options.NamespaceHandling != XmlNamespaceHandling.Disabled && options.NamespaceHandling != XmlNamespaceHandling.Namespaces10 && options.NamespaceHandling != XmlNamespaceHandling.Namespaces11)
        {
            throw new ArgumentOutOfRangeException("options.NamespaceHandling");
        }

        if (options.DtdProcessing != XmlDtdProcessing.Prohibit && options.DtdProcessing != XmlDtdProcessing.Ignore && options.DtdProcessing != XmlDtdProcessing.Parse)
        {
            throw new ArgumentOutOfRangeException("options.DtdProcessing");
        }

        if (options.EntityHandling != XmlEntityHandling.Expand && options.EntityHandling != XmlEntityHandling.PreserveReferences)
        {
            throw new ArgumentOutOfRangeException("options.EntityHandling");
        }

        if (options.ValidationMode != XmlValidationMode.None && options.ValidationMode != XmlValidationMode.Dtd && options.ValidationMode != XmlValidationMode.Schema)
        {
            throw new ArgumentOutOfRangeException("options.ValidationMode");
        }
    }
}
