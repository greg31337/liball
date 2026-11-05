namespace asn1;

internal class XmlValueScanner
{
    internal LexInputBuffer Inputs;
    internal DiagnosticBag Diagnostics;
    internal TextAccumulator Accumulator;
    internal TextAccumulator EntityAccumulator;
    internal System.Collections.Generic.List<TriviaScannerResult> AttemptInputs;
    internal bool Scanning;
    internal bool Committed;
    internal bool InsideTag;
    internal bool ClosingTag;
    internal int TagMarkerStage;
    internal int TagQuote;
    internal bool LastTagCharacterWasSlash;
    internal int Depth;
    internal bool EntityOpen;
    internal bool LastTextCharacterWasHyphen;
    internal bool Invalid;
    internal bool DiagnosticAdded;
    internal AsnSourcePosition TokenStart = new AsnSourcePosition();
    internal AsnSourcePosition TokenEnd = new AsnSourcePosition();

    internal XmlValueScanner(TriviaScanner trivia, DiagnosticBag diagnostics)
    {
        this.Inputs = new LexInputBuffer(trivia);
        this.Diagnostics = diagnostics == null ? throw new System.ArgumentNullException("diagnostics") : diagnostics;
        this.Accumulator = new TextAccumulator(128);
        this.EntityAccumulator = new TextAccumulator(16);
        this.AttemptInputs = new System.Collections.Generic.List<TriviaScannerResult>();
    }

    internal void UseInputBuffer(LexInputBuffer inputs)
    {
        this.Inputs = inputs == null ? throw new System.ArgumentNullException("inputs") : inputs;
    }

    internal XmlValueScannerResult ScanNext()
    {
        if (this.Scanning)
        {
            return this.ScanXmlValue();
        }

        TriviaScannerResult first = this.ReadInput();
        if (first.Status == TriviaScannerStatus.NeedMoreData)
        {
            return this.CreateStateResult(XmlValueScannerStatus.NeedMoreData);
        }

        if (first.Status == TriviaScannerStatus.EndOfInput)
        {
            return this.CreateStateResult(XmlValueScannerStatus.EndOfInput);
        }

        if (first.Status == TriviaScannerStatus.Trivia)
        {
            return this.CreateTriviaResult(first.Token);
        }

        SourceCursorReadResult input = this.GetInput(first);
        if (input.Status != SourceCursorStatus.Value || input.ScalarValue != 60)
        {
            return this.CreateNonXmlResult(input);
        }

        this.Scanning = true;
        this.InsideTag = true;
        this.TagMarkerStage = 0;
        this.TokenStart = input.Start;
        this.TokenEnd = input.End;
        this.Accumulator.AppendText(input.Text);
        this.AttemptInputs.Add(first);
        return this.ScanXmlValue();
    }

    internal XmlValueScannerResult ScanXmlValue()
    {
        while (true)
        {
            TriviaScannerResult next = this.ReadInput();
            if (next.Status == TriviaScannerStatus.NeedMoreData)
            {
                return this.CreateStateResult(XmlValueScannerStatus.NeedMoreData);
            }

            if (next.Status == TriviaScannerStatus.EndOfInput)
            {
                if (!this.Committed)
                {
                    return this.RollBackAttempt();
                }

                this.Invalid = true;
                this.AddDiagnostic(12, "An XML value is unterminated.");
                this.DiagnosticAdded = true;
                return this.FinishXmlValue();
            }

            if (!this.Committed)
            {
                this.AttemptInputs.Add(next);
            }

            if (next.Status == TriviaScannerStatus.Trivia)
            {
                AsnToken token = next.Token == null
                    ? throw new System.InvalidOperationException("Trivia token is missing.")
                    : next.Token;
                this.AppendToken(token);
                if (this.InsideTag && this.TagMarkerStage < 2)
                {
                    if (!this.Committed)
                    {
                        return this.RollBackAttempt();
                    }

                    this.Invalid = true;
                }

                if (token.Kind == AsnTokenKind.Comment)
                {
                    this.Invalid = true;
                    if (this.Committed)
                    {
                        this.AddDiagnostic(13, "ASN.1 comments cannot occur inside XML value notation.");
                        this.DiagnosticAdded = true;
                    }
                }

                this.LastTagCharacterWasSlash = false;
                this.LastTextCharacterWasHyphen = false;
                continue;
            }

            SourceCursorReadResult input = this.GetInput(next);
            this.AppendInput(input);
            int action = this.ProcessScalar(input);
            if (action == 1)
            {
                return this.FinishXmlValue();
            }

            if (action == 2)
            {
                return this.RollBackAttempt();
            }
        }
    }

    internal int ProcessScalar(SourceCursorReadResult input)
    {
        if (!this.IsXmlCharacter(input))
        {
            this.Invalid = true;
        }

        if (this.EntityOpen)
        {
            return this.ProcessEntityScalar(input);
        }

        if (this.InsideTag)
        {
            return this.ProcessTagScalar(input);
        }

        if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 60)
        {
            this.InsideTag = true;
            this.ClosingTag = false;
            this.TagMarkerStage = 0;
            this.TagQuote = 0;
            this.LastTagCharacterWasSlash = false;
            this.LastTextCharacterWasHyphen = false;
            return 0;
        }

        if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 38)
        {
            this.EntityOpen = true;
            this.EntityAccumulator.Clear();
            this.EntityAccumulator.AppendCharacter('&');
            this.LastTextCharacterWasHyphen = false;
            return 0;
        }

        if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 62)
        {
            this.Invalid = true;
        }

        if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 45)
        {
            if (this.LastTextCharacterWasHyphen)
            {
                this.Invalid = true;
            }

            this.LastTextCharacterWasHyphen = true;
        }
        else
        {
            this.LastTextCharacterWasHyphen = false;
        }

        return 0;
    }

    internal int ProcessTagScalar(SourceCursorReadResult input)
    {
        if (this.TagQuote != 0)
        {
            if (input.Status == SourceCursorStatus.Value && input.ScalarValue == this.TagQuote)
            {
                this.TagQuote = 0;
            }
            else if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 38)
            {
                this.EntityOpen = true;
                this.EntityAccumulator.Clear();
                this.EntityAccumulator.AppendCharacter('&');
            }

            return 0;
        }

        if (this.TagMarkerStage == 0)
        {
            if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 47)
            {
                if (!this.Committed)
                {
                    return 2;
                }

                this.ClosingTag = true;
                this.TagMarkerStage = 1;
                return 0;
            }

            if (!this.IsXmlNameStart(input.ScalarValue))
            {
                if (!this.Committed)
                {
                    return 2;
                }

                this.Invalid = true;
            }

            this.TagMarkerStage = 2;
        }
        else if (this.TagMarkerStage == 1)
        {
            if (!this.IsXmlNameStart(input.ScalarValue))
            {
                this.Invalid = true;
            }

            this.TagMarkerStage = 2;
        }

        if (input.Status == SourceCursorStatus.Value
            && (input.ScalarValue == 34 || input.ScalarValue == 39))
        {
            this.TagQuote = input.ScalarValue;
            this.LastTagCharacterWasSlash = false;
            return 0;
        }

        if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 60)
        {
            this.Invalid = true;
            this.LastTagCharacterWasSlash = false;
            return 0;
        }

        if (input.Status == SourceCursorStatus.Value && input.ScalarValue == 62)
        {
            bool selfClosing = this.LastTagCharacterWasSlash;
            if (this.ClosingTag)
            {
                if (selfClosing)
                {
                    this.Invalid = true;
                }

                this.Depth = this.Depth - 1;
            }
            else if (!selfClosing)
            {
                this.Depth = this.Depth + 1;
            }

            this.InsideTag = false;
            this.LastTagCharacterWasSlash = false;
            this.LastTextCharacterWasHyphen = false;
            if (!this.Committed)
            {
                this.Committed = true;
                this.AttemptInputs.Clear();
            }

            if (this.Depth == 0)
            {
                return 1;
            }

            if (this.Depth < 0)
            {
                this.Invalid = true;
                return 1;
            }

            return 0;
        }

        this.LastTagCharacterWasSlash = input.Status == SourceCursorStatus.Value
            && input.ScalarValue == 47;
        return 0;
    }

    internal int ProcessEntityScalar(SourceCursorReadResult input)
    {
        if (input.Status != SourceCursorStatus.Value
            || input.IsLineBreak
            || input.ScalarValue == 9
            || input.ScalarValue == 32
            || input.ScalarValue == 60
            || input.ScalarValue == 62)
        {
            this.Invalid = true;
            this.EntityOpen = false;
            this.EntityAccumulator.Clear();
            return this.InsideTag ? this.ProcessTagScalar(input) : this.ProcessScalar(input);
        }

        this.EntityAccumulator.AppendText(input.Text);
        if (input.ScalarValue == 59)
        {
            if (!this.IsValidEntity(this.EntityAccumulator.ToText()))
            {
                this.Invalid = true;
            }

            this.EntityOpen = false;
            this.EntityAccumulator.Clear();
        }
        else if (this.EntityAccumulator.Count > 24)
        {
            this.Invalid = true;
        }

        return 0;
    }

    internal bool IsValidEntity(string text)
    {
        if (text == "&amp;"
            || text == "&lt;"
            || text == "&gt;"
            || text == "&quot;"
            || text == "&apos;")
        {
            return true;
        }

        if (text.Length < 4 || text[1] != '#')
        {
            return false;
        }

        int index = 2;
        bool hexadecimal = false;
        if (index < text.Length && text[index] == 'x')
        {
            hexadecimal = true;
            index = index + 1;
        }

        int digitStart = index;
        while (index < text.Length - 1)
        {
            char value = text[index];
            bool valid = hexadecimal
                ? value >= '0' && value <= '9'
                    || value >= 'A' && value <= 'F'
                    || value >= 'a' && value <= 'f'
                : value >= '0' && value <= '9';
            if (!valid)
            {
                return false;
            }

            index = index + 1;
        }

        return index > digitStart && text[text.Length - 1] == ';';
    }

    internal bool IsXmlNameStart(int scalarValue)
    {
        return scalarValue == 95
            || scalarValue >= 65 && scalarValue <= 90
            || scalarValue >= 97 && scalarValue <= 122;
    }

    internal bool IsXmlCharacter(SourceCursorReadResult input)
    {
        if (input.Status != SourceCursorStatus.Value)
        {
            return false;
        }

        return input.IsLineBreak
            || input.ScalarValue == 9
            || input.ScalarValue >= 32 && input.ScalarValue <= 55295
            || input.ScalarValue >= 57344 && input.ScalarValue <= 65533
            || input.ScalarValue >= 65536 && input.ScalarValue <= 1114111;
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

    internal XmlValueScannerResult FinishXmlValue()
    {
        if (this.EntityOpen || this.TagQuote != 0)
        {
            this.Invalid = true;
        }

        if (this.Invalid && !this.DiagnosticAdded)
        {
            this.AddDiagnostic(13, "An XML value contains invalid markup, character data, or escaping.");
        }

        AsnToken token = new AsnToken
        {
            Kind = this.Invalid ? AsnTokenKind.Invalid : AsnTokenKind.XmlValue,
            Text = this.Accumulator.ToText(),
            Span = new AsnSourceSpan
            {
                Start = this.TokenStart,
                End = this.TokenEnd
            }
        };
        this.ResetState();
        return new XmlValueScannerResult
        {
            Status = XmlValueScannerStatus.Token,
            Token = token
        };
    }

    internal XmlValueScannerResult RollBackAttempt()
    {
        int index = this.AttemptInputs.Count - 1;
        while (index >= 1)
        {
            this.PushInput(this.AttemptInputs[index]);
            index = index - 1;
        }

        TriviaScannerResult first = this.AttemptInputs[0];
        SourceCursorReadResult input = this.GetInput(first);
        this.ResetState();
        return this.CreateNonXmlResult(input);
    }

    internal void ResetState()
    {
        this.Accumulator.Clear();
        this.EntityAccumulator.Clear();
        this.AttemptInputs.Clear();
        this.Scanning = false;
        this.Committed = false;
        this.InsideTag = false;
        this.ClosingTag = false;
        this.TagMarkerStage = 0;
        this.TagQuote = 0;
        this.LastTagCharacterWasSlash = false;
        this.Depth = 0;
        this.EntityOpen = false;
        this.LastTextCharacterWasHyphen = false;
        this.Invalid = false;
        this.DiagnosticAdded = false;
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

    internal XmlValueScannerResult CreateTriviaResult(AsnToken? token)
    {
        return new XmlValueScannerResult
        {
            Status = XmlValueScannerStatus.Trivia,
            Token = token
        };
    }

    internal XmlValueScannerResult CreateNonXmlResult(SourceCursorReadResult input)
    {
        return new XmlValueScannerResult
        {
            Status = XmlValueScannerStatus.NonXmlValue,
            Input = input
        };
    }

    internal XmlValueScannerResult CreateStateResult(XmlValueScannerStatus status)
    {
        return new XmlValueScannerResult
        {
            Status = status
        };
    }
}
