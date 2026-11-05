using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace iso20022;

internal static class XsdLogicalModelEmitter
{
    internal static XsdGenerationContext CreateContext(
        XsdGenerationUnit unit,
        GenerationOptions options,
        GenerationResult result)
    {
        XsdGenerationContext context = new XsdGenerationContext();
        context.Unit = unit;
        context.Options = options;
        context.Result = result;
        if (unit.Schema != null)
        {
            PopulateSchemaContext(context, unit.Schema);
        }
        else if (unit.Repository != null && unit.Message != null)
        {
            PopulateRepositoryContext(context, unit.Repository, unit.Message);
        }
        BuildGenerationNames(context);
        BuildSchemaImports(context);
        SortGenerationMessages(context.Messages);
        return context;
    }

    internal static void PopulateSchemaContext(
        XsdGenerationContext context,
        SchemaArtifact schema)
    {
        context.Messages.AddRange(schema.MessageDefinitions);
        context.Components.AddRange(schema.MessageComponents);
        context.Elements.AddRange(schema.MessageElements);
        context.Choices.AddRange(schema.Choices);
        context.DataTypes.AddRange(schema.DataTypes);
        context.ExternalSchemas.AddRange(schema.ExternalSchemas);
        context.ExternalSchemas.AddRange(schema.RepositoryFallbackDefinitions);
    }

    internal static void PopulateRepositoryContext(
        XsdGenerationContext context,
        RepositorySnapshot repository,
        MessageDefinitionMetadata message)
    {
        context.Messages.Add(message);
        int index = 0;
        while (index < message.BuildingBlocks.Count)
        {
            AddRepositoryBuildingBlockClosure(
                context,
                repository,
                message.BuildingBlocks[index].TargetIdentifier);
            index++;
        }
        index = 0;
        while (index < message.Choices.Count)
        {
            AddRepositoryChoiceClosure(
                context,
                repository,
                message.Choices[index].TargetIdentifier);
            index++;
        }
    }

    internal static void AddRepositoryBuildingBlockClosure(
        XsdGenerationContext context,
        RepositorySnapshot repository,
        string identifier)
    {
        if (FindBuildingBlock(context.BuildingBlocks, identifier) != null)
        {
            return;
        }
        MessageBuildingBlockDefinition? block = FindBuildingBlock(
            repository.MessageBuildingBlocks,
            identifier);
        if (block == null)
        {
            return;
        }
        context.BuildingBlocks.Add(block);
        RepositoryReference reference = block.MemberType;
        if (block.SimpleType != null)
        {
            reference = block.SimpleType;
        }
        else if (block.ComplexType != null)
        {
            reference = block.ComplexType;
        }
        AddRepositoryTypeClosure(context, repository, reference);
    }

    internal static void AddRepositoryTypeClosure(
        XsdGenerationContext context,
        RepositorySnapshot repository,
        RepositoryReference reference)
    {
        string identifier = reference.TargetIdentifier;
        if (identifier.Length == 0 || FindQualifiedPrefix(identifier) == "xs" ||
            FindQualifiedPrefix(identifier) == "xsd" ||
            BuiltInSchemaTypeExists(FindQualifiedLocalName(identifier)))
        {
            return;
        }
        if (reference.TargetKind == RepositoryEntityKind.MessageComponent)
        {
            AddRepositoryComponentClosure(context, repository, identifier);
            return;
        }
        if (reference.TargetKind == RepositoryEntityKind.Choice)
        {
            AddRepositoryChoiceClosure(context, repository, identifier);
            return;
        }
        if (reference.TargetKind == RepositoryEntityKind.DataType)
        {
            AddRepositoryDataTypeClosure(context, repository, identifier);
            return;
        }
        if (reference.TargetKind == RepositoryEntityKind.CodeSet)
        {
            AddRepositoryCodeSetClosure(context, repository, identifier);
            return;
        }
        if (reference.TargetKind == RepositoryEntityKind.ExternalSchema)
        {
            AddRepositoryExternalSchemaClosure(context, repository, identifier);
            return;
        }
        if (FindMessageComponent(repository.MessageComponents, identifier) != null)
        {
            AddRepositoryComponentClosure(context, repository, identifier);
        }
        else if (FindChoiceDefinition(repository.Choices, identifier) != null)
        {
            AddRepositoryChoiceClosure(context, repository, identifier);
        }
        else if (FindDataType(repository.DataTypes, identifier) != null)
        {
            AddRepositoryDataTypeClosure(context, repository, identifier);
        }
        else if (FindCodeSet(repository.CodeSets, identifier) != null)
        {
            AddRepositoryCodeSetClosure(context, repository, identifier);
        }
        else if (FindExternalSchema(repository.ExternalSchemas, identifier) != null)
        {
            AddRepositoryExternalSchemaClosure(context, repository, identifier);
        }
    }

    internal static void AddRepositoryComponentClosure(
        XsdGenerationContext context,
        RepositorySnapshot repository,
        string identifier)
    {
        if (FindMessageComponent(context.Components, identifier) != null)
        {
            return;
        }
        MessageComponentDefinition? component = FindMessageComponent(
            repository.MessageComponents,
            identifier);
        if (component == null)
        {
            return;
        }
        context.Components.Add(component);
        int index = 0;
        while (index < component.MessageElements.Count)
        {
            AddRepositoryElementClosure(
                context,
                repository,
                component.MessageElements[index].TargetIdentifier);
            index++;
        }
        index = 0;
        while (index < component.Choices.Count)
        {
            AddRepositoryChoiceClosure(
                context,
                repository,
                component.Choices[index].TargetIdentifier);
            index++;
        }
    }

    internal static void AddRepositoryElementClosure(
        XsdGenerationContext context,
        RepositorySnapshot repository,
        string identifier)
    {
        if (FindMessageElement(context.Elements, identifier) != null)
        {
            return;
        }
        MessageElementDefinition? element = FindMessageElement(
            repository.MessageElements,
            identifier);
        if (element == null)
        {
            return;
        }
        context.Elements.Add(element);
        RepositoryReference reference = element.MemberType;
        if (element.SimpleType != null)
        {
            reference = element.SimpleType;
        }
        else if (element.ComplexType != null)
        {
            reference = element.ComplexType;
        }
        AddRepositoryTypeClosure(context, repository, reference);
    }

    internal static void AddRepositoryChoiceClosure(
        XsdGenerationContext context,
        RepositorySnapshot repository,
        string identifier)
    {
        if (FindChoiceDefinition(context.Choices, identifier) != null)
        {
            return;
        }
        ChoiceDefinition? choice = FindChoiceDefinition(repository.Choices, identifier);
        if (choice == null)
        {
            return;
        }
        context.Choices.Add(choice);
        int index = 0;
        while (index < choice.MessageElements.Count)
        {
            AddRepositoryElementClosure(
                context,
                repository,
                choice.MessageElements[index].TargetIdentifier);
            index++;
        }
        index = 0;
        while (index < choice.ImpactedMessageElements.Count)
        {
            AddRepositoryElementClosure(
                context,
                repository,
                choice.ImpactedMessageElements[index].TargetIdentifier);
            index++;
        }
        index = 0;
        while (index < choice.ImpactedBuildingBlocks.Count)
        {
            AddRepositoryBuildingBlockClosure(
                context,
                repository,
                choice.ImpactedBuildingBlocks[index].TargetIdentifier);
            index++;
        }
    }

    internal static void AddRepositoryDataTypeClosure(
        XsdGenerationContext context,
        RepositorySnapshot repository,
        string identifier)
    {
        if (FindDataType(context.DataTypes, identifier) != null)
        {
            return;
        }
        DataTypeDefinition? dataType = FindDataType(repository.DataTypes, identifier);
        if (dataType == null)
        {
            return;
        }
        context.DataTypes.Add(dataType);
        if (dataType.CurrencyIdentifierSet != null)
        {
            AddRepositoryTypeClosure(context, repository, dataType.CurrencyIdentifierSet);
        }
        if (dataType.SchemaTypeName.Length > 0)
        {
            RepositoryReference baseType = new RepositoryReference();
            baseType.TargetIdentifier = dataType.SchemaTypeName;
            AddRepositoryTypeClosure(context, repository, baseType);
        }
    }

    internal static void AddRepositoryCodeSetClosure(
        XsdGenerationContext context,
        RepositorySnapshot repository,
        string identifier)
    {
        if (FindCodeSet(context.CodeSets, identifier) != null)
        {
            return;
        }
        CodeSetDefinition? codeSet = FindCodeSet(repository.CodeSets, identifier);
        if (codeSet == null)
        {
            return;
        }
        context.CodeSets.Add(codeSet);
        if (codeSet.BaseCodeSet != null)
        {
            AddRepositoryTypeClosure(context, repository, codeSet.BaseCodeSet);
        }
    }

    internal static void AddRepositoryExternalSchemaClosure(
        XsdGenerationContext context,
        RepositorySnapshot repository,
        string identifier)
    {
        if (FindExternalSchema(context.ExternalSchemas, identifier) != null)
        {
            return;
        }
        ExternalSchemaDefinition? external = FindExternalSchema(
            repository.ExternalSchemas,
            identifier);
        if (external != null)
        {
            context.ExternalSchemas.Add(external);
        }
    }

    internal static long CountGenerationItems(XsdGenerationContext context)
    {
        return context.Messages.Count + context.BuildingBlocks.Count +
            context.Components.Count + context.Elements.Count + context.Choices.Count +
            context.DataTypes.Count + context.CodeSets.Count + context.ExternalSchemas.Count +
            context.Names.Count + context.Imports.Count;
    }

    internal static void BuildGenerationNames(XsdGenerationContext context)
    {
        int index = 0;
        while (index < context.Messages.Count)
        {
            MessageDefinitionMetadata message = context.Messages[index];
            if (message.BuildingBlocks.Count > 0)
            {
                string name = message.XmlName;
                if (name.Length == 0)
                {
                    name = message.Name + "Type";
                }
                AddGenerationName(context, GenerationConceptKind.MessageDefinition,
                    message.Identifier, name);
            }
            index++;
        }
        index = 0;
        while (index < context.Components.Count)
        {
            MessageComponentDefinition value = context.Components[index];
            AddGenerationName(context, GenerationConceptKind.MessageComponent,
                value.Identifier, value.Name);
            index++;
        }
        index = 0;
        while (index < context.Choices.Count)
        {
            ChoiceDefinition value = context.Choices[index];
            if (value.ChoiceType != "XsdChoice")
            {
                AddGenerationName(context, GenerationConceptKind.Choice,
                    value.Identifier, value.Name);
            }
            index++;
        }
        index = 0;
        while (index < context.DataTypes.Count)
        {
            DataTypeDefinition value = context.DataTypes[index];
            AddGenerationName(context, GenerationConceptKind.DataType,
                value.Identifier, value.Name);
            index++;
        }
        index = 0;
        while (index < context.CodeSets.Count)
        {
            CodeSetDefinition value = context.CodeSets[index];
            AddGenerationName(context, GenerationConceptKind.CodeSet,
                value.Identifier, value.Name);
            index++;
        }
        index = 0;
        while (index < context.ExternalSchemas.Count)
        {
            ExternalSchemaDefinition value = context.ExternalSchemas[index];
            AddGenerationName(context, GenerationConceptKind.ExternalSchema,
                value.Identifier, value.Name);
            index++;
        }
        ResolveGenerationNameCollisions(context.Names);
        SortGenerationNames(context.Names);
    }

    internal static void AddGenerationName(
        XsdGenerationContext context,
        GenerationConceptKind kind,
        string identifier,
        string candidate)
    {
        XsdGenerationName name = new XsdGenerationName();
        name.Kind = kind;
        name.Identifier = identifier;
        name.CandidateName = candidate;
        name.Name = SanitizeXsdName(candidate, identifier, kind);
        context.Names.Add(name);
    }

    internal static string SanitizeXsdName(
        string candidate,
        string identifier,
        GenerationConceptKind kind)
    {
        string value = candidate;
        if (value.Length == 0)
        {
            value = identifier;
        }
        if (value.Length > 0 && XmlNameLexical.IsNcName(value.AsSpan()))
        {
            return value;
        }
        GenerationIdentifierNamer namer = new GenerationIdentifierNamer();
        value = namer.CreateTypeName(value);
        if (value.Length > 0 && value[0] == '@')
        {
            value = value.Substring(1);
        }
        if (value == "_")
        {
            value = kind.ToString() + "_" +
                GenerationStableHash.CreateHex(identifier).Substring(0, 12);
        }
        return value;
    }

    internal static void ResolveGenerationNameCollisions(List<XsdGenerationName> names)
    {
        int index = 0;
        while (index < names.Count)
        {
            XsdGenerationName name = names[index];
            bool collision = false;
            int candidateIndex = 0;
            while (candidateIndex < names.Count)
            {
                XsdGenerationName candidate = names[candidateIndex];
                if (candidateIndex != index && candidate.Name == name.Name &&
                    (candidate.Kind != name.Kind || candidate.Identifier != name.Identifier))
                {
                    collision = true;
                    break;
                }
                candidateIndex++;
            }
            if (collision)
            {
                string identity = ((int)name.Kind).ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + "|" + name.Identifier;
                string suffix = GenerationStableHash.CreateHex(identity).Substring(0, 16);
                GenerationIdentifierNamer namer = new GenerationIdentifierNamer();
                name.Name = namer.AppendDisambiguator(name.Name, suffix);
            }
            index++;
        }
    }

    internal static void SortGenerationNames(List<XsdGenerationName> names)
    {
        int index = 1;
        while (index < names.Count)
        {
            XsdGenerationName value = names[index];
            int destination = index;
            while (destination > 0 &&
                CompareGenerationNames(value, names[destination - 1]) < 0)
            {
                names[destination] = names[destination - 1];
                destination--;
            }
            names[destination] = value;
            index++;
        }
    }

    internal static int CompareGenerationNames(
        XsdGenerationName left,
        XsdGenerationName right)
    {
        int comparison = string.CompareOrdinal(left.Name, right.Name);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = ((int)left.Kind).CompareTo((int)right.Kind);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.Identifier, right.Identifier);
    }

    internal static void BuildSchemaImports(XsdGenerationContext context)
    {
        if (context.Unit.Schema == null)
        {
            return;
        }
        SchemaArtifact schema = context.Unit.Schema;
        int index = 0;
        while (index < schema.IncludeLocations.Count)
        {
            XsdGenerationImport include = new XsdGenerationImport();
            include.IsInclude = true;
            include.Location = schema.IncludeLocations[index];
            context.Imports.Add(include);
            index++;
        }
        index = 0;
        while (index < schema.ImportNamespaceUris.Count ||
            index < schema.ImportLocations.Count)
        {
            XsdGenerationImport import = new XsdGenerationImport();
            if (index < schema.ImportNamespaceUris.Count)
            {
                import.NamespaceUri = schema.ImportNamespaceUris[index];
            }
            if (index < schema.ImportLocations.Count)
            {
                import.Location = schema.ImportLocations[index];
            }
            context.Imports.Add(import);
            index++;
        }
        SortGenerationImports(context.Imports);
    }

    internal static void SortGenerationImports(List<XsdGenerationImport> imports)
    {
        int index = 1;
        while (index < imports.Count)
        {
            XsdGenerationImport value = imports[index];
            int destination = index;
            while (destination > 0 &&
                CompareGenerationImports(value, imports[destination - 1]) < 0)
            {
                imports[destination] = imports[destination - 1];
                destination--;
            }
            imports[destination] = value;
            index++;
        }
    }

    internal static int CompareGenerationImports(
        XsdGenerationImport left,
        XsdGenerationImport right)
    {
        if (left.IsInclude && !right.IsInclude)
        {
            return -1;
        }
        if (!left.IsInclude && right.IsInclude)
        {
            return 1;
        }
        int comparison = string.CompareOrdinal(left.NamespaceUri, right.NamespaceUri);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.Location, right.Location);
    }

    internal static void SortGenerationMessages(List<MessageDefinitionMetadata> messages)
    {
        int index = 1;
        while (index < messages.Count)
        {
            MessageDefinitionMetadata value = messages[index];
            int destination = index;
            while (destination > 0 &&
                CompareGenerationMessages(value, messages[destination - 1]) < 0)
            {
                messages[destination] = messages[destination - 1];
                destination--;
            }
            messages[destination] = value;
            index++;
        }
    }

    internal static int CompareGenerationMessages(
        MessageDefinitionMetadata left,
        MessageDefinitionMetadata right)
    {
        int comparison = string.CompareOrdinal(left.RootElement, right.RootElement);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.Identifier, right.Identifier);
    }

    internal static byte[] EmitSchema(XsdGenerationContext context)
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Encoding = new UTF8Encoding(false);
        settings.Indent = true;
        settings.IndentChars = context.Options.Indentation;
        settings.NewLineChars = context.Options.NewLine;
        settings.NewLineHandling = NewLineHandling.Replace;
        settings.OmitXmlDeclaration = false;
        using (MemoryStream output = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(output, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("xs", "schema", Iso20022Namespaces.XmlSchema);
                if (context.Unit.NamespaceUri.Length > 0)
                {
                    writer.WriteAttributeString("xmlns", "tns", Iso20022Namespaces.Xmlns,
                        context.Unit.NamespaceUri);
                    writer.WriteAttributeString("targetNamespace", context.Unit.NamespaceUri);
                    writer.WriteAttributeString("elementFormDefault", "qualified");
                }
                else
                {
                    writer.WriteAttributeString("elementFormDefault", "unqualified");
                }
                writer.WriteAttributeString("attributeFormDefault", "unqualified");
                WriteSchemaImports(writer, context);
                WriteAllNamedTypes(writer, context);
                WriteGlobalMessageElements(writer, context);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            return output.ToArray();
        }
    }

    internal static void WriteSchemaImports(XmlWriter writer, XsdGenerationContext context)
    {
        int index = 0;
        while (index < context.Imports.Count)
        {
            XsdGenerationImport value = context.Imports[index];
            if (value.IsInclude)
            {
                writer.WriteStartElement("xs", "include", Iso20022Namespaces.XmlSchema);
                if (value.Location.Length > 0)
                {
                    writer.WriteAttributeString("schemaLocation", value.Location);
                }
            }
            else
            {
                writer.WriteStartElement("xs", "import", Iso20022Namespaces.XmlSchema);
                if (value.NamespaceUri.Length > 0)
                {
                    writer.WriteAttributeString("namespace", value.NamespaceUri);
                }
                if (value.Location.Length > 0)
                {
                    writer.WriteAttributeString("schemaLocation", value.Location);
                }
            }
            writer.WriteEndElement();
            index++;
        }
    }

    internal static void WriteAllNamedTypes(XmlWriter writer, XsdGenerationContext context)
    {
        int index = 0;
        while (index < context.Names.Count)
        {
            context.Options.CancellationToken.ThrowIfCancellationRequested();
            XsdGenerationName name = context.Names[index];
            if (name.Kind == GenerationConceptKind.MessageDefinition)
            {
                MessageDefinitionMetadata? message = FindMessageDefinition(
                    context.Messages, name.Identifier);
                if (message != null)
                {
                    WriteMessageDefinitionType(writer, context, message, name.Name);
                }
            }
            else if (name.Kind == GenerationConceptKind.MessageComponent)
            {
                MessageComponentDefinition? component = FindMessageComponent(
                    context.Components, name.Identifier);
                if (component != null)
                {
                    WriteMessageComponentType(writer, context, component, name.Name);
                }
            }
            else if (name.Kind == GenerationConceptKind.Choice)
            {
                ChoiceDefinition? choice = FindChoiceDefinition(context.Choices, name.Identifier);
                if (choice != null)
                {
                    WriteChoiceDefinitionType(writer, context, choice, name.Name);
                }
            }
            else if (name.Kind == GenerationConceptKind.DataType)
            {
                DataTypeDefinition? dataType = FindDataType(context.DataTypes, name.Identifier);
                if (dataType != null)
                {
                    WriteDataTypeDefinition(writer, context, dataType, name.Name);
                }
            }
            else if (name.Kind == GenerationConceptKind.CodeSet)
            {
                CodeSetDefinition? codeSet = FindCodeSet(context.CodeSets, name.Identifier);
                if (codeSet != null)
                {
                    WriteCodeSetDefinition(writer, context, codeSet, name.Name);
                }
            }
            else if (name.Kind == GenerationConceptKind.ExternalSchema)
            {
                ExternalSchemaDefinition? external = FindExternalSchema(
                    context.ExternalSchemas, name.Identifier);
                if (external != null)
                {
                    WriteExternalSchemaType(writer, context, external, name.Name);
                }
            }
            index++;
        }
    }

    internal static void WriteDataTypeDefinition(
        XmlWriter writer,
        XsdGenerationContext context,
        DataTypeDefinition dataType,
        string name)
    {
        writer.WriteStartElement("xs", "simpleType", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("name", name);
        WriteGenerationDocumentation(writer, context, dataType.Definition);
        writer.WriteStartElement("xs", "restriction", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("base", FindDataTypeBase(context, dataType));
        WriteDataTypeFacets(writer, dataType.Facets);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteCodeSetDefinition(
        XmlWriter writer,
        XsdGenerationContext context,
        CodeSetDefinition codeSet,
        string name)
    {
        writer.WriteStartElement("xs", "simpleType", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("name", name);
        WriteGenerationDocumentation(writer, context, codeSet.Definition);
        writer.WriteStartElement("xs", "restriction", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("base", "xs:string");
        WriteDataTypeFacets(writer, codeSet.Facets);
        List<CodeValueDefinition> codes = new List<CodeValueDefinition>();
        codes.AddRange(codeSet.Codes);
        SortCodeValues(codes);
        int index = 0;
        while (index < codes.Count)
        {
            writer.WriteStartElement("xs", "enumeration", Iso20022Namespaces.XmlSchema);
            writer.WriteAttributeString("value", codes[index].Value);
            WriteGenerationDocumentation(writer, context, codes[index].Definition);
            writer.WriteEndElement();
            index++;
        }
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void SortCodeValues(List<CodeValueDefinition> codes)
    {
        int index = 1;
        while (index < codes.Count)
        {
            CodeValueDefinition value = codes[index];
            int destination = index;
            while (destination > 0 &&
                string.CompareOrdinal(value.Value, codes[destination - 1].Value) < 0)
            {
                codes[destination] = codes[destination - 1];
                destination--;
            }
            codes[destination] = value;
            index++;
        }
    }

    internal static void WriteDataTypeFacets(XmlWriter writer, List<DataTypeFacet> source)
    {
        List<DataTypeFacet> facets = new List<DataTypeFacet>();
        facets.AddRange(source);
        SortDataTypeFacets(facets);
        int index = 0;
        while (index < facets.Count)
        {
            DataTypeFacet facet = facets[index];
            if (facet.Name == "enumeration")
            {
                List<string> values = new List<string>();
                values.AddRange(facet.EnumerationValues);
                SortFacetValues(values);
                int valueIndex = 0;
                while (valueIndex < values.Count)
                {
                    WriteSingleFacet(writer, "enumeration", values[valueIndex]);
                    valueIndex++;
                }
            }
            else if (SchemaXsdReader.FacetNameIsSupported(facet.Name))
            {
                WriteSingleFacet(writer, facet.Name, facet.Value);
            }
            index++;
        }
    }

    internal static void SortDataTypeFacets(List<DataTypeFacet> facets)
    {
        int index = 1;
        while (index < facets.Count)
        {
            DataTypeFacet value = facets[index];
            int destination = index;
            while (destination > 0 &&
                CompareDataTypeFacets(value, facets[destination - 1]) < 0)
            {
                facets[destination] = facets[destination - 1];
                destination--;
            }
            facets[destination] = value;
            index++;
        }
    }

    internal static int CompareDataTypeFacets(DataTypeFacet left, DataTypeFacet right)
    {
        int comparison = string.CompareOrdinal(left.Name, right.Name);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.Value, right.Value);
    }

    internal static void SortFacetValues(List<string> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            string value = values[index];
            int destination = index;
            while (destination > 0 &&
                string.CompareOrdinal(value, values[destination - 1]) < 0)
            {
                values[destination] = values[destination - 1];
                destination--;
            }
            values[destination] = value;
            index++;
        }
    }

    internal static void WriteSingleFacet(XmlWriter writer, string name, string value)
    {
        writer.WriteStartElement("xs", name, Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("value", value);
        writer.WriteEndElement();
    }

    internal static void WriteMessageComponentType(
        XmlWriter writer,
        XsdGenerationContext context,
        MessageComponentDefinition component,
        string name)
    {
        writer.WriteStartElement("xs", "complexType", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("name", name);
        WriteGenerationDocumentation(writer, context, component.Definition);
        writer.WriteStartElement("xs", "sequence", Iso20022Namespaces.XmlSchema);
        WriteComponentParticleContent(writer, context, component);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteComponentParticleContent(
        XmlWriter writer,
        XsdGenerationContext context,
        MessageComponentDefinition component)
    {
        List<string> emittedChoices = new List<string>();
        int index = 0;
        while (index < component.MessageElements.Count)
        {
            RepositoryReference member = component.MessageElements[index];
            ChoiceDefinition? choice = FindLegacyElementChoice(context, component, member.TargetIdentifier);
            if (choice != null)
            {
                if (!TextValueExists(emittedChoices, choice.Identifier))
                {
                    writer.WriteStartElement("xs", "choice", Iso20022Namespaces.XmlSchema);
                    if (choice.ChoiceType == "XsdChoice")
                    {
                        WriteMultiplicityAttributes(writer, choice.Multiplicity);
                    }
                    else
                    {
                        writer.WriteAttributeString("minOccurs", "0");
                        writer.WriteAttributeString("maxOccurs", "1");
                    }
                    WriteMessageElementReferences(writer, context,
                        choice.ImpactedMessageElements);
                    writer.WriteEndElement();
                    emittedChoices.Add(choice.Identifier);
                }
            }
            else
            {
                MessageElementDefinition? element = FindMessageElement(
                    context.Elements,
                    member.TargetIdentifier);
                if (element != null)
                {
                    WriteMessageElementParticle(writer, context, element);
                }
                else
                {
                    AddGenerationDiagnostic(context, "generation.xsd.element_missing",
                        "A message-element reference could not be resolved.",
                        member.TargetIdentifier);
                }
            }
            index++;
        }
    }

    internal static ChoiceDefinition? FindLegacyElementChoice(
        XsdGenerationContext context,
        MessageComponentDefinition component,
        string elementIdentifier)
    {
        int choiceIndex = 0;
        while (choiceIndex < component.Choices.Count)
        {
            ChoiceDefinition? choice = FindChoiceDefinition(
                context.Choices,
                component.Choices[choiceIndex].TargetIdentifier);
            if (choice != null && choice.ImpactedMessageElements.Count > 0 &&
                ReferenceTargetExists(choice.ImpactedMessageElements, elementIdentifier))
            {
                return choice;
            }
            choiceIndex++;
        }
        return null;
    }

    internal static bool ReferenceTargetExists(
        List<RepositoryReference> references,
        string identifier)
    {
        int index = 0;
        while (index < references.Count)
        {
            if (references[index].TargetIdentifier == identifier)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool TextValueExists(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void WriteChoiceDefinitionType(
        XmlWriter writer,
        XsdGenerationContext context,
        ChoiceDefinition choice,
        string name)
    {
        writer.WriteStartElement("xs", "complexType", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("name", name);
        WriteGenerationDocumentation(writer, context, choice.Definition);
        writer.WriteStartElement("xs", "choice", Iso20022Namespaces.XmlSchema);
        if (choice.MessageElements.Count > 0)
        {
            WriteMessageElementReferences(writer, context, choice.MessageElements);
        }
        else
        {
            WriteMessageElementReferences(writer, context, choice.ImpactedMessageElements);
        }
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteExternalSchemaType(
        XmlWriter writer,
        XsdGenerationContext context,
        ExternalSchemaDefinition external,
        string name)
    {
        writer.WriteStartElement("xs", "complexType", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("name", name);
        WriteGenerationDocumentation(writer, context, external.Definition);
        writer.WriteStartElement("xs", "sequence", Iso20022Namespaces.XmlSchema);
        writer.WriteStartElement("xs", "any", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("namespace", FindWildcardNamespace(external));
        writer.WriteAttributeString("processContents", FindWildcardProcessContents(external));
        if (external.SourceTypeName == "XsdAny")
        {
            WriteMultiplicityAttributes(writer, external.Multiplicity);
        }
        else
        {
            writer.WriteAttributeString("minOccurs", "0");
            writer.WriteAttributeString("maxOccurs", "unbounded");
        }
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static string FindWildcardNamespace(ExternalSchemaDefinition external)
    {
        if (external.NamespaceUris.Count > 0)
        {
            StringBuilder value = new StringBuilder();
            int index = 0;
            while (index < external.NamespaceUris.Count)
            {
                if (value.Length > 0)
                {
                    value.Append(' ');
                }
                value.Append(external.NamespaceUris[index]);
                index++;
            }
            return value.ToString();
        }
        if (external.NamespaceMode.Length > 0 && external.NamespaceMode != "list")
        {
            return external.NamespaceMode;
        }
        return "##any";
    }

    internal static string FindWildcardProcessContents(ExternalSchemaDefinition external)
    {
        string value = external.ProcessContents.ToLowerInvariant();
        if (value == "strict" || value == "lax" || value == "skip")
        {
            return value;
        }
        return "lax";
    }

    internal static void WriteMessageDefinitionType(
        XmlWriter writer,
        XsdGenerationContext context,
        MessageDefinitionMetadata message,
        string name)
    {
        writer.WriteStartElement("xs", "complexType", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("name", name);
        WriteGenerationDocumentation(writer, context, message.Definition);
        writer.WriteStartElement("xs", "sequence", Iso20022Namespaces.XmlSchema);
        int index = 0;
        while (index < message.BuildingBlocks.Count)
        {
            MessageBuildingBlockDefinition? block = FindBuildingBlock(
                context.BuildingBlocks,
                message.BuildingBlocks[index].TargetIdentifier);
            if (block != null)
            {
                WriteBuildingBlockParticle(writer, context, block);
            }
            else
            {
                AddGenerationDiagnostic(context, "generation.xsd.building_block_missing",
                    "A message building-block reference could not be resolved.",
                    message.BuildingBlocks[index].TargetIdentifier);
            }
            index++;
        }
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void WriteMessageElementReferences(
        XmlWriter writer,
        XsdGenerationContext context,
        List<RepositoryReference> references)
    {
        int index = 0;
        while (index < references.Count)
        {
            context.Options.CancellationToken.ThrowIfCancellationRequested();
            MessageElementDefinition? element = FindMessageElement(
                context.Elements,
                references[index].TargetIdentifier);
            if (element != null)
            {
                WriteMessageElementParticle(writer, context, element);
            }
            else
            {
                AddGenerationDiagnostic(context, "generation.xsd.element_missing",
                    "A message-element reference could not be resolved.",
                    references[index].TargetIdentifier);
            }
            index++;
        }
    }

    internal static void WriteMessageElementParticle(
        XmlWriter writer,
        XsdGenerationContext context,
        MessageElementDefinition element)
    {
        writer.WriteStartElement("xs", "element", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("name", FindElementName(element.XmlTag, element.Name,
            element.Identifier));
        writer.WriteAttributeString("type", FindElementType(context, element));
        WriteMultiplicityAttributes(writer, element.Multiplicity);
        WriteGenerationDocumentation(writer, context, element.Definition);
        writer.WriteEndElement();
    }

    internal static void WriteBuildingBlockParticle(
        XmlWriter writer,
        XsdGenerationContext context,
        MessageBuildingBlockDefinition block)
    {
        writer.WriteStartElement("xs", "element", Iso20022Namespaces.XmlSchema);
        writer.WriteAttributeString("name", FindElementName(block.XmlTag, block.Name,
            block.Identifier));
        writer.WriteAttributeString("type", FindBuildingBlockType(context, block));
        WriteMultiplicityAttributes(writer, block.Multiplicity);
        WriteGenerationDocumentation(writer, context, block.Definition);
        writer.WriteEndElement();
    }

    internal static string FindElementName(string xmlTag, string name, string identifier)
    {
        string candidate = xmlTag;
        if (candidate.Length == 0)
        {
            candidate = name;
        }
        return SanitizeXsdName(candidate, identifier, GenerationConceptKind.MessageElement);
    }

    internal static void WriteMultiplicityAttributes(
        XmlWriter writer,
        MultiplicityDefinition multiplicity)
    {
        writer.WriteAttributeString("minOccurs", multiplicity.MinimumOccurs.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        if (multiplicity.HasMaximumOccurs)
        {
            writer.WriteAttributeString("maxOccurs", multiplicity.MaximumOccurs.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteAttributeString("maxOccurs", "unbounded");
        }
    }

    internal static string FindElementType(
        XsdGenerationContext context,
        MessageElementDefinition element)
    {
        RepositoryReference reference = element.MemberType;
        if (element.SimpleType != null)
        {
            reference = element.SimpleType;
        }
        else if (element.ComplexType != null)
        {
            reference = element.ComplexType;
        }
        return FindReferenceType(context, reference, element.Identifier);
    }

    internal static string FindBuildingBlockType(
        XsdGenerationContext context,
        MessageBuildingBlockDefinition block)
    {
        RepositoryReference reference = block.MemberType;
        if (block.SimpleType != null)
        {
            reference = block.SimpleType;
        }
        else if (block.ComplexType != null)
        {
            reference = block.ComplexType;
        }
        return FindReferenceType(context, reference, block.Identifier);
    }

    internal static string FindReferenceType(
        XsdGenerationContext context,
        RepositoryReference reference,
        string ownerIdentifier)
    {
        string target = reference.TargetIdentifier;
        if (target.Length == 0)
        {
            target = reference.RawValue;
        }
        string prefix = FindQualifiedPrefix(target);
        string localName = FindQualifiedLocalName(target);
        if (prefix == "xs" || prefix == "xsd")
        {
            return "xs:" + localName;
        }
        if (BuiltInSchemaTypeExists(localName))
        {
            return "xs:" + localName;
        }
        XsdGenerationName? name = FindTypeGenerationName(context.Names, target, localName,
            reference.TargetKind);
        if (name != null)
        {
            return QualifyTargetType(context, name.Name);
        }
        AddGenerationDiagnostic(context, "generation.xsd.type_fallback",
            "A member type could not be resolved and was generated as xs:string.",
            ownerIdentifier);
        return "xs:string";
    }

    internal static XsdGenerationName? FindTypeGenerationName(
        List<XsdGenerationName> names,
        string target,
        string localName,
        RepositoryEntityKind repositoryKind)
    {
        GenerationConceptKind expected = MapRepositoryKind(repositoryKind);
        int index = 0;
        while (index < names.Count)
        {
            XsdGenerationName name = names[index];
            if ((expected == GenerationConceptKind.Unknown || name.Kind == expected) &&
                (name.Identifier == target || name.Identifier == localName ||
                name.CandidateName == target || name.CandidateName == localName ||
                name.Name == localName))
            {
                return name;
            }
            index++;
        }
        index = 0;
        while (index < names.Count)
        {
            XsdGenerationName name = names[index];
            if (name.Identifier == target || name.Identifier == localName ||
                name.CandidateName == target || name.CandidateName == localName ||
                name.Name == localName)
            {
                return name;
            }
            index++;
        }
        return null;
    }

    internal static GenerationConceptKind MapRepositoryKind(RepositoryEntityKind kind)
    {
        if (kind == RepositoryEntityKind.MessageComponent)
        {
            return GenerationConceptKind.MessageComponent;
        }
        if (kind == RepositoryEntityKind.Choice)
        {
            return GenerationConceptKind.Choice;
        }
        if (kind == RepositoryEntityKind.DataType)
        {
            return GenerationConceptKind.DataType;
        }
        if (kind == RepositoryEntityKind.CodeSet)
        {
            return GenerationConceptKind.CodeSet;
        }
        if (kind == RepositoryEntityKind.ExternalSchema)
        {
            return GenerationConceptKind.ExternalSchema;
        }
        return GenerationConceptKind.Unknown;
    }

    internal static string FindQualifiedPrefix(string value)
    {
        int colon = value.IndexOf(':');
        if (colon > 0)
        {
            return value.Substring(0, colon);
        }
        return string.Empty;
    }

    internal static string FindQualifiedLocalName(string value)
    {
        int colon = value.IndexOf(':');
        if (colon >= 0 && colon + 1 < value.Length)
        {
            return value.Substring(colon + 1);
        }
        return value;
    }

    internal static bool BuiltInSchemaTypeExists(string value)
    {
        return value == "string" || value == "boolean" || value == "decimal" ||
            value == "integer" || value == "date" || value == "dateTime" ||
            value == "time" || value == "duration" || value == "gYear" ||
            value == "gMonth" || value == "gDay" || value == "gYearMonth" ||
            value == "gMonthDay" || value == "base64Binary" || value == "hexBinary" ||
            value == "anyURI" || value == "QName" || value == "token" ||
            value == "normalizedString" || value == "anyType";
    }

    internal static string QualifyTargetType(XsdGenerationContext context, string name)
    {
        if (context.Unit.NamespaceUri.Length == 0)
        {
            return name;
        }
        return "tns:" + name;
    }

    internal static string FindDataTypeBase(
        XsdGenerationContext context,
        DataTypeDefinition dataType)
    {
        string schemaType = dataType.SchemaTypeName;
        if (schemaType.Length > 0)
        {
            RepositoryReference reference = new RepositoryReference();
            reference.TargetIdentifier = schemaType;
            return FindReferenceType(context, reference, dataType.Identifier);
        }
        if (dataType.Kind == DataTypeKind.Boolean || dataType.Kind == DataTypeKind.Indicator)
        {
            return "xs:boolean";
        }
        if (dataType.Kind == DataTypeKind.Decimal || dataType.Kind == DataTypeKind.Amount ||
            dataType.Kind == DataTypeKind.Quantity || dataType.Kind == DataTypeKind.Rate)
        {
            return "xs:decimal";
        }
        if (dataType.Kind == DataTypeKind.Binary)
        {
            if (dataType.BinaryEncoding == BinaryEncodingKind.Hexadecimal)
            {
                return "xs:hexBinary";
            }
            return "xs:base64Binary";
        }
        if (dataType.Kind == DataTypeKind.Date)
        {
            return "xs:date";
        }
        if (dataType.Kind == DataTypeKind.DateTime)
        {
            return "xs:dateTime";
        }
        if (dataType.Kind == DataTypeKind.Time)
        {
            return "xs:time";
        }
        if (dataType.Kind == DataTypeKind.Duration)
        {
            return "xs:duration";
        }
        if (dataType.Kind == DataTypeKind.Day)
        {
            return "xs:gDay";
        }
        if (dataType.Kind == DataTypeKind.Month)
        {
            return "xs:gMonth";
        }
        if (dataType.Kind == DataTypeKind.MonthDay)
        {
            return "xs:gMonthDay";
        }
        if (dataType.Kind == DataTypeKind.Year)
        {
            return "xs:gYear";
        }
        if (dataType.Kind == DataTypeKind.YearMonth)
        {
            return "xs:gYearMonth";
        }
        return "xs:string";
    }

    internal static void WriteGlobalMessageElements(
        XmlWriter writer,
        XsdGenerationContext context)
    {
        int index = 0;
        while (index < context.Messages.Count)
        {
            MessageDefinitionMetadata message = context.Messages[index];
            writer.WriteStartElement("xs", "element", Iso20022Namespaces.XmlSchema);
            string root = message.RootElement;
            if (root.Length == 0)
            {
                root = message.XmlTag;
            }
            if (root.Length == 0)
            {
                root = "Document";
            }
            writer.WriteAttributeString("name", SanitizeXsdName(root, message.Identifier,
                GenerationConceptKind.MessageDefinition));
            writer.WriteAttributeString("type", FindMessageRootType(context, message));
            WriteGenerationDocumentation(writer, context, message.Definition);
            writer.WriteEndElement();
            index++;
        }
    }

    internal static string FindMessageRootType(
        XsdGenerationContext context,
        MessageDefinitionMetadata message)
    {
        if (message.BuildingBlocks.Count > 0)
        {
            XsdGenerationName? wrapper = FindExactGenerationName(context.Names,
                GenerationConceptKind.MessageDefinition, message.Identifier);
            if (wrapper != null)
            {
                return QualifyTargetType(context, wrapper.Name);
            }
        }
        if (message.XmlName.Length > 0)
        {
            XsdGenerationName? named = FindTypeGenerationName(context.Names,
                message.XmlName, FindQualifiedLocalName(message.XmlName),
                RepositoryEntityKind.Unknown);
            if (named != null)
            {
                return QualifyTargetType(context, named.Name);
            }
            if (BuiltInSchemaTypeExists(FindQualifiedLocalName(message.XmlName)))
            {
                return "xs:" + FindQualifiedLocalName(message.XmlName);
            }
        }
        MessageElementDefinition? global = FindGlobalMessageElement(context.Elements, message);
        if (global != null)
        {
            return FindElementType(context, global);
        }
        AddGenerationDiagnostic(context, "generation.xsd.root_type_fallback",
            "A message root type could not be resolved and was generated as xs:string.",
            message.Identifier);
        return "xs:string";
    }

    internal static MessageElementDefinition? FindGlobalMessageElement(
        List<MessageElementDefinition> elements,
        MessageDefinitionMetadata message)
    {
        int index = 0;
        while (index < elements.Count)
        {
            MessageElementDefinition element = elements[index];
            if (element.XmlTag == message.RootElement || element.Name == message.RootElement ||
                element.Identifier == message.Identifier)
            {
                return element;
            }
            index++;
        }
        return null;
    }

    internal static void WriteGenerationDocumentation(
        XmlWriter writer,
        XsdGenerationContext context,
        string definition)
    {
        if (!context.Options.IncludeDocumentation || definition.Length == 0)
        {
            return;
        }
        writer.WriteStartElement("xs", "annotation", Iso20022Namespaces.XmlSchema);
        writer.WriteStartElement("xs", "documentation", Iso20022Namespaces.XmlSchema);
        writer.WriteString(definition);
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    internal static void AddGenerationDiagnostic(
        XsdGenerationContext context,
        string code,
        string message,
        string sourceIdentifier)
    {
        if (context.Result.DiagnosticCodes.Count >=
            context.Options.Limits.MaxDiagnosticCount)
        {
            return;
        }
        context.Result.DiagnosticCodes.Add(code);
        context.Result.DiagnosticMessages.Add(message);
        context.Result.DiagnosticSourceIdentifiers.Add(sourceIdentifier);
    }

    internal static XsdGenerationName? FindExactGenerationName(
        List<XsdGenerationName> names,
        GenerationConceptKind kind,
        string identifier)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (names[index].Kind == kind && names[index].Identifier == identifier)
            {
                return names[index];
            }
            index++;
        }
        return null;
    }

    internal static MessageDefinitionMetadata? FindMessageDefinition(
        List<MessageDefinitionMetadata> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }

    internal static MessageBuildingBlockDefinition? FindBuildingBlock(
        List<MessageBuildingBlockDefinition> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }

    internal static MessageComponentDefinition? FindMessageComponent(
        List<MessageComponentDefinition> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }

    internal static MessageElementDefinition? FindMessageElement(
        List<MessageElementDefinition> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }

    internal static ChoiceDefinition? FindChoiceDefinition(
        List<ChoiceDefinition> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }

    internal static DataTypeDefinition? FindDataType(
        List<DataTypeDefinition> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }

    internal static CodeSetDefinition? FindCodeSet(
        List<CodeSetDefinition> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }

    internal static ExternalSchemaDefinition? FindExternalSchema(
        List<ExternalSchemaDefinition> values,
        string identifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }
}
