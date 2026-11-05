namespace asn1;

internal class LiteralScanner
{
    internal LexInputBuffer Inputs;
    internal DiagnosticBag Diagnostics;
    internal TextAccumulator Accumulator;
    internal System.Collections.Generic.List<TriviaScannerResult> TupleInputs;
    internal LiteralScannerState State;
    internal AsnSourcePosition TokenStart = new AsnSourcePosition();
    internal AsnSourcePosition TokenEnd = new AsnSourcePosition();
    internal int IntegerDigitCount;
    internal bool IntegerStartsWithZero;
    internal bool NumberHasDecimalPoint;
    internal bool NumberHasExponent;
    internal int FractionDigitCount;
    internal bool FractionHasNonZero;
    internal int ExponentDigitCount;
    internal bool ExponentSignAllowed;
    internal bool NumberInvalidCharacter;
    internal bool RadixClosingQuote;
    internal bool RadixAllBinary;
    internal bool RadixAllHexadecimal;
    internal bool CharacterQuotePending;
    internal bool CharacterInvalid;
    internal int TupleStage;
    internal int TupleComponentCount;
    internal int TupleDigitCount;
    internal bool TupleComponentStartsWithZero;
    internal bool TupleInvalidNumber;

    internal LiteralScanner(TriviaScanner trivia, DiagnosticBag diagnostics)
    {
        this.Inputs = new LexInputBuffer(trivia);
        this.Diagnostics = diagnostics == null ? throw new System.ArgumentNullException("diagnostics") : diagnostics;
        this.Accumulator = new TextAccumulator(64);
        this.TupleInputs = new System.Collections.Generic.List<TriviaScannerResult>();
    }

    internal void UseInputBuffer(LexInputBuffer inputs)
    {
        this.Inputs = inputs == null ? throw new System.ArgumentNullException("inputs") : inputs;
    }

    internal LiteralScannerResult ScanNext()
    {
        if (this.State == LiteralScannerState.Number)
        {
            return this.ScanNumber();
        }

        if (this.State == LiteralScannerState.RadixString)
        {
            return this.ScanRadixString();
        }

        if (this.State == LiteralScannerState.CharacterString)
        {
            return this.ScanCharacterString();
        }

        if (this.State == LiteralScannerState.Tuple)
        {
            return this.ScanTuple();
        }

        TriviaScannerResult first = this.ReadInput();
        if (first.Status == TriviaScannerStatus.NeedMoreData)
        {
            return this.CreateStateResult(LiteralScannerStatus.NeedMoreData);
        }

        if (first.Status == TriviaScannerStatus.EndOfInput)
        {
            return this.CreateStateResult(LiteralScannerStatus.EndOfInput);
        }

        if (first.Status == TriviaScannerStatus.Trivia)
        {
            return this.CreateTriviaResult(first.Token);
        }

        SourceCursorReadResult input = this.GetInput(first);
        if (this.IsDigit(input.ScalarValue))
        {
            this.BeginToken(LiteralScannerState.Number, input);
            this.IntegerDigitCount = 1;
            this.IntegerStartsWithZero = input.ScalarValue == 48;
            return this.ScanNumber();
        }

        if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 39)
        {
            this.BeginToken(LiteralScannerState.RadixString, input);
            this.RadixAllBinary = true;
            this.RadixAllHexadecimal = true;
            return this.ScanRadixString();
        }

        if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 34)
        {
            this.BeginToken(LiteralScannerState.CharacterString, input);
            return this.ScanCharacterString();
        }

        if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 123)
        {
            this.BeginTuple(first);
            return this.ScanTuple();
        }

        return this.CreateNonLiteralResult(input);
    }

    internal LiteralScannerResult ScanNextForKind(AsnTokenKind requestedKind)
    {
        LiteralScannerResult result = this.ScanNext();
        if (result.Status == LiteralScannerStatus.Token
            && result.Token != null
            && result.CanBeKind(requestedKind))
        {
            result.Token.Kind = requestedKind;
        }

        return result;
    }

    internal void BeginToken(LiteralScannerState state, SourceCursorReadResult first)
    {
        this.State = state;
        this.TokenStart = first.Start;
        this.TokenEnd = first.End;
        this.Accumulator.AppendText(first.Text);
    }

    internal LiteralScannerResult ScanNumber()
    {
        while (true)
        {
            TriviaScannerResult next = this.ReadInput();
            if (next.Status == TriviaScannerStatus.NeedMoreData)
            {
                return this.CreateStateResult(LiteralScannerStatus.NeedMoreData);
            }

            if (next.Status == TriviaScannerStatus.EndOfInput)
            {
                return this.FinishNumber();
            }

            if (next.Status == TriviaScannerStatus.Trivia)
            {
                this.PushInput(next);
                return this.FinishNumber();
            }

            SourceCursorReadResult input = this.GetInput(next);
            if (this.IsDigit(input.ScalarValue))
            {
                this.AppendInput(input);
                if (this.NumberHasExponent)
                {
                    this.ExponentDigitCount = this.ExponentDigitCount + 1;
                    this.ExponentSignAllowed = false;
                }
                else if (this.NumberHasDecimalPoint)
                {
                    this.FractionDigitCount = this.FractionDigitCount + 1;
                    if (input.ScalarValue != 48)
                    {
                        this.FractionHasNonZero = true;
                    }
                }
                else
                {
                    this.IntegerDigitCount = this.IntegerDigitCount + 1;
                }

                continue;
            }

            if (input.Status == SourceCursorStatus.Value
                && input.ScalarValue == 46
                && !this.NumberHasDecimalPoint
                && !this.NumberHasExponent)
            {
                LiteralScannerResult? dotResult = this.ScanNumberDot(next, input);
                if (dotResult != null)
                {
                    return dotResult;
                }

                continue;
            }

            if (input.Status == SourceCursorStatus.Value
                && (input.ScalarValue == 69 || input.ScalarValue == 101)
                && !this.NumberHasExponent)
            {
                this.AppendInput(input);
                this.NumberHasExponent = true;
                this.ExponentSignAllowed = true;
                continue;
            }

            if (input.Status == SourceCursorStatus.Value
                && (input.ScalarValue == 43 || input.ScalarValue == 45)
                && this.NumberHasExponent
                && this.ExponentSignAllowed)
            {
                this.AppendInput(input);
                this.ExponentSignAllowed = false;
                continue;
            }

            if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 95)
            {
                this.AppendInput(input);
                this.NumberInvalidCharacter = true;
                return this.FinishNumber();
            }

            this.PushInput(next);
            return this.FinishNumber();
        }
    }

    internal LiteralScannerResult? ScanNumberDot(
        TriviaScannerResult dotResult,
        SourceCursorReadResult dotInput)
    {
        TriviaScannerResult lookahead = this.ReadInput();
        if (lookahead.Status == TriviaScannerStatus.NeedMoreData)
        {
            this.PushInput(dotResult);
            return this.CreateStateResult(LiteralScannerStatus.NeedMoreData);
        }

        if (lookahead.Status == TriviaScannerStatus.NonTrivia
            && lookahead.Input != null
            && lookahead.Input.Status == SourceCursorStatus.Value
            && lookahead.Input.ScalarValue == 46)
        {
            this.PushInput(lookahead);
            this.PushInput(dotResult);
            return this.FinishNumber();
        }

        this.AppendInput(dotInput);
        this.NumberHasDecimalPoint = true;
        if (lookahead.Status != TriviaScannerStatus.EndOfInput)
        {
            this.PushInput(lookahead);
        }

        if (lookahead.Status == TriviaScannerStatus.EndOfInput)
        {
            return this.FinishNumber();
        }

        return null;
    }

    internal LiteralScannerResult FinishNumber()
    {
        bool invalid = this.NumberInvalidCharacter || this.IntegerDigitCount > 1 && this.IntegerStartsWithZero;
        string message = "A numeric literal contains an invalid character or leading zero.";
        int code = 4;
        if (this.NumberHasExponent && this.ExponentDigitCount == 0)
        {
            invalid = true;
            message = "A real-number exponent requires at least one digit.";
            code = 5;
        }
        else if (this.IntegerStartsWithZero
            && this.NumberHasDecimalPoint
            && !this.FractionHasNonZero)
        {
            invalid = true;
            message = "A real number beginning with zero requires a non-zero fractional digit.";
            code = 4;
        }

        AsnTokenKind kind = this.NumberHasDecimalPoint || this.NumberHasExponent
            ? AsnTokenKind.RealLiteral
            : AsnTokenKind.IntegerLiteral;
        AsnTokenKind[] candidates = kind == AsnTokenKind.IntegerLiteral
            ? [AsnTokenKind.IntegerLiteral, AsnTokenKind.RealLiteral]
            : [AsnTokenKind.RealLiteral];
        if (invalid)
        {
            kind = AsnTokenKind.Invalid;
            candidates = [AsnTokenKind.Invalid];
            this.AddDiagnostic(code, message);
        }

        return this.FinishToken(kind, candidates);
    }

    internal LiteralScannerResult ScanRadixString()
    {
        while (true)
        {
            TriviaScannerResult next = this.ReadInput();
            if (next.Status == TriviaScannerStatus.NeedMoreData)
            {
                return this.CreateStateResult(LiteralScannerStatus.NeedMoreData);
            }

            if (next.Status == TriviaScannerStatus.EndOfInput)
            {
                if (this.RadixClosingQuote)
                {
                    this.AddDiagnostic(8, "A binary or hexadecimal string requires an immediate B or H suffix.");
                }
                else
                {
                    this.AddDiagnostic(7, "A binary or hexadecimal string is unterminated.");
                }

                return this.FinishToken(AsnTokenKind.Invalid, [AsnTokenKind.Invalid]);
            }

            if (this.RadixClosingQuote)
            {
                if (next.Status == TriviaScannerStatus.Trivia)
                {
                    this.PushInput(next);
                    this.AddDiagnostic(8, "A binary or hexadecimal string requires an immediate B or H suffix.");
                    return this.FinishToken(AsnTokenKind.Invalid, [AsnTokenKind.Invalid]);
                }

                SourceCursorReadResult suffix = this.GetInput(next);
                this.AppendInput(suffix);
                if (suffix.Status != SourceCursorStatus.Value
                    || (suffix.ScalarValue != 66 && suffix.ScalarValue != 72))
                {
                    this.AddDiagnostic(8, "A binary or hexadecimal string requires an immediate B or H suffix.");
                    return this.FinishToken(AsnTokenKind.Invalid, [AsnTokenKind.Invalid]);
                }

                AsnTokenKind kind = suffix.ScalarValue == 66
                    ? AsnTokenKind.BinaryStringLiteral
                    : AsnTokenKind.HexStringLiteral;
                bool validContent = suffix.ScalarValue == 66
                    ? this.RadixAllBinary
                    : this.RadixAllHexadecimal;
                if (!validContent)
                {
                    this.AddDiagnostic(
                        6,
                        suffix.ScalarValue == 66
                            ? "A binary string can contain only zero, one, and white-space."
                            : "A hexadecimal string can contain only upper-case hexadecimal digits and white-space.");
                    return this.FinishToken(AsnTokenKind.Invalid, [AsnTokenKind.Invalid]);
                }

                return this.FinishToken(kind, [kind]);
            }

            if (next.Status == TriviaScannerStatus.Trivia)
            {
                AsnToken trivia = next.Token == null
                    ? throw new System.InvalidOperationException("Trivia token is missing.")
                    : next.Token;
                this.AppendToken(trivia);
                if (trivia.Kind != AsnTokenKind.Whitespace)
                {
                    this.RadixAllBinary = false;
                    this.RadixAllHexadecimal = false;
                }

                continue;
            }

            SourceCursorReadResult input = this.GetInput(next);
            this.AppendInput(input);
            if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 39)
            {
                this.RadixClosingQuote = true;
                continue;
            }

            if (!this.IsDigit(input.ScalarValue)
                || (input.ScalarValue != 48 && input.ScalarValue != 49))
            {
                this.RadixAllBinary = false;
            }

            if (!this.IsDigit(input.ScalarValue)
                && (input.ScalarValue < 65 || input.ScalarValue > 70))
            {
                this.RadixAllHexadecimal = false;
            }
        }
    }

    internal LiteralScannerResult ScanCharacterString()
    {
        while (true)
        {
            TriviaScannerResult next = this.ReadInput();
            if (next.Status == TriviaScannerStatus.NeedMoreData)
            {
                return this.CreateStateResult(LiteralScannerStatus.NeedMoreData);
            }

            if (next.Status == TriviaScannerStatus.EndOfInput)
            {
                if (!this.CharacterQuotePending)
                {
                    this.AddDiagnostic(10, "A quoted character string is unterminated.");
                    return this.FinishToken(AsnTokenKind.Invalid, [AsnTokenKind.Invalid]);
                }

                return this.FinishCharacterString();
            }

            if (this.CharacterQuotePending)
            {
                if (next.Status == TriviaScannerStatus.NonTrivia
                    && next.Input != null
                    && next.Input.Status == SourceCursorStatus.Value
                    && next.Input.ScalarValue == 34)
                {
                    this.AppendInput(next.Input);
                    this.CharacterQuotePending = false;
                    continue;
                }

                this.PushInput(next);
                return this.FinishCharacterString();
            }

            if (next.Status == TriviaScannerStatus.Trivia)
            {
                AsnToken trivia = next.Token == null
                    ? throw new System.InvalidOperationException("Trivia token is missing.")
                    : next.Token;
                this.AppendToken(trivia);
                continue;
            }

            SourceCursorReadResult input = this.GetInput(next);
            this.AppendInput(input);
            if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 34)
            {
                this.CharacterQuotePending = true;
            }
            else if (!this.IsCharacterStringScalar(input))
            {
                this.CharacterInvalid = true;
            }
        }
    }

    internal LiteralScannerResult FinishCharacterString()
    {
        if (this.CharacterInvalid)
        {
            this.AddDiagnostic(9, "A quoted character string contains a non-graphic control character.");
            return this.FinishToken(AsnTokenKind.Invalid, [AsnTokenKind.Invalid]);
        }

        return this.FinishToken(
            AsnTokenKind.CharacterStringLiteral,
            [AsnTokenKind.CharacterStringLiteral]);
    }

    internal void BeginTuple(TriviaScannerResult first)
    {
        SourceCursorReadResult input = this.GetInput(first);
        this.BeginToken(LiteralScannerState.Tuple, input);
        this.TupleInputs.Add(first);
        this.TupleStage = 0;
    }

    internal LiteralScannerResult ScanTuple()
    {
        while (true)
        {
            TriviaScannerResult next = this.ReadInput();
            if (next.Status == TriviaScannerStatus.NeedMoreData)
            {
                return this.CreateStateResult(LiteralScannerStatus.NeedMoreData);
            }

            if (next.Status == TriviaScannerStatus.EndOfInput)
            {
                return this.RollBackTuple();
            }

            this.TupleInputs.Add(next);
            this.AppendTupleInput(next);
            if (next.Status == TriviaScannerStatus.Trivia)
            {
                if (this.TupleStage == 1)
                {
                    this.FinishTupleComponent();
                    this.TupleStage = 2;
                }

                continue;
            }

            SourceCursorReadResult input = this.GetInput(next);
            if (this.IsDigit(input.ScalarValue))
            {
                if (this.TupleStage == 2)
                {
                    return this.RollBackTuple();
                }

                if (this.TupleStage == 0)
                {
                    this.TupleStage = 1;
                    this.TupleDigitCount = 1;
                    this.TupleComponentStartsWithZero = input.ScalarValue == 48;
                }
                else
                {
                    this.TupleDigitCount = this.TupleDigitCount + 1;
                }

                continue;
            }

            if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 44)
            {
                if (this.TupleStage == 0)
                {
                    return this.RollBackTuple();
                }

                if (this.TupleStage == 1)
                {
                    this.FinishTupleComponent();
                }

                this.TupleStage = 0;
                continue;
            }

            if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 125)
            {
                if (this.TupleStage == 0)
                {
                    return this.RollBackTuple();
                }

                if (this.TupleStage == 1)
                {
                    this.FinishTupleComponent();
                }

                if (this.TupleComponentCount != 2 && this.TupleComponentCount != 4)
                {
                    return this.RollBackTuple();
                }

                AsnTokenKind kind = this.TupleComponentCount == 2
                    ? AsnTokenKind.TupleLiteral
                    : AsnTokenKind.QuadrupleLiteral;
                if (this.TupleInvalidNumber)
                {
                    this.AddDiagnostic(11, "A tuple or quadruple component cannot contain a leading zero.");
                    return this.FinishTupleToken(AsnTokenKind.Invalid, [AsnTokenKind.Invalid]);
                }

                return this.FinishTupleToken(kind, [kind]);
            }

            return this.RollBackTuple();
        }
    }

    internal void FinishTupleComponent()
    {
        if (this.TupleComponentStartsWithZero && this.TupleDigitCount > 1)
        {
            this.TupleInvalidNumber = true;
        }

        this.TupleComponentCount = this.TupleComponentCount + 1;
        this.TupleDigitCount = 0;
        this.TupleComponentStartsWithZero = false;
    }

    internal LiteralScannerResult FinishTupleToken(AsnTokenKind kind, AsnTokenKind[] candidates)
    {
        this.TupleInputs.Clear();
        return this.FinishToken(kind, candidates);
    }

    internal LiteralScannerResult RollBackTuple()
    {
        int index = this.TupleInputs.Count - 1;
        while (index >= 1)
        {
            this.PushInput(this.TupleInputs[index]);
            index = index - 1;
        }

        TriviaScannerResult first = this.TupleInputs[0];
        SourceCursorReadResult input = this.GetInput(first);
        this.TupleInputs.Clear();
        this.ResetTokenState();
        return this.CreateNonLiteralResult(input);
    }

    internal void AppendTupleInput(TriviaScannerResult result)
    {
        if (result.Status == TriviaScannerStatus.Trivia)
        {
            AsnToken token = result.Token == null
                ? throw new System.InvalidOperationException("Trivia token is missing.")
                : result.Token;
            this.AppendToken(token);
            return;
        }

        this.AppendInput(this.GetInput(result));
    }

    internal void AppendInput(SourceCursorReadResult input)
    {
        this.Accumulator.AppendText(input.Text);
        this.TokenEnd = input.End;
    }

    internal void AppendToken(AsnToken token)
    {
        this.Accumulator.AppendText(token.Text);
        if (token.Span != null)
        {
            this.TokenEnd = token.Span.End;
        }
    }

    internal bool IsDigit(int scalarValue)
    {
        return scalarValue >= 48 && scalarValue <= 57;
    }

    internal bool IsCharacterStringScalar(SourceCursorReadResult input)
    {
        if (input.Status != SourceCursorStatus.Value)
        {
            return false;
        }

        return input.IsLineBreak
            || input.ScalarValue == 9
            || input.ScalarValue >= 32 && input.ScalarValue <= 126
            || input.ScalarValue >= 160;
    }

    internal SourceCursorReadResult GetInput(TriviaScannerResult result)
    {
        return result.Input == null
            ? throw new System.InvalidOperationException("Non-trivia input is missing.")
            : result.Input;
    }

    internal TriviaScannerResult ReadInput()
    {
        return this.Inputs.ReadInput();
    }

    internal void PushInput(TriviaScannerResult input)
    {
        this.Inputs.PushInput(input);
    }

    internal void AddDiagnostic(int code, string message)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Lexical,
            code,
            message,
            new AsnSourceSpan
            {
                Start = this.TokenStart,
                End = this.TokenEnd
            },
            null);
    }

    internal LiteralScannerResult FinishToken(AsnTokenKind kind, AsnTokenKind[] candidates)
    {
        AsnToken token = new AsnToken
        {
            Kind = kind,
            Text = this.Accumulator.ToText(),
            Span = new AsnSourceSpan
            {
                Start = this.TokenStart,
                End = this.TokenEnd
            }
        };
        this.ResetTokenState();
        return new LiteralScannerResult
        {
            Status = LiteralScannerStatus.Token,
            Token = token,
            CandidateKinds = candidates
        };
    }

    internal void ResetTokenState()
    {
        this.Accumulator.Clear();
        this.State = LiteralScannerState.Idle;
        this.IntegerDigitCount = 0;
        this.IntegerStartsWithZero = false;
        this.NumberHasDecimalPoint = false;
        this.NumberHasExponent = false;
        this.FractionDigitCount = 0;
        this.FractionHasNonZero = false;
        this.ExponentDigitCount = 0;
        this.ExponentSignAllowed = false;
        this.NumberInvalidCharacter = false;
        this.RadixClosingQuote = false;
        this.RadixAllBinary = false;
        this.RadixAllHexadecimal = false;
        this.CharacterQuotePending = false;
        this.CharacterInvalid = false;
        this.TupleStage = 0;
        this.TupleComponentCount = 0;
        this.TupleDigitCount = 0;
        this.TupleComponentStartsWithZero = false;
        this.TupleInvalidNumber = false;
    }

    internal LiteralScannerResult CreateTriviaResult(AsnToken? token)
    {
        return new LiteralScannerResult
        {
            Status = LiteralScannerStatus.Trivia,
            Token = token
        };
    }

    internal LiteralScannerResult CreateNonLiteralResult(SourceCursorReadResult input)
    {
        return new LiteralScannerResult
        {
            Status = LiteralScannerStatus.NonLiteral,
            Input = input
        };
    }

    internal LiteralScannerResult CreateStateResult(LiteralScannerStatus status)
    {
        return new LiteralScannerResult
        {
            Status = status
        };
    }
}
