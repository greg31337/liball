using System.Collections.Generic;

namespace iso20022;

internal static class ProfileOverlayValidator
{
    internal static void Validate(
        ValidationPipelineState state,
        ProfileOverlayResolution resolution)
    {
        BuiltInSemanticIndex index = BuiltInSemanticRuleEvaluator.BuildIndex(
            state.Request);
        if (!index.Continue)
        {
            AddIndexDiagnostics(state, index.Diagnostics);
            return;
        }
        if (resolution.Constraints.Count > 0 && index.Elements.Count == 0)
        {
            ValidationDiagnostic diagnostic = MessageValidator.CreateDiagnostic(
                "validation.profile.representation_unsupported",
                "iso20022.profile.overlay",
                DiagnosticKind.UnsupportedConstraint,
                "Profile path restrictions require a materialized generic XML representation.",
                string.Empty,
                "A generic message document containing the selected paths.",
                state.Request);
            diagnostic.Severity = ValidationSeverity.Warning;
            AddSelectedProfileProvenance(
                diagnostic,
                state.Request.Profile);
            state.AddDiagnostic(diagnostic);
            return;
        }
        int selectionIndex = 0;
        while (selectionIndex < resolution.Constraints.Count && state.Continue)
        {
            ProfileConstraintSelection selection =
                resolution.Constraints[selectionIndex];
            ValidateConstraint(state, index, selection);
            selectionIndex++;
        }
    }

    internal static void AddIndexDiagnostics(
        ValidationPipelineState state,
        List<ValidationDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count && state.Continue)
        {
            ValidationDiagnostic diagnostic = diagnostics[index];
            diagnostic.Code = "validation.profile.resource_limit";
            diagnostic.RuleIdentifier = "iso20022.profile.overlay";
            diagnostic.Kind = DiagnosticKind.ResourceLimit;
            AddSelectedProfileProvenance(
                diagnostic,
                state.Request.Profile);
            state.AddDiagnostic(diagnostic);
            index++;
        }
    }

    internal static void ValidateConstraint(
        ValidationPipelineState state,
        BuiltInSemanticIndex index,
        ProfileConstraintSelection selection)
    {
        ProfileConstraint constraint = selection.Constraint;
        List<SemanticXmlElement> matches = FindMatches(index, constraint.Path);
        int minimum = 0;
        bool hasMinimum = constraint.HasMinimumOccurrences;
        if (constraint.RequirePath && matches.Count == 0)
        {
            AddConstraintDiagnostic(
                state,
                selection,
                null,
                "validation.profile.path_required",
                DiagnosticKind.MissingContent,
                "0",
                "At least one occurrence.",
                "The profile requires this message path.");
        }
        if (!state.Continue)
        {
            return;
        }
        if (constraint.RequirePath && !hasMinimum)
        {
            hasMinimum = true;
            minimum = 1;
        }
        else if (hasMinimum)
        {
            minimum = constraint.MinimumOccurrences;
        }
        if (hasMinimum && matches.Count < minimum && matches.Count > 0)
        {
            AddConstraintDiagnostic(
                state,
                selection,
                null,
                "validation.profile.minimum_occurs",
                DiagnosticKind.Cardinality,
                matches.Count.ToString(),
                "At least " + minimum.ToString() + " occurrence(s).",
                "The profile path occurs fewer times than permitted.");
        }
        if (!state.Continue)
        {
            return;
        }
        if (constraint.ProhibitPath && matches.Count > 0)
        {
            AddConstraintDiagnostic(
                state,
                selection,
                matches[0],
                "validation.profile.path_prohibited",
                DiagnosticKind.UnexpectedContent,
                matches.Count.ToString(),
                "No occurrences.",
                "The profile prohibits this message path.");
        }
        if (!state.Continue)
        {
            return;
        }
        if (constraint.HasMaximumOccurrences &&
            matches.Count > constraint.MaximumOccurrences)
        {
            AddConstraintDiagnostic(
                state,
                selection,
                matches[constraint.MaximumOccurrences],
                "validation.profile.maximum_occurs",
                DiagnosticKind.Cardinality,
                matches.Count.ToString(),
                "At most " + constraint.MaximumOccurrences.ToString() +
                    " occurrence(s).",
                "The profile path occurs more times than permitted.");
        }
        int matchIndex = 0;
        while (matchIndex < matches.Count && state.Continue)
        {
            SemanticXmlElement match = matches[matchIndex];
            string value = NamespaceService.TrimXmlWhitespace(
                BuiltInSemanticRuleEvaluator.CollectElementText(match.Node));
            bool prohibited = ProfileOverlayResolver.TextListContains(
                constraint.ProhibitedValues,
                value);
            if (prohibited)
            {
                AddConstraintDiagnostic(
                    state,
                    selection,
                    match,
                    "validation.profile.value_prohibited",
                    DiagnosticKind.InvalidValue,
                    value,
                    "A value not prohibited by the profile.",
                    "The message value is prohibited by the selected profile.");
            }
            else if (constraint.AllowedValues.Count > 0 &&
                !ProfileOverlayResolver.TextListContains(
                    constraint.AllowedValues,
                    value))
            {
                AddConstraintDiagnostic(
                    state,
                    selection,
                    match,
                    "validation.profile.value_not_allowed",
                    DiagnosticKind.InvalidValue,
                    value,
                    "A value in the profile allowed-value set.",
                    "The message value is not allowed by the selected profile.");
            }
            matchIndex++;
        }
    }

    internal static List<SemanticXmlElement> FindMatches(
        BuiltInSemanticIndex index,
        XmlPathData path)
    {
        List<SemanticXmlElement> matches = new List<SemanticXmlElement>();
        int indexValue = 0;
        while (indexValue < index.Elements.Count)
        {
            SemanticXmlElement element = index.Elements[indexValue];
            if (PathMatches(element, path))
            {
                matches.Add(element);
            }
            indexValue++;
        }
        return matches;
    }

    internal static bool PathMatches(
        SemanticXmlElement element,
        XmlPathData path)
    {
        if (path.Segments.Count == 0)
        {
            return path.Value == element.Path;
        }
        int segmentIndex = path.Segments.Count - 1;
        SemanticXmlElement? current = element;
        while (segmentIndex >= 0 && current != null)
        {
            QualifiedNameData segment = path.Segments[segmentIndex];
            if (segment.LocalName != current.Node.Name.LocalName ||
                (segment.NamespaceUri.Length > 0 &&
                segment.NamespaceUri != current.Node.Name.NamespaceUri))
            {
                return false;
            }
            current = current.Parent;
            segmentIndex--;
        }
        return segmentIndex < 0 && current == null;
    }

    internal static void AddConstraintDiagnostic(
        ValidationPipelineState state,
        ProfileConstraintSelection selection,
        SemanticXmlElement? element,
        string code,
        DiagnosticKind kind,
        string offendingValue,
        string expectedConstraint,
        string message)
    {
        ValidationDiagnostic diagnostic = MessageValidator.CreateDiagnostic(
            code,
            selection.Constraint.Identifier,
            kind,
            message,
            offendingValue,
            expectedConstraint,
            state.Request);
        diagnostic.Severity = selection.Constraint.Severity;
        diagnostic.Path = selection.Constraint.Path;
        if (element != null)
        {
            diagnostic.Location = element.Node.Location;
        }
        diagnostic.SourceIdentifier = selection.Constraint.BaseConstraintIdentifier;
        AddSelectedProfileProvenance(diagnostic, selection.Profile);
        state.AddDiagnostic(diagnostic);
    }

    internal static void AddSelectedProfileProvenance(
        ValidationDiagnostic diagnostic,
        ProfileDefinition? profile)
    {
        if (profile == null)
        {
            return;
        }
        diagnostic.ProfileIdentifier = profile.Identifier;
        diagnostic.ProfileVersion = profile.Version;
        diagnostic.SourceArtifact = profile.Artifact;
    }
}
