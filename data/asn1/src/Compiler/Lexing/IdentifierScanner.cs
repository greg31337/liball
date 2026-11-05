namespace asn1;

internal class IdentifierScanner
{
    internal LexInputBuffer Inputs;
    internal DiagnosticBag Diagnostics;
    internal TextAccumulator Accumulator;
    internal TextAccumulator NormalizedAccumulator;
    internal bool Scanning;
    internal bool FieldReference;
    internal bool LastWasHyphen;
    internal bool HasAdjacentHyphens;
    internal bool HasLowercase;
    internal bool EncodingClassReference;
    internal bool InvalidEncodingClassReference;
    internal AsnSourcePosition TokenStart = new AsnSourcePosition();
    internal AsnSourcePosition TokenEnd = new AsnSourcePosition();

    internal IdentifierScanner(TriviaScanner trivia, DiagnosticBag diagnostics)
    {
        this.Inputs = new LexInputBuffer(trivia);
        this.Diagnostics = diagnostics == null ? throw new System.ArgumentNullException("diagnostics") : diagnostics;
        this.Accumulator = new TextAccumulator(32);
        this.NormalizedAccumulator = new TextAccumulator(32);
    }

    internal void UseInputBuffer(LexInputBuffer inputs)
    {
        this.Inputs = inputs == null ? throw new System.ArgumentNullException("inputs") : inputs;
    }

    internal IdentifierScannerResult ScanNext()
    {
        if (this.Scanning)
        {
            return this.ScanIdentifierTail();
        }

        TriviaScannerResult first = this.ReadInput();
        if (first.Status == TriviaScannerStatus.NeedMoreData)
        {
            return this.CreateStateResult(IdentifierScannerStatus.NeedMoreData);
        }

        if (first.Status == TriviaScannerStatus.EndOfInput)
        {
            return this.CreateStateResult(IdentifierScannerStatus.EndOfInput);
        }

        if (first.Status == TriviaScannerStatus.Trivia)
        {
            return this.CreateTriviaResult(first.Token);
        }

        SourceCursorReadResult firstInput = first.Input == null
            ? throw new System.InvalidOperationException("Non-trivia input is missing.")
            : first.Input;
        if (this.IsLetter(firstInput))
        {
            this.BeginIdentifier(firstInput, false);
            return this.ScanIdentifierTail();
        }

        if (firstInput.Status == SourceCursorStatus.Value && firstInput.ScalarValue == 38)
        {
            return this.ScanAmpersand(firstInput);
        }

        if (firstInput.Status == SourceCursorStatus.Value
            && firstInput.ScalarValue == 35)
        {
            return this.ScanEncodingClassReference(firstInput);
        }

        return this.CreateNonIdentifierResult(firstInput);
    }

    internal IdentifierScannerResult ScanEncodingClassReference(
        SourceCursorReadResult hash)
    {
        TriviaScannerResult second = this.ReadInput();
        if (second.Status == TriviaScannerStatus.NeedMoreData)
        {
            this.PushInput(new TriviaScannerResult
            {
                Status = TriviaScannerStatus.NonTrivia,
                Input = hash
            });
            return this.CreateStateResult(
                IdentifierScannerStatus.NeedMoreData);
        }

        if (second.Status == TriviaScannerStatus.NonTrivia
            && second.Input != null
            && this.IsLetter(second.Input))
        {
            this.BeginIdentifier(hash, false);
            this.EncodingClassReference = true;
            this.InvalidEncodingClassReference = second.Input.ScalarValue
                >= 97 && second.Input.ScalarValue <= 122;
            this.AppendNameCharacter(second.Input);
            return this.ScanIdentifierTail();
        }

        if (second.Status != TriviaScannerStatus.EndOfInput)
        {
            this.PushInput(second);
        }

        return this.CreateNonIdentifierResult(hash);
    }

    internal IdentifierScannerResult ScanNextForKind(AsnTokenKind requestedKind)
    {
        IdentifierScannerResult result = this.ScanNext();
        if (result.Status == IdentifierScannerStatus.Token
            && result.Token != null
            && result.CanBeKind(requestedKind))
        {
            result.Token.Kind = requestedKind;
        }

        return result;
    }

    internal IdentifierScannerResult ScanAmpersand(SourceCursorReadResult ampersand)
    {
        TriviaScannerResult second = this.ReadInput();
        if (second.Status == TriviaScannerStatus.NeedMoreData)
        {
            this.PushInput(new TriviaScannerResult
            {
                Status = TriviaScannerStatus.NonTrivia,
                Input = ampersand
            });
            return this.CreateStateResult(IdentifierScannerStatus.NeedMoreData);
        }

        if (second.Status == TriviaScannerStatus.NonTrivia
            && second.Input != null
            && this.IsLetter(second.Input))
        {
            this.BeginIdentifier(ampersand, true);
            this.AppendNameCharacter(second.Input);
            return this.ScanIdentifierTail();
        }

        if (second.Status != TriviaScannerStatus.EndOfInput)
        {
            this.PushInput(second);
        }

        return this.CreateNonIdentifierResult(ampersand);
    }

    internal void BeginIdentifier(SourceCursorReadResult first, bool fieldReference)
    {
        this.Scanning = true;
        this.FieldReference = fieldReference;
        this.LastWasHyphen = false;
        this.HasAdjacentHyphens = false;
        this.HasLowercase = false;
        this.TokenStart = first.Start;
        this.TokenEnd = first.End;
        this.Accumulator.AppendText(first.Text);
        this.NormalizedAccumulator.AppendText(first.Text);
        if (!fieldReference)
        {
            this.AppendCaseInformation(first.ScalarValue);
        }
    }

    internal IdentifierScannerResult ScanIdentifierTail()
    {
        while (true)
        {
            TriviaScannerResult input = this.ReadInput();
            if (input.Status == TriviaScannerStatus.NeedMoreData)
            {
                return this.CreateStateResult(IdentifierScannerStatus.NeedMoreData);
            }

            if (input.Status == TriviaScannerStatus.EndOfInput)
            {
                return this.FinishIdentifier();
            }

            if (input.Status == TriviaScannerStatus.Trivia)
            {
                this.PushInput(input);
                return this.FinishIdentifier();
            }

            SourceCursorReadResult scalar = input.Input == null
                ? throw new System.InvalidOperationException("Non-trivia input is missing.")
                : input.Input;
            if (!this.IsNameCharacter(scalar))
            {
                this.PushInput(input);
                return this.FinishIdentifier();
            }

            this.AppendNameCharacter(scalar);
        }
    }

    internal void AppendNameCharacter(SourceCursorReadResult input)
    {
        bool isHyphen = this.IsHyphen(input.ScalarValue);
        if (isHyphen && this.LastWasHyphen)
        {
            this.HasAdjacentHyphens = true;
        }

        this.LastWasHyphen = isHyphen;
        this.Accumulator.AppendText(input.Text);
        if (isHyphen)
        {
            this.NormalizedAccumulator.AppendCharacter('-');
        }
        else
        {
            this.NormalizedAccumulator.AppendText(input.Text);
            this.AppendCaseInformation(input.ScalarValue);
        }

        this.TokenEnd = input.End;
    }

    internal void AppendCaseInformation(int scalarValue)
    {
        if (scalarValue >= 97 && scalarValue <= 122)
        {
            this.HasLowercase = true;
        }
    }

    internal IdentifierScannerResult FinishIdentifier()
    {
        string text = this.Accumulator.ToText();
        string normalizedText = this.NormalizedAccumulator.ToText();
        AsnSourceSpan span = new AsnSourceSpan
        {
            Start = this.TokenStart,
            End = this.TokenEnd
        };
        AsnTokenKind[] candidates = this.GetCandidateKinds(normalizedText);
        AsnTokenKind kind = candidates[0];

        if (this.HasAdjacentHyphens)
        {
            kind = AsnTokenKind.Invalid;
            candidates = [AsnTokenKind.Invalid];
            this.Diagnostics.TryAddDiagnostic(
                AsnDiagnosticSeverity.Error,
                AsnDiagnosticCategory.Lexical,
                2,
                "A reference or identifier cannot contain adjacent hyphens.",
                span,
                null);
        }
        else if (this.LastWasHyphen)
        {
            kind = AsnTokenKind.Invalid;
            candidates = [AsnTokenKind.Invalid];
            this.Diagnostics.TryAddDiagnostic(
                AsnDiagnosticSeverity.Error,
                AsnDiagnosticCategory.Lexical,
                3,
                "A reference or identifier cannot end with a hyphen.",
                span,
                null);
        }
        else if (this.InvalidEncodingClassReference)
        {
            kind = AsnTokenKind.Invalid;
            candidates = [AsnTokenKind.Invalid];
            this.Diagnostics.TryAddDiagnostic(
                AsnDiagnosticSeverity.Error,
                AsnDiagnosticCategory.Lexical,
                16,
                "An ECN encoding class reference must begin with an upper-case letter after '#'.",
                span,
                null);
        }

        this.Accumulator.Clear();
        this.NormalizedAccumulator.Clear();
        this.Scanning = false;
        this.FieldReference = false;
        this.LastWasHyphen = false;
        this.HasAdjacentHyphens = false;
        this.HasLowercase = false;
        this.EncodingClassReference = false;
        this.InvalidEncodingClassReference = false;

        return new IdentifierScannerResult
        {
            Status = IdentifierScannerStatus.Token,
            Token = new AsnToken
            {
                Kind = kind,
                Text = text,
                Span = span
            },
            CandidateKinds = candidates
        };
    }

    internal AsnTokenKind[] GetCandidateKinds(string normalizedText)
    {
        if (this.EncodingClassReference)
        {
            return [AsnTokenKind.EncodingClassReference];
        }

        if (!this.FieldReference && this.IsReservedWord(normalizedText))
        {
            return [AsnTokenKind.ReservedWord];
        }

        int firstIndex = this.FieldReference ? 1 : 0;
        char first = normalizedText[firstIndex];
        if (first >= 'a' && first <= 'z')
        {
            if (this.FieldReference)
            {
                return [AsnTokenKind.ValueFieldReference, AsnTokenKind.ObjectFieldReference];
            }

            return [AsnTokenKind.Identifier, AsnTokenKind.ValueReference, AsnTokenKind.ObjectReference];
        }

        if (this.FieldReference)
        {
            return [
                AsnTokenKind.TypeFieldReference,
                AsnTokenKind.ValueSetFieldReference,
                AsnTokenKind.ObjectSetFieldReference
            ];
        }

        if (!this.HasLowercase)
        {
            if (this.IsEncodingReference(normalizedText))
            {
                return [
                    AsnTokenKind.ObjectClassReference,
                    AsnTokenKind.EncodingReference,
                    AsnTokenKind.TypeReference,
                    AsnTokenKind.ModuleReference,
                    AsnTokenKind.ObjectSetReference
                ];
            }

            return [
                AsnTokenKind.ObjectClassReference,
                AsnTokenKind.TypeReference,
                AsnTokenKind.ModuleReference,
                AsnTokenKind.ObjectSetReference
            ];
        }

        return [AsnTokenKind.TypeReference, AsnTokenKind.ModuleReference, AsnTokenKind.ObjectSetReference];
    }

    internal bool IsEncodingReference(string text)
    {
        return text == "ECN" || text == "TAG"
            || text == "XER" || text == "PER";
    }

    internal bool IsReservedWord(string text)
    {
        switch (text)
        {
            case "ABSENT":
            case "ABSTRACT-SYNTAX":
            case "ALL":
            case "APPLICATION":
            case "AUTOMATIC":
            case "BEGIN":
            case "BIT":
            case "BMPString":
            case "BOOLEAN":
            case "BY":
            case "CHARACTER":
            case "CHOICE":
            case "CLASS":
            case "COMPONENT":
            case "COMPONENTS":
            case "CONSTRAINED":
            case "CONTAINING":
            case "DATE":
            case "DATE-TIME":
            case "DEFAULT":
            case "DEFINITIONS":
            case "DURATION":
            case "EMBEDDED":
            case "ENCODED":
            case "ENCODING-CONTROL":
            case "ENCODING-CLASS":
            case "ENCODING-DEFINITIONS":
            case "ENCODE":
            case "ENCODE-DECODE":
            case "END":
            case "ENUMERATED":
            case "EXCEPT":
            case "EXPLICIT":
            case "EXPORTS":
            case "FIELDS":
            case "EXTENSIBILITY":
            case "EXTERNAL":
            case "FALSE":
            case "FROM":
            case "GeneralizedTime":
            case "GeneralString":
            case "GraphicString":
            case "IA5String":
            case "IDENTIFIER":
            case "IMPLICIT":
            case "IMPLIED":
            case "IMPORTS":
            case "INCLUDES":
            case "IN":
            case "INSTANCE":
            case "INSTRUCTIONS":
            case "INTEGER":
            case "INTERSECTION":
            case "LINK-DEFINITIONS":
            case "ISO646String":
            case "MAX":
            case "MAPPING":
            case "MIN":
            case "MINUS-INFINITY":
            case "NON-ECN-BEGIN":
            case "NON-ECN-END":
            case "NOT-A-NUMBER":
            case "NULL":
            case "NumericString":
            case "OBJECT":
            case "ObjectDescriptor":
            case "OCTET":
            case "OF":
            case "OID-IRI":
            case "OPTIONAL":
            case "OPTIONAL-ENCODING":
            case "OPTIONS":
            case "ORDERED":
            case "OUTER":
            case "PATTERN":
            case "PER-BASIC-ALIGNED":
            case "PER-BASIC-UNALIGNED":
            case "PER-CANONICAL-ALIGNED":
            case "PER-CANONICAL-UNALIGNED":
            case "PDV":
            case "PLUS-INFINITY":
            case "PRESENT":
            case "REFERENCE":
            case "REMAINDER":
            case "RENAMES":
            case "PrintableString":
            case "PRIVATE":
            case "REAL":
            case "RELATIVE-OID":
            case "RELATIVE-OID-IRI":
            case "SEQUENCE":
            case "SET":
            case "SETTINGS":
            case "SIZE":
            case "STRING":
            case "STRUCTURE":
            case "STRUCTURED":
            case "SYNTAX":
            case "T61String":
            case "TAGS":
            case "TeletexString":
            case "TIME":
            case "TIME-OF-DAY":
            case "TO":
            case "TRANSFORMS":
            case "TRUE":
            case "TYPE-IDENTIFIER":
            case "UNION":
            case "UNIQUE":
            case "UNIVERSAL":
            case "USE":
            case "USE-SET":
            case "UniversalString":
            case "UTCTime":
            case "UTF8String":
            case "VideotexString":
            case "VisibleString":
            case "WITH":
            case "VALUES":
            case "COMPLETED":
            case "DECODE":
            case "DISTRIBUTION":
            case "GENERATES":
            case "IF":
            case "BITS":
            case "AS":
            case "BER":
            case "CER":
            case "DER":
                return true;
            default:
                return false;
        }
    }

    internal bool IsLetter(SourceCursorReadResult input)
    {
        if (input.Status != SourceCursorStatus.Value)
        {
            return false;
        }

        return (input.ScalarValue >= 65 && input.ScalarValue <= 90)
            || (input.ScalarValue >= 97 && input.ScalarValue <= 122);
    }

    internal bool IsNameCharacter(SourceCursorReadResult input)
    {
        if (input.Status != SourceCursorStatus.Value)
        {
            return false;
        }

        return this.IsLetter(input)
            || (input.ScalarValue >= 48 && input.ScalarValue <= 57)
            || this.IsHyphen(input.ScalarValue);
    }

    internal bool IsHyphen(int scalarValue)
    {
        return scalarValue == 45 || scalarValue == 8209;
    }

    internal TriviaScannerResult ReadInput()
    {
        return this.Inputs.ReadInput();
    }

    internal void PushInput(TriviaScannerResult input)
    {
        this.Inputs.PushInput(input);
    }

    internal IdentifierScannerResult CreateTriviaResult(AsnToken? token)
    {
        return new IdentifierScannerResult
        {
            Status = IdentifierScannerStatus.Trivia,
            Token = token
        };
    }

    internal IdentifierScannerResult CreateNonIdentifierResult(SourceCursorReadResult input)
    {
        return new IdentifierScannerResult
        {
            Status = IdentifierScannerStatus.NonIdentifier,
            Input = input
        };
    }

    internal IdentifierScannerResult CreateStateResult(IdentifierScannerStatus status)
    {
        return new IdentifierScannerResult
        {
            Status = status
        };
    }
}
