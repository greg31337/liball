using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace odf;

internal class StreamingXmlWriteFrame
{
    internal OdfQualifiedName Name = new OdfQualifiedName();
    internal bool WasEmptyElement;
}

internal class XmlEventWriter
{
    const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    Stream Output;
    OdfSaveOptions Options;
    XmlWriter Writer;
    List<StreamingXmlWriteFrame> Frames =
        new List<StreamingXmlWriteFrame>();
    bool Finished;

    internal XmlEventWriter(Stream output, OdfSaveOptions options)
    {
        if (output == null || !output.CanWrite || options == null
            || options.Cancellation == null)
        {
            throw new OdfXmlException(
                "A writable output stream and save options are required for XML streaming.");
        }
        Output = output;
        Options = options;
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.CheckCharacters = true;
        settings.CloseOutput = false;
        settings.ConformanceLevel = ConformanceLevel.Document;
        settings.Encoding = new UTF8Encoding(false);
        settings.Indent = options.IndentXml;
        settings.IndentChars = "  ";
        settings.NewLineChars = "\n";
        settings.NewLineHandling = NewLineHandling.None;
        settings.NewLineOnAttributes = false;
        settings.OmitXmlDeclaration = false;
        Writer = XmlWriter.Create(output, settings);
        Writer.WriteStartDocument();
    }

    internal void WriteEvent(OdfStreamEvent streamEvent)
    {
        CheckXmlWriteState();
        if (streamEvent == null)
        {
            throw new OdfXmlException(
                "An XML stream event is required for writing.");
        }
        if (streamEvent.Kind == OdfStreamEventKind.ElementStart)
        {
            WriteElementStart(streamEvent);
        }
        else if (streamEvent.Kind == OdfStreamEventKind.ElementEnd)
        {
            WriteElementEnd(streamEvent);
        }
        else if (streamEvent.Kind == OdfStreamEventKind.Text)
        {
            WriteTextEvent(streamEvent.Value);
        }
        else if (streamEvent.Kind == OdfStreamEventKind.Comment)
        {
            Writer.WriteComment(streamEvent.Value);
        }
        else if (streamEvent.Kind
            == OdfStreamEventKind.ProcessingInstruction)
        {
            WriteProcessingInstruction(streamEvent);
        }
        else if (streamEvent.Kind == OdfStreamEventKind.EndOfStream)
        {
            CompleteXml();
        }
        else
        {
            throw new OdfXmlException(
                "A package event cannot be written into an XML stream.");
        }
    }

    internal void CompleteXml()
    {
        if (Finished)
        {
            return;
        }
        CheckXmlCancellation();
        if (Frames.Count != 0)
        {
            throw new OdfXmlException(
                "The XML event stream ended before every element was closed.");
        }
        Writer.WriteEndDocument();
        Writer.Flush();
        Writer.Dispose();
        Finished = true;
        if (!Options.LeaveOutputOpen)
        {
            Output.Dispose();
        }
    }

    void WriteElementStart(OdfStreamEvent streamEvent)
    {
        OdfQualifiedName name = RequireXmlName(streamEvent.Name);
        Writer.WriteStartElement(
            name.Prefix,
            name.LocalName,
            name.NamespaceName);
        OdfNamespaceDeclaration[] declarations =
            streamEvent.NamespaceDeclarations;
        OdfAttribute[] attributes = streamEvent.Attributes;
        if (Options.Deterministic)
        {
            declarations = SortXmlDeclarations(declarations);
            attributes = SortXmlAttributes(attributes);
        }
        int declarationIndex = 0;
        while (declarationIndex < declarations.Length)
        {
            WriteXmlDeclaration(declarations[declarationIndex]);
            declarationIndex = declarationIndex + 1;
        }
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            WriteXmlAttribute(attributes[attributeIndex]);
            attributeIndex = attributeIndex + 1;
        }
        StreamingXmlWriteFrame frame = new StreamingXmlWriteFrame();
        frame.Name = CopyXmlName(name);
        frame.WasEmptyElement = streamEvent.IsEmptyElement;
        Frames.Add(frame);
    }

    void WriteElementEnd(OdfStreamEvent streamEvent)
    {
        if (Frames.Count == 0)
        {
            throw new OdfXmlException(
                "The XML event stream contains an unmatched closing element.");
        }
        int index = Frames.Count - 1;
        StreamingXmlWriteFrame frame = Frames[index];
        if (!XmlNamesMatch(frame.Name, streamEvent.Name))
        {
            throw new OdfXmlException(
                "The XML event stream contains mismatched element boundaries.");
        }
        if (Options.Deterministic || !frame.WasEmptyElement)
        {
            Writer.WriteFullEndElement();
        }
        else
        {
            Writer.WriteEndElement();
        }
        Frames.RemoveAt(index);
    }

    void WriteTextEvent(string value)
    {
        if (IsXmlWhitespace(value))
        {
            Writer.WriteWhitespace(value);
        }
        else
        {
            Writer.WriteString(value);
        }
    }

    void WriteProcessingInstruction(OdfStreamEvent streamEvent)
    {
        OdfQualifiedName name = RequireXmlName(streamEvent.Name);
        if (name.NamespaceName.Length > 0 || name.Prefix.Length > 0)
        {
            throw new OdfXmlException(
                "An XML processing instruction requires an unqualified target.");
        }
        Writer.WriteProcessingInstruction(name.LocalName, streamEvent.Value);
    }

    void WriteXmlDeclaration(OdfNamespaceDeclaration declaration)
    {
        if (declaration == null)
        {
            throw new OdfXmlException(
                "The XML event contains a null namespace declaration.");
        }
        if (declaration.Prefix.Length == 0)
        {
            Writer.WriteAttributeString(
                "",
                "xmlns",
                XmlnsNamespace,
                declaration.NamespaceName);
        }
        else
        {
            Writer.WriteAttributeString(
                "xmlns",
                declaration.Prefix,
                XmlnsNamespace,
                declaration.NamespaceName);
        }
    }

    void WriteXmlAttribute(OdfAttribute attribute)
    {
        if (attribute == null || attribute.Name == null
            || !OdfNameRules.IsQualifiedNameValue(attribute.Name))
        {
            throw new OdfXmlException(
                "The XML event contains an invalid attribute name.");
        }
        Writer.WriteAttributeString(
            attribute.Name.Prefix,
            attribute.Name.LocalName,
            attribute.Name.NamespaceName,
            attribute.Value);
    }

    static OdfQualifiedName RequireXmlName(OdfQualifiedName? source)
    {
        if (source == null || !OdfNameRules.IsQualifiedNameValue(source))
        {
            throw new OdfXmlException(
                "The XML event requires a valid qualified name.");
        }
        return source;
    }

    static bool XmlNamesMatch(
        OdfQualifiedName expected,
        OdfQualifiedName? actual)
    {
        return actual != null
            && expected.NamespaceName == actual.NamespaceName
            && expected.LocalName == actual.LocalName;
    }

    static OdfQualifiedName CopyXmlName(OdfQualifiedName source)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = source.NamespaceName;
        result.LocalName = source.LocalName;
        result.Prefix = source.Prefix;
        return result;
    }

    static OdfNamespaceDeclaration[] SortXmlDeclarations(
        OdfNamespaceDeclaration[] source)
    {
        OdfNamespaceDeclaration[] result =
            new OdfNamespaceDeclaration[source.Length];
        Array.Copy(source, result, source.Length);
        int index = 1;
        while (index < result.Length)
        {
            OdfNamespaceDeclaration current = result[index];
            int destination = index;
            while (destination > 0
                && string.CompareOrdinal(
                    current.Prefix,
                    result[destination - 1].Prefix) < 0)
            {
                result[destination] = result[destination - 1];
                destination = destination - 1;
            }
            result[destination] = current;
            index = index + 1;
        }
        return result;
    }

    static OdfAttribute[] SortXmlAttributes(OdfAttribute[] source)
    {
        OdfAttribute[] result = new OdfAttribute[source.Length];
        Array.Copy(source, result, source.Length);
        int index = 1;
        while (index < result.Length)
        {
            OdfAttribute current = result[index];
            int destination = index;
            while (destination > 0
                && CompareXmlAttributes(
                    current,
                    result[destination - 1]) < 0)
            {
                result[destination] = result[destination - 1];
                destination = destination - 1;
            }
            result[destination] = current;
            index = index + 1;
        }
        return result;
    }

    static int CompareXmlAttributes(
        OdfAttribute first,
        OdfAttribute second)
    {
        int comparison = string.CompareOrdinal(
            first.Name.NamespaceName,
            second.Name.NamespaceName);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(
            first.Name.LocalName,
            second.Name.LocalName);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(first.Name.Prefix, second.Name.Prefix);
    }

    static bool IsXmlWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' ' && character != '\t'
                && character != '\r' && character != '\n')
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    void CheckXmlWriteState()
    {
        if (Finished)
        {
            throw new OdfXmlException(
                "The XML event writer has already completed.");
        }
        CheckXmlCancellation();
    }

    void CheckXmlCancellation()
    {
        if (Options.Cancellation.IsCancellationRequested)
        {
            throw new OdfXmlException(
                "XML event writing was cancelled.");
        }
    }
}
