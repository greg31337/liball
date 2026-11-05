using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class CSharpWriterEmitter
{
    internal static void WriteTypedWriter(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        if (symbol.Concept.MessageDefinition != null ||
            symbol.Concept.MessageComponent != null)
        {
            WriteSequenceWriter(builder, context, symbol, members);
        }
        else if (symbol.Concept.Choice != null)
        {
            WriteChoiceWriter(builder, context, symbol, members);
        }
        else if (symbol.Concept.DataType != null ||
            symbol.Concept.CodeSet != null)
        {
            WriteSimpleWriter(builder, context, symbol);
        }
        else if (symbol.Concept.ExternalSchema != null)
        {
            WriteExternalWriter(builder, context, symbol);
        }
    }

    internal static void WriteSequenceWriter(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        WriteSequenceValidator(builder, context, symbol, members);
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string runtimeName = CSharpMetadataEmitter.FindReaderRuntimeQualifiedName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void WriteModel(global::System.Xml.XmlWriter writer, " +
            typeName + " model, string elementName, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "ValidateModel(model);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".WriteStartElement(writer, elementName, namespaceUri);");
        int index = 0;
        while (index < members.Count)
        {
            WriteSequenceMember(builder, context, symbol, members[index], index);
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteEndElement();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
        if (symbol.Concept.MessageDefinition != null)
        {
            WriteDocumentWriter(builder, context, symbol);
        }
    }

    internal static void WriteSequenceValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateModel(" + typeName + " model)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        WriteNullGuard(builder, context, "model", "The generated model is required.", 2);
        int index = 0;
        while (index < members.Count)
        {
            WriteSequenceMemberValidation(
                builder, context, symbol, members[index], index);
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteSequenceMemberValidation(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member,
        int memberIndex)
    {
        bool repeated = !member.HasMaximumOccurs || member.MaximumOccurs > 1;
        string target = "model." + member.Name;
        if (repeated)
        {
            WriteNullGuard(builder, context, target,
                "Collection '" + member.Name + "' is required.", 2);
            if (member.MinimumOccurs > 0)
            {
                WriteCountGuard(builder, context, target, member, true);
            }
            if (member.HasMaximumOccurs)
            {
                WriteCountGuard(builder, context, target, member, false);
            }
            string suffix = memberIndex.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "int item" + suffix + " = 0;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "while (item" + suffix + " < " + target + ".Count)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            string itemTarget = target + "[item" + suffix + "]";
            if (!member.IsValueType)
            {
                WriteNullGuard(builder, context, itemTarget,
                    "Repeating element '" + member.XmlName + "' cannot contain null.", 3);
            }
            WriteReferencedValidation(
                builder, context, owner, member, itemTarget, 3);
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "item" + suffix + "++;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            return;
        }
        if (member.IsValueType)
        {
            if (member.PresenceName.Length > 0)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                    "if (model." + member.PresenceName + ")");
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
                WriteReferencedValidation(builder, context, owner, member, target, 3);
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            }
            else
            {
                WriteReferencedValidation(builder, context, owner, member, target, 2);
            }
            return;
        }
        if (member.MinimumOccurs > 0)
        {
            WriteNullGuard(builder, context, target,
                "Required element '" + member.XmlName + "' is missing.", 2);
            WriteReferencedValidation(builder, context, owner, member, target, 2);
        }
        else
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (" + target + " != null)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteReferencedValidation(builder, context, owner, member, target, 3);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        }
    }

    internal static void WriteCountGuard(
        StringBuilder builder,
        CSharpGenerationContext context,
        string target,
        CSharpMemberMetadata member,
        bool minimum)
    {
        string comparison = " < ";
        int count = member.MinimumOccurs;
        string role = "minimum";
        if (!minimum)
        {
            comparison = " > ";
            count = member.MaximumOccurs;
            role = "maximum";
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (" + target + ".Count" + comparison + count.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.InvalidOperationException(\"Element '" +
            CSharpMetadataEmitter.EscapeCSharpString(member.XmlName) +
            "' violates its " + role + " multiplicity.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
    }

    internal static void WriteReferencedValidation(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member,
        string target,
        int level)
    {
        if (member.ReferencedSymbol == null)
        {
            return;
        }
        string codecName = FindQualifiedCodecName(context, member.ReferencedSymbol);
        if (member.ReferencedSymbol.Concept.CodeSet != null &&
            CSharpModelEmitter.CodeSetIsClosed(member.ReferencedSymbol.Concept.CodeSet))
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, level,
                codecName + ".ValidateCode(" + target + ");");
        }
        else
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, level,
                codecName + ".ValidateModel(" + target + ");");
        }
    }

    internal static void WriteSequenceMember(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member,
        int memberIndex)
    {
        bool repeated = !member.HasMaximumOccurs || member.MaximumOccurs > 1;
        string target = "model." + member.Name;
        if (repeated)
        {
            string suffix = memberIndex.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "int item" + suffix + " = 0;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "while (item" + suffix + " < " + target + ".Count)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteMemberElement(builder, context, owner, member,
                target + "[item" + suffix + "]", 3);
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "item" + suffix + "++;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            return;
        }
        if (member.IsValueType && member.PresenceName.Length > 0)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (model." + member.PresenceName + ")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteMemberElement(builder, context, owner, member, target, 3);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        }
        else if (!member.IsValueType && member.MinimumOccurs == 0)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (" + target + " != null)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteMemberElement(builder, context, owner, member, target, 3);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        }
        else
        {
            WriteMemberElement(builder, context, owner, member, target, 2);
        }
    }

    internal static void WriteMemberElement(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member,
        string target,
        int level)
    {
        string xmlName = "\"" + CSharpMetadataEmitter.EscapeCSharpString(
            member.XmlName) + "\"";
        if (member.ReferencedSymbol != null)
        {
            string codecName = FindQualifiedCodecName(context, member.ReferencedSymbol);
            CSharpModelEmitter.AppendSourceLine(builder, context, level,
                codecName + ".WriteModel(writer, " + target + ", " + xmlName +
                ", namespaceUri);");
            return;
        }
        string runtimeName = CSharpMetadataEmitter.FindReaderRuntimeQualifiedName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            runtimeName + ".WriteStartElement(writer, " + xmlName + ", namespaceUri);");
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            runtimeName + "." + FindRuntimeContentWriter(member.TypeName) +
            "(writer, " + target + ");");
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            "writer.WriteEndElement();");
    }

    internal static string FindRuntimeContentWriter(string typeName)
    {
        if (typeName == "global::iso20022.IsoBooleanValue")
        {
            return "WriteBooleanContent";
        }
        if (typeName == "global::iso20022.IsoDecimalValue")
        {
            return "WriteDecimalContent";
        }
        if (typeName == "global::iso20022.IsoBinaryValue")
        {
            return "WriteBinaryContent";
        }
        if (typeName == "global::iso20022.IsoDateValue")
        {
            return "WriteDateContent";
        }
        if (typeName == "global::iso20022.IsoTimeValue")
        {
            return "WriteTimeContent";
        }
        if (typeName == "global::iso20022.IsoDateTimeValue")
        {
            return "WriteDateTimeContent";
        }
        if (typeName == "global::iso20022.IsoDurationValue")
        {
            return "WriteDurationContent";
        }
        if (typeName == "global::iso20022.IsoPartialDateValue")
        {
            return "WritePartialDateContent";
        }
        return "WriteTextContent";
    }

    internal static void WriteDocumentWriter(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        MessageDefinitionMetadata message = symbol.Concept.MessageDefinition ??
            new MessageDefinitionMetadata();
        XsdGenerator helper = new XsdGenerator();
        string identifier = helper.FindIdentifierValue(message.MessageIdentifier);
        string rootElement = message.RootElement;
        if (rootElement.Length == 0)
        {
            rootElement = "Document";
        }
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void WriteDocument(global::System.Xml.XmlWriter writer, " +
            typeName + " model, string messageIdentifier, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        WriteEqualityGuard(builder, context, "messageIdentifier", identifier,
            "The message identifier does not match this generated model.");
        WriteEqualityGuard(builder, context, "namespaceUri", message.NamespaceUri,
            "The namespace does not match this generated model version.");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "WriteModel(writer, model, \"" +
            CSharpMetadataEmitter.EscapeCSharpString(rootElement) +
            "\", namespaceUri);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteEqualityGuard(
        StringBuilder builder,
        CSharpGenerationContext context,
        string variable,
        string expected,
        string message)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (" + variable + " != \"" +
            CSharpMetadataEmitter.EscapeCSharpString(expected) + "\")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.ArgumentException(\"" +
            CSharpMetadataEmitter.EscapeCSharpString(message) + "\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
    }

    internal static void WriteChoiceWriter(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        WriteChoiceValidator(builder, context, symbol, members);
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string kindName = "global::" + symbol.NamespaceName + "." +
            CSharpModelEmitter.FindChoiceKindName(context, symbol);
        string runtimeName = CSharpMetadataEmitter.FindReaderRuntimeQualifiedName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void WriteModel(global::System.Xml.XmlWriter writer, " +
            typeName + " model, string elementName, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "ValidateModel(model);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".WriteStartElement(writer, elementName, namespaceUri);");
        int index = 0;
        while (index < members.Count)
        {
            CSharpMemberMetadata member = members[index];
            string branch = "if";
            if (index > 0)
            {
                branch = "else if";
            }
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                branch + " (model.Choice == " + kindName + "." + member.Name + ")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteMemberElement(builder, context, symbol, member,
                "model." + member.Name, 3);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "writer.WriteEndElement();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteChoiceValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string kindName = "global::" + symbol.NamespaceName + "." +
            CSharpModelEmitter.FindChoiceKindName(context, symbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateModel(" + typeName + " model)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        WriteNullGuard(builder, context, "model", "The generated choice is required.", 2);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "int selected = 0;");
        int index = 0;
        while (index < members.Count)
        {
            CSharpMemberMetadata member = members[index];
            string present = "model." + member.Name + " != null";
            if (member.IsValueType)
            {
                present = "model." + member.PresenceName;
            }
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (" + present + ")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "selected++;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "if (model.Choice != " + kindName + "." + member.Name + ")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "throw new global::System.InvalidOperationException(" +
                "\"The choice discriminator does not match its populated alternative.\");");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
            WriteReferencedValidation(builder, context, symbol, member,
                "model." + member.Name, 3);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (selected != 1)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.InvalidOperationException(" +
            "\"A choice must contain exactly one populated alternative.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteSimpleWriter(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string runtimeName = CSharpMetadataEmitter.FindReaderRuntimeQualifiedName(context);
        string contentWriter = FindSimpleContentWriter(symbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateModel(" + typeName + " model)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        WriteNullGuard(builder, context, "model", "The generated value is required.", 2);
        WriteNullGuard(builder, context, "model.Value",
            "The generated simple content value is required.", 2);
        if (symbol.Concept.DataType != null &&
            symbol.Concept.DataType.Kind == DataTypeKind.Amount)
        {
            WriteNullGuard(builder, context, "model.Value.Amount",
                "The amount decimal value is required.", 2);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (model.Value.Currency.Length == 0)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "throw new global::System.InvalidOperationException(" +
                "\"The amount currency attribute is required.\");");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void WriteModel(global::System.Xml.XmlWriter writer, " +
            typeName + " model, string elementName, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "ValidateModel(model);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".WriteStartElement(writer, elementName, namespaceUri);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + "." + contentWriter + "(writer, model.Value);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "writer.WriteEndElement();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static string FindSimpleContentWriter(GenerationSymbolEntry symbol)
    {
        if (symbol.Concept.CodeSet != null)
        {
            return "WriteCodeContent";
        }
        DataTypeDefinition dataType = symbol.Concept.DataType ?? new DataTypeDefinition();
        if (dataType.Kind == DataTypeKind.Amount)
        {
            return "WriteAmountContent";
        }
        CSharpTypeReference type = CSharpModelEmitter.FindDataTypeValueType(dataType);
        return FindRuntimeContentWriter(type.Name);
    }

    internal static void WriteExternalWriter(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string runtimeName = CSharpMetadataEmitter.FindReaderRuntimeQualifiedName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateModel(" + typeName + " model)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        WriteNullGuard(builder, context, "model", "The external model is required.", 2);
        WriteNullGuard(builder, context, "model.Value",
            "The external raw fragment is required.", 2);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".ValidateRawFragment(model.Value);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void WriteModel(global::System.Xml.XmlWriter writer, " +
            typeName + " model, string elementName, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "ValidateModel(model);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".WriteStartElement(writer, elementName, namespaceUri);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".WriteRawFragment(writer, model.Value);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteEndElement();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteClosedCodeWriter(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string runtimeName = CSharpMetadataEmitter.FindReaderRuntimeQualifiedName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateCode(" + typeName + " value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (WriteCode(value).Length == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.InvalidOperationException(" +
            "\"A registered closed-code value is required.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void WriteModel(global::System.Xml.XmlWriter writer, " +
            typeName + " value, string elementName, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "ValidateCode(value);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".WriteStartElement(writer, elementName, namespaceUri);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteString(WriteCode(value));");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "writer.WriteEndElement();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static string FindQualifiedCodecName(
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        return "global::" + symbol.NamespaceName + "." +
            CSharpMetadataEmitter.FindInternalGeneratedName(context, symbol, "Codec");
    }

    internal static void WriteNullGuard(
        StringBuilder builder,
        CSharpGenerationContext context,
        string target,
        string message,
        int level)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            "if (" + target + " == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, level, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
            "throw new global::System.InvalidOperationException(\"" +
            CSharpMetadataEmitter.EscapeCSharpString(message) + "\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, level, "}");
    }

    internal static void WriteRuntime(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeNavigation(builder, context);
        WriteRuntimeText(builder, context);
        WriteRuntimeBinary(builder, context);
        WriteRuntimeCalendar(builder, context);
    }

    internal static void WriteRuntimeNavigation(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeStart(builder, context,
            "internal static void WriteStartElement(global::System.Xml.XmlWriter writer, " +
            "string localName, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (writer == null || localName.Length == 0 || namespaceUri == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.ArgumentException(" +
            "\"A writer, local name, and namespace are required.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteStartElement(string.Empty, localName, namespaceUri);");
        WriteRuntimeEnd(builder, context);
    }

    internal static void WriteRuntimeText(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeStart(builder, context,
            "internal static void WriteTextContent(global::System.Xml.XmlWriter writer, " +
            "global::iso20022.IsoTextValue value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = value.LexicalValue;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (lexical.Length == 0 && value.Value.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "lexical = value.Value;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteString(lexical);");
        WriteRuntimeEnd(builder, context);

        WriteRuntimeStart(builder, context,
            "internal static void WriteCodeContent(global::System.Xml.XmlWriter writer, " +
            "global::iso20022.IsoCodeValue value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = value.LexicalValue;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (lexical.Length == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "lexical = value.Value;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteString(lexical);");
        WriteRuntimeEnd(builder, context);

        WriteRuntimeStart(builder, context,
            "internal static void WriteBooleanContent(global::System.Xml.XmlWriter writer, " +
            "global::iso20022.IsoBooleanValue value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = value.LexicalValue;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (lexical.Length == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "lexical = global::System.Xml.XmlConvert.ToString(value.Value);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteString(lexical);");
        WriteRuntimeEnd(builder, context);

        WriteRuntimeStart(builder, context,
            "internal static void WriteDecimalContent(global::System.Xml.XmlWriter writer, " +
            "global::iso20022.IsoDecimalValue value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteString(FindDecimalLexical(value));");
        WriteRuntimeEnd(builder, context);

        WriteRuntimeStart(builder, context,
            "internal static string FindDecimalLexical(global::iso20022.IsoDecimalValue value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (value.LexicalValue.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "return value.LexicalValue;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return global::System.Xml.XmlConvert.ToString(value.Value);");
        WriteRuntimeEnd(builder, context);

        WriteRuntimeStart(builder, context,
            "internal static void WriteAmountContent(global::System.Xml.XmlWriter writer, " +
            "global::iso20022.IsoAmountValue value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (value.Currency.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "writer.WriteAttributeString(\"Ccy\", value.Currency);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteString(FindDecimalLexical(value.Amount));");
        WriteRuntimeEnd(builder, context);
    }

    internal static void WriteRuntimeBinary(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeStart(builder, context,
            "internal static void WriteBinaryContent(global::System.Xml.XmlWriter writer, " +
            "global::iso20022.IsoBinaryValue value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string lexical = value.LexicalValue;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (lexical.Length == 0 && value.Encoding == " +
            "global::iso20022.BinaryEncodingKind.Hexadecimal)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "lexical = global::System.Convert.ToHexString(value.Value);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "else if (lexical.Length == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "lexical = global::System.Convert.ToBase64String(value.Value);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteString(lexical);");
        WriteRuntimeEnd(builder, context);
    }

    internal static void WriteRuntimeCalendar(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteCalendarContentMethod(builder, context, "WriteDateContent",
            "global::iso20022.IsoDateValue", "FindDateLexical(value)");
        WriteCalendarContentMethod(builder, context, "WriteTimeContent",
            "global::iso20022.IsoTimeValue", "FindTimeLexical(value)");
        WriteCalendarContentMethod(builder, context, "WriteDateTimeContent",
            "global::iso20022.IsoDateTimeValue", "FindDateTimeLexical(value)");
        WriteCalendarContentMethod(builder, context, "WriteDurationContent",
            "global::iso20022.IsoDurationValue", "FindDurationLexical(value)");
        WriteCalendarContentMethod(builder, context, "WritePartialDateContent",
            "global::iso20022.IsoPartialDateValue", "FindPartialDateLexical(value)");
        WriteCalendarLexicalMethods(builder, context);
    }

    internal static void WriteCalendarContentMethod(
        StringBuilder builder,
        CSharpGenerationContext context,
        string methodName,
        string typeName,
        string expression)
    {
        WriteRuntimeStart(builder, context,
            "internal static void " + methodName +
            "(global::System.Xml.XmlWriter writer, " + typeName + " value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.WriteString(" + expression + ");");
        WriteRuntimeEnd(builder, context);
    }

    internal static void WriteCalendarLexicalMethods(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeStart(builder, context,
            "internal static string FindDateLexical(global::iso20022.IsoDateValue value)");
        WriteLexicalReturn(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return FindDateContentLexical(value) + " +
            "FindTimezoneLexical(value.HasTimezone, value.TimezoneOffsetMinutes, value.IsUtc, " +
            "value.IsNegativeTimezoneZero);");
        WriteRuntimeEnd(builder, context);

        WriteRuntimeStart(builder, context,
            "internal static string FindDateContentLexical(" +
            "global::iso20022.IsoDateValue value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return value.Year.ToString(\"D4\", " +
            "global::System.Globalization.CultureInfo.InvariantCulture) + \"-\" + " +
            "value.Month.ToString(\"D2\", " +
            "global::System.Globalization.CultureInfo.InvariantCulture) + \"-\" + " +
            "value.Day.ToString(\"D2\", " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        WriteRuntimeEnd(builder, context);

        WriteRuntimeStart(builder, context,
            "internal static string FindTimeLexical(global::iso20022.IsoTimeValue value)");
        WriteLexicalReturn(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string result = value.Hour.ToString(\"D2\", global::System.Globalization.CultureInfo.InvariantCulture) + " +
            "\":\" + value.Minute.ToString(\"D2\", global::System.Globalization.CultureInfo.InvariantCulture) + " +
            "\":\" + value.Second.ToString(\"D2\", global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (value.FractionalSecondDigits.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "result = result + \".\" + value.FractionalSecondDigits;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return result + FindTimezoneLexical(value.HasTimezone, " +
            "value.TimezoneOffsetMinutes, value.IsUtc, value.IsNegativeTimezoneZero);");
        WriteRuntimeEnd(builder, context);

        WriteRuntimeStart(builder, context,
            "internal static string FindDateTimeLexical(global::iso20022.IsoDateTimeValue value)");
        WriteLexicalReturn(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return FindDateContentLexical(value.Date) + \"T\" + " +
            "FindTimeLexical(value.Time);");
        WriteRuntimeEnd(builder, context);

        WriteDurationLexicalMethod(builder, context);
        WritePartialDateLexicalMethod(builder, context);
        WriteTimezoneLexicalMethod(builder, context);
    }

    internal static void WriteLexicalReturn(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (value.LexicalValue.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "return value.LexicalValue;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
    }

    internal static void WriteDurationLexicalMethod(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeStart(builder, context,
            "internal static string FindDurationLexical(global::iso20022.IsoDurationValue value)");
        WriteLexicalReturn(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string result = value.IsNegative ? \"-P\" : \"P\";");
        WriteDurationComponent(builder, context, "Years", "Y", false);
        WriteDurationComponent(builder, context, "Months", "M", false);
        WriteDurationComponent(builder, context, "Days", "D", false);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (value.HasHours || value.HasMinutes || value.HasSeconds || " +
            "value.Hours != 0 || value.Minutes != 0 || value.Seconds != 0 || " +
            "value.FractionalSecondDigits.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "result = result + \"T\";");
        WriteDurationComponent(builder, context, "Hours", "H", true);
        WriteDurationComponent(builder, context, "Minutes", "M", true);
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (value.HasSeconds || value.Seconds != 0 || " +
            "value.FractionalSecondDigits.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "result = result + value.Seconds.ToString(" +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "if (value.FractionalSecondDigits.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 5,
            "result = result + \".\" + value.FractionalSecondDigits;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "result = result + \"S\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (result == \"P\" || result == \"-P\")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "result = result + \"T0S\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return result;");
        WriteRuntimeEnd(builder, context);
    }

    internal static void WriteDurationComponent(
        StringBuilder builder,
        CSharpGenerationContext context,
        string field,
        string suffix,
        bool inTime)
    {
        int level = 2;
        if (inTime)
        {
            level = 3;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            "if (value.Has" + field + " || value." + field + " != 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, level, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
            "result = result + value." + field + ".ToString(" +
            "global::System.Globalization.CultureInfo.InvariantCulture) + \"" +
            suffix + "\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, level, "}");
    }

    internal static void WritePartialDateLexicalMethod(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeStart(builder, context,
            "internal static string FindPartialDateLexical(" +
            "global::iso20022.IsoPartialDateValue value)");
        WriteLexicalReturn(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string result = string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "switch (value.Kind)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WritePartialDateCase(builder, context, "Year",
            "value.Year.ToString(\"D4\", global::System.Globalization.CultureInfo.InvariantCulture)");
        WritePartialDateCase(builder, context, "Month",
            "\"--\" + value.Month.ToString(\"D2\", global::System.Globalization.CultureInfo.InvariantCulture)");
        WritePartialDateCase(builder, context, "Day",
            "\"---\" + value.Day.ToString(\"D2\", global::System.Globalization.CultureInfo.InvariantCulture)");
        WritePartialDateCase(builder, context, "YearMonth",
            "value.Year.ToString(\"D4\", global::System.Globalization.CultureInfo.InvariantCulture) + \"-\" + value.Month.ToString(\"D2\", global::System.Globalization.CultureInfo.InvariantCulture)");
        WritePartialDateCase(builder, context, "MonthDay",
            "\"--\" + value.Month.ToString(\"D2\", global::System.Globalization.CultureInfo.InvariantCulture) + \"-\" + value.Day.ToString(\"D2\", global::System.Globalization.CultureInfo.InvariantCulture)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "throw new global::System.InvalidOperationException(" +
            "\"A partial-date kind is required.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return result + FindTimezoneLexical(value.HasTimezone, " +
            "value.TimezoneOffsetMinutes, value.IsUtc, value.IsNegativeTimezoneZero);");
        WriteRuntimeEnd(builder, context);
    }

    internal static void WritePartialDateCase(
        StringBuilder builder,
        CSharpGenerationContext context,
        string kind,
        string expression)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "case global::iso20022.PartialDateKind." + kind + ":");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "result = " + expression + ";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
    }

    internal static void WriteTimezoneLexicalMethod(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteRuntimeStart(builder, context,
            "internal static string FindTimezoneLexical(bool hasTimezone, " +
            "int offsetMinutes, bool isUtc, bool isNegativeZero)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!hasTimezone)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (isUtc)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return \"Z\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (isNegativeZero)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return \"-00:00\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string sign = offsetMinutes < 0 ? \"-\" : \"+\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int absolute = global::System.Math.Abs(offsetMinutes);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return sign + (absolute / 60).ToString(\"D2\", " +
            "global::System.Globalization.CultureInfo.InvariantCulture) + \":\" + " +
            "(absolute % 60).ToString(\"D2\", " +
            "global::System.Globalization.CultureInfo.InvariantCulture);");
        WriteRuntimeEnd(builder, context);
    }

    internal static void WriteRuntimeStart(
        StringBuilder builder,
        CSharpGenerationContext context,
        string declaration)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, declaration);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
    }

    internal static void WriteRuntimeEnd(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }
}
