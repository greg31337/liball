using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace odf;

internal class XmlEventReader
{
    Stream Input;
    OdfLoadOptions Options;
    OdfStreamSelection Selection;
    OdfSecurityLimits Limits;
    XmlReader Reader;
    List<StreamNamespaceFrame> NamespaceFrames =
        new List<StreamNamespaceFrame>();
    OdfStreamEvent? PendingEvent;
    OdfStreamEvent? CurrentStartEvent;
    OdfStreamEvent? CurrentSyntheticEnd;
    bool ReaderHasUnreadPosition;
    bool PopBeforeRead;
    bool PopAfterPending;
    bool Finished;
    bool InputClosed;
    bool CanSkipCurrent;
    bool CurrentStartIsEmpty;
    int SelectedRootDepth = -1;
    int PendingPopDepth = -1;

    internal XmlEventReader(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        if (input == null || !input.CanRead)
        {
            throw new OdfDocumentException(
                "A readable XML input stream is required.");
        }
        if (partPath == null || options == null
            || options.StreamSelection == null
            || options.SecurityLimits == null
            || options.Cancellation == null)
        {
            throw new OdfDocumentException(
                "XML streaming options and a part path are required.");
        }
        ValidateXmlLimits(options.SecurityLimits);
        Input = input;
        Options = options;
        Selection = options.StreamSelection;
        Limits = options.SecurityLimits;
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.Async = false;
        settings.CloseInput = false;
        settings.ConformanceLevel = ConformanceLevel.Document;
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.IgnoreComments = false;
        settings.IgnoreProcessingInstructions = false;
        settings.IgnoreWhitespace = false;
        settings.XmlResolver = null;
        Reader = XmlReader.Create(input, settings, partPath);
        OdfNamespaceDeclaration xmlNamespace =
            new OdfNamespaceDeclaration();
        xmlNamespace.Prefix = OdfNamespaces.XmlPrefix;
        xmlNamespace.NamespaceName = OdfNamespaces.Xml;
        xmlNamespace.Location.PartPath = partPath;
        PushNamespaceFrame(-1, [xmlNamespace]);
    }

    internal OdfStreamEvent ReadNextEvent()
    {
        CheckXmlCancellation();
        CanSkipCurrent = false;
        CurrentStartIsEmpty = false;
        CurrentStartEvent = null;
        CurrentSyntheticEnd = null;
        if (Finished)
        {
            return CreateXmlEndOfStream();
        }
        if (PendingEvent != null)
        {
            OdfStreamEvent pending = PendingEvent;
            PendingEvent = null;
            if (PopAfterPending)
            {
                PopBeforeRead = true;
                PopAfterPending = false;
            }
            return pending;
        }
        if (PopBeforeRead)
        {
            PopNamespaceFrames(PendingPopDepth);
            PendingPopDepth = -1;
            PopBeforeRead = false;
        }
        while (!Finished)
        {
            bool available;
            if (ReaderHasUnreadPosition)
            {
                ReaderHasUnreadPosition = false;
                available = !Reader.EOF;
            }
            else
            {
                available = Reader.Read();
            }
            CheckXmlCancellation();
            if (!available)
            {
                return FinishXmlInput();
            }
            if (Reader.NodeType == XmlNodeType.Element)
            {
                OdfStreamEvent? elementStart = ReadElementStart();
                if (elementStart != null)
                {
                    return elementStart;
                }
            }
            else if (Reader.NodeType == XmlNodeType.EndElement)
            {
                OdfStreamEvent? elementEnd = ReadElementEnd();
                if (elementEnd != null)
                {
                    return elementEnd;
                }
            }
            else if (IsTextNode(Reader.NodeType))
            {
                if (IsCurrentXmlSubtreeSelected()
                    && Selection.IncludeTextEvents)
                {
                    return CreateValueEvent(
                        OdfStreamEventKind.Text,
                        Reader.Value);
                }
            }
            else if (Reader.NodeType == XmlNodeType.Comment)
            {
                if (IsCurrentXmlSubtreeSelected()
                    && Selection.IncludeCommentEvents)
                {
                    return CreateValueEvent(
                        OdfStreamEventKind.Comment,
                        Reader.Value);
                }
            }
            else if (Reader.NodeType == XmlNodeType.ProcessingInstruction)
            {
                if (IsCurrentXmlSubtreeSelected()
                    && Selection.IncludeProcessingInstructionEvents)
                {
                    return CreateProcessingInstructionEvent();
                }
            }
        }
        return CreateXmlEndOfStream();
    }

    internal void SkipCurrentSubtree()
    {
        CheckXmlCancellation();
        if (!CanSkipCurrent || CurrentStartEvent == null
            || CurrentSyntheticEnd == null)
        {
            throw new OdfDocumentException(
                "There is no current element subtree to skip.");
        }
        CurrentStartEvent.WasSkipped = true;
        if (CurrentStartIsEmpty)
        {
            if (PendingEvent != null)
            {
                PendingEvent.WasSkipped = true;
            }
            CanSkipCurrent = false;
            return;
        }
        int depth = CurrentSyntheticEnd.Depth;
        OdfStreamEvent endEvent = CurrentSyntheticEnd;
        endEvent.WasSkipped = true;
        ReaderHasUnreadPosition = MoveBeyondCurrentElement();
        PopNamespaceFrames(depth);
        if (SelectedRootDepth >= depth)
        {
            SelectedRootDepth = -1;
        }
        PendingEvent = endEvent;
        PopAfterPending = false;
        PendingPopDepth = -1;
        CanSkipCurrent = false;
    }

    bool MoveBeyondCurrentElement()
    {
        int depth = Reader.Depth;
        if (Reader.IsEmptyElement)
        {
            bool nextAvailable = Reader.Read();
            CheckXmlCancellation();
            return nextAvailable;
        }
        bool available = Reader.Read();
        CheckXmlCancellation();
        while (available
            && (Reader.NodeType != XmlNodeType.EndElement
                || Reader.Depth != depth))
        {
            available = Reader.Read();
            CheckXmlCancellation();
        }
        if (available)
        {
            available = Reader.Read();
            CheckXmlCancellation();
        }
        return available;
    }

    static void ValidateXmlLimits(OdfSecurityLimits limits)
    {
        if (limits.MaximumXmlDepth <= 0
            || limits.MaximumXmlAttributesPerElement <= 0
            || limits.MaximumXmlTextLength <= 0)
        {
            throw new OdfDocumentException(
                "XML security limits must be positive.");
        }
    }

    OdfStreamEvent? ReadElementStart()
    {
        int depth = Reader.Depth;
        if (depth + 1 > Limits.MaximumXmlDepth)
        {
            throw FailXml("The XML element depth exceeds the configured limit.");
        }
        if (Reader.AttributeCount > Limits.MaximumXmlAttributesPerElement)
        {
            throw FailXml(
                "An XML element exceeds the configured attribute-count limit.");
        }
        OdfNamespaceDeclaration[] declarations = ReadLocalNamespaces();
        PushNamespaceFrame(depth, declarations);
        SetCurrentFrameXmlId();
        bool selected = IsCurrentXmlSubtreeSelected();
        if (!selected && MatchesSelectedElement())
        {
            SelectedRootDepth = depth;
            selected = true;
        }
        if (!selected && Selection.SkipUnselectedSubtrees)
        {
            ReaderHasUnreadPosition = MoveBeyondCurrentElement();
            PopNamespaceFrames(depth);
            return null;
        }
        if (!selected)
        {
            if (Reader.IsEmptyElement)
            {
                PopNamespaceFrames(depth);
            }
            return null;
        }
        OdfStreamEvent result = CreateElementStartEvent(declarations);
        if (Reader.IsEmptyElement)
        {
            if (SelectedRootDepth == depth)
            {
                SelectedRootDepth = -1;
            }
            if (Selection.IncludeElementEvents)
            {
                PendingEvent = CreateSyntheticEnd(result, false);
                CurrentSyntheticEnd = PendingEvent;
                PopAfterPending = true;
                PendingPopDepth = depth;
            }
            else
            {
                PopNamespaceFrames(depth);
            }
        }
        if (!Selection.IncludeElementEvents)
        {
            return null;
        }
        CanSkipCurrent = true;
        CurrentStartIsEmpty = result.IsEmptyElement;
        CurrentStartEvent = result;
        if (CurrentSyntheticEnd == null)
        {
            CurrentSyntheticEnd = CreateSyntheticEnd(result, false);
        }
        return result;
    }

    OdfStreamEvent? ReadElementEnd()
    {
        int depth = Reader.Depth;
        bool selected = IsCurrentXmlSubtreeSelected();
        OdfStreamEvent? result = null;
        if (selected && Selection.IncludeElementEvents)
        {
            result = CreateElementEndEvent();
        }
        if (SelectedRootDepth == depth)
        {
            SelectedRootDepth = -1;
        }
        if (result != null)
        {
            PopBeforeRead = true;
            PendingPopDepth = depth;
            return result;
        }
        PopNamespaceFrames(depth);
        return null;
    }

    OdfStreamEvent CreateElementStartEvent(
        OdfNamespaceDeclaration[] declarations)
    {
        OdfStreamEvent result = CreateXmlEvent(
            OdfStreamEventKind.ElementStart);
        result.Name = CurrentQualifiedName();
        result.NamespaceDeclarations = declarations;
        result.NamespaceScope = CurrentNamespaceScope();
        result.Attributes = ReadElementAttributes();
        result.Depth = Reader.Depth;
        result.IsEmptyElement = Reader.IsEmptyElement;
        return result;
    }

    OdfStreamEvent CreateElementEndEvent()
    {
        OdfStreamEvent result = CreateXmlEvent(
            OdfStreamEventKind.ElementEnd);
        result.Name = CurrentQualifiedName();
        result.NamespaceScope = CurrentNamespaceScope();
        result.Depth = Reader.Depth;
        return result;
    }

    OdfStreamEvent CreateValueEvent(
        OdfStreamEventKind kind,
        string value)
    {
        if (value.Length > Limits.MaximumXmlTextLength)
        {
            throw FailXml("An XML text value exceeds the configured limit.");
        }
        OdfStreamEvent result = CreateXmlEvent(kind);
        result.Value = value;
        result.NamespaceScope = CurrentNamespaceScope();
        result.Depth = Reader.Depth;
        result.Location.TextLength = value.Length;
        if (kind == OdfStreamEventKind.Text
            && IsWhitespaceValue(value)
            && (Reader.NodeType == XmlNodeType.SignificantWhitespace
                || Reader.XmlSpace == XmlSpace.Preserve))
        {
            result.IsSignificantWhitespace = true;
        }
        return result;
    }

    OdfStreamEvent CreateProcessingInstructionEvent()
    {
        OdfStreamEvent result = CreateValueEvent(
            OdfStreamEventKind.ProcessingInstruction,
            Reader.Value);
        OdfQualifiedName name = new OdfQualifiedName();
        name.LocalName = Reader.Name;
        result.Name = name;
        return result;
    }

    OdfStreamEvent CreateXmlEvent(OdfStreamEventKind kind)
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = kind;
        result.PartPath = Reader.BaseURI;
        result.Location.PartPath = Reader.BaseURI;
        result.Location.XmlId = CurrentContextXmlId();
        IXmlLineInfo lineInfo = (IXmlLineInfo)Reader;
        if (lineInfo.HasLineInfo())
        {
            result.Location.Line = lineInfo.LineNumber;
            result.Location.Column = lineInfo.LinePosition;
        }
        return result;
    }

    OdfStreamEvent CreateSyntheticEnd(
        OdfStreamEvent start,
        bool skipped)
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = OdfStreamEventKind.ElementEnd;
        result.PartPath = start.PartPath;
        result.Name = CopyQualifiedName(start.Name);
        result.NamespaceScope = CopyNamespaceDeclarations(
            start.NamespaceScope);
        result.Depth = start.Depth;
        result.WasSkipped = skipped;
        result.Location = CopyXmlLocation(start.Location);
        return result;
    }

    OdfQualifiedName CurrentQualifiedName()
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = Reader.NamespaceURI;
        result.LocalName = Reader.LocalName;
        result.Prefix = Reader.Prefix;
        return result;
    }

    OdfAttribute[] ReadElementAttributes()
    {
        List<OdfAttribute> result = new List<OdfAttribute>();
        if (!Reader.HasAttributes)
        {
            return [];
        }
        bool available = Reader.MoveToFirstAttribute();
        while (available)
        {
            if (!IsNamespaceAttribute())
            {
                OdfAttribute attribute = new OdfAttribute();
                attribute.Name.NamespaceName = Reader.NamespaceURI;
                attribute.Name.LocalName = Reader.LocalName;
                attribute.Name.Prefix = Reader.Prefix;
                attribute.Value = Reader.Value;
                attribute.Location.PartPath = Reader.BaseURI;
                IXmlLineInfo lineInfo = (IXmlLineInfo)Reader;
                if (lineInfo.HasLineInfo())
                {
                    attribute.Location.Line = lineInfo.LineNumber;
                    attribute.Location.Column = lineInfo.LinePosition;
                }
                result.Add(attribute);
            }
            available = Reader.MoveToNextAttribute();
        }
        Reader.MoveToElement();
        return result.ToArray();
    }

    OdfNamespaceDeclaration[] ReadLocalNamespaces()
    {
        List<OdfNamespaceDeclaration> result =
            new List<OdfNamespaceDeclaration>();
        if (!Reader.HasAttributes)
        {
            return [];
        }
        bool available = Reader.MoveToFirstAttribute();
        while (available)
        {
            if (IsNamespaceAttribute())
            {
                OdfNamespaceDeclaration declaration =
                    new OdfNamespaceDeclaration();
                declaration.Prefix = Reader.Prefix == "xmlns"
                    ? Reader.LocalName
                    : "";
                declaration.NamespaceName = Reader.Value;
                declaration.Location.PartPath = Reader.BaseURI;
                IXmlLineInfo lineInfo = (IXmlLineInfo)Reader;
                if (lineInfo.HasLineInfo())
                {
                    declaration.Location.Line = lineInfo.LineNumber;
                    declaration.Location.Column = lineInfo.LinePosition;
                }
                result.Add(declaration);
            }
            available = Reader.MoveToNextAttribute();
        }
        Reader.MoveToElement();
        return result.ToArray();
    }

    bool IsNamespaceAttribute()
    {
        return Reader.Prefix == "xmlns" || Reader.Name == "xmlns";
    }

    void PushNamespaceFrame(
        int depth,
        OdfNamespaceDeclaration[] declarations)
    {
        StreamNamespaceFrame frame = new StreamNamespaceFrame();
        frame.Depth = depth;
        frame.Declarations = CopyNamespaceDeclarations(declarations);
        frame.XmlId = CurrentContextXmlId();
        NamespaceFrames.Add(frame);
    }

    void SetCurrentFrameXmlId()
    {
        string? xmlId = Reader.GetAttribute("id", OdfNamespaces.Xml);
        if (xmlId != null && xmlId.Length > 0)
        {
            NamespaceFrames[NamespaceFrames.Count - 1].XmlId = xmlId;
        }
    }

    string CurrentContextXmlId()
    {
        if (NamespaceFrames.Count == 0)
        {
            return "";
        }
        return NamespaceFrames[NamespaceFrames.Count - 1].XmlId;
    }

    void PopNamespaceFrames(int depth)
    {
        int index = NamespaceFrames.Count - 1;
        while (index >= 0 && NamespaceFrames[index].Depth >= depth)
        {
            NamespaceFrames.RemoveAt(index);
            index = index - 1;
        }
    }

    OdfNamespaceDeclaration[] CurrentNamespaceScope()
    {
        List<OdfNamespaceDeclaration> result =
            new List<OdfNamespaceDeclaration>();
        int frameIndex = 0;
        while (frameIndex < NamespaceFrames.Count)
        {
            OdfNamespaceDeclaration[] declarations =
                NamespaceFrames[frameIndex].Declarations;
            int declarationIndex = 0;
            while (declarationIndex < declarations.Length)
            {
                AddOrReplaceNamespace(
                    result,
                    declarations[declarationIndex]);
                declarationIndex = declarationIndex + 1;
            }
            frameIndex = frameIndex + 1;
        }
        return CopyNamespaceDeclarations(result.ToArray());
    }

    static void AddOrReplaceNamespace(
        List<OdfNamespaceDeclaration> values,
        OdfNamespaceDeclaration declaration)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Prefix == declaration.Prefix)
            {
                values[index] = declaration;
                return;
            }
            index = index + 1;
        }
        values.Add(declaration);
    }

    bool MatchesSelectedElement()
    {
        if (Selection.ElementNames == null
            || Selection.ElementNames.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < Selection.ElementNames.Length)
        {
            OdfQualifiedName name = Selection.ElementNames[index];
            if (name != null
                && name.NamespaceName == Reader.NamespaceURI
                && name.LocalName == Reader.LocalName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    bool IsCurrentXmlSubtreeSelected()
    {
        return Selection.ElementNames == null
            || Selection.ElementNames.Length == 0
            || SelectedRootDepth >= 0;
    }

    static bool IsTextNode(XmlNodeType nodeType)
    {
        return nodeType == XmlNodeType.Text
            || nodeType == XmlNodeType.CDATA
            || nodeType == XmlNodeType.Whitespace
            || nodeType == XmlNodeType.SignificantWhitespace;
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

    static OdfQualifiedName? CopyQualifiedName(OdfQualifiedName? source)
    {
        if (source == null)
        {
            return null;
        }
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = source.NamespaceName;
        result.LocalName = source.LocalName;
        result.Prefix = source.Prefix;
        return result;
    }

    static OdfNamespaceDeclaration[] CopyNamespaceDeclarations(
        OdfNamespaceDeclaration[] source)
    {
        OdfNamespaceDeclaration[] result =
            new OdfNamespaceDeclaration[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            OdfNamespaceDeclaration declaration =
                new OdfNamespaceDeclaration();
            declaration.Prefix = source[index].Prefix;
            declaration.NamespaceName = source[index].NamespaceName;
            declaration.Location = CopyXmlLocation(source[index].Location);
            result[index] = declaration;
            index = index + 1;
        }
        return result;
    }

    static OdfLocation CopyXmlLocation(OdfLocation source)
    {
        OdfLocation result = new OdfLocation();
        result.PartPath = source.PartPath;
        result.NodePath = source.NodePath;
        result.XmlId = source.XmlId;
        result.Line = source.Line;
        result.Column = source.Column;
        result.ByteOffset = source.ByteOffset;
        result.TextOffset = source.TextOffset;
        result.TextLength = source.TextLength;
        return result;
    }

    OdfStreamEvent FinishXmlInput()
    {
        Finished = true;
        Reader.Dispose();
        CloseXmlInputIfOwned();
        return CreateXmlEndOfStream();
    }

    static OdfStreamEvent CreateXmlEndOfStream()
    {
        OdfStreamEvent result = new OdfStreamEvent();
        result.Kind = OdfStreamEventKind.EndOfStream;
        return result;
    }

    void CheckXmlCancellation()
    {
        if (Options.Cancellation.IsCancellationRequested)
        {
            CloseXmlInputIfOwned();
            string reason = Options.Cancellation.Reason;
            if (reason == null || reason.Length == 0)
            {
                throw new OdfDocumentException(
                    "XML streaming was cancelled.");
            }
            throw new OdfDocumentException(
                "XML streaming was cancelled: " + reason);
        }
    }

    OdfDocumentException FailXml(string message)
    {
        CloseXmlInputIfOwned();
        return new OdfDocumentException(message);
    }

    void CloseXmlInputIfOwned()
    {
        if (!InputClosed && !Options.LeaveInputOpen)
        {
            Input.Dispose();
            InputClosed = true;
        }
    }
}

internal class StreamNamespaceFrame
{
    internal int Depth;
    internal OdfNamespaceDeclaration[] Declarations = [];
    internal string XmlId = "";
}
