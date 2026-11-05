namespace asn1;

internal class EcnDiagnosticContextEnricher
{
    internal AsnDiagnostic[] EnrichDiagnostics(
        AsnDiagnostic[] diagnostics,
        EcnSymbolTable? table,
        EcnModuleSyntax? module,
        string phase)
    {
        if (diagnostics == null)
        {
            throw new System.ArgumentNullException("diagnostics");
        }
        int index = 0;
        while (index < diagnostics.Length)
        {
            AsnDiagnostic diagnostic = diagnostics[index];
            if (diagnostic.Severity == AsnDiagnosticSeverity.Error)
            {
                this.EnrichDiagnostic(diagnostic, table, module, phase);
            }
            index = index + 1;
        }
        return diagnostics;
    }

    internal void EnrichDiagnostic(
        AsnDiagnostic diagnostic,
        EcnSymbolTable? table,
        EcnModuleSyntax? module,
        string phase)
    {
        if (diagnostic.Span == null && module != null)
        {
            diagnostic.Span = new EcnPublicPlanBuilder().CloneSpan(
                module.Span);
        }
        EcnExecutablePlanStep? step = this.FindPlanStep(
            table,
            diagnostic.Span);
        EcnSymbol? symbol = this.FindSymbol(table, diagnostic.Span);
        bool addDeclarationLocations =
            diagnostic.RelatedLocations.Length == 0;
        if (step != null)
        {
            diagnostic.PlanStepId = step.StableId;
            diagnostic.ValuePath = step.Path.Length == 0
                ? "$value"
                : step.Path;
        }
        else
        {
            if (diagnostic.PlanStepId.Length == 0)
            {
                diagnostic.PlanStepId = "ecn:" + phase;
            }
            if (diagnostic.ValuePath.Length == 0)
            {
                diagnostic.ValuePath = phase == "syntax"
                    ? "$syntax"
                    : "$value";
            }
        }
        if (symbol != null && addDeclarationLocations)
        {
            this.AddRelatedLocation(
                diagnostic,
                "The affected ECN declaration is here.",
                symbol.Span);
            if (symbol.GovernorSymbol != null)
            {
                this.AddRelatedLocation(
                    diagnostic,
                    "The governing ECN declaration is here.",
                    symbol.GovernorSymbol.Span);
            }
            if (symbol.AsnTypeAssignment != null)
            {
                this.AddRelatedLocation(
                    diagnostic,
                    "The affected ASN.1 type is declared here.",
                    symbol.AsnTypeAssignment.Span);
            }
        }
        if (step != null && table != null && addDeclarationLocations)
        {
            EcnExecutablePlan? plan = this.FindPlan(table, step.StableId);
            if (plan != null)
            {
                this.AddRelatedLocation(
                    diagnostic,
                    "The ECN encoding object is declared here.",
                    plan.Span);
            }
        }
    }

    internal EcnExecutablePlanStep? FindPlanStep(
        EcnSymbolTable? table,
        AsnSourceSpan? span)
    {
        if (table == null || span == null)
        {
            return null;
        }
        EcnExecutablePlanStep? best = null;
        int bestLength = int.MaxValue;
        int objectIndex = 0;
        while (objectIndex < table.EncodingObjects.Length)
        {
            EcnEncodingObjectDefinition? definition =
                table.EncodingObjects[objectIndex].ObjectDefinition;
            if (definition != null && definition.Plan != null)
            {
                int stepIndex = 0;
                while (stepIndex < definition.Plan.Steps.Length)
                {
                    EcnExecutablePlanStep candidate =
                        definition.Plan.Steps[stepIndex];
                    if (this.SpanContains(candidate.Trace.Span, span.Start))
                    {
                        int length = this.SpanLength(candidate.Trace.Span);
                        if (best == null || length < bestLength)
                        {
                            best = candidate;
                            bestLength = length;
                        }
                    }
                    stepIndex = stepIndex + 1;
                }
            }
            objectIndex = objectIndex + 1;
        }
        return best;
    }

    internal EcnSymbol? FindSymbol(
        EcnSymbolTable? table,
        AsnSourceSpan? span)
    {
        if (table == null || span == null)
        {
            return null;
        }
        EcnSymbol? best = null;
        int bestLength = int.MaxValue;
        int index = 0;
        while (index < table.LocalSymbols.Length)
        {
            EcnSymbol candidate = table.LocalSymbols[index];
            if (this.SpanContains(candidate.Span, span.Start))
            {
                int length = this.SpanLength(candidate.Span);
                if (best == null || length < bestLength)
                {
                    best = candidate;
                    bestLength = length;
                }
            }
            index = index + 1;
        }
        return best;
    }

    internal EcnExecutablePlan? FindPlan(
        EcnSymbolTable table,
        string stepId)
    {
        int index = 0;
        while (index < table.EncodingObjects.Length)
        {
            EcnEncodingObjectDefinition? definition =
                table.EncodingObjects[index].ObjectDefinition;
            if (definition != null && definition.Plan != null
                && stepId.StartsWith(
                    definition.Plan.StableId + "/step/",
                    System.StringComparison.Ordinal))
            {
                return definition.Plan;
            }
            index = index + 1;
        }
        return null;
    }

    internal bool SpanContains(
        AsnSourceSpan? span,
        AsnSourcePosition position)
    {
        return span != null
            && span.Start.SourceName == position.SourceName
            && span.Start.Offset <= position.Offset
            && span.End.Offset >= position.Offset;
    }

    internal int SpanLength(AsnSourceSpan? span)
    {
        return span == null
            ? int.MaxValue
            : span.End.Offset - span.Start.Offset;
    }

    internal void AddRelatedLocation(
        AsnDiagnostic diagnostic,
        string message,
        AsnSourceSpan? span)
    {
        if (span == null || this.SpansEqual(diagnostic.Span, span))
        {
            return;
        }
        int index = 0;
        while (index < diagnostic.RelatedLocations.Length)
        {
            if (diagnostic.RelatedLocations[index].Message == message
                && this.SpansEqual(
                    diagnostic.RelatedLocations[index].Span,
                    span))
            {
                return;
            }
            index = index + 1;
        }
        AsnDiagnosticRelatedLocation[] result =
            new AsnDiagnosticRelatedLocation[
                diagnostic.RelatedLocations.Length + 1];
        index = 0;
        while (index < diagnostic.RelatedLocations.Length)
        {
            result[index] = diagnostic.RelatedLocations[index];
            index = index + 1;
        }
        result[result.Length - 1] = new AsnDiagnosticRelatedLocation
        {
            Message = message,
            Span = new EcnPublicPlanBuilder().CloneSpan(span)
                ?? new AsnSourceSpan()
        };
        diagnostic.RelatedLocations = result;
    }

    internal bool SpansEqual(AsnSourceSpan? left, AsnSourceSpan? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }
        return left.Start.SourceName == right.Start.SourceName
            && left.Start.Offset == right.Start.Offset
            && left.End.SourceName == right.End.SourceName
            && left.End.Offset == right.End.Offset;
    }
}
