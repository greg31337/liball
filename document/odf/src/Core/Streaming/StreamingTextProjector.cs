using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace odf;

internal class StreamingTextFrame
{
    internal bool Included;
    internal bool Suppressed;
    internal bool IsBlock;
}

internal class StreamingTextState
{
    internal StringBuilder Text = new StringBuilder();
    internal List<StreamingTextFrame> Frames =
        new List<StreamingTextFrame>();
    internal bool UsesExplicitContainers;
    internal bool HasSeenContainer;
    internal bool HasSeenBlock;
    internal int TrailingBlockBoundaries;
    internal int MaximumRepeatCount;
}

internal static class StreamingTextProjector
{
    internal static string ProjectXmlText(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        ValidateXmlProjectionInputs(input, partPath, options);
        OdfLoadOptions readerOptions = CreateTextXmlOptions(options);
        XmlEventReader reader = new XmlEventReader(
            input,
            partPath,
            readerOptions);
        return ReadProjectedText(
            reader,
            HasExplicitContainers(readerOptions.StreamSelection),
            readerOptions.SecurityLimits.MaximumXmlTextLength);
    }

    internal static string ProjectPackageText(
        Stream input,
        OdfLoadOptions options)
    {
        ValidatePackageProjectionInputs(input, options);
        OdfLoadOptions packageOptions = CreateTextPackageOptions(options);
        PackageEventReader reader = new PackageEventReader(
            input,
            packageOptions);
        StringBuilder result = new StringBuilder();
        OdfStreamEvent streamEvent = reader.ReadNextEvent();
        while (streamEvent.Kind != OdfStreamEventKind.EndOfStream)
        {
            if (streamEvent.Kind == OdfStreamEventKind.PackageEntryStart)
            {
                if (IsXmlTextPart(streamEvent.PartPath))
                {
                    byte[] content = reader.ReadCurrentEntryBytes();
                    AppendProjectedPackagePart(
                        result,
                        content,
                        streamEvent.PartPath,
                        options);
                }
                else
                {
                    reader.SkipCurrentEntry();
                }
            }
            streamEvent = reader.ReadNextEvent();
        }
        return result.ToString();
    }

    static string ReadProjectedText(
        XmlEventReader reader,
        bool usesExplicitContainers,
        int maximumRepeatCount)
    {
        StreamingTextState state = new StreamingTextState();
        state.UsesExplicitContainers = usesExplicitContainers;
        state.MaximumRepeatCount = maximumRepeatCount;
        OdfStreamEvent streamEvent = reader.ReadNextEvent();
        while (streamEvent.Kind != OdfStreamEventKind.EndOfStream)
        {
            if (streamEvent.Kind == OdfStreamEventKind.ElementStart)
            {
                StartTextElement(state, streamEvent);
            }
            else if (streamEvent.Kind == OdfStreamEventKind.ElementEnd)
            {
                FinishTextElement(state);
            }
            else if (streamEvent.Kind == OdfStreamEventKind.Text)
            {
                AppendTextValue(state, streamEvent.Value);
            }
            streamEvent = reader.ReadNextEvent();
        }
        RemoveTrailingBlockBoundaries(state);
        return state.Text.ToString();
    }

    static void StartTextElement(
        StreamingTextState state,
        OdfStreamEvent streamEvent)
    {
        StreamingTextFrame frame = new StreamingTextFrame();
        bool parentIncluded = false;
        bool parentSuppressed = false;
        if (state.Frames.Count > 0)
        {
            StreamingTextFrame parent =
                state.Frames[state.Frames.Count - 1];
            parentIncluded = parent.Included;
            parentSuppressed = parent.Suppressed;
        }
        bool startsContainer = false;
        bool explicitlySelectedRoot = state.UsesExplicitContainers
            && state.Frames.Count == 0;
        if (explicitlySelectedRoot)
        {
            startsContainer = true;
        }
        else if (!state.UsesExplicitContainers
            && IsDefaultTextContainer(streamEvent.Name))
        {
            startsContainer = true;
        }
        frame.Included = parentIncluded || startsContainer;
        frame.Suppressed = parentSuppressed
            || (!explicitlySelectedRoot
                && IsSuppressedTextElement(streamEvent.Name));
        frame.IsBlock = frame.Included && !frame.Suppressed
            && IsTextBlock(streamEvent.Name);
        if (startsContainer && !parentIncluded)
        {
            StartProjectionContainer(state);
        }
        state.Frames.Add(frame);
        if (!frame.Included || frame.Suppressed)
        {
            return;
        }
        if (frame.IsBlock)
        {
            StartTextBlock(state);
        }
        if (CanAppendInlineText(state))
        {
            AppendSpecialTextElement(state, streamEvent);
        }
    }

    static void FinishTextElement(StreamingTextState state)
    {
        if (state.Frames.Count == 0)
        {
            throw new OdfXmlException(
                "The text event stream contains an unmatched closing element.");
        }
        int index = state.Frames.Count - 1;
        StreamingTextFrame frame = state.Frames[index];
        if (frame.IsBlock)
        {
            state.Text.Append('\n');
            state.TrailingBlockBoundaries =
                state.TrailingBlockBoundaries + 1;
        }
        state.Frames.RemoveAt(index);
    }

    static void StartTextBlock(StreamingTextState state)
    {
        if (state.HasSeenBlock)
        {
            CommitTrailingBlockBoundaries(state);
            if (state.Text.Length > 0
                && state.Text[state.Text.Length - 1] != '\n')
            {
                state.Text.Append('\n');
            }
        }
        state.HasSeenBlock = true;
    }

    static void StartProjectionContainer(StreamingTextState state)
    {
        if (state.HasSeenContainer)
        {
            CommitTrailingBlockBoundaries(state);
            if (state.Text.Length > 0
                && state.Text[state.Text.Length - 1] != '\n')
            {
                state.Text.Append('\n');
            }
        }
        state.HasSeenContainer = true;
    }

    static void AppendTextValue(
        StreamingTextState state,
        string value)
    {
        if (!IsCurrentTextIncluded(state))
        {
            return;
        }
        if (!IsWithinTextBlock(state) && IsWhitespaceValue(value))
        {
            return;
        }
        AppendProjectedValue(state, value);
    }

    static void AppendSpecialTextElement(
        StreamingTextState state,
        OdfStreamEvent streamEvent)
    {
        if (IsNamed(streamEvent.Name, OdfNamespaces.Text, "s"))
        {
            int count = ReadSpaceCount(
                streamEvent.Attributes,
                state.MaximumRepeatCount);
            AppendRepeatedCharacter(state, ' ', count);
        }
        else if (IsNamed(streamEvent.Name, OdfNamespaces.Text, "tab"))
        {
            AppendProjectedValue(state, "\t");
        }
        else if (IsNamed(
            streamEvent.Name,
            OdfNamespaces.Text,
            "line-break"))
        {
            AppendProjectedValue(state, "\n");
        }
    }

    static bool CanAppendInlineText(StreamingTextState state)
    {
        return state.UsesExplicitContainers || IsWithinTextBlock(state);
    }

    static bool IsWithinTextBlock(StreamingTextState state)
    {
        int index = state.Frames.Count - 1;
        while (index >= 0)
        {
            if (state.Frames[index].IsBlock)
            {
                return true;
            }
            index = index - 1;
        }
        return false;
    }

    static bool IsCurrentTextIncluded(StreamingTextState state)
    {
        if (state.Frames.Count == 0)
        {
            return false;
        }
        StreamingTextFrame frame = state.Frames[state.Frames.Count - 1];
        return frame.Included && !frame.Suppressed;
    }

    static void AppendProjectedValue(
        StreamingTextState state,
        string value)
    {
        if (value.Length == 0)
        {
            return;
        }
        CommitTrailingBlockBoundaries(state);
        state.Text.Append(value);
    }

    static void AppendRepeatedCharacter(
        StreamingTextState state,
        char value,
        int count)
    {
        if (count <= 0)
        {
            return;
        }
        CommitTrailingBlockBoundaries(state);
        int index = 0;
        while (index < count)
        {
            state.Text.Append(value);
            index = index + 1;
        }
    }

    static void CommitTrailingBlockBoundaries(StreamingTextState state)
    {
        state.TrailingBlockBoundaries = 0;
    }

    static void RemoveTrailingBlockBoundaries(StreamingTextState state)
    {
        int count = state.TrailingBlockBoundaries;
        if (count > state.Text.Length)
        {
            count = state.Text.Length;
        }
        state.Text.Length = state.Text.Length - count;
        state.TrailingBlockBoundaries = 0;
    }

    static int ReadSpaceCount(
        OdfAttribute[] attributes,
        int maximumRepeatCount)
    {
        string value = AttributeValue(
            attributes,
            OdfNamespaces.Text,
            "c");
        if (value.Length == 0)
        {
            return 1;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(value);
        if (!parsed.IsValid || parsed.IntegerValue < 0)
        {
            return 1;
        }
        if (parsed.IntegerValue > maximumRepeatCount)
        {
            return maximumRepeatCount;
        }
        return (int)parsed.IntegerValue;
    }

    static string AttributeValue(
        OdfAttribute[] attributes,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            OdfAttribute attribute = attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static bool IsDefaultTextContainer(OdfQualifiedName? name)
    {
        if (IsNamed(name, OdfNamespaces.Office, "text")
            || IsNamed(name, OdfNamespaces.Office, "annotation")
            || IsNamed(name, OdfNamespaces.Text, "note-body")
            || IsNamed(name, OdfNamespaces.Presentation, "notes")
            || IsNamed(name, OdfNamespaces.Presentation, "header-decl")
            || IsNamed(name, OdfNamespaces.Presentation, "footer-decl"))
        {
            return true;
        }
        if (name == null || name.NamespaceName != OdfNamespaces.Style)
        {
            return false;
        }
        return name.LocalName == "header"
            || name.LocalName == "header-left"
            || name.LocalName == "header-first"
            || name.LocalName == "footer"
            || name.LocalName == "footer-left"
            || name.LocalName == "footer-first";
    }

    static bool IsSuppressedTextElement(OdfQualifiedName? name)
    {
        if (IsNamed(name, OdfNamespaces.Text, "ruby-text")
            || IsNamed(name, OdfNamespaces.Text, "soft-page-break")
            || IsNamed(name, OdfNamespaces.DublinCore, "creator")
            || IsNamed(name, OdfNamespaces.DublinCore, "date")
            || IsNamed(name, OdfNamespaces.Metadata, "date-string")
            || IsNamed(name, OdfNamespaces.Text, "sender-initials"))
        {
            return true;
        }
        return false;
    }

    static bool IsTextBlock(OdfQualifiedName? name)
    {
        return IsNamed(name, OdfNamespaces.Text, "p")
            || IsNamed(name, OdfNamespaces.Text, "h");
    }

    static bool IsNamed(
        OdfQualifiedName? name,
        string namespaceName,
        string localName)
    {
        return name != null
            && name.NamespaceName == namespaceName
            && name.LocalName == localName;
    }

    static bool IsWhitespaceValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!char.IsWhiteSpace(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    static bool HasExplicitContainers(OdfStreamSelection selection)
    {
        return selection.ElementNames != null
            && selection.ElementNames.Length > 0;
    }

    static bool IsXmlTextPart(string partPath)
    {
        return partPath.EndsWith(
            ".xml",
            StringComparison.OrdinalIgnoreCase);
    }

    static void AppendProjectedPackagePart(
        StringBuilder result,
        byte[] content,
        string partPath,
        OdfLoadOptions options)
    {
        OdfLoadOptions xmlOptions = CreateTextXmlOptions(options);
        xmlOptions.LeaveInputOpen = true;
        string projected;
        using (MemoryStream input = new MemoryStream(content, false))
        {
            projected = ProjectXmlText(input, partPath, xmlOptions);
        }
        if (projected.Length == 0)
        {
            return;
        }
        if (result.Length > 0)
        {
            result.Append('\n');
        }
        result.Append(projected);
    }

    static OdfLoadOptions CreateTextXmlOptions(OdfLoadOptions source)
    {
        OdfLoadOptions result = CopyTextBaseOptions(source);
        result.StreamSelection = CopyTextSelection(source.StreamSelection);
        result.StreamSelection.IncludeElementEvents = true;
        result.StreamSelection.IncludeTextEvents = true;
        result.StreamSelection.IncludeCommentEvents = false;
        result.StreamSelection.IncludeProcessingInstructionEvents = false;
        return result;
    }

    static OdfLoadOptions CreateTextPackageOptions(OdfLoadOptions source)
    {
        OdfLoadOptions result = CopyTextBaseOptions(source);
        result.StreamSelection = CopyTextSelection(source.StreamSelection);
        result.StreamSelection.IncludePackageEntryEvents = true;
        result.StreamSelection.IncludeBinaryContent = false;
        return result;
    }

    static OdfLoadOptions CopyTextBaseOptions(OdfLoadOptions source)
    {
        OdfLoadOptions result = new OdfLoadOptions();
        result.SecurityLimits = source.SecurityLimits;
        result.Cancellation = source.Cancellation;
        result.LeaveInputOpen = source.LeaveInputOpen;
        result.PreserveLexicalValues = source.PreserveLexicalValues;
        result.PreserveUnknownContent = source.PreserveUnknownContent;
        result.MaterializeXmlTrees = source.MaterializeXmlTrees;
        result.IndexPackageEntries = source.IndexPackageEntries;
        result.ValidateOnLoad = source.ValidateOnLoad;
        result.AllowRecovery = source.AllowRecovery;
        return result;
    }

    static OdfStreamSelection CopyTextSelection(OdfStreamSelection source)
    {
        OdfStreamSelection result = new OdfStreamSelection();
        result.PartPaths = CopyTextPartPaths(source.PartPaths);
        result.ElementNames = CopyTextElementNames(source.ElementNames);
        result.IncludePackageEntryEvents = source.IncludePackageEntryEvents;
        result.IncludeElementEvents = source.IncludeElementEvents;
        result.IncludeTextEvents = source.IncludeTextEvents;
        result.IncludeCommentEvents = source.IncludeCommentEvents;
        result.IncludeProcessingInstructionEvents =
            source.IncludeProcessingInstructionEvents;
        result.IncludeBinaryContent = source.IncludeBinaryContent;
        result.SkipUnselectedSubtrees = source.SkipUnselectedSubtrees;
        return result;
    }

    static string[] CopyTextPartPaths(string[] source)
    {
        if (source == null)
        {
            return [];
        }
        string[] result = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }

    static OdfQualifiedName[] CopyTextElementNames(
        OdfQualifiedName[] source)
    {
        if (source == null)
        {
            return [];
        }
        List<OdfQualifiedName> result = new List<OdfQualifiedName>();
        int index = 0;
        while (index < source.Length)
        {
            OdfQualifiedName? sourceName = source[index];
            if (sourceName != null)
            {
                OdfQualifiedName name = new OdfQualifiedName();
                name.NamespaceName = sourceName.NamespaceName;
                name.LocalName = sourceName.LocalName;
                name.Prefix = sourceName.Prefix;
                result.Add(name);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ValidateXmlProjectionInputs(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        if (input == null || !input.CanRead || partPath == null
            || options == null || options.StreamSelection == null
            || options.SecurityLimits == null
            || options.Cancellation == null)
        {
            throw new OdfDocumentException(
                "A readable XML stream, part path, and load options are required for text projection.");
        }
    }

    static void ValidatePackageProjectionInputs(
        Stream input,
        OdfLoadOptions options)
    {
        if (input == null || !input.CanRead || options == null
            || options.StreamSelection == null
            || options.SecurityLimits == null
            || options.Cancellation == null)
        {
            throw new OdfDocumentException(
                "A readable package stream and load options are required for text projection.");
        }
    }
}
