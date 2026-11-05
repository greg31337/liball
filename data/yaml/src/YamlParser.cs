using System.Collections.Generic;

namespace yaml;

internal class YamlParserState
{
    internal bool StreamStarted;
    internal bool StreamEnded;
    internal bool DocumentOpen;
    internal bool PendingSequenceEntry;
    internal bool PendingMappingValue;
    internal bool PendingExplicitMappingKey;
    internal bool PendingExplicitCollectionKey;
    internal int PendingExplicitMappingColumn;
    internal bool HasPendingScalar;
    internal YamlToken PendingScalar;
    internal int PendingMappingKeyColumn;
    internal YamlSourceSpan PendingNodeSpan;
    internal List<int> SequenceColumns;
    internal List<int> MappingColumns;
    internal List<YamlEventKind> BlockCollections;
    internal List<int> BlockCollectionColumns;
    internal List<YamlFlowParserFrame> FlowFrames;
    internal string PendingTag;
    internal string PendingTagPresentation;
    internal string PendingAnchor;
    internal YamlSourceSpan PendingTagSpan;
    internal YamlSourceSpan PendingAnchorSpan;
    internal bool HasYamlDirective;
    internal bool DirectivesAwaitDocumentStart;
    internal List<string> DeclaredTagHandles;
    internal List<string> DeclaredTagPrefixes;
    internal bool StopParsing;
    internal int ParserDiagnosticCount;
    internal bool DocumentHasRoot;
    internal bool PreviousDocumentEnded;
    internal int DocumentStartLine;
    internal bool PendingBlockAliasKey;
    internal bool HasPendingFlowCollection;
    internal bool PendingFlowCollectionWasExplicitKey;
    internal int PendingFlowCollectionEventIndex;
    internal int PendingFlowCollectionColumn;
    internal int PendingFlowCollectionStartLine;
    internal YamlSourceSpan PendingFlowCollectionSpan;

    internal YamlParserState()
    {
        StreamStarted = false;
        StreamEnded = false;
        DocumentOpen = false;
        PendingSequenceEntry = false;
        PendingMappingValue = false;
        PendingExplicitMappingKey = false;
        PendingExplicitCollectionKey = false;
        PendingExplicitMappingColumn = 0;
        HasPendingScalar = false;
        PendingScalar = new YamlToken();
        PendingMappingKeyColumn = 0;
        PendingNodeSpan = YamlSourceSpan.Empty();
        SequenceColumns = new List<int>();
        MappingColumns = new List<int>();
        BlockCollections = new List<YamlEventKind>();
        BlockCollectionColumns = new List<int>();
        FlowFrames = new List<YamlFlowParserFrame>();
        PendingTag = "";
        PendingTagPresentation = "";
        PendingAnchor = "";
        PendingTagSpan = YamlSourceSpan.Empty();
        PendingAnchorSpan = YamlSourceSpan.Empty();
        HasYamlDirective = false;
        DirectivesAwaitDocumentStart = false;
        DeclaredTagHandles = new List<string>();
        DeclaredTagPrefixes = new List<string>();
        StopParsing = false;
        ParserDiagnosticCount = 0;
        DocumentHasRoot = false;
        PreviousDocumentEnded = false;
        DocumentStartLine = 0;
        PendingBlockAliasKey = false;
        HasPendingFlowCollection = false;
        PendingFlowCollectionWasExplicitKey = false;
        PendingFlowCollectionEventIndex = -1;
        PendingFlowCollectionColumn = 0;
        PendingFlowCollectionStartLine = 0;
        PendingFlowCollectionSpan = YamlSourceSpan.Empty();
    }
}

internal class YamlFlowParserFrame
{
    internal YamlEventKind Kind;
    internal bool IsMappingKeyInParent;
    internal bool PendingMappingKey;
    internal bool PendingMappingValue;
    internal bool HasPendingScalar;
    internal YamlToken PendingScalar;
    internal YamlSourceSpan PendingNodeSpan;
    internal int SequenceEntryEventIndex;
    internal bool ImplicitMappingOpen;
    internal bool ImplicitMappingNeedsValue;
    internal YamlSourceSpan ImplicitMappingSpan;
    internal bool IsBlockCollectionCandidate;
    internal bool WasExplicitBlockKey;
    internal int CollectionEventIndex;
    internal int CollectionColumn;
    internal int CollectionStartLine;
    internal bool EntryHasNode;
    internal bool EntryComplete;
    internal bool AfterSeparator;
    internal bool ExplicitKeyIndicator;
    internal bool ExplicitKeyHasNode;
    internal int EntryStartLine;

    internal YamlFlowParserFrame(YamlEventKind kind, bool isMappingKeyInParent)
    {
        Kind = kind;
        IsMappingKeyInParent = isMappingKeyInParent;
        PendingMappingKey = false;
        PendingMappingValue = false;
        HasPendingScalar = false;
        PendingScalar = new YamlToken();
        PendingNodeSpan = YamlSourceSpan.Empty();
        SequenceEntryEventIndex = -1;
        ImplicitMappingOpen = false;
        ImplicitMappingNeedsValue = false;
        ImplicitMappingSpan = YamlSourceSpan.Empty();
        IsBlockCollectionCandidate = false;
        WasExplicitBlockKey = false;
        CollectionEventIndex = -1;
        CollectionColumn = 0;
        CollectionStartLine = 0;
        EntryHasNode = false;
        EntryComplete = false;
        AfterSeparator = false;
        ExplicitKeyIndicator = false;
        ExplicitKeyHasNode = false;
        EntryStartLine = 0;
    }
}

/// <summary>
/// Converts scanner tokens into a validated sequence of YAML parsing events.
/// </summary>
internal static class YamlParser
{
    /// <summary>
    /// Parses a token sequence into structural events and diagnostics.
    /// </summary>
    /// <param name="tokens">The scanner tokens in source order.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The resulting YAML parser result.</returns>
    internal static YamlParserResult Parse(IReadOnlyList<YamlToken>? tokens, YamlParserOptions? options)
    {
        return ParseConfigured(tokens, options, null);
    }

    internal static YamlParserResult ParseWithSecurity(IReadOnlyList<YamlToken>? tokens, YamlParserOptions? options, YamlSecurityOptions? security)
    {
        return ParseConfigured(tokens, options, security);
    }

    internal static YamlParserResult ParseConfigured(IReadOnlyList<YamlToken>? tokens, YamlParserOptions? options, YamlSecurityOptions? security)
    {
        YamlParserResult result = new YamlParserResult();
        result.ConfigureSecurity(security);
        YamlParserState state = new YamlParserState();
        YamlParserOptions effective = options == null ? new YamlParserOptions() : options;
        int index = 0;

        if (tokens == null)
        {
            AddStreamStart(result, YamlSourceSpan.Empty());
            AddStreamEnd(result, YamlSourceSpan.Empty());
            return result;
        }

        while (index < tokens.Count && !state.StopParsing)
        {
            YamlToken? token = tokens[index];
            if (token == null)
            {
                EnsureStreamStarted(result, state, YamlSourceSpan.Empty());
                AddParserError(result, state, effective, "YAMLP014", "The token sequence contains a null token.", YamlSourceSpan.Empty());
                index = index + 1;
                continue;
            }

            ValidateDirectiveDocumentStart(result, state, effective, token);
            if (state.StopParsing)
            {
                break;
            }

            if (token.Kind == YamlTokenKind.StreamStart)
            {
                if (state.StreamStarted)
                {
                    AddParserError(result, state, effective, "YAMLP001", "Stream start appeared more than once.", token.Span);
                }
                else
                {
                    AddStreamStart(result, token.Span);
                    state.StreamStarted = true;
                }
            }
            else if (token.Kind == YamlTokenKind.DocumentStart)
            {
                EnsureStreamStarted(result, state, token.Span);

                if (state.DocumentOpen)
                {
                    CloseUnterminatedFlowCollections(result, state, effective, token.Span);
                    CloseBlockCollections(result, state, token.Span);
                    AddDocumentEnd(result, token.Span);
                    state.PreviousDocumentEnded = true;
                    ResetDirectiveState(state);
                }

                AddDocumentStart(result, token.Span);
                state.DocumentOpen = true;
                state.DocumentHasRoot = false;
                state.PreviousDocumentEnded = false;
                state.DocumentStartLine = token.Span.StartLine;
                state.DirectivesAwaitDocumentStart = false;
            }
            else if (token.Kind == YamlTokenKind.DocumentEnd)
            {
                EnsureStreamStarted(result, state, token.Span);

                if (!state.DocumentOpen)
                {
                    state.PreviousDocumentEnded = true;
                    ResetDirectiveState(state);
                    index = index + 1;
                    continue;
                }

                CloseUnterminatedFlowCollections(result, state, effective, token.Span);
                CloseBlockCollections(result, state, token.Span);
                AddDocumentEnd(result, token.Span);
                state.DocumentOpen = false;
                state.DocumentHasRoot = false;
                state.PreviousDocumentEnded = true;
                ResetDirectiveState(state);
            }
            else if (token.Kind == YamlTokenKind.StreamEnd)
            {
                EnsureStreamStarted(result, state, token.Span);

                if (state.DocumentOpen)
                {
                    CloseUnterminatedFlowCollections(result, state, effective, token.Span);
                    CloseBlockCollections(result, state, token.Span);
                    AddDocumentEnd(result, token.Span);
                    state.DocumentOpen = false;
                    state.PreviousDocumentEnded = true;
                    ResetDirectiveState(state);
                }

                AddStreamEnd(result, token.Span);
                state.StreamEnded = true;
            }
            else if (token.Kind == YamlTokenKind.BlockSequenceEntry)
            {
                StartOrContinueBlockSequence(result, state, token);
            }
            else if (token.Kind == YamlTokenKind.BlockMappingKey)
            {
                if (IsInFlowCollection(state))
                {
                    AcceptFlowMappingKey(result, state, token, effective);
                }
                else
                {
                    PrepareExplicitMappingKey(result, state, token);
                }
            }
            else if (token.Kind == YamlTokenKind.Directive)
            {
                AcceptDirective(result, state, effective, token);
            }
            else if (token.Kind == YamlTokenKind.Comment)
            {
                AcceptComment(result, effective, tokens, index);
            }
            else if (token.Kind == YamlTokenKind.BlankLine)
            {
                AcceptBlankLine(result, effective, tokens, index);
            }
            else if (token.Kind == YamlTokenKind.Tag)
            {
                AcceptTag(result, state, effective, tokens, index, token);
            }
            else if (token.Kind == YamlTokenKind.Anchor)
            {
                AcceptAnchor(result, state, effective, tokens, index, token);
            }
            else if (token.Kind == YamlTokenKind.Alias)
            {
                AcceptAlias(result, state, effective, tokens, index, token);
            }
            else if (token.Kind == YamlTokenKind.FlowSequenceStart)
            {
                StartFlowSequence(result, state, token, effective);
            }
            else if (token.Kind == YamlTokenKind.FlowMappingStart)
            {
                StartFlowMapping(result, state, token, effective);
            }
            else if (token.Kind == YamlTokenKind.FlowSequenceEnd)
            {
                EndFlowSequence(result, state, token, effective);
            }
            else if (token.Kind == YamlTokenKind.FlowMappingEnd)
            {
                EndFlowMapping(result, state, token, effective);
            }
            else if (token.Kind == YamlTokenKind.FlowEntry)
            {
                AcceptFlowEntry(result, state, token, effective);
            }
            else if (token.Kind == YamlTokenKind.BlockMappingValue)
            {
                if (IsInFlowCollection(state))
                {
                    AcceptFlowMappingValue(result, state, token, effective);
                }
                else
                {
                    AcceptBlockMappingValue(result, state, token, effective);
                }
            }
            else if (token.Kind == YamlTokenKind.Dedent)
            {
                PrepareForBlockDedent(result, state, tokens, index, token);
                CloseBlockCollectionsAboveColumn(result, state, token.Span.EndColumn, token.Span);
            }
            else if (IsScalarToken(token.Kind))
            {
                int scalarEnd = CombinedScalarEndIndex(tokens, index);
                YamlToken scalarToken = CombinePlainScalarTokens(tokens, index, scalarEnd);
                bool implicitMappingKey = IsImplicitMappingKey(tokens, index, scalarEnd);
                if (!CanAcceptScalar(state, implicitMappingKey, scalarToken))
                {
                    AddParserError(result, state, effective, "YAMLP021", "A YAML node cannot follow a completed node without a collection indicator or document marker.", scalarToken.Span);
                    index = scalarEnd + 1;
                    continue;
                }

                if (IsInFlowCollection(state))
                {
                    AcceptFlowScalar(result, state, scalarToken, effective);
                }
                else if (state.PendingExplicitMappingKey && implicitMappingKey)
                {
                    state.PendingExplicitCollectionKey = true;
                    state.PendingExplicitMappingKey = false;
                    PrepareImplicitMappingKey(result, state, scalarToken);
                }
                else if (state.PendingExplicitMappingKey)
                {
                    AcceptExplicitMappingKeyScalar(state, scalarToken);
                }
                else if (implicitMappingKey)
                {
                    PrepareImplicitMappingKey(result, state, scalarToken);
                }
                else if (state.PendingMappingValue)
                {
                    AddScalarFromToken(result, state, scalarToken);
                    state.PendingMappingValue = false;
                }
                else if (state.PendingSequenceEntry && state.SequenceColumns.Count > 0)
                {
                    AddScalarFromToken(result, state, scalarToken);
                }
                else
                {
                    EnsureDocumentOpen(result, state, scalarToken.Span);
                    AddScalarFromToken(result, state, scalarToken);
                }

                index = scalarEnd;
            }
            else if (token.Kind == YamlTokenKind.Error)
            {
                AddParserError(result, state, effective, "YAMLP002", "Scanner error token reached the parser.", token.Span);
            }
            else if (token.Kind == YamlTokenKind.Whitespace || token.Kind == YamlTokenKind.Indent)
            {
                ;
            }
            else
            {
                EnsureStreamStarted(result, state, token.Span);
                AddParserError(result, state, effective, "YAMLP014", "The token sequence contains an unsupported token kind.", token.Span);
            }

            if (result.SecurityLimitExceeded)
            {
                state.StopParsing = true;
            }

            index = index + 1;
        }

        if (!state.StreamStarted)
        {
            AddStreamStart(result, YamlSourceSpan.Empty());
        }

        if (state.DocumentOpen)
        {
            if (state.StopParsing)
            {
                CloseFlowCollections(result, state, YamlSourceSpan.Empty());
            }
            else
            {
                CloseUnterminatedFlowCollections(result, state, effective, YamlSourceSpan.Empty());
            }

            CloseBlockCollections(result, state, YamlSourceSpan.Empty());
            AddDocumentEnd(result, YamlSourceSpan.Empty());
        }

        if (!state.StreamEnded)
        {
            AddStreamEnd(result, YamlSourceSpan.Empty());
        }

        return result;
    }

    internal static bool CanAcceptScalar(YamlParserState state, bool implicitMappingKey, YamlToken token)
    {
        if (IsInFlowCollection(state)
            || state.PendingExplicitMappingKey
            || state.PendingMappingValue
            || state.PendingSequenceEntry)
        {
            return true;
        }

        if (implicitMappingKey)
        {
            if (LastBlockCollectionKind(state) == YamlEventKind.SequenceStart
                && (!state.PendingSequenceEntry || token.Span.StartColumn <= LastSequenceColumn(state))
                && !HasEnclosingBlockMappingAtColumn(state, token.Span.StartColumn))
            {
                return false;
            }

            if (HasPendingNodeProperties(state))
            {
                return token.Span.StartLine != state.DocumentStartLine;
            }

            return LastBlockCollectionKind(state) != YamlEventKind.MappingStart
                || token.Span.StartColumn <= LastBlockCollectionColumn(state);
        }

        if (state.BlockCollections.Count > 0)
        {
            return false;
        }

        return !state.DocumentHasRoot;
    }

    /// <summary>
    /// Parses scanner output and carries scanner diagnostics into the parser result.
    /// </summary>
    /// <param name="scannerResult">The scanner result whose tokens and diagnostics are parsed.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The resulting YAML parser result.</returns>
    internal static YamlParserResult ParseScannerResult(YamlScannerResult? scannerResult, YamlParserOptions? options)
    {
        return ParseScannerResultConfigured(scannerResult, options, null);
    }

    internal static YamlParserResult ParseScannerResultWithSecurity(YamlScannerResult? scannerResult, YamlParserOptions? options, YamlSecurityOptions? security)
    {
        return ParseScannerResultConfigured(scannerResult, options, security);
    }

    internal static YamlParserResult ParseScannerResultConfigured(YamlScannerResult? scannerResult, YamlParserOptions? options, YamlSecurityOptions? security)
    {
        if (scannerResult == null)
        {
            return ParseWithSecurity(null, options, security);
        }

        YamlParserResult result = ParseWithSecurity(scannerResult.Tokens, options, security);

        int index = 0;
        while (index < scannerResult.Diagnostics.Count)
        {
            YamlDiagnostic diagnostic = scannerResult.Diagnostics[index];
            if (CanAppendScannerDiagnostic(result, options))
            {
                result.Diagnostics.Add(diagnostic);
                if (diagnostic.IsError())
                {
                    result.Success = false;
                }
            }
            else if (diagnostic.IsError())
            {
                result.Success = false;
            }

            index = index + 1;
        }

        return result;
    }

    internal static bool CanAppendScannerDiagnostic(YamlParserResult result, YamlParserOptions? options)
    {
        if (options == null || options.MaxDiagnostics <= 0)
        {
            return true;
        }

        return result.Diagnostics.Count < options.MaxDiagnostics;
    }

    internal static void AddStreamStart(YamlParserResult result, YamlSourceSpan span)
    {
        result.AddEvent(YamlEvent.Create(YamlEventKind.StreamStart, "", span));
    }

    internal static void AddStreamEnd(YamlParserResult result, YamlSourceSpan span)
    {
        result.AddEvent(YamlEvent.Create(YamlEventKind.StreamEnd, "", span));
    }

    internal static void AddDocumentStart(YamlParserResult result, YamlSourceSpan span)
    {
        result.AddEvent(YamlEvent.Create(YamlEventKind.DocumentStart, "", span));
    }

    internal static void AddDocumentEnd(YamlParserResult result, YamlSourceSpan span)
    {
        result.AddEvent(YamlEvent.Create(YamlEventKind.DocumentEnd, "", span));
    }

    internal static void AddParserError(YamlParserResult result, YamlParserState state, YamlParserOptions options, string code, string message, YamlSourceSpan span)
    {
        if (CanAddParserDiagnostic(state, options))
        {
            result.AddError(code, message, span);
            state.ParserDiagnosticCount = state.ParserDiagnosticCount + 1;
        }
        else
        {
            result.Success = false;
        }

        if (options == null
            || !options.Recover
            || (options.MaxDiagnostics > 0 && state.ParserDiagnosticCount >= options.MaxDiagnostics))
        {
            state.StopParsing = true;
        }
    }

    internal static void AddParserWarning(YamlParserResult result, YamlParserState state, YamlParserOptions options, string code, string message, YamlSourceSpan span)
    {
        if (!CanAddParserDiagnostic(state, options))
        {
            return;
        }

        result.AddDiagnostic(YamlDiagnostic.Warning(code, message, span));
        state.ParserDiagnosticCount = state.ParserDiagnosticCount + 1;
    }

    internal static bool CanAddParserDiagnostic(YamlParserState state, YamlParserOptions options)
    {
        if (options == null || options.MaxDiagnostics <= 0)
        {
            return true;
        }

        return state.ParserDiagnosticCount < options.MaxDiagnostics;
    }

    internal static void EnsureStreamStarted(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        if (!state.StreamStarted)
        {
            AddStreamStart(result, span);
            state.StreamStarted = true;
        }
    }

    internal static void EnsureDocumentOpen(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        EnsureStreamStarted(result, state, span);
        if (!state.DocumentOpen)
        {
            AddDocumentStart(result, span);
            state.DocumentOpen = true;
            state.DocumentHasRoot = false;
            state.PreviousDocumentEnded = false;
        }
    }

    internal static void StartOrContinueBlockSequence(YamlParserResult result, YamlParserState state, YamlToken token)
    {
        EnsureDocumentOpen(result, state, token.Span);
        bool mappingValue = state.PendingMappingValue;
        bool explicitKey = state.PendingExplicitMappingKey;
        if (explicitKey
            && HasPendingNodeProperties(state)
            && token.Span.StartColumn <= state.PendingExplicitMappingColumn)
        {
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingExplicitMappingKey = false;
            explicitKey = false;
        }

        if (state.PendingSequenceEntry && state.SequenceColumns.Count > 0)
        {
            if (token.Span.StartColumn <= LastSequenceColumn(state))
            {
                AddEmptyScalar(result, state, state.PendingNodeSpan);
            }

            state.PendingSequenceEntry = false;
        }

        if (mappingValue)
        {
            state.PendingMappingValue = false;
        }

        if (explicitKey)
        {
            state.PendingExplicitCollectionKey = true;
            state.PendingExplicitMappingKey = false;
        }

        CloseBlockCollectionsAboveColumn(result, state, token.Span.StartColumn, token.Span);
        if (!mappingValue && !explicitKey)
        {
            CloseBlockMappingsAtOrAboveColumn(result, state, token.Span.StartColumn, token.Span);
        }
        CloseBlockSequencesAboveColumn(result, state, token.Span.StartColumn, token.Span);

        if (state.SequenceColumns.Count == 0 || token.Span.StartColumn > LastSequenceColumn(state))
        {
            state.SequenceColumns.Add(token.Span.StartColumn);
            PushBlockCollection(state, YamlEventKind.SequenceStart, token.Span.StartColumn);
            AddEventWithProperties(result, state, YamlEvent.SequenceStart(YamlCollectionStyle.Block, token.Span));
        }

        state.PendingSequenceEntry = true;
        state.PendingMappingValue = false;
        if (!explicitKey)
        {
            state.PendingExplicitMappingKey = false;
        }
        state.PendingNodeSpan = token.Span;
        ClearPendingScalar(state);
    }

    internal static void CloseBlockSequencesAboveColumn(YamlParserResult result, YamlParserState state, int column, YamlSourceSpan span)
    {
        while (LastBlockCollectionKind(state) == YamlEventKind.SequenceStart && column < LastBlockCollectionColumn(state))
        {
            CloseOneBlockSequence(result, state, span);
        }
    }

    internal static void CloseBlockMappingsAboveColumn(YamlParserResult result, YamlParserState state, int column, YamlSourceSpan span)
    {
        while (LastBlockCollectionKind(state) == YamlEventKind.MappingStart && column < LastBlockCollectionColumn(state))
        {
            CloseOneBlockMapping(result, state, span);
        }
    }

    internal static void CloseBlockMappingsAtOrAboveColumn(YamlParserResult result, YamlParserState state, int column, YamlSourceSpan span)
    {
        while (LastBlockCollectionKind(state) == YamlEventKind.MappingStart && column <= LastBlockCollectionColumn(state))
        {
            CloseOneBlockMapping(result, state, span);
        }
    }

    internal static void CloseBlockCollectionsAboveColumn(YamlParserResult result, YamlParserState state, int column, YamlSourceSpan span)
    {
        while (state.BlockCollections.Count > 0 && column < LastBlockCollectionColumn(state))
        {
            CloseOneBlockCollection(result, state, span);
        }
    }

    internal static void CloseBlockSequences(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        while (LastBlockCollectionKind(state) == YamlEventKind.SequenceStart)
        {
            CloseOneBlockSequence(result, state, span);
        }

        state.PendingSequenceEntry = false;
    }

    internal static void CloseBlockMappings(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        while (LastBlockCollectionKind(state) == YamlEventKind.MappingStart)
        {
            CloseOneBlockMapping(result, state, span);
        }

        state.PendingMappingValue = false;
        ClearPendingScalar(state);
    }

    internal static void CloseBlockCollections(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        AddPendingEmptyNode(result, state);
        while (state.BlockCollections.Count > 0)
        {
            CloseOneBlockCollection(result, state, span);
        }

        state.PendingSequenceEntry = false;
        state.PendingMappingValue = false;
        state.PendingExplicitMappingKey = false;
        state.PendingExplicitCollectionKey = false;
        ClearPendingScalar(state);

        if (!state.DocumentHasRoot)
        {
            AddEmptyScalar(result, state, span);
        }
    }

    internal static void CloseOneBlockSequence(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        int index = state.SequenceColumns.Count - 1;
        state.SequenceColumns.RemoveAt(index);
        PopBlockCollection(state, YamlEventKind.SequenceStart);
        result.AddEvent(YamlEvent.Create(YamlEventKind.SequenceEnd, "", span));
    }

    internal static void CloseOneBlockMapping(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        int index = state.MappingColumns.Count - 1;
        state.MappingColumns.RemoveAt(index);
        PopBlockCollection(state, YamlEventKind.MappingStart);
        result.AddEvent(YamlEvent.Create(YamlEventKind.MappingEnd, "", span));
    }

    internal static void CloseOneBlockCollection(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        if (LastBlockCollectionKind(state) == YamlEventKind.MappingStart)
        {
            CloseOneBlockMapping(result, state, span);
        }
        else if (LastBlockCollectionKind(state) == YamlEventKind.SequenceStart)
        {
            CloseOneBlockSequence(result, state, span);
        }
    }

    internal static void PushBlockCollection(YamlParserState state, YamlEventKind kind, int column)
    {
        state.BlockCollections.Add(kind);
        state.BlockCollectionColumns.Add(column);
    }

    internal static void PopBlockCollection(YamlParserState state, YamlEventKind expectedKind)
    {
        if (state.BlockCollections.Count == 0)
        {
            return;
        }

        int index = state.BlockCollections.Count - 1;
        if (state.BlockCollections[index] == expectedKind)
        {
            state.BlockCollections.RemoveAt(index);
            state.BlockCollectionColumns.RemoveAt(index);
        }
    }

    internal static YamlEventKind LastBlockCollectionKind(YamlParserState state)
    {
        if (state.BlockCollections.Count == 0)
        {
            return YamlEventKind.StreamStart;
        }

        return state.BlockCollections[state.BlockCollections.Count - 1];
    }

    internal static int LastBlockCollectionColumn(YamlParserState state)
    {
        if (state.BlockCollectionColumns.Count == 0)
        {
            return 0;
        }

        return state.BlockCollectionColumns[state.BlockCollectionColumns.Count - 1];
    }

    internal static int LastSequenceColumn(YamlParserState state)
    {
        return state.SequenceColumns[state.SequenceColumns.Count - 1];
    }

    internal static int LastMappingColumn(YamlParserState state)
    {
        return state.MappingColumns[state.MappingColumns.Count - 1];
    }

    internal static bool HasEnclosingBlockMappingAtColumn(YamlParserState state, int column)
    {
        int index = state.BlockCollections.Count - 2;
        while (index >= 0)
        {
            if (state.BlockCollections[index] == YamlEventKind.MappingStart
                && state.BlockCollectionColumns[index] == column)
            {
                return true;
            }

            index = index - 1;
        }

        return false;
    }

    internal static bool IsScalarToken(YamlTokenKind kind)
    {
        return kind == YamlTokenKind.PlainScalar
            || kind == YamlTokenKind.SingleQuotedScalar
            || kind == YamlTokenKind.DoubleQuotedScalar
            || kind == YamlTokenKind.LiteralScalar
            || kind == YamlTokenKind.FoldedScalar;
    }

    internal static void AddScalarFromToken(YamlParserResult result, YamlParserState state, YamlToken token)
    {
        AddEventWithProperties(result, state, YamlEvent.Scalar(token.Value, ScalarStyleForToken(token.Kind), token.Span));
        state.PendingSequenceEntry = false;
    }

    internal static void AddEmptyScalar(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        YamlEvent yamlEvent = YamlEvent.Scalar("", YamlScalarStyle.Plain, span);
        yamlEvent.ImplicitEmpty = true;
        AddEventWithProperties(result, state, yamlEvent);
    }

    internal static void AddEventWithProperties(YamlParserResult result, YamlParserState state, YamlEvent yamlEvent)
    {
        if (state.PendingTag != "")
        {
            yamlEvent.Tag = state.PendingTag;
            yamlEvent.ParsedTag = state.PendingTag;
            yamlEvent.TagPresentation = state.PendingTagPresentation;
        }

        if (state.PendingAnchor != "")
        {
            yamlEvent.Anchor = state.PendingAnchor;
        }

        ClearPendingProperties(state);
        result.AddEvent(yamlEvent);
        if (yamlEvent.Kind == YamlEventKind.Scalar
            || yamlEvent.Kind == YamlEventKind.Alias
            || yamlEvent.Kind == YamlEventKind.SequenceStart
            || yamlEvent.Kind == YamlEventKind.MappingStart)
        {
            state.DocumentHasRoot = true;
        }
    }

    internal static void AcceptDirective(YamlParserResult result, YamlParserState state, YamlParserOptions options, YamlToken token)
    {
        EnsureStreamStarted(result, state, token.Span);
        state.DirectivesAwaitDocumentStart = true;
        YamlDirectiveParseResult parsed = YamlDirectiveParser.Parse(token.Value, token.Span);
        result.AddEvent(YamlEvent.DirectiveEvent(parsed.Kind, parsed.Directive));

        if (state.DocumentOpen)
        {
            AddParserError(result, state, options, "YAMLP012", "Directives must appear before document content.", token.Span);
            if (state.StopParsing)
            {
                return;
            }
        }

        if (!parsed.Valid)
        {
            AddParserError(result, state, options, parsed.DiagnosticCode, parsed.DiagnosticMessage, token.Span);
            return;
        }

        if (parsed.Warning)
        {
            AddParserWarning(result, state, options, parsed.DiagnosticCode, parsed.DiagnosticMessage, token.Span);
        }

        if (parsed.Reserved)
        {
            AddParserWarning(result, state, options, "YAMLP011", "Reserved directive '%" + parsed.Directive.Name + "' was preserved but is not interpreted.", token.Span);
            return;
        }

        if (parsed.Kind == YamlEventKind.YamlVersionDirective)
        {
            if (state.HasYamlDirective)
            {
                AddParserError(result, state, options, "YAMLP007", "YAML version directive must not be repeated for one document.", token.Span);
                return;
            }

            state.HasYamlDirective = true;
            return;
        }

        if (HasDeclaredTagHandle(state, parsed.Directive.Handle))
        {
            AddParserError(result, state, options, "YAMLP010", "TAG handle '" + parsed.Directive.Handle + "' must not be repeated for one document.", token.Span);
            return;
        }

        state.DeclaredTagHandles.Add(parsed.Directive.Handle);
        state.DeclaredTagPrefixes.Add(parsed.Directive.Prefix);
    }

    internal static void ValidateDirectiveDocumentStart(
        YamlParserResult result,
        YamlParserState state,
        YamlParserOptions options,
        YamlToken token)
    {
        if (!state.DirectivesAwaitDocumentStart
            || token.Kind == YamlTokenKind.Directive
            || token.Kind == YamlTokenKind.DocumentStart
            || IsTriviaToken(token.Kind)
            || token.Kind == YamlTokenKind.Indent
            || token.Kind == YamlTokenKind.Dedent)
        {
            return;
        }

        AddParserError(result, state, options, "YAMLP020", "Directives must be followed by an explicit document start marker.", token.Span);
        state.DirectivesAwaitDocumentStart = false;
    }

    internal static void AcceptComment(YamlParserResult result, YamlParserOptions options, IReadOnlyList<YamlToken> tokens, int index)
    {
        if (CanIncludeComments(options))
        {
            YamlToken token = tokens[index];
            YamlEvent yamlEvent = YamlEvent.Comment(token.Value, token.Span);
            yamlEvent.Trivia.Placement = TriviaPlacementForToken(tokens, index);
            result.AddEvent(yamlEvent);
        }
    }

    internal static void AcceptBlankLine(YamlParserResult result, YamlParserOptions options, IReadOnlyList<YamlToken> tokens, int index)
    {
        if (CanIncludeComments(options))
        {
            YamlToken token = tokens[index];
            YamlEvent yamlEvent = YamlEvent.Comment("", token.Span);
            yamlEvent.Trivia.Placement = TriviaPlacementForToken(tokens, index);
            result.AddEvent(yamlEvent);
        }
    }

    internal static YamlTriviaPlacement TriviaPlacementForToken(IReadOnlyList<YamlToken> tokens, int index)
    {
        int previous = PreviousTriviaContentTokenIndex(tokens, index - 1);
        YamlToken token = tokens[index];
        if (previous >= 0 && tokens[previous].Span.EndLine == token.Span.StartLine)
        {
            return YamlTriviaPlacement.Inline;
        }

        bool blankLineFollows = false;
        int current = index + 1;
        while (current < tokens.Count)
        {
            YamlTokenKind kind = tokens[current].Kind;
            if (kind == YamlTokenKind.BlankLine)
            {
                blankLineFollows = true;
            }
            else if (kind == YamlTokenKind.DocumentEnd || kind == YamlTokenKind.StreamEnd)
            {
                return previous >= 0 ? YamlTriviaPlacement.Following : YamlTriviaPlacement.Before;
            }
            else if (IsTriviaContentToken(kind))
            {
                if (blankLineFollows && previous >= 0)
                {
                    return YamlTriviaPlacement.Following;
                }

                return YamlTriviaPlacement.Before;
            }

            current = current + 1;
        }

        return previous >= 0 ? YamlTriviaPlacement.Following : YamlTriviaPlacement.Before;
    }

    internal static int PreviousTriviaContentTokenIndex(IReadOnlyList<YamlToken> tokens, int index)
    {
        int current = index;
        while (current >= 0)
        {
            if (tokens[current].Kind == YamlTokenKind.DocumentStart)
            {
                if (tokens[current].Span.EndLine == tokens[index].Span.StartLine)
                {
                    return current;
                }

                return -1;
            }

            if (IsTriviaContentToken(tokens[current].Kind))
            {
                return current;
            }

            current = current - 1;
        }

        return -1;
    }

    internal static bool IsTriviaContentToken(YamlTokenKind kind)
    {
        return kind != YamlTokenKind.Unknown
            && kind != YamlTokenKind.StreamStart
            && kind != YamlTokenKind.StreamEnd
            && kind != YamlTokenKind.DocumentStart
            && kind != YamlTokenKind.DocumentEnd
            && kind != YamlTokenKind.Indent
            && kind != YamlTokenKind.Dedent
            && kind != YamlTokenKind.Whitespace
            && kind != YamlTokenKind.Comment
            && kind != YamlTokenKind.BlankLine;
    }

    internal static bool CanIncludeComments(YamlParserOptions options)
    {
        return options == null || options.IncludeComments;
    }

    internal static void AcceptTag(
        YamlParserResult result,
        YamlParserState state,
        YamlParserOptions options,
        IReadOnlyList<YamlToken> tokens,
        int tokenIndex,
        YamlToken token)
    {
        EnsureDocumentOpen(result, state, token.Span);
        ValidateNodePropertyPosition(result, state, options, token);
        if (state.StopParsing)
        {
            return;
        }

        CompletePendingMappingKeyBeforeProperty(result, state);
        PrepareRepeatedNodeProperty(result, state, options, tokens, tokenIndex, token, true);
        state.PendingTag = ExpandTag(result, state, options, token.Value, token.Span);
        state.PendingTagPresentation = token.Value;
        state.PendingTagSpan = token.Span;
    }

    internal static void AcceptAnchor(
        YamlParserResult result,
        YamlParserState state,
        YamlParserOptions options,
        IReadOnlyList<YamlToken> tokens,
        int tokenIndex,
        YamlToken token)
    {
        EnsureDocumentOpen(result, state, token.Span);
        ValidateNodePropertyPosition(result, state, options, token);
        if (state.StopParsing)
        {
            return;
        }

        CompletePendingMappingKeyBeforeProperty(result, state);
        PrepareRepeatedNodeProperty(result, state, options, tokens, tokenIndex, token, false);
        state.PendingAnchor = StripIndicator(token.Value);
        state.PendingAnchorSpan = token.Span;
    }

    internal static void ValidateNodePropertyPosition(YamlParserResult result, YamlParserState state, YamlParserOptions options, YamlToken token)
    {
        bool unindentedMappingValue = state.PendingMappingValue
            && token.Span.StartLine > state.PendingNodeSpan.StartLine
            && state.MappingColumns.Count > 0
            && token.Span.StartColumn <= LastMappingColumn(state);
        bool betweenSequenceEntries = LastBlockCollectionKind(state) == YamlEventKind.SequenceStart
            && !state.PendingSequenceEntry;
        if (unindentedMappingValue || betweenSequenceEntries)
        {
            AddParserError(result, state, options, "YAMLP022", "A node property must be indented with and immediately precede its node.", token.Span);
        }
    }

    internal static void AcceptAlias(
        YamlParserResult result,
        YamlParserState state,
        YamlParserOptions options,
        IReadOnlyList<YamlToken> tokens,
        int tokenIndex,
        YamlToken token)
    {
        EnsureDocumentOpen(result, state, token.Span);
        if (!IsInFlowCollection(state)
            && state.PendingMappingValue
            && HasPendingNodeProperties(state)
            && PendingPropertyEndLine(state) < token.Span.StartLine)
        {
            StartOrContinueBlockMapping(result, state, token.Span.StartColumn, token.Span, true);
            state.PendingMappingValue = false;
        }

        if (HasPendingNodeProperties(state))
        {
            AddParserError(result, state, options, "YAMLP019", "An alias node cannot have a tag or anchor property.", token.Span);
            ClearPendingProperties(state);
        }

        if (IsInFlowCollection(state))
        {
            YamlFlowParserFrame frame = LastFlowFrame(state);
            if (frame.Kind == YamlEventKind.MappingStart)
            {
                if (frame.EntryComplete
                    || (frame.PendingMappingKey
                        && (!frame.ExplicitKeyIndicator || frame.ExplicitKeyHasNode)))
                {
                    AddParserError(result, state, options, "YAMLP017", "Flow mapping entries must be separated by a comma.", token.Span);
                    return;
                }

                if (frame.PendingMappingValue)
                {
                    result.AddEvent(YamlEvent.Alias(StripIndicator(token.Value), token.Span));
                    frame.PendingMappingValue = false;
                    frame.EntryComplete = true;
                    frame.AfterSeparator = false;
                }
                else
                {
                    if (!frame.EntryHasNode)
                    {
                        frame.EntryHasNode = true;
                        frame.EntryStartLine = token.Span.StartLine;
                    }

                    result.AddEvent(YamlEvent.Alias(StripIndicator(token.Value), token.Span));
                    frame.PendingMappingKey = true;
                    frame.PendingNodeSpan = token.Span;
                    frame.ExplicitKeyHasNode = frame.ExplicitKeyIndicator;
                    frame.AfterSeparator = false;
                    ClearFlowPendingScalar(frame);
                }

                return;
            }

            if (frame.EntryHasNode
                && (!frame.ImplicitMappingOpen || !frame.ImplicitMappingNeedsValue)
                && (!frame.ExplicitKeyIndicator || frame.ExplicitKeyHasNode))
            {
                AddParserError(result, state, options, "YAMLP017", "Flow sequence entries must be separated by a comma.", token.Span);
                return;
            }

            if (!frame.EntryHasNode)
            {
                frame.EntryHasNode = true;
                frame.EntryStartLine = token.Span.StartLine;
            }

            MarkFlowSequenceNode(result, frame, token.Span);
            result.AddEvent(YamlEvent.Alias(StripIndicator(token.Value), token.Span));
            frame.EntryComplete = true;
            frame.ExplicitKeyHasNode = frame.ExplicitKeyIndicator;
            frame.AfterSeparator = false;
            return;
        }

        if (state.PendingExplicitMappingKey)
        {
            result.AddEvent(YamlEvent.Alias(StripIndicator(token.Value), token.Span));
            state.PendingExplicitMappingKey = false;
            state.PendingBlockAliasKey = true;
            state.PendingNodeSpan = token.Span;
            return;
        }

        if (IsImplicitMappingKey(tokens, tokenIndex, tokenIndex))
        {
            PrepareImplicitAliasKey(result, state, token);
            return;
        }

        result.AddEvent(YamlEvent.Alias(StripIndicator(token.Value), token.Span));
        state.PendingSequenceEntry = false;
        state.PendingMappingValue = false;
    }

    internal static void PrepareRepeatedNodeProperty(
        YamlParserResult result,
        YamlParserState state,
        YamlParserOptions options,
        IReadOnlyList<YamlToken> tokens,
        int tokenIndex,
        YamlToken token,
        bool isTag)
    {
        bool repeated = isTag ? state.PendingTag != "" : state.PendingAnchor != "";
        if (!repeated)
        {
            return;
        }

        YamlSourceSpan previous = isTag ? state.PendingTagSpan : state.PendingAnchorSpan;
        if (previous.StartLine == token.Span.StartLine
            || IsInFlowCollection(state)
            || !PropertyBeginsNestedMapping(tokens, tokenIndex))
        {
            AddParserError(result, state, options, "YAMLP019", "A node cannot contain more than one tag or anchor property of the same kind.", token.Span);
            return;
        }

        string currentLineProperty = "";
        YamlSourceSpan currentLineSpan = YamlSourceSpan.Empty();
        if (isTag && state.PendingAnchor != "" && state.PendingAnchorSpan.StartLine == token.Span.StartLine)
        {
            currentLineProperty = state.PendingAnchor;
            currentLineSpan = state.PendingAnchorSpan;
            state.PendingAnchor = "";
            state.PendingAnchorSpan = YamlSourceSpan.Empty();
        }
        else if (!isTag && state.PendingTag != "" && state.PendingTagSpan.StartLine == token.Span.StartLine)
        {
            currentLineProperty = state.PendingTag;
            currentLineSpan = state.PendingTagSpan;
            state.PendingTag = "";
            state.PendingTagSpan = YamlSourceSpan.Empty();
        }

        int column = token.Span.StartColumn;
        if (currentLineProperty != "" && currentLineSpan.StartColumn < column)
        {
            column = currentLineSpan.StartColumn;
        }

        StartOrContinueBlockMapping(result, state, column, previous, true);
        if (isTag && currentLineProperty != "")
        {
            state.PendingAnchor = currentLineProperty;
            state.PendingAnchorSpan = currentLineSpan;
        }
        else if (!isTag && currentLineProperty != "")
        {
            state.PendingTag = currentLineProperty;
            state.PendingTagSpan = currentLineSpan;
        }

        state.PendingMappingValue = false;
    }

    internal static bool PropertyBeginsNestedMapping(IReadOnlyList<YamlToken> tokens, int tokenIndex)
    {
        int next = NextMeaningfulTokenIndex(tokens, tokenIndex + 1);
        while (next < tokens.Count
            && (tokens[next].Kind == YamlTokenKind.Tag || tokens[next].Kind == YamlTokenKind.Anchor))
        {
            next = NextMeaningfulTokenIndex(tokens, next + 1);
        }

        if (next >= tokens.Count)
        {
            return false;
        }

        if (IsScalarToken(tokens[next].Kind))
        {
            int scalarEnd = CombinedScalarEndIndex(tokens, next);
            return IsImplicitMappingKey(tokens, next, scalarEnd);
        }

        if (tokens[next].Kind == YamlTokenKind.BlockMappingKey
            || tokens[next].Kind == YamlTokenKind.BlockMappingValue)
        {
            return true;
        }

        if (tokens[next].Kind == YamlTokenKind.FlowSequenceStart
            || tokens[next].Kind == YamlTokenKind.FlowMappingStart)
        {
            return FlowCollectionIsMappingKey(tokens, next);
        }

        return false;
    }

    internal static bool FlowCollectionIsMappingKey(IReadOnlyList<YamlToken> tokens, int start)
    {
        int depth = 0;
        int index = start;
        while (index < tokens.Count)
        {
            YamlTokenKind kind = tokens[index].Kind;
            if (kind == YamlTokenKind.FlowSequenceStart || kind == YamlTokenKind.FlowMappingStart)
            {
                depth = depth + 1;
            }
            else if (kind == YamlTokenKind.FlowSequenceEnd || kind == YamlTokenKind.FlowMappingEnd)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    int next = NextMeaningfulTokenIndex(tokens, index + 1);
                    return next < tokens.Count
                        && tokens[next].Kind == YamlTokenKind.BlockMappingValue
                        && tokens[next].Span.StartLine == tokens[index].Span.StartLine;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static void CompletePendingMappingKeyBeforeProperty(YamlParserResult result, YamlParserState state)
    {
        if (!state.HasPendingScalar)
        {
            return;
        }

        AddScalarFromToken(result, state, state.PendingScalar);
        AddEmptyScalar(result, state, state.PendingNodeSpan);
        ClearPendingScalar(state);
        state.PendingExplicitMappingKey = false;
    }

    internal static string StripIndicator(string value)
    {
        if (value == null || value.Length <= 1)
        {
            return "";
        }

        return value.Substring(1);
    }

    internal static string NormalizeTag(string value)
    {
        if (value == null)
        {
            return "";
        }

        if (value.Length >= 3 && value[0] == '!' && value[1] == '<' && value[value.Length - 1] == '>')
        {
            return DecodeTag(value.Substring(2, value.Length - 3));
        }

        return DecodeTag(value);
    }

    internal static string DecodeTag(string value)
    {
        if (value == null || value.IndexOf('%') < 0)
        {
            return value == null ? "" : value;
        }

        return global::System.Uri.UnescapeDataString(value);
    }

    internal static string ExpandTag(YamlParserResult result, YamlParserState state, YamlParserOptions options, string value, YamlSourceSpan span)
    {
        if (value == null || value == "")
        {
            return "";
        }

        if (value.Length >= 3 && value[0] == '!' && value[1] == '<' && value[value.Length - 1] == '>')
        {
            return NormalizeTag(value);
        }

        if (value[0] != '!')
        {
            return DecodeTag(value);
        }

        string handle = "!";
        int suffixStart = 1;
        if (value.Length >= 2 && value[1] == '!')
        {
            handle = "!!";
            suffixStart = 2;
        }
        else
        {
            int index = 1;
            while (index < value.Length)
            {
                if (value[index] == '!')
                {
                    handle = value.Substring(0, index + 1);
                    suffixStart = index + 1;
                    break;
                }

                index = index + 1;
            }
        }

        string prefix = TagPrefix(state, handle);
        if (prefix == "")
        {
            AddParserError(result, state, options, "YAMLP013", "Tag handle '" + handle + "' has not been declared.", span);
            return value;
        }

        return DecodeTag(prefix + value.Substring(suffixStart));
    }

    internal static string TagPrefix(YamlParserState state, string handle)
    {
        int index = 0;
        while (index < state.DeclaredTagHandles.Count)
        {
            if (state.DeclaredTagHandles[index] == handle)
            {
                return state.DeclaredTagPrefixes[index];
            }

            index = index + 1;
        }

        if (handle == "!")
        {
            return "!";
        }

        if (handle == "!!")
        {
            return "tag:yaml.org,2002:";
        }

        return "";
    }

    internal static bool HasDeclaredTagHandle(YamlParserState state, string handle)
    {
        int index = 0;
        while (index < state.DeclaredTagHandles.Count)
        {
            if (state.DeclaredTagHandles[index] == handle)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ResetDirectiveState(YamlParserState state)
    {
        state.HasYamlDirective = false;
        state.DirectivesAwaitDocumentStart = false;
        state.DeclaredTagHandles.Clear();
        state.DeclaredTagPrefixes.Clear();
        state.HasPendingFlowCollection = false;
        state.PendingFlowCollectionWasExplicitKey = false;
        state.PendingFlowCollectionEventIndex = -1;
    }

    internal static void ClearPendingProperties(YamlParserState state)
    {
        state.PendingTag = "";
        state.PendingTagPresentation = "";
        state.PendingAnchor = "";
        state.PendingTagSpan = YamlSourceSpan.Empty();
        state.PendingAnchorSpan = YamlSourceSpan.Empty();
    }

    internal static bool HasPendingNodeProperties(YamlParserState state)
    {
        return state.PendingTag != "" || state.PendingAnchor != "";
    }

    internal static int PendingPropertyEndLine(YamlParserState state)
    {
        int line = state.PendingTag == "" ? 0 : state.PendingTagSpan.EndLine;
        if (state.PendingAnchor != "" && state.PendingAnchorSpan.EndLine > line)
        {
            line = state.PendingAnchorSpan.EndLine;
        }

        return line;
    }

    internal static int NodeStartColumn(YamlParserState state, YamlSourceSpan span)
    {
        int column = span.StartColumn;
        if (state.PendingTag != ""
            && state.PendingTagSpan.StartLine == span.StartLine
            && state.PendingTagSpan.StartColumn < column)
        {
            column = state.PendingTagSpan.StartColumn;
        }

        if (state.PendingAnchor != ""
            && state.PendingAnchorSpan.StartLine == span.StartLine
            && state.PendingAnchorSpan.StartColumn < column)
        {
            column = state.PendingAnchorSpan.StartColumn;
        }

        return column;
    }

    internal static void StartFlowSequence(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        int collectionColumn = NodeStartColumn(state, token.Span);
        bool isBlockCollectionCandidate = state.FlowFrames.Count == 0
            && !state.PendingMappingValue
            && !state.PendingSequenceEntry;
        bool wasExplicitBlockKey = isBlockCollectionCandidate && state.PendingExplicitMappingKey;
        bool collectionIsMappingKey = PrepareFlowCollectionParent(result, state, token.Span, options);
        PrepareBlockCollectionValue(state);
        EnsureDocumentOpen(result, state, token.Span);
        int eventIndex = result.Events.Count;
        AddEventWithProperties(result, state, YamlEvent.SequenceStart(YamlCollectionStyle.Flow, token.Span));
        YamlFlowParserFrame frame = new YamlFlowParserFrame(YamlEventKind.SequenceStart, collectionIsMappingKey);
        frame.IsBlockCollectionCandidate = isBlockCollectionCandidate;
        frame.WasExplicitBlockKey = wasExplicitBlockKey;
        frame.CollectionEventIndex = eventIndex;
        frame.CollectionColumn = collectionColumn;
        frame.CollectionStartLine = token.Span.StartLine;
        state.FlowFrames.Add(frame);
    }

    internal static void StartFlowMapping(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        int collectionColumn = NodeStartColumn(state, token.Span);
        bool isBlockCollectionCandidate = state.FlowFrames.Count == 0
            && !state.PendingMappingValue
            && !state.PendingSequenceEntry;
        bool wasExplicitBlockKey = isBlockCollectionCandidate && state.PendingExplicitMappingKey;
        bool collectionIsMappingKey = PrepareFlowCollectionParent(result, state, token.Span, options);
        PrepareBlockCollectionValue(state);
        EnsureDocumentOpen(result, state, token.Span);
        int eventIndex = result.Events.Count;
        AddEventWithProperties(result, state, YamlEvent.MappingStart(YamlCollectionStyle.Flow, token.Span));
        YamlFlowParserFrame frame = new YamlFlowParserFrame(YamlEventKind.MappingStart, collectionIsMappingKey);
        frame.IsBlockCollectionCandidate = isBlockCollectionCandidate;
        frame.WasExplicitBlockKey = wasExplicitBlockKey;
        frame.CollectionEventIndex = eventIndex;
        frame.CollectionColumn = collectionColumn;
        frame.CollectionStartLine = token.Span.StartLine;
        state.FlowFrames.Add(frame);
    }

    internal static void EndFlowSequence(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        if (state.FlowFrames.Count == 0)
        {
            AddParserError(result, state, options, "YAMLP003", "Flow sequence end appeared without an open flow sequence.", token.Span);
            return;
        }

        YamlFlowParserFrame frame = LastFlowFrame(state);
        if (frame.Kind != YamlEventKind.SequenceStart)
        {
            AddParserError(result, state, options, "YAMLP005", "Flow sequence end closed a flow mapping.", token.Span);
            CloseOneFlowCollection(result, state, token.Span);
            return;
        }

        CloseImplicitFlowMapping(result, state, frame, token.Span);
        state.FlowFrames.RemoveAt(state.FlowFrames.Count - 1);
        result.AddEvent(YamlEvent.Create(YamlEventKind.SequenceEnd, "", token.Span));
        MarkCompletedFlowCollection(state, frame.IsMappingKeyInParent, token.Span);
        MarkCompletedBlockFlowCollection(state, frame, token.Span);
    }

    internal static void EndFlowMapping(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        if (state.FlowFrames.Count == 0)
        {
            AddParserError(result, state, options, "YAMLP004", "Flow mapping end appeared without an open flow mapping.", token.Span);
            return;
        }

        YamlFlowParserFrame frame = LastFlowFrame(state);
        if (frame.Kind != YamlEventKind.MappingStart)
        {
            AddParserError(result, state, options, "YAMLP005", "Flow mapping end closed a flow sequence.", token.Span);
            CloseOneFlowCollection(result, state, token.Span);
            return;
        }

        CompleteFlowMappingEntry(result, state, frame);
        state.FlowFrames.RemoveAt(state.FlowFrames.Count - 1);
        result.AddEvent(YamlEvent.Create(YamlEventKind.MappingEnd, "", token.Span));
        MarkCompletedFlowCollection(state, frame.IsMappingKeyInParent, token.Span);
        MarkCompletedBlockFlowCollection(state, frame, token.Span);
    }

    internal static void CloseOneFlowCollection(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        if (state.FlowFrames.Count == 0)
        {
            return;
        }

        YamlFlowParserFrame frame = LastFlowFrame(state);
        if (frame.Kind == YamlEventKind.MappingStart)
        {
            CompleteFlowMappingEntry(result, state, frame);
            state.FlowFrames.RemoveAt(state.FlowFrames.Count - 1);
            result.AddEvent(YamlEvent.Create(YamlEventKind.MappingEnd, "", span));
        }
        else
        {
            CloseImplicitFlowMapping(result, state, frame, span);
            state.FlowFrames.RemoveAt(state.FlowFrames.Count - 1);
            result.AddEvent(YamlEvent.Create(YamlEventKind.SequenceEnd, "", span));
        }

        MarkCompletedFlowCollection(state, frame.IsMappingKeyInParent, span);
        MarkCompletedBlockFlowCollection(state, frame, span);
    }

    internal static void CloseFlowCollections(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        while (state.FlowFrames.Count > 0)
        {
            CloseOneFlowCollection(result, state, span);
        }
    }

    internal static void CloseUnterminatedFlowCollections(
        YamlParserResult result,
        YamlParserState state,
        YamlParserOptions options,
        YamlSourceSpan span)
    {
        if (state.FlowFrames.Count > 0)
        {
            AddParserError(result, state, options, "YAMLP015", "A flow collection was not closed before the document ended.", span);
        }

        CloseFlowCollections(result, state, span);
    }

    internal static void AcceptFlowEntry(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        if (state.FlowFrames.Count == 0)
        {
            return;
        }

        YamlFlowParserFrame frame = LastFlowFrame(state);
        if (!frame.EntryHasNode || frame.AfterSeparator)
        {
            AddParserError(result, state, options, "YAMLP016", "A flow collection separator must follow an entry.", token.Span);
            return;
        }

        if (frame.Kind == YamlEventKind.MappingStart)
        {
            CompleteFlowMappingEntry(result, state, frame);
        }
        else
        {
            CloseImplicitFlowMapping(result, state, frame, token.Span);
            frame.SequenceEntryEventIndex = -1;
        }

        frame.EntryHasNode = false;
        frame.EntryComplete = false;
        frame.AfterSeparator = true;
        frame.ExplicitKeyIndicator = false;
        frame.ExplicitKeyHasNode = false;
        frame.EntryStartLine = 0;
    }

    internal static void AcceptFlowMappingKey(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        YamlFlowParserFrame frame = LastFlowFrame(state);
        if (frame.EntryHasNode)
        {
            AddParserError(result, state, options, "YAMLP017", "Flow collection entries must be separated by a comma.", token.Span);
            return;
        }

        frame.EntryHasNode = true;
        frame.EntryComplete = false;
        frame.AfterSeparator = false;
        frame.ExplicitKeyIndicator = true;
        frame.EntryStartLine = token.Span.StartLine;
        if (frame.Kind == YamlEventKind.MappingStart)
        {
            frame.PendingMappingKey = true;
            frame.PendingNodeSpan = token.Span;
            ClearFlowPendingScalar(frame);
            return;
        }

        MarkFlowSequenceNode(result, frame, token.Span);
    }

    internal static void AcceptFlowScalar(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        YamlFlowParserFrame frame = LastFlowFrame(state);
        if (state.FlowFrames.Count == 1
            && !frame.IsBlockCollectionCandidate
            && state.MappingColumns.Count > 0
            && token.Span.StartLine > frame.CollectionStartLine
            && token.Span.StartColumn <= LastMappingColumn(state))
        {
            AddParserError(result, state, options, "YAMLP022", "A flow collection used as a mapping value must be indented on continuation lines.", token.Span);
            return;
        }

        if (frame.Kind == YamlEventKind.MappingStart)
        {
            if (frame.EntryComplete
                || (frame.PendingMappingKey
                    && (frame.HasPendingScalar || !frame.ExplicitKeyIndicator || frame.ExplicitKeyHasNode)))
            {
                AddParserError(result, state, options, "YAMLP017", "Flow mapping entries must be separated by a comma.", token.Span);
                return;
            }

            if (frame.PendingMappingValue)
            {
                AddScalarFromToken(result, state, token);
                frame.PendingMappingValue = false;
                frame.EntryComplete = true;
                return;
            }

            if (!frame.EntryHasNode)
            {
                frame.EntryHasNode = true;
                frame.EntryStartLine = token.Span.StartLine;
            }

            frame.AfterSeparator = false;
            frame.PendingScalar = token;
            frame.HasPendingScalar = true;
            frame.PendingMappingKey = true;
            frame.PendingNodeSpan = token.Span;
            if (frame.ExplicitKeyIndicator)
            {
                frame.ExplicitKeyHasNode = true;
            }
            return;
        }

        if (frame.EntryHasNode
            && (!frame.ImplicitMappingOpen || !frame.ImplicitMappingNeedsValue)
            && (!frame.ExplicitKeyIndicator || frame.ExplicitKeyHasNode))
        {
            AddParserError(result, state, options, "YAMLP017", "Flow sequence entries must be separated by a comma.", token.Span);
            return;
        }

        if (!frame.EntryHasNode)
        {
            frame.EntryHasNode = true;
            frame.EntryStartLine = token.Span.StartLine;
        }

        frame.AfterSeparator = false;
        MarkFlowSequenceNode(result, frame, token.Span);
        AddScalarFromToken(result, state, token);
        frame.EntryComplete = true;
        if (frame.ExplicitKeyIndicator)
        {
            frame.ExplicitKeyHasNode = true;
        }
    }

    internal static void AcceptFlowMappingValue(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        YamlFlowParserFrame frame = LastFlowFrame(state);
        if (frame.Kind == YamlEventKind.SequenceStart)
        {
            if (frame.EntryHasNode
                && !frame.ExplicitKeyIndicator
                && frame.EntryStartLine != token.Span.StartLine)
            {
                AddParserError(result, state, options, "YAMLP018", "An implicit flow mapping key must be confined to one line.", token.Span);
                return;
            }

            if (!frame.EntryHasNode)
            {
                frame.EntryHasNode = true;
                frame.EntryStartLine = token.Span.StartLine;
            }

            frame.AfterSeparator = false;
            StartImplicitFlowMapping(result, state, frame, token);
            frame.EntryComplete = false;
            return;
        }

        if (frame.PendingMappingValue || frame.EntryComplete)
        {
            AddParserError(result, state, options, "YAMLP017", "A flow mapping value indicator must follow a mapping key.", token.Span);
            return;
        }

        if (!frame.EntryHasNode)
        {
            frame.EntryHasNode = true;
            frame.EntryStartLine = token.Span.StartLine;
        }

        frame.AfterSeparator = false;

        if (frame.PendingMappingKey && frame.HasPendingScalar)
        {
            AddScalarFromToken(result, state, frame.PendingScalar);
            ClearFlowPendingScalar(frame);
        }
        else if (frame.PendingMappingKey)
        {
            ClearFlowPendingScalar(frame);
        }
        else
        {
            AddEmptyScalar(result, state, token.Span);
        }

        frame.PendingMappingKey = false;
        frame.PendingMappingValue = true;
        frame.PendingNodeSpan = token.Span;
    }

    internal static void PrepareBlockCollectionValue(YamlParserState state)
    {
        if (state.PendingMappingValue)
        {
            state.PendingExplicitCollectionKey = false;
        }

        state.PendingSequenceEntry = false;
        state.PendingMappingValue = false;
        ClearPendingScalar(state);
    }

    internal static bool IsInFlowCollection(YamlParserState state)
    {
        return state.FlowFrames.Count > 0;
    }

    internal static bool IsInFlowMapping(YamlParserState state)
    {
        if (state.FlowFrames.Count == 0)
        {
            return false;
        }

        return LastFlowFrame(state).Kind == YamlEventKind.MappingStart;
    }

    internal static YamlFlowParserFrame LastFlowFrame(YamlParserState state)
    {
        return state.FlowFrames[state.FlowFrames.Count - 1];
    }

    internal static bool PrepareFlowCollectionParent(YamlParserResult result, YamlParserState state, YamlSourceSpan span, YamlParserOptions options)
    {
        if (state.FlowFrames.Count == 0)
        {
            return false;
        }

        YamlFlowParserFrame parent = LastFlowFrame(state);
        if (parent.EntryHasNode
            && (parent.Kind == YamlEventKind.MappingStart
                || !parent.ImplicitMappingOpen
                || !parent.ImplicitMappingNeedsValue))
        {
            if ((parent.Kind != YamlEventKind.MappingStart || !parent.PendingMappingValue)
                && (!parent.ExplicitKeyIndicator || parent.ExplicitKeyHasNode))
            {
                AddParserError(result, state, options, "YAMLP017", "Flow collection entries must be separated by a comma.", span);
            }
        }

        if (parent.Kind == YamlEventKind.MappingStart)
        {
            if (parent.PendingMappingValue)
            {
                parent.PendingMappingValue = false;
                parent.EntryComplete = true;
                return false;
            }

            if (!parent.EntryHasNode)
            {
                parent.EntryHasNode = true;
                parent.EntryStartLine = span.StartLine;
            }

            parent.AfterSeparator = false;
            parent.PendingNodeSpan = span;
            if (parent.ExplicitKeyIndicator)
            {
                parent.ExplicitKeyHasNode = true;
            }
            return true;
        }

        if (!parent.EntryHasNode)
        {
            parent.EntryHasNode = true;
            parent.EntryStartLine = span.StartLine;
        }

        parent.AfterSeparator = false;
        MarkFlowSequenceNode(result, parent, span);
        parent.EntryComplete = true;
        if (parent.ExplicitKeyIndicator)
        {
            parent.ExplicitKeyHasNode = true;
        }
        return false;
    }

    internal static void MarkCompletedFlowCollection(YamlParserState state, bool collectionWasMappingKey, YamlSourceSpan span)
    {
        if (!collectionWasMappingKey || state.FlowFrames.Count == 0)
        {
            return;
        }

        YamlFlowParserFrame parent = LastFlowFrame(state);
        if (parent.Kind == YamlEventKind.MappingStart)
        {
            parent.PendingMappingKey = true;
            parent.PendingMappingValue = false;
            parent.PendingNodeSpan = span;
            ClearFlowPendingScalar(parent);
        }
    }

    internal static void CompleteFlowMappingEntry(YamlParserResult result, YamlParserState state, YamlFlowParserFrame frame)
    {
        if (frame.PendingMappingValue)
        {
            AddEmptyScalar(result, state, frame.PendingNodeSpan);
        }
        else if (frame.PendingMappingKey)
        {
            if (frame.HasPendingScalar)
            {
                AddScalarFromToken(result, state, frame.PendingScalar);
            }
            else
            {
                AddEmptyScalar(result, state, frame.PendingNodeSpan);
            }

            AddEmptyScalar(result, state, frame.PendingNodeSpan);
        }

        frame.PendingMappingKey = false;
        frame.PendingMappingValue = false;
        ClearFlowPendingScalar(frame);
    }

    internal static void ClearFlowPendingScalar(YamlFlowParserFrame frame)
    {
        frame.HasPendingScalar = false;
        frame.PendingScalar = new YamlToken();
    }

    internal static void MarkCompletedBlockFlowCollection(YamlParserState state, YamlFlowParserFrame frame, YamlSourceSpan span)
    {
        if (!frame.IsBlockCollectionCandidate || state.FlowFrames.Count > 0)
        {
            return;
        }

        state.HasPendingFlowCollection = true;
        state.PendingFlowCollectionWasExplicitKey = frame.WasExplicitBlockKey;
        state.PendingFlowCollectionEventIndex = frame.CollectionEventIndex;
        state.PendingFlowCollectionColumn = frame.CollectionColumn;
        state.PendingFlowCollectionStartLine = frame.CollectionStartLine;
        state.PendingFlowCollectionSpan = span;
        if (frame.WasExplicitBlockKey)
        {
            state.PendingExplicitMappingKey = false;
        }
    }

    internal static void MarkFlowSequenceNode(YamlParserResult result, YamlFlowParserFrame frame, YamlSourceSpan span)
    {
        if (frame.ImplicitMappingOpen && frame.ImplicitMappingNeedsValue)
        {
            frame.ImplicitMappingNeedsValue = false;
            return;
        }

        if (frame.SequenceEntryEventIndex < 0)
        {
            frame.SequenceEntryEventIndex = result.Events.Count;
            frame.ImplicitMappingSpan = span;
        }
    }

    internal static void StartImplicitFlowMapping(YamlParserResult result, YamlParserState state, YamlFlowParserFrame frame, YamlToken token)
    {
        int eventIndex = frame.SequenceEntryEventIndex;
        if (eventIndex < 0)
        {
            eventIndex = result.Events.Count;
        }

        result.InsertEvent(eventIndex, YamlEvent.MappingStart(YamlCollectionStyle.Flow, frame.ImplicitMappingSpan));
        AdjustFlowEventIndexesAfterInsert(state, eventIndex, frame);
        if (frame.SequenceEntryEventIndex < 0)
        {
            AddEmptyScalar(result, state, token.Span);
        }

        frame.SequenceEntryEventIndex = eventIndex;
        frame.ImplicitMappingOpen = true;
        frame.ImplicitMappingNeedsValue = true;
        frame.ImplicitMappingSpan = token.Span;
    }

    internal static void CloseImplicitFlowMapping(YamlParserResult result, YamlParserState state, YamlFlowParserFrame frame, YamlSourceSpan span)
    {
        if (!frame.ImplicitMappingOpen)
        {
            return;
        }

        if (frame.ImplicitMappingNeedsValue)
        {
            AddEmptyScalar(result, state, frame.ImplicitMappingSpan);
        }

        result.AddEvent(YamlEvent.Create(YamlEventKind.MappingEnd, "", span));
        frame.ImplicitMappingOpen = false;
        frame.ImplicitMappingNeedsValue = false;
    }

    internal static void AdjustFlowEventIndexesAfterInsert(YamlParserState state, int eventIndex, YamlFlowParserFrame insertedFrame)
    {
        int index = 0;
        while (index < state.FlowFrames.Count)
        {
            YamlFlowParserFrame frame = state.FlowFrames[index];
            if (frame != insertedFrame && frame.SequenceEntryEventIndex >= eventIndex)
            {
                frame.SequenceEntryEventIndex = frame.SequenceEntryEventIndex + 1;
            }

            index = index + 1;
        }
    }

    internal static bool IsImplicitMappingKey(IReadOnlyList<YamlToken> tokens, int index, int scalarEnd)
    {
        YamlToken token = tokens[index];
        int next = NextMeaningfulTokenIndex(tokens, scalarEnd + 1);
        if (next >= tokens.Count)
        {
            return false;
        }

        if (tokens[next].Kind != YamlTokenKind.BlockMappingValue)
        {
            return false;
        }

        return tokens[next].Span.StartLine == token.Span.StartLine;
    }

    internal static int CombinedScalarEndIndex(IReadOnlyList<YamlToken> tokens, int index)
    {
        if (tokens[index].Kind != YamlTokenKind.PlainScalar)
        {
            return index;
        }

        int end = index;
        bool scanning = true;
        while (scanning)
        {
            scanning = false;
            int whitespace = end + 1;
            int next = end + 2;
            if (next < tokens.Count
                && tokens[whitespace].Kind == YamlTokenKind.Whitespace
                && tokens[next].Kind == YamlTokenKind.PlainScalar
                && tokens[whitespace].Span.StartLine == tokens[index].Span.StartLine
                && tokens[next].Span.StartLine == tokens[index].Span.StartLine)
            {
                end = next;
                scanning = true;
            }
        }

        return end;
    }

    internal static YamlToken CombinePlainScalarTokens(IReadOnlyList<YamlToken> tokens, int index, int end)
    {
        if (index == end)
        {
            return tokens[index];
        }

        string value = "";
        int current = index;
        while (current <= end)
        {
            if (tokens[current].Kind == YamlTokenKind.PlainScalar || tokens[current].Kind == YamlTokenKind.Whitespace)
            {
                value = value + tokens[current].Value;
            }

            current = current + 1;
        }

        YamlSourceSpan start = tokens[index].Span;
        YamlSourceSpan finish = tokens[end].Span;
        return YamlToken.Create(
            YamlTokenKind.PlainScalar,
            value,
            YamlSourceSpan.FromBounds(start.StartIndex, start.StartLine, start.StartColumn, finish.EndIndex, finish.EndLine, finish.EndColumn));
    }

    internal static int NextMeaningfulTokenIndex(IReadOnlyList<YamlToken> tokens, int index)
    {
        int current = index;
        while (current < tokens.Count && IsTriviaToken(tokens[current].Kind))
        {
            current = current + 1;
        }

        return current;
    }

    internal static bool IsTriviaToken(YamlTokenKind kind)
    {
        return kind == YamlTokenKind.Whitespace
            || kind == YamlTokenKind.Comment
            || kind == YamlTokenKind.BlankLine;
    }

    internal static void PrepareImplicitMappingKey(YamlParserResult result, YamlParserState state, YamlToken token)
    {
        if (state.HasPendingScalar)
        {
            AddScalarFromToken(result, state, state.PendingScalar);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            ClearPendingScalar(state);
        }

        int keyColumn = NodeStartColumn(state, token.Span);
        CloseBlockSequencesAtOrAboveColumn(result, state, keyColumn, token.Span);
        bool propertiesPrecedeKey = HasPendingNodeProperties(state)
            && PendingPropertyEndLine(state) < token.Span.StartLine;
        if (state.PendingMappingValue)
        {
            if (state.MappingColumns.Count > 0 && keyColumn > LastMappingColumn(state))
            {
                StartOrContinueBlockMapping(result, state, keyColumn, token.Span, propertiesPrecedeKey);
                state.PendingMappingValue = false;
            }
            else
            {
                AddEmptyScalar(result, state, state.PendingNodeSpan);
                state.PendingMappingValue = false;
                CloseBlockCollectionsAboveColumn(result, state, keyColumn, token.Span);
            }
        }
        else
        {
            CloseBlockCollectionsAboveColumn(result, state, keyColumn, token.Span);
            if (propertiesPrecedeKey
                && (state.MappingColumns.Count == 0 || keyColumn > LastMappingColumn(state)))
            {
                StartOrContinueBlockMapping(result, state, keyColumn, token.Span, true);
            }
        }

        state.PendingScalar = token;
        state.HasPendingScalar = true;
        state.PendingMappingKeyColumn = keyColumn;
        state.PendingNodeSpan = token.Span;
    }

    internal static void PrepareImplicitAliasKey(YamlParserResult result, YamlParserState state, YamlToken token)
    {
        CloseBlockSequencesAtOrAboveColumn(result, state, token.Span.StartColumn, token.Span);
        if (state.PendingMappingValue)
        {
            if (state.MappingColumns.Count > 0 && token.Span.StartColumn > LastMappingColumn(state))
            {
                StartOrContinueBlockMapping(result, state, token.Span.StartColumn, token.Span, true);
                state.PendingMappingValue = false;
            }
            else
            {
                AddEmptyScalar(result, state, state.PendingNodeSpan);
                state.PendingMappingValue = false;
                CloseBlockCollectionsAboveColumn(result, state, token.Span.StartColumn, token.Span);
            }
        }
        else
        {
            CloseBlockCollectionsAboveColumn(result, state, token.Span.StartColumn, token.Span);
            StartOrContinueBlockMapping(result, state, token.Span.StartColumn, token.Span, false);
        }

        result.AddEvent(YamlEvent.Alias(StripIndicator(token.Value), token.Span));
        state.PendingBlockAliasKey = true;
        state.PendingMappingKeyColumn = token.Span.StartColumn;
        state.PendingNodeSpan = token.Span;
    }

    internal static void AcceptBlockMappingValue(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        if (state.HasPendingFlowCollection)
        {
            AcceptBlockMappingValueAfterFlowCollection(result, state, token, options);
            return;
        }

        if (!state.HasPendingScalar)
        {
            if (state.PendingBlockAliasKey)
            {
                state.PendingBlockAliasKey = false;
                state.PendingMappingValue = true;
                state.PendingNodeSpan = token.Span;
                return;
            }

            if (state.PendingExplicitCollectionKey)
            {
                CloseExplicitKeyCollections(result, state, token.Span);
                state.PendingExplicitCollectionKey = false;
                state.PendingExplicitMappingKey = false;
                state.PendingMappingValue = true;
                state.PendingNodeSpan = token.Span;
                return;
            }

            if (state.PendingExplicitMappingKey)
            {
                if (token.Span.StartColumn > state.PendingMappingKeyColumn)
                {
                    state.PendingExplicitCollectionKey = true;
                    state.PendingExplicitMappingKey = false;
                    StartOrContinueBlockMapping(result, state, token.Span.StartColumn, token.Span, false);
                    AddEmptyScalar(result, state, token.Span);
                    state.PendingMappingValue = true;
                    state.PendingNodeSpan = token.Span;
                    return;
                }

                AddEmptyScalar(result, state, state.PendingNodeSpan);
                state.PendingExplicitMappingKey = false;
                state.PendingMappingValue = true;
                state.PendingNodeSpan = token.Span;
                return;
            }

            bool propertiesPrecedeValue = HasPendingNodeProperties(state)
                && PendingPropertyEndLine(state) < token.Span.StartLine;
            int emptyKeyColumn = NodeStartColumn(state, token.Span);
            StartOrContinueBlockMapping(result, state, emptyKeyColumn, token.Span, propertiesPrecedeValue);
            AddEmptyScalar(result, state, token.Span);
            state.PendingMappingValue = true;
            state.PendingNodeSpan = token.Span;
            return;
        }

        StartOrContinueBlockMapping(result, state, state.PendingMappingKeyColumn, state.PendingScalar.Span, false);
        AddScalarFromToken(result, state, state.PendingScalar);
        ClearPendingScalar(state);
        state.PendingMappingValue = true;
        state.PendingExplicitMappingKey = false;
        state.PendingNodeSpan = token.Span;
    }

    internal static void AcceptBlockMappingValueAfterFlowCollection(YamlParserResult result, YamlParserState state, YamlToken token, YamlParserOptions options)
    {
        if (!state.PendingFlowCollectionWasExplicitKey
            && state.PendingFlowCollectionStartLine != token.Span.StartLine)
        {
            AddParserError(result, state, options, "YAMLP018", "An implicit mapping key must be confined to one line.", token.Span);
            state.HasPendingFlowCollection = false;
            return;
        }

        bool compactExplicitKey = state.PendingFlowCollectionWasExplicitKey
            && token.Span.StartLine == state.PendingFlowCollectionSpan.EndLine
            && token.Span.StartColumn > state.PendingExplicitMappingColumn;

        if (!state.PendingFlowCollectionWasExplicitKey || compactExplicitKey)
        {
            if (state.MappingColumns.Count == 0 || state.PendingFlowCollectionColumn > LastMappingColumn(state))
            {
                YamlEvent mappingStart = YamlEvent.MappingStart(YamlCollectionStyle.Block, state.PendingFlowCollectionSpan);
                result.InsertEvent(state.PendingFlowCollectionEventIndex, mappingStart);
                state.MappingColumns.Add(state.PendingFlowCollectionColumn);
                PushBlockCollection(state, YamlEventKind.MappingStart, state.PendingFlowCollectionColumn);
            }
        }

        if (state.PendingFlowCollectionWasExplicitKey)
        {
            state.PendingExplicitCollectionKey = true;
        }

        state.HasPendingFlowCollection = false;
        state.PendingFlowCollectionWasExplicitKey = false;
        state.PendingFlowCollectionEventIndex = -1;
        state.PendingMappingValue = true;
        state.PendingNodeSpan = token.Span;
    }

    internal static void StartOrContinueBlockMapping(
        YamlParserResult result,
        YamlParserState state,
        int column,
        YamlSourceSpan span,
        bool applyPendingProperties)
    {
        EnsureDocumentOpen(result, state, span);
        CloseBlockCollectionsAboveColumn(result, state, column, span);

        if (state.MappingColumns.Count == 0 || column > LastMappingColumn(state))
        {
            state.MappingColumns.Add(column);
            PushBlockCollection(state, YamlEventKind.MappingStart, column);
            if (applyPendingProperties)
            {
                AddEventWithProperties(result, state, YamlEvent.MappingStart(YamlCollectionStyle.Block, span));
            }
            else
            {
                result.AddEvent(YamlEvent.MappingStart(YamlCollectionStyle.Block, span));
                state.DocumentHasRoot = true;
            }
        }

        state.PendingSequenceEntry = false;
    }

    internal static void PrepareExplicitMappingKey(YamlParserResult result, YamlParserState state, YamlToken token)
    {
        if (state.PendingExplicitCollectionKey)
        {
            CloseExplicitKeyCollections(result, state, token.Span);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingExplicitCollectionKey = false;
        }

        if (state.HasPendingScalar)
        {
            AddScalarFromToken(result, state, state.PendingScalar);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            ClearPendingScalar(state);
        }
        else if (state.PendingExplicitMappingKey)
        {
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingExplicitMappingKey = false;
        }

        if (state.PendingMappingValue)
        {
            if (state.MappingColumns.Count > 0 && token.Span.StartColumn > LastMappingColumn(state))
            {
                state.PendingMappingValue = false;
            }
            else
            {
                AddEmptyScalar(result, state, state.PendingNodeSpan);
                state.PendingMappingValue = false;
            }
        }

        CloseBlockSequencesAtOrAboveColumn(result, state, token.Span.StartColumn, token.Span);
        bool propertiesPrecedeKey = HasPendingNodeProperties(state)
            && PendingPropertyEndLine(state) < token.Span.StartLine;
        StartOrContinueBlockMapping(result, state, token.Span.StartColumn, token.Span, propertiesPrecedeKey);
        state.PendingExplicitMappingKey = true;
        state.PendingExplicitMappingColumn = token.Span.StartColumn;
        state.PendingMappingKeyColumn = token.Span.StartColumn;
        state.PendingNodeSpan = token.Span;
        ClearPendingScalar(state);
    }

    internal static void AcceptExplicitMappingKeyScalar(YamlParserState state, YamlToken token)
    {
        state.PendingScalar = token;
        state.HasPendingScalar = true;
        state.PendingExplicitMappingKey = false;
        state.PendingNodeSpan = token.Span;
    }

    internal static void AddPendingEmptyNode(YamlParserResult result, YamlParserState state)
    {
        if (state.PendingExplicitCollectionKey)
        {
            CloseExplicitKeyCollections(result, state, state.PendingNodeSpan);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingExplicitCollectionKey = false;
        }

        if (state.HasPendingScalar)
        {
            AddScalarFromToken(result, state, state.PendingScalar);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            ClearPendingScalar(state);
        }
        else if (state.PendingExplicitMappingKey)
        {
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingExplicitMappingKey = false;
        }

        if (state.PendingSequenceEntry && state.SequenceColumns.Count > 0)
        {
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingSequenceEntry = false;
        }

        if (state.PendingMappingValue)
        {
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingMappingValue = false;
        }
    }

    internal static void CloseBlockSequencesAtOrAboveColumn(YamlParserResult result, YamlParserState state, int column, YamlSourceSpan span)
    {
        while (LastBlockCollectionKind(state) == YamlEventKind.SequenceStart
            && column <= LastBlockCollectionColumn(state))
        {
            CloseOneBlockSequence(result, state, span);
        }
    }

    internal static void CloseExplicitKeyCollections(YamlParserResult result, YamlParserState state, YamlSourceSpan span)
    {
        while (state.BlockCollections.Count > 0)
        {
            if (LastBlockCollectionKind(state) == YamlEventKind.MappingStart
                && LastBlockCollectionColumn(state) == state.PendingExplicitMappingColumn)
            {
                return;
            }

            CloseOneBlockCollection(result, state, span);
        }
    }

    internal static void PrepareForBlockDedent(YamlParserResult result, YamlParserState state, IReadOnlyList<YamlToken> tokens, int index, YamlToken token)
    {
        int next = NextBlockTokenIndex(tokens, index + 1);
        bool explicitValueFollows = next < tokens.Count
            && tokens[next].Kind == YamlTokenKind.BlockMappingValue
            && tokens[next].Span.StartColumn == state.PendingExplicitMappingColumn;

        if (state.PendingExplicitCollectionKey && !explicitValueFollows)
        {
            CloseExplicitKeyCollections(result, state, token.Span);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingExplicitCollectionKey = false;
        }

        int targetColumn = token.Indent + 1;
        if (state.PendingExplicitMappingKey
            && HasPendingNodeProperties(state)
            && targetColumn <= state.PendingExplicitMappingColumn)
        {
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingExplicitMappingKey = false;
        }

        if (state.PendingMappingValue
            && state.MappingColumns.Count > 0
            && LastMappingColumn(state) > targetColumn)
        {
            AddEmptyScalar(result, state, state.PendingNodeSpan);
            state.PendingMappingValue = false;
        }
    }

    internal static int NextBlockTokenIndex(IReadOnlyList<YamlToken> tokens, int index)
    {
        int current = index;
        while (current < tokens.Count
            && (IsTriviaToken(tokens[current].Kind)
                || tokens[current].Kind == YamlTokenKind.Indent
                || tokens[current].Kind == YamlTokenKind.Dedent))
        {
            current = current + 1;
        }

        return current;
    }

    internal static void ClearPendingScalar(YamlParserState state)
    {
        state.HasPendingScalar = false;
        state.PendingScalar = new YamlToken();
    }

    internal static YamlScalarStyle ScalarStyleForToken(YamlTokenKind kind)
    {
        if (kind == YamlTokenKind.SingleQuotedScalar)
        {
            return YamlScalarStyle.SingleQuoted;
        }

        if (kind == YamlTokenKind.DoubleQuotedScalar)
        {
            return YamlScalarStyle.DoubleQuoted;
        }

        if (kind == YamlTokenKind.LiteralScalar)
        {
            return YamlScalarStyle.Literal;
        }

        if (kind == YamlTokenKind.FoldedScalar)
        {
            return YamlScalarStyle.Folded;
        }

        return YamlScalarStyle.Plain;
    }
}
