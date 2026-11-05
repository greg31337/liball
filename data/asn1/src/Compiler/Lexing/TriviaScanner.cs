namespace asn1;

internal class TriviaScanner
{
    internal SourceCursor Cursor;
    internal DiagnosticBag Diagnostics;
    internal TextAccumulator Accumulator;
    internal System.Collections.Generic.List<SourceCursorReadResult> PendingInputs;
    internal TriviaScannerState State;
    internal AsnSourcePosition TriviaStart = new AsnSourcePosition();
    internal bool HyphenPending;
    internal int BlockDepth;
    internal int BlockPendingDelimiter;

    internal TriviaScanner(SourceCursor cursor, DiagnosticBag diagnostics)
    {
        this.Cursor = cursor == null ? throw new System.ArgumentNullException("cursor") : cursor;
        this.Diagnostics = diagnostics == null ? throw new System.ArgumentNullException("diagnostics") : diagnostics;
        this.Accumulator = new TextAccumulator(64);
        this.PendingInputs = new System.Collections.Generic.List<SourceCursorReadResult>();
    }

    internal TriviaScannerResult ScanNext()
    {
        if (this.State == TriviaScannerState.Whitespace)
        {
            return this.ScanWhitespace();
        }

        if (this.State == TriviaScannerState.HyphenComment)
        {
            return this.ScanHyphenComment();
        }

        if (this.State == TriviaScannerState.BlockComment)
        {
            return this.ScanBlockComment();
        }

        SourceCursorReadResult first = this.ReadInput();
        if (first.Status == SourceCursorStatus.NeedMoreData)
        {
            return this.CreateStateResult(TriviaScannerStatus.NeedMoreData);
        }

        if (first.Status == SourceCursorStatus.EndOfInput)
        {
            return this.CreateStateResult(TriviaScannerStatus.EndOfInput);
        }

        if (this.IsWhitespace(first))
        {
            this.State = TriviaScannerState.Whitespace;
            this.TriviaStart = first.Start;
            this.Accumulator.AppendText(first.Text);
            return this.ScanWhitespace();
        }

        if (first.Status == SourceCursorStatus.Value && (first.ScalarValue == 45 || first.ScalarValue == 47))
        {
            SourceCursorReadResult second = this.ReadInput();
            if (second.Status == SourceCursorStatus.NeedMoreData)
            {
                this.PushInput(first);
                return this.CreateStateResult(TriviaScannerStatus.NeedMoreData);
            }

            if (second.Status == SourceCursorStatus.Value
                && first.ScalarValue == 45
                && second.ScalarValue == 45)
            {
                this.State = TriviaScannerState.HyphenComment;
                this.TriviaStart = first.Start;
                this.Accumulator.AppendText(first.Text);
                this.Accumulator.AppendText(second.Text);
                this.HyphenPending = false;
                return this.ScanHyphenComment();
            }

            if (second.Status == SourceCursorStatus.Value
                && first.ScalarValue == 47
                && second.ScalarValue == 42)
            {
                this.State = TriviaScannerState.BlockComment;
                this.TriviaStart = first.Start;
                this.Accumulator.AppendText(first.Text);
                this.Accumulator.AppendText(second.Text);
                this.BlockDepth = 1;
                this.BlockPendingDelimiter = 0;
                return this.ScanBlockComment();
            }

            if (second.Status != SourceCursorStatus.EndOfInput)
            {
                this.PushInput(second);
            }

            return this.CreateNonTriviaResult(first);
        }

        return this.CreateNonTriviaResult(first);
    }

    internal TriviaScannerResult ScanWhitespace()
    {
        while (true)
        {
            SourceCursorReadResult input = this.ReadInput();
            if (input.Status == SourceCursorStatus.NeedMoreData)
            {
                return this.CreateStateResult(TriviaScannerStatus.NeedMoreData);
            }

            if (input.Status == SourceCursorStatus.EndOfInput)
            {
                return this.FinishTrivia(AsnTokenKind.Whitespace, input.Start);
            }

            if (!this.IsWhitespace(input))
            {
                this.PushInput(input);
                return this.FinishTrivia(AsnTokenKind.Whitespace, input.Start);
            }

            this.Accumulator.AppendText(input.Text);
        }
    }

    internal TriviaScannerResult ScanHyphenComment()
    {
        while (true)
        {
            SourceCursorReadResult input = this.ReadInput();
            if (input.Status == SourceCursorStatus.NeedMoreData)
            {
                return this.CreateStateResult(TriviaScannerStatus.NeedMoreData);
            }

            if (input.Status == SourceCursorStatus.EndOfInput)
            {
                this.HyphenPending = false;
                return this.FinishTrivia(AsnTokenKind.Comment, input.Start);
            }

            if (input.IsLineBreak)
            {
                this.HyphenPending = false;
                this.PushInput(input);
                return this.FinishTrivia(AsnTokenKind.Comment, input.Start);
            }

            this.Accumulator.AppendText(input.Text);
            if (this.HyphenPending && input.Status == SourceCursorStatus.Value && input.ScalarValue == 45)
            {
                this.HyphenPending = false;
                return this.FinishTrivia(AsnTokenKind.Comment, input.End);
            }

            this.HyphenPending = input.Status == SourceCursorStatus.Value && input.ScalarValue == 45;
        }
    }

    internal TriviaScannerResult ScanBlockComment()
    {
        while (true)
        {
            SourceCursorReadResult input = this.ReadInput();
            if (input.Status == SourceCursorStatus.NeedMoreData)
            {
                return this.CreateStateResult(TriviaScannerStatus.NeedMoreData);
            }

            if (input.Status == SourceCursorStatus.EndOfInput)
            {
                AsnSourceSpan span = new AsnSourceSpan
                {
                    Start = this.TriviaStart,
                    End = input.Start
                };
                this.Diagnostics.TryAddDiagnostic(
                    AsnDiagnosticSeverity.Error,
                    AsnDiagnosticCategory.Lexical,
                    1,
                    "Unterminated multiple-line comment.",
                    span,
                    null);
                this.BlockDepth = 0;
                this.BlockPendingDelimiter = 0;
                return this.FinishTrivia(AsnTokenKind.Comment, input.Start);
            }

            this.Accumulator.AppendText(input.Text);
            if (input.Status != SourceCursorStatus.Value || input.IsLineBreak)
            {
                this.BlockPendingDelimiter = 0;
                continue;
            }

            if (this.BlockPendingDelimiter == 47 && input.ScalarValue == 42)
            {
                this.BlockDepth = this.BlockDepth + 1;
                this.BlockPendingDelimiter = 0;
                continue;
            }

            if (this.BlockPendingDelimiter == 42 && input.ScalarValue == 47)
            {
                this.BlockDepth = this.BlockDepth - 1;
                this.BlockPendingDelimiter = 0;
                if (this.BlockDepth == 0)
                {
                    return this.FinishTrivia(AsnTokenKind.Comment, input.End);
                }

                continue;
            }

            if (input.ScalarValue == 47 || input.ScalarValue == 42)
            {
                this.BlockPendingDelimiter = input.ScalarValue;
            }
            else
            {
                this.BlockPendingDelimiter = 0;
            }
        }
    }

    internal bool IsWhitespace(SourceCursorReadResult input)
    {
        if (input.Status != SourceCursorStatus.Value)
        {
            return false;
        }

        return input.IsLineBreak
            || input.ScalarValue == 9
            || input.ScalarValue == 32
            || input.ScalarValue == 160;
    }

    internal SourceCursorReadResult ReadInput()
    {
        if (this.PendingInputs.Count == 0)
        {
            return this.Cursor.ReadNext();
        }

        SourceCursorReadResult result = this.PendingInputs[0];
        this.PendingInputs.RemoveAt(0);
        return result;
    }

    internal void PushInput(SourceCursorReadResult input)
    {
        this.PendingInputs.Insert(0, input);
    }

    internal TriviaScannerResult FinishTrivia(AsnTokenKind kind, AsnSourcePosition end)
    {
        AsnToken token = new AsnToken
        {
            Kind = kind,
            Text = this.Accumulator.ToText(),
            Span = new AsnSourceSpan
            {
                Start = this.TriviaStart,
                End = end
            }
        };
        this.Accumulator.Clear();
        this.State = TriviaScannerState.Idle;
        return new TriviaScannerResult
        {
            Status = TriviaScannerStatus.Trivia,
            Token = token
        };
    }

    internal TriviaScannerResult CreateNonTriviaResult(SourceCursorReadResult input)
    {
        return new TriviaScannerResult
        {
            Status = TriviaScannerStatus.NonTrivia,
            Input = input
        };
    }

    internal TriviaScannerResult CreateStateResult(TriviaScannerStatus status)
    {
        return new TriviaScannerResult
        {
            Status = status
        };
    }
}
