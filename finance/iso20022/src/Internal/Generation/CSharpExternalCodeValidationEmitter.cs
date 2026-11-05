using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class CSharpExternalCodeValidationEmitter
{
    internal static void WriteRuntimeMethods(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        WriteValidateExternalCodeValue(builder, context);
        WriteApplyExternalCodeEdition(builder, context);
        WriteApplyExternalLookupArtifact(builder, context);
        WriteSetExternalOperationFailure(builder, context);
    }

    internal static void WriteValidateExternalCodeValue(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateExternalCodeValue(" +
            "Iso20022GeneratedStructuralState state, " +
            "global::iso20022.IsoCodeValue? value, string codeSetIdentifier, " +
            "string path, int sourceOrdinal)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!state.Continue)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (value == null || value.Value == null || value.Value.Length == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteRuntimeDiagnostic(builder, context, 3,
            "validation.external_code.value_missing", "InvalidValue",
            "The generated external-code value is missing.", "string.Empty",
            "One non-empty external code value.");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "ApplySourceArtifact(state, sourceOrdinal, codeSetIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (state.CodeSetProvider == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteRuntimeDiagnostic(builder, context, 3,
            "validation.external_code.provider_missing", "Configuration",
            "External-code validation requires a code-set provider.",
            "string.Empty", "One configured code-set provider.");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "ApplySourceArtifact(state, sourceOrdinal, codeSetIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "SetExternalOperationFailure(state, " +
            "\"validation.external_code.provider_missing\", " +
            "\"External-code validation requires a code-set provider.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.CodeLookupRequest request = " +
            "new global::iso20022.CodeLookupRequest();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "request.Kind = global::iso20022.CodeSetKind.External;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "request.CodeSetIdentifier = codeSetIdentifier;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "request.Value = value.Value;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "request.Edition = state.Policy.ExternalCodeEdition;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "request.SelectLatestEdition = state.Policy.SelectLatestExternalCodeEdition;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "request.ActiveOn = state.ActiveOn;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "request.IncludeInactive = state.Policy.IncludeInactiveExternalCodes;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.CodeLookupResult? result = " +
            "state.CodeSetProvider.LookupCode(request);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (result == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteRuntimeDiagnostic(builder, context, 3,
            "validation.external_code.lookup_invalid", "Configuration",
            "The code-set provider returned no lookup result.",
            "string.Empty", "One exception-less code lookup result.");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "ApplySourceArtifact(state, sourceOrdinal, codeSetIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "SetExternalOperationFailure(state, " +
            "\"validation.external_code.lookup_invalid\", " +
            "\"The code-set provider returned no lookup result.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!result.Success)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteRuntimeDiagnostic(builder, context, 3,
            "validation.external_code.lookup_failed", "Configuration",
            "The external-code lookup could not be evaluated.",
            "result.ErrorCode", "A successful external-code lookup.");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "ApplyExternalLookupArtifact(state, result, codeSetIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "SetExternalOperationFailure(state, result.ErrorCode, result.ErrorMessage);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "ApplyExternalCodeEdition(state, result, codeSetIdentifier, path);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!state.Continue)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!result.Found)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteRuntimeDiagnostic(builder, context, 3,
            "validation.external_code.not_found", "InvalidValue",
            "The external code is not registered or active under the selected policy.",
            "value.Value",
            "A registered external code active in the selected edition.");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "ApplyExternalLookupArtifact(state, result, codeSetIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRuntimeDiagnostic(
        StringBuilder builder,
        CSharpGenerationContext context,
        int level,
        string code,
        string kind,
        string message,
        string offending,
        string expected)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, level,
            "AddDiagnostic(state, \"" +
            CSharpMetadataEmitter.EscapeCSharpString(code) + "\", " +
            "global::iso20022.DiagnosticKind." + kind + ", " +
            "\"iso20022.external_code\", \"" +
            CSharpMetadataEmitter.EscapeCSharpString(message) + "\", path, " +
            offending + ", " +
            CSharpStructuralValidationEmitter.QuoteExpression(expected) +
            ", codeSetIdentifier);");
    }

    internal static void WriteApplyExternalCodeEdition(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ApplyExternalCodeEdition(" +
            "Iso20022GeneratedStructuralState state, " +
            "global::iso20022.CodeLookupResult result, " +
            "string codeSetIdentifier, string path)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (result.Edition == null || result.Edition.Length == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteRuntimeDiagnostic(builder, context, 3,
            "validation.external_code.edition_unavailable", "Configuration",
            "The selected external-code edition is unavailable.",
            "state.Policy.ExternalCodeEdition",
            "One available pinned or latest external-code edition.");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "ApplyExternalLookupArtifact(state, result, codeSetIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "SetExternalOperationFailure(state, " +
            "\"validation.external_code.edition_unavailable\", " +
            "\"The selected external-code edition is unavailable.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (state.Report.ExternalCodeEdition.Length == 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.ExternalCodeEdition = result.Edition;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "else if (state.Report.ExternalCodeEdition != result.Edition)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        WriteRuntimeDiagnostic(builder, context, 3,
            "validation.external_code.edition_conflict", "Conflict",
            "External-code lookups selected inconsistent catalogue editions.",
            "result.Edition", "One consistent external-code edition.");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "ApplyExternalLookupArtifact(state, result, codeSetIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "SetExternalOperationFailure(state, " +
            "\"validation.external_code.edition_conflict\", " +
            "\"External-code lookups selected inconsistent catalogue editions.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteApplyExternalLookupArtifact(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ApplyExternalLookupArtifact(" +
            "Iso20022GeneratedStructuralState state, " +
            "global::iso20022.CodeLookupResult result, string sourceIdentifier)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (state.Report.Diagnostics.Count == 0 || result.Artifact == null)");
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
            "diagnostic.SourceArtifact = result.Artifact;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteSetExternalOperationFailure(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void SetExternalOperationFailure(" +
            "Iso20022GeneratedStructuralState state, string code, string message)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.Success = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.Valid = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.ErrorCode = code;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.ErrorMessage = message;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.StoppedEarly = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Continue = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteTypeValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        if (symbol.Concept.MessageDefinition != null ||
            symbol.Concept.MessageComponent != null ||
            symbol.Concept.Choice != null)
        {
            CSharpPrimitiveValidationEmitter.WriteContainerValidator(
                builder, context, symbol, members, "ValidateExternalCodes");
            return;
        }
        if (symbol.Concept.CodeSet != null &&
            symbol.Concept.CodeSet.Kind == CodeSetKind.External)
        {
            WriteExternalCodeValidator(builder, context, symbol);
            return;
        }
        CSharpPrimitiveValidationEmitter.WritePassValidator(
            builder, context, symbol, "ValidateExternalCodes");
    }

    internal static void WriteExternalCodeValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = CSharpStructuralValidationEmitter.FindStateName(context);
        string runtimeName = CSharpStructuralValidationEmitter.FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateExternalCodes(" + typeName + " model, " +
            stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!" + runtimeName + ".RecordModel(state, path, depth))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            runtimeName + ".ValidateExternalCodeValue(state, model.Value, " +
            CSharpStructuralValidationEmitter.QuoteExpression(
                symbol.Concept.Identifier) + ", path, " +
            symbol.Concept.SourceOrdinal.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteRegistryValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        string stateName = CSharpStructuralValidationEmitter.FindStateName(context);
        string runtimeName = CSharpStructuralValidationEmitter.FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static global::iso20022.ValidationReport " +
            "ValidateMessageExternalCodes(object? model, string? messageIdentifier, " +
            "string? namespaceUri, global::iso20022.ValidationPolicy? policy, " +
            "global::iso20022.ICodeSetProvider? provider, " +
            "global::iso20022.IsoDateValue? activeOn)");
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
            "global::iso20022.ValidationStage.ExternalCode);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.CodeSetProvider = provider;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.ActiveOn = activeOn;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "switch (messageIdentifier + \"\\n\" + namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < entries.Count)
        {
            CSharpPrimitiveValidationEmitter.WriteRegistryCase(
                builder, context, entries[index], "ValidateExternalCodes", runtimeName);
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
}
