using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class CSharpStructuralValidationEmitter
{
    internal static CSharpGeneratedSource? EmitRuntimeSource(
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
        WriteRuntimeState(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, string.Empty);
        WriteRuntimeClass(builder, context);

        CSharpGeneratedSource source = new CSharpGeneratedSource();
        source.Kind = GenerationFileKind.Metadata;
        source.TypeName = "Iso20022GeneratedStructuralRuntime";
        source.NamespaceName = namespaceName;
        source.RelativePath = CSharpMetadataEmitter.FindInternalOutputDirectory(
            context,
            namespaceName) + "/Iso20022GeneratedStructuralRuntime.cs";
        source.Content = Encoding.UTF8.GetBytes(builder.ToString());
        return source;
    }

    internal static void WriteRuntimeState(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 0, "internal class Iso20022GeneratedStructuralState");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal global::iso20022.ValidationPolicy Policy = " +
            "new global::iso20022.ValidationPolicy();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal global::iso20022.ValidationReport Report = " +
            "new global::iso20022.ValidationReport();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal bool Continue = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal int ModelCount;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal global::iso20022.ValidationStage Stage;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal global::iso20022.ICodeSetProvider? CodeSetProvider;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal global::iso20022.IsoDateValue? ActiveOn;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteRuntimeClass(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 0, "internal static class Iso20022GeneratedStructuralRuntime");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "{");
        WriteRuntimeCreateState(builder, context);
        WriteRuntimeCreateFailure(builder, context);
        WriteRuntimeAddDiagnostic(builder, context);
        WriteRuntimeRecordModel(builder, context);
        WriteRuntimeCompleteReport(builder, context);
        CSharpPrimitiveValidationEmitter.WriteRuntimeMethods(builder, context);
        CSharpExternalCodeValidationEmitter.WriteRuntimeMethods(builder, context);
        CSharpRepositoryConstraintValidationEmitter.WriteRuntimeMethods(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteRuntimeCreateState(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static Iso20022GeneratedStructuralState CreateState(" +
            "global::iso20022.ValidationPolicy policy, string messageIdentifier, " +
            "string namespaceUri, global::iso20022.ValidationStage stage)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "Iso20022GeneratedStructuralState state = " +
            "new Iso20022GeneratedStructuralState();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Policy = policy;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Stage = stage;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.Success = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.Valid = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.MessageIdentifier = new global::iso20022.MessageIdentifier();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.MessageIdentifier.Value = messageIdentifier;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.NamespaceUri = namespaceUri;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return state;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRuntimeCreateFailure(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static global::iso20022.ValidationReport CreateFailure(" +
            "string code, string message)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.ValidationReport report = " +
            "new global::iso20022.ValidationReport();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "report.Success = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "report.Valid = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "report.ErrorCode = code;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "report.ErrorMessage = message;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "report.ErrorCount = 1;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.ValidationDiagnostic diagnostic = " +
            "new global::iso20022.ValidationDiagnostic();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Code = code;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.RuleIdentifier = \"iso20022.structure.dispatch\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Severity = global::iso20022.ValidationSeverity.Error;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Stage = global::iso20022.ValidationStage.Structural;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Kind = global::iso20022.DiagnosticKind.Configuration;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Message = message;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "report.Diagnostics.Add(diagnostic);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return report;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRuntimeAddDiagnostic(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void AddDiagnostic(" +
            "Iso20022GeneratedStructuralState state, string code, " +
            "global::iso20022.DiagnosticKind kind, string ruleIdentifier, " +
            "string message, string path, string offendingValue, " +
            "string expectedConstraint, string sourceIdentifier)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!state.Continue)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (state.Report.Diagnostics.Count >= " +
            "state.Policy.Limits.MaxDiagnosticCount)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.Success = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.Valid = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.DiagnosticLimitReached = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.StoppedEarly = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.ErrorCode = \"validation.diagnostic_limit\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.ErrorMessage = " +
            "\"Generated structural validation reached the diagnostic limit.\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Continue = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.ValidationDiagnostic diagnostic = " +
            "new global::iso20022.ValidationDiagnostic();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Code = code;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.RuleIdentifier = ruleIdentifier;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Severity = global::iso20022.ValidationSeverity.Error;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Stage = state.Stage;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Kind = kind;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Message = message;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Path.Value = path;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.OffendingValue = offendingValue;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.ExpectedConstraint = expectedConstraint;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.SourceIdentifier = sourceIdentifier;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (state.Policy.RedactOffendingValues && offendingValue.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "diagnostic.OffendingValue = string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "diagnostic.OffendingValueRedacted = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.Diagnostics.Add(diagnostic);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.ErrorCount++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.Valid = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (kind == global::iso20022.DiagnosticKind.ResourceLimit)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.Success = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.ErrorCode = code;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.ErrorMessage = message;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.StoppedEarly = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Continue = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "else if (state.Policy.FailFast)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.StoppedEarly = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Continue = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRuntimeRecordModel(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static bool RecordModel(" +
            "Iso20022GeneratedStructuralState state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!state.Continue)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.ModelCount++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (depth > state.Policy.Limits.MaxDepth || " +
            "state.ModelCount > state.Policy.Limits.MaxElementCount)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "AddDiagnostic(state, \"validation.structural.resource_limit\", " +
            "global::iso20022.DiagnosticKind.ResourceLimit, " +
            "\"iso20022.structure.resource\", " +
            "\"The generated model graph exceeds a structural resource limit.\", " +
            "path, string.Empty, \"A bounded generated model graph.\", string.Empty);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return state.Continue;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRuntimeCompleteReport(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static global::iso20022.ValidationReport CompleteReport(" +
            "Iso20022GeneratedStructuralState state)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.CompletedStages.Add(state.Stage);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return state.Report;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteTypeValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        if (symbol.Concept.MessageDefinition != null ||
            symbol.Concept.MessageComponent != null)
        {
            WriteSequenceValidator(builder, context, symbol, members);
        }
        else if (symbol.Concept.Choice != null)
        {
            WriteChoiceValidator(builder, context, symbol, members);
        }
        else if (symbol.Concept.CodeSet != null &&
            CSharpModelEmitter.CodeSetIsClosed(symbol.Concept.CodeSet))
        {
            WriteClosedCodeValidator(builder, context, symbol);
        }
        else if (symbol.Concept.ExternalSchema != null)
        {
            WriteExternalValidator(builder, context, symbol);
        }
        else
        {
            WriteSimpleValidator(builder, context, symbol, members);
        }
    }

    internal static void WriteSequenceValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = FindStateName(context);
        string runtimeName = FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateStructure(" + typeName + " model, " +
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
            WriteSequenceMemberValidator(
                builder, context, symbol, members[index], index);
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteSequenceMemberValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member,
        int index)
    {
        bool repeated = !member.HasMaximumOccurs || member.MaximumOccurs > 1;
        if (repeated)
        {
            WriteRepeatedValidator(builder, context, owner, member, index);
        }
        else
        {
            WriteSingletonValidator(builder, context, owner, member);
        }
    }

    internal static void WriteRepeatedValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member,
        int index)
    {
        string runtimeName = FindRuntimeName(context);
        string target = "model." + member.Name;
        string path = "path + \"/" +
            CSharpMetadataEmitter.EscapeCSharpString(member.XmlName) + "\"";
        string rule = "iso20022.structure." + owner.Concept.Identifier + "." +
            member.XmlName;
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (" + target + " == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteAddDiagnosticLine(builder, context, 3, runtimeName,
            "validation.structural.collection_missing", "MissingContent", rule,
            "The generated collection is missing.", path, "string.Empty",
            QuoteExpression("A non-null generated collection."),
            owner.Concept.Identifier);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (" + target + ".Count > state.Policy.Limits.MaxCollectionCount)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        WriteAddDiagnosticLine(builder, context, 4, runtimeName,
            "validation.structural.collection_limit", "ResourceLimit", rule,
            "The generated collection exceeds the configured collection limit.",
            path, target + ".Count.ToString(" +
            "global::System.Globalization.CultureInfo.InvariantCulture)",
            QuoteExpression("A bounded generated collection."),
            owner.Concept.Identifier);
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        if (member.MinimumOccurs > 0)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "if (state.Continue && " + target + ".Count < " +
                member.MinimumOccurs.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + ")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
            WriteAddDiagnosticLine(builder, context, 4, runtimeName,
                "validation.structural.minimum_occurs", "Cardinality", rule,
                "The generated collection contains fewer than the required items.",
                path, target + ".Count.ToString(" +
                "global::System.Globalization.CultureInfo.InvariantCulture)",
                QuoteExpression("At least " + member.MinimumOccurs.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + " item(s)."),
                owner.Concept.Identifier);
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        }
        if (member.HasMaximumOccurs)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "if (state.Continue && " + target + ".Count > " +
                member.MaximumOccurs.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + ")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
            WriteAddDiagnosticLine(builder, context, 4, runtimeName,
                "validation.structural.maximum_occurs", "Cardinality", rule,
                "The generated collection contains more than the permitted items.",
                path, target + ".Count.ToString(" +
                "global::System.Globalization.CultureInfo.InvariantCulture)",
                QuoteExpression("At most " + member.MaximumOccurs.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + " item(s)."),
                owner.Concept.Identifier);
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        }
        string suffix = index.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "int structuralItem" + suffix + " = 0;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "while (state.Continue && structuralItem" + suffix + " < " +
            target + ".Count)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        string item = target + "[structuralItem" + suffix + "]";
        if (!member.IsValueType)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "if (" + item + " == null)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
            WriteAddDiagnosticLine(builder, context, 5, runtimeName,
                "validation.structural.null_item", "MissingContent", rule,
                "A generated collection contains a null item.", path,
                "string.Empty", QuoteExpression("A populated collection item."),
                owner.Concept.Identifier);
            CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
            if (member.ReferencedSymbol != null &&
                !ReferencedSymbolIsClosedCode(member.ReferencedSymbol))
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 4, "else");
                CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
                WriteReferencedValidator(builder, context, member, item, path, 5);
                CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
            }
        }
        else
        {
            WriteReferencedValidator(builder, context, member, item, path, 4);
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "structuralItem" + suffix + "++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
    }

    internal static void WriteSingletonValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry owner,
        CSharpMemberMetadata member)
    {
        string runtimeName = FindRuntimeName(context);
        string target = "model." + member.Name;
        string path = "path + \"/" +
            CSharpMetadataEmitter.EscapeCSharpString(member.XmlName) + "\"";
        string rule = "iso20022.structure." + owner.Concept.Identifier + "." +
            member.XmlName;
        if (member.IsValueType)
        {
            if (member.PresenceName.Length > 0)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                    "if (model." + member.PresenceName + ")");
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
                WriteReferencedValidator(builder, context, member, target, path, 3);
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            }
            else
            {
                WriteReferencedValidator(builder, context, member, target, path, 2);
            }
            return;
        }

        if (member.MinimumOccurs > 0)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (" + target + " == null)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteAddDiagnosticLine(builder, context, 3, runtimeName,
                "validation.structural.required_missing", "MissingContent", rule,
                "A required generated field is missing.", path, "string.Empty",
                QuoteExpression("One required value."), owner.Concept.Identifier);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            if (member.ReferencedSymbol != null &&
                !ReferencedSymbolIsClosedCode(member.ReferencedSymbol))
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                    "else if (state.Continue)");
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
                WriteReferencedValidator(builder, context, member, target, path, 3);
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            }
        }
        else if (member.ReferencedSymbol != null &&
            !ReferencedSymbolIsClosedCode(member.ReferencedSymbol))
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (state.Continue && " + target + " != null)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteReferencedValidator(builder, context, member, target, path, 3);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        }
    }

    internal static void WriteReferencedValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        CSharpMemberMetadata member,
        string target,
        string path,
        int level)
    {
        if (member.ReferencedSymbol == null ||
            ReferencedSymbolIsClosedCode(member.ReferencedSymbol))
        {
            return;
        }
        string codec = CSharpWriterEmitter.FindQualifiedCodecName(
            context,
            member.ReferencedSymbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            codec + ".ValidateStructure(" + target + ", state, " + path +
            ", depth + 1);");
    }

    internal static void WriteChoiceValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = FindStateName(context);
        string runtimeName = FindRuntimeName(context);
        string kindName = "global::" + symbol.NamespaceName + "." +
            CSharpModelEmitter.FindChoiceKindName(context, symbol);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateStructure(" + typeName + " model, " +
            stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!" + runtimeName + ".RecordModel(state, path, depth))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "int structuralSelected = 0;");
        int index = 0;
        while (index < members.Count)
        {
            CSharpMemberMetadata member = members[index];
            string present = "model." + member.Name + " != null";
            if (member.IsValueType)
            {
                present = "model." + member.PresenceName;
            }
            string memberPath = "path + \"/" +
                CSharpMetadataEmitter.EscapeCSharpString(member.XmlName) + "\"";
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (state.Continue && " + present + ")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "structuralSelected++;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "if (model.Choice != " + kindName + "." + member.Name + ")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
            WriteAddDiagnosticLine(builder, context, 4, runtimeName,
                "validation.structural.choice_discriminator", "Choice",
                "iso20022.structure." + symbol.Concept.Identifier + ".choice",
                "The choice discriminator does not match its populated alternative.",
                memberPath, "model.Choice.ToString()",
                QuoteExpression("The discriminator for " + member.XmlName + "."),
                symbol.Concept.Identifier);
            CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
            WriteReferencedValidator(
                builder, context, member, "model." + member.Name, memberPath, 3);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (state.Continue && structuralSelected != 1)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteAddDiagnosticLine(builder, context, 3, runtimeName,
            "validation.structural.choice_cardinality", "Choice",
            "iso20022.structure." + symbol.Concept.Identifier + ".choice",
            "A generated choice must contain exactly one populated alternative.",
            "path", "structuralSelected.ToString(" +
            "global::System.Globalization.CultureInfo.InvariantCulture)",
            QuoteExpression("Exactly one populated choice alternative."),
            symbol.Concept.Identifier);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteSimpleValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = FindStateName(context);
        string runtimeName = FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateStructure(" + typeName + " model, " +
            stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!" + runtimeName + ".RecordModel(state, path, depth))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        if (members.Count > 0 && !members[0].IsValueType)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "if (model.Value == null)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteAddDiagnosticLine(builder, context, 3, runtimeName,
                "validation.structural.simple_content_missing", "MissingContent",
                "iso20022.structure." + symbol.Concept.Identifier + ".value",
                "The generated simple-content value is missing.", "path",
                "string.Empty", QuoteExpression("One simple-content value."),
                symbol.Concept.Identifier);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        }
        if (symbol.Concept.DataType != null &&
            symbol.Concept.DataType.Kind == DataTypeKind.Amount)
        {
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "else if (model.Value.Amount == null)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteAddDiagnosticLine(builder, context, 3, runtimeName,
                "validation.structural.amount_missing", "MissingContent",
                "iso20022.structure." + symbol.Concept.Identifier + ".amount",
                "The generated amount decimal value is missing.", "path",
                "string.Empty", QuoteExpression("One amount decimal value."),
                symbol.Concept.Identifier);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                "else if (model.Value.Currency.Length == 0)");
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
            WriteAddDiagnosticLine(builder, context, 3, runtimeName,
                "validation.structural.currency_missing", "MissingContent",
                "iso20022.structure." + symbol.Concept.Identifier + ".currency",
                "The generated amount currency attribute is missing.", "path",
                "string.Empty", QuoteExpression("One currency attribute."),
                symbol.Concept.Identifier);
            CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteExternalValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = FindStateName(context);
        string runtimeName = FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateStructure(" + typeName + " model, " +
            stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!" + runtimeName + ".RecordModel(state, path, depth))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (model.Value == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteAddDiagnosticLine(builder, context, 3, runtimeName,
            "validation.structural.raw_content_missing", "MissingContent",
            "iso20022.structure." + symbol.Concept.Identifier + ".raw",
            "The generated wildcard content is missing.", "path", "string.Empty",
            QuoteExpression("One namespace-complete raw fragment."),
            symbol.Concept.Identifier);
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteClosedCodeValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = FindStateName(context);
        string runtimeName = FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateStructure(" + typeName + " model, " +
            stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".RecordModel(state, path, depth);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRegistryValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        string stateName = FindStateName(context);
        string runtimeName = FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static global::iso20022.ValidationReport " +
            "ValidateMessageModel(object? model, string? messageIdentifier, " +
            "string? namespaceUri, global::iso20022.ValidationPolicy? policy)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (model == null || messageIdentifier == null || namespaceUri == null || " +
            "policy == null || policy.Limits == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "return " + runtimeName + ".CreateFailure(" +
            "\"validation.structural.request_invalid\", " +
            "\"A model, exact identity, namespace, policy, and limits are required.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            stateName + " state = " + runtimeName +
            ".CreateState(policy, messageIdentifier, namespaceUri, " +
            "global::iso20022.ValidationStage.Structural);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "switch (messageIdentifier + \"\\n\" + namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < entries.Count)
        {
            WriteRegistryKnownCase(
                builder, context, entries[index], index, runtimeName);
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "string expectedNamespace = FindMessageNamespace(messageIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "if (expectedNamespace.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        WriteAddDiagnosticLine(builder, context, 5, runtimeName,
            "validation.structural.namespace_mismatch", "InvalidValue",
            "iso20022.structure.version", "The model namespace does not match its " +
            "generated message version.", "string.Empty", "namespaceUri",
            "expectedNamespace", "messageIdentifier");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        WriteRegistryUnknownIdentity(builder, context, entries, runtimeName);
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return " + runtimeName + ".CompleteReport(state);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRegistryKnownCase(
        StringBuilder builder,
        CSharpGenerationContext context,
        CSharpRegistryEntry entry,
        int index,
        string runtimeName)
    {
        string modelType = entry.ModelTypeName;
        string runtimeType = modelType;
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
        WriteAddDiagnosticLine(builder, context, 5, runtimeName,
            "validation.structural.model_type_mismatch", "InvalidValue",
            "iso20022.structure.model", "The supplied model type does not match the " +
            "selected generated message version.", "string.Empty",
            "model.GetType().FullName ?? string.Empty", "\"" +
            CSharpMetadataEmitter.EscapeCSharpString(runtimeType) + "\"",
            entry.SourceIdentifier);
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "else");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 5,
            entry.CodecTypeName + ".ValidateStructure((" + modelType + ")model, " +
            "state, \"/" + CSharpMetadataEmitter.EscapeCSharpString(
                entry.RootElement) + "\", 1);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
    }

    internal static void WriteRegistryUnknownIdentity(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries,
        string runtimeName)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 5,
            "switch (namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 5, "{");
        List<string> emitted = new List<string>();
        int index = 0;
        while (index < entries.Count)
        {
            CSharpRegistryEntry entry = entries[index];
            if (entry.NamespaceUri.Length > 0 &&
                !CSharpModelEmitter.TextListContains(emitted, entry.NamespaceUri))
            {
                emitted.Add(entry.NamespaceUri);
                CSharpModelEmitter.AppendSourceLine(builder, context, 6,
                    "case \"" + CSharpMetadataEmitter.EscapeCSharpString(
                        entry.NamespaceUri) + "\":");
                WriteAddDiagnosticLine(builder, context, 7, runtimeName,
                    "validation.structural.identifier_mismatch", "InvalidValue",
                    "iso20022.structure.version", "The model identifier does not match " +
                    "the generated namespace version.", "string.Empty",
                    "messageIdentifier", "\"" +
                    CSharpMetadataEmitter.EscapeCSharpString(entry.MessageIdentifier) +
                    "\"", entry.SourceIdentifier);
                CSharpModelEmitter.AppendSourceLine(builder, context, 7, "break;");
            }
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 6, "default:");
        WriteAddDiagnosticLine(builder, context, 7, runtimeName,
            "validation.structural.model_unknown", "Applicability",
            "iso20022.structure.model", "No generated structural metadata matches " +
            "the supplied model identity and namespace.", "string.Empty",
            "messageIdentifier + \"|\" + namespaceUri",
            "\"A registered generated model identity and namespace.\"",
            "string.Empty");
        CSharpModelEmitter.AppendSourceLine(builder, context, 7,
            "state.Report.Success = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 7,
            "state.Report.ErrorCode = \"validation.structural.model_unknown\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 7,
            "state.Report.ErrorMessage = " +
            "\"No generated structural metadata matches the supplied model.\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 7, "break;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 5, "}");
    }

    internal static void WriteAddDiagnosticLine(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string runtimeName,
        string code,
        string kind,
        string rule,
        string message,
        string path,
        string offending,
        string expected,
        string source)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            runtimeName + ".AddDiagnostic(state, \"" +
            CSharpMetadataEmitter.EscapeCSharpString(code) + "\", " +
            "global::iso20022.DiagnosticKind." + kind + ", \"" +
            CSharpMetadataEmitter.EscapeCSharpString(rule) + "\", \"" +
            CSharpMetadataEmitter.EscapeCSharpString(message) + "\", " + path +
            ", " + offending + ", " + expected + ", \"" +
            CSharpMetadataEmitter.EscapeCSharpString(source) + "\");");
    }

    internal static string QuoteExpression(string value)
    {
        return "\"" + CSharpMetadataEmitter.EscapeCSharpString(value) + "\"";
    }

    internal static bool ReferencedSymbolIsClosedCode(
        GenerationSymbolEntry symbol)
    {
        return symbol.Concept.CodeSet != null &&
            CSharpModelEmitter.CodeSetIsClosed(symbol.Concept.CodeSet);
    }

    internal static string FindRuntimeName(CSharpGenerationContext context)
    {
        return "global::" + context.Symbols.RootNamespace +
            ".Internal.Iso20022GeneratedStructuralRuntime";
    }

    internal static string FindStateName(CSharpGenerationContext context)
    {
        return "global::" + context.Symbols.RootNamespace +
            ".Internal.Iso20022GeneratedStructuralState";
    }
}
