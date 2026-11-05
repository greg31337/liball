using System;
using System.Collections.Generic;
using System.Xml;

namespace iso20022;

internal class XmiPassTwoState
{
    Iso20022Ecore2013Adapter adapter;
    XmiPassOneResult passOne;
    XmiPassTwoOptions options;
    XmiPassTwoResult result = new XmiPassTwoResult();
    HashSet<string> duplicateIdentifiers = new HashSet<string>(StringComparer.Ordinal);
    List<string> typeNames = new List<string>();
    List<string> identifiers = new List<string>();
    List<int> documentOrders = new List<int>();
    List<string> partitionIdentifiers = new List<string>();
    List<RepositoryEntityKind> partitionKinds = new List<RepositoryEntityKind>();
    int identifiedEntryIndex;
    int diagnosticCount;

    internal XmiPassTwoState(
        Iso20022Ecore2013Adapter adapter,
        XmiPassOneResult passOne,
        XmiPassTwoOptions options)
    {
        this.adapter = adapter;
        this.passOne = passOne;
        this.options = options;
        int duplicateIndex = 0;
        while (duplicateIndex < passOne.Duplicates.Count)
        {
            duplicateIdentifiers.Add(passOne.Duplicates[duplicateIndex].Identifier);
            duplicateIndex++;
        }
    }

    internal void RecordGraphElement(XmlReader reader, string sourceName)
    {
        int depth = reader.Depth;
        ValidateElementBounds(reader, depth);
        EnsureGraphDepth(depth);
        string parentTypeName = string.Empty;
        if (depth > 0)
        {
            parentTypeName = typeNames[depth - 1];
        }

        bool isElementAttribute = depth > 0 &&
            adapter.IsElementAttributeFeature(parentTypeName, reader.LocalName);
        string typeName = string.Empty;
        if (!isElementAttribute)
        {
            typeName = ResolveGraphTypeName(reader, depth, parentTypeName);
        }

        string sourceTypeName = ReadSourceTypeName(reader);
        string sourceTypeNamespaceUri = ResolveSourceTypeNamespace(reader, sourceTypeName);
        string identifier = ReadIdentifier(reader);
        ValidatePassOneEntry(identifier, typeName);
        RepositoryEntityKind kind = adapter.ResolveEntityKind(typeName);
        string partitionIdentifier = ResolveGraphPartitionIdentifier(depth);
        RepositoryEntityKind partitionKind = ResolveGraphPartitionKind(depth);
        if (depth == 1 && parentTypeName == "Repository" && adapter.IsTopLevelPartitionType(typeName))
        {
            partitionIdentifier = identifier;
            partitionKind = kind;
        }

        int parentDocumentOrder = -1;
        string parentIdentifier = string.Empty;
        if (depth > 0)
        {
            parentDocumentOrder = documentOrders[depth - 1];
            parentIdentifier = identifiers[depth - 1];
        }

        XmiPassTwoEntity entity = CreateGraphEntity(
            reader,
            sourceName,
            identifier,
            typeName,
            sourceTypeName,
            sourceTypeNamespaceUri,
            kind,
            parentDocumentOrder,
            parentIdentifier,
            partitionIdentifier,
            partitionKind,
            depth);
        result.Entities.Add(entity);
        typeNames[depth] = typeName;
        identifiers[depth] = identifier;
        documentOrders[depth] = entity.DocumentOrder;
        partitionIdentifiers[depth] = partitionIdentifier;
        partitionKinds[depth] = partitionKind;
        if (isElementAttribute)
        {
            RecordElementAttribute(entity);
            return;
        }

        if (depth > 0 && adapter.IsContainmentFeature(parentTypeName, reader.LocalName))
        {
            RecordContainment(entity, reader.LocalName);
        }

        ReadGraphAttributes(reader, entity);
    }

    internal void RecordGraphText(XmlReader reader)
    {
        int elementDepth = reader.Depth - 1;
        if (elementDepth < 0 || elementDepth >= documentOrders.Count)
        {
            return;
        }

        int documentOrder = documentOrders[elementDepth];
        if (documentOrder < 0 || documentOrder >= result.Entities.Count)
        {
            return;
        }

        XmiPassTwoEntity entity = result.Entities[documentOrder];
        XmiPassTwoAttribute? attribute = entity.ElementAttribute;
        if (entity.IsElementAttribute && attribute != null)
        {
            if (attribute.Value.Length + reader.Value.Length > options.MaximumAttributeValueLength)
            {
                throw new XmlException("An XMI attribute value exceeds MaximumAttributeValueLength.");
            }

            attribute.Value = attribute.Value + reader.Value;
            return;
        }

        if (entity.TextValue.Length + reader.Value.Length > options.MaximumAttributeValueLength)
        {
            throw new XmlException("An XMI element text value exceeds MaximumAttributeValueLength.");
        }

        entity.TextValue = entity.TextValue + reader.Value;
    }

    internal XmiPassTwoResult CompleteGraph()
    {
        if (result.Entities.Count != passOne.ElementCount ||
            identifiedEntryIndex != passOne.Entries.Count)
        {
            throw new XmlException("The XMI input changed between pass one and pass two.");
        }

        int entityIndex = 0;
        while (entityIndex < result.Entities.Count)
        {
            result.Entities[entityIndex].ContainmentPositions = null;
            entityIndex++;
        }

        return result;
    }

    internal void ValidateElementBounds(XmlReader reader, int depth)
    {
        if (depth + 1 > options.ReaderLimits.MaximumDepth)
        {
            throw new XmlException("The XMI element depth exceeds MaximumDepth.");
        }

        if (reader.AttributeCount > options.ReaderLimits.MaximumAttributesPerElement)
        {
            throw new XmlException("An XMI element exceeds MaximumAttributesPerElement.");
        }

        if (result.Entities.Count >= options.ReaderLimits.MaximumElementCount)
        {
            throw new XmlException("The XMI document exceeds MaximumElementCount.");
        }
    }

    internal void EnsureGraphDepth(int depth)
    {
        while (typeNames.Count <= depth)
        {
            typeNames.Add(string.Empty);
            identifiers.Add(string.Empty);
            documentOrders.Add(-1);
            partitionIdentifiers.Add(string.Empty);
            partitionKinds.Add(RepositoryEntityKind.Unknown);
        }

        int removeIndex = typeNames.Count - 1;
        while (removeIndex > depth)
        {
            typeNames.RemoveAt(removeIndex);
            identifiers.RemoveAt(removeIndex);
            documentOrders.RemoveAt(removeIndex);
            partitionIdentifiers.RemoveAt(removeIndex);
            partitionKinds.RemoveAt(removeIndex);
            removeIndex--;
        }
    }

    internal string ResolveGraphTypeName(XmlReader reader, int depth, string parentTypeName)
    {
        if (depth == 0)
        {
            if (reader.LocalName != "Repository" || reader.NamespaceURI != adapter.GetMetamodelNamespace())
            {
                throw new XmlException("The XMI root does not match the selected repository adapter.");
            }

            return "Repository";
        }

        string? xsiTypeValue = reader.GetAttribute("type", Iso20022Namespaces.XmlSchemaInstance);
        string xsiType = string.Empty;
        if (xsiTypeValue != null)
        {
            xsiType = xsiTypeValue;
        }

        string typeName = adapter.ResolveContainedType(parentTypeName, reader.LocalName, xsiType);
        if (typeName.Length == 0 && adapter.IsKnownType(reader.LocalName))
        {
            typeName = adapter.NormalizeTypeName(reader.LocalName);
        }

        return typeName;
    }

    internal string ReadIdentifier(XmlReader reader)
    {
        string? identifierValue = reader.GetAttribute("id", Iso20022Namespaces.Xmi);
        if (identifierValue == null)
        {
            return string.Empty;
        }

        return identifierValue;
    }

    internal string ReadSourceTypeName(XmlReader reader)
    {
        string? sourceType = reader.GetAttribute("type", Iso20022Namespaces.XmlSchemaInstance);
        if (sourceType == null)
        {
            return string.Empty;
        }

        return sourceType;
    }

    internal string ResolveSourceTypeNamespace(XmlReader reader, string sourceTypeName)
    {
        if (sourceTypeName.Length == 0)
        {
            return string.Empty;
        }

        string prefix = string.Empty;
        int separator = sourceTypeName.IndexOf(':');
        if (separator >= 0)
        {
            prefix = sourceTypeName.Substring(0, separator);
        }

        string? namespaceUri = reader.LookupNamespace(prefix);
        if (namespaceUri == null)
        {
            return string.Empty;
        }

        return namespaceUri;
    }

    internal void ValidatePassOneEntry(string identifier, string typeName)
    {
        if (identifier.Length == 0)
        {
            return;
        }

        if (identifiedEntryIndex >= passOne.Entries.Count)
        {
            throw new XmlException("The XMI input changed between pass one and pass two.");
        }

        XmiPassOneEntry expected = passOne.Entries[identifiedEntryIndex];
        if (expected.Identifier != identifier ||
            expected.DocumentOrder != result.Entities.Count ||
            expected.TypeName != typeName)
        {
            throw new XmlException("The XMI input changed between pass one and pass two.");
        }

        identifiedEntryIndex++;
    }

    internal string ResolveGraphPartitionIdentifier(int depth)
    {
        if (depth == 0)
        {
            return string.Empty;
        }

        return partitionIdentifiers[depth - 1];
    }

    internal RepositoryEntityKind ResolveGraphPartitionKind(int depth)
    {
        if (depth == 0)
        {
            return RepositoryEntityKind.Unknown;
        }

        return partitionKinds[depth - 1];
    }

    internal XmiPassTwoEntity CreateGraphEntity(
        XmlReader reader,
        string sourceName,
        string identifier,
        string typeName,
        string sourceTypeName,
        string sourceTypeNamespaceUri,
        RepositoryEntityKind kind,
        int parentDocumentOrder,
        string parentIdentifier,
        string partitionIdentifier,
        RepositoryEntityKind partitionKind,
        int depth)
    {
        XmiPassTwoEntity entity = new XmiPassTwoEntity();
        entity.Identifier = identifier;
        entity.TypeName = typeName;
        entity.SourceTypeName = sourceTypeName;
        entity.SourceTypeNamespaceUri = sourceTypeNamespaceUri;
        entity.Kind = kind;
        entity.ElementName.NamespaceUri = reader.NamespaceURI;
        entity.ElementName.LocalName = reader.LocalName;
        entity.ElementName.Prefix = reader.Prefix;
        entity.Location = CaptureGraphLocation(reader, sourceName);
        entity.DocumentOrder = result.Entities.Count;
        entity.Depth = depth;
        entity.ParentDocumentOrder = parentDocumentOrder;
        entity.ParentIdentifier = parentIdentifier;
        entity.TopLevelPartitionIdentifier = partitionIdentifier;
        entity.TopLevelPartitionKind = partitionKind;
        return entity;
    }

    internal void RecordContainment(XmiPassTwoEntity target, string featureName)
    {
        XmiPassTwoEntity source = result.Entities[target.ParentDocumentOrder];
        XmiPassTwoReference relationship = new XmiPassTwoReference();
        relationship.SourceIdentifier = source.Identifier;
        relationship.SourceKind = source.Kind;
        relationship.SourceDocumentOrder = source.DocumentOrder;
        relationship.PropertyName = featureName;
        relationship.Position = source.TakeContainmentPosition(featureName);
        relationship.TargetIdentifier = target.Identifier;
        relationship.RawToken = target.Identifier;
        relationship.TargetKind = target.Kind;
        relationship.TargetDocumentOrder = target.DocumentOrder;
        relationship.IsContainment = true;
        relationship.IsResolved = true;
        relationship.HasLineInformation = target.Location.HasLineInformation;
        relationship.LineNumber = target.Location.LineNumber;
        relationship.ColumnNumber = target.Location.ColumnNumber;
        AddRelationship(relationship);
        result.ContainmentCount++;
    }

    internal void RecordElementAttribute(XmiPassTwoEntity valueEntity)
    {
        XmiPassTwoEntity owner = result.Entities[valueEntity.ParentDocumentOrder];
        result.AttributeCount++;
        if (result.AttributeCount > options.MaximumAttributeCount)
        {
            throw new XmlException("The XMI attribute collection exceeds MaximumAttributeCount.");
        }

        int attributeOrder = 0;
        if (owner.Attributes.Count > 0)
        {
            attributeOrder = owner.Attributes[owner.Attributes.Count - 1].AttributeOrder + 1;
        }

        XmiPassTwoAttribute attribute = new XmiPassTwoAttribute();
        attribute.NamespaceUri = valueEntity.ElementName.NamespaceUri;
        attribute.LocalName = valueEntity.ElementName.LocalName;
        attribute.Prefix = valueEntity.ElementName.Prefix;
        attribute.AttributeOrder = attributeOrder;
        attribute.HasLineInformation = valueEntity.Location.HasLineInformation;
        attribute.LineNumber = valueEntity.Location.LineNumber;
        attribute.ColumnNumber = valueEntity.Location.ColumnNumber;
        owner.Attributes.Add(attribute);
        valueEntity.IsElementAttribute = true;
        valueEntity.ElementAttribute = attribute;
    }

    internal void ReadGraphAttributes(XmlReader reader, XmiPassTwoEntity entity)
    {
        bool hasAttribute = reader.MoveToFirstAttribute();
        int attributeOrder = 0;
        while (hasAttribute)
        {
            if (ShouldCaptureAttribute(reader))
            {
                RecordGraphAttribute(reader, entity, attributeOrder);
            }

            attributeOrder++;
            hasAttribute = reader.MoveToNextAttribute();
        }

        reader.MoveToElement();
    }

    internal bool ShouldCaptureAttribute(XmlReader reader)
    {
        return reader.NamespaceURI != Iso20022Namespaces.Xmlns &&
            reader.NamespaceURI != Iso20022Namespaces.Xmi &&
            reader.NamespaceURI != Iso20022Namespaces.XmlSchemaInstance;
    }

    internal void RecordGraphAttribute(
        XmlReader reader,
        XmiPassTwoEntity entity,
        int attributeOrder)
    {
        if (reader.Value.Length > options.MaximumAttributeValueLength)
        {
            throw new XmlException("An XMI attribute value exceeds MaximumAttributeValueLength.");
        }

        result.AttributeCount++;
        if (result.AttributeCount > options.MaximumAttributeCount)
        {
            throw new XmlException("The XMI attribute collection exceeds MaximumAttributeCount.");
        }

        XmiPassTwoAttribute attribute = new XmiPassTwoAttribute();
        attribute.NamespaceUri = reader.NamespaceURI;
        attribute.LocalName = reader.LocalName;
        attribute.Prefix = reader.Prefix;
        attribute.Value = reader.Value;
        attribute.AttributeOrder = attributeOrder;
        attribute.IsReference = reader.NamespaceURI.Length == 0 &&
            adapter.IsReferenceFeature(entity.TypeName, reader.LocalName);
        IXmlLineInfo lineInformation = (IXmlLineInfo)reader;
        if (lineInformation.HasLineInfo())
        {
            attribute.HasLineInformation = true;
            attribute.LineNumber = lineInformation.LineNumber;
            attribute.ColumnNumber = lineInformation.LinePosition;
        }

        entity.Attributes.Add(attribute);
        if (attribute.IsReference)
        {
            RecordAttributeReferences(entity, attribute);
        }
    }

    internal void RecordAttributeReferences(XmiPassTwoEntity source, XmiPassTwoAttribute attribute)
    {
        string[] tokens = attribute.Value.Split(
            new char[] { ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        int tokenIndex = 0;
        while (tokenIndex < tokens.Length)
        {
            XmiPassTwoReference relationship = ResolveAttributeReference(
                source,
                attribute,
                tokens[tokenIndex],
                tokenIndex);
            AddRelationship(relationship);
            tokenIndex++;
        }
    }

    internal XmiPassTwoReference ResolveAttributeReference(
        XmiPassTwoEntity source,
        XmiPassTwoAttribute attribute,
        string rawToken,
        int position)
    {
        string targetIdentifier = rawToken;
        if (targetIdentifier.StartsWith("#", StringComparison.Ordinal))
        {
            targetIdentifier = targetIdentifier.Substring(1);
        }

        XmiPassTwoReference relationship = new XmiPassTwoReference();
        relationship.SourceIdentifier = source.Identifier;
        relationship.SourceKind = source.Kind;
        relationship.SourceDocumentOrder = source.DocumentOrder;
        relationship.PropertyName = attribute.LocalName;
        relationship.Position = position;
        relationship.AttributeOrder = attribute.AttributeOrder;
        relationship.TargetIdentifier = targetIdentifier;
        relationship.RawToken = rawToken;
        relationship.RawValue = attribute.Value;
        relationship.HasLineInformation = attribute.HasLineInformation;
        relationship.LineNumber = attribute.LineNumber;
        relationship.ColumnNumber = attribute.ColumnNumber;
        XmiPassOneEntry? target = passOne.FindIdentifier(targetIdentifier);
        if (target == null)
        {
            RecordMissingReference(relationship);
            return relationship;
        }

        relationship.TargetKind = target.Kind;
        relationship.TargetDocumentOrder = target.DocumentOrder;
        if (duplicateIdentifiers.Contains(targetIdentifier))
        {
            RecordAmbiguousReference(relationship);
            return relationship;
        }

        relationship.IsResolved = true;
        result.ResolvedReferenceCount++;
        if (target.DocumentOrder > source.DocumentOrder)
        {
            relationship.IsForwardReference = true;
            result.ForwardReferenceCount++;
        }

        if (target.DocumentOrder < source.DocumentOrder)
        {
            relationship.IsBackwardReference = true;
            result.BackwardReferenceCount++;
        }

        return relationship;
    }

    internal void RecordMissingReference(XmiPassTwoReference relationship)
    {
        relationship.ErrorCode = "repository.xmi.reference_missing";
        relationship.ErrorMessage = "The XMI reference target is not declared in the repository artifact.";
        RecordGraphDiagnostic();
        result.UnresolvedReferenceCount++;
    }

    internal void RecordAmbiguousReference(XmiPassTwoReference relationship)
    {
        relationship.ErrorCode = "repository.xmi.reference_ambiguous";
        relationship.ErrorMessage = "The XMI reference target is ambiguous because its identifier is duplicated.";
        RecordGraphDiagnostic();
        result.UnresolvedReferenceCount++;
    }

    internal void RecordGraphDiagnostic()
    {
        diagnosticCount++;
        if (diagnosticCount > options.MaximumDiagnosticCount)
        {
            throw new XmlException("The XMI relationship diagnostics exceed MaximumDiagnosticCount.");
        }
    }

    internal void AddRelationship(XmiPassTwoReference relationship)
    {
        if (result.Relationships.Count >= options.MaximumRelationshipCount)
        {
            throw new XmlException("The XMI relationship graph exceeds MaximumRelationshipCount.");
        }

        result.Relationships.Add(relationship);
    }

    internal SourceLocation CaptureGraphLocation(XmlReader reader, string sourceName)
    {
        SourceLocation location = new SourceLocation();
        location.SourceName = sourceName;
        IXmlLineInfo lineInformation = (IXmlLineInfo)reader;
        if (lineInformation.HasLineInfo())
        {
            location.HasLineInformation = true;
            location.LineNumber = lineInformation.LineNumber;
            location.ColumnNumber = lineInformation.LinePosition;
        }

        return location;
    }
}
