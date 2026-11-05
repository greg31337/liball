using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class XmlTreeAssembler
{
    internal XmlLineNormalizationResult Source;
    internal SourceTracker Tracker;
    internal XmlParserOptions Options;
    internal ParserWorkLimiter WorkLimiter;
    internal bool Fragment;
    internal XmlDocument Document;
    internal XmlNode Container;
    internal XmlScanner Scanner;
    internal XmlElementTagParser ElementParser;
    internal EntityExpander EntityExpander;
    internal DtdValidationContext ValidationContext;
    internal DtdContentValidator? ContentValidator;
    internal DtdAttributeProcessor? AttributeProcessor;
    internal DtdDocumentValidator? DocumentValidator;
    internal NamespaceScopeProcessor? NamespaceProcessor;
    internal ParserEventQueue EventQueue;
    internal XmlDocumentType? EventDocumentType;
    internal XmlEvent[] DtdDeclarationEvents;
    internal XmlAttributeValueNormalizer AttributeNormalizer;
    internal IdIndex IdentifierIndex;
    internal List<XmlNode> OpenElements;
    internal long NextIdentity;

    internal XmlTreeAssembler(XmlLineNormalizationResult source, SourceTracker tracker, XmlParserOptions options, ParserWorkLimiter workLimiter, XmlVersion version, IXmlErrorReporter? errorReporter, bool fragment, XmlFragmentContext? fragmentContext, IXmlGeneralEntitySource? entitySource, IXmlEntityReplacementScanSource? replacementScanSource, IXmlExternalGeneralEntitySource? externalEntitySource, IDtdContentModelSource? contentModelSource, IDtdAttributeDeclarationSource? attributeDeclarationSource, IDtdDocumentDeclarationSource? documentDeclarationSource, XmlStandalone standalone, string? documentTypeName, bool skipUnresolvedEntities, ParserEventQueue eventQueue, XmlDocumentType? eventDocumentType, XmlEvent[] dtdDeclarationEvents)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(tracker, "tracker");
        Guard.NotNull(options, "options");
        Guard.NotNull(workLimiter, "workLimiter");
        Guard.NotNull(eventQueue, "eventQueue");
        Guard.NotNull(dtdDeclarationEvents, "dtdDeclarationEvents");
        Source = source;
        Tracker = tracker;
        Options = options;
        WorkLimiter = workLimiter;
        EventQueue = eventQueue;
        EventDocumentType = eventDocumentType;
        DtdDeclarationEvents = dtdDeclarationEvents;
        Fragment = fragment;
        Document = new XmlDocument();
        Scanner = new XmlScanner(source.Text.AsMemory(), version, fragment ? XmlEncodingEntityKind.ExternalParsedEntity : XmlEncodingEntityKind.Document);
        Scanner.ConfigureXmlScannerErrors(tracker, errorReporter);
        Scanner.ConfigureXmlScannerWork(workLimiter);
        ElementParser = new XmlElementTagParser(Scanner, true, null);
        ElementParser.ConfigureXmlElementWork(workLimiter);
        XmlEntityHandling treeEntityHandling = options.PreserveEntityReferences ? XmlEntityHandling.PreserveReferences : options.EntityHandling;
        EntityExpander = new EntityExpander(version, treeEntityHandling, skipUnresolvedEntities, entitySource);
        EntityExpander.ConfigureXmlEntityReplacementScanSource(replacementScanSource);
        EntityExpander.ConfigureXmlEntityErrors(tracker, errorReporter);
        EntityExpander.ConfigureXmlEntityWork(workLimiter, externalEntitySource);
        ValidationContext = new DtdValidationContext(tracker, errorReporter, options.AccumulateValidationDiagnostics);
        ContentValidator = null;
        if (options.ValidationMode == XmlValidationMode.Dtd && contentModelSource != null)
        {
            ContentValidator = new DtdContentValidator(contentModelSource, ValidationContext);
        }

        DocumentValidator = null;
        IXmlEntityReferenceObserver? wellFormednessObserver = null;
        if (documentDeclarationSource != null)
        {
            wellFormednessObserver = new DtdEntityWellFormednessObserver(documentDeclarationSource, tracker, errorReporter, standalone == XmlStandalone.Yes);
            EntityExpander.ConfigureXmlEntityReferenceObserver(wellFormednessObserver);
        }

        if (options.ValidationMode == XmlValidationMode.Dtd && documentDeclarationSource != null)
        {
            DocumentValidator = new DtdDocumentValidator(documentDeclarationSource, ValidationContext, documentTypeName, standalone == XmlStandalone.Yes);
        }

        AttributeNormalizer = new XmlAttributeValueNormalizer(version);
        IdentifierIndex = new IdIndex();
        AttributeNormalizer.ConfigureXmlAttributeEntities(entitySource, workLimiter, tracker, errorReporter);
        AttributeNormalizer.ConfigureXmlAttributeEntityObserver(wellFormednessObserver);
        AttributeProcessor = null;
        if (attributeDeclarationSource != null)
        {
            AttributeProcessor = new DtdAttributeProcessor(attributeDeclarationSource, AttributeNormalizer, ValidationContext, options.ValidationMode == XmlValidationMode.Dtd, standalone == XmlStandalone.Yes);
        }

        NamespaceProcessor = null;
        if (options.NamespaceHandling == XmlNamespaceHandling.Namespaces10 || options.NamespaceHandling == XmlNamespaceHandling.Namespaces11)
        {
            XmlNamespaceBinding[] inheritedBindings = fragmentContext == null ? [] : fragmentContext.NamespaceBindings;
            NamespaceProcessor = new NamespaceScopeProcessor(options.NamespaceHandling, errorReporter, inheritedBindings);
            ValidateXmlNamespaceDtdNames(documentDeclarationSource, attributeDeclarationSource);
        }

        OpenElements = new List<XmlNode>();
        NextIdentity = 1;
        Container = CreateXmlTreeNode(fragment ? XmlNodeKind.DocumentFragment : XmlNodeKind.Document, null, null, 0, source.Text.Length);
        Document.DocumentNode = Container;
        Document.MutationVersion = 0;
        Document.IsReadOnly = false;
        WorkLimiter.ConsumeXmlNodes(1);
    }

    internal XmlDocument AssembleXmlTree()
    {
        while (true)
        {
            XmlToken token = Scanner.ScanNextXmlToken();
            if (token.Kind == XmlTokenKind.EndOfInput)
            {
                break;
            }

            ProcessXmlTreeToken(token);
        }

        DtdAttributeProcessor? optionalAttributeProcessor = AttributeProcessor;
        if (optionalAttributeProcessor != null)
        {
            optionalAttributeProcessor.CompleteDtdAttributeValidation();
        }

        if (!Fragment && Options.ValidationMode == XmlValidationMode.Dtd && DocumentValidator == null)
        {
            ValidationContext.ReportDtdValidationError("A validating parse requires a document type declaration", 0);
        }

        return Document;
    }

    internal void ProcessXmlTreeToken(XmlToken token)
    {
        if (token.Kind == XmlTokenKind.XmlDeclaration || token.Kind == XmlTokenKind.TextDeclaration)
        {
            return;
        }

        if (token.Kind == XmlTokenKind.MarkupDeclarationOpen)
        {
            ProcessXmlDocumentTypeNode(token);
            return;
        }

        if (token.Kind == XmlTokenKind.StartTagOpen)
        {
            XmlElementTag tag = ElementParser.ParseXmlStartOrEmptyElementTag(token);
            ProcessXmlStartElementNode(tag);
            return;
        }

        if (token.Kind == XmlTokenKind.EndTagOpen)
        {
            XmlElementTag tag = ElementParser.ParseXmlEndElementTag(token);
            ProcessXmlEndElementNode(tag);
            return;
        }

        if (token.Kind == XmlTokenKind.EntityReference || token.Kind == XmlTokenKind.CharacterReference)
        {
            ProcessXmlReferenceNode(token);
            return;
        }

        if (token.Kind == XmlTokenKind.Text)
        {
            if (!Fragment && OpenElements.Count == 0)
            {
                return;
            }

            bool ignorableWhitespace = IsDtdIgnorableWhitespace(token.Value);
            ObserveDtdCharacterContent(token.Value, token.StartUtf16Offset);
            EventQueue.EnqueueXmlTokenEvent(token, OpenElements.Count, ignorableWhitespace);
            if (!ignorableWhitespace || Options.PreserveIgnorableWhitespace)
            {
                AddXmlValueNode(XmlNodeKind.Text, token.Value, token.StartUtf16Offset, token.Utf16Length);
            }

            return;
        }

        if (token.Kind == XmlTokenKind.CData)
        {
            bool ignorableWhitespace = IsDtdIgnorableWhitespace(token.Value);
            ObserveDtdCharacterContent(token.Value, token.StartUtf16Offset);
            DtdContentValidator? optionalCDataValidator = ContentValidator;
            if (optionalCDataValidator != null)
            {
                optionalCDataValidator.ConsumeDtdReferencedCharacterContent(token.Value == null ? "" : token.Value, token.StartUtf16Offset, true);
            }
            EventQueue.EnqueueXmlTokenEvent(token, OpenElements.Count, ignorableWhitespace);
            if (!ignorableWhitespace || Options.PreserveIgnorableWhitespace)
            {
                XmlNodeKind kind = Options.PreserveCData ? XmlNodeKind.CData : XmlNodeKind.Text;
                AddXmlValueNode(kind, token.Value, token.StartUtf16Offset, token.Utf16Length);
            }

            return;
        }

        if (token.Kind == XmlTokenKind.Comment)
        {
            ObserveDtdMarkupContent(token.StartUtf16Offset);
            EventQueue.EnqueueXmlTokenEvent(token, OpenElements.Count, false);
            if (Options.PreserveComments)
            {
                AddXmlValueNode(XmlNodeKind.Comment, token.Value, token.StartUtf16Offset, token.Utf16Length);
            }

            return;
        }

        if (token.Kind == XmlTokenKind.ProcessingInstruction)
        {
            NamespaceScopeProcessor? optionalNamespaceProcessor = NamespaceProcessor;
            if (optionalNamespaceProcessor != null && token.Name != null && token.Name.IndexOf(':') >= 0)
            {
                throw optionalNamespaceProcessor.NameParser.CreateXmlQualifiedNameError("A processing-instruction target cannot contain ':' when namespace processing is enabled", XmlErrorCodes.MalformedMarkup, Tracker.CreateTrackedRange(token.StartUtf16Offset, token.StartUtf16Offset + token.Utf16Length));
            }

            ObserveDtdMarkupContent(token.StartUtf16Offset);
            EventQueue.EnqueueXmlTokenEvent(token, OpenElements.Count, false);
            if (Options.PreserveProcessingInstructions)
            {
                XmlNode processingInstruction = CreateXmlTreeNode(XmlNodeKind.ProcessingInstruction, CreateXmlTreeName(token.Name == null ? "" : token.Name), token.Value, token.StartUtf16Offset, token.Utf16Length);
                AppendXmlTreeChild(CurrentXmlTreeParent(), processingInstruction);
                WorkLimiter.ConsumeXmlNodes(1);
            }
        }
    }

    internal void ProcessXmlStartElementNode(XmlElementTag tag)
    {
        DtdDocumentValidator? optionalDocumentValidator = DocumentValidator;
        if (optionalDocumentValidator != null)
        {
            optionalDocumentValidator.BeginDtdDocumentElement(tag.Name, tag.StartUtf16Offset, tag.IsEmptyElement);
        }

        DtdContentValidator? optionalValidator = ContentValidator;
        if (optionalValidator != null)
        {
            optionalValidator.BeginDtdElementContent(tag.Name, tag.StartUtf16Offset, tag.IsEmptyElement);
        }

        XmlAttributeData[] sourceAttributes = CreateXmlTreeAttributeData(tag.Attributes);
        DtdAttributeProcessor? optionalAttributeProcessor = AttributeProcessor;
        if (optionalAttributeProcessor != null)
        {
            sourceAttributes = optionalAttributeProcessor.ProcessDtdElementAttributes(tag.Name, sourceAttributes, tag.StartUtf16Offset);
            WorkLimiter.CheckXmlAttributeCount(sourceAttributes.Length);
            ValidateXmlNamespaceDtdAttributeValues(tag.Name, sourceAttributes, optionalAttributeProcessor);
        }

        NamespaceScopeProcessor? optionalProcessor = NamespaceProcessor;
        XmlName elementName;
        XmlAttributeData[] selectedAttributes;
        XmlNamespaceBinding[] declarations;
        if (optionalProcessor == null)
        {
            elementName = CreateXmlTreeName(tag.Name);
            selectedAttributes = sourceAttributes;
            declarations = [];
        }
        else
        {
            SourceRange range = Tracker.CreateTrackedRange(tag.StartUtf16Offset, tag.StartUtf16Offset + tag.Utf16Length);
            NamespaceElementResolution resolution = optionalProcessor.ProcessXmlNamespaceStart(CreateXmlTreeSourceName(tag.Name), sourceAttributes, range);
            elementName = PreserveXmlTreeName(resolution.Name);
            selectedAttributes = Options.RetainNamespaceDeclarations ? resolution.Attributes : resolution.OrdinaryAttributes;
            declarations = resolution.Declarations;
        }

        NormalizeXmlTreeIdentifierAttributes(selectedAttributes);

        XmlNode element = CreateXmlTreeNode(XmlNodeKind.Element, elementName, null, tag.StartUtf16Offset, tag.Utf16Length);
        if (optionalProcessor != null)
        {
            element.NamespaceBindings = declarations;
        }

        element.Attributes = CreateXmlTreeAttributesFromData(selectedAttributes, element);
        AppendXmlTreeChild(CurrentXmlTreeParent(), element);
        WorkLimiter.ConsumeXmlNodes((long)selectedAttributes.Length + 1);
        SourceRange eventRange = Tracker.CreateTrackedRange(tag.StartUtf16Offset, tag.StartUtf16Offset + tag.Utf16Length);
        EventQueue.EnqueueXmlResolvedElementStart(elementName, selectedAttributes, declarations, tag.Depth, tag.IsEmptyElement, eventRange);
        if (tag.IsEmptyElement)
        {
            EventQueue.EnqueueXmlResolvedElementEnd(elementName, declarations, tag.Depth, true, eventRange);
        }

        if (!Fragment && Document.DocumentElement == null)
        {
            Document.DocumentElement = element;
        }

        if (!tag.IsEmptyElement)
        {
            OpenElements.Add(element);
        }
        else if (optionalProcessor != null)
        {
            optionalProcessor.ResolveXmlNamespaceEnd(CreateXmlTreeSourceName(tag.Name), element.Range);
            optionalProcessor.PopXmlNamespaceScope();
        }
    }

    internal void ProcessXmlEndElementNode(XmlElementTag tag)
    {
        DtdContentValidator? optionalValidator = ContentValidator;
        if (optionalValidator != null)
        {
            optionalValidator.EndDtdElementContent(tag.Name, tag.StartUtf16Offset);
        }

        DtdDocumentValidator? optionalDocumentValidator = DocumentValidator;
        if (optionalDocumentValidator != null)
        {
            optionalDocumentValidator.EndDtdDocumentElement(tag.Name);
        }

        if (OpenElements.Count == 0)
        {
            return;
        }

        int index = OpenElements.Count - 1;
        XmlNode element = OpenElements[index];
        NamespaceScopeProcessor? optionalProcessor = NamespaceProcessor;
        XmlNamespaceBinding[] declarations = [];
        if (optionalProcessor != null)
        {
            SourceRange range = Tracker.CreateTrackedRange(tag.StartUtf16Offset, tag.StartUtf16Offset + tag.Utf16Length);
            declarations = optionalProcessor.CurrentXmlNamespaceDeclarations();
            optionalProcessor.ResolveXmlNamespaceEnd(CreateXmlTreeSourceName(tag.Name), range);
            optionalProcessor.PopXmlNamespaceScope();
        }

        XmlName eventName = element.Name == null ? CreateXmlTreeName(tag.Name) : element.Name;
        SourceRange eventRange = Tracker.CreateTrackedRange(tag.StartUtf16Offset, tag.StartUtf16Offset + tag.Utf16Length);
        EventQueue.EnqueueXmlResolvedElementEnd(eventName, declarations, tag.Depth, false, eventRange);
        OpenElements.RemoveAt(index);
        if (Options.PreserveSourceLocations && element.Range != null)
        {
            element.Range.End = Tracker.TrackedLocationAt(tag.StartUtf16Offset + tag.Utf16Length);
        }
    }

    internal void ProcessXmlReferenceNode(XmlToken token)
    {
        XmlEntityDispatchResult dispatch = EntityExpander.DispatchXmlReference(token);
        if (dispatch.Skipped && Options.ValidationMode == XmlValidationMode.Dtd)
        {
            ValidationContext.ReportDtdValidationError("The general entity '" + (dispatch.Name == null ? "" : dispatch.Name) + "' is not declared", token.StartUtf16Offset);
        }

        if (!Options.PreserveEntityReferences && Options.EntityHandling == XmlEntityHandling.Expand && HasXmlEntityReplacementMarkup(dispatch))
        {
            ProcessXmlEntityReplacementMarkup(dispatch, token.StartUtf16Offset, token.Utf16Length);
            return;
        }

        bool ignorableWhitespace = IsDtdEntityDispatchIgnorableWhitespace(dispatch);
        ObserveDtdReferencedContent(dispatch, token.StartUtf16Offset, token.Kind == XmlTokenKind.CharacterReference);
        ValidateDtdEntityDispatchContent(dispatch, token.StartUtf16Offset);
        SourceRange eventRange = Tracker.CreateTrackedRange(token.StartUtf16Offset, token.StartUtf16Offset + token.Utf16Length);
        EventQueue.EnqueueXmlEntityDispatchContext(dispatch, OpenElements.Count, ignorableWhitespace, eventRange);
        if ((Options.PreserveEntityReferences || Options.EntityHandling == XmlEntityHandling.PreserveReferences) && token.Kind == XmlTokenKind.EntityReference)
        {
            AddXmlPreservedEntityDispatch(dispatch, token, ignorableWhitespace);
            return;
        }

        int index = 0;
        while (index < dispatch.Items.Length)
        {
            XmlEntityDispatchItem item = dispatch.Items[index];
            if ((item.Kind == XmlEntityDispatchKind.Character || item.Kind == XmlEntityDispatchKind.ReplacementText) && item.Value != null && (!ignorableWhitespace || Options.PreserveIgnorableWhitespace))
            {
                AddXmlValueNode(XmlNodeKind.Text, item.Value, token.StartUtf16Offset, token.Utf16Length);
            }

            index = index + 1;
        }
    }

    internal bool HasXmlEntityReplacementMarkup(XmlEntityDispatchResult dispatch)
    {
        StringBuilder replacement = new StringBuilder();
        int index = 0;
        while (index < dispatch.Items.Length)
        {
            XmlEntityDispatchItem item = dispatch.Items[index];
            if (item.Kind == XmlEntityDispatchKind.ReplacementText && item.Value != null)
            {
                replacement.Append(item.Value);
            }
            else if (item.Kind == XmlEntityDispatchKind.Character)
            {
                replacement.Append("&#x");
                replacement.Append(item.ScalarValue.ToString("X"));
                replacement.Append(';');
            }

            index = index + 1;
        }

        return replacement.ToString().IndexOf('<') >= 0;
    }

    internal void ProcessXmlEntityReplacementMarkup(XmlEntityDispatchResult dispatch, int sourceOffset, int sourceLength)
    {
        StringBuilder replacement = new StringBuilder();
        int itemIndex = 0;
        while (itemIndex < dispatch.Items.Length)
        {
            XmlEntityDispatchItem item = dispatch.Items[itemIndex];
            if (item.Value != null && item.Value.IndexOf('<') >= 0)
            {
                replacement.Append(item.Value);
            }
            else if (item.Kind == XmlEntityDispatchKind.ReplacementText && item.Value != null)
            {
                replacement.Append(item.Value);
            }
            else if (item.Kind == XmlEntityDispatchKind.Character)
            {
                replacement.Append("&#x");
                replacement.Append(item.ScalarValue.ToString("X"));
                replacement.Append(';');
            }

            itemIndex = itemIndex + 1;
        }

        XmlScanner replacementScanner = new XmlScanner(replacement.ToString().AsMemory(), Scanner.CurrentXmlScannerVersion(), XmlEncodingEntityKind.ExternalParsedEntity);
        replacementScanner.ConfigureXmlScannerWork(WorkLimiter);
        XmlElementTagParser replacementElementParser = new XmlElementTagParser(replacementScanner, true, null);
        replacementElementParser.ConfigureXmlElementWork(WorkLimiter);
        int initialDepth = OpenElements.Count;
        while (true)
        {
            XmlToken item = replacementScanner.ScanNextXmlToken();
            if (item.Kind == XmlTokenKind.EndOfInput)
            {
                if (replacementElementParser.CurrentXmlElementDepth() != 0 || OpenElements.Count != initialDepth)
                {
                    throw new FormatException("An entity replacement ended before all of its elements were closed.");
                }

                return;
            }

            if (item.Kind == XmlTokenKind.StartTagOpen)
            {
                XmlElementTag tag = replacementElementParser.ParseXmlStartOrEmptyElementTag(item);
                tag.StartUtf16Offset = sourceOffset;
                tag.Utf16Length = sourceLength;
                ProcessXmlStartElementNode(tag);
            }
            else if (item.Kind == XmlTokenKind.EndTagOpen)
            {
                XmlElementTag tag = replacementElementParser.ParseXmlEndElementTag(item);
                tag.StartUtf16Offset = sourceOffset;
                tag.Utf16Length = sourceLength;
                ProcessXmlEndElementNode(tag);
            }
            else if (item.Kind == XmlTokenKind.XmlDeclaration || item.Kind == XmlTokenKind.TextDeclaration)
            {
                throw new FormatException("An entity replacement cannot contain an XML or text declaration in document content.");
            }
            else
            {
                item.StartUtf16Offset = sourceOffset;
                item.Utf16Length = sourceLength;
                ProcessXmlTreeToken(item);
            }
        }
    }

    internal void AddXmlPreservedEntityDispatch(XmlEntityDispatchResult dispatch, XmlToken token, bool ignorableWhitespace)
    {
        XmlNode baseParent = CurrentXmlTreeParent();
        List<XmlNode> entityParents = new List<XmlNode>();
        int index = 0;
        while (index < dispatch.Items.Length)
        {
            XmlEntityDispatchItem item = dispatch.Items[index];
            XmlNode parent = entityParents.Count == 0 ? baseParent : entityParents[entityParents.Count - 1];
            if (item.Kind == XmlEntityDispatchKind.StartEntity)
            {
                XmlNode reference = CreateXmlTreeNode(XmlNodeKind.EntityReference, CreateXmlTreeName(item.Name == null ? "" : item.Name), null, token.StartUtf16Offset, token.Utf16Length);
                AppendXmlTreeChild(parent, reference);
                WorkLimiter.ConsumeXmlNodes(1);
                entityParents.Add(reference);
            }
            else if (item.Kind == XmlEntityDispatchKind.EndEntity)
            {
                if (entityParents.Count > 0)
                {
                    entityParents.RemoveAt(entityParents.Count - 1);
                }
            }
            else if ((item.Kind == XmlEntityDispatchKind.Character || item.Kind == XmlEntityDispatchKind.ReplacementText) && item.Value != null && (!ignorableWhitespace || Options.PreserveIgnorableWhitespace))
            {
                XmlNode replacement = CreateXmlTreeNode(XmlNodeKind.Text, null, item.Value, token.StartUtf16Offset, token.Utf16Length);
                AppendXmlTreeChild(parent, replacement);
                WorkLimiter.ConsumeXmlNodes(1);
            }

            index = index + 1;
        }
    }

    internal void ValidateDtdEntityDispatchContent(XmlEntityDispatchResult dispatch, int sourceOffset)
    {
        DtdContentValidator? optionalValidator = ContentValidator;
        StringBuilder replacement = new StringBuilder();
        int itemIndex = 0;
        while (itemIndex < dispatch.Items.Length)
        {
            XmlEntityDispatchItem item = dispatch.Items[itemIndex];
            if (item.Kind == XmlEntityDispatchKind.ReplacementText && item.Value != null)
            {
                replacement.Append(item.Value);
            }
            else if (item.Kind == XmlEntityDispatchKind.Character)
            {
                replacement.Append("&#x");
                replacement.Append(item.ScalarValue.ToString("X"));
                replacement.Append(';');
            }

            itemIndex = itemIndex + 1;
        }

        if (replacement.Length == 0)
        {
            return;
        }

        XmlScanner replacementScanner = new XmlScanner(replacement.ToString().AsMemory(), Scanner.CurrentXmlScannerVersion(), XmlEncodingEntityKind.ExternalParsedEntity);
        XmlElementTagParser replacementElementParser = new XmlElementTagParser(replacementScanner, true, null);
        while (true)
        {
            XmlToken item = replacementScanner.ScanNextXmlToken();
            if (item.Kind == XmlTokenKind.EndOfInput)
            {
                if (replacementElementParser.CurrentXmlElementDepth() != 0)
                {
                    throw new FormatException("An entity replacement ended before all of its elements were closed.");
                }

                return;
            }

            if (item.Kind == XmlTokenKind.StartTagOpen)
            {
                XmlElementTag tag = replacementElementParser.ParseXmlStartOrEmptyElementTag(item);
                DtdDocumentValidator? optionalDocumentValidator = DocumentValidator;
                if (optionalDocumentValidator != null)
                {
                    optionalDocumentValidator.BeginDtdDocumentElement(tag.Name, sourceOffset, tag.IsEmptyElement);
                }

                DtdAttributeProcessor? optionalAttributeProcessor = AttributeProcessor;
                if (optionalAttributeProcessor != null)
                {
                    XmlAttributeData[] attributes = CreateXmlTreeAttributeData(tag.Attributes);
                    attributes = optionalAttributeProcessor.ProcessDtdElementAttributes(tag.Name, attributes, sourceOffset);
                    WorkLimiter.CheckXmlAttributeCount(attributes.Length);
                }

                if (optionalValidator != null)
                {
                    optionalValidator.BeginDtdElementContent(tag.Name, sourceOffset, tag.IsEmptyElement);
                }
            }
            else if (item.Kind == XmlTokenKind.EndTagOpen)
            {
                XmlElementTag tag = replacementElementParser.ParseXmlEndElementTag(item);
                if (optionalValidator != null)
                {
                    optionalValidator.EndDtdElementContent(tag.Name, sourceOffset);
                }
                DtdDocumentValidator? optionalDocumentValidator = DocumentValidator;
                if (optionalDocumentValidator != null)
                {
                    optionalDocumentValidator.EndDtdDocumentElement(tag.Name);
                }
            }
            else if (item.Kind == XmlTokenKind.Text || item.Kind == XmlTokenKind.CData || item.Kind == XmlTokenKind.CharacterReference)
            {
                if (optionalValidator != null)
                {
                    optionalValidator.ConsumeDtdCharacterContent(item.Value == null ? "" : item.Value, sourceOffset);
                }
                DtdDocumentValidator? optionalDocumentValidator = DocumentValidator;
                if (optionalDocumentValidator != null)
                {
                    optionalDocumentValidator.ObserveDtdDocumentCharacters(item.Value == null ? "" : item.Value, sourceOffset);
                }
            }
            else if (item.Kind == XmlTokenKind.EntityReference)
            {
                throw new FormatException("An entity replacement contains an unresolved nested entity reference.");
            }
            else if (item.Kind == XmlTokenKind.MarkupDeclarationOpen || item.Kind == XmlTokenKind.XmlDeclaration || item.Kind == XmlTokenKind.TextDeclaration)
            {
                throw new FormatException("An entity replacement contains markup that is not permitted in element content.");
            }
        }
    }

    internal void ObserveDtdReferencedContent(XmlEntityDispatchResult dispatch, int offset, bool syntacticCharacterReference)
    {
        DtdContentValidator? optionalValidator = ContentValidator;
        if (optionalValidator == null)
        {
            return;
        }

        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < dispatch.Items.Length)
        {
            XmlEntityDispatchItem item = dispatch.Items[index];
            if ((item.Kind == XmlEntityDispatchKind.Character || item.Kind == XmlEntityDispatchKind.ReplacementText) && item.Value != null)
            {
                value.Append(item.Value);
            }

            if (item.Kind == XmlEntityDispatchKind.Character && item.Name == null)
            {
                syntacticCharacterReference = true;
            }

            index = index + 1;
        }

        optionalValidator.ConsumeDtdReferencedCharacterContent(value.ToString(), offset, syntacticCharacterReference);
    }

    internal void ObserveDtdMarkupContent(int offset)
    {
        DtdContentValidator? optionalValidator = ContentValidator;
        if (optionalValidator != null)
        {
            optionalValidator.ConsumeDtdMarkupContent(offset);
        }
    }

    internal void ValidateXmlNamespaceDtdNames(IDtdDocumentDeclarationSource? documentSource, IDtdAttributeDeclarationSource? attributeSource)
    {
        NamespaceScopeProcessor? optionalProcessor = NamespaceProcessor;
        if (optionalProcessor == null)
        {
            return;
        }

        if (documentSource != null)
        {
            XmlEntityDeclaration[] entities = documentSource.FindAllDtdDocumentEntityDeclarations();
            int entityIndex = 0;
            while (entityIndex < entities.Length)
            {
                string? optionalName = entities[entityIndex].Name;
                if (optionalName != null && optionalName.IndexOf(':') >= 0)
                {
                    throw optionalProcessor.NameParser.CreateXmlQualifiedNameError("An entity name cannot contain ':' when namespace processing is enabled", XmlErrorCodes.MalformedMarkup, null);
                }

                entityIndex = entityIndex + 1;
            }

            XmlNotationDeclaration[] notations = documentSource.FindAllDtdDocumentNotationDeclarations();
            int notationIndex = 0;
            while (notationIndex < notations.Length)
            {
                string? optionalName = notations[notationIndex].Name;
                if (optionalName != null && optionalName.IndexOf(':') >= 0)
                {
                    throw optionalProcessor.NameParser.CreateXmlQualifiedNameError("A notation name cannot contain ':' when namespace processing is enabled", XmlErrorCodes.MalformedMarkup, null);
                }

                notationIndex = notationIndex + 1;
            }
        }

        if (attributeSource != null && Options.ValidationMode == XmlValidationMode.Dtd)
        {
            XmlAttributeDeclaration[] declarations = attributeSource.FindAllDtdAttributeDeclarations();
            int attributeIndex = 0;
            while (attributeIndex < declarations.Length)
            {
                XmlAttributeDeclaration declaration = declarations[attributeIndex];
                string? optionalName = declaration.Name == null ? null : declaration.Name.QualifiedName;
                if (declaration.TypeName == "ID" && optionalName != null && optionalName.IndexOf(':') >= 0)
                {
                    ValidationContext.ReportDtdValidationError("An ID attribute name cannot contain ':' when namespace processing is enabled", 0);
                }

                attributeIndex = attributeIndex + 1;
            }
        }
    }

    internal void ValidateXmlNamespaceDtdAttributeValues(string elementName, XmlAttributeData[] attributes, DtdAttributeProcessor processor)
    {
        if (NamespaceProcessor == null || Options.ValidationMode != XmlValidationMode.Dtd)
        {
            return;
        }

        int index = 0;
        while (index < attributes.Length)
        {
            XmlAttributeData attribute = attributes[index];
            string attributeName = attribute.Name == null || attribute.Name.QualifiedName == null ? "" : attribute.Name.QualifiedName;
            XmlAttributeDeclaration? declaration = processor.Source.FindDtdAttributeDeclaration(elementName, attributeName);
            string typeName = declaration == null || declaration.TypeName == null ? "" : declaration.TypeName;
            if (typeName == "ID" || typeName == "IDREF" || typeName == "IDREFS")
            {
                string value = attribute.Value == null ? "" : attribute.Value;
                string[] names = value.Split(' ');
                int nameIndex = 0;
                while (nameIndex < names.Length)
                {
                    if (names[nameIndex].IndexOf(':') >= 0)
                    {
                        SourceRange? range = attribute.Range;
                        int offset = range == null || range.Start == null ? 0 : (int)range.Start.CharacterOffset;
                        ValidationContext.ReportDtdValidationError("Namespace-aware ID and IDREF values must be NCNames", offset);
                    }

                    nameIndex = nameIndex + 1;
                }
            }

            index = index + 1;
        }
    }

    internal void ProcessXmlDocumentTypeNode(XmlToken opening)
    {
        XmlDocumentTypeInfo information = new XmlDocumentTypeParser(Scanner).ParseXmlDocumentTypeDeclaration(opening);
        XmlDocumentType? optionalDocumentType = EventDocumentType;
        if (optionalDocumentType != null)
        {
            EventQueue.EnqueueXmlDocumentTypeEvents(optionalDocumentType, DtdDeclarationEvents);
        }

        if (Options.DtdProcessing == XmlDtdProcessing.Parse)
        {
            XmlNode documentType = CreateXmlTreeNode(XmlNodeKind.DocumentType, CreateXmlTreeName(information.Name), null, information.StartUtf16Offset, information.Utf16Length);
            AppendXmlTreeChild(CurrentXmlTreeParent(), documentType);
            WorkLimiter.ConsumeXmlNodes(1);
        }
    }

    internal XmlAttributeData[] CreateXmlTreeAttributeData(XmlRawAttribute[] rawAttributes)
    {
        XmlAttributeData[] attributes = new XmlAttributeData[rawAttributes.Length];
        int index = 0;
        while (index < rawAttributes.Length)
        {
            XmlRawAttribute raw = rawAttributes[index];
            XmlAttributeData attribute = new XmlAttributeData();
            attribute.Name = NamespaceProcessor == null ? CreateXmlTreeName(raw.Name) : CreateXmlTreeSourceName(raw.Name);
            attribute.Value = AttributeProcessor == null ? AttributeNormalizer.NormalizeXmlAttributeValue(raw.RawValue) : raw.RawValue;
            if (string.Equals(raw.Name, "xml:id", StringComparison.Ordinal))
            {
                attribute.Value = IdentifierIndex.CollapseXmlIdentifierWhitespace(attribute.Value);
            }

            attribute.RawValue = raw.RawValue;
            attribute.Quote = raw.Quote;
            attribute.IsSpecified = true;
            attribute.IsDefaulted = false;
            attribute.IsFixed = false;
            attribute.IsNamespaceDeclaration = false;
            attribute.Range = Tracker.CreateTrackedRange(raw.StartUtf16Offset, raw.StartUtf16Offset + raw.Utf16Length);
            attributes[index] = attribute;
            index = index + 1;
        }

        return attributes;
    }

    internal XmlNode[] CreateXmlTreeAttributesFromData(XmlAttributeData[] sourceAttributes, XmlNode element)
    {
        XmlNode[] attributes = new XmlNode[sourceAttributes.Length];
        int index = 0;
        while (index < sourceAttributes.Length)
        {
            XmlAttributeData source = sourceAttributes[index];
            XmlName? name = source.Name;
            if (name != null)
            {
                name = PreserveXmlTreeName(name);
            }

            int start = source.Range == null || source.Range.Start == null ? 0 : (int)source.Range.Start.CharacterOffset;
            int end = source.Range == null || source.Range.End == null ? start : (int)source.Range.End.CharacterOffset;
            XmlNode attribute = CreateXmlTreeNode(XmlNodeKind.Attribute, name, source.Value, start, end - start);
            attribute.Parent = element;
            attributes[index] = attribute;
            index = index + 1;
        }

        return attributes;
    }

    internal void AddXmlValueNode(XmlNodeKind kind, string? value, int start, int length)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }

        XmlNode parent = CurrentXmlTreeParent();
        if (kind == XmlNodeKind.Text && parent.Children != null && parent.Children.Length > 0)
        {
            XmlNode previous = parent.Children[parent.Children.Length - 1];
            if (previous.Kind == XmlNodeKind.Text)
            {
                previous.Value = (previous.Value == null ? "" : previous.Value) + value;
                if (Options.PreserveSourceLocations && previous.Range != null)
                {
                    previous.Range.End = Tracker.TrackedLocationAt(start + length);
                }

                WorkLimiter.CheckXmlTextLength(previous.Value);
                return;
            }
        }

        WorkLimiter.CheckXmlTextLength(value);
        XmlNode node = CreateXmlTreeNode(kind, null, value, start, length);
        AppendXmlTreeChild(parent, node);
        WorkLimiter.ConsumeXmlNodes(1);
    }

    internal void ObserveDtdCharacterContent(string? value, int offset)
    {
        DtdContentValidator? optionalValidator = ContentValidator;
        if (optionalValidator != null && value != null)
        {
            optionalValidator.ConsumeDtdCharacterContent(value, offset);
        }

        DtdDocumentValidator? optionalDocumentValidator = DocumentValidator;
        if (optionalDocumentValidator != null && value != null)
        {
            optionalDocumentValidator.ObserveDtdDocumentCharacters(value, offset);
        }
    }

    internal bool IsDtdIgnorableWhitespace(string? value)
    {
        DtdContentValidator? optionalValidator = ContentValidator;
        return optionalValidator != null && optionalValidator.IsDtdIgnorableWhitespace(value);
    }

    internal bool IsDtdEntityDispatchIgnorableWhitespace(XmlEntityDispatchResult dispatch)
    {
        DtdContentValidator? optionalValidator = ContentValidator;
        if (optionalValidator == null)
        {
            return false;
        }

        StringBuilder text = new StringBuilder();
        int index = 0;
        while (index < dispatch.Items.Length)
        {
            XmlEntityDispatchItem item = dispatch.Items[index];
            if ((item.Kind == XmlEntityDispatchKind.Character || item.Kind == XmlEntityDispatchKind.ReplacementText) && item.Value != null)
            {
                text.Append(item.Value);
            }
            else if (item.Kind == XmlEntityDispatchKind.SkippedEntity)
            {
                return false;
            }

            index = index + 1;
        }

        return optionalValidator.IsDtdIgnorableWhitespace(text.ToString());
    }

    internal XmlDiagnostic[] SnapshotDtdValidationDiagnostics()
    {
        return ValidationContext.SnapshotDtdValidationDiagnostics();
    }

    internal XmlNode CurrentXmlTreeParent()
    {
        if (OpenElements.Count == 0)
        {
            return Container;
        }

        return OpenElements[OpenElements.Count - 1];
    }

    internal void AppendXmlTreeChild(XmlNode parent, XmlNode child)
    {
        int count = parent.Children == null ? 0 : parent.Children.Length;
        XmlNode[] children = new XmlNode[count + 1];
        if (parent.Children != null)
        {
            Array.Copy(parent.Children, children, count);
        }

        children[count] = child;
        parent.Children = children;
        child.Parent = parent;
    }

    internal XmlNode CreateXmlTreeNode(XmlNodeKind kind, XmlName? name, string? value, int start, int length)
    {
        XmlNode node = new XmlNode();
        node.Kind = kind;
        node.Name = name;
        node.Value = value;
        node.OwnerDocument = Document;
        node.Children = [];
        node.Identity = NextIdentity;
        node.MutationVersion = 0;
        node.IsReadOnly = false;
        NextIdentity = NextIdentity + 1;
        if (Options.PreserveSourceLocations)
        {
            node.Range = Tracker.CreateTrackedRange(start, start + length);
        }

        return node;
    }

    internal XmlName CreateXmlTreeName(string qualifiedName)
    {
        return PreserveXmlTreeName(CreateXmlTreeSourceName(qualifiedName));
    }

    internal void NormalizeXmlTreeIdentifierAttributes(XmlAttributeData[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            XmlAttributeData attribute = attributes[index];
            if (IdentifierIndex.IsXmlIdName(attribute.Name))
            {
                attribute.Value = IdentifierIndex.CollapseXmlIdentifierWhitespace(attribute.Value);
            }

            index = index + 1;
        }
    }

    internal XmlName CreateXmlTreeSourceName(string qualifiedName)
    {
        XmlName name = new XmlName();
        name.QualifiedName = qualifiedName;
        int colon = qualifiedName.IndexOf(':');
        if (colon > 0 && colon < qualifiedName.Length - 1)
        {
            name.LocalName = qualifiedName.Substring(colon + 1);
            name.Prefix = qualifiedName.Substring(0, colon);
        }
        else
        {
            name.LocalName = qualifiedName;
            name.Prefix = null;
        }

        return name;
    }

    internal XmlName PreserveXmlTreeName(XmlName name)
    {
        Guard.NotNull(name, "name");
        if (!Options.PreservePrefixes)
        {
            name.Prefix = null;
            name.QualifiedName = name.LocalName;
        }

        return name;
    }
}
