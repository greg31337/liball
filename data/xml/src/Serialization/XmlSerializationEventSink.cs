using System.Collections.Generic;

namespace xml;

internal class XmlSerializationEventElement
{
    internal XmlNode Node;
    internal bool Empty;

    internal XmlSerializationEventElement(XmlNode node, bool empty)
    {
        Node = node;
        Empty = empty;
    }
}

internal class XmlSerializationEventSink : IXmlEventHandler, IXmlEventCompletion
{
    internal const int MaximumInputCharacterCount = 1024;
    internal const int MaximumBufferedCharacterCount = MaximumInputCharacterCount * 6;
    internal XmlSerializationOptions Options;
    internal XmlOutputWriter Output;
    internal XmlSerializationWriter? XmlFormatter;
    internal TextSerializationWriter? TextFormatter;
    internal XmlEventNodeFactory Nodes;
    internal List<XmlSerializationEventElement> Elements;
    internal bool Started;
    internal bool Completed;
    internal int PeakBufferedCharacterCount;

    internal XmlSerializationEventSink(XmlOutput output, XmlSerializationOptions options)
    {
        Guard.NotNull(output, "output");
        Guard.NotNull(options, "options");
        if (options.Method != XmlSerializationMethod.Xml && options.Method != XmlSerializationMethod.Text)
        {
            throw new System.ArgumentException("The streaming serialization sink supports XML and text output.", "options");
        }

        if (!SupportsXmlEventStreamingNormalization(options.NormalizationForm))
        {
            throw new System.ArgumentException("Streaming serialization requires normalization-form none.", "options");
        }

        Options = options;
        Output = new XmlOutputWriter(output, options.Encoding, options.ByteOrderMark, 4096);
        if (output.Kind != XmlOutputKind.TextWriter && options.Method == XmlSerializationMethod.Xml && string.Equals(Output.InspectXmlOutputEncodingName(), "utf-16", System.StringComparison.OrdinalIgnoreCase))
        {
            Output.EmitByteOrderMark = true;
        }

        XmlVersion version = options.Version != null && string.Equals(options.Version, "1.1", System.StringComparison.Ordinal) ? XmlVersion.Xml11 : XmlVersion.Xml10;
        Nodes = new XmlEventNodeFactory(version);
        Elements = new List<XmlSerializationEventElement>();
        Started = false;
        Completed = false;
        PeakBufferedCharacterCount = 0;
        if (options.Method == XmlSerializationMethod.Text)
        {
            TextFormatter = new TextSerializationWriter(options);
            XmlFormatter = null;
        }
        else
        {
            XmlFormatter = new XmlSerializationWriter(options, version, Output.InspectXmlOutputEncodingName());
            TextFormatter = null;
        }
    }

    public XmlHandlerAction HandleXmlEvent(XmlEvent value)
    {
        Guard.NotNull(value, "value");
        if (Completed)
        {
            throw new System.InvalidOperationException("The streaming serialization sink is complete.");
        }

        if (Options.Method == XmlSerializationMethod.Text)
        {
            WriteTextSerializationEvent(value);
            if (value.Kind == XmlEventKind.EndDocument)
            {
                CompleteXmlEventSink();
            }

            return XmlHandlerAction.Continue;
        }

        WriteXmlSerializationEvent(value);
        if (value.Kind == XmlEventKind.EndDocument)
        {
            CompleteXmlEventSink();
        }

        return XmlHandlerAction.Continue;
    }

    public void CompleteXmlEventSink()
    {
        if (Completed)
        {
            return;
        }

        if (Elements.Count != 0)
        {
            throw CreateXmlEventSerializationError("The XML event stream ends inside an element.");
        }

        EnsureXmlEventSerializationStarted();
        DrainXmlEventSerializationBuffer();
        Output.CompleteXmlOutput();
        Completed = true;
    }

    internal void WriteTextSerializationEvent(XmlEvent value)
    {
        if (value.Kind != XmlEventKind.Text && value.Kind != XmlEventKind.IgnorableWhitespace && value.Kind != XmlEventKind.CData)
        {
            return;
        }

        TextSerializationWriter formatter = TextFormatter == null ? throw new System.InvalidOperationException("The text event formatter is unavailable.") : TextFormatter;
        string source = value.Value == null ? "" : value.Value;
        int position = 0;
        while (position < source.Length)
        {
            int count = SelectXmlEventCharacterCount(source, position);
            string chunk = source.Substring(position, count);
            formatter.ValidateTextSerializationCharacters(chunk);
            string written = formatter.ApplyTextSerializationCharacterMap(chunk);
            ObserveXmlEventBuffer(written.Length);
            Output.WriteXmlOutputText(written);
            position = position + count;
        }
    }

    internal void WriteXmlSerializationEvent(XmlEvent value)
    {
        if (value.Kind == XmlEventKind.StartDocument)
        {
            EnsureXmlEventSerializationStarted();
        }
        else if (value.Kind == XmlEventKind.StartElement)
        {
            WriteXmlEventStartElement(value);
        }
        else if (value.Kind == XmlEventKind.EndElement)
        {
            WriteXmlEventEndElement(value);
        }
        else if (value.Kind == XmlEventKind.Text || value.Kind == XmlEventKind.IgnorableWhitespace)
        {
            EnsureXmlEventSerializationStarted();
            bool cdata = Elements.Count > 0 && FindXmlEventFormatter().MatchesXmlSerializationName(Elements[Elements.Count - 1].Node.Name, Options.CDataSectionElements);
            WriteXmlEventChunkedValue(value.Value == null ? "" : value.Value, XmlCharacterEscapeContext.Text, cdata, true);
        }
        else if (value.Kind == XmlEventKind.CData)
        {
            EnsureXmlEventSerializationStarted();
            WriteXmlEventChunkedValue(value.Value == null ? "" : value.Value, XmlCharacterEscapeContext.CData, true, true);
        }
        else if (value.Kind == XmlEventKind.Comment)
        {
            EnsureXmlEventSerializationStarted();
            AppendXmlEventSyntax("<!--");
            WriteXmlEventChunkedValue(value.Value == null ? "" : value.Value, XmlCharacterEscapeContext.Comment, false, false);
            AppendXmlEventSyntax("-->");
        }
        else if (value.Kind == XmlEventKind.ProcessingInstruction)
        {
            EnsureXmlEventSerializationStarted();
            XmlNode node = Nodes.CreateXmlEventNamedNode(value, XmlNodeKind.ProcessingInstruction, Elements.Count == 0 ? Nodes.DocumentNode : Elements[Elements.Count - 1].Node);
            AppendXmlEventSyntax("<?" + FindXmlEventFormatter().XmlSerializationNodeName(node));
            string instructionValue = value.Value == null ? "" : value.Value;
            if (instructionValue.Length > 0)
            {
                AppendXmlEventSyntax(" ");
                WriteXmlEventChunkedValue(instructionValue, XmlCharacterEscapeContext.ProcessingInstruction, false, false);
            }

            AppendXmlEventSyntax("?>");
        }
        else if (value.Kind == XmlEventKind.SkippedEntity)
        {
            XmlNode entity = Nodes.CreateXmlEventNamedNode(value, XmlNodeKind.EntityReference, Elements.Count == 0 ? Nodes.DocumentNode : Elements[Elements.Count - 1].Node);
            AppendXmlEventSyntax("&" + FindXmlEventFormatter().XmlSerializationNodeName(entity) + ";");
        }
        else if (value.Kind == XmlEventKind.StartDtd || value.Kind == XmlEventKind.EndDtd || value.Kind == XmlEventKind.ElementDeclaration || value.Kind == XmlEventKind.AttributeDeclaration || value.Kind == XmlEventKind.EntityDeclaration || value.Kind == XmlEventKind.NotationDeclaration)
        {
            throw CreateXmlEventSerializationError("DTD events require grounded serialization.");
        }
    }

    internal void WriteXmlEventStartElement(XmlEvent value)
    {
        EnsureXmlEventSerializationStarted();
        XmlSerializationWriter formatter = FindXmlEventFormatter();
        XmlNode parent = Elements.Count == 0 ? Nodes.DocumentNode : Elements[Elements.Count - 1].Node;
        XmlNode element = Nodes.CreateXmlEventElementNode(value, parent);
        AppendXmlEventSyntax("<" + formatter.XmlSerializationNodeName(element));
        WriteXmlEventNamespaces(element.NamespaceBindings);
        WriteXmlEventAttributes(element);
        AppendXmlEventSyntax(value.IsEmptyElement ? "/>" : ">");
        Elements.Add(new XmlSerializationEventElement(element, value.IsEmptyElement));
    }

    internal void WriteXmlEventEndElement(XmlEvent value)
    {
        if (Elements.Count == 0)
        {
            throw CreateXmlEventSerializationError("An end-element event has no matching start element.");
        }

        XmlSerializationEventElement element = Elements[Elements.Count - 1];
        Elements.RemoveAt(Elements.Count - 1);
        if (!element.Empty)
        {
            AppendXmlEventSyntax("</" + FindXmlEventFormatter().XmlSerializationNodeName(element.Node) + ">");
        }
    }

    internal void WriteXmlEventNamespaces(XmlNamespaceBinding[]? bindings)
    {
        XmlNamespaceBinding[] values = bindings == null ? [] : bindings;
        int index = 0;
        while (index < values.Length)
        {
            XmlNamespaceBinding binding = values[index];
            if (binding.Prefix == null || binding.NamespaceUri == null)
            {
                throw CreateXmlEventSerializationError("An element contains an incomplete namespace binding.");
            }

            if ((!binding.IsUndeclaration && binding.NamespaceUri.Length > 0) || binding.Prefix.Length == 0 || Options.UndeclarePrefixes)
            {
                AppendXmlEventSyntax(binding.Prefix.Length == 0 ? " xmlns=\"" : " xmlns:" + binding.Prefix + "=\"");
                WriteXmlEventChunkedValue(binding.IsUndeclaration ? "" : binding.NamespaceUri, XmlCharacterEscapeContext.AttributeDoubleQuoted, false, false);
                AppendXmlEventSyntax("\"");
            }

            index = index + 1;
        }
    }

    internal void WriteXmlEventAttributes(XmlNode element)
    {
        XmlSerializationWriter formatter = FindXmlEventFormatter();
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            if (!formatter.IsXmlSerializationNamespaceAttribute(attribute))
            {
                AppendXmlEventSyntax(" " + formatter.XmlSerializationNodeName(attribute) + "=\"");
                WriteXmlEventChunkedValue(attribute.Value == null ? "" : attribute.Value, XmlCharacterEscapeContext.AttributeDoubleQuoted, false, true);
                AppendXmlEventSyntax("\"");
            }

            index = index + 1;
        }
    }

    internal void WriteXmlEventChunkedValue(string value, XmlCharacterEscapeContext context, bool cdata, bool mapped)
    {
        XmlSerializationWriter formatter = FindXmlEventFormatter();
        int position = 0;
        while (position < value.Length)
        {
            int count = SelectXmlEventCharacterCount(value, position);
            string chunk = value.Substring(position, count);
            if (mapped)
            {
                formatter.WriteXmlSerializationMappedValue(chunk, context, cdata);
            }
            else
            {
                formatter.WriteXmlSerializationPlainValue(chunk, context, cdata);
            }

            DrainXmlEventSerializationBuffer();
            position = position + count;
        }
    }

    internal int SelectXmlEventCharacterCount(string value, int position)
    {
        int remaining = value.Length - position;
        int count = remaining < MaximumInputCharacterCount ? remaining : MaximumInputCharacterCount;
        if (position + count < value.Length && count > 0 && char.IsHighSurrogate(value[position + count - 1]))
        {
            count = count - 1;
        }

        return count == 0 ? remaining : count;
    }

    internal void EnsureXmlEventSerializationStarted()
    {
        if (Started)
        {
            return;
        }

        Started = true;
        if (XmlFormatter != null)
        {
            XmlFormatter.WriteXmlSerializationDeclaration();
            DrainXmlEventSerializationBuffer();
        }
    }

    internal void AppendXmlEventSyntax(string value)
    {
        XmlSerializationWriter formatter = FindXmlEventFormatter();
        formatter.Result.Append(value);
        DrainXmlEventSerializationBuffer();
    }

    internal void DrainXmlEventSerializationBuffer()
    {
        XmlSerializationWriter? formatter = XmlFormatter;
        if (formatter == null || formatter.Result.Length == 0)
        {
            return;
        }

        ObserveXmlEventBuffer(formatter.Result.Length);
        string value = formatter.Result.ToString();
        formatter.Result.Clear();
        Output.WriteXmlOutputText(value);
    }

    internal void ObserveXmlEventBuffer(int count)
    {
        if (count > PeakBufferedCharacterCount)
        {
            PeakBufferedCharacterCount = count;
        }
    }

    internal XmlSerializationWriter FindXmlEventFormatter()
    {
        return XmlFormatter == null ? throw new System.InvalidOperationException("The XML event formatter is unavailable.") : XmlFormatter;
    }

    internal bool SupportsXmlEventStreamingNormalization(string? value)
    {
        return value == null || string.Equals(value, "none", System.StringComparison.OrdinalIgnoreCase);
    }

    internal XmlSerializationException CreateXmlEventSerializationError(string message)
    {
        XmlSerializationWriter? formatter = XmlFormatter;
        if (formatter != null)
        {
            return formatter.CreateXmlSerializationError(message, XmlErrorCodes.SENR0001);
        }

        TextSerializationWriter? text = TextFormatter;
        if (text != null)
        {
            return text.CreateTextSerializationError(message, XmlErrorCodes.SENR0001);
        }

        throw new System.InvalidOperationException(message);
    }
}
