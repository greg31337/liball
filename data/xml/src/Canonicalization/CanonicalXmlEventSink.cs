using System.Collections.Generic;

namespace xml;

internal class CanonicalXmlEventElement
{
    internal XmlNode Node;
    internal CanonicalXml11Context Context;

    internal CanonicalXmlEventElement(XmlNode node, CanonicalXml11Context context)
    {
        Node = node;
        Context = context;
    }
}

internal class CanonicalXmlEventSink : IXmlEventHandler, IXmlEventCompletion
{
    internal XmlEventNodeFactory Nodes;
    internal CanonicalXml11Writer Formatter;
    internal CanonicalXml11Context RootContext;
    internal List<CanonicalXmlEventElement> Elements;
    internal bool RootStarted;
    internal bool RootEnded;
    internal bool Completed;

    internal CanonicalXmlEventSink(XmlOutput output, XmlCanonicalizationOptions options)
    {
        Guard.NotNull(output, "output");
        Guard.NotNull(options, "options");
        if (options.Selection != null)
        {
            throw CreateCanonicalXmlEventError("A node selection cannot be applied to an event stream.");
        }

        if (options.Method != XmlCanonicalizationMethod.CanonicalXml11 && options.Method != XmlCanonicalizationMethod.ExclusiveXml10 && options.Method != XmlCanonicalizationMethod.CanonicalXml20Experimental)
        {
            throw CreateCanonicalXmlEventError("The requested canonicalization method is not implemented.");
        }

        XmlOutput canonicalOutput = new XmlOutput();
        canonicalOutput.Kind = output.Kind;
        canonicalOutput.ByteStream = output.ByteStream;
        canonicalOutput.TextWriter = output.TextWriter;
        canonicalOutput.ByteSink = output.ByteSink;
        canonicalOutput.BaseUri = output.BaseUri;
        canonicalOutput.MediaType = output.MediaType;
        canonicalOutput.OwnsResource = output.OwnsResource;
        XmlVersion version = options.Method == XmlCanonicalizationMethod.ExclusiveXml10 ? XmlVersion.Xml10 : XmlVersion.Xml11;
        Nodes = new XmlEventNodeFactory(version);
        Formatter = new CanonicalXml11Writer(Nodes.Document, options, new XmlOutputWriter(canonicalOutput, "utf-8", false, 4096));
        List<CanonicalNamespaceValue> namespaces = new List<CanonicalNamespaceValue>();
        namespaces.Add(new CanonicalNamespaceValue("", ""));
        RootContext = new CanonicalXml11Context(namespaces, null);
        Elements = new List<CanonicalXmlEventElement>();
        RootStarted = false;
        RootEnded = false;
        Completed = false;
    }

    public XmlHandlerAction HandleXmlEvent(XmlEvent value)
    {
        Guard.NotNull(value, "value");
        if (Completed)
        {
            throw new System.InvalidOperationException("The canonical event sink is complete.");
        }

        if (value.Kind == XmlEventKind.StartElement)
        {
            WriteCanonicalXmlEventStartElement(value);
        }
        else if (value.Kind == XmlEventKind.EndElement)
        {
            WriteCanonicalXmlEventEndElement(value);
        }
        else if (value.Kind == XmlEventKind.Text || value.Kind == XmlEventKind.IgnorableWhitespace || value.Kind == XmlEventKind.CData)
        {
            Formatter.WriteCanonicalXml11Text(value.Value == null ? "" : value.Value);
        }
        else if (value.Kind == XmlEventKind.ProcessingInstruction)
        {
            WriteCanonicalXmlEventProcessingInstruction(value);
        }
        else if (value.Kind == XmlEventKind.Comment && Formatter.Options.IncludeComments)
        {
            WriteCanonicalXmlEventComment(value);
        }
        else if (value.Kind == XmlEventKind.SkippedEntity)
        {
            Formatter.WriteCanonicalXml11Text(value.Value == null ? "" : value.Value);
        }
        else if (value.Kind == XmlEventKind.EndDocument)
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
            throw CreateCanonicalXmlEventError("The canonical event stream ends inside an element.");
        }

        Formatter.Result.CompleteCanonicalOutput();
        Completed = true;
    }

    internal void WriteCanonicalXmlEventStartElement(XmlEvent value)
    {
        if (Elements.Count == 0 && RootStarted)
        {
            throw CreateCanonicalXmlEventError("A canonical document cannot contain more than one document element.");
        }

        XmlNode parent = Elements.Count == 0 ? Nodes.DocumentNode : Elements[Elements.Count - 1].Node;
        XmlNode element = Nodes.CreateXmlEventElementNode(value, parent);
        CanonicalXml11Context parentContext = Elements.Count == 0 ? RootContext : Elements[Elements.Count - 1].Context;
        CanonicalXml11Context context = Formatter.CopyCanonicalXml11Context(parentContext);
        Formatter.Result.AppendCanonicalCharacter('<');
        Formatter.Result.AppendCanonicalText(Formatter.CanonicalXml11NodeName(element));
        List<CanonicalNamespaceValue> namespaces = Formatter.CollectCanonicalXml11Namespaces(element);
        Formatter.AddCanonicalXml11DefaultUndeclaration(element, namespaces, parentContext, true);
        Formatter.SortCanonicalXml11Namespaces(namespaces);
        Formatter.WriteCanonicalXml11Namespaces(namespaces, context, true);
        List<CanonicalAttributeValue> attributes = Formatter.CollectCanonicalXml11Attributes(element, context, true);
        Formatter.SortCanonicalXml11Attributes(attributes);
        Formatter.WriteCanonicalXml11Attributes(attributes);
        Formatter.Result.AppendCanonicalCharacter('>');
        Elements.Add(new CanonicalXmlEventElement(element, context));
        if (Elements.Count == 1)
        {
            RootStarted = true;
        }
    }

    internal void WriteCanonicalXmlEventEndElement(XmlEvent value)
    {
        if (Elements.Count == 0)
        {
            throw CreateCanonicalXmlEventError("An end-element event has no matching start element.");
        }

        CanonicalXmlEventElement element = Elements[Elements.Count - 1];
        Elements.RemoveAt(Elements.Count - 1);
        Formatter.Result.AppendCanonicalText("</");
        Formatter.Result.AppendCanonicalText(Formatter.CanonicalXml11NodeName(element.Node));
        Formatter.Result.AppendCanonicalCharacter('>');
        if (Elements.Count == 0)
        {
            RootEnded = true;
        }
    }

    internal void WriteCanonicalXmlEventProcessingInstruction(XmlEvent value)
    {
        XmlNode parent = Elements.Count == 0 ? Nodes.DocumentNode : Elements[Elements.Count - 1].Node;
        XmlNode node = Nodes.CreateXmlEventNamedNode(value, XmlNodeKind.ProcessingInstruction, parent);
        if (Elements.Count == 0 && RootEnded)
        {
            Formatter.Result.AppendCanonicalCharacter('\n');
            node.Parent = null;
        }

        Formatter.WriteCanonicalXml11ProcessingInstruction(node);
    }

    internal void WriteCanonicalXmlEventComment(XmlEvent value)
    {
        XmlNode parent = Elements.Count == 0 ? Nodes.DocumentNode : Elements[Elements.Count - 1].Node;
        XmlNode node = Nodes.CreateXmlEventNamedNode(value, XmlNodeKind.Comment, parent);
        if (Elements.Count == 0 && RootEnded)
        {
            Formatter.Result.AppendCanonicalCharacter('\n');
            node.Parent = null;
        }

        Formatter.WriteCanonicalXml11Comment(node);
    }

    internal XmlCanonicalizationException CreateCanonicalXmlEventError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.CanonicalizationFailure;
        diagnostic.Category = XmlErrorCategory.Canonicalization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlCanonicalizationException(message, diagnostic, null);
    }
}
