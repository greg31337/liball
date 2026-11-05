namespace asn1;

internal class DiagnosticBag
{
    internal bool TreatWarningsAsErrors;
    internal bool EnableLegacyAny;
    internal ResourceCounter Resources;
    internal System.Collections.Generic.List<AsnDiagnostic> Items;
    internal bool RecoveryLimitReached;

    internal DiagnosticBag(bool treatWarningsAsErrors, AsnResourceLimits limits)
    {
        this.TreatWarningsAsErrors = treatWarningsAsErrors;
        this.Resources = new ResourceCounter(limits == null ? throw new System.ArgumentNullException("limits") : limits);
        this.Items = new System.Collections.Generic.List<AsnDiagnostic>();
    }

    internal void ApplyCompilerOptions(AsnCompilerOptions? options)
    {
        this.EnableLegacyAny = options != null && options.EnableLegacyAny;
    }

    internal bool TryAddDiagnostic(
        AsnDiagnosticSeverity severity,
        AsnDiagnosticCategory category,
        int number,
        string message,
        AsnSourceSpan? span,
        AsnDiagnosticRelatedLocation[]? relatedLocations)
    {
        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }

        AsnDiagnostic diagnostic = new AsnDiagnostic
        {
            Severity = this.TreatWarningsAsErrors && severity == AsnDiagnosticSeverity.Warning
                ? AsnDiagnosticSeverity.Error
                : severity,
            Category = category,
            Code = CreateCode(category, number),
            Message = message,
            Span = span == null ? null : this.CloneSpan(span),
            RelatedLocations = this.CloneRelatedLocations(relatedLocations)
        };

        int duplicateIndex = this.FindDuplicateIndex(diagnostic);
        if (duplicateIndex >= 0)
        {
            this.MergeRelatedLocations(this.Items[duplicateIndex], diagnostic.RelatedLocations);
            return true;
        }

        if (!this.Resources.TryChargeDiagnostic())
        {
            this.RecoveryLimitReached = true;
            return false;
        }

        this.Items.Add(diagnostic);
        return true;
    }

    internal AsnDiagnostic[] GetDiagnostics()
    {
        AsnDiagnostic[] result = new AsnDiagnostic[this.Items.Count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.CloneDiagnostic(this.Items[index]);
            this.SortRelatedLocations(result[index].RelatedLocations);
            index = index + 1;
        }

        int insertionIndex = 1;
        while (insertionIndex < result.Length)
        {
            AsnDiagnostic current = result[insertionIndex];
            int position = insertionIndex;
            while (position > 0 && this.CompareDiagnostics(result[position - 1], current) > 0)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            insertionIndex = insertionIndex + 1;
        }

        return result;
    }

    internal int GetCount()
    {
        return this.Items.Count;
    }

    internal void AddDiagnostic(AsnDiagnostic diagnostic)
    {
        AsnDiagnostic clone = this.CloneDiagnostic(diagnostic);
        if (this.TreatWarningsAsErrors
            && clone.Severity == AsnDiagnosticSeverity.Warning)
        {
            clone.Severity = AsnDiagnosticSeverity.Error;
        }

        if (this.FindDuplicateIndex(clone) < 0)
        {
            this.Items.Add(clone);
        }
    }

    internal bool CanContinueRecovery()
    {
        return !this.RecoveryLimitReached;
    }

    internal bool ContainsErrors()
    {
        int index = 0;
        while (index < this.Items.Count)
        {
            if (this.Items[index].Severity == AsnDiagnosticSeverity.Error)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string CreateCode(AsnDiagnosticCategory category, int number)
    {
        if (number < 0)
        {
            throw new System.ArgumentOutOfRangeException("number");
        }

        string prefix = "ASN";
        switch (category)
        {
            case AsnDiagnosticCategory.General:
                prefix = "ASN";
                break;
            case AsnDiagnosticCategory.Source:
                prefix = "ASNSRC";
                break;
            case AsnDiagnosticCategory.Lexical:
                prefix = "ASNL";
                break;
            case AsnDiagnosticCategory.Syntax:
                prefix = "ASNP";
                break;
            case AsnDiagnosticCategory.Binding:
                prefix = "ASNB";
                break;
            case AsnDiagnosticCategory.Semantic:
                prefix = "ASNS";
                break;
            case AsnDiagnosticCategory.Constraint:
                prefix = "ASNC";
                break;
            case AsnDiagnosticCategory.Generation:
                prefix = "ASNGEN";
                break;
            case AsnDiagnosticCategory.Encoding:
                prefix = "ASNE";
                break;
            case AsnDiagnosticCategory.Decoding:
                prefix = "ASND";
                break;
            case AsnDiagnosticCategory.Canonicality:
                prefix = "ASNCAN";
                break;
            case AsnDiagnosticCategory.Resource:
                prefix = "ASNR";
                break;
            case AsnDiagnosticCategory.Ecn:
                prefix = "ASNECN";
                break;
            case AsnDiagnosticCategory.Xsd:
                prefix = "ASNXSD";
                break;
            case AsnDiagnosticCategory.InputOutput:
                prefix = "ASNIO";
                break;
            default:
                prefix = "ASN";
                break;
        }

        return prefix + number.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal int FindDuplicateIndex(AsnDiagnostic candidate)
    {
        int index = 0;
        while (index < this.Items.Count)
        {
            AsnDiagnostic existing = this.Items[index];
            if (existing.Severity == candidate.Severity
                && existing.Category == candidate.Category
                && existing.Code == candidate.Code
                && existing.Message == candidate.Message
                && this.SpansEqual(existing.Span, candidate.Span))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void MergeRelatedLocations(AsnDiagnostic target, AsnDiagnosticRelatedLocation[] additions)
    {
        int additionIndex = 0;
        while (additionIndex < additions.Length)
        {
            AsnDiagnosticRelatedLocation addition = additions[additionIndex];
            bool found = false;
            int existingIndex = 0;
            while (existingIndex < target.RelatedLocations.Length)
            {
                AsnDiagnosticRelatedLocation existing = target.RelatedLocations[existingIndex];
                if (existing.Message == addition.Message && this.SpansEqual(existing.Span, addition.Span))
                {
                    found = true;
                    break;
                }

                existingIndex = existingIndex + 1;
            }

            if (!found)
            {
                AsnDiagnosticRelatedLocation[] replacement = new AsnDiagnosticRelatedLocation[target.RelatedLocations.Length + 1];
                System.Array.Copy(target.RelatedLocations, 0, replacement, 0, target.RelatedLocations.Length);
                replacement[replacement.Length - 1] = this.CloneRelatedLocation(addition);
                target.RelatedLocations = replacement;
            }

            additionIndex = additionIndex + 1;
        }
    }

    internal AsnDiagnostic CloneDiagnostic(AsnDiagnostic diagnostic)
    {
        return new AsnDiagnostic
        {
            Severity = diagnostic.Severity,
            Category = diagnostic.Category,
            Code = diagnostic.Code,
            Message = diagnostic.Message,
            ConstraintId = diagnostic.ConstraintId,
            ConstraintKind = diagnostic.ConstraintKind,
            ValuePath = diagnostic.ValuePath,
            PlanStepId = diagnostic.PlanStepId,
            HasBitOffset = diagnostic.HasBitOffset,
            BitOffset = diagnostic.BitOffset,
            Span = diagnostic.Span == null ? null : this.CloneSpan(diagnostic.Span),
            RelatedLocations = this.CloneRelatedLocations(diagnostic.RelatedLocations)
        };
    }

    internal AsnDiagnosticRelatedLocation[] CloneRelatedLocations(AsnDiagnosticRelatedLocation[]? locations)
    {
        if (locations == null || locations.Length == 0)
        {
            return [];
        }

        AsnDiagnosticRelatedLocation[] result = new AsnDiagnosticRelatedLocation[locations.Length];
        int index = 0;
        while (index < locations.Length)
        {
            if (locations[index] == null)
            {
                throw new System.ArgumentException("Related locations cannot contain null.", "locations");
            }

            result[index] = this.CloneRelatedLocation(locations[index]);
            index = index + 1;
        }

        return result;
    }

    internal AsnDiagnosticRelatedLocation CloneRelatedLocation(AsnDiagnosticRelatedLocation location)
    {
        return new AsnDiagnosticRelatedLocation
        {
            Message = location.Message,
            Span = this.CloneSpan(location.Span)
        };
    }

    internal AsnSourceSpan CloneSpan(AsnSourceSpan span)
    {
        return new AsnSourceSpan
        {
            Start = this.ClonePosition(span.Start),
            End = this.ClonePosition(span.End)
        };
    }

    internal AsnSourcePosition ClonePosition(AsnSourcePosition position)
    {
        return new AsnSourcePosition
        {
            SourceName = position.SourceName,
            Offset = position.Offset,
            Line = position.Line,
            Column = position.Column
        };
    }

    internal bool SpansEqual(AsnSourceSpan? left, AsnSourceSpan? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        return this.PositionsEqual(left.Start, right.Start) && this.PositionsEqual(left.End, right.End);
    }

    internal bool PositionsEqual(AsnSourcePosition left, AsnSourcePosition right)
    {
        return left.SourceName == right.SourceName
            && left.Offset == right.Offset
            && left.Line == right.Line
            && left.Column == right.Column;
    }

    internal int CompareDiagnostics(AsnDiagnostic left, AsnDiagnostic right)
    {
        int spanComparison = this.CompareOptionalSpans(left.Span, right.Span);
        if (spanComparison != 0)
        {
            return spanComparison;
        }

        int categoryComparison = this.CompareIntegers((int)left.Category, (int)right.Category);
        if (categoryComparison != 0)
        {
            return categoryComparison;
        }

        int codeComparison = System.String.CompareOrdinal(left.Code, right.Code);
        if (codeComparison != 0)
        {
            return codeComparison;
        }

        int severityComparison = this.CompareIntegers((int)left.Severity, (int)right.Severity);
        if (severityComparison != 0)
        {
            return severityComparison;
        }

        return System.String.CompareOrdinal(left.Message, right.Message);
    }

    internal int CompareOptionalSpans(AsnSourceSpan? left, AsnSourceSpan? right)
    {
        if (left == null)
        {
            return right == null ? 0 : -1;
        }

        if (right == null)
        {
            return 1;
        }

        return this.CompareSpans(left, right);
    }

    internal int CompareSpans(AsnSourceSpan left, AsnSourceSpan right)
    {
        int startComparison = this.ComparePositions(left.Start, right.Start);
        if (startComparison != 0)
        {
            return startComparison;
        }

        return this.ComparePositions(left.End, right.End);
    }

    internal int ComparePositions(AsnSourcePosition left, AsnSourcePosition right)
    {
        int sourceComparison = System.String.CompareOrdinal(left.SourceName, right.SourceName);
        if (sourceComparison != 0)
        {
            return sourceComparison;
        }

        int offsetComparison = this.CompareIntegers(left.Offset, right.Offset);
        if (offsetComparison != 0)
        {
            return offsetComparison;
        }

        int lineComparison = this.CompareIntegers(left.Line, right.Line);
        if (lineComparison != 0)
        {
            return lineComparison;
        }

        return this.CompareIntegers(left.Column, right.Column);
    }

    internal int CompareIntegers(int left, int right)
    {
        if (left < right)
        {
            return -1;
        }

        if (left > right)
        {
            return 1;
        }

        return 0;
    }

    internal void SortRelatedLocations(AsnDiagnosticRelatedLocation[] locations)
    {
        int insertionIndex = 1;
        while (insertionIndex < locations.Length)
        {
            AsnDiagnosticRelatedLocation current = locations[insertionIndex];
            int position = insertionIndex;
            while (position > 0 && this.CompareRelatedLocations(locations[position - 1], current) > 0)
            {
                locations[position] = locations[position - 1];
                position = position - 1;
            }

            locations[position] = current;
            insertionIndex = insertionIndex + 1;
        }
    }

    internal int CompareRelatedLocations(AsnDiagnosticRelatedLocation left, AsnDiagnosticRelatedLocation right)
    {
        int spanComparison = this.CompareSpans(left.Span, right.Span);
        if (spanComparison != 0)
        {
            return spanComparison;
        }

        return System.String.CompareOrdinal(left.Message, right.Message);
    }
}
