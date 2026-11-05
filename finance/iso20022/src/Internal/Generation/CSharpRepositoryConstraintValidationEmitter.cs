using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class CSharpRepositoryConstraintValidationEmitter
{
    internal static void WriteRuntimeMethods(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        string runtimeName = CSharpStructuralValidationEmitter.FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void AddUnsupportedConstraint(" +
            "Iso20022GeneratedStructuralState state, string ruleIdentifier, " +
            "string message, string path, string expectedConstraint, " +
            "string sourceIdentifier, int sourceOrdinal)");
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
            "\"Generated repository-constraint validation reached the diagnostic limit.\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Continue = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.ValidationDiagnostic diagnostic = " +
            "new global::iso20022.ValidationDiagnostic();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Code = \"validation.repository_constraint.unsupported\";");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.RuleIdentifier = ruleIdentifier;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Severity = global::iso20022.ValidationSeverity.Warning;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Stage = state.Stage;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Kind = global::iso20022.DiagnosticKind.UnsupportedConstraint;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Message = message;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.Path.Value = path;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.ExpectedConstraint = expectedConstraint;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.SourceIdentifier = sourceIdentifier;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "diagnostic.SourceArtifact = " + runtimeName +
            ".CreateSourceArtifact(sourceOrdinal);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.Diagnostics.Add(diagnostic);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "state.Report.WarningCount++;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (state.Policy.TreatWarningsAsErrors)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "state.Report.Valid = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "if (state.Policy.FailFast)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "state.Report.StoppedEarly = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "state.Continue = false;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteTypeValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        string stateName = CSharpStructuralValidationEmitter.FindStateName(context);
        string runtimeName = CSharpStructuralValidationEmitter.FindRuntimeName(context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static void ValidateRepositoryConstraints(" + typeName +
            " model, " + stateName + " state, string path, int depth)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!" + runtimeName + ".RecordModel(state, path, depth))");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "return;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        if (symbol.Concept.MessageDefinition != null)
        {
            WriteConstraintList(
                builder,
                context,
                FindRepositoryConstraints(context, symbol.Concept.SourceOrdinal),
                "path",
                FindRepositoryIdentifier(context, symbol.Concept.SourceOrdinal),
                symbol.Concept.SourceOrdinal,
                2);
        }
        WriteConstraintList(
            builder,
            context,
            FindConceptConstraints(symbol.Concept),
            "path",
            symbol.Concept.Identifier,
            symbol.Concept.SourceOrdinal,
            2);
        if (symbol.Concept.CodeSet != null &&
            CSharpModelEmitter.CodeSetIsClosed(symbol.Concept.CodeSet))
        {
            WriteCodeValueConstraints(builder, context, symbol, members);
        }
        else
        {
            int index = 0;
            while (index < members.Count)
            {
                CSharpMemberMetadata member = members[index];
                string memberPath = "path + \"/" +
                    CSharpMetadataEmitter.EscapeCSharpString(member.XmlName) + "\"";
                WriteConstraintList(
                    builder,
                    context,
                    member.Constraints,
                    memberPath,
                    member.SourceIdentifier,
                    symbol.Concept.SourceOrdinal,
                    2);
                CSharpPrimitiveValidationEmitter.WriteContainerMember(
                    builder,
                    context,
                    member,
                    "ValidateRepositoryConstraints",
                    index);
                index++;
            }
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static List<ConstraintDefinition> FindRepositoryConstraints(
        CSharpGenerationContext context,
        int sourceOrdinal)
    {
        int index = 0;
        while (index < context.Model.Sources.Count)
        {
            GenerationSource source = context.Model.Sources[index];
            if (source.Ordinal == sourceOrdinal && source.Repository != null)
            {
                return source.Repository.Constraints;
            }
            index++;
        }
        return new List<ConstraintDefinition>();
    }

    internal static string FindRepositoryIdentifier(
        CSharpGenerationContext context,
        int sourceOrdinal)
    {
        int index = 0;
        while (index < context.Model.Sources.Count)
        {
            GenerationSource source = context.Model.Sources[index];
            if (source.Ordinal == sourceOrdinal)
            {
                return source.Identifier;
            }
            index++;
        }
        return string.Empty;
    }

    internal static void WriteCodeValueConstraints(
        StringBuilder builder,
        CSharpGenerationContext context,
        GenerationSymbolEntry symbol,
        List<CSharpMemberMetadata> members)
    {
        string typeName = CSharpMetadataEmitter.FindQualifiedSymbolName(symbol);
        int index = 0;
        while (index < members.Count)
        {
            CSharpMemberMetadata member = members[index];
            if (member.Constraints.Count > 0)
            {
                CSharpModelEmitter.AppendSourceLine(builder, context, 2,
                    "if (state.Continue && model == " + typeName + "." +
                    member.Name + ")");
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
                WriteConstraintList(
                    builder,
                    context,
                    member.Constraints,
                    "path",
                    member.SourceIdentifier,
                    symbol.Concept.SourceOrdinal,
                    3);
                CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
            }
            index++;
        }
    }

    internal static List<ConstraintDefinition> FindConceptConstraints(
        GenerationConcept concept)
    {
        List<ConstraintDefinition> constraints = new List<ConstraintDefinition>();
        if (concept.MessageDefinition != null)
        {
            constraints.AddRange(concept.MessageDefinition.Registration.Constraints);
        }
        else if (concept.MessageComponent != null)
        {
            constraints.AddRange(concept.MessageComponent.Registration.Constraints);
        }
        else if (concept.Choice != null)
        {
            constraints.AddRange(concept.Choice.Registration.Constraints);
        }
        else if (concept.DataType != null)
        {
            constraints.AddRange(concept.DataType.Registration.Constraints);
        }
        else if (concept.CodeSet != null)
        {
            constraints.AddRange(concept.CodeSet.Registration.Constraints);
        }
        else if (concept.ExternalSchema != null)
        {
            constraints.AddRange(concept.ExternalSchema.Registration.Constraints);
        }
        return constraints;
    }

    internal static void WriteConstraintList(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<ConstraintDefinition> constraints,
        string path,
        string ownerIdentifier,
        int sourceOrdinal,
        int level)
    {
        int index = 0;
        while (index < constraints.Count)
        {
            ConstraintDefinition constraint = constraints[index];
            string identifier = FindConstraintIdentifier(
                constraint,
                ownerIdentifier,
                index);
            string expected = FindExpectedConstraint(constraint);
            string message = FindConstraintMessage(constraint);
            CSharpModelEmitter.AppendSourceLine(builder, context, level,
                "if (state.Continue)");
            CSharpModelEmitter.AppendSourceLine(builder, context, level, "{");
            CSharpModelEmitter.AppendSourceLine(builder, context, level + 1,
                CSharpStructuralValidationEmitter.FindRuntimeName(context) +
                ".AddUnsupportedConstraint(state, " +
                CSharpStructuralValidationEmitter.QuoteExpression(identifier) + ", " +
                CSharpStructuralValidationEmitter.QuoteExpression(message) + ", " +
                path + ", " +
                CSharpStructuralValidationEmitter.QuoteExpression(expected) + ", " +
                CSharpStructuralValidationEmitter.QuoteExpression(identifier) + ", " +
                sourceOrdinal.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + ");");
            CSharpModelEmitter.AppendSourceLine(builder, context, level, "}");
            index++;
        }
    }

    internal static string FindConstraintIdentifier(
        ConstraintDefinition constraint,
        string ownerIdentifier,
        int index)
    {
        if (constraint.Identifier.Length > 0)
        {
            return constraint.Identifier;
        }
        string name = constraint.Name;
        if (name.Length == 0)
        {
            name = "constraint";
        }
        return ownerIdentifier + "." + name + "." + index.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string FindExpectedConstraint(ConstraintDefinition constraint)
    {
        if (constraint.Expression.Length > 0)
        {
            return constraint.Expression;
        }
        if (constraint.Definition.Length > 0)
        {
            return constraint.Definition;
        }
        return constraint.Name;
    }

    internal static string FindConstraintMessage(ConstraintDefinition constraint)
    {
        string name = constraint.Name;
        if (name.Length == 0)
        {
            name = constraint.Identifier;
        }
        if (name.Length == 0)
        {
            name = "Unnamed repository constraint";
        }
        if (constraint.ExpressionLanguage.Length > 0)
        {
            return "Repository constraint '" + name + "' uses " +
                constraint.ExpressionLanguage +
                " and is applicable but cannot be executed automatically.";
        }
        return "Repository constraint '" + name +
            "' is applicable but cannot be executed automatically.";
    }

    internal static void WriteRegistryValidator(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        CSharpPrimitiveValidationEmitter.WriteRegistryValidator(
            builder,
            context,
            entries,
            "ValidateMessageRepositoryConstraints",
            "ValidateRepositoryConstraints",
            "RepositoryConstraint");
    }
}
