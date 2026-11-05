using System;
using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class CSharpPrimitiveValidationEmitter
{
    internal static void WriteRuntimeMethods(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteCountTextCharacters(builder, context);
        WriteCountTotalDigits(builder, context);
        WriteCountFractionDigits(builder, context);
        WriteWhitespaceMatches(builder, context);
        WritePatternMatches(builder, context);
        WriteApplySourceArtifact(builder, context);
        WriteCreateSourceArtifact(builder, context);
    }

    internal static void WriteCountTextCharacters(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static int CountTextCharacters(string value)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "int count = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "int index = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (index < value.Length)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (value[index] >= '\\uD800' && value[index] <= '\\uDBFF' && " +
            "index + 1 < value.Length && value[index + 1] >= '\\uDC00' && " +
            "value[index + 1] <= '\\uDFFF')");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "index++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "count++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "index++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return count;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteCountTotalDigits(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static int CountTotalDigits(string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "int first = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (first < lexical.Length && (lexical[first] == '+' || " +
            "lexical[first] == '-' || lexical[first] == '0' || " +
            "lexical[first] == '.'))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "first++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int last = lexical.Length - 1;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (last >= first && lexical[last] == '0' && " +
            "lexical.IndexOf('.') >= 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "last--;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "int count = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (first <= last)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (lexical[first] >= '0' && lexical[first] <= '9')");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "count++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "first++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (count == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return 1;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return count;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteCountFractionDigits(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static int CountFractionDigits(string lexical)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int point = lexical.IndexOf('.');");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (point < 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int last = lexical.Length - 1;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (last > point && lexical[last] == '0')");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "last--;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return last - point;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteWhitespaceMatches(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static bool WhitespaceMatches(string value, string mode)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (mode == \"preserve\")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "int index = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "while (index < value.Length)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (value[index] == '\\t' || value[index] == '\\r' || " +
            "value[index] == '\\n')");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "return false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (mode == \"collapse\" && value[index] == ' ' && " +
            "(index == 0 || index + 1 == value.Length || value[index - 1] == ' '))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "return false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "index++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WritePatternMatches(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static bool PatternMatches(string value, string pattern)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return global::System.Text.RegularExpressions.Regex.IsMatch(" +
            "value, pattern, global::System.Text.RegularExpressions.RegexOptions.CultureInvariant);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteApplySourceArtifact(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ApplySourceArtifact(" +
            "Iso20022GeneratedStructuralState state, int sourceOrdinal, " +
            "string sourceIdentifier)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (state.Report.Diagnostics.Count == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.ValidationDiagnostic diagnostic = " +
            "state.Report.Diagnostics[state.Report.Diagnostics.Count - 1];");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (diagnostic.SourceIdentifier == sourceIdentifier)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "diagnostic.SourceArtifact = CreateSourceArtifact(sourceOrdinal);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteCreateSourceArtifact(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static global::iso20022.ArtifactManifest? " +
            "CreateSourceArtifact(int sourceOrdinal)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.ArtifactManifest? artifact = null;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "switch (sourceOrdinal)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < context.Model.Sources.Count)
        {
            GenerationSource source = context.Model.Sources[index];
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "case " + source.Ordinal.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + ":");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "artifact = new global::iso20022.ArtifactManifest();");
            WriteArtifactAssignments(builder, context, source.Artifact, 4);
            CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return artifact;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteArtifactAssignments(
        StringBuilder builder,
        CSharpGenerationContext context,
        ArtifactManifest artifact,
        int level)
    {
        WriteAssignment(builder, context, level, "artifact.Coordinate.Kind",
            "(global::iso20022.ArtifactKind)" + ((int)artifact.Coordinate.Kind).ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        WriteStringAssignment(builder, context, level, "artifact.Coordinate.Edition",
            artifact.Coordinate.Edition);
        WriteStringAssignment(builder, context, level, "artifact.Coordinate.Identifier",
            artifact.Coordinate.Identifier);
        WriteStringAssignment(builder, context, level, "artifact.Coordinate.Version",
            artifact.Coordinate.Version);
        WriteStringAssignment(builder, context, level, "artifact.OriginUri",
            artifact.OriginUri);
        WriteByteArrayAssignment(builder, context, level, "artifact.Hash.Value",
            artifact.Hash.Value);
        WriteAssignment(builder, context, level, "artifact.Hash.Encoding",
            "(global::iso20022.BinaryEncodingKind)" + ((int)artifact.Hash.Encoding).ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        WriteStringAssignment(builder, context, level, "artifact.Hash.LexicalValue",
            artifact.Hash.LexicalValue);
        WriteStringAssignment(builder, context, level, "artifact.HashAlgorithm",
            artifact.HashAlgorithm);
        WriteStringAssignment(builder, context, level, "artifact.MediaType",
            artifact.MediaType);
        WriteStringAssignment(builder, context, level, "artifact.LicenseNotice",
            artifact.LicenseNotice);
        WriteAssignment(builder, context, level, "artifact.IsApproved",
            artifact.IsApproved ? "true" : "false");
        if (artifact.RetrievedAt != null)
        {
            WriteAssignment(builder, context, level, "artifact.RetrievedAt",
                "new global::iso20022.IsoDateTimeValue()");
            WriteDateTimeAssignments(
                builder, context, level, "artifact.RetrievedAt", artifact.RetrievedAt);
        }
        if (artifact.PublicationDate != null)
        {
            WriteAssignment(builder, context, level, "artifact.PublicationDate",
                "new global::iso20022.IsoPartialDateValue()");
            WritePartialDateAssignments(
                builder, context, level, "artifact.PublicationDate", artifact.PublicationDate);
        }
    }

    internal static void WriteDateTimeAssignments(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string target,
        IsoDateTimeValue value)
    {
        WriteDateAssignments(builder, context, level, target + ".Date", value.Date);
        WriteTimeAssignments(builder, context, level, target + ".Time", value.Time);
        WriteAssignment(builder, context, level, target + ".LexicalMode",
            "(global::iso20022.DateTimeLexicalMode)" + ((int)value.LexicalMode).ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        WriteStringAssignment(builder, context, level, target + ".LexicalValue",
            value.LexicalValue);
    }

    internal static void WriteDateAssignments(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string target,
        IsoDateValue value)
    {
        WriteIntegerAssignment(builder, context, level, target + ".Year", value.Year);
        WriteIntegerAssignment(builder, context, level, target + ".Month", value.Month);
        WriteIntegerAssignment(builder, context, level, target + ".Day", value.Day);
        WriteBooleanAssignment(
            builder, context, level, target + ".HasTimezone", value.HasTimezone);
        WriteIntegerAssignment(builder, context, level,
            target + ".TimezoneOffsetMinutes", value.TimezoneOffsetMinutes);
        WriteBooleanAssignment(builder, context, level, target + ".IsUtc", value.IsUtc);
        WriteBooleanAssignment(builder, context, level,
            target + ".IsNegativeTimezoneZero", value.IsNegativeTimezoneZero);
        WriteAssignment(builder, context, level, target + ".LexicalMode",
            "(global::iso20022.DateTimeLexicalMode)" + ((int)value.LexicalMode).ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        WriteStringAssignment(builder, context, level, target + ".LexicalValue",
            value.LexicalValue);
    }

    internal static void WriteTimeAssignments(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string target,
        IsoTimeValue value)
    {
        WriteIntegerAssignment(builder, context, level, target + ".Hour", value.Hour);
        WriteIntegerAssignment(builder, context, level, target + ".Minute", value.Minute);
        WriteIntegerAssignment(builder, context, level, target + ".Second", value.Second);
        WriteStringAssignment(builder, context, level,
            target + ".FractionalSecondDigits", value.FractionalSecondDigits);
        WriteBooleanAssignment(
            builder, context, level, target + ".HasTimezone", value.HasTimezone);
        WriteIntegerAssignment(builder, context, level,
            target + ".TimezoneOffsetMinutes", value.TimezoneOffsetMinutes);
        WriteBooleanAssignment(builder, context, level, target + ".IsUtc", value.IsUtc);
        WriteBooleanAssignment(builder, context, level,
            target + ".IsNegativeTimezoneZero", value.IsNegativeTimezoneZero);
        WriteAssignment(builder, context, level, target + ".LexicalMode",
            "(global::iso20022.DateTimeLexicalMode)" + ((int)value.LexicalMode).ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        WriteStringAssignment(builder, context, level, target + ".LexicalValue",
            value.LexicalValue);
    }

    internal static void WritePartialDateAssignments(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string target,
        IsoPartialDateValue value)
    {
        WriteAssignment(builder, context, level, target + ".Kind",
            "(global::iso20022.PartialDateKind)" + ((int)value.Kind).ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        WriteIntegerAssignment(builder, context, level, target + ".Year", value.Year);
        WriteIntegerAssignment(builder, context, level, target + ".Month", value.Month);
        WriteIntegerAssignment(builder, context, level, target + ".Day", value.Day);
        WriteBooleanAssignment(
            builder, context, level, target + ".HasTimezone", value.HasTimezone);
        WriteIntegerAssignment(builder, context, level,
            target + ".TimezoneOffsetMinutes", value.TimezoneOffsetMinutes);
        WriteBooleanAssignment(builder, context, level, target + ".IsUtc", value.IsUtc);
        WriteBooleanAssignment(builder, context, level,
            target + ".IsNegativeTimezoneZero", value.IsNegativeTimezoneZero);
        WriteStringAssignment(builder, context, level, target + ".LexicalValue",
            value.LexicalValue);
    }

    internal static void WriteIntegerAssignment(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string target,
        int value)
    {
        WriteAssignment(builder, context, level, target,
            value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static void WriteBooleanAssignment(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string target,
        bool value)
    {
        WriteAssignment(builder, context, level, target, value ? "true" : "false");
    }

    internal static void WriteStringAssignment(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string target,
        string value)
    {
        WriteAssignment(builder, context, level, target,
            CSharpStructuralValidationEmitter.QuoteExpression(value));
    }

    internal static void WriteByteArrayAssignment(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string target,
        byte[] values)
    {
        StringBuilder expression = new StringBuilder();
        expression.Append("new byte[] {");
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                expression.Append(", ");
            }
            expression.Append(values[index].ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            index++;
        }
        expression.Append(" }");
        WriteAssignment(builder, context, level, target, expression.ToString());
    }

    internal static void WriteAssignment(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string target,
        string expression)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder, context, level, target + " = " + expression + ";");
    }

    internal static void WriteTypeValidators(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        if (symbol.Concept.MessageDefinition != null ||
            symbol.Concept.MessageComponent != null ||
            symbol.Concept.Choice != null)
        {
            WriteContainerValidator(builder, context, symbol, members,
                "ValidatePrimitiveFacets");
            WriteContainerValidator(builder, context, symbol, members,
                "ValidateInternalCodes");
        }
        else if (symbol.Concept.DataType != null)
        {
            WritePrimitiveFacetValidator(builder, context, symbol);
            WritePassValidator(builder, context, symbol, "ValidateInternalCodes");
        }
        else if (symbol.Concept.CodeSet != null &&
            CSharpModelEmitter.CodeSetIsClosed(symbol.Concept.CodeSet))
        {
            WritePassValidator(builder, context, symbol, "ValidatePrimitiveFacets");
            WriteClosedCodeValidator(builder, context, symbol);
        }
        else
        {
            WritePassValidator(builder, context, symbol, "ValidatePrimitiveFacets");
            WritePassValidator(builder, context, symbol, "ValidateInternalCodes");
        }
    }

    internal static void WritePassValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string methodName)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = CSharpStructuralValidationEmitter.FindStateName(context);
        string runtimeName = CSharpStructuralValidationEmitter.FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void " + methodName + "(" + typeName + " model, " +
            stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".RecordModel(state, path, depth);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteContainerValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members,
        string methodName)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = CSharpStructuralValidationEmitter.FindStateName(context);
        string runtimeName = CSharpStructuralValidationEmitter.FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void " + methodName + "(" + typeName + " model, " +
            stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!" + runtimeName + ".RecordModel(state, path, depth))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        int index = 0;
        while (index < members.Count)
        {
            WriteContainerMember(
                builder, context, members[index], methodName, index);
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteContainerMember(
        StringBuilder builder,
        CSharpGenerationContext context,
        CSharpMemberMetadata member,
        string methodName,
        int index)
    {
        if (member.ReferencedSymbol == null)
        {
            return;
        }
        string target = "model." + member.Name;
        string path = "path + \"/" +
            CSharpMetadataEmitter.EscapeCSharpString(member.XmlName) + "\"";
        string codec = CSharpWriterEmitter.FindQualifiedCodecName(
            context, member.ReferencedSymbol);
        bool repeated = !member.HasMaximumOccurs || member.MaximumOccurs > 1;
        if (repeated)
        {
            string itemIndex = "validationItem" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (state.Continue && " + target + " != null)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "if (" + target + ".Count > state.Policy.Limits.MaxCollectionCount)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
            CSharpStructuralValidationEmitter.WriteAddDiagnosticLine(
                builder, context, 4,
                CSharpStructuralValidationEmitter.FindRuntimeName(context),
                "validation.generated.collection_limit", "ResourceLimit",
                "iso20022.validation.collection",
                "The generated collection exceeds the configured collection limit.",
                path,
                target + ".Count.ToString(" +
                "global::System.Globalization.CultureInfo.InvariantCulture)",
                CSharpStructuralValidationEmitter.QuoteExpression(
                    "A bounded generated collection."),
                member.ReferencedSymbol.Concept.Identifier);
            WriteProvenanceLine(
                builder, context, 4,
                CSharpStructuralValidationEmitter.FindRuntimeName(context),
                member.ReferencedSymbol);
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "int " + itemIndex + " = 0;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "while (state.Continue && " + itemIndex + " < " + target +
                ".Count && " + itemIndex + " < state.Policy.Limits.MaxCollectionCount)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
            string item = target + "[" + itemIndex + "]";
            if (member.IsValueType)
            {
                WriteReferencedCall(
                    builder, context, codec, methodName, item, path, 4);
            }
            else
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                    "if (" + item + " != null)");
                CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
                WriteReferencedCall(
                    builder, context, codec, methodName, item, path, 5);
                CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
            }
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                itemIndex + "++;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            return;
        }
        if (member.IsValueType)
        {
            if (member.PresenceName.Length > 0)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                    "if (state.Continue && model." + member.PresenceName + ")");
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
                WriteReferencedCall(
                    builder, context, codec, methodName, target, path, 3);
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            }
            else
            {
                WriteReferencedCall(
                    builder, context, codec, methodName, target, path, 2);
            }
            return;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (state.Continue && " + target + " != null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteReferencedCall(builder, context, codec, methodName, target, path, 3);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
    }

    internal static void WriteReferencedCall(
        StringBuilder builder,
        CSharpGenerationContext context,
        string codec,
        string methodName,
        string target,
        string path,
        int level)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            codec + "." + methodName + "(" + target + ", state, " + path +
            ", depth + 1);");
    }

    internal static void WritePrimitiveFacetValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = CSharpStructuralValidationEmitter.FindStateName(context);
        string runtimeName = CSharpStructuralValidationEmitter.FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidatePrimitiveFacets(" + typeName + " model, " +
            stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!" + runtimeName + ".RecordModel(state, path, depth))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        DataTypeDefinition? dataType = symbol.Concept.DataType;
        string present = "model.Value != null";
        if (dataType != null)
        {
            present = FindPrimitivePresenceExpression(dataType);
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (" + present + ")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteFacetChecks(builder, context, symbol, runtimeName);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteFacetChecks(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string runtimeName)
    {
        DataTypeDefinition? dataType = symbol.Concept.DataType;
        if (dataType == null)
        {
            return;
        }
        List<DataTypeFacet> facets = new List<DataTypeFacet>();
        facets.AddRange(dataType.Facets);
        XsdLogicalModelEmitter.SortDataTypeFacets(facets);
        string logicalValue = FindLogicalValueExpression(dataType);
        string lexicalValue = FindLexicalValueExpression(dataType);
        string length = runtimeName + ".CountTextCharacters(" + logicalValue + ")";
        if (DataTypeIsBinary(dataType))
        {
            length = "model.Value.Value.Length";
        }
        int index = 0;
        while (index < facets.Count)
        {
            DataTypeFacet facet = facets[index];
            if (facet.Name == "length" || facet.Name == "minLength" ||
                facet.Name == "maxLength")
            {
                WriteLengthFacet(builder, context, symbol, runtimeName,
                    facet, length, logicalValue);
            }
            else if (facet.Name == "pattern" && facet.Value.Length > 0)
            {
                WriteFacetCondition(builder, context, symbol, runtimeName,
                    "!" + runtimeName + ".PatternMatches(" + logicalValue + ", " +
                    CSharpStructuralValidationEmitter.QuoteExpression(facet.Value) + ")",
                    "pattern", logicalValue,
                    "The value must match the repository pattern " + facet.Value + ".");
            }
            else if (facet.Name == "enumeration")
            {
                WriteEnumerationFacet(builder, context, symbol, runtimeName,
                    facet, logicalValue);
            }
            else if (facet.Name == "whiteSpace" && facet.Value.Length > 0)
            {
                WriteFacetCondition(builder, context, symbol, runtimeName,
                    "!" + runtimeName + ".WhitespaceMatches(" + logicalValue + ", " +
                    CSharpStructuralValidationEmitter.QuoteExpression(facet.Value) + ")",
                    "white_space", logicalValue,
                    "The value must satisfy whiteSpace=" + facet.Value + ".");
            }
            else if (facet.Name == "totalDigits" ||
                facet.Name == "fractionDigits")
            {
                WriteDigitFacet(builder, context, symbol, runtimeName,
                    facet, lexicalValue);
            }
            else if (facet.Name == "minInclusive" ||
                facet.Name == "minExclusive" ||
                facet.Name == "maxInclusive" ||
                facet.Name == "maxExclusive")
            {
                WriteNumericBoundFacet(builder, context, symbol, runtimeName,
                    dataType, facet);
            }
            index++;
        }
    }

    internal static void WriteLengthFacet(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string runtimeName,
        DataTypeFacet facet,
        string length,
        string offending)
    {
        int value = FindFacetInteger(facet);
        if (value < 0)
        {
            return;
        }
        string comparison = length + " != " + value.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        if (facet.Name == "minLength")
        {
            comparison = length + " < " + value.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }
        else if (facet.Name == "maxLength")
        {
            comparison = length + " > " + value.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }
        WriteFacetCondition(builder, context, symbol, runtimeName, comparison,
            ToRuleSuffix(facet.Name), offending,
            "The value must satisfy " + facet.Name + "=" + value.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ".");
    }

    internal static void WriteEnumerationFacet(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string runtimeName,
        DataTypeFacet facet,
        string logicalValue)
    {
        List<string> values = new List<string>();
        values.AddRange(facet.EnumerationValues);
        if (values.Count == 0 && facet.Value.Length > 0)
        {
            values.Add(facet.Value);
        }
        XsdLogicalModelEmitter.SortFacetValues(values);
        if (values.Count == 0)
        {
            return;
        }
        string condition = string.Empty;
        string expected = string.Empty;
        int index = 0;
        while (index < values.Count)
        {
            if (condition.Length > 0)
            {
                condition += " && ";
                expected += ", ";
            }
            condition += logicalValue + " != " +
                CSharpStructuralValidationEmitter.QuoteExpression(values[index]);
            expected += values[index];
            index++;
        }
        WriteFacetCondition(builder, context, symbol, runtimeName, condition,
            "enumeration", logicalValue,
            "The value must be one of: " + expected + ".");
    }

    internal static void WriteDigitFacet(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string runtimeName,
        DataTypeFacet facet,
        string lexicalValue)
    {
        int value = FindFacetInteger(facet);
        if (value < 0)
        {
            return;
        }
        string counter = "CountTotalDigits";
        if (facet.Name == "fractionDigits")
        {
            counter = "CountFractionDigits";
        }
        WriteFacetCondition(builder, context, symbol, runtimeName,
            runtimeName + "." + counter + "(" + lexicalValue + ") > " +
            value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ToRuleSuffix(facet.Name), lexicalValue,
            "The value must satisfy " + facet.Name + "=" + value.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ".");
    }

    internal static void WriteNumericBoundFacet(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string runtimeName,
        DataTypeDefinition dataType,
        DataTypeFacet facet)
    {
        if (!DataTypeIsDecimal(dataType))
        {
            return;
        }
        IsoDecimalValue? parsed = facet.DecimalValue;
        if (parsed == null)
        {
            parsed = DecimalLexical.ParseDecimal(facet.Value.AsSpan());
        }
        if (parsed == null)
        {
            return;
        }
        string comparison = "<";
        if (facet.Name == "minExclusive")
        {
            comparison = "<=";
        }
        else if (facet.Name == "maxInclusive")
        {
            comparison = ">";
        }
        else if (facet.Name == "maxExclusive")
        {
            comparison = ">=";
        }
        string numeric = "model.Value.Value";
        string offending = "model.Value.LexicalValue";
        if (dataType.Kind == DataTypeKind.Amount)
        {
            numeric = "model.Value.Amount.Value";
            offending = "model.Value.Amount.LexicalValue";
        }
        string literal = parsed.Value.ToString(
            System.Globalization.CultureInfo.InvariantCulture) + "m";
        WriteFacetCondition(builder, context, symbol, runtimeName,
            numeric + " " + comparison + " " + literal,
            ToRuleSuffix(facet.Name), offending,
            "The value must satisfy " + facet.Name + "=" + facet.Value + ".");
    }

    internal static void WriteFacetCondition(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        string runtimeName,
        string condition,
        string ruleSuffix,
        string offending,
        string expected)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (state.Continue && " + condition + ")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpStructuralValidationEmitter.WriteAddDiagnosticLine(
            builder, context, 4, runtimeName,
            "validation.primitive." + ruleSuffix,
            "InvalidValue",
            "iso20022.facet." + symbol.Concept.Identifier + "." + ruleSuffix,
            "The generated primitive value violates a repository facet.",
            "path", offending,
            CSharpStructuralValidationEmitter.QuoteExpression(expected),
            symbol.Concept.Identifier);
        WriteProvenanceLine(builder, context, 4, runtimeName, symbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
    }

    internal static void WriteClosedCodeValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = CSharpStructuralValidationEmitter.FindStateName(context);
        string runtimeName = CSharpStructuralValidationEmitter.FindRuntimeName(context);
        CodeSetDefinition? codeSet = symbol.Concept.CodeSet;
        if (codeSet == null)
        {
            return;
        }
        List<CodeValueDefinition> codes =
            CSharpModelEmitter.CollectClosedCodeValues(codeSet);
        List<string> names = FindClosedCodeMemberNames(symbol, codes);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateInternalCodes(" + typeName + " model, " +
            stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!" + runtimeName + ".RecordModel(state, path, depth))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "switch (model)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < names.Count)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "case " + typeName + "." + names[index] + ":");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpStructuralValidationEmitter.WriteAddDiagnosticLine(
            builder, context, 4, runtimeName,
            "validation.internal_code.unregistered",
            "InvalidValue",
            "iso20022.code." + symbol.Concept.Identifier,
            "The generated closed-code value is not registered in the repository.",
            "path", "model.ToString()",
            CSharpStructuralValidationEmitter.QuoteExpression(
                "One registered code from " + FindCodeLexicalList(codes) + "."),
            symbol.Concept.Identifier);
        WriteProvenanceLine(builder, context, 4, runtimeName, symbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static List<string> FindClosedCodeMemberNames(
        GenerationSymbolEntry symbol,
        List<CodeValueDefinition> codes)
    {
        List<string> names = new List<string>();
        List<string> used = new List<string>();
        used.Add(symbol.SymbolName);
        used.Add("Unknown");
        int index = 0;
        while (index < codes.Count)
        {
            string candidate = codes[index].Name;
            if (candidate.Length == 0)
            {
                candidate = codes[index].Value;
            }
            names.Add(CSharpModelEmitter.CreateUniqueMemberName(
                used, candidate, codes[index].Identifier + "|" + codes[index].Value));
            index++;
        }
        return names;
    }

    internal static string FindCodeLexicalList(List<CodeValueDefinition> codes)
    {
        string result = string.Empty;
        int index = 0;
        while (index < codes.Count)
        {
            if (result.Length > 0)
            {
                result += ", ";
            }
            result += codes[index].Value;
            index++;
        }
        return result;
    }

    internal static void WriteProvenanceLine(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string runtimeName,
        GenerationSymbolEntry symbol)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            runtimeName + ".ApplySourceArtifact(state, " +
            symbol.Concept.SourceOrdinal.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ", " +
            CSharpStructuralValidationEmitter.QuoteExpression(
                symbol.Concept.Identifier) + ");");
    }

    internal static string FindLogicalValueExpression(DataTypeDefinition dataType)
    {
        if (DataTypeIsText(dataType))
        {
            return "model.Value.Value";
        }
        return FindLexicalValueExpression(dataType);
    }

    internal static string FindPrimitivePresenceExpression(
        DataTypeDefinition dataType)
    {
        if (DataTypeIsText(dataType))
        {
            return "model.Value != null && model.Value.Value != null";
        }
        if (dataType.Kind == DataTypeKind.Amount)
        {
            return "model.Value != null && model.Value.Amount != null && " +
                "model.Value.Amount.LexicalValue != null";
        }
        if (DataTypeIsBinary(dataType))
        {
            return "model.Value != null && model.Value.Value != null && " +
                "model.Value.LexicalValue != null";
        }
        return "model.Value != null && model.Value.LexicalValue != null";
    }

    internal static string FindLexicalValueExpression(DataTypeDefinition dataType)
    {
        if (dataType.Kind == DataTypeKind.Amount)
        {
            return "model.Value.Amount.LexicalValue";
        }
        return "model.Value.LexicalValue";
    }

    internal static bool DataTypeIsDecimal(DataTypeDefinition dataType)
    {
        CSharpTypeReference type = CSharpModelEmitter.FindDataTypeValueType(dataType);
        return dataType.Kind == DataTypeKind.Amount ||
            type.Name == "global::iso20022.IsoDecimalValue";
    }

    internal static bool DataTypeIsBinary(DataTypeDefinition dataType)
    {
        CSharpTypeReference type = CSharpModelEmitter.FindDataTypeValueType(dataType);
        return type.Name == "global::iso20022.IsoBinaryValue";
    }

    internal static bool DataTypeIsText(DataTypeDefinition dataType)
    {
        CSharpTypeReference type = CSharpModelEmitter.FindDataTypeValueType(dataType);
        return type.Name == "global::iso20022.IsoTextValue";
    }

    internal static int FindFacetInteger(DataTypeFacet facet)
    {
        if (facet.HasIntegerValue)
        {
            return facet.IntegerValue;
        }
        return JsonSchemaLogicalModelEmitter.ParseFacetInteger(facet.Value);
    }

    internal static string ToRuleSuffix(string name)
    {
        if (name == "minLength")
        {
            return "minimum_length";
        }
        if (name == "maxLength")
        {
            return "maximum_length";
        }
        if (name == "totalDigits")
        {
            return "total_digits";
        }
        if (name == "fractionDigits")
        {
            return "fraction_digits";
        }
        if (name == "minInclusive")
        {
            return "minimum_inclusive";
        }
        if (name == "minExclusive")
        {
            return "minimum_exclusive";
        }
        if (name == "maxInclusive")
        {
            return "maximum_inclusive";
        }
        if (name == "maxExclusive")
        {
            return "maximum_exclusive";
        }
        return name;
    }

    internal static void WriteRegistryValidators(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        WriteRegistryValidator(builder, context, entries,
            "ValidateMessagePrimitiveFacets", "ValidatePrimitiveFacets",
            "PrimitiveFacet");
        WriteRegistryValidator(builder, context, entries,
            "ValidateMessageInternalCodes", "ValidateInternalCodes",
            "InternalCode");
    }

    internal static void WriteRegistryValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries,
        string registryMethod,
        string codecMethod,
        string stage)
    {
        string stateName = CSharpStructuralValidationEmitter.FindStateName(context);
        string runtimeName = CSharpStructuralValidationEmitter.FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static global::iso20022.ValidationReport " + registryMethod +
            "(object? model, string? messageIdentifier, string? namespaceUri, " +
            "global::iso20022.ValidationPolicy? policy)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (model == null || messageIdentifier == null || namespaceUri == null || " +
            "policy == null || policy.Limits == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "return " + runtimeName + ".CreateFailure(" +
            "\"validation.generated.request_invalid\", " +
            "\"A model, exact identity, namespace, policy, and limits are required.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            stateName + " state = " + runtimeName +
            ".CreateState(policy, messageIdentifier, namespaceUri, " +
            "global::iso20022.ValidationStage." + stage + ");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "switch (messageIdentifier + \"\\n\" + namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < entries.Count)
        {
            WriteRegistryCase(builder, context, entries[index], codecMethod, runtimeName);
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "string expectedNamespace = FindMessageNamespace(messageIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "if (expectedNamespace.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        CSharpStructuralValidationEmitter.WriteAddDiagnosticLine(
            builder, context, 5, runtimeName,
            "validation.structural.namespace_mismatch", "InvalidValue",
            "iso20022.structure.version",
            "The model namespace does not match its generated message version.",
            "string.Empty", "namespaceUri", "expectedNamespace", "messageIdentifier");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        CSharpStructuralValidationEmitter.WriteRegistryUnknownIdentity(
            builder, context, entries, runtimeName);
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return " + runtimeName + ".CompleteReport(state);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRegistryCase(
        StringBuilder builder,
        CSharpGenerationContext context,
        CSharpRegistryEntry entry,
        string codecMethod,
        string runtimeName)
    {
        string runtimeType = entry.ModelTypeName;
        if (runtimeType.StartsWith("global::", System.StringComparison.Ordinal))
        {
            runtimeType = runtimeType.Substring(8);
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "case \"" + CSharpMetadataEmitter.EscapeCSharpString(
                entry.MessageIdentifier + "\n" + entry.NamespaceUri) + "\":");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "if (model.GetType().FullName != \"" +
            CSharpMetadataEmitter.EscapeCSharpString(runtimeType) + "\")");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        CSharpStructuralValidationEmitter.WriteAddDiagnosticLine(
            builder, context, 5, runtimeName,
            "validation.structural.model_type_mismatch", "InvalidValue",
            "iso20022.structure.model",
            "The supplied model type does not match the selected generated message version.",
            "string.Empty", "model.GetType().FullName ?? string.Empty",
            CSharpStructuralValidationEmitter.QuoteExpression(runtimeType),
            entry.SourceIdentifier);
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 5,
            entry.CodecTypeName + "." + codecMethod + "((" + entry.ModelTypeName +
            ")model, state, \"/" + CSharpMetadataEmitter.EscapeCSharpString(
                entry.RootElement) + "\", 1);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
    }
}
