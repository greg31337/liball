using System;

namespace fake;

internal class EdgeSelectionCandidate
{
    internal EdgeCaseCategory Category;
    internal int Index;
    internal ValidityMode Validity;
    internal string ViolatedRule = string.Empty;

    internal EdgeSelectionCandidate(
        EdgeCaseCategory category,
        int index,
        ValidityMode validity,
        string violatedRule)
    {
        Category = category;
        Index = index;
        Validity = validity;
        ViolatedRule = violatedRule;
    }
}

internal static class EdgeCaseSelector
{
    internal static EdgeSelectionResult AccordingToMode(
        GenerationContext context,
        FakeRandom random)
    {
        switch (context.GenerationModeValue)
        {
            case GenerationMode.ValidUnusual:
                return ValidUnusual(context, random, GenerationMode.ValidUnusual);
            case GenerationMode.Invalid:
                return Invalid(context, random, GenerationMode.Invalid);
            case GenerationMode.Mixed:
                return Mixed(context, random);
            case GenerationMode.Adversarial:
                return Adversarial(context, random, GenerationMode.Adversarial);
            default:
                throw new InvalidOperationException(
                    "Edge selection requires ValidUnusual, Invalid, Mixed, or Adversarial generation mode.");
        }
    }

    internal static EdgeSelectionResult ValidUnusual(
        GenerationContext context,
        FakeRandom random,
        GenerationMode resultMode)
    {
        EdgeSelectionCandidate[] candidates = ValidCandidates();
        return Choose(context, random, candidates, resultMode);
    }

    internal static EdgeSelectionResult Invalid(
        GenerationContext context,
        FakeRandom random,
        GenerationMode resultMode)
    {
        EdgeSelectionCandidate[] candidates = InvalidCandidates();
        return Choose(context, random, candidates, resultMode);
    }

    internal static EdgeSelectionResult Adversarial(
        GenerationContext context,
        FakeRandom random,
        GenerationMode resultMode)
    {
        EdgeSelectionCandidate[] candidates = AdversarialCandidates();
        return Choose(context, random, candidates, resultMode);
    }

    internal static EdgeSelectionResult AdversarialForContext(
        GenerationContext context,
        AdversarialTextContext textContext)
    {
        EdgeSelectionCandidate candidate = AdversarialCandidate(textContext);
        EdgeCaseRecord selected = EdgeCaseCatalog.AdversarialText(
            context,
            textContext);
        return Build(
            context,
            selected,
            candidate,
            GenerationMode.Adversarial);
    }

    internal static EdgeSelectionResult Mixed(
        GenerationContext context,
        FakeRandom random)
    {
        int pool = random.IntFromMaximumExclusive(3);
        if (pool == 0)
        {
            return ValidUnusual(context, random, GenerationMode.Mixed);
        }
        if (pool == 1)
        {
            return Invalid(context, random, GenerationMode.Mixed);
        }
        return Adversarial(context, random, GenerationMode.Mixed);
    }

    static EdgeSelectionResult Choose(
        GenerationContext context,
        FakeRandom random,
        EdgeSelectionCandidate[] candidates,
        GenerationMode resultMode)
    {
        int index = random.IntFromMaximumExclusive(candidates.Length);
        EdgeSelectionCandidate candidate = candidates[index];
        return Build(context, candidate, resultMode);
    }

    static EdgeSelectionResult Build(
        GenerationContext context,
        EdgeSelectionCandidate candidate,
        GenerationMode resultMode)
    {
        EdgeCaseRecord[] category = EdgeCaseCatalog.Cases(
            context,
            candidate.Category);
        EdgeCaseRecord selected = category[candidate.Index];
        return Build(context, selected, candidate, resultMode);
    }

    static EdgeSelectionResult Build(
        GenerationContext context,
        EdgeCaseRecord selected,
        EdgeSelectionCandidate candidate,
        GenerationMode resultMode)
    {
        EdgeSelectionResult result = new EdgeSelectionResult();
        result.Edge = selected;
        result.SelectedEdge = selected.Name;
        result.Category = selected.Category;
        result.GenerationMode = resultMode;
        result.Validity = candidate.Validity;
        result.ViolatedRule = candidate.ViolatedRule;
        context.AttachModesFromMetadata(result);
        ValidationResult validation = new ValidationResult();
        validation.Valid = candidate.Validity == ValidityMode.Valid;
        validation.StructuralValidity = candidate.Validity;
        validation.ChecksumValidity = ValidityMode.NotApplicable;
        validation.ConstraintValidity = ValidityMode.NotApplicable;
        validation.CorrelationValidity = ValidityMode.NotApplicable;
        validation.AllocationStatus = AllocationStatus.NotApplicable;
        validation.RequestedEdgeCase = selected.Name;
        validation.ViolatedRule = candidate.ViolatedRule;
        validation.Detail = selected.Description;
        if (selected.Category == EdgeCaseCategory.Identifier)
        {
            validation.AllocationStatus = AllocationStatus.Unknown;
            validation.ChecksumValidity = ValidityMode.Unknown;
            if (selected.Name == "wrong-checksum")
            {
                validation.StructuralValidity = ValidityMode.Valid;
                validation.ChecksumValidity = ValidityMode.Invalid;
            }
            else if (selected.Name == "bad-uuid-version" ||
                selected.Name == "ulid-overflow")
            {
                validation.StructuralValidity = ValidityMode.Valid;
                validation.ConstraintValidity = ValidityMode.Invalid;
            }
        }
        else if (selected.Category == EdgeCaseCategory.ContradictoryInput)
        {
            validation.StructuralValidity = ValidityMode.NotApplicable;
            validation.CorrelationValidity = ValidityMode.Invalid;
        }
        else if (selected.Category == EdgeCaseCategory.Injection)
        {
            validation.StructuralValidity = ValidityMode.Valid;
            validation.ConstraintValidity = ValidityMode.Invalid;
        }
        GenerationMetadata metadata = new GenerationMetadata();
        metadata.Value = selected.Name;
        metadata.GeneratedValue = selected.Value;
        metadata.ValueKind = selected.Category.ToString();
        metadata.Provider = "edge";
        metadata.Locale = context.CapturedOptions.Locale;
        metadata.SourceLocale = LocaleFieldValue.InvariantLocale;
        metadata.Rule = selected.Description;
        metadata.Format = "edge fixture";
        metadata.RepresentsAssignment = false;
        context.AttachModes(metadata);
        metadata.GenerationMode = resultMode;
        metadata.StructuralValidity = validation.StructuralValidity;
        metadata.ChecksumValidity = validation.ChecksumValidity;
        metadata.ConstraintValidity = validation.ConstraintValidity;
        metadata.CorrelationValidity = validation.CorrelationValidity;
        metadata.FictionalReserved = validation.FictionalReserved;
        metadata.SafeReserved = validation.SafeReserved;
        metadata.AllocationStatus = validation.AllocationStatus;
        metadata.Validation = validation;
        metadata.RequestedEdgeCase = selected.Name;
        metadata.ViolatedRule = candidate.ViolatedRule;
        result.Metadata = metadata;
        return result;
    }

    static EdgeSelectionCandidate AdversarialCandidate(
        AdversarialTextContext textContext)
    {
        switch (textContext)
        {
            case AdversarialTextContext.Sql:
                return Invalid(EdgeCaseCategory.Injection, 0, "SQL input must be treated as data");
            case AdversarialTextContext.Html:
                return Invalid(EdgeCaseCategory.Injection, 1, "HTML input must be escaped for its output context");
            case AdversarialTextContext.Spreadsheet:
                return Invalid(EdgeCaseCategory.Injection, 2, "spreadsheet formulas must be guarded");
            case AdversarialTextContext.Shell:
                return Invalid(EdgeCaseCategory.Injection, 3, "shell input must not be executed");
            case AdversarialTextContext.Path:
                return Invalid(EdgeCaseCategory.Injection, 4, "path input must remain within its allowed root");
            case AdversarialTextContext.FormatString:
                return Invalid(EdgeCaseCategory.Injection, 5, "format input must not be interpreted as directives");
            case AdversarialTextContext.Template:
                return Invalid(EdgeCaseCategory.Injection, 6, "template input must not be evaluated");
            case AdversarialTextContext.Header:
                return Invalid(EdgeCaseCategory.Injection, 7, "header values must not contain record separators");
            default:
                throw new ArgumentOutOfRangeException(
                    "context",
                    textContext,
                    "context is not supported.");
        }
    }

    static EdgeSelectionCandidate[] ValidCandidates()
    {
        return new EdgeSelectionCandidate[]
        {
            Valid(EdgeCaseCategory.BoundaryNumber, 2),
            Valid(EdgeCaseCategory.BoundaryNumber, 3),
            Valid(EdgeCaseCategory.BoundaryNumber, 4),
            Valid(EdgeCaseCategory.BoundaryNumber, 5),
            Valid(EdgeCaseCategory.BoundaryNumber, 6),
            Valid(EdgeCaseCategory.BoundaryNumber, 7),
            Valid(EdgeCaseCategory.BoundaryNumber, 8),
            Valid(EdgeCaseCategory.BoundaryNumber, 9),
            Valid(EdgeCaseCategory.BoundaryNumber, 10),
            Valid(EdgeCaseCategory.BoundaryNumber, 11),
            Valid(EdgeCaseCategory.BoundaryNumber, 15),
            Valid(EdgeCaseCategory.LongRepeatedText, 0),
            Valid(EdgeCaseCategory.LongRepeatedText, 1),
            Valid(EdgeCaseCategory.LongRepeatedText, 2),
            Valid(EdgeCaseCategory.Unicode, 0),
            Valid(EdgeCaseCategory.Unicode, 2),
            Valid(EdgeCaseCategory.Unicode, 3),
            Valid(EdgeCaseCategory.Unicode, 4),
            Valid(EdgeCaseCategory.Unicode, 5),
            Valid(EdgeCaseCategory.Unicode, 6),
            Valid(EdgeCaseCategory.Unicode, 7),
            Valid(EdgeCaseCategory.Unicode, 8),
            Valid(EdgeCaseCategory.Temporal, 0),
            Valid(EdgeCaseCategory.Temporal, 1),
            Valid(EdgeCaseCategory.Temporal, 2),
            Valid(EdgeCaseCategory.Temporal, 3),
            Valid(EdgeCaseCategory.Temporal, 4),
            Valid(EdgeCaseCategory.Temporal, 5),
            Valid(EdgeCaseCategory.Temporal, 6),
            Valid(EdgeCaseCategory.Temporal, 7),
            Valid(EdgeCaseCategory.Temporal, 8)
        };
    }

    static EdgeSelectionCandidate[] InvalidCandidates()
    {
        return new EdgeSelectionCandidate[]
        {
            Invalid(EdgeCaseCategory.EmptyNullWhitespace, 0, "value must not be null"),
            Invalid(EdgeCaseCategory.EmptyNullWhitespace, 1, "value must not be empty"),
            Invalid(EdgeCaseCategory.EmptyNullWhitespace, 2, "value must not be whitespace-only"),
            Invalid(EdgeCaseCategory.EmptyNullWhitespace, 3, "value must not be whitespace-only"),
            Invalid(EdgeCaseCategory.BoundaryNumber, 12, "number must be finite"),
            Invalid(EdgeCaseCategory.BoundaryNumber, 13, "number must be finite"),
            Invalid(EdgeCaseCategory.BoundaryNumber, 14, "number must be finite"),
            Invalid(EdgeCaseCategory.Unicode, 9, "text must contain well-formed UTF-16"),
            Invalid(EdgeCaseCategory.Identifier, 0, "identifier checksum must be valid"),
            Invalid(EdgeCaseCategory.Identifier, 1, "identifier length must be supported"),
            Invalid(EdgeCaseCategory.Identifier, 2, "identifier characters must be permitted"),
            Invalid(EdgeCaseCategory.Identifier, 3, "identifier prefix must be permitted"),
            Invalid(EdgeCaseCategory.Identifier, 4, "UUID version must be supported"),
            Invalid(EdgeCaseCategory.Identifier, 5, "ULID text must fit in 128 bits"),
            Invalid(EdgeCaseCategory.Identifier, 6, "IBAN country and length must be supported"),
            Invalid(EdgeCaseCategory.Identifier, 7, "IDN label must contain a complete encoding"),
            Invalid(EdgeCaseCategory.Identifier, 8, "phone extension formatting must be unambiguous"),
            Invalid(EdgeCaseCategory.ContradictoryInput, 0, "minimum must not exceed maximum"),
            Invalid(EdgeCaseCategory.ContradictoryInput, 1, "count must not be negative"),
            Invalid(EdgeCaseCategory.ContradictoryInput, 2, "exclusive interval must contain a representable value"),
            Invalid(EdgeCaseCategory.ContradictoryInput, 3, "one field cannot equal two distinct required values"),
            Invalid(EdgeCaseCategory.ContradictoryInput, 4, "one feature cannot be both required and forbidden")
        };
    }

    static EdgeSelectionCandidate[] AdversarialCandidates()
    {
        return new EdgeSelectionCandidate[]
        {
            AdversarialCandidate(AdversarialTextContext.Sql),
            AdversarialCandidate(AdversarialTextContext.Html),
            AdversarialCandidate(AdversarialTextContext.Spreadsheet),
            AdversarialCandidate(AdversarialTextContext.Shell),
            AdversarialCandidate(AdversarialTextContext.Path),
            AdversarialCandidate(AdversarialTextContext.FormatString),
            AdversarialCandidate(AdversarialTextContext.Template),
            AdversarialCandidate(AdversarialTextContext.Header)
        };
    }

    static EdgeSelectionCandidate Valid(
        EdgeCaseCategory category,
        int index)
    {
        return new EdgeSelectionCandidate(
            category,
            index,
            ValidityMode.Valid,
            "");
    }

    static EdgeSelectionCandidate Invalid(
        EdgeCaseCategory category,
        int index,
        string violatedRule)
    {
        return new EdgeSelectionCandidate(
            category,
            index,
            ValidityMode.Invalid,
            violatedRule);
    }
}
