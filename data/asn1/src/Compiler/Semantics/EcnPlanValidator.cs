namespace asn1;

internal class EcnPlanValidator
{
    internal DiagnosticBag Diagnostics;
    internal EcnEncodingObjectDefinition[] Definitions = [];
    internal int[] RecursionStates = [];
    internal bool Success = true;

    internal EcnPlanValidator(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool ValidateTables(EcnSymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Success = true;
        this.Definitions = this.CollectDefinitions(tables);
        this.RecursionStates = new int[this.Definitions.Length];
        int index = 0;
        while (index < this.Definitions.Length)
        {
            EcnEncodingObjectDefinition definition = this.Definitions[index];
            if (definition.Plan != null)
            {
                this.ValidateDefinitionPlan(definition);
            }
            index = index + 1;
        }

        index = 0;
        while (index < this.Definitions.Length)
        {
            if (this.RecursionStates[index] == 0)
            {
                this.VisitObjectLinks(index);
            }
            index = index + 1;
        }
        return this.Success;
    }

    internal bool ValidateDefinitionPlan(EcnEncodingObjectDefinition definition)
    {
        if (definition == null)
        {
            throw new System.ArgumentNullException("definition");
        }
        EcnExecutablePlan? plan = definition.Plan;
        if (plan == null)
        {
            return false;
        }

        plan.ValidationIssues = [];
        plan.Valid = true;
        plan.Deterministic = true;
        plan.Decodable = true;
        plan.Complete = true;
        int index = 0;
        while (index < plan.Steps.Length)
        {
            plan.Steps[index].Valid = true;
            index = index + 1;
        }

        this.ValidatePlanShape(definition, plan);
        this.ValidateStepDependencies(definition, plan);
        this.ValidateFieldCoverage(definition, plan);
        this.ValidateLegalSteps(definition, plan);
        this.ValidateConditions(definition, plan);
        this.ValidateDeterminants(definition, plan);
        this.ValidateAllBounds(definition, plan);
        this.ValidateAlternatives(definition, plan);
        return plan.Valid;
    }

    internal EcnEncodingObjectDefinition[] CollectDefinitions(
        EcnSymbolTable[] tables)
    {
        System.Collections.Generic.List<EcnEncodingObjectDefinition> result =
            new System.Collections.Generic.List<EcnEncodingObjectDefinition>();
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].EncodingObjects.Length)
            {
                EcnSymbol symbol = tables[tableIndex].EncodingObjects[symbolIndex];
                if (!symbol.Imported && symbol.ObjectDefinition != null)
                {
                    result.Add(symbol.ObjectDefinition);
                }
                symbolIndex = symbolIndex + 1;
            }
            tableIndex = tableIndex + 1;
        }
        return result.ToArray();
    }

    internal void ValidatePlanShape(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan)
    {
        if (plan.Root == null || plan.Steps.Length == 0
            || plan.Root != plan.Steps[0])
        {
            this.ReportIssue(
                definition,
                plan,
                null,
                EcnDiagnosticCodes.InvalidPlan,
                "The executable ECN plan must have its root as the first step.",
                plan.Span);
            return;
        }

        int index = 0;
        while (index < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            if (step.StableId.Length == 0 || step.Order != index)
            {
                this.ReportIssue(
                    definition,
                    plan,
                    step,
                    EcnDiagnosticCodes.InvalidPlan,
                    "ECN plan steps must have stable identifiers and contiguous deterministic order.",
                    step.Trace.Span);
            }
            int earlier = 0;
            while (earlier < index)
            {
                if (plan.Steps[earlier].StableId == step.StableId)
                {
                    this.ReportIssue(
                        definition,
                        plan,
                        step,
                        EcnDiagnosticCodes.NonDeterministicPlan,
                        "ECN plan step identifiers must be unique.",
                        step.Trace.Span);
                }
                earlier = earlier + 1;
            }
            index = index + 1;
        }
    }

    internal void ValidateStepDependencies(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan)
    {
        int stepIndex = 0;
        while (stepIndex < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[stepIndex];
            int dependencyIndex = 0;
            while (dependencyIndex < step.Dependencies.Length)
            {
                EcnPlanDependency dependency = step.Dependencies[dependencyIndex];
                if (dependency.SourceStepId.Length > 0)
                {
                    int sourceIndex = this.FindStepIndex(
                        plan,
                        dependency.SourceStepId);
                    if (sourceIndex < 0)
                    {
                        this.ReportIssue(
                            definition,
                            plan,
                            step,
                            EcnDiagnosticCodes.InvalidPlan,
                            "An ECN plan dependency refers to a missing step.",
                            dependency.Span);
                    }
                    else if (dependency.MustPrecede && sourceIndex >= stepIndex)
                    {
                        this.ReportIssue(
                            definition,
                            plan,
                            step,
                            EcnDiagnosticCodes.NonDeterministicPlan,
                            "An ECN plan dependency does not precede the step that consumes it.",
                            dependency.Span);
                    }
                }
                dependencyIndex = dependencyIndex + 1;
            }
            stepIndex = stepIndex + 1;
        }

        stepIndex = 0;
        while (stepIndex < plan.Steps.Length)
        {
            if (this.DependencyReaches(plan, stepIndex, stepIndex))
            {
                this.ReportIssue(
                    definition,
                    plan,
                    plan.Steps[stepIndex],
                    EcnDiagnosticCodes.PlanRecursion,
                    "The ECN plan contains a recursive step dependency.",
                    plan.Steps[stepIndex].Trace.Span);
            }
            stepIndex = stepIndex + 1;
        }

        stepIndex = 0;
        while (stepIndex < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[stepIndex];
            if (step.Kind == EcnPlanStepKind.OpenType
                && !this.HasDependencyKind(
                    step,
                    EcnPlanDependencyKind.EncodingObjectSet))
            {
                this.ReportIssue(
                    definition,
                    plan,
                    step,
                    EcnDiagnosticCodes.NonDecodablePlan,
                    "An open-type plan step requires an encoding object set for decoding.",
                    step.Trace.Span);
            }
            if (step.Kind == EcnPlanStepKind.Transformation
                && step.Encodes && !step.Decodes && !step.Reversible)
            {
                this.ReportIssue(
                    definition,
                    plan,
                    step,
                    EcnDiagnosticCodes.NonDecodablePlan,
                    "An encode-only transformation must be reversible to support decoding.",
                    step.Trace.Span);
            }
            stepIndex = stepIndex + 1;
        }

        this.ValidateWriterConflicts(definition, plan);
    }

    internal void ValidateWriterConflicts(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan)
    {
        int index = 0;
        while (index < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            string writePath = this.WriterPath(step);
            if (writePath.Length > 0)
            {
                int earlier = 0;
                while (earlier < index)
                {
                    string previousPath = this.WriterPath(plan.Steps[earlier]);
                    if (previousPath.Length > 0 && previousPath == writePath)
                    {
                        this.ReportIssue(
                            definition,
                            plan,
                            step,
                            EcnDiagnosticCodes.NonDeterministicPlan,
                            "Multiple ECN plan steps write the same field or determinant.",
                            step.Trace.Span);
                    }
                    earlier = earlier + 1;
                }
            }
            index = index + 1;
        }
    }

    internal string WriterPath(EcnExecutablePlanStep step)
    {
        if (step.Kind == EcnPlanStepKind.DeterminantWrite)
        {
            EcnPlanDependency? determinant = this.FindDeterminantDependency(step);
            return determinant == null ? "" : determinant.Path;
        }
        if (step.Trace.SemanticKind == "link")
        {
            return step.Path;
        }
        return "";
    }

    internal bool DependencyReaches(
        EcnExecutablePlan plan,
        int originIndex,
        int targetIndex)
    {
        bool[] visited = new bool[plan.Steps.Length];
        System.Collections.Generic.List<int> pending =
            new System.Collections.Generic.List<int>();
        pending.Add(originIndex);
        while (pending.Count > 0)
        {
            int pendingIndex = pending.Count - 1;
            int current = pending[pendingIndex];
            pending.RemoveAt(pendingIndex);
            EcnPlanDependency[] dependencies = plan.Steps[current].Dependencies;
            int dependencyIndex = 0;
            while (dependencyIndex < dependencies.Length)
            {
                string sourceId = dependencies[dependencyIndex].SourceStepId;
                if (sourceId.Length > 0)
                {
                    int source = this.FindStepIndex(plan, sourceId);
                    if (source == targetIndex)
                    {
                        return true;
                    }
                    if (source >= 0 && !visited[source])
                    {
                        visited[source] = true;
                        pending.Add(source);
                    }
                }
                dependencyIndex = dependencyIndex + 1;
            }
        }
        return false;
    }

    internal int FindStepIndex(EcnExecutablePlan plan, string stableId)
    {
        int index = 0;
        while (index < plan.Steps.Length)
        {
            if (plan.Steps[index].StableId == stableId)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal bool HasDependencyKind(
        EcnExecutablePlanStep step,
        EcnPlanDependencyKind kind)
    {
        int index = 0;
        while (index < step.Dependencies.Length)
        {
            if (step.Dependencies[index].Kind == kind
                && (kind != EcnPlanDependencyKind.EncodingObjectSet
                    || step.Dependencies[index].StableTargetId.Length > 0))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ValidateFieldCoverage(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan)
    {
        EcnEncodingField[] fields = definition.Constructor == null
            ? []
            : definition.Constructor.Fields;
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            int count = 0;
            int stepIndex = 0;
            while (stepIndex < plan.Steps.Length)
            {
                EcnExecutablePlanStep step = plan.Steps[stepIndex];
                if (step.Kind == EcnPlanStepKind.Field
                    && step.Trace.SemanticKind == "field"
                    && step.Name == fields[fieldIndex].Name)
                {
                    count = count + 1;
                }
                stepIndex = stepIndex + 1;
            }
            if (count != 1)
            {
                this.ReportIssue(
                    definition,
                    plan,
                    null,
                    EcnDiagnosticCodes.IncompleteFieldCoverage,
                    "ECN field '" + fields[fieldIndex].Name
                        + "' must occur exactly once in the executable plan.",
                    fields[fieldIndex].Span);
            }
            fieldIndex = fieldIndex + 1;
        }


        int planFieldIndex = 0;
        while (planFieldIndex < plan.Steps.Length)
        {
            EcnExecutablePlanStep planField = plan.Steps[planFieldIndex];
            if (planField.Kind == EcnPlanStepKind.Field
                && planField.Trace.SemanticKind == "field"
                && !this.ConstructorHasField(fields, planField.Name))
            {
                this.ReportIssue(
                    definition,
                    plan,
                    planField,
                    EcnDiagnosticCodes.IncompleteFieldCoverage,
                    "The ECN plan contains a field not present in its constructor.",
                    planField.Trace.Span);
            }
            planFieldIndex = planFieldIndex + 1;
        }
    }

    internal bool ConstructorHasField(
        EcnEncodingField[] fields,
        string name)
    {
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Name == name)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ValidateLegalSteps(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan)
    {
        if (plan.Root != null)
        {
            EcnPlanStepKind expected = this.ExpectedRootKind(definition);
            if (plan.Root.Kind != expected)
            {
                this.ReportIssue(
                    definition,
                    plan,
                    plan.Root,
                    EcnDiagnosticCodes.IllegalPlanStep,
                    "The ECN plan root is incompatible with its encoding class category.",
                    plan.Root.Trace.Span);
            }
        }

        EcnClassCategory category = definition.GovernorSymbol == null
            ? EcnClassCategory.Unknown
            : definition.GovernorSymbol.ClassCategory;
        int propertyIndex = 0;
        while (propertyIndex < definition.Properties.Length)
        {
            EcnEncodingProperty property = definition.Properties[propertyIndex];
            if (!EcnPropertyApplicability.IsAllowed(property.Kind, category))
            {
                this.ReportIssue(
                    definition,
                    plan,
                    this.FindSemanticStep(plan, "property", property.Name),
                    EcnDiagnosticCodes.IllegalPlanStep,
                    "ECN property '" + property.Name
                        + "' is not legal for this plan category.",
                    property.Span);
            }
            propertyIndex = propertyIndex + 1;
        }

        int stepIndex = 0;
        while (stepIndex < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[stepIndex];
            if (step.Kind == EcnPlanStepKind.OpenType
                && category != EcnClassCategory.Unknown
                && category != EcnClassCategory.OpenType)
            {
                this.ReportIssue(
                    definition,
                    plan,
                    step,
                    EcnDiagnosticCodes.IllegalPlanStep,
                    "An open-type step is legal only for an open-type encoding class.",
                    step.Trace.Span);
            }
            if (step.Kind == EcnPlanStepKind.Replacement
                && (definition.Conversion == null
                    || definition.Conversion.Replacements.Length == 0))
            {
                this.ReportIssue(
                    definition,
                    plan,
                    step,
                    EcnDiagnosticCodes.IllegalPlanStep,
                    "A replacement step requires a compiled replacement definition.",
                    step.Trace.Span);
            }
            if (step.Kind == EcnPlanStepKind.StandardEncoding
                && !this.HasDependencyKind(
                    step,
                    EcnPlanDependencyKind.EncodingObjectSet))
            {
                this.ReportIssue(
                    definition,
                    plan,
                    step,
                    EcnDiagnosticCodes.IllegalPlanStep,
                    "A standard-encoding step requires a stable encoding object set target.",
                    step.Trace.Span);
            }
            stepIndex = stepIndex + 1;
        }
    }

    internal EcnPlanStepKind ExpectedRootKind(
        EcnEncodingObjectDefinition definition)
    {
        EcnConstructorKind kind = definition.Constructor == null
            ? EcnConstructorKind.Unknown
            : definition.Constructor.Kind;
        switch (kind)
        {
            case EcnConstructorKind.Concatenation:
                return EcnPlanStepKind.Sequence;
            case EcnConstructorKind.Alternatives:
                return EcnPlanStepKind.Choice;
            case EcnConstructorKind.Repetition:
            case EcnConstructorKind.ConditionalRepetition:
                return EcnPlanStepKind.Repetition;
            case EcnConstructorKind.Optionality:
                return EcnPlanStepKind.Optional;
            case EcnConstructorKind.Tag:
                return EcnPlanStepKind.Tag;
            default:
                return EcnPlanStepKind.Primitive;
        }
    }

    internal EcnExecutablePlanStep? FindSemanticStep(
        EcnExecutablePlan plan,
        string semanticKind,
        string semanticName)
    {
        int index = 0;
        while (index < plan.Steps.Length)
        {
            if (plan.Steps[index].Trace.SemanticKind == semanticKind
                && plan.Steps[index].Trace.SemanticName == semanticName)
            {
                return plan.Steps[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal void ValidateConditions(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan)
    {
        EcnConditionDefinition[] conditions = definition.Constructor == null
            ? []
            : definition.Constructor.Conditions;
        int index = 0;
        while (index < conditions.Length)
        {
            EcnConditionDefinition condition = conditions[index];
            string expression = condition.Expression.Trim();
            if (expression.Length == 0
                || string.Equals(
                    expression,
                    "FALSE",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                this.ReportIssue(
                    definition,
                    plan,
                    this.FindSemanticStep(plan, "condition", condition.Name),
                    EcnDiagnosticCodes.UnsatisfiableCondition,
                    "The ECN condition can never select an executable path.",
                    condition.Span);
            }
            int earlier = 0;
            while (earlier < index)
            {
                EcnConditionDefinition previous = conditions[earlier];
                if (string.Equals(
                    previous.Expression.Trim(),
                    expression,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    this.ReportIssue(
                        definition,
                        plan,
                        this.FindSemanticStep(plan, "condition", condition.Name),
                        EcnDiagnosticCodes.AmbiguousAlternative,
                        "Equivalent ECN conditions make alternative selection ambiguous.",
                        condition.Span);
                }
                if (condition.Reference.Length > 0
                    && condition.Reference == previous.Reference
                    && condition.Comparison == "="
                    && previous.Comparison == "="
                    && condition.Value.Length > 0
                    && previous.Value.Length > 0
                    && condition.Value != previous.Value)
                {
                    this.ReportIssue(
                        definition,
                        plan,
                        this.FindSemanticStep(plan, "condition", condition.Name),
                        EcnDiagnosticCodes.UnsatisfiableCondition,
                        "Conflicting equality conditions cannot be satisfied together.",
                        condition.Span);
                }
                earlier = earlier + 1;
            }
            index = index + 1;
        }
    }

    internal void ValidateDeterminants(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan)
    {
        int stepIndex = 0;
        while (stepIndex < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[stepIndex];
            if (step.Kind == EcnPlanStepKind.DeterminantRead
                || step.Kind == EcnPlanStepKind.DeterminantWrite)
            {
                EcnPlanDependency? determinant = this.FindDeterminantDependency(step);
                if (determinant == null || determinant.Path.Length == 0)
                {
                    this.ReportIssue(
                        definition,
                        plan,
                        step,
                        EcnDiagnosticCodes.UnavailableDeterminant,
                        "The ECN determinant is not available in the plan field or parameter scope.",
                        determinant == null ? step.Trace.Span : determinant.Span);
                }
            }
            stepIndex = stepIndex + 1;
        }

        int relationshipIndex = 0;
        while (relationshipIndex < definition.Determinants.Length)
        {
            EcnDeterminantRelationship relationship =
                definition.Determinants[relationshipIndex];
            string determinantPath = relationship.DeterminantPath == null
                ? ""
                : relationship.DeterminantPath.NormalizedText.Length > 0
                    ? relationship.DeterminantPath.NormalizedText
                    : relationship.DeterminantPath.Text;
            if (this.ModeRequiresScopedDeterminant(relationship.Mode)
                && relationship.DeterminantPath != null
                && !relationship.DeterminantPath.Deferred
                && !this.PathIsAvailable(definition, determinantPath))
            {
                this.ReportIssue(
                    definition,
                    plan,
                    this.FindSemanticStep(
                        plan,
                        "determinant",
                        relationship.ControlKind.ToString()),
                    EcnDiagnosticCodes.UnavailableDeterminant,
                    "The ECN determinant is not available in the plan field or parameter scope.",
                    relationship.Span);
            }
            if (relationship.Direction == EcnLinkDirection.DeterminantToContent
                && relationship.ControlledPath != null
                && relationship.DeterminantPath != null
                && relationship.ControlledPath.TopLevelFieldOrder >= 0
                && relationship.DeterminantPath.TopLevelFieldOrder >= 0
                && relationship.DeterminantPath.TopLevelFieldOrder
                    >= relationship.ControlledPath.TopLevelFieldOrder)
            {
                this.ReportIssue(
                    definition,
                    plan,
                    this.FindSemanticStep(
                        plan,
                        "determinant",
                        relationship.ControlKind.ToString()),
                    EcnDiagnosticCodes.UnavailableDeterminant,
                    "A decoding determinant must be available before its controlled field.",
                    relationship.Span);
            }
            relationshipIndex = relationshipIndex + 1;
        }
    }

    internal bool ModeRequiresScopedDeterminant(EcnDeterminationMode mode)
    {
        return mode == EcnDeterminationMode.FieldToBeSet
            || mode == EcnDeterminationMode.FieldToBeUsed
            || mode == EcnDeterminationMode.FlagToBeSet
            || mode == EcnDeterminationMode.FlagToBeUsed;
    }

    internal EcnPlanDependency? FindDeterminantDependency(
        EcnExecutablePlanStep step)
    {
        int index = 0;
        while (index < step.Dependencies.Length)
        {
            if (step.Dependencies[index].Kind
                == EcnPlanDependencyKind.DeterminantValue)
            {
                return step.Dependencies[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal bool PathIsAvailable(
        EcnEncodingObjectDefinition definition,
        string path)
    {
        if (path.Length == 0)
        {
            return false;
        }
        if (path[0] == '$')
        {
            return true;
        }
        int dot = path.IndexOf('.', System.StringComparison.Ordinal);
        if (dot >= 0)
        {
            return true;
        }
        string root = dot < 0 ? path : path.Substring(0, dot);
        EcnEncodingField[] fields = definition.Constructor == null
            ? []
            : definition.Constructor.Fields;
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Name == root)
            {
                return true;
            }
            index = index + 1;
        }
        EcnScope? scope = definition.Symbol == null
            ? null
            : definition.Symbol.Scope;
        while (scope != null)
        {
            if (scope.FindLocalSymbol(root) != null)
            {
                return true;
            }
            scope = scope.Parent;
        }
        return false;
    }

    internal void ValidateAllBounds(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan)
    {
        if (!this.BoundsAreValid(plan.Bounds))
        {
            this.ReportIssue(
                definition,
                plan,
                plan.Root,
                EcnDiagnosticCodes.InvalidPlanBounds,
                "The ECN plan has inconsistent or negative bounds.",
                plan.Span);
        }
        int index = 0;
        while (index < plan.Steps.Length)
        {
            if (!this.BoundsAreValid(plan.Steps[index].Bounds))
            {
                this.ReportIssue(
                    definition,
                    plan,
                    plan.Steps[index],
                    EcnDiagnosticCodes.InvalidPlanBounds,
                    "The ECN plan step has inconsistent or negative bounds.",
                    plan.Steps[index].Trace.Span);
            }
            index = index + 1;
        }
    }

    internal bool BoundsAreValid(EcnPlanBounds bounds)
    {
        if ((bounds.HasMinimumBits && bounds.MinimumBits < 0)
            || (bounds.HasMaximumBits && bounds.MaximumBits < 0)
            || (bounds.HasMinimumBytes && bounds.MinimumBytes < 0)
            || (bounds.HasMaximumBytes && bounds.MaximumBytes < 0))
        {
            return false;
        }
        if (bounds.HasMinimumBits && bounds.HasMaximumBits
            && bounds.MinimumBits > bounds.MaximumBits)
        {
            return false;
        }
        if (bounds.HasMinimumBytes && bounds.HasMaximumBytes
            && bounds.MinimumBytes > bounds.MaximumBytes)
        {
            return false;
        }
        if (bounds.Exact
            && ((!bounds.HasMinimumBits || !bounds.HasMaximumBits)
                && (!bounds.HasMinimumBytes || !bounds.HasMaximumBytes)))
        {
            return false;
        }
        if (bounds.Exact && bounds.HasMinimumBits && bounds.HasMaximumBits
            && bounds.MinimumBits != bounds.MaximumBits)
        {
            return false;
        }
        if (bounds.Exact && bounds.HasMinimumBytes && bounds.HasMaximumBytes
            && bounds.MinimumBytes != bounds.MaximumBytes)
        {
            return false;
        }
        if (bounds.Exact && bounds.HasMinimumBits && bounds.HasMinimumBytes
            && bounds.MinimumBits != bounds.MinimumBytes * 8)
        {
            return false;
        }
        if (bounds.HasLowerValue && bounds.HasUpperValue
            && this.CompareIntegerText(bounds.LowerValue, bounds.UpperValue) > 0)
        {
            return false;
        }
        return true;
    }

    internal int CompareIntegerText(string left, string right)
    {
        if (!this.IsIntegerText(left) || !this.IsIntegerText(right))
        {
            return 0;
        }
        string leftText = this.NormalizeIntegerText(left);
        string rightText = this.NormalizeIntegerText(right);
        bool leftNegative = leftText[0] == '-';
        bool rightNegative = rightText[0] == '-';
        if (leftNegative != rightNegative)
        {
            return leftNegative ? -1 : 1;
        }
        int leftStart = leftNegative ? 1 : 0;
        int rightStart = rightNegative ? 1 : 0;
        int lengthComparison = leftText.Length - leftStart
            - (rightText.Length - rightStart);
        if (lengthComparison != 0)
        {
            return leftNegative ? -lengthComparison : lengthComparison;
        }
        int comparison = string.CompareOrdinal(
            leftText.Substring(leftStart),
            rightText.Substring(rightStart));
        return leftNegative ? -comparison : comparison;
    }

    internal bool IsIntegerText(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }
        int index = text[0] == '-' || text[0] == '+' ? 1 : 0;
        if (index == text.Length)
        {
            return false;
        }
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal string NormalizeIntegerText(string text)
    {
        bool negative = text[0] == '-';
        int index = text[0] == '-' || text[0] == '+' ? 1 : 0;
        while (index + 1 < text.Length && text[index] == '0')
        {
            index = index + 1;
        }
        string digits = text.Substring(index);
        if (digits == "0")
        {
            return "0";
        }
        return negative ? "-" + digits : digits;
    }

    internal void ValidateAlternatives(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan)
    {
        if (plan.Root == null || plan.Root.Kind != EcnPlanStepKind.Choice)
        {
            return;
        }
        int index = 0;
        while (index < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[index];
            if (step.Trace.SemanticKind == "field")
            {
                int earlier = 0;
                while (earlier < index)
                {
                    EcnExecutablePlanStep previous = plan.Steps[earlier];
                    if (previous.Trace.SemanticKind == "field"
                        && (previous.Name == step.Name
                            || previous.Path == step.Path))
                    {
                        this.ReportIssue(
                            definition,
                            plan,
                            step,
                            EcnDiagnosticCodes.AmbiguousAlternative,
                            "Choice alternatives must have distinct field identities and paths.",
                            step.Trace.Span);
                    }
                    earlier = earlier + 1;
                }
            }
            index = index + 1;
        }
    }

    internal void VisitObjectLinks(int definitionIndex)
    {
        this.RecursionStates[definitionIndex] = 1;
        EcnEncodingObjectDefinition definition =
            this.Definitions[definitionIndex];
        int linkIndex = 0;
        while (linkIndex < definition.Links.Length)
        {
            EcnSymbol? reference = definition.Links[linkIndex].ReferenceSymbol;
            EcnEncodingObjectDefinition? target = reference == null
                ? null
                : reference.ObjectDefinition;
            int targetIndex = this.FindDefinitionIndex(target);
            if (targetIndex >= 0)
            {
                if (this.RecursionStates[targetIndex] == 1)
                {
                    if (definition.Plan != null)
                    {
                        this.ReportIssue(
                            definition,
                            definition.Plan,
                            this.FindSemanticStep(
                                definition.Plan,
                                "link",
                                definition.Links[linkIndex].ReferenceName),
                            EcnDiagnosticCodes.PlanRecursion,
                            "Encoding-object links form a recursive executable plan.",
                            definition.Links[linkIndex].Span);
                    }
                }
                else if (this.RecursionStates[targetIndex] == 0)
                {
                    this.VisitObjectLinks(targetIndex);
                }
            }
            linkIndex = linkIndex + 1;
        }
        this.RecursionStates[definitionIndex] = 2;
    }

    internal int FindDefinitionIndex(EcnEncodingObjectDefinition? definition)
    {
        if (definition == null)
        {
            return -1;
        }
        int index = 0;
        while (index < this.Definitions.Length)
        {
            if (this.Definitions[index] == definition)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal void ReportIssue(
        EcnEncodingObjectDefinition definition,
        EcnExecutablePlan plan,
        EcnExecutablePlanStep? step,
        int code,
        string message,
        AsnSourceSpan? span)
    {
        EcnPlanValidationIssue issue = new EcnPlanValidationIssue
        {
            Code = code,
            Message = message,
            StepId = step == null ? "" : step.StableId,
            Path = step == null ? "" : step.Path,
            Span = span
        };
        plan.ValidationIssues = this.AppendIssue(plan.ValidationIssues, issue);
        plan.Valid = false;
        definition.Valid = false;
        if (step != null)
        {
            step.Valid = false;
        }
        if (code == EcnDiagnosticCodes.NonDeterministicPlan
            || code == EcnDiagnosticCodes.PlanRecursion
            || code == EcnDiagnosticCodes.AmbiguousAlternative)
        {
            plan.Deterministic = false;
        }
        if (code == EcnDiagnosticCodes.NonDecodablePlan
            || code == EcnDiagnosticCodes.UnavailableDeterminant
            || code == EcnDiagnosticCodes.UnsatisfiableCondition
            || code == EcnDiagnosticCodes.AmbiguousAlternative)
        {
            plan.Decodable = false;
        }
        if (code == EcnDiagnosticCodes.IncompleteFieldCoverage)
        {
            plan.Complete = false;
        }
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Ecn,
            code,
            message,
            span,
            null);
        this.Success = false;
    }

    internal EcnPlanValidationIssue[] AppendIssue(
        EcnPlanValidationIssue[] source,
        EcnPlanValidationIssue issue)
    {
        EcnPlanValidationIssue[] result =
            new EcnPlanValidationIssue[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        result[source.Length] = issue;
        return result;
    }
}
