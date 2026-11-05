using System.Collections.Generic;

namespace yaml;

internal class YamlComposerFrame
{
    internal YamlNode Node;
    internal YamlNode? PendingKey;
    internal YamlPair? ParentPair;
    internal List<YamlTrivia> PendingPairTrivia;

    internal YamlComposerFrame(YamlNode node)
    {
        Node = node == null ? YamlNode.Empty(YamlSourceSpan.Empty()) : node;
        PendingKey = null;
        ParentPair = null;
        PendingPairTrivia = new List<YamlTrivia>();
    }
}

internal class YamlComposerState
{
    internal YamlDocument? Document;
    internal List<YamlComposerFrame> Frames;
    internal List<YamlTrivia> PendingTrivia;
    internal List<YamlDirective> PendingDirectives;
    internal List<YamlDiagnostic> PendingDiagnostics;
    internal YamlNode? LastNode;
    internal YamlPair? LastPair;
    internal int LastContentLine;

    internal YamlComposerState()
    {
        Document = null;
        Frames = new List<YamlComposerFrame>();
        PendingTrivia = new List<YamlTrivia>();
        PendingDirectives = new List<YamlDirective>();
        PendingDiagnostics = new List<YamlDiagnostic>();
        LastNode = null;
        LastPair = null;
        LastContentLine = 0;
    }
}

/// <summary>
/// Builds mutable YAML document graphs from parser events.
/// </summary>
internal static class YamlComposer
{
    /// <summary>
    /// Composes parser events into an ordered, mutable YAML node graph.
    /// </summary>
    /// <param name="events">The parser events in stream order.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The resulting YAML composer result.</returns>
    internal static YamlComposerResult Compose(IReadOnlyList<YamlEvent>? events, YamlComposerOptions? options)
    {
        YamlComposerResult result = new YamlComposerResult();
        YamlComposerState state = new YamlComposerState();
        YamlComposerOptions effective = options == null ? new YamlComposerOptions() : options;

        if (events == null)
        {
            return result;
        }

        int index = 0;
        while (index < events.Count)
        {
            YamlEvent yamlEvent = events[index];
            ComposeEvent(result, state, yamlEvent, effective);
            index = index + 1;
        }

        if (state.Document != null)
        {
            FinishDocument(result, state, YamlSourceSpan.Empty(), effective);
        }

        return result;
    }

    /// <summary>
    /// Composes parser output and carries earlier diagnostics into the composer result.
    /// </summary>
    /// <param name="parserResult">The parser result whose events and diagnostics are composed.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The resulting YAML composer result.</returns>
    internal static YamlComposerResult ComposeParserResult(YamlParserResult? parserResult, YamlComposerOptions? options)
    {
        if (parserResult == null)
        {
            return Compose(null, options);
        }

        YamlComposerResult result = Compose(parserResult.Events, options);
        int index = 0;
        while (index < parserResult.Diagnostics.Count)
        {
            AddDiagnosticIfMissing(result, parserResult.Diagnostics[index]);
            index = index + 1;
        }

        if (!parserResult.Success)
        {
            result.Success = false;
        }

        return result;
    }

    internal static void ComposeEvent(YamlComposerResult result, YamlComposerState state, YamlEvent? yamlEvent, YamlComposerOptions options)
    {
        if (yamlEvent == null)
        {
            AttachDiagnostic(result, state, YamlDiagnostic.Error("YAMLC010", "The event sequence contains a null event.", YamlSourceSpan.Empty()));
            return;
        }

        if (yamlEvent.Kind == YamlEventKind.DocumentStart)
        {
            StartDocument(result, state, yamlEvent.Span, options);
        }
        else if (yamlEvent.Kind == YamlEventKind.StreamStart)
        {
            result.Stream.Span = yamlEvent.Span == null ? YamlSourceSpan.Empty() : yamlEvent.Span.Copy();
        }
        else if (yamlEvent.Kind == YamlEventKind.StreamEnd)
        {
            result.Stream.Span = YamlSourceSpan.Cover(result.Stream.Span, yamlEvent.Span);
        }
        else if (yamlEvent.Kind == YamlEventKind.DocumentEnd)
        {
            FinishDocument(result, state, yamlEvent.Span, options);
        }
        else if (YamlEventRules.IsDirectiveEvent(yamlEvent.Kind))
        {
            AttachDirective(state, yamlEvent.Directive);
        }
        else if (yamlEvent.Kind == YamlEventKind.Scalar)
        {
            AttachNode(result, state, NodeFromScalarEvent(yamlEvent), options);
        }
        else if (yamlEvent.Kind == YamlEventKind.Alias)
        {
            AttachNode(result, state, NodeFromAliasEvent(yamlEvent), options);
        }
        else if (yamlEvent.Kind == YamlEventKind.SequenceStart)
        {
            StartCollection(result, state, NodeFromSequenceEvent(yamlEvent), options);
        }
        else if (yamlEvent.Kind == YamlEventKind.MappingStart)
        {
            StartCollection(result, state, NodeFromMappingEvent(yamlEvent), options);
        }
        else if (yamlEvent.Kind == YamlEventKind.SequenceEnd)
        {
            EndSequence(result, state, yamlEvent.Span);
        }
        else if (yamlEvent.Kind == YamlEventKind.MappingEnd)
        {
            EndMapping(result, state, yamlEvent.Span, options);
        }
        else if (yamlEvent.Kind == YamlEventKind.Comment)
        {
            AttachComment(state, yamlEvent.Trivia, yamlEvent.Value, yamlEvent.Span, options);
        }
        else if (yamlEvent.Kind == YamlEventKind.Diagnostic)
        {
            AttachDiagnostic(result, state, yamlEvent.Diagnostic);
        }
        else
        {
            AttachDiagnostic(result, state, YamlDiagnostic.Error("YAMLC010", "The event sequence contains an unsupported event kind.", yamlEvent.Span));
        }
    }

    internal static void StartDocument(YamlComposerResult result, YamlComposerState state, YamlSourceSpan span, YamlComposerOptions options)
    {
        if (state.Document != null)
        {
            if (!state.Document.HasRoot() && state.Frames.Count == 0)
            {
                state.Document.Span = YamlSourceSpan.Cover(state.Document.Span, span);
                return;
            }

            FinishDocument(result, state, span, options);
        }

        state.Document = new YamlDocument();
        state.Document.Span = span == null ? YamlSourceSpan.Empty() : span;
        AttachPendingDocumentData(state);
        state.Frames.Clear();
        ClearPendingTrivia(state);
        ClearLastContent(state);
    }

    internal static void FinishDocument(YamlComposerResult result, YamlComposerState state, YamlSourceSpan span, YamlComposerOptions options)
    {
        EnsureDocument(state, span);
        AttachPendingTriviaToTail(state);
        while (state.Frames.Count > 0)
        {
            YamlComposerFrame frame = LastFrame(state);
            if (frame.Node.Kind == YamlNodeKind.Mapping)
            {
                FinishPendingMappingValue(result, state, frame, span, options);
            }

            CompleteCollectionFrame(state, frame, span);
            state.Frames.RemoveAt(state.Frames.Count - 1);
        }

        if (state.Document != null && !state.Document.HasRoot())
        {
            state.Document.SetRoot(YamlNode.Empty(span));
        }

        if (state.Document != null)
        {
            RefreshDocumentSpan(state.Document, span);
            result.Stream.AddDocument(state.Document);
        }

        state.Document = null;
        state.Frames.Clear();
        ClearPendingTrivia(state);
        ClearLastContent(state);
    }

    internal static void StartCollection(YamlComposerResult result, YamlComposerState state, YamlNode node, YamlComposerOptions options)
    {
        AttachNode(result, state, node, options);
        YamlComposerFrame frame = new YamlComposerFrame(node);
        frame.ParentPair = state.LastPair;
        state.Frames.Add(frame);
    }

    internal static void EndSequence(YamlComposerResult result, YamlComposerState state, YamlSourceSpan span)
    {
        if (state.Frames.Count == 0)
        {
            result.AddDiagnostic(YamlDiagnostic.Error("YAMLC001", "Sequence end appeared without a matching sequence start.", span));
            return;
        }

        YamlComposerFrame frame = LastFrame(state);
        if (frame.Node.Kind != YamlNodeKind.Sequence)
        {
            result.AddDiagnostic(YamlDiagnostic.Error("YAMLC002", "Sequence end appeared while composing another collection type.", span));
            return;
        }

        CompleteCollectionFrame(state, frame, span);
        state.Frames.RemoveAt(state.Frames.Count - 1);
    }

    internal static void EndMapping(YamlComposerResult result, YamlComposerState state, YamlSourceSpan span, YamlComposerOptions options)
    {
        if (state.Frames.Count == 0)
        {
            result.AddDiagnostic(YamlDiagnostic.Error("YAMLC003", "Mapping end appeared without a matching mapping start.", span));
            return;
        }

        YamlComposerFrame frame = LastFrame(state);
        if (frame.Node.Kind != YamlNodeKind.Mapping)
        {
            result.AddDiagnostic(YamlDiagnostic.Error("YAMLC004", "Mapping end appeared while composing another collection type.", span));
            return;
        }

        FinishPendingMappingValue(result, state, frame, span, options);
        CompleteCollectionFrame(state, frame, span);
        state.Frames.RemoveAt(state.Frames.Count - 1);
    }

    internal static void AttachNode(YamlComposerResult result, YamlComposerState state, YamlNode node, YamlComposerOptions options)
    {
        EnsureDocument(state, node == null ? YamlSourceSpan.Empty() : node.Span);
        if (node == null)
        {
            return;
        }

        RegisterAnchor(result, state, node);
        ResolveAlias(result, state, node, options);

        if (state.Frames.Count == 0)
        {
            AttachDocumentRoot(result, state, node);
            return;
        }

        YamlComposerFrame frame = LastFrame(state);
        if (frame.Node.Kind == YamlNodeKind.Sequence)
        {
            AttachPendingTriviaToNode(state, node);
            frame.Node.AddChild(node);
            RememberLastNode(state, node);
            state.LastPair = null;
        }
        else if (frame.Node.Kind == YamlNodeKind.Mapping)
        {
            AttachMappingNode(result, state, frame, node, options);
        }
    }

    internal static void AttachDocumentRoot(YamlComposerResult result, YamlComposerState state, YamlNode node)
    {
        if (state.Document == null)
        {
            return;
        }

        if (!state.Document.HasRoot())
        {
            AttachPendingTriviaToNode(state, node);
            state.Document.SetRoot(node);
            RememberLastNode(state, node);
            return;
        }

        result.AddDiagnostic(YamlDiagnostic.Error("YAMLC005", "Document contains more than one root node.", node.Span));
    }

    internal static void RegisterAnchor(YamlComposerResult result, YamlComposerState state, YamlNode node)
    {
        if (state.Document == null || node.Anchor == "")
        {
            return;
        }

        state.Document.Anchors.Register(node.Anchor, node, node.Span);
    }

    internal static void ResolveAlias(YamlComposerResult result, YamlComposerState state, YamlNode node, YamlComposerOptions options)
    {
        if (node.Kind != YamlNodeKind.Alias)
        {
            return;
        }

        if (options != null && !options.ResolveAliases)
        {
            return;
        }

        if (state.Document == null || node.AliasName == "")
        {
            AddDocumentDiagnostic(result, state, YamlDiagnostic.Error("YAMLC007", "Alias does not name an anchor.", node.Span));
            return;
        }

        YamlNode? target = state.Document.Anchors.Find(node.AliasName);
        if (target == null)
        {
            AddDocumentDiagnostic(result, state, YamlDiagnostic.Error("YAMLC007", "Alias '" + node.AliasName + "' references an unknown anchor.", node.Span));
            return;
        }

        if (IsRecursiveAliasTarget(state, target) && options != null && !options.AllowRecursiveAliases)
        {
            AddDocumentDiagnostic(result, state, YamlDiagnostic.Error("YAMLC008", "Alias '" + node.AliasName + "' creates a recursive reference.", node.Span));
            return;
        }

        node.AliasTarget = target;
    }

    internal static bool IsRecursiveAliasTarget(YamlComposerState state, YamlNode target)
    {
        int index = 0;
        while (index < state.Frames.Count)
        {
            if (state.Frames[index].Node == target)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void AddDocumentDiagnostic(YamlComposerResult result, YamlComposerState state, YamlDiagnostic diagnostic)
    {
        result.AddDiagnostic(diagnostic);
        if (state.Document != null)
        {
            state.Document.AddDiagnostic(diagnostic);
        }
    }

    internal static void AttachMappingNode(YamlComposerResult result, YamlComposerState state, YamlComposerFrame frame, YamlNode node, YamlComposerOptions options)
    {
        if (frame.PendingKey == null)
        {
            MovePendingTriviaToPair(frame, state);
            AttachPendingTriviaToNode(state, node);
            frame.PendingKey = node;
            RememberLastNode(state, node);
            state.LastPair = null;
            return;
        }

        AttachPendingTriviaToNode(state, node);
        AddMappingPair(result, state, frame, CreatePair(frame.PendingKey, node), options);
        frame.PendingKey = null;
        RememberLastNode(state, node);
    }

    internal static void FinishPendingMappingValue(YamlComposerResult result, YamlComposerState state, YamlComposerFrame frame, YamlSourceSpan span, YamlComposerOptions options)
    {
        if (frame.PendingKey == null)
        {
            return;
        }

        YamlNode empty = YamlNode.Empty(span);
        AttachPendingTriviaToNode(state, empty);
        AddMappingPair(result, state, frame, CreatePair(frame.PendingKey, empty), options);
        frame.PendingKey = null;
        RememberLastNode(state, empty);
    }

    internal static YamlPair CreatePair(YamlNode key, YamlNode value)
    {
        YamlPair pair = YamlPair.Create(key, value, YamlSourceSpan.Cover(key.Span, value.Span));
        pair.RefreshComplexKey();
        return pair;
    }

    internal static void CompleteCollectionFrame(YamlComposerState state, YamlComposerFrame frame, YamlSourceSpan span)
    {
        frame.Node.Span = YamlSourceSpan.Cover(frame.Node.Span, span);
        if (frame.ParentPair != null)
        {
            frame.ParentPair.RefreshSpanFromNodes();
            state.LastPair = frame.ParentPair;
        }
        else
        {
            state.LastPair = null;
        }

        RememberLastNode(state, frame.Node);
    }

    internal static void RefreshDocumentSpan(YamlDocument document, YamlSourceSpan span)
    {
        document.Span = YamlSourceSpan.Cover(document.Span, span);
        if (document.Root != null)
        {
            document.Span = YamlSourceSpan.Cover(document.Span, document.Root.Span);
        }
    }

    internal static void AddMappingPair(YamlComposerResult result, YamlComposerState state, YamlComposerFrame frame, YamlPair pair, YamlComposerOptions options)
    {
        int duplicateIndex = FindDuplicateKeyIndex(frame.Node, pair.Key);
        YamlDuplicateKeyPolicy policy = DuplicateKeyPolicy(options);

        if (duplicateIndex < 0 || policy == YamlDuplicateKeyPolicy.PreserveAll)
        {
            AttachPendingPairTrivia(frame, pair);
            frame.Node.AddPair(pair);
            state.LastPair = pair;
            return;
        }

        if (policy == YamlDuplicateKeyPolicy.Error)
        {
            AddDuplicateKeyDiagnostic(result, state, pair, YamlDiagnosticSeverity.Error);
            AttachPendingPairTrivia(frame, pair);
            frame.Node.AddPair(pair);
            state.LastPair = pair;
        }
        else if (policy == YamlDuplicateKeyPolicy.Warn)
        {
            AddDuplicateKeyDiagnostic(result, state, pair, YamlDiagnosticSeverity.Warning);
            AttachPendingPairTrivia(frame, pair);
            frame.Node.AddPair(pair);
            state.LastPair = pair;
        }
        else if (policy == YamlDuplicateKeyPolicy.LastWins)
        {
            AttachPendingPairTrivia(frame, pair);
            frame.Node.Pairs[duplicateIndex] = pair;
            state.LastPair = pair;
        }
    }

    internal static YamlDuplicateKeyPolicy DuplicateKeyPolicy(YamlComposerOptions options)
    {
        if (options == null)
        {
            return YamlDuplicateKeyPolicy.Error;
        }

        return options.DuplicateKeyPolicy;
    }

    internal static void AddDuplicateKeyDiagnostic(YamlComposerResult result, YamlComposerState state, YamlPair pair, YamlDiagnosticSeverity severity)
    {
        YamlSourceSpan span = pair.Key == null ? pair.Span : pair.Key.Span;
        YamlDiagnostic diagnostic = YamlDiagnostic.Create(severity, "YAMLC009", "Duplicate mapping key was found.", span);
        AddDocumentDiagnostic(result, state, diagnostic);
    }

    internal static int FindDuplicateKeyIndex(YamlNode mapping, YamlNode? key)
    {
        int index = 0;
        while (index < mapping.Pairs.Count)
        {
            if (KeysEqual(mapping.Pairs[index].Key, key))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool KeysEqual(YamlNode? left, YamlNode? right)
    {
        if (!CanCompareKey(left) || !CanCompareKey(right))
        {
            return false;
        }

        if (left == null || right == null)
        {
            return false;
        }

        if (left.Kind != right.Kind)
        {
            return false;
        }

        if (left.Tag != right.Tag)
        {
            return false;
        }

        if (left.Kind == YamlNodeKind.Scalar)
        {
            return left.Value == right.Value;
        }

        if (left.Kind == YamlNodeKind.Alias)
        {
            return left.AliasName == right.AliasName;
        }

        return left.Kind == YamlNodeKind.Empty;
    }

    internal static bool CanCompareKey(YamlNode? key)
    {
        if (key == null)
        {
            return false;
        }

        return key.Kind == YamlNodeKind.Scalar
            || key.Kind == YamlNodeKind.Alias;
    }

    internal static void EnsureDocument(YamlComposerState state, YamlSourceSpan span)
    {
        if (state.Document != null)
        {
            return;
        }

        state.Document = new YamlDocument();
        state.Document.Span = span == null ? YamlSourceSpan.Empty() : span;
        AttachPendingDocumentData(state);
    }

    internal static void AttachDirective(YamlComposerState state, YamlDirective directive)
    {
        if (directive == null)
        {
            return;
        }

        if (state.Document == null)
        {
            state.PendingDirectives.Add(directive.Copy());
            return;
        }

        state.Document.AddDirective(directive.Copy());
        state.Document.Span = YamlSourceSpan.Cover(state.Document.Span, directive.Span);
    }

    internal static void AttachPendingDocumentData(YamlComposerState state)
    {
        if (state.Document == null)
        {
            return;
        }

        int index = 0;
        while (index < state.PendingDirectives.Count)
        {
            YamlDirective directive = state.PendingDirectives[index];
            state.Document.AddDirective(directive);
            state.Document.Span = YamlSourceSpan.Cover(state.Document.Span, directive.Span);
            index = index + 1;
        }

        index = 0;
        while (index < state.PendingDiagnostics.Count)
        {
            state.Document.AddDiagnostic(state.PendingDiagnostics[index]);
            index = index + 1;
        }

        state.PendingDirectives.Clear();
        state.PendingDiagnostics.Clear();
    }

    internal static YamlComposerFrame LastFrame(YamlComposerState state)
    {
        return state.Frames[state.Frames.Count - 1];
    }

    internal static YamlNode NodeFromScalarEvent(YamlEvent yamlEvent)
    {
        YamlNode node;
        if (yamlEvent.ImplicitEmpty)
        {
            node = YamlNode.Empty(yamlEvent.Span);
        }
        else
        {
            node = YamlNode.Scalar(yamlEvent.Value, yamlEvent.ScalarStyle, yamlEvent.Span);
        }

        CopyProperties(yamlEvent, node);
        return node;
    }

    internal static YamlNode NodeFromAliasEvent(YamlEvent yamlEvent)
    {
        YamlNode node = YamlNode.Alias(yamlEvent.AliasName, yamlEvent.Span);
        CopyProperties(yamlEvent, node);
        return node;
    }

    internal static YamlNode NodeFromSequenceEvent(YamlEvent yamlEvent)
    {
        YamlNode node = YamlNode.Sequence(yamlEvent.CollectionStyle, yamlEvent.Span);
        CopyProperties(yamlEvent, node);
        return node;
    }

    internal static YamlNode NodeFromMappingEvent(YamlEvent yamlEvent)
    {
        YamlNode node = YamlNode.Mapping(yamlEvent.CollectionStyle, yamlEvent.Span);
        CopyProperties(yamlEvent, node);
        return node;
    }

    internal static void CopyProperties(YamlEvent yamlEvent, YamlNode node)
    {
        node.Tag = yamlEvent.Tag == null ? "" : yamlEvent.Tag;
        node.TagPresentation = yamlEvent.TagPresentation;
        node.ParsedTag = yamlEvent.ParsedTag;
        node.Anchor = yamlEvent.Anchor == null ? "" : yamlEvent.Anchor;
    }

    internal static void AttachComment(YamlComposerState state, YamlTrivia eventTrivia, string comment, YamlSourceSpan span, YamlComposerOptions options)
    {
        if (options != null && !options.PreserveComments)
        {
            return;
        }

        EnsureDocument(state, YamlSourceSpan.Empty());
        YamlTrivia trivia = eventTrivia == null || eventTrivia.Kind == YamlTriviaKind.Unknown
            ? YamlTrivia.Create(comment == null || comment == "" ? YamlTriviaKind.BlankLine : YamlTriviaKind.Comment, YamlTriviaPlacement.Before, comment, span)
            : eventTrivia.Copy();

        if (trivia.Kind == YamlTriviaKind.Comment
            && trivia.Placement == YamlTriviaPlacement.Before
            && state.LastNode != null
            && state.LastContentLine > 0
            && span != null
            && span.StartLine == state.LastContentLine)
        {
            trivia.Placement = YamlTriviaPlacement.Inline;
        }

        if ((trivia.Placement == YamlTriviaPlacement.Inline || trivia.Placement == YamlTriviaPlacement.Following)
            && state.LastNode != null)
        {
            state.LastNode.AddTrivia(trivia);
            if (state.LastPair != null)
            {
                state.LastPair.AddTrivia(trivia);
            }

            return;
        }

        if (state.Document != null && !state.Document.HasRoot() && state.Frames.Count == 0)
        {
            state.Document.AddTrivia(trivia);
            return;
        }

        state.PendingTrivia.Add(trivia);
    }

    internal static void AttachPendingTriviaToNode(YamlComposerState state, YamlNode node)
    {
        int index = 0;
        while (index < state.PendingTrivia.Count)
        {
            YamlTrivia trivia = state.PendingTrivia[index].Copy();
            trivia.Placement = YamlTriviaPlacement.Before;
            node.AddTrivia(trivia);
            index = index + 1;
        }

        ClearPendingTrivia(state);
    }

    internal static void AttachPendingTriviaToTail(YamlComposerState state)
    {
        if (state.LastNode == null)
        {
            return;
        }

        int index = 0;
        while (index < state.PendingTrivia.Count)
        {
            YamlTrivia trivia = state.PendingTrivia[index].Copy();
            trivia.Placement = YamlTriviaPlacement.Following;
            state.LastNode.AddTrivia(trivia);
            if (state.LastPair != null)
            {
                state.LastPair.AddTrivia(trivia);
            }

            index = index + 1;
        }

        ClearPendingTrivia(state);
    }

    internal static void MovePendingTriviaToPair(YamlComposerFrame frame, YamlComposerState state)
    {
        int index = 0;
        while (index < state.PendingTrivia.Count)
        {
            YamlTrivia trivia = state.PendingTrivia[index].Copy();
            trivia.Placement = YamlTriviaPlacement.Before;
            frame.PendingPairTrivia.Add(trivia);
            index = index + 1;
        }
    }

    internal static void AttachPendingPairTrivia(YamlComposerFrame frame, YamlPair pair)
    {
        int index = 0;
        while (index < frame.PendingPairTrivia.Count)
        {
            pair.AddTrivia(frame.PendingPairTrivia[index]);
            index = index + 1;
        }

        frame.PendingPairTrivia.Clear();
    }

    internal static void RememberLastNode(YamlComposerState state, YamlNode node)
    {
        state.LastNode = node;
        state.LastContentLine = node.Span == null ? 0 : node.Span.EndLine;
    }

    internal static void ClearPendingTrivia(YamlComposerState state)
    {
        state.PendingTrivia.Clear();
    }

    internal static void ClearLastContent(YamlComposerState state)
    {
        state.LastNode = null;
        state.LastPair = null;
        state.LastContentLine = 0;
    }

    internal static void AttachDiagnostic(YamlComposerResult result, YamlComposerState state, YamlDiagnostic diagnostic)
    {
        result.AddDiagnostic(diagnostic);
        if (state.Document != null)
        {
            state.Document.AddDiagnostic(diagnostic);
        }
        else
        {
            state.PendingDiagnostics.Add(diagnostic);
        }
    }

    internal static void AddDiagnosticIfMissing(YamlComposerResult result, YamlDiagnostic diagnostic)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            if (result.Diagnostics[index] == diagnostic)
            {
                return;
            }

            index = index + 1;
        }

        result.AddDiagnostic(diagnostic);
    }
}
