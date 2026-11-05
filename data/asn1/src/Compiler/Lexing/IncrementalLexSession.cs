namespace asn1;

internal class IncrementalLexSession
{
    internal SourceCursor Cursor;
    internal DiagnosticBag Diagnostics;
    internal TriviaScanner Trivia;
    internal LexInputBuffer Inputs;
    internal IdentifierScanner Identifiers;
    internal LiteralScanner Literals;
    internal XmlValueScanner XmlValues;
    internal PunctuationScanner Punctuation;
    internal Utf8ChunkDecoder Utf8Decoder;
    internal int ActiveScanner;
    internal int InputMode;
    internal bool EndTokenEmitted;
    internal bool NonEcnContentPending;
    internal bool NonEcnEndPending;
    internal System.Text.StringBuilder NonEcnText =
        new System.Text.StringBuilder();
    internal string NonEcnEndText = "NON-ECN-END";
    internal AsnSourcePosition NonEcnStart = new AsnSourcePosition();
    internal AsnSourcePosition NonEcnEndStart = new AsnSourcePosition();
    internal AsnSourcePosition NonEcnEnd = new AsnSourcePosition();

    internal IncrementalLexSession(
        string sourceName,
        bool treatWarningsAsErrors,
        AsnResourceLimits limits)
    {
        this.Cursor = new SourceCursor(sourceName == null
            ? throw new System.ArgumentNullException("sourceName")
            : sourceName);
        this.Diagnostics = new DiagnosticBag(
            treatWarningsAsErrors,
            limits == null ? throw new System.ArgumentNullException("limits") : limits);
        this.Trivia = new TriviaScanner(this.Cursor, this.Diagnostics);
        this.Inputs = new LexInputBuffer(this.Trivia);
        this.Identifiers = new IdentifierScanner(this.Trivia, this.Diagnostics);
        this.Literals = new LiteralScanner(this.Trivia, this.Diagnostics);
        this.XmlValues = new XmlValueScanner(this.Trivia, this.Diagnostics);
        this.Punctuation = new PunctuationScanner(this.Trivia, this.Diagnostics);
        this.Identifiers.UseInputBuffer(this.Inputs);
        this.Literals.UseInputBuffer(this.Inputs);
        this.XmlValues.UseInputBuffer(this.Inputs);
        this.Punctuation.UseInputBuffer(this.Inputs);
        this.Utf8Decoder = new Utf8ChunkDecoder();
    }

    internal void SupplyTextChunk(string text, bool isFinal)
    {
        if (this.InputMode == 2)
        {
            throw new System.InvalidOperationException("Text and UTF-8 chunks cannot be mixed in one lex session.");
        }

        this.InputMode = 1;
        this.Cursor.SupplyChunk(text, isFinal);
    }

    internal void SupplyUtf8Chunk(byte[] data, bool isFinal)
    {
        if (this.InputMode == 1)
        {
            throw new System.InvalidOperationException("Text and UTF-8 chunks cannot be mixed in one lex session.");
        }

        this.InputMode = 2;
        string text = this.Utf8Decoder.DecodeChunk(data, isFinal);
        this.Cursor.SupplyChunk(text, isFinal);
    }

    internal void UseCompilerOptions(AsnCompilerOptions? options)
    {
        this.Diagnostics.ApplyCompilerOptions(options);
    }

    internal IncrementalLexResult ScanNext()
    {
        if (this.NonEcnEndPending)
        {
            this.NonEcnEndPending = false;
            return this.CreateTokenResult(new AsnToken
            {
                Kind = AsnTokenKind.ReservedWord,
                Text = this.NonEcnEndText,
                Span = new AsnSourceSpan
                {
                    Start = this.NonEcnEndStart,
                    End = this.NonEcnEnd
                }
            });
        }

        if (this.NonEcnContentPending)
        {
            return this.ScanNonEcnContent();
        }

        if (this.ActiveScanner != 0)
        {
            return this.ContinueActiveScanner();
        }

        if (this.EndTokenEmitted)
        {
            return this.CreateStateResult(IncrementalLexStatus.EndOfInput);
        }

        TriviaScannerResult first = this.Inputs.ReadInput();
        if (first.Status == TriviaScannerStatus.NeedMoreData)
        {
            return this.CreateStateResult(IncrementalLexStatus.NeedMoreData);
        }

        if (first.Status == TriviaScannerStatus.EndOfInput)
        {
            return this.CreateEndToken();
        }

        if (first.Status == TriviaScannerStatus.Trivia)
        {
            return this.CreateTokenResult(first.Token);
        }

        SourceCursorReadResult input = first.Input == null
            ? throw new System.InvalidOperationException("Non-trivia input is missing.")
            : first.Input;
        this.Inputs.PushInput(first);
        if (this.IsLetter(input.ScalarValue)
            || input.ScalarValue == 35
            || input.ScalarValue == 38)
        {
            this.ActiveScanner = 1;
        }
        else if (this.IsDigit(input.ScalarValue)
            || input.ScalarValue == 34
            || input.ScalarValue == 39
            || input.ScalarValue == 123)
        {
            this.ActiveScanner = 2;
        }
        else if (input.ScalarValue == 60)
        {
            this.ActiveScanner = 3;
        }
        else
        {
            this.ActiveScanner = 4;
        }

        return this.ContinueActiveScanner();
    }

    internal IncrementalLexResult ContinueActiveScanner()
    {
        if (this.ActiveScanner == 1)
        {
            return this.ContinueIdentifierScanner();
        }

        if (this.ActiveScanner == 2)
        {
            return this.ContinueLiteralScanner();
        }

        if (this.ActiveScanner == 3)
        {
            return this.ContinueXmlScanner();
        }

        if (this.ActiveScanner == 4)
        {
            return this.ContinuePunctuationScanner();
        }

        throw new System.InvalidOperationException("The active scanner is not recognized.");
    }

    internal IncrementalLexResult ContinueIdentifierScanner()
    {
        IdentifierScannerResult result = this.Identifiers.ScanNext();
        if (result.Status == IdentifierScannerStatus.NeedMoreData)
        {
            return this.CreateStateResult(IncrementalLexStatus.NeedMoreData);
        }

        if (result.Status == IdentifierScannerStatus.Token
            || result.Status == IdentifierScannerStatus.Trivia)
        {
            this.ActiveScanner = 0;
            return this.CreateTokenResult(result.Token);
        }

        if (result.Status == IdentifierScannerStatus.EndOfInput)
        {
            this.ActiveScanner = 0;
            return this.CreateEndToken();
        }

        SourceCursorReadResult input = result.Input == null
            ? throw new System.InvalidOperationException("Identifier fallback input is missing.")
            : result.Input;
        this.PushRawInput(input);
        this.ActiveScanner = 4;
        return this.ContinuePunctuationScanner();
    }

    internal IncrementalLexResult ContinueLiteralScanner()
    {
        LiteralScannerResult result = this.Literals.ScanNext();
        if (result.Status == LiteralScannerStatus.NeedMoreData)
        {
            return this.CreateStateResult(IncrementalLexStatus.NeedMoreData);
        }

        if (result.Status == LiteralScannerStatus.Token
            || result.Status == LiteralScannerStatus.Trivia)
        {
            this.ActiveScanner = 0;
            return this.CreateTokenResult(result.Token);
        }

        if (result.Status == LiteralScannerStatus.EndOfInput)
        {
            this.ActiveScanner = 0;
            return this.CreateEndToken();
        }

        SourceCursorReadResult input = result.Input == null
            ? throw new System.InvalidOperationException("Literal fallback input is missing.")
            : result.Input;
        this.PushRawInput(input);
        this.ActiveScanner = 4;
        return this.ContinuePunctuationScanner();
    }

    internal IncrementalLexResult ContinueXmlScanner()
    {
        XmlValueScannerResult result = this.XmlValues.ScanNext();
        if (result.Status == XmlValueScannerStatus.NeedMoreData)
        {
            return this.CreateStateResult(IncrementalLexStatus.NeedMoreData);
        }

        if (result.Status == XmlValueScannerStatus.Token
            || result.Status == XmlValueScannerStatus.Trivia)
        {
            this.ActiveScanner = 0;
            return this.CreateTokenResult(result.Token);
        }

        if (result.Status == XmlValueScannerStatus.EndOfInput)
        {
            this.ActiveScanner = 0;
            return this.CreateEndToken();
        }

        SourceCursorReadResult input = result.Input == null
            ? throw new System.InvalidOperationException("XML fallback input is missing.")
            : result.Input;
        this.PushRawInput(input);
        this.ActiveScanner = 4;
        return this.ContinuePunctuationScanner();
    }

    internal IncrementalLexResult ContinuePunctuationScanner()
    {
        PunctuationScannerResult result = this.Punctuation.ScanNext();
        if (result.Status == PunctuationScannerStatus.NeedMoreData)
        {
            return this.CreateStateResult(IncrementalLexStatus.NeedMoreData);
        }

        if (result.Status == PunctuationScannerStatus.Token
            || result.Status == PunctuationScannerStatus.Trivia)
        {
            this.ActiveScanner = 0;
            return this.CreateTokenResult(result.Token);
        }

        if (result.Status == PunctuationScannerStatus.EndOfInput)
        {
            this.ActiveScanner = 0;
            return this.CreateEndToken();
        }

        throw new System.InvalidOperationException("A routed input was not punctuation.");
    }

    internal void PushRawInput(SourceCursorReadResult input)
    {
        this.Inputs.PushInput(new TriviaScannerResult
        {
            Status = TriviaScannerStatus.NonTrivia,
            Input = input
        });
    }

    internal bool IsLetter(int scalarValue)
    {
        return scalarValue >= 65 && scalarValue <= 90
            || scalarValue >= 97 && scalarValue <= 122;
    }

    internal bool IsDigit(int scalarValue)
    {
        return scalarValue >= 48 && scalarValue <= 57;
    }

    internal AsnDiagnostic[] GetDiagnostics()
    {
        return this.Diagnostics.GetDiagnostics();
    }

    internal int GetConsumedCharacterCount()
    {
        return this.Cursor.GetConsumedCharacterCount();
    }

    internal IncrementalLexResult CreateEndToken()
    {
        this.EndTokenEmitted = true;
        AsnSourcePosition position = this.Cursor.GetPosition();
        return this.CreateTokenResult(new AsnToken
        {
            Kind = AsnTokenKind.EndOfInput,
            Span = new AsnSourceSpan
            {
                Start = position,
                End = position
            }
        });
    }

    internal IncrementalLexResult CreateTokenResult(AsnToken? token)
    {
        if (token != null && this.IsNonEcnBeginToken(token))
        {
            this.NonEcnContentPending = true;
            this.NonEcnText.Clear();
            this.NonEcnStart = token.Span == null
                ? this.Cursor.GetPosition()
                : token.Span.End;
        }

        return new IncrementalLexResult
        {
            Status = IncrementalLexStatus.Token,
            Token = token
        };
    }

    internal IncrementalLexResult ScanNonEcnContent()
    {
        string marker = "NON-ECN-END";
        while (true)
        {
            TriviaScannerResult input = this.Inputs.ReadInput();
            if (input.Status == TriviaScannerStatus.NeedMoreData)
            {
                return this.CreateStateResult(
                    IncrementalLexStatus.NeedMoreData);
            }

            if (input.Status == TriviaScannerStatus.EndOfInput)
            {
                this.NonEcnContentPending = false;
                AsnSourcePosition end = this.Cursor.GetPosition();
                this.Diagnostics.TryAddDiagnostic(
                    AsnDiagnosticSeverity.Error,
                    AsnDiagnosticCategory.Lexical,
                    15,
                    "The ECN NON-ECN-BEGIN item is missing NON-ECN-END.",
                    new AsnSourceSpan
                    {
                        Start = this.NonEcnStart,
                        End = end
                    },
                    null);
                return this.CreateTokenResult(new AsnToken
                {
                    Kind = AsnTokenKind.Invalid,
                    Text = this.NonEcnText.ToString(),
                    Span = new AsnSourceSpan
                    {
                        Start = this.NonEcnStart,
                        End = end
                    }
                });
            }

            string text;
            AsnSourcePosition endPosition;
            if (input.Status == TriviaScannerStatus.Trivia
                && input.Token != null)
            {
                text = input.Token.Text;
                endPosition = input.Token.Span == null
                    ? this.Cursor.GetPosition()
                    : input.Token.Span.End;
            }
            else
            {
                SourceCursorReadResult scalar = input.Input == null
                    ? throw new System.InvalidOperationException(
                        "The NON-ECN input scalar is missing.")
                    : input.Input;
                text = scalar.Text;
                endPosition = scalar.End;
            }

            this.NonEcnText.Append(text);
            if (this.NonEcnText.Length >= marker.Length)
            {
                string accumulated = this.NonEcnText.ToString();
                if (accumulated.EndsWith(
                    marker,
                    System.StringComparison.Ordinal)
                    || this.EndsWithEquivalentNonEcnEnd(accumulated))
                {
                    string content = accumulated.Substring(
                        0,
                        accumulated.Length - marker.Length);
                    this.NonEcnEndText = accumulated.Substring(
                        accumulated.Length - marker.Length);
                    this.NonEcnContentPending = false;
                    this.NonEcnEndPending = true;
                    this.NonEcnEnd = endPosition;
                    this.NonEcnEndStart = new AsnSourcePosition
                    {
                        SourceName = endPosition.SourceName,
                        Offset = endPosition.Offset - marker.Length,
                        Line = endPosition.Line,
                        Column = endPosition.Column - marker.Length
                    };
                    this.NonEcnText.Clear();
                    return this.CreateTokenResult(new AsnToken
                    {
                        Kind = AsnTokenKind.NonEcnText,
                        Text = content,
                        Span = new AsnSourceSpan
                        {
                            Start = this.NonEcnStart,
                            End = this.NonEcnEndStart
                        }
                    });
                }
            }
        }
    }

    internal bool IsNonEcnBeginToken(AsnToken token)
    {
        return token.Kind == AsnTokenKind.ReservedWord
            && this.MatchesEquivalentHyphens(
                token.Text,
                0,
                "NON-ECN-BEGIN");
    }

    internal bool EndsWithEquivalentNonEcnEnd(string text)
    {
        string marker = "NON-ECN-END";
        return text.Length >= marker.Length
            && this.MatchesEquivalentHyphens(
                text,
                text.Length - marker.Length,
                marker);
    }

    internal bool MatchesEquivalentHyphens(
        string text,
        int start,
        string expected)
    {
        if (start < 0 || text.Length - start != expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < expected.Length)
        {
            char actualCharacter = text[start + index];
            char expectedCharacter = expected[index];
            if (actualCharacter != expectedCharacter
                && !(expectedCharacter == '-'
                    && actualCharacter == '\u2011'))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal IncrementalLexResult CreateStateResult(IncrementalLexStatus status)
    {
        return new IncrementalLexResult
        {
            Status = status
        };
    }
}
