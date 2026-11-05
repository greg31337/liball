using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class CSharpMetadataEmitter
{
    internal static List<CSharpGeneratedSource> EmitMetadataSources(
        CSharpGenerationContext context)
    {
        List<CSharpGeneratedSource> sources = new List<CSharpGeneratedSource>();
        int index = 0;
        while (index < context.Symbols.Symbols.Count)
        {
            context.Plan.Options.CancellationToken.ThrowIfCancellationRequested();
            sources.Add(EmitTypeMetadataSource(
                context,
                context.Symbols.Symbols[index]));
            index++;
        }
        CSharpGeneratedSource? registry = EmitRegistrySource(context);
        if (registry != null)
        {
            sources.Add(registry);
        }
        CSharpGeneratedSource? readerRuntime = EmitReaderRuntimeSource(context);
        if (readerRuntime != null)
        {
            sources.Add(readerRuntime);
        }
        CSharpGeneratedSource? structuralRuntime =
            CSharpStructuralValidationEmitter.EmitRuntimeSource(context);
        if (structuralRuntime != null)
        {
            sources.Add(structuralRuntime);
        }
        CSharpModelEmitter.SortGeneratedSources(sources);
        return sources;
    }

    internal static CSharpGeneratedSource EmitTypeMetadataSource(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        StringBuilder builder = new StringBuilder();
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 0, "namespace " + symbol.NamespaceName + ";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, string.Empty);
        string metadataName = FindInternalGeneratedName(context, symbol, "Metadata");
        string codecName = FindInternalGeneratedName(context, symbol, "Codec");
        List<CSharpMemberMetadata> members = CollectMemberMetadata(context, symbol);
        WriteMetadataTable(builder, context, symbol, metadataName, members);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, string.Empty);
        WriteTypeCodec(builder, context, symbol, codecName, members);

        CSharpGeneratedSource source = new CSharpGeneratedSource();
        source.Symbol = symbol;
        source.Kind = GenerationFileKind.Metadata;
        source.TypeName = metadataName;
        source.NamespaceName = symbol.NamespaceName;
        source.SourceIdentifier = symbol.Concept.Identifier;
        source.RelativePath = FindInternalOutputDirectory(context, symbol.NamespaceName) +
            "/" + CSharpModelEmitter.RemoveIdentifierEscape(symbol.SymbolName) +
            ".Metadata.cs";
        source.Content = Encoding.UTF8.GetBytes(builder.ToString());
        return source;
    }

    internal static string FindInternalGeneratedName(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string suffix)
    {
        GenerationIdentifierNamer namer = new GenerationIdentifierNamer();
        string candidate = namer.CreateTypeName(
            CSharpModelEmitter.RemoveIdentifierEscape(symbol.SymbolName) + suffix);
        if (!InternalGeneratedNameExists(context, symbol.NamespaceName, candidate))
        {
            return candidate;
        }
        return namer.AppendDisambiguator(
            candidate,
            GenerationStableHash.CreateHex(
                symbol.Concept.Identifier + "|" + suffix).Substring(0, 16));
    }

    internal static bool InternalGeneratedNameExists(
        CSharpGenerationContext context,
        string namespaceName,
        string candidate)
    {
        return context.PublicTypeNames.Contains(namespaceName + "\n" + candidate);
    }

    internal static List<CSharpMemberMetadata> CollectMemberMetadata(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        List<CSharpMemberMetadata> members = new List<CSharpMemberMetadata>();
        if (symbol.Concept.MessageDefinition != null)
        {
            CollectMessageDefinitionMetadata(
                context,
                symbol,
                symbol.Concept.MessageDefinition,
                members);
        }
        else if (symbol.Concept.MessageComponent != null)
        {
            CollectComponentMetadata(
                context,
                symbol,
                symbol.Concept.MessageComponent,
                members);
        }
        else if (symbol.Concept.Choice != null)
        {
            CollectChoiceMetadata(
                context,
                symbol,
                symbol.Concept.Choice,
                members);
        }
        else if (symbol.Concept.CodeSet != null &&
            CSharpModelEmitter.CodeSetIsClosed(symbol.Concept.CodeSet))
        {
            CollectCodeSetMetadata(symbol, symbol.Concept.CodeSet, members);
        }
        else
        {
            CSharpMemberMetadata value = new CSharpMemberMetadata();
            value.Name = "Value";
            value.XmlName = string.Empty;
            value.TypeName = FindWrapperValueTypeName(symbol);
            value.MinimumOccurs = 1;
            value.HasMaximumOccurs = true;
            value.MaximumOccurs = 1;
            members.Add(value);
        }
        return members;
    }

    internal static void CollectMessageDefinitionMetadata(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        MessageDefinitionMetadata message,
        List<CSharpMemberMetadata> members)
    {
        List<string> usedNames = new List<string>();
        usedNames.Add(symbol.SymbolName);
        int index = 0;
        while (index < message.BuildingBlocks.Count)
        {
            MessageBuildingBlockDefinition? block = CSharpModelEmitter.FindBuildingBlockConcept(
                context,
                symbol.Concept.SourceOrdinal,
                message.BuildingBlocks[index].TargetIdentifier);
            if (block != null)
            {
                string memberName = CSharpModelEmitter.CreateUniqueMemberName(
                    usedNames,
                    CSharpModelEmitter.FindMemberCandidate(block.XmlTag, block.Name),
                    block.Identifier);
                AddReferenceMemberMetadata(
                    context,
                    symbol,
                    memberName,
                    FindMetadataXmlName(block.XmlTag, block.Name, block.Identifier),
                    CSharpModelEmitter.FindBuildingBlockReference(block),
                    block.Multiplicity,
                    members);
                CSharpMemberMetadata member = members[members.Count - 1];
                member.SourceIdentifier = block.Identifier;
                member.Constraints.AddRange(block.Registration.Constraints);
            }
            index++;
        }
        if (message.BuildingBlocks.Count == 0 && message.XmlName.Length > 0)
        {
            RepositoryReference reference = new RepositoryReference();
            reference.TargetIdentifier = message.XmlName;
            reference.RawValue = message.XmlName;
            reference.TargetKind = RepositoryEntityKind.MessageComponent;
            MultiplicityDefinition multiplicity = new MultiplicityDefinition();
            multiplicity.MinimumOccurs = 1;
            multiplicity.HasMaximumOccurs = true;
            multiplicity.MaximumOccurs = 1;
            string memberName = CSharpModelEmitter.CreateUniqueMemberName(
                usedNames,
                "Value",
                message.Identifier + "|root");
            AddReferenceMemberMetadata(
                context,
                symbol,
                memberName,
                message.RootElement,
                reference,
                multiplicity,
                members);
        }
    }

    internal static void CollectComponentMetadata(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        MessageComponentDefinition component,
        List<CSharpMemberMetadata> members)
    {
        List<string> usedNames = new List<string>();
        usedNames.Add(symbol.SymbolName);
        int index = 0;
        while (index < component.MessageElements.Count)
        {
            MessageElementDefinition? element = CSharpModelEmitter.FindMessageElementConcept(
                context,
                symbol.Concept.SourceOrdinal,
                component.MessageElements[index].TargetIdentifier);
            if (element != null)
            {
                string memberName = CSharpModelEmitter.CreateUniqueMemberName(
                    usedNames,
                    CSharpModelEmitter.FindMemberCandidate(element.XmlTag, element.Name),
                    element.Identifier);
                RepositoryReference reference = CSharpModelEmitter.FindMessageElementReference(
                    element);
                CSharpTypeReference type = CSharpModelEmitter.FindCSharpReferenceType(
                    context,
                    symbol.Concept.SourceOrdinal,
                    reference);
                if (element.Kind == MessageElementKind.AssociationEnd &&
                    !element.IsComposite)
                {
                    type = CSharpModelEmitter.CreateRuntimeTypeReference("IsoTextValue");
                }
                AddKnownTypeMemberMetadata(
                    memberName,
                    FindMetadataXmlName(element.XmlTag, element.Name, element.Identifier),
                    type.Name,
                    element.Multiplicity,
                    members);
                CSharpMemberMetadata member = members[members.Count - 1];
                member.SourceIdentifier = element.Identifier;
                member.Constraints.AddRange(element.Registration.Constraints);
                member.IsValueType = type.IsValueType;
                if (!(element.Kind == MessageElementKind.AssociationEnd &&
                    !element.IsComposite))
                {
                    member.ReferencedSymbol = FindReferencedSymbol(
                        context, symbol, reference);
                }
                if (type.IsValueType && element.Multiplicity.MinimumOccurs == 0)
                {
                    member.PresenceName = CSharpModelEmitter.CreateUniquePresenceName(
                        usedNames,
                        memberName,
                        element.Name + "|presence");
                }
            }
            index++;
        }
    }

    internal static void CollectChoiceMetadata(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        ChoiceDefinition choice,
        List<CSharpMemberMetadata> members)
    {
        List<string> usedNames = new List<string>();
        usedNames.Add(symbol.SymbolName);
        usedNames.Add("Choice");
        int index = 0;
        while (index < choice.MessageElements.Count)
        {
            MessageElementDefinition? element = CSharpModelEmitter.FindMessageElementConcept(
                context,
                symbol.Concept.SourceOrdinal,
                choice.MessageElements[index].TargetIdentifier);
            if (element != null)
            {
                string memberName = CSharpModelEmitter.CreateUniqueMemberName(
                    usedNames,
                    CSharpModelEmitter.FindMemberCandidate(element.XmlTag, element.Name),
                    element.Identifier);
                MultiplicityDefinition optional = new MultiplicityDefinition();
                optional.MinimumOccurs = 0;
                optional.HasMaximumOccurs = element.Multiplicity.HasMaximumOccurs;
                optional.MaximumOccurs = element.Multiplicity.MaximumOccurs;
                CSharpTypeReference type = CSharpModelEmitter.FindCSharpReferenceType(
                    context,
                    symbol.Concept.SourceOrdinal,
                    CSharpModelEmitter.FindMessageElementReference(element));
                if (element.Kind == MessageElementKind.AssociationEnd &&
                    !element.IsComposite)
                {
                    type = CSharpModelEmitter.CreateRuntimeTypeReference("IsoTextValue");
                }
                AddKnownTypeMemberMetadata(
                    memberName,
                    FindMetadataXmlName(element.XmlTag, element.Name, element.Identifier),
                    type.Name,
                    optional,
                    members);
                CSharpMemberMetadata member = members[members.Count - 1];
                member.SourceIdentifier = element.Identifier;
                member.Constraints.AddRange(element.Registration.Constraints);
                member.IsValueType = type.IsValueType;
                if (!(element.Kind == MessageElementKind.AssociationEnd &&
                    !element.IsComposite))
                {
                    member.ReferencedSymbol = FindReferencedSymbol(
                        context,
                        symbol,
                        CSharpModelEmitter.FindMessageElementReference(element));
                }
                if (type.IsValueType)
                {
                    member.PresenceName = CSharpModelEmitter.CreateUniquePresenceName(
                        usedNames,
                        memberName,
                        element.Name + "|presence");
                }
            }
            index++;
        }
    }

    internal static void CollectCodeSetMetadata(
        GenerationSymbolEntry symbol,
        CodeSetDefinition codeSet,
        List<CSharpMemberMetadata> members)
    {
        List<CodeValueDefinition> codes = CSharpModelEmitter.CollectClosedCodeValues(codeSet);
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
            CSharpMemberMetadata member = new CSharpMemberMetadata();
            member.Name = CSharpModelEmitter.CreateUniqueMemberName(
                usedNames,
                candidate,
                code.Identifier + "|" + code.Value);
            member.XmlName = code.Value;
            member.TypeName = string.Empty;
            member.MinimumOccurs = 0;
            member.HasMaximumOccurs = true;
            member.MaximumOccurs = 1;
            member.SourceIdentifier = code.Identifier;
            member.Constraints.AddRange(code.Registration.Constraints);
            members.Add(member);
            index++;
        }
    }

    internal static void AddReferenceMemberMetadata(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string memberName,
        string xmlName,
        RepositoryReference reference,
        MultiplicityDefinition multiplicity,
        List<CSharpMemberMetadata> members)
    {
        CSharpTypeReference type = CSharpModelEmitter.FindCSharpReferenceType(
            context,
            symbol.Concept.SourceOrdinal,
            reference);
        AddKnownTypeMemberMetadata(
            memberName,
            xmlName,
            type.Name,
            multiplicity,
            members);
        CSharpMemberMetadata member = members[members.Count - 1];
        member.IsValueType = type.IsValueType;
        member.ReferencedSymbol = FindReferencedSymbol(context, symbol, reference);
    }

    internal static void AddKnownTypeMemberMetadata(
        string memberName,
        string xmlName,
        string typeName,
        MultiplicityDefinition multiplicity,
        List<CSharpMemberMetadata> members)
    {
        CSharpMemberMetadata member = new CSharpMemberMetadata();
        member.Name = memberName;
        member.XmlName = xmlName;
        member.TypeName = typeName;
        member.MinimumOccurs = multiplicity.MinimumOccurs;
        member.HasMaximumOccurs = multiplicity.HasMaximumOccurs;
        member.MaximumOccurs = multiplicity.MaximumOccurs;
        members.Add(member);
    }

    internal static GenerationSymbolEntry? FindReferencedSymbol(
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        RepositoryReference reference)
    {
        string target = reference.TargetIdentifier;
        if (target.Length == 0)
        {
            target = reference.RawValue;
        }
        string localName = XsdLogicalModelEmitter.FindQualifiedLocalName(target);
        return CSharpModelEmitter.FindReferenceSymbol(
            context,
            owner.Concept.SourceOrdinal,
            target,
            localName,
            reference.TargetKind);
    }

    internal static string FindMetadataXmlName(
        string xmlTag,
        string name,
        string identifier)
    {
        return XsdLogicalModelEmitter.FindElementName(xmlTag, name, identifier);
    }

    internal static string FindWrapperValueTypeName(GenerationSymbolEntry symbol)
    {
        if (symbol.Concept.DataType != null)
        {
            return CSharpModelEmitter.FindDataTypeValueType(
                symbol.Concept.DataType).Name;
        }
        if (symbol.Concept.CodeSet != null)
        {
            return "global::iso20022.IsoCodeValue";
        }
        if (symbol.Concept.ExternalSchema != null)
        {
            return "global::iso20022.RawXmlFragment";
        }
        return "global::iso20022.IsoTextValue";
    }

    internal static void WriteMetadataTable(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string metadataName,
        List<CSharpMemberMetadata> members)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 0, "internal static class " + metadataName);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "{");
        WriteMetadataConstant(builder, context, "SourceIdentifier", symbol.Concept.Identifier);
        WriteMetadataConstant(builder, context, "SourceType", symbol.Concept.Kind.ToString());
        WriteStringMetadataArray(builder, context, "MemberNames", members, 0);
        WriteStringMetadataArray(builder, context, "XmlNames", members, 1);
        WriteStringMetadataArray(builder, context, "TypeNames", members, 2);
        WriteIntegerMetadataArray(builder, context, "MinimumOccurs", members, false);
        WriteBooleanMetadataArray(builder, context, "HasMaximumOccurs", members);
        WriteIntegerMetadataArray(builder, context, "MaximumOccurs", members, true);
        WriteOrderMetadataArray(builder, context, members);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteOrderMetadataArray(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpMemberMetadata> members)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static readonly int[] Order = new int[]");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        int index = 0;
        while (index < members.Count)
        {
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                2,
                index.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + ",");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "};");
    }

    internal static void WriteMetadataConstant(
        StringBuilder builder,
        CSharpGenerationContext context,
        string name,
        string value)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal const string " + name + " = \"" +
            EscapeCSharpString(value) + "\";");
    }

    internal static void WriteStringMetadataArray(
        StringBuilder builder,
        CSharpGenerationContext context,
        string name,
        List<CSharpMemberMetadata> members,
        int selector)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static readonly string[] " + name + " = new string[]");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        int index = 0;
        while (index < members.Count)
        {
            string value = members[index].Name;
            if (selector == 1)
            {
                value = members[index].XmlName;
            }
            else if (selector == 2)
            {
                value = members[index].TypeName;
            }
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                2,
                "\"" + EscapeCSharpString(value) + "\",");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "};");
    }

    internal static void WriteIntegerMetadataArray(
        StringBuilder builder,
        CSharpGenerationContext context,
        string name,
        List<CSharpMemberMetadata> members,
        bool maximum)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static readonly int[] " + name + " = new int[]");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        int index = 0;
        while (index < members.Count)
        {
            int value = members[index].MinimumOccurs;
            if (maximum)
            {
                value = members[index].MaximumOccurs;
            }
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                2,
                value.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "};");
    }

    internal static void WriteBooleanMetadataArray(
        StringBuilder builder,
        CSharpGenerationContext context,
        string name,
        List<CSharpMemberMetadata> members)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static readonly bool[] " + name + " = new bool[]");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        int index = 0;
        while (index < members.Count)
        {
            string value = "false";
            if (members[index].HasMaximumOccurs)
            {
                value = "true";
            }
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, value + ",");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "};");
    }

    internal static void WriteTypeCodec(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string codecName,
        List<CSharpMemberMetadata> members)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 0, "internal static class " + codecName);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "{");
        if (symbol.Concept.CodeSet != null &&
            CSharpModelEmitter.CodeSetIsClosed(symbol.Concept.CodeSet))
        {
            WriteClosedCodeCodec(builder, context, symbol, members);
        }
        else
        {
            WriteModelFactory(builder, context, symbol);
            WriteTypedReader(builder, context, symbol, members);
            CSharpWriterEmitter.WriteTypedWriter(
                builder, context, symbol, members);
            if (members.Count > 0 &&
                (symbol.Concept.MessageDefinition != null ||
                symbol.Concept.MessageComponent != null ||
                symbol.Concept.Choice != null))
            {
                WriteMemberLookupCodec(builder, context, members);
            }
        }
        CSharpStructuralValidationEmitter.WriteTypeValidator(
            builder, context, symbol, members);
        CSharpPrimitiveValidationEmitter.WriteTypeValidators(
            builder, context, symbol, members);
        CSharpExternalCodeValidationEmitter.WriteTypeValidator(
            builder, context, symbol, members);
        CSharpRepositoryConstraintValidationEmitter.WriteTypeValidator(
            builder, context, symbol, members);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteTypedReader(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        if (symbol.Concept.MessageDefinition != null ||
            symbol.Concept.MessageComponent != null)
        {
            WriteSequenceReader(builder, context, symbol, members);
        }
        else if (symbol.Concept.Choice != null)
        {
            WriteChoiceReader(builder, context, symbol, members);
        }
        else if (symbol.Concept.DataType != null ||
            symbol.Concept.CodeSet != null)
        {
            WriteSimpleContentReader(builder, context, symbol);
        }
        else if (symbol.Concept.ExternalSchema != null)
        {
            WriteExternalContentReader(builder, context, symbol);
        }
    }

    internal static void WriteSequenceReader(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = FindQualifiedSymbolName(symbol);
        string runtimeName = FindReaderRuntimeQualifiedName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static " + typeName +
            " ReadModel(global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".MoveToElement(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string elementNamespace = reader.NamespaceURI;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "bool emptyElement = reader.IsEmptyElement;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "reader.ReadStartElement();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            typeName + " model = new " + typeName + "();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!emptyElement)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "reader.MoveToContent();");
        int index = 0;
        while (index < members.Count)
        {
            WriteSequenceMemberReader(
                builder, context, symbol, members[index], index, runtimeName);
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (reader.NodeType != global::System.Xml.XmlNodeType.EndElement)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "throw new global::System.Xml.XmlException(\"Unexpected element '" +
            "\" + reader.LocalName + \"' or out-of-order content.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "reader.ReadEndElement();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        WriteEmptyElementRequiredGuards(builder, context, members);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return model;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteEmptyElementRequiredGuards(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpMemberMetadata> members)
    {
        int index = 0;
        while (index < members.Count)
        {
            if (members[index].MinimumOccurs > 0)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                    "if (emptyElement)");
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
                CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                    "throw new global::System.Xml.XmlException(\"Required element '" +
                    EscapeCSharpString(members[index].XmlName) + "' is missing.\");");
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
                return;
            }
            index++;
        }
    }

    internal static void WriteSequenceMemberReader(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member,
        int memberIndex,
        string runtimeName)
    {
        bool repeated = !member.HasMaximumOccurs || member.MaximumOccurs > 1;
        string suffix = memberIndex.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        if (repeated)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "int count" + suffix + " = 0;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "while (" + runtimeName + ".IsElement(reader, \"" +
                EscapeCSharpString(member.XmlName) + "\", elementNamespace))");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
            WriteMemberValueReader(
                builder, context, owner, member, "model." + member.Name,
                true, 4, runtimeName);
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "count" + suffix + "++;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "reader.MoveToContent();");
            if (member.HasMaximumOccurs)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                    "if (count" + suffix + " == " + member.MaximumOccurs.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + ")");
                CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
                CSharpModelEmitter.AppendSourceLine(builder, context, 5, "break;");
                CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
            }
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
            if (member.MinimumOccurs > 0)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                    "if (count" + suffix + " < " + member.MinimumOccurs.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + ")");
                CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
                WriteMissingElementThrow(builder, context, member, 4);
                CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
            }
            return;
        }

        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (" + runtimeName + ".IsElement(reader, \"" +
            EscapeCSharpString(member.XmlName) + "\", elementNamespace))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        WriteMemberValueReader(
            builder, context, owner, member, "model." + member.Name,
            false, 4, runtimeName);
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "reader.MoveToContent();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        if (member.MinimumOccurs > 0)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "else");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
            WriteMissingElementThrow(builder, context, member, 4);
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        }
    }

    internal static void WriteMissingElementThrow(
        StringBuilder builder,
        CSharpGenerationContext context,
        CSharpMemberMetadata member,
        int level)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            "throw new global::System.Xml.XmlException(\"Required element '" +
            EscapeCSharpString(member.XmlName) + "' is missing or out of order.\");");
    }

    internal static void WriteMemberValueReader(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member,
        string target,
        bool repeated,
        int level,
        string runtimeName)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            "if (" + runtimeName + ".IsNilElement(reader))");
        CSharpModelEmitter.AppendSourceLine(builder, context, level, "{");
        if (member.IsValueType)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
                "reader.Skip();");
            if (repeated || member.MinimumOccurs > 0)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
                    "throw new global::System.Xml.XmlException(" +
                    "\"A value-type element cannot be nil.\");");
            }
        }
        else
        {
            if (repeated)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
                    target + ".Add(null);");
            }
            else
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
                    target + " = null;");
            }
            CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
                "reader.Skip();");
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, level, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, level, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, level, "{");
        string expression = FindMemberReadExpression(context, owner, member, runtimeName);
        if (repeated)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
                target + ".Add(" + expression + ");");
        }
        else
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
                target + " = " + expression + ";");
            if (member.PresenceName.Length > 0)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
                    "model." + member.PresenceName + " = true;");
            }
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, level, "}");
    }

    internal static string FindMemberReadExpression(
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member,
        string runtimeName)
    {
        if (member.ReferencedSymbol != null)
        {
            string codecName = FindInternalGeneratedName(
                context, member.ReferencedSymbol, "Codec");
            string qualifiedCodec = "global::" + member.ReferencedSymbol.NamespaceName +
                "." + codecName;
            if (member.ReferencedSymbol.Concept.CodeSet != null &&
                CSharpModelEmitter.CodeSetIsClosed(
                    member.ReferencedSymbol.Concept.CodeSet))
            {
                return qualifiedCodec + ".ReadCode(" + runtimeName +
                    ".ReadElementText(reader))";
            }
            return qualifiedCodec + ".ReadModel(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoBooleanValue")
        {
            return runtimeName + ".ReadBooleanValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoDecimalValue")
        {
            return runtimeName + ".ReadDecimalValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoBinaryValue")
        {
            return runtimeName + ".ReadBase64Value(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoDateValue")
        {
            return runtimeName + ".ReadDateValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoTimeValue")
        {
            return runtimeName + ".ReadTimeValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoDateTimeValue")
        {
            return runtimeName + ".ReadDateTimeValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoDurationValue")
        {
            return runtimeName + ".ReadDurationValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoPartialDateValue")
        {
            return runtimeName + ".ReadPartialDateValue(reader)";
        }
        return runtimeName + ".ReadTextValue(reader)";
    }

    internal static void WriteChoiceReader(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = FindQualifiedSymbolName(symbol);
        string kindName = "global::" + symbol.NamespaceName + "." +
            CSharpModelEmitter.FindChoiceKindName(context, symbol);
        string runtimeName = FindReaderRuntimeQualifiedName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static " + typeName +
            " ReadModel(global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".MoveToElement(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string elementNamespace = reader.NamespaceURI;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "bool emptyElement = reader.IsEmptyElement;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "reader.ReadStartElement();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            typeName + " model = new " + typeName + "();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (emptyElement)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.Xml.XmlException(\"A choice requires one alternative.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "reader.MoveToContent();");
        int index = 0;
        while (index < members.Count)
        {
            string branch = "if";
            if (index > 0)
            {
                branch = "else if";
            }
            CSharpMemberMetadata member = members[index];
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                branch + " (" + runtimeName + ".IsElement(reader, \"" +
                EscapeCSharpString(member.XmlName) + "\", elementNamespace))");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteMemberValueReader(
                builder, context, symbol, member, "model." + member.Name,
                false, 3, runtimeName);
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "model.Choice = " + kindName + "." + member.Name + ";");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            index++;
        }
        if (members.Count > 0)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "else");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "throw new global::System.Xml.XmlException(\"A recognized choice alternative is required.\");");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        }
        else
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "throw new global::System.Xml.XmlException(\"A recognized choice alternative is required.\");");
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "reader.MoveToContent();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (reader.NodeType != global::System.Xml.XmlNodeType.EndElement)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.Xml.XmlException(\"A choice can contain only one alternative.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "reader.ReadEndElement();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return model;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteSimpleContentReader(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = FindQualifiedSymbolName(symbol);
        string runtimeName = FindReaderRuntimeQualifiedName(context);
        string expression = runtimeName + ".ReadTextValue(reader)";
        if (symbol.Concept.CodeSet != null)
        {
            expression = runtimeName + ".ReadCodeValue(reader, \"" +
                EscapeCSharpString(symbol.Concept.CodeSet.Name) + "\")";
        }
        else if (symbol.Concept.DataType != null)
        {
            expression = FindDataTypeReadExpression(
                symbol.Concept.DataType, runtimeName);
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static " + typeName +
            " ReadModel(global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            typeName + " model = new " + typeName + "();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "model.Value = " + expression + ";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return model;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static string FindDataTypeReadExpression(
        DataTypeDefinition dataType,
        string runtimeName)
    {
        if (dataType.Kind == DataTypeKind.Boolean ||
            dataType.Kind == DataTypeKind.Indicator)
        {
            return runtimeName + ".ReadBooleanValue(reader)";
        }
        if (dataType.Kind == DataTypeKind.Decimal ||
            dataType.Kind == DataTypeKind.Quantity ||
            dataType.Kind == DataTypeKind.Rate)
        {
            return runtimeName + ".ReadDecimalValue(reader)";
        }
        if (dataType.Kind == DataTypeKind.Amount)
        {
            return runtimeName + ".ReadAmountValue(reader)";
        }
        if (dataType.Kind == DataTypeKind.Binary)
        {
            if (dataType.BinaryEncoding == BinaryEncodingKind.Hexadecimal ||
                dataType.SchemaTypeName == "hexBinary")
            {
                return runtimeName + ".ReadHexadecimalValue(reader)";
            }
            return runtimeName + ".ReadBase64Value(reader)";
        }
        if (dataType.Kind == DataTypeKind.Date)
        {
            return runtimeName + ".ReadDateValue(reader)";
        }
        if (dataType.Kind == DataTypeKind.Time)
        {
            return runtimeName + ".ReadTimeValue(reader)";
        }
        if (dataType.Kind == DataTypeKind.DateTime)
        {
            return runtimeName + ".ReadDateTimeValue(reader)";
        }
        if (dataType.Kind == DataTypeKind.Duration)
        {
            return runtimeName + ".ReadDurationValue(reader)";
        }
        if (dataType.Kind == DataTypeKind.Day ||
            dataType.Kind == DataTypeKind.Month ||
            dataType.Kind == DataTypeKind.MonthDay ||
            dataType.Kind == DataTypeKind.Year ||
            dataType.Kind == DataTypeKind.YearMonth)
        {
            return runtimeName + ".ReadPartialDateValue(reader)";
        }
        if (dataType.Kind == DataTypeKind.Schema)
        {
            CSharpTypeReference type = CSharpModelEmitter.FindBuiltInRuntimeType(
                dataType.SchemaTypeName);
            CSharpMemberMetadata member = new CSharpMemberMetadata();
            member.TypeName = type.Name;
            return FindMemberReadExpressionForRuntimeType(member, runtimeName);
        }
        return runtimeName + ".ReadTextValue(reader)";
    }

    internal static string FindMemberReadExpressionForRuntimeType(
        CSharpMemberMetadata member,
        string runtimeName)
    {
        if (member.TypeName == "global::iso20022.IsoBooleanValue")
        {
            return runtimeName + ".ReadBooleanValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoDecimalValue")
        {
            return runtimeName + ".ReadDecimalValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoBinaryValue")
        {
            return runtimeName + ".ReadBase64Value(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoDateValue")
        {
            return runtimeName + ".ReadDateValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoTimeValue")
        {
            return runtimeName + ".ReadTimeValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoDateTimeValue")
        {
            return runtimeName + ".ReadDateTimeValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoDurationValue")
        {
            return runtimeName + ".ReadDurationValue(reader)";
        }
        if (member.TypeName == "global::iso20022.IsoPartialDateValue")
        {
            return runtimeName + ".ReadPartialDateValue(reader)";
        }
        return runtimeName + ".ReadTextValue(reader)";
    }

    internal static void WriteExternalContentReader(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = FindQualifiedSymbolName(symbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static " + typeName +
            " ReadModel(global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            typeName + " model = new " + typeName + "();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "model.Value = " + FindReaderRuntimeQualifiedName(context) +
            ".ReadRawFragment(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return model;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static string FindReaderRuntimeQualifiedName(
        CSharpGenerationContext context)
    {
        return "global::" + context.Symbols.RootNamespace +
            ".Internal.Iso20022GeneratedReaderRuntime";
    }

    internal static void WriteModelFactory(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = FindQualifiedSymbolName(symbol);
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static " + typeName + " CreateModel()");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 2, "return new " + typeName + "();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteMemberLookupCodec(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpMemberMetadata> members)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 1, "internal static int FindMemberIndex(string xmlName)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "switch (xmlName)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < members.Count)
        {
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                3,
                "case \"" + EscapeCSharpString(members[index].XmlName) + "\":");
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                4,
                "return " + index.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + ";");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "return -1;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 1, "internal static string FindMemberXmlName(int index)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "switch (index)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        index = 0;
        while (index < members.Count)
        {
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                3,
                "case " + index.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + ":");
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                4,
                "return \"" + EscapeCSharpString(members[index].XmlName) + "\";");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "return string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteClosedCodeCodec(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = FindQualifiedSymbolName(symbol);
        CSharpWriterEmitter.WriteClosedCodeWriter(
            builder, context, symbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static " + typeName +
            " ReadModel(global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return ReadCode(" + FindReaderRuntimeQualifiedName(context) +
            ".ReadElementText(reader));");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static " + typeName + " ReadCode(string value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "switch (value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < members.Count)
        {
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                3,
                "case \"" + EscapeCSharpString(members[index].XmlName) + "\":");
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                4,
                "return " + typeName + "." + members[index].Name + ";");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 4, "return " + typeName + ".Unknown;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static string WriteCode(" + typeName + " value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "switch (value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        index = 0;
        while (index < members.Count)
        {
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                3,
                "case " + typeName + "." + members[index].Name + ":");
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                4,
                "return \"" + EscapeCSharpString(members[index].XmlName) + "\";");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "return string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static CSharpGeneratedSource? EmitRegistrySource(
        CSharpGenerationContext context)
    {
        List<CSharpRegistryEntry> entries = CollectRegistryEntries(context);
        if (entries.Count == 0)
        {
            return null;
        }
        string namespaceName = context.Symbols.RootNamespace + ".Internal";
        StringBuilder builder = new StringBuilder();
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 0, "namespace " + namespaceName + ";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, string.Empty);
        CSharpDispatchEmitter.WriteResultClass(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, string.Empty);
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 0, "internal static class Iso20022GeneratedRegistry");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "{");
        WriteRegistryCreateMethod(builder, context, entries, false);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        WriteRegistryCreateMethod(builder, context, entries, true);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        WriteRegistryNameMethod(builder, context, entries);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        WriteRegistryNamespaceMethod(builder, context, entries);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpDispatchEmitter.WriteRegistryDispatchMethods(
            builder, context, entries);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpStructuralValidationEmitter.WriteRegistryValidator(
            builder, context, entries);
        CSharpPrimitiveValidationEmitter.WriteRegistryValidators(
            builder, context, entries);
        CSharpExternalCodeValidationEmitter.WriteRegistryValidator(
            builder, context, entries);
        CSharpRepositoryConstraintValidationEmitter.WriteRegistryValidator(
            builder, context, entries);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "}");

        CSharpGeneratedSource source = new CSharpGeneratedSource();
        source.Kind = GenerationFileKind.Metadata;
        source.TypeName = "Iso20022GeneratedRegistry";
        source.NamespaceName = namespaceName;
        source.RelativePath = FindInternalOutputDirectory(context, namespaceName) +
            "/Iso20022GeneratedRegistry.cs";
        source.Content = Encoding.UTF8.GetBytes(builder.ToString());
        return source;
    }

    internal static CSharpGeneratedSource? EmitReaderRuntimeSource(
        CSharpGenerationContext context)
    {
        if (context.Symbols.Symbols.Count == 0)
        {
            return null;
        }
        string namespaceName = context.Symbols.RootNamespace + ".Internal";
        StringBuilder builder = new StringBuilder();
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 0, "namespace " + namespaceName + ";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0,
            "internal static class Iso20022GeneratedReaderRuntime");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "{");
        WriteReaderNavigationRuntime(builder, context);
        WriteReaderTextRuntime(builder, context);
        WriteReaderNumericRuntime(builder, context);
        WriteReaderCalendarRuntime(builder, context);
        CSharpWriterEmitter.WriteRuntime(builder, context);
        CSharpWildcardEmitter.WriteRuntime(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "}");

        CSharpGeneratedSource source = new CSharpGeneratedSource();
        source.Kind = GenerationFileKind.Metadata;
        source.TypeName = "Iso20022GeneratedReaderRuntime";
        source.NamespaceName = namespaceName;
        source.RelativePath = FindInternalOutputDirectory(context, namespaceName) +
            "/Iso20022GeneratedReaderRuntime.cs";
        source.Content = Encoding.UTF8.GetBytes(builder.ToString());
        return source;
    }

    internal static void WriteReaderNavigationRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeMethodStart(builder, context,
            "internal static void MoveToElement(global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "reader.MoveToContent();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (reader.NodeType != global::System.Xml.XmlNodeType.Element)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.Xml.XmlException(\"An XML element is required.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static bool IsElement(global::System.Xml.XmlReader reader, " +
            "string localName, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return reader.NodeType == global::System.Xml.XmlNodeType.Element && " +
            "reader.LocalName == localName && reader.NamespaceURI == namespaceUri;");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static bool IsNilElement(global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string value = reader.GetAttribute(\"nil\", " +
            "\"http://www.w3.org/2001/XMLSchema-instance\") ?? string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return value == \"true\" || value == \"1\";");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static string ReadElementText(global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "MoveToElement(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return reader.ReadElementContentAsString();");
        WriteRuntimeMethodEnd(builder, context);
    }

    internal static void WriteReaderTextRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoTextValue ReadTextValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = ReadElementText(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoTextValue value = new global::iso20022.IsoTextValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Value = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoCodeValue ReadCodeValue(" +
            "global::System.Xml.XmlReader reader, string codeSetName)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = ReadElementText(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoCodeValue value = new global::iso20022.IsoCodeValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Value = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.CodeSetName = codeSetName;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoBinaryValue ReadBase64Value(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = ReadElementText(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoBinaryValue value = new global::iso20022.IsoBinaryValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Value = global::System.Convert.FromBase64String(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Encoding = global::iso20022.BinaryEncodingKind.Base64;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoBinaryValue ReadHexadecimalValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = ReadElementText(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoBinaryValue value = new global::iso20022.IsoBinaryValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Value = global::System.Convert.FromHexString(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Encoding = global::iso20022.BinaryEncodingKind.Hexadecimal;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);
    }

    internal static void WriteReaderNumericRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoBooleanValue ReadBooleanValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = ReadElementText(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoBooleanValue value = new global::iso20022.IsoBooleanValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Value = global::System.Xml.XmlConvert.ToBoolean(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoDecimalValue ReadDecimalValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return CreateDecimalValue(ReadElementText(reader));");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoDecimalValue CreateDecimalValue(" +
            "string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoDecimalValue value = new global::iso20022.IsoDecimalValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Value = global::System.Xml.XmlConvert.ToDecimal(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int point = lexical.IndexOf('.');");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (point >= 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "value.Scale = lexical.Length - point - 1;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.IsNegativeZero = value.Value == 0m && lexical.StartsWith(\"-\", " +
            "global::System.StringComparison.Ordinal);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoAmountValue ReadAmountValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "MoveToElement(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string currency = reader.GetAttribute(\"Ccy\") ?? string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = reader.ReadElementContentAsString();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoAmountValue value = new global::iso20022.IsoAmountValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Amount = CreateDecimalValue(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Currency = currency;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);
    }

    internal static void WriteReaderCalendarRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteDateReaderRuntime(builder, context);
        WriteTimeReaderRuntime(builder, context);
        WriteDateTimeReaderRuntime(builder, context);
        WriteDurationReaderRuntime(builder, context);
        WritePartialDateReaderRuntime(builder, context);
        WriteTimezoneReaderRuntime(builder, context);
    }

    internal static void WriteDateReaderRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoDateValue ReadDateValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return CreateDateValue(ReadElementText(reader));");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoDateValue CreateDateValue(string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.DateTimeOffset parsed = " +
            "global::System.Xml.XmlConvert.ToDateTimeOffset(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoDateValue value = new global::iso20022.IsoDateValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Year = parsed.Year;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Month = parsed.Month;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Day = parsed.Day;");
        WriteCalendarTimezoneAssignments(builder, context, "value");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalMode = global::iso20022.DateTimeLexicalMode.Preserve;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);
    }

    internal static void WriteTimeReaderRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoTimeValue ReadTimeValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return CreateTimeValue(ReadElementText(reader));");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoTimeValue CreateTimeValue(string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.DateTimeOffset parsed = global::System.DateTimeOffset.Parse(" +
            "lexical, global::System.Globalization.CultureInfo.InvariantCulture, " +
            "global::System.Globalization.DateTimeStyles.AllowWhiteSpaces);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoTimeValue value = new global::iso20022.IsoTimeValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Hour = parsed.Hour;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Minute = parsed.Minute;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Second = parsed.Second;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.FractionalSecondDigits = FindFractionalSecondDigits(lexical);");
        WriteCalendarTimezoneAssignments(builder, context, "value");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalMode = global::iso20022.DateTimeLexicalMode.Preserve;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);
    }

    internal static void WriteDateTimeReaderRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoDateTimeValue ReadDateTimeValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = ReadElementText(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int separator = lexical.IndexOf('T');");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (separator < 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.FormatException(\"An ISO date-time requires T.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoDateTimeValue value = " +
            "new global::iso20022.IsoDateTimeValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string timeText = lexical.Substring(separator + 1);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string dateText = lexical.Substring(0, separator) + " +
            "FindTimezoneSuffix(timeText);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Date = CreateDateValue(dateText);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.Time = CreateTimeValue(timeText);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalMode = global::iso20022.DateTimeLexicalMode.Preserve;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);
    }

    internal static void WriteDurationReaderRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoDurationValue ReadDurationValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = ReadElementText(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.TimeSpan parsed = global::System.Xml.XmlConvert.ToTimeSpan(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoDurationValue value = " +
            "new global::iso20022.IsoDurationValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.IsNegative = parsed.Ticks < 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (value.IsNegative)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "parsed = parsed.Negate();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Days = parsed.Days;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Hours = parsed.Hours;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Minutes = parsed.Minutes;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.Seconds = parsed.Seconds;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.HasDays = value.Days != 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.HasHours = value.Hours != 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "value.HasMinutes = value.Minutes != 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.HasSeconds = value.Seconds != 0 || parsed.Ticks % " +
            "global::System.TimeSpan.TicksPerSecond != 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.FractionalSecondDigits = FindFractionalSecondDigits(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);
    }

    internal static void WritePartialDateReaderRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeMethodStart(builder, context,
            "internal static global::iso20022.IsoPartialDateValue ReadPartialDateValue(" +
            "global::System.Xml.XmlReader reader)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = ReadElementText(reader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string content = RemoveTimezoneSuffix(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.IsoPartialDateValue value = " +
            "new global::iso20022.IsoPartialDateValue();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (content.StartsWith(\"---\", global::System.StringComparison.Ordinal))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "value.Kind = global::iso20022.PartialDateKind.Day;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "value.Day = global::System.Int32.Parse(content.Substring(3), " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "else if (content.StartsWith(\"--\", global::System.StringComparison.Ordinal))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "value.Month = global::System.Int32.Parse(content.Substring(2, 2), " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (content.Length > 4)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "value.Kind = global::iso20022.PartialDateKind.MonthDay;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "value.Day = global::System.Int32.Parse(content.Substring(5, 2), " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "value.Kind = global::iso20022.PartialDateKind.Month;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "int separator = content.LastIndexOf('-');");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (separator > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "value.Kind = global::iso20022.PartialDateKind.YearMonth;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "value.Year = global::System.Int32.Parse(content.Substring(0, separator), " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "value.Month = global::System.Int32.Parse(content.Substring(separator + 1), " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "value.Kind = global::iso20022.PartialDateKind.Year;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "value.Year = global::System.Int32.Parse(content, " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        WriteCalendarTimezoneAssignments(builder, context, "value");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "value.LexicalValue = lexical;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return value;");
        WriteRuntimeMethodEnd(builder, context);
    }

    internal static void WriteCalendarTimezoneAssignments(
        StringBuilder builder,
        CSharpGenerationContext context,
        string target)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            target + ".HasTimezone = HasTimezoneSuffix(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            target + ".TimezoneOffsetMinutes = FindTimezoneOffsetMinutes(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            target + ".IsUtc = lexical.EndsWith(\"Z\", " +
            "global::System.StringComparison.Ordinal);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            target + ".IsNegativeTimezoneZero = lexical.EndsWith(\"-00:00\", " +
            "global::System.StringComparison.Ordinal);");
    }

    internal static void WriteTimezoneReaderRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeMethodStart(builder, context,
            "internal static bool HasTimezoneSuffix(string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return lexical.EndsWith(\"Z\", global::System.StringComparison.Ordinal) || " +
            "(lexical.Length >= 6 && (lexical[lexical.Length - 6] == '+' || " +
            "lexical[lexical.Length - 6] == '-')); ");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static int FindTimezoneOffsetMinutes(string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!HasTimezoneSuffix(lexical) || lexical.EndsWith(\"Z\", " +
            "global::System.StringComparison.Ordinal))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int start = lexical.Length - 6;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int hours = global::System.Int32.Parse(lexical.Substring(start + 1, 2), " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int minutes = global::System.Int32.Parse(lexical.Substring(start + 4, 2), " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int result = hours * 60 + minutes;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (lexical[start] == '-')");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "result = -result;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return result;");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static string FindTimezoneSuffix(string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (lexical.EndsWith(\"Z\", global::System.StringComparison.Ordinal))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return \"Z\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (HasTimezoneSuffix(lexical))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "return lexical.Substring(lexical.Length - 6);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return string.Empty;");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static string RemoveTimezoneSuffix(string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string suffix = FindTimezoneSuffix(lexical);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return lexical.Substring(0, lexical.Length - suffix.Length);");
        WriteRuntimeMethodEnd(builder, context);

        WriteRuntimeMethodStart(builder, context,
            "internal static string FindFractionalSecondDigits(string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int point = lexical.IndexOf('.');");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (point < 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int end = point + 1;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (end < lexical.Length && lexical[end] >= '0' && lexical[end] <= '9')");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "end++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return lexical.Substring(point + 1, end - point - 1);");
        WriteRuntimeMethodEnd(builder, context);
    }

    internal static void WriteRuntimeMethodStart(
        StringBuilder builder,
        CSharpGenerationContext context,
        string declaration)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, declaration);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
    }

    internal static void WriteRuntimeMethodEnd(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static List<CSharpRegistryEntry> CollectRegistryEntries(
        CSharpGenerationContext context)
    {
        List<CSharpRegistryEntry> entries = new List<CSharpRegistryEntry>();
        int index = 0;
        while (index < context.Symbols.Symbols.Count)
        {
            GenerationSymbolEntry symbol = context.Symbols.Symbols[index];
            MessageDefinitionMetadata? message = symbol.Concept.MessageDefinition;
            if (message != null)
            {
                XsdGenerator helper = new XsdGenerator();
                string identifier = helper.FindIdentifierValue(message.MessageIdentifier);
                if (identifier != "..." && !RegistryIdentifierExists(entries, identifier))
                {
                    CSharpRegistryEntry entry = new CSharpRegistryEntry();
                    entry.MessageIdentifier = identifier;
                    entry.NamespaceUri = message.NamespaceUri;
                    entry.ModelTypeName = FindQualifiedSymbolName(symbol);
                    entry.CodecTypeName = "global::" + symbol.NamespaceName + "." +
                        FindInternalGeneratedName(context, symbol, "Codec");
                    entry.RootElement = message.RootElement;
                    if (entry.RootElement.Length == 0)
                    {
                        entry.RootElement = "Document";
                    }
                    entry.SourceIdentifier = message.Identifier;
                    entries.Add(entry);
                }
            }
            index++;
        }
        SortRegistryEntries(entries);
        return entries;
    }

    internal static bool RegistryIdentifierExists(
        List<CSharpRegistryEntry> entries,
        string identifier)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].MessageIdentifier == identifier)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void SortRegistryEntries(List<CSharpRegistryEntry> entries)
    {
        int index = 1;
        while (index < entries.Count)
        {
            CSharpRegistryEntry value = entries[index];
            int destination = index;
            while (destination > 0 && string.CompareOrdinal(
                value.MessageIdentifier,
                entries[destination - 1].MessageIdentifier) < 0)
            {
                entries[destination] = entries[destination - 1];
                destination--;
            }
            entries[destination] = value;
            index++;
        }
    }

    internal static void WriteRegistryCreateMethod(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries,
        bool byNamespace)
    {
        string methodName = "CreateMessage";
        string parameterName = "messageIdentifier";
        if (byNamespace)
        {
            methodName = "CreateMessageByNamespace";
            parameterName = "namespaceUri";
        }
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static object? " + methodName + "(string " + parameterName + ")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 2, "switch (" + parameterName + ")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        List<string> emitted = new List<string>();
        int index = 0;
        while (index < entries.Count)
        {
            string key = entries[index].MessageIdentifier;
            if (byNamespace)
            {
                key = entries[index].NamespaceUri;
            }
            if (key.Length > 0 && !CSharpModelEmitter.TextListContains(emitted, key))
            {
                emitted.Add(key);
                CSharpModelEmitter.AppendSourceLine(
                    builder,
                    context,
                    3,
                    "case \"" + EscapeCSharpString(key) + "\":");
                CSharpModelEmitter.AppendSourceLine(
                    builder,
                    context,
                    4,
                    "return new " + entries[index].ModelTypeName + "();");
            }
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "return null;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRegistryNameMethod(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static string FindModelName(string messageIdentifier)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 2, "switch (messageIdentifier)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < entries.Count)
        {
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                3,
                "case \"" + EscapeCSharpString(entries[index].MessageIdentifier) + "\":");
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                4,
                "return \"" + EscapeCSharpString(entries[index].ModelTypeName) + "\";");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "return string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRegistryNamespaceMethod(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder,
            context,
            1,
            "internal static string FindMessageNamespace(string messageIdentifier)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 2, "switch (messageIdentifier)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < entries.Count)
        {
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                3,
                "case \"" + EscapeCSharpString(entries[index].MessageIdentifier) + "\":");
            CSharpModelEmitter.AppendSourceLine(
                builder,
                context,
                4,
                "return \"" + EscapeCSharpString(entries[index].NamespaceUri) + "\";");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "return string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static string FindQualifiedSymbolName(GenerationSymbolEntry symbol)
    {
        return "global::" + symbol.NamespaceName + "." + symbol.SymbolName;
    }

    internal static string FindInternalOutputDirectory(
        CSharpGenerationContext context,
        string namespaceName)
    {
        string edition = CSharpModelEmitter.CreateCSharpPathSegment(
            context.Plan.Options.ArtifactEdition,
            "Unspecified");
        return "Api/Internal/Generated/" + edition + "/" +
            namespaceName.Replace('.', '/');
    }

    internal static string EscapeCSharpString(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\\')
            {
                result.Append("\\\\");
            }
            else if (current == '"')
            {
                result.Append("\\\"");
            }
            else if (current == '\r')
            {
                result.Append("\\r");
            }
            else if (current == '\n')
            {
                result.Append("\\n");
            }
            else if (current == '\t')
            {
                result.Append("\\t");
            }
            else if (current < ' ')
            {
                result.Append(' ');
            }
            else
            {
                result.Append(current);
            }
            index++;
        }
        return result.ToString();
    }
}
