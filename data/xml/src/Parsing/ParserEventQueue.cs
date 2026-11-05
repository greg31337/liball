using System;
using System.Collections.Generic;

namespace xml;

internal class ParserEventQueue
{
    internal Queue<XmlEvent> Events;
    internal bool PreserveCData;
    internal bool PreserveEntityReferences;
    internal bool PreserveComments;
    internal bool PreserveProcessingInstructions;
    internal bool PreservePrefixes;
    internal bool PreserveIgnorableWhitespace;
    internal bool RetainNamespaceDeclarations;
    internal bool PreserveSourceLocations;
    internal XmlNamespaceHandling NamespaceHandling;
    internal NamespaceScopeProcessor? NamespaceProcessor;
    internal XmlAttributeValueNormalizer AttributeNormalizer;
    internal SourceTracker? Tracker;

    internal ParserEventQueue(XmlParserOptions options)
    {
        Guard.NotNull(options, "options");
        Events = new Queue<XmlEvent>();
        PreserveCData = options.PreserveCData;
        PreserveEntityReferences = options.PreserveEntityReferences || options.EntityHandling == XmlEntityHandling.PreserveReferences;
        PreserveComments = options.PreserveComments;
        PreserveProcessingInstructions = options.PreserveProcessingInstructions;
        PreservePrefixes = options.PreservePrefixes;
        PreserveIgnorableWhitespace = options.PreserveIgnorableWhitespace;
        RetainNamespaceDeclarations = options.RetainNamespaceDeclarations;
        PreserveSourceLocations = options.PreserveSourceLocations;
        NamespaceHandling = options.NamespaceHandling;
        NamespaceProcessor = null;
        AttributeNormalizer = new XmlAttributeValueNormalizer(options.Version);
        Tracker = null;
        if (NamespaceHandling == XmlNamespaceHandling.Namespaces10 || NamespaceHandling == XmlNamespaceHandling.Namespaces11)
        {
            XmlNamespaceBinding[] inheritedBindings = !options.ParseFragment || options.FragmentNamespaceBindings == null ? [] : options.FragmentNamespaceBindings;
            NamespaceProcessor = new NamespaceScopeProcessor(NamespaceHandling, null, inheritedBindings);
        }
    }

    internal void ConfigureXmlEventLocations(SourceTracker tracker)
    {
        Guard.NotNull(tracker, "tracker");
        Tracker = tracker;
    }

    internal void EnqueueXmlDocumentBoundary(XmlEventKind kind, int offset)
    {
        if (kind != XmlEventKind.StartDocument && kind != XmlEventKind.EndDocument)
        {
            throw new ArgumentException("A document boundary event must start or end a document.", "kind");
        }

        XmlEvent item = new XmlEvent();
        item.Kind = kind;
        item.NodeKind = XmlNodeKind.Document;
        item.Range = CreateXmlEventRange(offset, 0);
        Events.Enqueue(item);
    }

    internal void EnqueueXmlDocumentTypeEvents(XmlDocumentType documentType, XmlEvent[] declarationEvents)
    {
        Guard.NotNull(documentType, "documentType");
        Guard.NotNull(declarationEvents, "declarationEvents");
        XmlEvent start = new XmlEvent();
        start.Kind = XmlEventKind.StartDtd;
        start.NodeKind = XmlNodeKind.DocumentType;
        start.Name = CopyXmlEventName(documentType.Name);
        start.PublicIdentifier = documentType.PublicIdentifier;
        start.SystemIdentifier = documentType.SystemIdentifier;
        start.Range = PreserveSourceLocations ? documentType.Range : null;
        Events.Enqueue(start);

        int index = 0;
        while (index < declarationEvents.Length)
        {
            XmlEvent? optionalDeclaration = declarationEvents[index];
            if (optionalDeclaration == null)
            {
                throw new ArgumentException("A DTD declaration event cannot be null.", "declarationEvents");
            }

            Events.Enqueue(CopyXmlDtdDeclarationEvent(optionalDeclaration));
            index = index + 1;
        }

        XmlEvent end = new XmlEvent();
        end.Kind = XmlEventKind.EndDtd;
        end.NodeKind = XmlNodeKind.DocumentType;
        end.Name = CopyXmlEventName(documentType.Name);
        end.PublicIdentifier = documentType.PublicIdentifier;
        end.SystemIdentifier = documentType.SystemIdentifier;
        end.Range = PreserveSourceLocations ? documentType.Range : null;
        Events.Enqueue(end);
    }

    internal void EnqueueXmlResolvedElementStart(XmlName name, XmlAttributeData[] attributes, XmlNamespaceBinding[] declarations, int depth, bool emptyElement, SourceRange? range)
    {
        Guard.NotNull(name, "name");
        Guard.NotNull(attributes, "attributes");
        Guard.NotNull(declarations, "declarations");
        Guard.NonNegativeInt(depth, "depth");
        XmlEvent item = new XmlEvent();
        item.Kind = XmlEventKind.StartElement;
        item.NodeKind = XmlNodeKind.Element;
        item.Name = CopyXmlEventName(name);
        item.Attributes = CopyXmlEventAttributes(attributes);
        item.Depth = depth;
        item.IsEmptyElement = emptyElement;
        item.Range = PreserveSourceLocations ? range : null;
        if (NamespaceHandling == XmlNamespaceHandling.Namespaces10 || NamespaceHandling == XmlNamespaceHandling.Namespaces11)
        {
            EnqueueXmlNamespaceStartEvents(item, declarations);
        }
        else
        {
            Events.Enqueue(item);
        }
    }

    internal void EnqueueXmlResolvedElementEnd(XmlName name, XmlNamespaceBinding[] declarations, int depth, bool emptyElement, SourceRange? range)
    {
        Guard.NotNull(name, "name");
        Guard.NotNull(declarations, "declarations");
        Guard.NonNegativeInt(depth, "depth");
        XmlEvent item = new XmlEvent();
        item.Kind = XmlEventKind.EndElement;
        item.NodeKind = XmlNodeKind.Element;
        item.Name = CopyXmlEventName(name);
        item.Depth = depth;
        item.IsEmptyElement = emptyElement;
        item.Range = PreserveSourceLocations ? range : null;
        if (NamespaceHandling == XmlNamespaceHandling.Namespaces10 || NamespaceHandling == XmlNamespaceHandling.Namespaces11)
        {
            EnqueueXmlNamespaceEndEvents(item, declarations);
        }
        else
        {
            Events.Enqueue(item);
        }
    }

    internal void EnqueueXmlDiagnosticEvent(XmlDiagnostic diagnostic)
    {
        Guard.NotNull(diagnostic, "diagnostic");
        XmlEvent item = new XmlEvent();
        item.Kind = XmlEventKind.Diagnostic;
        item.NodeKind = XmlNodeKind.Document;
        item.Diagnostic = diagnostic;
        item.Range = diagnostic.Range;
        Events.Enqueue(item);
    }

    internal void EnqueueXmlElementTag(XmlElementTag tag)
    {
        Guard.NotNull(tag, "tag");
        if (NamespaceProcessor != null)
        {
            EnqueueNamespaceAwareXmlElementTag(tag);
            return;
        }

        if (tag.IsEndElement)
        {
            Events.Enqueue(CreateXmlElementEvent(tag, true));
            return;
        }

        Events.Enqueue(CreateXmlElementEvent(tag, false));
        if (tag.IsEmptyElement)
        {
            Events.Enqueue(CreateXmlElementEvent(tag, true));
        }
    }

    internal void EnqueueNamespaceAwareXmlElementTag(XmlElementTag tag)
    {
        NamespaceScopeProcessor? optionalProcessor = NamespaceProcessor;
        if (optionalProcessor == null)
        {
            throw new InvalidOperationException("Namespace processing is not enabled.");
        }

        SourceRange? range = null;
        if (tag.IsEndElement)
        {
            XmlEvent endElement = CreateXmlNamespaceSourceElementEvent(tag, true);
            endElement.Name = PreserveXmlEventName(optionalProcessor.ResolveXmlNamespaceEnd(endElement.Name == null ? new XmlName() : endElement.Name, range));
            XmlNamespaceBinding[] declarations = optionalProcessor.CurrentXmlNamespaceDeclarations();
            EnqueueXmlNamespaceEndEvents(endElement, declarations);
            optionalProcessor.PopXmlNamespaceScope();
            return;
        }

        XmlEvent startElement = CreateXmlNamespaceSourceElementEvent(tag, false);
        XmlAttributeData[] attributes = startElement.Attributes == null ? [] : startElement.Attributes;
        NamespaceElementResolution resolution = optionalProcessor.ProcessXmlNamespaceStart(startElement.Name == null ? new XmlName() : startElement.Name, attributes, range);
        startElement.Name = PreserveXmlEventName(resolution.Name);
        startElement.Attributes = PreserveXmlEventAttributes(RetainNamespaceDeclarations ? resolution.Attributes : resolution.OrdinaryAttributes);
        EnqueueXmlNamespaceStartEvents(startElement, resolution.Declarations);
        if (tag.IsEmptyElement)
        {
            XmlEvent endElement = CreateXmlNamespaceSourceElementEvent(tag, true);
            endElement.Name = PreserveXmlEventName(optionalProcessor.ResolveXmlNamespaceEnd(endElement.Name == null ? new XmlName() : endElement.Name, range));
            EnqueueXmlNamespaceEndEvents(endElement, resolution.Declarations);
            optionalProcessor.PopXmlNamespaceScope();
        }
    }

    internal XmlEvent CreateXmlNamespaceSourceElementEvent(XmlElementTag tag, bool endElement)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = endElement ? XmlEventKind.EndElement : XmlEventKind.StartElement;
        item.NodeKind = XmlNodeKind.Element;
        item.Name = CreateXmlSourceEventName(tag.Name);
        item.Depth = tag.Depth;
        item.IsEmptyElement = tag.IsEmptyElement;
        item.Range = CreateXmlEventRange(tag.StartUtf16Offset, tag.Utf16Length);
        if (!endElement)
        {
            item.Attributes = CreateXmlNamespaceSourceAttributes(tag.Attributes);
        }

        return item;
    }

    internal XmlAttributeData[] CreateXmlNamespaceSourceAttributes(XmlRawAttribute[] rawAttributes)
    {
        XmlAttributeData[] attributes = new XmlAttributeData[rawAttributes.Length];
        int index = 0;
        while (index < rawAttributes.Length)
        {
            XmlRawAttribute raw = rawAttributes[index];
            XmlAttributeData attribute = new XmlAttributeData();
            attribute.Name = CreateXmlSourceEventName(raw.Name);
            attribute.Value = AttributeNormalizer.NormalizeXmlAttributeValue(raw.RawValue);
            attribute.RawValue = raw.RawValue;
            attribute.Quote = raw.Quote;
            attribute.IsSpecified = true;
            attribute.Range = CreateXmlEventRange(raw.StartUtf16Offset, raw.Utf16Length);
            attributes[index] = attribute;
            index = index + 1;
        }

        return attributes;
    }

    internal XmlAttributeData[] PreserveXmlEventAttributes(XmlAttributeData[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null)
            {
                attributes[index].Name = PreserveXmlEventName(name);
            }

            index = index + 1;
        }

        return attributes;
    }

    internal XmlName PreserveXmlEventName(XmlName name)
    {
        Guard.NotNull(name, "name");
        if (!PreservePrefixes)
        {
            name.Prefix = null;
            name.QualifiedName = name.LocalName;
        }

        return name;
    }

    internal void EnqueueXmlTokenEvent(XmlToken token, int depth, bool ignorableWhitespace)
    {
        Guard.NotNull(token, "token");
        Guard.NonNegativeInt(depth, "depth");
        if (token.Kind == XmlTokenKind.Text)
        {
            if (ignorableWhitespace && !PreserveIgnorableWhitespace)
            {
                return;
            }

            XmlEvent text = CreateXmlValueEvent(ignorableWhitespace ? XmlEventKind.IgnorableWhitespace : XmlEventKind.Text, XmlNodeKind.Text, token.Value, depth);
            text.IsIgnorableWhitespace = ignorableWhitespace;
            text.Range = CreateXmlEventRange(token.StartUtf16Offset, token.Utf16Length);
            Events.Enqueue(text);
            return;
        }

        if (token.Kind == XmlTokenKind.CData)
        {
            if (ignorableWhitespace && !PreserveIgnorableWhitespace)
            {
                return;
            }

            XmlEventKind eventKind = PreserveCData ? XmlEventKind.CData : ignorableWhitespace ? XmlEventKind.IgnorableWhitespace : XmlEventKind.Text;
            XmlNodeKind nodeKind = PreserveCData ? XmlNodeKind.CData : XmlNodeKind.Text;
            XmlEvent cdata = CreateXmlValueEvent(eventKind, nodeKind, token.Value, depth);
            cdata.IsIgnorableWhitespace = ignorableWhitespace;
            cdata.Range = CreateXmlEventRange(token.StartUtf16Offset, token.Utf16Length);
            Events.Enqueue(cdata);
            return;
        }

        if (token.Kind == XmlTokenKind.Comment)
        {
            if (PreserveComments)
            {
                XmlEvent comment = CreateXmlValueEvent(XmlEventKind.Comment, XmlNodeKind.Comment, token.Value, depth);
                comment.Range = CreateXmlEventRange(token.StartUtf16Offset, token.Utf16Length);
                Events.Enqueue(comment);
            }

            return;
        }

        if (token.Kind == XmlTokenKind.ProcessingInstruction)
        {
            if (PreserveProcessingInstructions)
            {
                XmlEvent processingInstruction = CreateXmlValueEvent(XmlEventKind.ProcessingInstruction, XmlNodeKind.ProcessingInstruction, token.Value, depth);
                processingInstruction.Name = CreateXmlEventName(token.Name == null ? "" : token.Name);
                processingInstruction.Range = CreateXmlEventRange(token.StartUtf16Offset, token.Utf16Length);
                Events.Enqueue(processingInstruction);
            }

            return;
        }

        throw new ArgumentException("The token cannot be converted directly to a parser event.", "token");
    }

    internal void EnqueueXmlEntityDispatch(XmlEntityDispatchResult dispatch, int depth)
    {
        EnqueueXmlEntityDispatchContext(dispatch, depth, false, null);
    }

    internal void EnqueueXmlEntityDispatchContext(XmlEntityDispatchResult dispatch, int depth, bool ignorableWhitespace, SourceRange? range)
    {
        Guard.NotNull(dispatch, "dispatch");
        Guard.NonNegativeInt(depth, "depth");
        bool hasStartBoundary = HasXmlEntityStartBoundary(dispatch);
        if (PreserveEntityReferences && dispatch.Resolved && dispatch.Name != null && !hasStartBoundary)
        {
            XmlEvent start = CreateXmlEntityEvent(XmlEventKind.StartEntity, dispatch.Name, null, depth);
            start.Range = PreserveSourceLocations ? range : null;
            Events.Enqueue(start);
        }

        int index = 0;
        while (index < dispatch.Items.Length)
        {
            XmlEntityDispatchItem item = dispatch.Items[index];
            if (item.Kind == XmlEntityDispatchKind.Character || item.Kind == XmlEntityDispatchKind.ReplacementText)
            {
                if (!ignorableWhitespace || PreserveIgnorableWhitespace)
                {
                    XmlEventKind kind = ignorableWhitespace ? XmlEventKind.IgnorableWhitespace : XmlEventKind.Text;
                    XmlEvent text = CreateXmlEntityEvent(kind, item.Name, item.Value, depth);
                    text.IsIgnorableWhitespace = ignorableWhitespace;
                    text.Range = PreserveSourceLocations ? range : null;
                    Events.Enqueue(text);
                }
            }
            else if (item.Kind == XmlEntityDispatchKind.StartEntity && PreserveEntityReferences)
            {
                XmlEvent start = CreateXmlEntityEvent(XmlEventKind.StartEntity, item.Name, null, depth);
                start.Range = PreserveSourceLocations ? range : null;
                Events.Enqueue(start);
            }
            else if (item.Kind == XmlEntityDispatchKind.EndEntity && PreserveEntityReferences)
            {
                XmlEvent end = CreateXmlEntityEvent(XmlEventKind.EndEntity, item.Name, null, depth);
                end.Range = PreserveSourceLocations ? range : null;
                Events.Enqueue(end);
            }
            else if (item.Kind == XmlEntityDispatchKind.SkippedEntity)
            {
                XmlEvent skipped = CreateXmlEntityEvent(XmlEventKind.SkippedEntity, item.Name, null, depth);
                skipped.Range = PreserveSourceLocations ? range : null;
                Events.Enqueue(skipped);
            }

            index = index + 1;
        }

        if (PreserveEntityReferences && dispatch.Resolved && dispatch.Name != null && !hasStartBoundary)
        {
            XmlEvent end = CreateXmlEntityEvent(XmlEventKind.EndEntity, dispatch.Name, null, depth);
            end.Range = PreserveSourceLocations ? range : null;
            Events.Enqueue(end);
        }
    }

    internal void EnqueueXmlNamespaceStartEvents(XmlEvent startElement, XmlNamespaceBinding[] declarations)
    {
        ValidateXmlNamespaceEventInputs(startElement, declarations, true);
        XmlNamespaceBinding[] copiedDeclarations = CopyXmlNamespaceBindings(declarations);
        startElement.NamespaceBindings = CopyXmlNamespaceBindings(declarations);
        int index = 0;
        while (index < copiedDeclarations.Length)
        {
            Events.Enqueue(CreateXmlNamespaceEvent(XmlEventKind.StartNamespace, copiedDeclarations[index], startElement.Depth));
            index = index + 1;
        }

        Events.Enqueue(startElement);
    }

    internal void EnqueueXmlNamespaceEndEvents(XmlEvent endElement, XmlNamespaceBinding[] declarations)
    {
        ValidateXmlNamespaceEventInputs(endElement, declarations, false);
        XmlNamespaceBinding[] copiedDeclarations = CopyXmlNamespaceBindings(declarations);
        Events.Enqueue(endElement);
        int index = copiedDeclarations.Length - 1;
        while (index >= 0)
        {
            Events.Enqueue(CreateXmlNamespaceEvent(XmlEventKind.EndNamespace, copiedDeclarations[index], endElement.Depth));
            index = index - 1;
        }
    }

    internal void ValidateXmlNamespaceEventInputs(XmlEvent element, XmlNamespaceBinding[] declarations, bool start)
    {
        Guard.NotNull(element, "element");
        Guard.NotNull(declarations, "declarations");
        if (NamespaceHandling != XmlNamespaceHandling.Namespaces10 && NamespaceHandling != XmlNamespaceHandling.Namespaces11)
        {
            throw new InvalidOperationException("Namespace events require namespace processing to be enabled.");
        }

        XmlEventKind requiredKind = start ? XmlEventKind.StartElement : XmlEventKind.EndElement;
        if (element.Kind != requiredKind)
        {
            throw new ArgumentException("The element event kind does not match the requested namespace boundary.", "element");
        }

        Guard.NonNegativeInt(element.Depth, "element.Depth");
        int index = 0;
        while (index < declarations.Length)
        {
            XmlNamespaceBinding? optionalBinding = declarations[index];
            if (optionalBinding == null || optionalBinding.Prefix == null || optionalBinding.NamespaceUri == null)
            {
                throw new ArgumentException("A namespace event requires a complete namespace binding.", "declarations");
            }

            bool undeclaration = optionalBinding.IsUndeclaration || optionalBinding.NamespaceUri.Length == 0;
            if (NamespaceHandling == XmlNamespaceHandling.Namespaces10 && optionalBinding.Prefix.Length > 0 && undeclaration)
            {
                throw new ArgumentException("Namespaces in XML 1.0 cannot emit a prefixed namespace undeclaration.", "declarations");
            }

            index = index + 1;
        }
    }

    internal XmlEvent CreateXmlNamespaceEvent(XmlEventKind kind, XmlNamespaceBinding binding, int depth)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = kind;
        item.NodeKind = XmlNodeKind.Namespace;
        item.NamespaceBindings = CopyXmlNamespaceBindings([binding]);
        item.Value = binding.NamespaceUri;
        item.Depth = depth;
        item.Range = PreserveSourceLocations ? binding.Range : null;
        return item;
    }

    internal XmlNamespaceBinding[] CopyXmlNamespaceBindings(XmlNamespaceBinding[] bindings)
    {
        XmlNamespaceBinding[] copies = new XmlNamespaceBinding[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            XmlNamespaceBinding source = bindings[index];
            XmlNamespaceBinding copy = new XmlNamespaceBinding();
            copy.Prefix = source.Prefix;
            copy.NamespaceUri = source.NamespaceUri;
            copy.IsUndeclaration = source.IsUndeclaration || source.NamespaceUri == "";
            copy.Range = PreserveSourceLocations ? source.Range : null;
            copies[index] = copy;
            index = index + 1;
        }

        return copies;
    }

    internal bool HasXmlEntityStartBoundary(XmlEntityDispatchResult dispatch)
    {
        int index = 0;
        while (index < dispatch.Items.Length)
        {
            if (dispatch.Items[index].Kind == XmlEntityDispatchKind.StartEntity)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int CountQueuedXmlEvents()
    {
        return Events.Count;
    }

    internal XmlEvent? DequeueXmlEvent()
    {
        if (Events.Count == 0)
        {
            return null;
        }

        return Events.Dequeue();
    }

    internal XmlEvent[] SnapshotXmlEvents()
    {
        return Events.ToArray();
    }

    internal XmlEventBatch CreateXmlEventBatch(int maximumCount, XmlParserStatus status, bool isFinal)
    {
        Guard.Positive(maximumCount, "maximumCount");
        List<XmlEvent> batchEvents = new List<XmlEvent>();
        while (batchEvents.Count < maximumCount && Events.Count > 0)
        {
            batchEvents.Add(Events.Dequeue());
        }

        XmlEventBatch batch = new XmlEventBatch();
        batch.Events = batchEvents.ToArray();
        batch.Offset = 0;
        batch.Count = batchEvents.Count;
        batch.Status = status;
        batch.IsFinal = isFinal && Events.Count == 0;
        return batch;
    }

    internal void ClearParserEvents()
    {
        Events.Clear();
        if (NamespaceProcessor != null)
        {
            NamespaceProcessor.ResetXmlNamespaceScopes();
        }
    }

    internal XmlEvent CreateXmlElementEvent(XmlElementTag tag, bool endElement)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = endElement ? XmlEventKind.EndElement : XmlEventKind.StartElement;
        item.NodeKind = XmlNodeKind.Element;
        item.Name = CreateXmlEventName(tag.Name);
        item.Depth = tag.Depth;
        item.IsEmptyElement = tag.IsEmptyElement;
        item.Range = CreateXmlEventRange(tag.StartUtf16Offset, tag.Utf16Length);
        if (!endElement)
        {
            item.Attributes = CreateXmlEventAttributes(tag.Attributes);
        }

        return item;
    }

    internal XmlAttributeData[] CreateXmlEventAttributes(XmlRawAttribute[] rawAttributes)
    {
        XmlAttributeData[] attributes = new XmlAttributeData[rawAttributes.Length];
        int index = 0;
        while (index < rawAttributes.Length)
        {
            XmlRawAttribute raw = rawAttributes[index];
            XmlAttributeData attribute = new XmlAttributeData();
            attribute.Name = CreateXmlEventName(raw.Name);
            attribute.Value = raw.RawValue;
            attribute.RawValue = raw.RawValue;
            attribute.Quote = raw.Quote;
            attribute.IsSpecified = true;
            attribute.Range = CreateXmlEventRange(raw.StartUtf16Offset, raw.Utf16Length);
            attributes[index] = attribute;
            index = index + 1;
        }

        return attributes;
    }

    internal XmlName CreateXmlEventName(string qualifiedName)
    {
        XmlName name = CreateXmlSourceEventName(qualifiedName);
        return PreserveXmlEventName(name);
    }

    internal XmlName CreateXmlSourceEventName(string qualifiedName)
    {
        XmlName name = new XmlName();
        int colon = qualifiedName.IndexOf(':');
        if (colon > 0 && colon < qualifiedName.Length - 1)
        {
            name.LocalName = qualifiedName.Substring(colon + 1);
            name.Prefix = qualifiedName.Substring(0, colon);
            name.QualifiedName = qualifiedName;
        }
        else
        {
            name.LocalName = qualifiedName;
            name.Prefix = null;
            name.QualifiedName = qualifiedName;
        }

        return name;
    }

    internal XmlEvent CreateXmlValueEvent(XmlEventKind kind, XmlNodeKind nodeKind, string? value, int depth)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = kind;
        item.NodeKind = nodeKind;
        item.Value = value;
        item.Depth = depth;
        return item;
    }

    internal XmlEvent CreateXmlEntityEvent(XmlEventKind kind, string? name, string? value, int depth)
    {
        bool textKind = kind == XmlEventKind.Text || kind == XmlEventKind.IgnorableWhitespace;
        XmlNodeKind nodeKind = textKind ? XmlNodeKind.Text : XmlNodeKind.EntityReference;
        XmlEvent item = CreateXmlValueEvent(kind, nodeKind, value, depth);
        if (!textKind && name != null)
        {
            item.Name = CreateXmlEventName(name);
        }

        return item;
    }

    internal XmlEvent CopyXmlDtdDeclarationEvent(XmlEvent source)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = source.Kind;
        item.NodeKind = XmlNodeKind.DocumentType;
        item.Name = CopyXmlEventName(source.Name);
        item.Value = source.Value;
        item.Range = PreserveSourceLocations ? source.Range : null;
        item.PublicIdentifier = source.PublicIdentifier;
        item.SystemIdentifier = source.SystemIdentifier;
        item.ElementDeclaration = source.ElementDeclaration;
        item.AttributeDeclaration = source.AttributeDeclaration;
        item.EntityDeclaration = source.EntityDeclaration;
        item.NotationDeclaration = source.NotationDeclaration;
        return item;
    }

    internal XmlName? CopyXmlEventName(XmlName? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlName copy = new XmlName();
        copy.Prefix = source.Prefix;
        copy.LocalName = source.LocalName;
        copy.NamespaceUri = source.NamespaceUri;
        copy.QualifiedName = source.QualifiedName;
        return copy;
    }

    internal XmlAttributeData[] CopyXmlEventAttributes(XmlAttributeData[] source)
    {
        XmlAttributeData[] copies = new XmlAttributeData[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlAttributeData attribute = source[index];
            XmlAttributeData copy = new XmlAttributeData();
            copy.Name = CopyXmlEventName(attribute.Name);
            copy.Value = attribute.Value;
            copy.RawValue = attribute.RawValue;
            copy.Quote = attribute.Quote;
            copy.IsSpecified = attribute.IsSpecified;
            copy.IsDefaulted = attribute.IsDefaulted;
            copy.IsFixed = attribute.IsFixed;
            copy.IsNamespaceDeclaration = attribute.IsNamespaceDeclaration;
            copy.Range = PreserveSourceLocations ? attribute.Range : null;
            copies[index] = copy;
            index = index + 1;
        }

        return copies;
    }

    internal SourceRange? CreateXmlEventRange(int start, int length)
    {
        SourceTracker? optionalTracker = Tracker;
        if (!PreserveSourceLocations || optionalTracker == null)
        {
            return null;
        }

        return optionalTracker.CreateTrackedRange(start, start + length);
    }
}
