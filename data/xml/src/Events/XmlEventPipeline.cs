namespace xml;

internal interface IXmlEventTransform
{
    XmlEvent? TransformXmlEvent(XmlEvent value);
}

internal interface IXmlEventCompletion
{
    void CompleteXmlEventSink();
}

internal class XmlEventPipeline : IXmlEventHandler
{
    internal IXmlEventTransform[] Transforms;
    internal IXmlEventHandler Sink;
    internal int ReadEventCount;
    internal int WrittenEventCount;
    internal bool Completed;
    internal bool Aborted;

    internal XmlEventPipeline(IXmlEventTransform[] transforms, IXmlEventHandler sink)
    {
        Guard.NotNull(transforms, "transforms");
        Guard.NotNull(sink, "sink");
        Transforms = (IXmlEventTransform[])transforms.Clone();
        Sink = sink;
        ReadEventCount = 0;
        WrittenEventCount = 0;
        Completed = false;
        Aborted = false;
        int index = 0;
        while (index < Transforms.Length)
        {
            if (Transforms[index] == null)
            {
                throw new System.ArgumentException("An XML event transform cannot be null.", "transforms");
            }

            index = index + 1;
        }
    }

    public XmlHandlerAction HandleXmlEvent(XmlEvent value)
    {
        Guard.NotNull(value, "value");
        if (Completed || Aborted)
        {
            throw new System.InvalidOperationException("The XML event pipeline is no longer accepting events.");
        }

        ReadEventCount = ReadEventCount + 1;
        XmlEvent? current = value;
        int index = 0;
        while (index < Transforms.Length && current != null)
        {
            current = Transforms[index].TransformXmlEvent(current);
            index = index + 1;
        }

        if (current == null)
        {
            return XmlHandlerAction.Continue;
        }

        XmlHandlerAction action = Sink.HandleXmlEvent(current);
        WrittenEventCount = WrittenEventCount + 1;
        if (action == XmlHandlerAction.Abort)
        {
            Aborted = true;
        }

        return action;
    }

    internal XmlHandlerAction ProcessXmlEventBatch(XmlEventBatch batch)
    {
        Guard.NotNull(batch, "batch");
        XmlEvent[] events = batch.Events == null ? [] : batch.Events;
        if (batch.Offset < 0 || batch.Count < 0 || batch.Offset > events.Length - batch.Count)
        {
            throw new System.ArgumentOutOfRangeException("batch");
        }

        int end = batch.Offset + batch.Count;
        int index = batch.Offset;
        while (index < end)
        {
            XmlHandlerAction action = HandleXmlEvent(events[index]);
            if (action != XmlHandlerAction.Continue)
            {
                return action;
            }

            index = index + 1;
        }

        if (batch.IsFinal)
        {
            CompleteXmlEventPipeline();
        }

        return XmlHandlerAction.Continue;
    }

    internal void CompleteXmlEventPipeline()
    {
        if (Completed)
        {
            return;
        }

        if (Aborted)
        {
            throw new System.InvalidOperationException("An aborted XML event pipeline cannot be completed.");
        }

        IXmlEventCompletion? completion = Sink as IXmlEventCompletion;
        if (completion != null)
        {
            completion.CompleteXmlEventSink();
        }

        Completed = true;
    }
}

internal class XmlEventNodeFactory
{
    internal XmlDocument Document;
    internal XmlNode DocumentNode;

    internal XmlEventNodeFactory(XmlVersion version)
    {
        Document = new XmlDocument();
        Document.Version = version;
        DocumentNode = new XmlNode();
        DocumentNode.Kind = XmlNodeKind.Document;
        DocumentNode.OwnerDocument = Document;
        DocumentNode.Children = [];
        Document.DocumentNode = DocumentNode;
    }

    internal XmlNode CreateXmlEventElementNode(XmlEvent value, XmlNode? parent)
    {
        XmlNode result = CreateXmlEventNamedNode(value, XmlNodeKind.Element, parent);
        result.Attributes = CreateXmlEventAttributeNodes(value.Attributes, result);
        result.NamespaceBindings = value.NamespaceBindings == null ? [] : value.NamespaceBindings;
        result.Children = [];
        return result;
    }

    internal XmlNode CreateXmlEventNamedNode(XmlEvent value, XmlNodeKind kind, XmlNode? parent)
    {
        XmlNode result = new XmlNode();
        result.Kind = kind;
        result.Name = value.Name;
        result.Value = value.Value;
        result.Parent = parent;
        result.OwnerDocument = Document;
        result.Children = [];
        return result;
    }

    internal XmlNode[] CreateXmlEventAttributeNodes(XmlAttributeData[]? attributes, XmlNode owner)
    {
        XmlAttributeData[] source = attributes == null ? [] : attributes;
        XmlNode[] result = new XmlNode[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlAttributeData attribute = source[index];
            XmlNode node = new XmlNode();
            node.Kind = XmlNodeKind.Attribute;
            node.Name = attribute.Name;
            node.Value = attribute.Value;
            node.Parent = owner;
            node.OwnerDocument = Document;
            node.Children = [];
            result[index] = node;
            index = index + 1;
        }

        return result;
    }
}
