using System;
using System.Xml;

namespace iso20022;

internal static class SchemaXsdReader
{
    internal const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

    internal static void ReadSchema(byte[] content, SchemaXsdReadState state)
    {
        MessageReadOptions readerOptions = new MessageReadOptions();
        readerOptions.Limits = state.Options.Limits;
        readerOptions.PreserveWhitespace = false;
        using (BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromBytes(
            content,
            readerOptions))
        {
            while (state.Success && reader.ReadNext())
            {
                state.Options.CancellationToken.ThrowIfCancellationRequested();
                XmlNodeType nodeType = reader.GetNodeType();
                if (nodeType == XmlNodeType.Element)
                {
                    ReadStartNode(reader, state);
                    if (state.Success && reader.GetIsEmptyElement())
                    {
                        ReadEndNode(reader.GetLocalName(), reader.GetNamespaceUri(), reader.GetDepth(), state);
                    }
                }
                else if (nodeType == XmlNodeType.Text || nodeType == XmlNodeType.CDATA)
                {
                    AppendDocumentation(reader.GetValue(), state);
                }
                else if (nodeType == XmlNodeType.EndElement)
                {
                    ReadEndNode(reader.GetLocalName(), reader.GetNamespaceUri(), reader.GetDepth(), state);
                }
            }
        }

        if (state.Success && !state.SawSchema)
        {
            FailSchemaRead(
                state,
                "schema.xsd.root",
                "The XSD root must be an XML Schema schema element.");
        }
    }

    internal static void ReadStartNode(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        string namespaceUri = reader.GetNamespaceUri();
        string localName = reader.GetLocalName();
        int depth = reader.GetDepth();
        if (depth == 0)
        {
            ReadRootSchema(reader, state);
            return;
        }

        if (namespaceUri != XmlSchemaNamespace)
        {
            return;
        }

        if (localName == "simpleType")
        {
            ReadSimpleType(reader, state);
        }
        else if (localName == "complexType")
        {
            ReadComplexType(reader, state);
        }
        else if (localName == "choice")
        {
            ReadChoice(reader, state);
        }
        else if (localName == "element")
        {
            ReadElementDeclaration(reader, state);
        }
        else if (localName == "any")
        {
            ReadAny(reader, state);
        }
        else if (localName == "restriction")
        {
            ReadRestriction(reader, state);
        }
        else if (FacetNameIsSupported(localName))
        {
            ReadFacet(reader, state);
        }
        else if (localName == "include")
        {
            ReadInclude(reader, state);
        }
        else if (localName == "import")
        {
            ReadImport(reader, state);
        }
        else if (localName == "documentation")
        {
            BeginDocumentation(reader, state);
        }
    }

    internal static void ReadRootSchema(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        if (state.SawSchema || reader.GetNamespaceUri() != XmlSchemaNamespace ||
            reader.GetLocalName() != "schema")
        {
            FailSchemaRead(
                state,
                "schema.xsd.root",
                "The XSD root must be an XML Schema schema element.");
            return;
        }

        state.SawSchema = true;
        string targetNamespace = ReadAttribute(reader, "targetNamespace");
        if (state.Schema.NamespaceUri.Length == 0)
        {
            state.Schema.NamespaceUri = targetNamespace;
        }
        else if (targetNamespace.Length > 0 && state.Schema.NamespaceUri != targetNamespace)
        {
            state.Schema.NamespaceUri = targetNamespace;
        }

        state.ParsedVersion = ReadAttribute(reader, "version");
    }

    internal static void ReadSimpleType(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        string name = ReadAttribute(reader, "name");
        if (name.Length == 0 && state.CurrentElement != null)
        {
            name = state.CurrentElement.Name + "Type";
        }

        if (name.Length == 0)
        {
            return;
        }

        if (GlobalTypeExists(state.Schema, name))
        {
            FailSchemaRead(
                state,
                "schema.xsd.type_duplicate",
                "The XSD declares a duplicate global or anonymous type name.");
            return;
        }

        if (state.Schema.DataTypes.Count + state.Schema.MessageComponents.Count >=
            state.Options.Limits.MaxCollectionCount)
        {
            FailSchemaRead(
                state,
                "schema.xsd.type_limit",
                "The XSD exceeds the configured normalized type limit.");
            return;
        }

        DataTypeDefinition type = new DataTypeDefinition();
        type.Identifier = name;
        type.Kind = DataTypeKind.Schema;
        type.SourceTypeName = "simpleType";
        type.Name = name;
        type.Location = reader.CaptureLocation(state.Schema.SourceName);
        state.Schema.DataTypes.Add(type);
        AddGlobalTypeName(state.Schema, name);
        state.CurrentDataType = type;
        state.CurrentDataTypeDepth = reader.GetDepth();
    }

    internal static void ReadComplexType(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        string name = ReadAttribute(reader, "name");
        if (name.Length == 0 && state.CurrentElement != null)
        {
            name = state.CurrentElement.Name + "Type";
        }

        if (name.Length == 0)
        {
            return;
        }

        if (GlobalTypeExists(state.Schema, name))
        {
            FailSchemaRead(
                state,
                "schema.xsd.type_duplicate",
                "The XSD declares a duplicate global or anonymous type name.");
            return;
        }

        if (state.Schema.DataTypes.Count + state.Schema.MessageComponents.Count >=
            state.Options.Limits.MaxCollectionCount)
        {
            FailSchemaRead(
                state,
                "schema.xsd.type_limit",
                "The XSD exceeds the configured normalized type limit.");
            return;
        }

        MessageComponentDefinition component = new MessageComponentDefinition();
        component.Identifier = name;
        component.Name = name;
        component.Location = reader.CaptureLocation(state.Schema.SourceName);
        state.Schema.MessageComponents.Add(component);
        AddGlobalTypeName(state.Schema, name);
        state.CurrentComponent = component;
        state.CurrentComponentDepth = reader.GetDepth();
    }

    internal static void ReadElementDeclaration(
        BoundedXmlReader reader,
        SchemaXsdReadState state)
    {
        string name = ReadAttribute(reader, "name");
        string reference = ReadAttribute(reader, "ref");
        if (name.Length == 0)
        {
            name = QualifiedLocalName(reference);
        }

        if (name.Length == 0)
        {
            FailSchemaRead(
                state,
                "schema.xsd.element_name",
                "An XSD element declaration must have a name or reference.");
            return;
        }

        if (state.Schema.MessageElements.Count >= state.Options.Limits.MaxCollectionCount)
        {
            FailSchemaRead(
                state,
                "schema.xsd.element_limit",
                "The XSD exceeds the configured normalized element limit.");
            return;
        }

        MessageElementDefinition element = new MessageElementDefinition();
        element.Identifier = CreateElementIdentifier(state, name);
        element.Kind = MessageElementKind.AssociationEnd;
        element.Name = name;
        element.XmlTag = name;
        element.Location = reader.CaptureLocation(state.Schema.SourceName);
        string typeName = ReadAttribute(reader, "type");
        if (typeName.Length == 0)
        {
            typeName = reference;
        }

        element.MemberType.SourceIdentifier = element.Identifier;
        element.MemberType.SourceKind = RepositoryEntityKind.MessageElement;
        element.MemberType.PropertyName = "type";
        element.MemberType.TargetIdentifier = typeName;
        element.MemberType.RawValue = typeName;
        ReadMultiplicity(reader, element);
        state.Schema.MessageElements.Add(element);
        if (state.CurrentComponent != null &&
            reader.GetDepth() > state.CurrentComponentDepth)
        {
            RepositoryReference member = new RepositoryReference();
            member.SourceIdentifier = state.CurrentComponent.Identifier;
            member.SourceKind = RepositoryEntityKind.MessageComponent;
            member.PropertyName = "MessageElements";
            member.Position = state.CurrentComponent.MessageElements.Count;
            member.TargetIdentifier = element.Identifier;
            member.TargetKind = RepositoryEntityKind.MessageElement;
            member.IsContainment = true;
            member.IsResolved = true;
            member.Location = element.Location;
            state.CurrentComponent.MessageElements.Add(member);
        }
        if (state.CurrentChoice != null && reader.GetDepth() > state.CurrentChoiceDepth)
        {
            RepositoryReference alternative = new RepositoryReference();
            alternative.SourceIdentifier = state.CurrentChoice.Identifier;
            alternative.SourceKind = RepositoryEntityKind.Choice;
            alternative.PropertyName = "ImpactedMessageElements";
            alternative.Position = state.CurrentChoice.ImpactedMessageElements.Count;
            alternative.TargetIdentifier = element.Identifier;
            alternative.TargetKind = RepositoryEntityKind.MessageElement;
            alternative.IsContainment = true;
            alternative.IsResolved = true;
            alternative.Location = element.Location;
            state.CurrentChoice.ImpactedMessageElements.Add(alternative);
        }

        if (reader.GetDepth() == 1)
        {
            if (GlobalElementExists(state.Schema, name))
            {
                FailSchemaRead(
                    state,
                    "schema.xsd.element_duplicate",
                    "The XSD declares a duplicate global element name.");
                return;
            }

            AddGlobalElementName(state.Schema, name);
            AddMessageDefinition(state.Schema, element, typeName);
            if (state.Schema.RootElement.Length == 0)
            {
                state.Schema.RootElement = name;
            }
        }

        state.CurrentElement = element;
        state.CurrentElementDepth = reader.GetDepth();
    }

    internal static void ReadChoice(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        if (state.CurrentComponent == null ||
            reader.GetDepth() <= state.CurrentComponentDepth)
        {
            return;
        }
        if (state.Schema.Choices.Count >= state.Options.Limits.MaxCollectionCount)
        {
            FailSchemaRead(
                state,
                "schema.xsd.choice_limit",
                "The XSD exceeds the configured normalized choice-group limit.");
            return;
        }

        ChoiceDefinition choice = new ChoiceDefinition();
        choice.Identifier = state.CurrentComponent.Identifier + ".choice." +
            state.CurrentComponent.Choices.Count.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        choice.ChoiceType = "XsdChoice";
        choice.Name = choice.Identifier;
        choice.Location = reader.CaptureLocation(state.Schema.SourceName);
        ReadParticleMultiplicity(reader, choice.Multiplicity);
        state.Schema.Choices.Add(choice);

        RepositoryReference reference = new RepositoryReference();
        reference.SourceIdentifier = state.CurrentComponent.Identifier;
        reference.SourceKind = RepositoryEntityKind.MessageComponent;
        reference.PropertyName = "Choices";
        reference.Position = state.CurrentComponent.Choices.Count;
        reference.TargetIdentifier = choice.Identifier;
        reference.TargetKind = RepositoryEntityKind.Choice;
        reference.IsContainment = true;
        reference.IsResolved = true;
        reference.Location = choice.Location;
        state.CurrentComponent.Choices.Add(reference);
        state.CurrentChoice = choice;
        state.CurrentChoiceDepth = reader.GetDepth();
    }

    internal static void ReadParticleMultiplicity(
        BoundedXmlReader reader,
        MultiplicityDefinition multiplicity)
    {
        multiplicity.MinimumOccurs = 1;
        multiplicity.HasMaximumOccurs = true;
        multiplicity.MaximumOccurs = 1;
        string minimum = ReadAttribute(reader, "minOccurs");
        string maximum = ReadAttribute(reader, "maxOccurs");
        IntegerLexicalResult parsedMinimum = IntegerLexical.ParseInt32(minimum.AsSpan());
        if (parsedMinimum.Success)
        {
            multiplicity.MinimumOccurs = parsedMinimum.Value;
        }
        if (maximum == "unbounded")
        {
            multiplicity.HasMaximumOccurs = false;
            multiplicity.MaximumOccurs = 0;
        }
        else
        {
            IntegerLexicalResult parsedMaximum = IntegerLexical.ParseInt32(maximum.AsSpan());
            if (parsedMaximum.Success)
            {
                multiplicity.MaximumOccurs = parsedMaximum.Value;
            }
        }
    }

    internal static void ReadAny(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        if (state.CurrentComponent == null ||
            reader.GetDepth() <= state.CurrentComponentDepth)
        {
            return;
        }
        if (state.Schema.ExternalSchemas.Count >=
            state.Options.Limits.MaxCollectionCount)
        {
            FailSchemaRead(
                state,
                "schema.xsd.wildcard_limit",
                "The XSD exceeds the configured normalized wildcard-type limit.");
            return;
        }

        ExternalSchemaDefinition external = new ExternalSchemaDefinition();
        external.Identifier = state.CurrentComponent.Identifier;
        external.Name = state.CurrentComponent.Name;
        external.Definition = state.CurrentComponent.Definition;
        external.Kind = DataTypeKind.ExternalSchema;
        external.SourceTypeName = "XsdAny";
        external.Location = reader.CaptureLocation(state.Schema.SourceName);
        string namespaceMode = ReadAttribute(reader, "namespace");
        if (namespaceMode.Length == 0)
        {
            namespaceMode = "##any";
        }
        if (namespaceMode.Contains(' '))
        {
            external.NamespaceMode = "list";
            string[] namespaces = namespaceMode.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);
            int namespaceIndex = 0;
            while (namespaceIndex < namespaces.Length)
            {
                external.NamespaceUris.Add(namespaces[namespaceIndex]);
                namespaceIndex++;
            }
        }
        else
        {
            external.NamespaceMode = namespaceMode;
        }
        external.ProcessContents = ReadAttribute(reader, "processContents");
        if (external.ProcessContents.Length == 0)
        {
            external.ProcessContents = "strict";
        }
        ReadParticleMultiplicity(reader, external.Multiplicity);
        state.Schema.MessageComponents.Remove(state.CurrentComponent);
        state.Schema.ExternalSchemas.Add(external);
        state.CurrentExternalSchema = external;
    }

    internal static void ReadRestriction(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        if (state.CurrentDataType != null)
        {
            state.CurrentDataType.SchemaTypeName = ReadAttribute(reader, "base");
        }
    }

    internal static void ReadFacet(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        if (state.CurrentDataType == null)
        {
            return;
        }

        string name = reader.GetLocalName();
        string value = ReadAttribute(reader, "value");
        if (name == "enumeration")
        {
            DataTypeFacet enumeration = FindOrCreateEnumerationFacet(state.CurrentDataType, reader, state);
            if (value.Length > 0)
            {
                enumeration.EnumerationValues.Add(value);
            }

            return;
        }

        if (state.CurrentDataType.Facets.Count >= state.Options.Limits.MaxCollectionCount)
        {
            FailSchemaRead(
                state,
                "schema.xsd.facet_limit",
                "An XSD simple type exceeds the configured facet limit.");
            return;
        }

        DataTypeFacet facet = new DataTypeFacet();
        facet.Name = name;
        facet.Value = value;
        facet.Location = reader.CaptureLocation(state.Schema.SourceName);
        IntegerLexicalResult integer = IntegerLexical.ParseInt32(value.AsSpan());
        if (integer.Success)
        {
            facet.HasIntegerValue = true;
            facet.IntegerValue = integer.Value;
        }

        state.CurrentDataType.Facets.Add(facet);
    }

    internal static void ReadInclude(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        string location = ReadAttribute(reader, "schemaLocation");
        state.Schema.IncludeLocations.Add(location);
    }

    internal static void ReadImport(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        state.Schema.ImportNamespaceUris.Add(ReadAttribute(reader, "namespace"));
        state.Schema.ImportLocations.Add(ReadAttribute(reader, "schemaLocation"));
    }

    internal static void BeginDocumentation(BoundedXmlReader reader, SchemaXsdReadState state)
    {
        state.DocumentationDataType = state.CurrentDataType;
        state.DocumentationComponent = state.CurrentComponent;
        state.DocumentationElement = state.CurrentElement;
        state.DocumentationDepth = reader.GetDepth();
        state.DocumentationText.Clear();
    }

    internal static void AppendDocumentation(string value, SchemaXsdReadState state)
    {
        if (state.DocumentationDepth >= 0)
        {
            state.DocumentationText.Append(value);
        }
    }

    internal static void ReadEndNode(
        string localName,
        string namespaceUri,
        int depth,
        SchemaXsdReadState state)
    {
        if (namespaceUri != XmlSchemaNamespace)
        {
            return;
        }

        if (localName == "documentation" && depth == state.DocumentationDepth)
        {
            CompleteDocumentation(state);
        }

        if (localName == "simpleType" && depth == state.CurrentDataTypeDepth)
        {
            state.CurrentDataType = null;
            state.CurrentDataTypeDepth = -1;
        }

        if (localName == "complexType" && depth == state.CurrentComponentDepth)
        {
            state.CurrentComponent = null;
            state.CurrentComponentDepth = -1;
            state.CurrentExternalSchema = null;
        }

        if (localName == "choice" && depth == state.CurrentChoiceDepth)
        {
            state.CurrentChoice = null;
            state.CurrentChoiceDepth = -1;
        }

        if (localName == "element" && depth == state.CurrentElementDepth)
        {
            state.CurrentElement = null;
            state.CurrentElementDepth = -1;
        }
    }

    internal static void CompleteDocumentation(SchemaXsdReadState state)
    {
        string text = state.DocumentationText.ToString().Trim();
        if (text.Length > 0)
        {
            if (state.DocumentationElement != null)
            {
                state.DocumentationElement.Definition = text;
            }
            else if (state.DocumentationDataType != null)
            {
                state.DocumentationDataType.Definition = text;
            }
            else if (state.DocumentationComponent != null)
            {
                state.DocumentationComponent.Definition = text;
            }
        }

        state.DocumentationDataType = null;
        state.DocumentationComponent = null;
        state.DocumentationElement = null;
        state.DocumentationDepth = -1;
        state.DocumentationText.Clear();
    }

    internal static bool FacetNameIsSupported(string name)
    {
        return name == "length" || name == "minLength" || name == "maxLength" ||
            name == "pattern" || name == "enumeration" || name == "whiteSpace" ||
            name == "minInclusive" || name == "maxInclusive" ||
            name == "minExclusive" || name == "maxExclusive" ||
            name == "totalDigits" || name == "fractionDigits";
    }

    internal static DataTypeFacet FindOrCreateEnumerationFacet(
        DataTypeDefinition type,
        BoundedXmlReader reader,
        SchemaXsdReadState state)
    {
        int index = 0;
        while (index < type.Facets.Count)
        {
            if (type.Facets[index].Name == "enumeration")
            {
                return type.Facets[index];
            }

            index++;
        }

        DataTypeFacet facet = new DataTypeFacet();
        facet.Name = "enumeration";
        facet.Location = reader.CaptureLocation(state.Schema.SourceName);
        type.Facets.Add(facet);
        return facet;
    }

    internal static void ReadMultiplicity(
        BoundedXmlReader reader,
        MessageElementDefinition element)
    {
        string minimum = ReadAttribute(reader, "minOccurs");
        string maximum = ReadAttribute(reader, "maxOccurs");
        element.Multiplicity.MinimumOccurs = 1;
        element.Multiplicity.HasMaximumOccurs = true;
        element.Multiplicity.MaximumOccurs = 1;
        IntegerLexicalResult parsedMinimum = IntegerLexical.ParseInt32(minimum.AsSpan());
        if (minimum.Length > 0 && parsedMinimum.Success && parsedMinimum.Value >= 0)
        {
            element.Multiplicity.MinimumOccurs = parsedMinimum.Value;
        }

        if (maximum == "unbounded")
        {
            element.Multiplicity.HasMaximumOccurs = false;
            element.Multiplicity.MaximumOccurs = 0;
        }
        else
        {
            IntegerLexicalResult parsedMaximum = IntegerLexical.ParseInt32(maximum.AsSpan());
            if (maximum.Length > 0 && parsedMaximum.Success && parsedMaximum.Value >= 0)
            {
                element.Multiplicity.MaximumOccurs = parsedMaximum.Value;
            }
        }

        element.Multiplicity.Location = element.Location;
    }

    internal static string CreateElementIdentifier(SchemaXsdReadState state, string name)
    {
        string owner = state.Schema.NamespaceUri;
        if (state.CurrentComponent != null)
        {
            owner = state.CurrentComponent.Identifier;
        }

        return owner + "#" + name + "#" + state.Schema.MessageElements.Count;
    }

    internal static void AddMessageDefinition(
        SchemaArtifact schema,
        MessageElementDefinition element,
        string typeName)
    {
        MessageDefinitionMetadata definition = new MessageDefinitionMetadata();
        definition.Identifier = element.Name;
        definition.Name = element.Name;
        definition.NamespaceUri = schema.NamespaceUri;
        definition.XmlName = QualifiedLocalName(typeName);
        definition.XmlTag = element.XmlTag;
        definition.RootElement = element.XmlTag;
        definition.Location = element.Location;
        schema.MessageDefinitions.Add(definition);
    }

    internal static void AddGlobalTypeName(SchemaArtifact schema, string name)
    {
        QualifiedNameData qualified = new QualifiedNameData();
        qualified.NamespaceUri = schema.NamespaceUri;
        qualified.LocalName = name;
        schema.GlobalTypes.Add(qualified);
    }

    internal static void AddGlobalElementName(SchemaArtifact schema, string name)
    {
        QualifiedNameData qualified = new QualifiedNameData();
        qualified.NamespaceUri = schema.NamespaceUri;
        qualified.LocalName = name;
        schema.GlobalElements.Add(qualified);
    }

    internal static bool GlobalTypeExists(SchemaArtifact schema, string name)
    {
        int index = 0;
        while (index < schema.GlobalTypes.Count)
        {
            if (schema.GlobalTypes[index].LocalName == name)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool GlobalElementExists(SchemaArtifact schema, string name)
    {
        int index = 0;
        while (index < schema.GlobalElements.Count)
        {
            if (schema.GlobalElements[index].LocalName == name)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static string QualifiedLocalName(string value)
    {
        int colon = value.IndexOf(':');
        if (colon >= 0 && colon + 1 < value.Length)
        {
            return value.Substring(colon + 1);
        }

        return value;
    }

    internal static string ReadAttribute(BoundedXmlReader reader, string localName)
    {
        if (!reader.MoveToFirstAttribute())
        {
            return string.Empty;
        }

        string value = string.Empty;
        bool move = true;
        while (move)
        {
            if (reader.GetLocalName() == localName)
            {
                value = reader.GetValue();
                move = false;
            }
            else
            {
                move = reader.MoveToNextAttribute();
            }
        }

        reader.MoveBackToElement();
        return value;
    }

    internal static void FailSchemaRead(
        SchemaXsdReadState state,
        string errorCode,
        string errorMessage)
    {
        state.Success = false;
        state.Result.ErrorCode = errorCode;
        state.Result.ErrorMessage = errorMessage;
    }
}
