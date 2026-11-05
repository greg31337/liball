using System;
using System.Collections.Generic;

namespace xml;

internal class DtdRetainedValidator
{
    internal RetainedDtdDeclarationSource Source;
    internal DtdValidationContext ValidationContext;
    internal DtdContentValidator ContentValidator;
    internal DtdAttributeProcessor AttributeProcessor;
    internal DtdDocumentValidator DocumentValidator;
    internal List<string> EventElements;
    internal bool SawEventRoot;

    internal DtdRetainedValidator(XmlDocumentType documentType, XmlParserOptions options, bool standalone)
    {
        Guard.NotNull(documentType, "documentType");
        Guard.NotNull(options, "options");
        if (options.Version != XmlVersion.Xml10 && options.Version != XmlVersion.Xml11)
        {
            throw new ArgumentOutOfRangeException("options.Version");
        }

        Source = new RetainedDtdDeclarationSource(documentType, options.Version);
        ValidationContext = new DtdValidationContext(null, null, options.AccumulateValidationDiagnostics);
        ContentValidator = new DtdContentValidator(Source, ValidationContext);
        XmlAttributeValueNormalizer normalizer = new XmlAttributeValueNormalizer(options.Version);
        AttributeProcessor = new DtdAttributeProcessor(Source, normalizer, ValidationContext, true, standalone);
        DocumentValidator = new DtdDocumentValidator(Source, ValidationContext, Source.RetainedDtdQualifiedName(documentType.Name), standalone);
        EventElements = new List<string>();
        SawEventRoot = false;
    }

    internal XmlDiagnostic[] ValidateRetainedDtdTree(XmlDocument document)
    {
        Guard.NotNull(document, "document");
        XmlNode? root = document.DocumentElement;
        if (root == null || root.Kind != XmlNodeKind.Element)
        {
            throw new ArgumentException("The retained XML document does not have a document element.", "document");
        }

        ValidateRetainedDtdTreeElement(root);
        AttributeProcessor.CompleteDtdAttributeValidation();
        return ValidationContext.SnapshotDtdValidationDiagnostics();
    }

    internal void ValidateRetainedDtdTreeElement(XmlNode element)
    {
        string elementName = Source.RetainedDtdQualifiedName(element.Name);
        if (elementName.Length == 0)
        {
            throw new ArgumentException("A retained element has no qualified name.", "document");
        }

        int offset = RetainedDtdTreeNodeOffset(element);
        DocumentValidator.BeginDtdDocumentElement(elementName, offset, false);
        ContentValidator.BeginDtdElementContent(elementName, offset, false);
        AttributeProcessor.ValidateNormalizedDtdElementAttributes(elementName, CreateRetainedDtdTreeAttributes(element.Attributes), offset);
        XmlNode[] children = element.Children == null ? [] : element.Children;
        int index = 0;
        while (index < children.Length)
        {
            ValidateRetainedDtdTreeChild(children[index]);
            index = index + 1;
        }

        ContentValidator.EndDtdElementContent(elementName, offset);
        DocumentValidator.EndDtdDocumentElement(elementName);
    }

    internal void ValidateRetainedDtdTreeChild(XmlNode child)
    {
        if (child.Kind == XmlNodeKind.Element)
        {
            ValidateRetainedDtdTreeElement(child);
            return;
        }

        if (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData)
        {
            string value = child.Value == null ? "" : child.Value;
            int offset = RetainedDtdTreeNodeOffset(child);
            ContentValidator.ConsumeDtdCharacterContent(value, offset);
            DocumentValidator.ObserveDtdDocumentCharacters(value, offset);
            return;
        }

        if (child.Kind == XmlNodeKind.EntityReference)
        {
            XmlNode[] replacements = child.Children == null ? [] : child.Children;
            int index = 0;
            while (index < replacements.Length)
            {
                ValidateRetainedDtdTreeChild(replacements[index]);
                index = index + 1;
            }
        }
    }

    internal XmlAttributeData[] CreateRetainedDtdTreeAttributes(XmlNode[]? nodes)
    {
        XmlNode[] sourceNodes = nodes == null ? [] : nodes;
        XmlAttributeData[] attributes = new XmlAttributeData[sourceNodes.Length];
        int index = 0;
        while (index < sourceNodes.Length)
        {
            XmlNode node = sourceNodes[index];
            if (node.Kind != XmlNodeKind.Attribute)
            {
                throw new ArgumentException("A retained element attribute collection contains a non-attribute node.", "document");
            }

            XmlAttributeData attribute = new XmlAttributeData();
            attribute.Name = node.Name;
            attribute.Value = node.Value;
            attribute.RawValue = null;
            attribute.IsSpecified = true;
            attribute.Range = node.Range;
            attributes[index] = attribute;
            index = index + 1;
        }

        return attributes;
    }

    internal int RetainedDtdTreeNodeOffset(XmlNode node)
    {
        if (node.Range == null || node.Range.Start == null || node.Range.Start.CharacterOffset < 0)
        {
            return 0;
        }

        long offset = node.Range.Start.CharacterOffset;
        return offset > int.MaxValue ? int.MaxValue : (int)offset;
    }

    internal XmlDiagnostic[] ValidateRetainedDtdEventBatch(XmlEventBatch batch)
    {
        Guard.NotNull(batch, "events");
        XmlEvent[] sourceEvents = batch.Events == null ? [] : batch.Events;
        if (batch.Offset < 0 || batch.Count < 0 || batch.Offset > sourceEvents.Length - batch.Count)
        {
            throw new ArgumentOutOfRangeException("events");
        }

        int index = batch.Offset;
        int end = batch.Offset + batch.Count;
        while (index < end)
        {
            ValidateRetainedDtdEvent(sourceEvents[index]);
            index = index + 1;
        }

        if (EventElements.Count != 0)
        {
            throw new ArgumentException("The normalized XML event batch ends with open elements.", "events");
        }

        if (!SawEventRoot)
        {
            throw new ArgumentException("The normalized XML event batch has no document element.", "events");
        }

        AttributeProcessor.CompleteDtdAttributeValidation();
        return ValidationContext.SnapshotDtdValidationDiagnostics();
    }

    internal void ValidateRetainedDtdEvent(XmlEvent item)
    {
        if (item == null)
        {
            throw new ArgumentException("The normalized XML event batch contains a null event.", "events");
        }

        if (item.Kind == XmlEventKind.StartElement)
        {
            ValidateRetainedDtdStartElementEvent(item);
            return;
        }

        if (item.Kind == XmlEventKind.EndElement)
        {
            ValidateRetainedDtdEndElementEvent(item);
            return;
        }

        if (item.Kind == XmlEventKind.Text || item.Kind == XmlEventKind.IgnorableWhitespace || item.Kind == XmlEventKind.CData)
        {
            string value = item.Value == null ? "" : item.Value;
            int offset = RetainedDtdEventOffset(item);
            ContentValidator.ConsumeDtdCharacterContent(value, offset);
            DocumentValidator.ObserveDtdDocumentCharacters(value, offset);
        }
    }

    internal void ValidateRetainedDtdStartElementEvent(XmlEvent item)
    {
        string elementName = Source.RetainedDtdQualifiedName(item.Name);
        if (elementName.Length == 0)
        {
            throw new ArgumentException("A normalized start-element event has no qualified name.", "events");
        }

        if (EventElements.Count == 0)
        {
            if (SawEventRoot)
            {
                throw new ArgumentException("The normalized XML event batch contains more than one document element.", "events");
            }

            SawEventRoot = true;
        }

        int offset = RetainedDtdEventOffset(item);
        DocumentValidator.BeginDtdDocumentElement(elementName, offset, false);
        ContentValidator.BeginDtdElementContent(elementName, offset, false);
        AttributeProcessor.ValidateNormalizedDtdElementAttributes(elementName, item.Attributes == null ? [] : item.Attributes, offset);
        EventElements.Add(elementName);
    }

    internal void ValidateRetainedDtdEndElementEvent(XmlEvent item)
    {
        string elementName = Source.RetainedDtdQualifiedName(item.Name);
        if (EventElements.Count == 0 || elementName != EventElements[EventElements.Count - 1])
        {
            throw new ArgumentException("The normalized end-element event does not match an open element.", "events");
        }

        int offset = RetainedDtdEventOffset(item);
        ContentValidator.EndDtdElementContent(elementName, offset);
        DocumentValidator.EndDtdDocumentElement(elementName);
        EventElements.RemoveAt(EventElements.Count - 1);
    }

    internal int RetainedDtdEventOffset(XmlEvent item)
    {
        if (item.Range == null || item.Range.Start == null || item.Range.Start.CharacterOffset < 0)
        {
            return 0;
        }

        long offset = item.Range.Start.CharacterOffset;
        return offset > int.MaxValue ? int.MaxValue : (int)offset;
    }
}
