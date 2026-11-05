namespace asn1;

internal class InformationFromObjectsParser
{
    internal AsnToken[] Tokens;
    internal DiagnosticBag Diagnostics;
    internal bool ReportDiagnostics;
    internal int Position;

    internal InformationFromObjectsParser(
        AsnToken[] tokens,
        DiagnosticBag diagnostics,
        bool reportDiagnostics)
    {
        this.Tokens = tokens == null ? throw new System.ArgumentNullException("tokens") : tokens;
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.ReportDiagnostics = reportDiagnostics;
    }

    internal InformationFromObjectsParseResult ParseInformationFromObjectsSyntax(
        AsnAssignmentKind expectedKind)
    {
        int start = this.Position;
        AsnToken? first = this.PeekToken();
        if (!this.IsReferenceToken(first))
        {
            this.AddDiagnostic(174, "Expected an object or object-set reference.", first);
            return this.CreateResult(false, null, start);
        }

        this.Position = this.Position + 1;
        string module = "";
        AsnToken reference = first == null ? new AsnToken() : first;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Dot)
            && !this.IsFieldReference(this.PeekTokenAt(1))
            && this.IsReferenceToken(this.PeekTokenAt(1))
            && this.IsKind(this.PeekTokenAt(2), AsnTokenKind.Dot)
            && this.IsFieldReference(this.PeekTokenAt(3)))
        {
            module = reference.Text;
            reference = this.PeekTokenAt(1) ?? reference;
            this.Position = this.Position + 2;
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.Dot)
            || !this.IsFieldReference(this.PeekTokenAt(1)))
        {
            this.AddDiagnostic(175, "Expected a field name after the referenced object or object set.", this.PeekToken());
            return this.CreateResult(false, null, start);
        }

        System.Collections.Generic.List<string> fields = new System.Collections.Generic.List<string>();
        while (this.IsKind(this.PeekToken(), AsnTokenKind.Dot)
            && this.IsFieldReference(this.PeekTokenAt(1)))
        {
            this.Position = this.Position + 1;
            AsnToken field = this.ReadRequiredToken();
            fields.Add(field.Text);
        }

        bool classReference = reference.Kind == AsnTokenKind.ObjectClassReference
            || reference.Text == "TYPE-IDENTIFIER"
            || reference.Text == "ABSTRACT-SYNTAX";
        AsnAssignmentKind[] candidates = this.GetCandidateKinds(
            fields[fields.Count - 1],
            this.IsObjectSetReference(reference));
        if (expectedKind != AsnAssignmentKind.Unknown
            && !this.ContainsKind(candidates, expectedKind)
            && !(expectedKind == AsnAssignmentKind.Type && classReference))
        {
            this.AddDiagnostic(176, "The selected field cannot produce the expected information category.", this.PeekTokenAt(-1));
            return this.CreateResult(false, null, start);
        }

        ParsedInformationFromObjectsSyntax syntax = new ParsedInformationFromObjectsSyntax
        {
            ReferenceModule = module,
            ReferenceName = reference.Text,
            ReferencedObjectSet = this.IsObjectSetReference(reference),
            FieldPath = fields.ToArray(),
            CandidateKinds = expectedKind == AsnAssignmentKind.Unknown
                ? candidates
                : [expectedKind],
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.CreateResult(true, syntax, start);
    }

    internal AsnAssignmentKind[] GetCandidateKinds(string finalField, bool fromSet)
    {
        bool upper = finalField.Length > 1
            && finalField[1] >= 'A' && finalField[1] <= 'Z';
        if (upper)
        {
            return fromSet
                ? [AsnAssignmentKind.ValueSet, AsnAssignmentKind.ObjectSet]
                : [AsnAssignmentKind.Type, AsnAssignmentKind.ValueSet, AsnAssignmentKind.ObjectSet];
        }

        return fromSet
            ? [AsnAssignmentKind.ValueSet, AsnAssignmentKind.ObjectSet]
            : [AsnAssignmentKind.Value, AsnAssignmentKind.Object];
    }

    internal bool ContainsKind(AsnAssignmentKind[] kinds, AsnAssignmentKind expected)
    {
        int index = 0;
        while (index < kinds.Length)
        {
            if (kinds[index] == expected)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsUpperReference(AsnToken token)
    {
        return token.Text.Length > 0
            && token.Text[0] >= 'A' && token.Text[0] <= 'Z';
    }

    internal bool IsObjectSetReference(AsnToken token)
    {
        if (token.Kind == AsnTokenKind.ObjectClassReference
            || token.Text == "TYPE-IDENTIFIER"
            || token.Text == "ABSTRACT-SYNTAX")
        {
            return false;
        }

        if (token.Kind == AsnTokenKind.ObjectSetReference)
        {
            return true;
        }

        return this.IsUpperReference(token);
    }

    internal bool IsReferenceToken(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.Identifier
                || token.Kind == AsnTokenKind.ValueReference
                || token.Kind == AsnTokenKind.ObjectReference
                || token.Kind == AsnTokenKind.TypeReference
                || token.Kind == AsnTokenKind.ModuleReference
                || token.Kind == AsnTokenKind.ObjectClassReference
                || token.Kind == AsnTokenKind.ObjectSetReference
                || token.Text == "TYPE-IDENTIFIER"
                || token.Text == "ABSTRACT-SYNTAX");
    }

    internal bool IsFieldReference(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.TypeFieldReference
                || token.Kind == AsnTokenKind.ValueFieldReference
                || token.Kind == AsnTokenKind.ValueSetFieldReference
                || token.Kind == AsnTokenKind.ObjectFieldReference
                || token.Kind == AsnTokenKind.ObjectSetFieldReference);
    }

    internal InformationFromObjectsParseResult CreateResult(
        bool success,
        ParsedInformationFromObjectsSyntax? syntax,
        int start)
    {
        return new InformationFromObjectsParseResult
        {
            Success = success,
            Syntax = syntax,
            ConsumedTokenCount = this.Position - start
        };
    }

    internal AsnToken[] CopyTokenRange(int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = this.Tokens[index];
            index = index + 1;
        }

        return result;
    }

    internal AsnToken ReadRequiredToken()
    {
        AsnToken token = this.Tokens[this.Position];
        this.Position = this.Position + 1;
        return token;
    }

    internal AsnToken? PeekToken()
    {
        return this.PeekTokenAt(0);
    }

    internal AsnToken? PeekTokenAt(int distance)
    {
        int index = this.Position + distance;
        return index < 0 || index >= this.Tokens.Length ? null : this.Tokens[index];
    }

    internal AsnToken? TokenAt(int index)
    {
        return index < 0 || index >= this.Tokens.Length ? null : this.Tokens[index];
    }

    internal bool IsKind(AsnToken? token, AsnTokenKind kind)
    {
        return token != null && token.Kind == kind;
    }

    internal AsnSourceSpan? CreateSpan(AsnToken? first, AsnToken? last)
    {
        if (first == null || first.Span == null || last == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan { Start = first.Span.Start, End = last.Span.End };
    }

    internal void AddDiagnostic(int number, string message, AsnToken? token)
    {
        if (!this.ReportDiagnostics)
        {
            return;
        }

        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Syntax,
            number,
            message,
            token == null ? null : token.Span,
            null);
    }
}
