using System;
using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class CSharpModelEmitter
{
    internal static CSharpGenerationContext CreateCSharpContext(
        GenerationPlan plan,
        List<XsdGenerationUnit> units,
        GenerationResult result)
    {
        CSharpGenerationContext context = new CSharpGenerationContext();
        context.Plan = plan;
        context.Result = result;
        GenerationModel model = new GenerationModel();
        GenerationModelNormalizer normalizer = new GenerationModelNormalizer();
        int unitIndex = 0;
        while (unitIndex < units.Count)
        {
            XsdGenerationUnit unit = units[unitIndex];
            if (unit.Repository != null &&
                FindRepositorySourceOrdinal(model, unit.Repository) >= 0)
            {
                unitIndex++;
                continue;
            }
            int sourceOrdinal = FindOrAddGenerationSource(model, unit);
            GenerationSourceKind sourceKind = GenerationSourceKind.Repository;
            XsdGenerationContext selected;
            if (unit.Repository != null)
            {
                selected = CreateRepositorySelectionContext(
                    units,
                    unit.Repository,
                    plan.Options,
                    result);
            }
            else
            {
                sourceKind = GenerationSourceKind.Xsd;
                selected = XsdLogicalModelEmitter.CreateContext(
                    unit,
                    plan.Options,
                    result);
            }
            normalizer.AppendMessageDefinitions(
                model, selected.Messages, sourceOrdinal, sourceKind);
            normalizer.AppendMessageBuildingBlocks(
                model, selected.BuildingBlocks, sourceOrdinal, sourceKind);
            normalizer.AppendMessageComponents(
                model, selected.Components, sourceOrdinal, sourceKind);
            normalizer.AppendMessageElements(
                model, selected.Elements, sourceOrdinal, sourceKind);
            normalizer.AppendChoices(
                model, selected.Choices, sourceOrdinal, sourceKind);
            normalizer.AppendDataTypes(
                model, selected.DataTypes, sourceOrdinal, sourceKind);
            normalizer.AppendCodeSets(
                model, selected.CodeSets, sourceOrdinal, sourceKind);
            normalizer.AppendExternalSchemas(
                model, selected.ExternalSchemas, sourceOrdinal, sourceKind,
                unit.Schema != null);
            unitIndex++;
        }
        RemoveDuplicateConcepts(model.Concepts);
        context.Model = model;
        context.Concepts.AddRange(model.Concepts);

        GenerationModel emissionModel = new GenerationModel();
        emissionModel.Sources.AddRange(model.Sources);
        int conceptIndex = 0;
        while (conceptIndex < model.Concepts.Count)
        {
            if (ConceptProducesCSharpType(model.Concepts[conceptIndex]))
            {
                emissionModel.Concepts.Add(model.Concepts[conceptIndex]);
            }
            conceptIndex++;
        }
        context.Symbols = new GenerationSymbolTableBuilder().BuildSymbolTable(
            emissionModel,
            plan.Options.RootNamespace);
        BuildGenerationIndexes(context);
        return context;
    }

    internal static void BuildGenerationIndexes(CSharpGenerationContext context)
    {
        int index = 0;
        while (index < context.Concepts.Count)
        {
            GenerationConcept concept = context.Concepts[index];
            if (concept.Identifier.Length > 0)
            {
                string identity = CreateGenerationIdentity(
                    concept.SourceOrdinal,
                    concept.Kind,
                    concept.Identifier);
                if (!context.ConceptsByIdentity.ContainsKey(identity))
                {
                    context.ConceptsByIdentity.Add(identity, concept);
                }
            }
            index++;
        }
        index = 0;
        while (index < context.Symbols.Symbols.Count)
        {
            GenerationSymbolEntry symbol = context.Symbols.Symbols[index];
            string publicName = symbol.NamespaceName + "\n" + symbol.SymbolName;
            context.PublicSymbolNames.Add(publicName);
            context.PublicTypeNames.Add(publicName);
            if (symbol.Concept.Identifier.Length > 0)
            {
                string identity = CreateGenerationIdentity(
                    symbol.Concept.SourceOrdinal,
                    symbol.Concept.Kind,
                    symbol.Concept.Identifier);
                if (!context.SymbolsByIdentity.ContainsKey(identity))
                {
                    context.SymbolsByIdentity.Add(identity, symbol);
                }
            }
            index++;
        }
        index = 0;
        while (index < context.Symbols.Symbols.Count)
        {
            GenerationSymbolEntry symbol = context.Symbols.Symbols[index];
            if (symbol.Concept.Kind == GenerationConceptKind.Choice)
            {
                context.PublicTypeNames.Add(
                    symbol.NamespaceName + "\n" + FindChoiceKindName(context, symbol));
            }
            index++;
        }
    }

    internal static string CreateGenerationIdentity(
        int sourceOrdinal,
        GenerationConceptKind kind,
        string identifier)
    {
        return sourceOrdinal.ToString(
            System.Globalization.CultureInfo.InvariantCulture) + "\n" +
            ((int)kind).ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\n" + identifier;
    }

    internal static int FindRepositorySourceOrdinal(
        GenerationModel model,
        RepositorySnapshot repository)
    {
        int index = 0;
        while (index < model.Sources.Count)
        {
            if (model.Sources[index].Repository == repository)
            {
                return model.Sources[index].Ordinal;
            }
            index++;
        }
        return -1;
    }

    internal static XsdGenerationContext CreateRepositorySelectionContext(
        List<XsdGenerationUnit> units,
        RepositorySnapshot repository,
        GenerationOptions options,
        GenerationResult result)
    {
        XsdGenerationContext context = new XsdGenerationContext();
        context.Options = options;
        context.Result = result;
        Dictionary<string, MessageBuildingBlockDefinition> buildingBlocks =
            new Dictionary<string, MessageBuildingBlockDefinition>();
        Dictionary<string, MessageComponentDefinition> components =
            new Dictionary<string, MessageComponentDefinition>();
        Dictionary<string, MessageElementDefinition> elements =
            new Dictionary<string, MessageElementDefinition>();
        Dictionary<string, ChoiceDefinition> choices =
            new Dictionary<string, ChoiceDefinition>();
        Dictionary<string, DataTypeDefinition> dataTypes =
            new Dictionary<string, DataTypeDefinition>();
        Dictionary<string, CodeSetDefinition> codeSets =
            new Dictionary<string, CodeSetDefinition>();
        Dictionary<string, ExternalSchemaDefinition> externalSchemas =
            new Dictionary<string, ExternalSchemaDefinition>();
        BuildRepositorySelectionIndexes(
            repository,
            buildingBlocks,
            components,
            elements,
            choices,
            dataTypes,
            codeSets,
            externalSchemas);
        List<RepositoryReference> pending = new List<RepositoryReference>();
        int index = 0;
        while (index < units.Count)
        {
            XsdGenerationUnit unit = units[index];
            if (unit.Repository == repository && unit.Message != null)
            {
                if (context.Unit.Repository == null)
                {
                    context.Unit = unit;
                }
                context.Messages.Add(unit.Message);
                pending.AddRange(unit.Message.BuildingBlocks);
                pending.AddRange(unit.Message.Choices);
            }
            index++;
        }
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        index = 0;
        while (index < pending.Count)
        {
            RepositoryReference reference = pending[index];
            string identifier = reference.TargetIdentifier;
            if (identifier.Length == 0)
            {
                identifier = reference.RawValue;
            }
            string localName = XsdLogicalModelEmitter.FindQualifiedLocalName(identifier);
            string prefix = XsdLogicalModelEmitter.FindQualifiedPrefix(identifier);
            if (prefix == "xs" || prefix == "xsd" ||
                XsdLogicalModelEmitter.BuiltInSchemaTypeExists(localName))
            {
                index++;
                continue;
            }
            RepositoryEntityKind kind = FindRepositorySelectionKind(
                reference.TargetKind,
                identifier,
                localName,
                buildingBlocks,
                components,
                elements,
                choices,
                dataTypes,
                codeSets,
                externalSchemas);
            string identity = ((int)kind).ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "\n" + identifier;
            if (!visited.Add(identity))
            {
                index++;
                continue;
            }
            if (kind == RepositoryEntityKind.MessageBuildingBlock &&
                buildingBlocks.ContainsKey(identifier))
            {
                MessageBuildingBlockDefinition block = buildingBlocks[identifier];
                context.BuildingBlocks.Add(block);
                pending.Add(FindBuildingBlockReference(block));
            }
            else if (kind == RepositoryEntityKind.MessageComponent &&
                components.ContainsKey(identifier))
            {
                MessageComponentDefinition component = components[identifier];
                context.Components.Add(component);
                pending.AddRange(component.MessageElements);
                pending.AddRange(component.Choices);
            }
            else if (kind == RepositoryEntityKind.MessageElement &&
                elements.ContainsKey(identifier))
            {
                MessageElementDefinition element = elements[identifier];
                context.Elements.Add(element);
                pending.Add(FindMessageElementReference(element));
            }
            else if (kind == RepositoryEntityKind.Choice &&
                choices.ContainsKey(identifier))
            {
                ChoiceDefinition choice = choices[identifier];
                context.Choices.Add(choice);
                pending.AddRange(choice.MessageElements);
                pending.AddRange(choice.ImpactedMessageElements);
                pending.AddRange(choice.ImpactedBuildingBlocks);
            }
            else if (kind == RepositoryEntityKind.DataType &&
                dataTypes.ContainsKey(identifier))
            {
                DataTypeDefinition dataType = dataTypes[identifier];
                context.DataTypes.Add(dataType);
                if (dataType.CurrencyIdentifierSet != null)
                {
                    pending.Add(dataType.CurrencyIdentifierSet);
                }
                if (dataType.SchemaTypeName.Length > 0)
                {
                    RepositoryReference schemaType = new RepositoryReference();
                    schemaType.TargetIdentifier = dataType.SchemaTypeName;
                    schemaType.RawValue = dataType.SchemaTypeName;
                    pending.Add(schemaType);
                }
            }
            else if (kind == RepositoryEntityKind.CodeSet &&
                codeSets.ContainsKey(identifier))
            {
                CodeSetDefinition codeSet = codeSets[identifier];
                context.CodeSets.Add(codeSet);
                if (codeSet.BaseCodeSet != null)
                {
                    pending.Add(codeSet.BaseCodeSet);
                }
            }
            else if (kind == RepositoryEntityKind.ExternalSchema &&
                externalSchemas.ContainsKey(identifier))
            {
                context.ExternalSchemas.Add(externalSchemas[identifier]);
            }
            index++;
        }
        return context;
    }

    internal static void BuildRepositorySelectionIndexes(
        RepositorySnapshot repository,
        Dictionary<string, MessageBuildingBlockDefinition> buildingBlocks,
        Dictionary<string, MessageComponentDefinition> components,
        Dictionary<string, MessageElementDefinition> elements,
        Dictionary<string, ChoiceDefinition> choices,
        Dictionary<string, DataTypeDefinition> dataTypes,
        Dictionary<string, CodeSetDefinition> codeSets,
        Dictionary<string, ExternalSchemaDefinition> externalSchemas)
    {
        int index = 0;
        while (index < repository.MessageBuildingBlocks.Count)
        {
            MessageBuildingBlockDefinition value = repository.MessageBuildingBlocks[index];
            if (!buildingBlocks.ContainsKey(value.Identifier))
            {
                buildingBlocks.Add(value.Identifier, value);
            }
            index++;
        }
        index = 0;
        while (index < repository.MessageComponents.Count)
        {
            MessageComponentDefinition value = repository.MessageComponents[index];
            if (!components.ContainsKey(value.Identifier))
            {
                components.Add(value.Identifier, value);
            }
            index++;
        }
        index = 0;
        while (index < repository.MessageElements.Count)
        {
            MessageElementDefinition value = repository.MessageElements[index];
            if (!elements.ContainsKey(value.Identifier))
            {
                elements.Add(value.Identifier, value);
            }
            index++;
        }
        index = 0;
        while (index < repository.Choices.Count)
        {
            ChoiceDefinition value = repository.Choices[index];
            if (!choices.ContainsKey(value.Identifier))
            {
                choices.Add(value.Identifier, value);
            }
            index++;
        }
        index = 0;
        while (index < repository.DataTypes.Count)
        {
            DataTypeDefinition value = repository.DataTypes[index];
            if (!dataTypes.ContainsKey(value.Identifier))
            {
                dataTypes.Add(value.Identifier, value);
            }
            index++;
        }
        index = 0;
        while (index < repository.CodeSets.Count)
        {
            CodeSetDefinition value = repository.CodeSets[index];
            if (!codeSets.ContainsKey(value.Identifier))
            {
                codeSets.Add(value.Identifier, value);
            }
            index++;
        }
        index = 0;
        while (index < repository.ExternalSchemas.Count)
        {
            ExternalSchemaDefinition value = repository.ExternalSchemas[index];
            if (!externalSchemas.ContainsKey(value.Identifier))
            {
                externalSchemas.Add(value.Identifier, value);
            }
            index++;
        }
    }

    internal static RepositoryEntityKind FindRepositorySelectionKind(
        RepositoryEntityKind requested,
        string identifier,
        string localName,
        Dictionary<string, MessageBuildingBlockDefinition> buildingBlocks,
        Dictionary<string, MessageComponentDefinition> components,
        Dictionary<string, MessageElementDefinition> elements,
        Dictionary<string, ChoiceDefinition> choices,
        Dictionary<string, DataTypeDefinition> dataTypes,
        Dictionary<string, CodeSetDefinition> codeSets,
        Dictionary<string, ExternalSchemaDefinition> externalSchemas)
    {
        if (requested != RepositoryEntityKind.Unknown)
        {
            return requested;
        }
        string candidate = identifier;
        if (candidate.Length == 0)
        {
            candidate = localName;
        }
        if (buildingBlocks.ContainsKey(candidate))
        {
            return RepositoryEntityKind.MessageBuildingBlock;
        }
        if (components.ContainsKey(candidate))
        {
            return RepositoryEntityKind.MessageComponent;
        }
        if (elements.ContainsKey(candidate))
        {
            return RepositoryEntityKind.MessageElement;
        }
        if (choices.ContainsKey(candidate))
        {
            return RepositoryEntityKind.Choice;
        }
        if (dataTypes.ContainsKey(candidate))
        {
            return RepositoryEntityKind.DataType;
        }
        if (codeSets.ContainsKey(candidate))
        {
            return RepositoryEntityKind.CodeSet;
        }
        if (externalSchemas.ContainsKey(candidate))
        {
            return RepositoryEntityKind.ExternalSchema;
        }
        return RepositoryEntityKind.Unknown;
    }

    internal static int FindOrAddGenerationSource(
        GenerationModel model,
        XsdGenerationUnit unit)
    {
        int index = 0;
        while (index < model.Sources.Count)
        {
            GenerationSource existing = model.Sources[index];
            if ((unit.Repository != null && existing.Repository == unit.Repository) ||
                (unit.Schema != null && existing.Schema == unit.Schema))
            {
                return existing.Ordinal;
            }
            index++;
        }

        GenerationSource source = new GenerationSource();
        source.Ordinal = model.Sources.Count;
        if (unit.Repository != null)
        {
            source.Kind = GenerationSourceKind.Repository;
            source.Repository = unit.Repository;
            source.Identifier = unit.Repository.Identifier;
            source.Edition = unit.Repository.Edition;
            source.Artifact = unit.Repository.Provenance.Artifact;
        }
        else if (unit.Schema != null)
        {
            source.Kind = GenerationSourceKind.Xsd;
            source.Schema = unit.Schema;
            source.Identifier = unit.Schema.SourceName;
            source.NamespaceUri = unit.Schema.NamespaceUri;
            source.Version = unit.Schema.Version;
            source.Profile = unit.Schema.Profile;
            source.Artifact = unit.Schema.Artifact;
        }
        model.Sources.Add(source);
        return source.Ordinal;
    }

    internal static void RemoveDuplicateConcepts(List<GenerationConcept> concepts)
    {
        HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < concepts.Count)
        {
            GenerationConcept value = concepts[index];
            string identity = CreateGenerationIdentity(
                value.SourceOrdinal,
                value.Kind,
                value.Identifier);
            if (value.Identifier.Length > 0 && !identities.Add(identity))
            {
                concepts.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
    }

    internal static bool ConceptProducesCSharpType(GenerationConcept concept)
    {
        if (concept.Kind == GenerationConceptKind.MessageDefinition ||
            concept.Kind == GenerationConceptKind.MessageComponent ||
            concept.Kind == GenerationConceptKind.DataType ||
            concept.Kind == GenerationConceptKind.CodeSet ||
            concept.Kind == GenerationConceptKind.ExternalSchema)
        {
            return true;
        }
        return concept.Kind == GenerationConceptKind.Choice &&
            concept.Choice != null && concept.Choice.MessageElements.Count > 0;
    }

    internal static List<CSharpGeneratedSource> EmitCSharpSources(
        CSharpGenerationContext context)
    {
        List<CSharpGeneratedSource> sources = new List<CSharpGeneratedSource>();
        Dictionary<string, CSharpGeneratedSource> sourcesByType =
            new Dictionary<string, CSharpGeneratedSource>();
        int index = 0;
        while (index < context.Symbols.Symbols.Count)
        {
            context.Plan.Options.CancellationToken.ThrowIfCancellationRequested();
            GenerationSymbolEntry symbol = context.Symbols.Symbols[index];
            string identity = symbol.NamespaceName + "\n" + symbol.SymbolName;
            if (sourcesByType.ContainsKey(identity))
            {
                MergeGeneratedSourceIdentifier(
                    sourcesByType[identity],
                    symbol.Concept.Identifier);
            }
            else
            {
                CSharpGeneratedSource source = EmitSingleCSharpSource(context, symbol);
                sources.Add(source);
                sourcesByType.Add(identity, source);
                if (symbol.Concept.Kind == GenerationConceptKind.Choice)
                {
                    CSharpGeneratedSource discriminator = EmitChoiceKindSource(
                        context,
                        symbol);
                    sources.Add(discriminator);
                }
            }
            index++;
        }
        SortGeneratedSources(sources);
        return sources;
    }

    internal static CSharpGeneratedSource? FindGeneratedTypeSource(
        List<CSharpGeneratedSource> sources,
        string namespaceName,
        string symbolName)
    {
        int index = 0;
        while (index < sources.Count)
        {
            if (sources[index].NamespaceName == namespaceName &&
                sources[index].TypeName == symbolName)
            {
                return sources[index];
            }
            index++;
        }
        return null;
    }

    internal static void MergeGeneratedSourceIdentifier(
        CSharpGeneratedSource source,
        string identifier)
    {
        if (source.SourceIdentifier.Length == 0)
        {
            source.SourceIdentifier = identifier;
        }
    }

    internal static CSharpGeneratedSource EmitSingleCSharpSource(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        StringBuilder builder = new StringBuilder();
        AppendSourceLine(builder, context, 0, "namespace " + symbol.NamespaceName + ";");
        AppendSourceLine(builder, context, 0, string.Empty);
        if (symbol.Concept.Kind == GenerationConceptKind.MessageDefinition)
        {
            WriteMessageDefinitionClass(builder, context, symbol);
        }
        else if (symbol.Concept.Kind == GenerationConceptKind.MessageComponent)
        {
            WriteMessageComponentClass(builder, context, symbol);
        }
        else if (symbol.Concept.Kind == GenerationConceptKind.Choice)
        {
            WriteChoiceClass(builder, context, symbol);
        }
        else if (symbol.Concept.Kind == GenerationConceptKind.DataType)
        {
            WriteDataTypeClass(builder, context, symbol);
        }
        else if (symbol.Concept.Kind == GenerationConceptKind.CodeSet)
        {
            WriteCodeSetType(builder, context, symbol);
        }
        else if (symbol.Concept.Kind == GenerationConceptKind.ExternalSchema)
        {
            WriteExternalSchemaClass(builder, context, symbol);
        }

        CSharpGeneratedSource source = new CSharpGeneratedSource();
        source.Symbol = symbol;
        source.TypeName = symbol.SymbolName;
        source.NamespaceName = symbol.NamespaceName;
        source.SourceIdentifier = symbol.Concept.Identifier;
        string fileName = RemoveIdentifierEscape(symbol.SymbolName);
        source.RelativePath = FindCSharpOutputDirectory(context, symbol, false) + "/" +
            fileName + ".cs";
        source.Content = Encoding.UTF8.GetBytes(builder.ToString());
        return source;
    }

    internal static CSharpGeneratedSource EmitChoiceKindSource(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        StringBuilder builder = new StringBuilder();
        AppendSourceLine(builder, context, 0, "namespace " + symbol.NamespaceName + ";");
        AppendSourceLine(builder, context, 0, string.Empty);
        WriteChoiceKindEnum(builder, context, symbol);
        string kindName = FindChoiceKindName(context, symbol);
        CSharpGeneratedSource source = new CSharpGeneratedSource();
        source.Symbol = symbol;
        source.TypeName = kindName;
        source.NamespaceName = symbol.NamespaceName;
        source.SourceIdentifier = symbol.Concept.Identifier;
        source.RelativePath = FindCSharpOutputDirectory(context, symbol, true) + "/" +
            RemoveIdentifierEscape(kindName) + ".cs";
        source.Content = Encoding.UTF8.GetBytes(builder.ToString());
        return source;
    }

    internal static string FindCSharpOutputDirectory(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        bool choiceKind)
    {
        bool isEnum = choiceKind;
        if (symbol.Concept.CodeSet != null &&
            CodeSetIsClosed(symbol.Concept.CodeSet))
        {
            isEnum = true;
        }
        string category = "Data";
        if (isEnum)
        {
            category = "Enums";
        }
        string edition = CreateCSharpPathSegment(
            context.Plan.Options.ArtifactEdition,
            "Unspecified");
        return "Api/" + category + "/Generated/" + edition + "/" +
            symbol.NamespaceName.Replace('.', '/');
    }

    internal static string CreateCSharpPathSegment(string value, string fallback)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if ((current >= 'a' && current <= 'z') ||
                (current >= 'A' && current <= 'Z') ||
                (current >= '0' && current <= '9') ||
                current == '.' || current == '-' || current == '_')
            {
                result.Append(current);
            }
            else
            {
                result.Append('_');
            }
            index++;
        }
        if (result.Length == 0)
        {
            return fallback;
        }
        return result.ToString();
    }

    internal static void WriteMessageDefinitionClass(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        MessageDefinitionMetadata? message = symbol.Concept.MessageDefinition;
        WritePublicTypeDocumentation(builder, context, 0, "Root model", symbol);
        AppendSourceLine(builder, context, 0, "public class " + symbol.SymbolName);
        AppendSourceLine(builder, context, 0, "{");
        if (message != null)
        {
            List<string> usedNames = new List<string>();
            usedNames.Add(symbol.SymbolName);
            int index = 0;
            while (index < message.BuildingBlocks.Count)
            {
                MessageBuildingBlockDefinition? block = FindBuildingBlockConcept(
                    context,
                    symbol.Concept.SourceOrdinal,
                    message.BuildingBlocks[index].TargetIdentifier);
                if (block != null)
                {
                    RepositoryReference reference = FindBuildingBlockReference(block);
                    CSharpTypeReference type = FindCSharpReferenceType(
                        context,
                        symbol.Concept.SourceOrdinal,
                        reference);
                    string memberName = CreateUniqueMemberName(
                        usedNames,
                        FindMemberCandidate(block.XmlTag, block.Name),
                        block.Identifier);
                    WriteModelField(builder, context, type, memberName,
                        block.Multiplicity, block.Name, block.Definition, usedNames);
                }
                index++;
            }
            if (message.BuildingBlocks.Count == 0 && message.XmlName.Length > 0)
            {
                RepositoryReference rootReference = new RepositoryReference();
                rootReference.TargetIdentifier = message.XmlName;
                rootReference.RawValue = message.XmlName;
                rootReference.TargetKind = RepositoryEntityKind.MessageComponent;
                CSharpTypeReference rootType = FindCSharpReferenceType(
                    context,
                    symbol.Concept.SourceOrdinal,
                    rootReference);
                MultiplicityDefinition rootMultiplicity = new MultiplicityDefinition();
                rootMultiplicity.MinimumOccurs = 1;
                rootMultiplicity.HasMaximumOccurs = true;
                rootMultiplicity.MaximumOccurs = 1;
                string rootMember = CreateUniqueMemberName(
                    usedNames,
                    "Value",
                    message.Identifier + "|root");
                WriteModelField(builder, context, rootType, rootMember,
                    rootMultiplicity, message.RootElement, message.Definition, usedNames);
            }
        }
        AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteMessageComponentClass(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        MessageComponentDefinition? component = symbol.Concept.MessageComponent;
        WritePublicTypeDocumentation(builder, context, 0, "Message component", symbol);
        AppendSourceLine(builder, context, 0, "public class " + symbol.SymbolName);
        AppendSourceLine(builder, context, 0, "{");
        if (component != null)
        {
            List<string> usedNames = new List<string>();
            usedNames.Add(symbol.SymbolName);
            int index = 0;
            while (index < component.MessageElements.Count)
            {
                MessageElementDefinition? element = FindMessageElementConcept(
                    context,
                    symbol.Concept.SourceOrdinal,
                    component.MessageElements[index].TargetIdentifier);
                if (element != null)
                {
                    RepositoryReference reference = FindMessageElementReference(element);
                    CSharpTypeReference type = FindCSharpReferenceType(
                        context,
                        symbol.Concept.SourceOrdinal,
                        reference);
                    if (element.Kind == MessageElementKind.AssociationEnd &&
                        !element.IsComposite)
                    {
                        type = CreateRuntimeTypeReference("IsoTextValue");
                    }
                    string memberName = CreateUniqueMemberName(
                        usedNames,
                        FindMemberCandidate(element.XmlTag, element.Name),
                        element.Identifier);
                    WriteModelField(builder, context, type, memberName,
                        element.Multiplicity, element.Name, element.Definition, usedNames);
                }
                index++;
            }
        }
        AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteChoiceKindEnum(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        ChoiceDefinition? choice = symbol.Concept.Choice;
        string kindName = FindChoiceKindName(context, symbol);
        AppendSourceLine(builder, context, 0,
            "/// <summary>Identifies the selected " +
            EscapeXmlDocumentation(RemoveIdentifierEscape(symbol.SymbolName)) +
            " alternative.</summary>");
        AppendSourceLine(builder, context, 0, "public enum " + kindName);
        AppendSourceLine(builder, context, 0, "{");
        AppendSourceLine(builder, context, 1,
            "/// <summary>No alternative has been selected.</summary>");
        AppendSourceLine(builder, context, 1, "None = 0,");
        List<string> enumNames = new List<string>();
        enumNames.Add(kindName);
        enumNames.Add("None");
        if (choice != null)
        {
            int enumIndex = 0;
            while (enumIndex < choice.MessageElements.Count)
            {
                MessageElementDefinition? element = FindMessageElementConcept(
                    context,
                    symbol.Concept.SourceOrdinal,
                    choice.MessageElements[enumIndex].TargetIdentifier);
                if (element != null)
                {
                    string enumName = CreateUniqueMemberName(
                        enumNames,
                        FindMemberCandidate(element.XmlTag, element.Name),
                        element.Identifier);
                    WritePublicFieldDocumentation(builder, context, 1,
                        element.Name, element.Definition);
                    AppendSourceLine(builder, context, 1, enumName + " = " +
                        (enumIndex + 1).ToString(
                            System.Globalization.CultureInfo.InvariantCulture) + ",");
                }
                enumIndex++;
            }
        }
        AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteChoiceClass(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        ChoiceDefinition? choice = symbol.Concept.Choice;
        string kindName = FindChoiceKindName(context, symbol);
        WritePublicTypeDocumentation(builder, context, 0, "Choice model", symbol);
        AppendSourceLine(builder, context, 0, "public class " + symbol.SymbolName);
        AppendSourceLine(builder, context, 0, "{");
        AppendSourceLine(builder, context, 1,
            "/// <summary>The selected choice alternative.</summary>");
        AppendSourceLine(builder, context, 1, "public " + kindName + " Choice;");
        if (choice != null)
        {
            List<string> usedNames = new List<string>();
            usedNames.Add(symbol.SymbolName);
            usedNames.Add("Choice");
            int index = 0;
            while (index < choice.MessageElements.Count)
            {
                MessageElementDefinition? element = FindMessageElementConcept(
                    context,
                    symbol.Concept.SourceOrdinal,
                    choice.MessageElements[index].TargetIdentifier);
                if (element != null)
                {
                    CSharpTypeReference type = FindCSharpReferenceType(
                        context,
                        symbol.Concept.SourceOrdinal,
                        FindMessageElementReference(element));
                    string memberName = CreateUniqueMemberName(
                        usedNames,
                        FindMemberCandidate(element.XmlTag, element.Name),
                        element.Identifier);
                    MultiplicityDefinition optional = new MultiplicityDefinition();
                    optional.MinimumOccurs = 0;
                    optional.HasMaximumOccurs = element.Multiplicity.HasMaximumOccurs;
                    optional.MaximumOccurs = element.Multiplicity.MaximumOccurs;
                    WriteModelField(builder, context, type, memberName, optional,
                        element.Name, element.Definition, usedNames);
                }
                index++;
            }
        }
        AppendSourceLine(builder, context, 0, "}");
    }

    internal static string FindChoiceKindName(
        CSharpGenerationContext context,
        GenerationSymbolEntry choice)
    {
        GenerationIdentifierNamer namer = new GenerationIdentifierNamer();
        string candidate = namer.CreateTypeName(
            RemoveIdentifierEscape(choice.SymbolName) + "Kind");
        string identity = choice.NamespaceName + "\n" + candidate;
        if (context.PublicSymbolNames.Contains(identity))
        {
            return namer.AppendDisambiguator(
                candidate,
                GenerationStableHash.CreateHex(choice.Concept.Identifier).Substring(0, 16));
        }
        return candidate;
    }

    internal static void WriteDataTypeClass(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        DataTypeDefinition? dataType = symbol.Concept.DataType;
        WritePublicTypeDocumentation(builder, context, 0, "Value wrapper", symbol);
        AppendSourceLine(builder, context, 0, "public class " + symbol.SymbolName);
        AppendSourceLine(builder, context, 0, "{");
        CSharpTypeReference type = CreateRuntimeTypeReference("IsoTextValue");
        if (dataType != null)
        {
            type = FindDataTypeValueType(dataType);
        }
        AppendSourceLine(builder, context, 1,
            "/// <summary>The exact ISO value and lexical state.</summary>");
        AppendSourceLine(builder, context, 1, "public " + type.Name +
            " Value = new " + type.Name + "();");
        AppendSourceLine(builder, context, 0, "}");
    }

    internal static CSharpTypeReference FindDataTypeValueType(DataTypeDefinition dataType)
    {
        if (dataType.Kind == DataTypeKind.Boolean ||
            dataType.Kind == DataTypeKind.Indicator)
        {
            return CreateRuntimeTypeReference("IsoBooleanValue");
        }
        if (dataType.Kind == DataTypeKind.Decimal ||
            dataType.Kind == DataTypeKind.Quantity ||
            dataType.Kind == DataTypeKind.Rate)
        {
            return CreateRuntimeTypeReference("IsoDecimalValue");
        }
        if (dataType.Kind == DataTypeKind.Amount)
        {
            return CreateRuntimeTypeReference("IsoAmountValue");
        }
        if (dataType.Kind == DataTypeKind.Binary)
        {
            return CreateRuntimeTypeReference("IsoBinaryValue");
        }
        if (dataType.Kind == DataTypeKind.Date)
        {
            return CreateRuntimeTypeReference("IsoDateValue");
        }
        if (dataType.Kind == DataTypeKind.Time)
        {
            return CreateRuntimeTypeReference("IsoTimeValue");
        }
        if (dataType.Kind == DataTypeKind.DateTime)
        {
            return CreateRuntimeTypeReference("IsoDateTimeValue");
        }
        if (dataType.Kind == DataTypeKind.Duration)
        {
            return CreateRuntimeTypeReference("IsoDurationValue");
        }
        if (dataType.Kind == DataTypeKind.Day ||
            dataType.Kind == DataTypeKind.Month ||
            dataType.Kind == DataTypeKind.MonthDay ||
            dataType.Kind == DataTypeKind.Year ||
            dataType.Kind == DataTypeKind.YearMonth)
        {
            return CreateRuntimeTypeReference("IsoPartialDateValue");
        }
        if (dataType.Kind == DataTypeKind.Schema)
        {
            return FindBuiltInRuntimeType(dataType.SchemaTypeName);
        }
        return CreateRuntimeTypeReference("IsoTextValue");
    }

    internal static void WriteCodeSetType(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        CodeSetDefinition? codeSet = symbol.Concept.CodeSet;
        if (codeSet != null && CodeSetIsClosed(codeSet))
        {
            WriteClosedCodeEnum(builder, context, symbol, codeSet);
            return;
        }
        WritePublicTypeDocumentation(builder, context, 0, "External code value", symbol);
        AppendSourceLine(builder, context, 0, "public class " + symbol.SymbolName);
        AppendSourceLine(builder, context, 0, "{");
        AppendSourceLine(builder, context, 1,
            "/// <summary>The exact externally maintained code value.</summary>");
        AppendSourceLine(builder, context, 1,
            "public global::iso20022.IsoCodeValue Value = " +
            "new global::iso20022.IsoCodeValue();");
        AppendSourceLine(builder, context, 0, "}");
    }

    internal static bool CodeSetIsClosed(CodeSetDefinition codeSet)
    {
        return codeSet.Kind != CodeSetKind.External &&
            CollectClosedCodeValues(codeSet).Count > 0;
    }

    internal static List<CodeValueDefinition> CollectClosedCodeValues(
        CodeSetDefinition codeSet)
    {
        List<CodeValueDefinition> codes = new List<CodeValueDefinition>();
        int index = 0;
        while (index < codeSet.Codes.Count)
        {
            if (codeSet.Codes[index].Value.Length > 0)
            {
                codes.Add(codeSet.Codes[index]);
            }
            index++;
        }
        XsdLogicalModelEmitter.SortCodeValues(codes);
        index = 1;
        while (index < codes.Count)
        {
            if (codes[index].Value == codes[index - 1].Value)
            {
                codes.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
        return codes;
    }

    internal static void WriteClosedCodeEnum(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        CodeSetDefinition codeSet)
    {
        WritePublicTypeDocumentation(builder, context, 0, "Closed code set", symbol);
        AppendSourceLine(builder, context, 0, "public enum " + symbol.SymbolName);
        AppendSourceLine(builder, context, 0, "{");
        AppendSourceLine(builder, context, 1,
            "/// <summary>No registered code value has been selected.</summary>");
        AppendSourceLine(builder, context, 1, "Unknown = 0,");
        List<CodeValueDefinition> codes = CollectClosedCodeValues(codeSet);
        List<string> usedNames = new List<string>();
        usedNames.Add(symbol.SymbolName);
        usedNames.Add("Unknown");
        int index = 0;
        while (index < codes.Count)
        {
            CodeValueDefinition code = codes[index];
            string candidate = code.Name;
            if (candidate.Length == 0)
            {
                candidate = code.Value;
            }
            string name = CreateUniqueMemberName(
                usedNames,
                candidate,
                code.Identifier + "|" + code.Value);
            WritePublicFieldDocumentation(builder, context, 1,
                code.Name, code.Definition);
            AppendSourceLine(builder, context, 1, name + " = " +
                (index + 1).ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + ",");
            index++;
        }
        AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteExternalSchemaClass(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        WritePublicTypeDocumentation(builder, context, 0, "Wildcard content wrapper", symbol);
        AppendSourceLine(builder, context, 0, "public class " + symbol.SymbolName);
        AppendSourceLine(builder, context, 0, "{");
        AppendSourceLine(builder, context, 1,
            "/// <summary>The namespace-complete raw external content.</summary>");
        AppendSourceLine(builder, context, 1,
            "public global::iso20022.RawXmlFragment Value = " +
            "new global::iso20022.RawXmlFragment();");
        AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteModelField(
        StringBuilder builder,
        CSharpGenerationContext context,
        CSharpTypeReference type,
        string memberName,
        MultiplicityDefinition multiplicity,
        string sourceName,
        string definition,
        List<string> usedNames)
    {
        WritePublicFieldDocumentation(builder, context, 1, sourceName, definition);
        bool repeated = !multiplicity.HasMaximumOccurs || multiplicity.MaximumOccurs > 1;
        if (repeated)
        {
            string listType = "global::System.Collections.Generic.List<" + type.Name + ">";
            AppendSourceLine(builder, context, 1, "public " + listType + " " +
                memberName + " = new " + listType + "();");
            return;
        }
        if (type.IsValueType)
        {
            AppendSourceLine(builder, context, 1,
                "public " + type.Name + " " + memberName + ";");
            if (multiplicity.MinimumOccurs == 0)
            {
                string presenceName = CreateUniquePresenceName(
                    usedNames,
                    memberName,
                    sourceName + "|presence");
                AppendSourceLine(builder, context, 1,
                    "/// <summary>Whether " +
                    EscapeXmlDocumentation(sourceName) + " is present.</summary>");
                AppendSourceLine(builder, context, 1,
                    "public bool " + presenceName + ";");
            }
            return;
        }
        if (multiplicity.MinimumOccurs == 0)
        {
            AppendSourceLine(builder, context, 1,
                "public " + type.Name + "? " + memberName + ";");
        }
        else
        {
            AppendSourceLine(builder, context, 1,
                "public " + type.Name + " " + memberName +
                " = new " + type.Name + "();");
        }
    }

    internal static string CreateUniquePresenceName(
        List<string> usedNames,
        string memberName,
        string identity)
    {
        return CreateUniqueMemberName(usedNames, "Has" +
            RemoveIdentifierEscape(memberName), identity);
    }

    internal static string CreateUniqueMemberName(
        List<string> usedNames,
        string candidate,
        string identity)
    {
        GenerationIdentifierNamer namer = new GenerationIdentifierNamer();
        string name = namer.CreateMemberName(candidate);
        if (!TextListContains(usedNames, name))
        {
            usedNames.Add(name);
            return name;
        }
        string suffix = GenerationStableHash.CreateHex(identity).Substring(0, 16);
        name = namer.AppendDisambiguator(name, suffix);
        int ordinal = 2;
        while (TextListContains(usedNames, name))
        {
            name = namer.AppendDisambiguator(
                name,
                ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
            ordinal++;
        }
        usedNames.Add(name);
        return name;
    }

    internal static bool TextListContains(List<string> values, string value)
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

    internal static string FindMemberCandidate(string xmlTag, string name)
    {
        if (name.Length > 0)
        {
            return name;
        }
        return xmlTag;
    }

    internal static RepositoryReference FindBuildingBlockReference(
        MessageBuildingBlockDefinition block)
    {
        if (block.SimpleType != null)
        {
            return block.SimpleType;
        }
        if (block.ComplexType != null)
        {
            return block.ComplexType;
        }
        return block.MemberType;
    }

    internal static RepositoryReference FindMessageElementReference(
        MessageElementDefinition element)
    {
        if (element.SimpleType != null)
        {
            return element.SimpleType;
        }
        if (element.ComplexType != null)
        {
            return element.ComplexType;
        }
        return element.MemberType;
    }

    internal static MessageBuildingBlockDefinition? FindBuildingBlockConcept(
        CSharpGenerationContext context,
        int sourceOrdinal,
        string identifier)
    {
        string identity = CreateGenerationIdentity(
            sourceOrdinal,
            GenerationConceptKind.MessageBuildingBlock,
            identifier);
        if (context.ConceptsByIdentity.ContainsKey(identity))
        {
            return context.ConceptsByIdentity[identity].MessageBuildingBlock;
        }
        return null;
    }

    internal static MessageElementDefinition? FindMessageElementConcept(
        CSharpGenerationContext context,
        int sourceOrdinal,
        string identifier)
    {
        string identity = CreateGenerationIdentity(
            sourceOrdinal,
            GenerationConceptKind.MessageElement,
            identifier);
        if (context.ConceptsByIdentity.ContainsKey(identity))
        {
            return context.ConceptsByIdentity[identity].MessageElement;
        }
        return null;
    }

    internal static CSharpTypeReference FindCSharpReferenceType(
        CSharpGenerationContext context,
        int sourceOrdinal,
        RepositoryReference reference)
    {
        string target = reference.TargetIdentifier;
        if (target.Length == 0)
        {
            target = reference.RawValue;
        }
        string prefix = XsdLogicalModelEmitter.FindQualifiedPrefix(target);
        string localName = XsdLogicalModelEmitter.FindQualifiedLocalName(target);
        if (prefix == "xs" || prefix == "xsd" ||
            XsdLogicalModelEmitter.BuiltInSchemaTypeExists(localName))
        {
            return FindBuiltInRuntimeType(localName);
        }

        GenerationSymbolEntry? symbol = FindReferenceSymbol(
            context,
            sourceOrdinal,
            target,
            localName,
            reference.TargetKind);
        if (symbol != null)
        {
            CSharpTypeReference result = new CSharpTypeReference();
            result.Name = "global::" + symbol.NamespaceName + "." + symbol.SymbolName;
            if (symbol.Concept.CodeSet != null &&
                CodeSetIsClosed(symbol.Concept.CodeSet))
            {
                result.IsValueType = true;
            }
            return result;
        }
        AddCSharpGenerationDiagnostic(
            context,
            "generation.csharp.type_fallback",
            "A member type could not be resolved and was generated as ISO text.",
            target);
        return CreateRuntimeTypeReference("IsoTextValue");
    }

    internal static void AddCSharpGenerationDiagnostic(
        CSharpGenerationContext context,
        string code,
        string message,
        string sourceIdentifier)
    {
        if (context.Result.DiagnosticCodes.Count >=
            context.Plan.Options.Limits.MaxDiagnosticCount)
        {
            return;
        }
        context.Result.DiagnosticCodes.Add(code);
        context.Result.DiagnosticMessages.Add(message);
        context.Result.DiagnosticSourceIdentifiers.Add(sourceIdentifier);
    }

    internal static GenerationSymbolEntry? FindReferenceSymbol(
        CSharpGenerationContext context,
        int sourceOrdinal,
        string target,
        string localName,
        RepositoryEntityKind targetKind)
    {
        GenerationConceptKind expected = XsdLogicalModelEmitter.MapRepositoryKind(targetKind);
        if (expected != GenerationConceptKind.Unknown)
        {
            string identity = CreateGenerationIdentity(sourceOrdinal, expected, target);
            if (context.SymbolsByIdentity.ContainsKey(identity))
            {
                return context.SymbolsByIdentity[identity];
            }
            identity = CreateGenerationIdentity(sourceOrdinal, expected, localName);
            if (context.SymbolsByIdentity.ContainsKey(identity))
            {
                return context.SymbolsByIdentity[identity];
            }
        }
        int index = 0;
        while (index < context.Symbols.Symbols.Count)
        {
            GenerationSymbolEntry symbol = context.Symbols.Symbols[index];
            if (symbol.Concept.SourceOrdinal == sourceOrdinal &&
                (expected == GenerationConceptKind.Unknown ||
                symbol.Concept.Kind == expected) &&
                SymbolMatchesReference(symbol, target, localName))
            {
                return symbol;
            }
            index++;
        }
        index = 0;
        while (index < context.Symbols.Symbols.Count)
        {
            GenerationSymbolEntry symbol = context.Symbols.Symbols[index];
            if ((expected == GenerationConceptKind.Unknown ||
                symbol.Concept.Kind == expected) &&
                SymbolMatchesReference(symbol, target, localName))
            {
                return symbol;
            }
            index++;
        }
        return null;
    }

    internal static bool SymbolMatchesReference(
        GenerationSymbolEntry symbol,
        string target,
        string localName)
    {
        return symbol.Concept.Identifier == target ||
            symbol.Concept.Identifier == localName ||
            symbol.Concept.Name == target ||
            symbol.Concept.Name == localName ||
            RemoveIdentifierEscape(symbol.SymbolName) == localName;
    }

    internal static CSharpTypeReference FindBuiltInRuntimeType(string localName)
    {
        if (localName == "boolean")
        {
            return CreateRuntimeTypeReference("IsoBooleanValue");
        }
        if (localName == "decimal" || localName == "integer" ||
            localName == "positiveInteger" || localName == "nonNegativeInteger" ||
            localName == "negativeInteger" || localName == "nonPositiveInteger" ||
            localName == "double" || localName == "float")
        {
            return CreateRuntimeTypeReference("IsoDecimalValue");
        }
        if (localName == "base64Binary" || localName == "hexBinary")
        {
            return CreateRuntimeTypeReference("IsoBinaryValue");
        }
        if (localName == "date")
        {
            return CreateRuntimeTypeReference("IsoDateValue");
        }
        if (localName == "time")
        {
            return CreateRuntimeTypeReference("IsoTimeValue");
        }
        if (localName == "dateTime")
        {
            return CreateRuntimeTypeReference("IsoDateTimeValue");
        }
        if (localName == "duration")
        {
            return CreateRuntimeTypeReference("IsoDurationValue");
        }
        if (localName == "gDay" || localName == "gMonth" ||
            localName == "gMonthDay" || localName == "gYear" ||
            localName == "gYearMonth")
        {
            return CreateRuntimeTypeReference("IsoPartialDateValue");
        }
        return CreateRuntimeTypeReference("IsoTextValue");
    }

    internal static CSharpTypeReference CreateRuntimeTypeReference(string typeName)
    {
        CSharpTypeReference result = new CSharpTypeReference();
        result.Name = "global::iso20022." + typeName;
        return result;
    }

    internal static void WritePublicTypeDocumentation(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string role,
        GenerationSymbolEntry symbol)
    {
        string summary = role + " for " + RemoveIdentifierEscape(symbol.SymbolName) + ".";
        if (context.Plan.Options.IncludeDocumentation &&
            symbol.Concept.Definition.Length > 0)
        {
            summary += " " + NormalizeDocumentationText(symbol.Concept.Definition);
        }
        AppendSourceLine(builder, context, level,
            "/// <summary>" + EscapeXmlDocumentation(summary) + "</summary>");
    }

    internal static void WritePublicFieldDocumentation(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string name,
        string definition)
    {
        string summary = "The " + name + " value.";
        if (name.Length == 0)
        {
            summary = "The generated member value.";
        }
        if (context.Plan.Options.IncludeDocumentation && definition.Length > 0)
        {
            summary += " " + NormalizeDocumentationText(definition);
        }
        AppendSourceLine(builder, context, level,
            "/// <summary>" + EscapeXmlDocumentation(summary) + "</summary>");
    }

    internal static string NormalizeDocumentationText(string value)
    {
        string result = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        while (result.Contains("  ", StringComparison.Ordinal))
        {
            result = result.Replace("  ", " ", StringComparison.Ordinal);
        }
        return result;
    }

    internal static string EscapeXmlDocumentation(string value)
    {
        return value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    internal static string RemoveIdentifierEscape(string value)
    {
        if (value.Length > 0 && value[0] == '@')
        {
            return value.Substring(1);
        }
        return value;
    }

    internal static void AppendSourceLine(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string value)
    {
        int index = 0;
        while (index < level)
        {
            builder.Append(context.Plan.Options.Indentation);
            index++;
        }
        builder.Append(value);
        builder.Append(context.Plan.Options.NewLine);
    }

    internal static void SortGeneratedSources(List<CSharpGeneratedSource> sources)
    {
        sources.Sort((left, right) => string.CompareOrdinal(
            left.RelativePath,
            right.RelativePath));
    }
}
