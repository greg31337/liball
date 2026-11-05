namespace yaml;

internal static class YamlEventRules
{
    internal static bool CanStart(YamlEventKind kind)
    {
        return kind == YamlEventKind.StreamStart;
    }

    internal static bool CanFollow(YamlEventKind previous, YamlEventKind next)
    {
        if (next == YamlEventKind.Unknown)
        {
            return false;
        }

        if (previous == YamlEventKind.Unknown)
        {
            return next == YamlEventKind.StreamStart;
        }

        if (previous == YamlEventKind.StreamEnd)
        {
            return false;
        }

        if (next == YamlEventKind.StreamStart)
        {
            return false;
        }

        if (next == YamlEventKind.Comment || next == YamlEventKind.Diagnostic)
        {
            return previous != YamlEventKind.StreamEnd;
        }

        if (IsDirectiveEvent(next))
        {
            return previous == YamlEventKind.StreamStart
                || previous == YamlEventKind.DocumentEnd
                || IsDirectiveEvent(previous)
                || previous == YamlEventKind.Comment
                || previous == YamlEventKind.Diagnostic;
        }

        if (previous == YamlEventKind.Comment || previous == YamlEventKind.Diagnostic)
        {
            return next != YamlEventKind.StreamStart;
        }

        if (previous == YamlEventKind.StreamStart)
        {
            return next == YamlEventKind.DocumentStart
                || next == YamlEventKind.StreamEnd
                || IsDirectiveEvent(next);
        }

        if (IsDirectiveEvent(previous))
        {
            return next == YamlEventKind.DocumentStart || IsDirectiveEvent(next);
        }

        if (previous == YamlEventKind.DocumentStart)
        {
            return next == YamlEventKind.DocumentEnd || IsNodeEvent(next);
        }

        if (previous == YamlEventKind.DocumentEnd)
        {
            return next == YamlEventKind.DocumentStart
                || next == YamlEventKind.StreamEnd
                || IsDirectiveEvent(next);
        }

        if (previous == YamlEventKind.SequenceStart)
        {
            return next == YamlEventKind.SequenceEnd || IsNodeEvent(next);
        }

        if (previous == YamlEventKind.MappingStart)
        {
            return next == YamlEventKind.MappingEnd || IsNodeEvent(next);
        }

        if (previous == YamlEventKind.SequenceEnd || previous == YamlEventKind.MappingEnd)
        {
            return next == YamlEventKind.SequenceEnd
                || next == YamlEventKind.MappingEnd
                || next == YamlEventKind.DocumentEnd
                || IsNodeEvent(next);
        }

        if (previous == YamlEventKind.Scalar || previous == YamlEventKind.Alias)
        {
            return next == YamlEventKind.SequenceEnd
                || next == YamlEventKind.MappingEnd
                || next == YamlEventKind.DocumentEnd
                || IsNodeEvent(next);
        }

        return false;
    }

    internal static bool IsNodeEvent(YamlEventKind kind)
    {
        return kind == YamlEventKind.Scalar
            || kind == YamlEventKind.Alias
            || kind == YamlEventKind.SequenceStart
            || kind == YamlEventKind.MappingStart;
    }

    internal static bool IsCollectionStart(YamlEventKind kind)
    {
        return kind == YamlEventKind.SequenceStart || kind == YamlEventKind.MappingStart;
    }

    internal static bool IsCollectionEnd(YamlEventKind kind)
    {
        return kind == YamlEventKind.SequenceEnd || kind == YamlEventKind.MappingEnd;
    }

    internal static bool IsStreamBoundary(YamlEventKind kind)
    {
        return kind == YamlEventKind.StreamStart || kind == YamlEventKind.StreamEnd;
    }

    internal static bool IsDocumentBoundary(YamlEventKind kind)
    {
        return kind == YamlEventKind.DocumentStart || kind == YamlEventKind.DocumentEnd;
    }

    internal static bool IsDirectiveEvent(YamlEventKind kind)
    {
        return kind == YamlEventKind.YamlVersionDirective
            || kind == YamlEventKind.TagDirective
            || kind == YamlEventKind.ReservedDirective;
    }
}
