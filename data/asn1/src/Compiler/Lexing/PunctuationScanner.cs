namespace asn1;

internal class PunctuationScanner
{
    internal LexInputBuffer Inputs;
    internal DiagnosticBag Diagnostics;

    internal PunctuationScanner(TriviaScanner trivia, DiagnosticBag diagnostics)
    {
        this.Inputs = new LexInputBuffer(trivia);
        this.Diagnostics = diagnostics == null ? throw new System.ArgumentNullException("diagnostics") : diagnostics;
    }

    internal void UseInputBuffer(LexInputBuffer inputs)
    {
        this.Inputs = inputs == null ? throw new System.ArgumentNullException("inputs") : inputs;
    }

    internal PunctuationScannerResult ScanNext()
    {
        TriviaScannerResult first = this.ReadInput();
        if (first.Status == TriviaScannerStatus.NeedMoreData)
        {
            return this.CreateStateResult(PunctuationScannerStatus.NeedMoreData);
        }

        if (first.Status == TriviaScannerStatus.EndOfInput)
        {
            return this.CreateStateResult(PunctuationScannerStatus.EndOfInput);
        }

        if (first.Status == TriviaScannerStatus.Trivia)
        {
            return new PunctuationScannerResult
            {
                Status = PunctuationScannerStatus.Trivia,
                Token = first.Token
            };
        }

        SourceCursorReadResult input = this.GetInput(first);
        if (input.Status != SourceCursorStatus.Value)
        {
            return this.CreateInvalidResult(input);
        }

        switch (input.ScalarValue)
        {
            case 123:
                return this.CreateTokenResult(AsnTokenKind.LeftBrace, input, input);
            case 125:
                return this.CreateTokenResult(AsnTokenKind.RightBrace, input, input);
            case 40:
                return this.CreateTokenResult(AsnTokenKind.LeftParenthesis, input, input);
            case 41:
                return this.CreateTokenResult(AsnTokenKind.RightParenthesis, input, input);
            case 91:
                return this.ScanBracket(first, input, true);
            case 93:
                return this.ScanBracket(first, input, false);
            case 60:
                return this.CreateTokenResult(AsnTokenKind.LessThan, input, input);
            case 62:
                return this.CreateTokenResult(AsnTokenKind.GreaterThan, input, input);
            case 44:
                return this.CreateTokenResult(AsnTokenKind.Comma, input, input);
            case 59:
                return this.CreateTokenResult(AsnTokenKind.Semicolon, input, input);
            case 58:
                return this.ScanColon(first, input);
            case 46:
                return this.ScanDot(first, input);
            case 33:
                return this.CreateTokenResult(AsnTokenKind.ExclamationMark, input, input);
            case 64:
                return this.CreateTokenResult(AsnTokenKind.AtSign, input, input);
            case 124:
                return this.CreateTokenResult(AsnTokenKind.VerticalBar, input, input);
            case 94:
                return this.CreateTokenResult(AsnTokenKind.Caret, input, input);
            case 38:
                return this.CreateTokenResult(AsnTokenKind.Ampersand, input, input);
            case 43:
                return this.CreateTokenResult(AsnTokenKind.Plus, input, input);
            case 45:
                return this.CreateTokenResult(AsnTokenKind.Minus, input, input);
            case 42:
                return this.CreateTokenResult(AsnTokenKind.Asterisk, input, input);
            case 47:
                return this.CreateTokenResult(AsnTokenKind.Slash, input, input);
            case 61:
                return this.CreateTokenResult(AsnTokenKind.Equals, input, input);
            default:
                if (this.IsOtherScannerStart(input.ScalarValue))
                {
                    return new PunctuationScannerResult
                    {
                        Status = PunctuationScannerStatus.NonPunctuation,
                        Input = input
                    };
                }

                return this.CreateInvalidResult(input);
        }
    }

    internal PunctuationScannerResult ScanColon(
        TriviaScannerResult firstResult,
        SourceCursorReadResult first)
    {
        TriviaScannerResult secondResult = this.ReadInput();
        if (secondResult.Status == TriviaScannerStatus.NeedMoreData)
        {
            this.PushInput(firstResult);
            return this.CreateStateResult(PunctuationScannerStatus.NeedMoreData);
        }

        if (!this.IsScalar(secondResult, 58))
        {
            this.PushIfPresent(secondResult);
            return this.CreateTokenResult(AsnTokenKind.Colon, first, first);
        }

        TriviaScannerResult thirdResult = this.ReadInput();
        if (thirdResult.Status == TriviaScannerStatus.NeedMoreData)
        {
            this.PushInput(secondResult);
            this.PushInput(firstResult);
            return this.CreateStateResult(PunctuationScannerStatus.NeedMoreData);
        }

        if (!this.IsScalar(thirdResult, 61))
        {
            this.PushIfPresent(thirdResult);
            this.PushInput(secondResult);
            return this.CreateTokenResult(AsnTokenKind.Colon, first, first);
        }

        SourceCursorReadResult third = this.GetInput(thirdResult);
        return this.CreateTokenResult(AsnTokenKind.Assignment, first, third);
    }

    internal PunctuationScannerResult ScanDot(
        TriviaScannerResult firstResult,
        SourceCursorReadResult first)
    {
        TriviaScannerResult secondResult = this.ReadInput();
        if (secondResult.Status == TriviaScannerStatus.NeedMoreData)
        {
            this.PushInput(firstResult);
            return this.CreateStateResult(PunctuationScannerStatus.NeedMoreData);
        }

        if (!this.IsScalar(secondResult, 46))
        {
            this.PushIfPresent(secondResult);
            return this.CreateTokenResult(AsnTokenKind.Dot, first, first);
        }

        TriviaScannerResult thirdResult = this.ReadInput();
        if (thirdResult.Status == TriviaScannerStatus.NeedMoreData)
        {
            this.PushInput(secondResult);
            this.PushInput(firstResult);
            return this.CreateStateResult(PunctuationScannerStatus.NeedMoreData);
        }

        SourceCursorReadResult second = this.GetInput(secondResult);
        if (!this.IsScalar(thirdResult, 46))
        {
            this.PushIfPresent(thirdResult);
            return this.CreateTokenResult(AsnTokenKind.Range, first, second);
        }

        SourceCursorReadResult third = this.GetInput(thirdResult);
        return this.CreateTokenResult(AsnTokenKind.Ellipsis, first, third);
    }

    internal PunctuationScannerResult ScanBracket(
        TriviaScannerResult firstResult,
        SourceCursorReadResult first,
        bool left)
    {
        TriviaScannerResult secondResult = this.ReadInput();
        if (secondResult.Status == TriviaScannerStatus.NeedMoreData)
        {
            this.PushInput(firstResult);
            return this.CreateStateResult(PunctuationScannerStatus.NeedMoreData);
        }

        int expected = left ? 91 : 93;
        if (!this.IsScalar(secondResult, expected))
        {
            this.PushIfPresent(secondResult);
            return this.CreateTokenResult(
                left ? AsnTokenKind.LeftBracket : AsnTokenKind.RightBracket,
                first,
                first);
        }

        SourceCursorReadResult second = this.GetInput(secondResult);
        return this.CreateTokenResult(
            left ? AsnTokenKind.LeftVersionBracket : AsnTokenKind.RightVersionBracket,
            first,
            second);
    }

    internal bool IsScalar(TriviaScannerResult result, int scalarValue)
    {
        return result.Status == TriviaScannerStatus.NonTrivia
            && result.Input != null
            && result.Input.Status == SourceCursorStatus.Value
            && result.Input.ScalarValue == scalarValue;
    }

    internal bool IsOtherScannerStart(int scalarValue)
    {
        return scalarValue >= 48 && scalarValue <= 57
            || scalarValue >= 65 && scalarValue <= 90
            || scalarValue >= 97 && scalarValue <= 122
            || scalarValue == 34
            || scalarValue == 39;
    }

    internal PunctuationScannerResult CreateInvalidResult(SourceCursorReadResult input)
    {
        AsnSourceSpan span = new AsnSourceSpan
        {
            Start = input.Start,
            End = input.End
        };
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Lexical,
            14,
            "The character does not form an ASN.1 lexical item in this context.",
            span,
            null);
        return new PunctuationScannerResult
        {
            Status = PunctuationScannerStatus.Token,
            Token = new AsnToken
            {
                Kind = AsnTokenKind.Invalid,
                Text = input.Text,
                Span = span
            }
        };
    }

    internal PunctuationScannerResult CreateTokenResult(
        AsnTokenKind kind,
        SourceCursorReadResult first,
        SourceCursorReadResult last)
    {
        string text = first == last ? first.Text : first.Text + last.Text;
        if (kind == AsnTokenKind.Assignment)
        {
            text = "::=";
        }
        else if (kind == AsnTokenKind.Ellipsis)
        {
            text = "...";
        }

        return new PunctuationScannerResult
        {
            Status = PunctuationScannerStatus.Token,
            Token = new AsnToken
            {
                Kind = kind,
                Text = text,
                Span = new AsnSourceSpan
                {
                    Start = first.Start,
                    End = last.End
                }
            }
        };
    }

    internal void PushIfPresent(TriviaScannerResult input)
    {
        if (input.Status != TriviaScannerStatus.EndOfInput)
        {
            this.PushInput(input);
        }
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

    internal PunctuationScannerResult CreateStateResult(PunctuationScannerStatus status)
    {
        return new PunctuationScannerResult
        {
            Status = status
        };
    }
}
