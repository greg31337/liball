namespace asn1;

internal class TokenCursor
{
    internal IncrementalLexSession Lexer;
    internal DiagnosticBag Diagnostics;
    internal int MaximumLookahead;
    internal System.Collections.Generic.List<AsnToken> Tokens;
    internal System.Collections.Generic.List<AsnToken> Trivia;
    internal System.Collections.Generic.List<TokenCheckpoint> Checkpoints;
    internal int Position;
    internal int NextCheckpointId = 1;
    internal bool LexerEnded;

    internal TokenCursor(IncrementalLexSession lexer, int maximumLookahead)
    {
        if (lexer == null)
        {
            throw new System.ArgumentNullException("lexer");
        }

        if (maximumLookahead <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumLookahead");
        }

        this.Lexer = lexer;
        this.Diagnostics = lexer.Diagnostics;
        this.MaximumLookahead = maximumLookahead;
        this.Tokens = new System.Collections.Generic.List<AsnToken>();
        this.Trivia = new System.Collections.Generic.List<AsnToken>();
        this.Checkpoints = new System.Collections.Generic.List<TokenCheckpoint>();
    }

    internal TokenCursorResult PeekNext(int distance)
    {
        if (distance < 0 || distance >= this.MaximumLookahead)
        {
            return this.CreateStateResult(TokenCursorStatus.LookaheadLimitExceeded);
        }

        if (distance > int.MaxValue - this.Position)
        {
            return this.CreateStateResult(TokenCursorStatus.LookaheadLimitExceeded);
        }

        int target = this.Position + distance;
        TokenCursorResult fill = this.FillThrough(target);
        if (fill.Status != TokenCursorStatus.Token)
        {
            return fill;
        }

        AsnToken token = this.Tokens[target];
        return new TokenCursorResult
        {
            Status = token.Kind == AsnTokenKind.EndOfInput
                ? TokenCursorStatus.EndOfInput
                : TokenCursorStatus.Token,
            Token = token
        };
    }

    internal TokenCursorResult ReadNext()
    {
        TokenCursorResult result = this.PeekNext(0);
        if ((result.Status == TokenCursorStatus.Token
                || result.Status == TokenCursorStatus.EndOfInput)
            && result.Token != null)
        {
            this.Position = this.Position + 1;
        }

        return result;
    }

    internal TokenCursorResult ExpectToken(AsnTokenKind expectedKind)
    {
        TokenCursorResult actual = this.PeekNext(0);
        if (actual.Status == TokenCursorStatus.NeedMoreData
            || actual.Status == TokenCursorStatus.LookaheadLimitExceeded)
        {
            return actual;
        }

        if (actual.Token != null && actual.Token.Kind == expectedKind)
        {
            this.Position = this.Position + 1;
            return actual;
        }

        this.AddExpectedDiagnostic(expectedKind.ToString(), actual.Token);
        return new TokenCursorResult
        {
            Status = TokenCursorStatus.Mismatch,
            Token = actual.Token
        };
    }

    internal TokenCursorResult ExpectOneOf(AsnTokenKind[] expectedKinds)
    {
        if (expectedKinds == null)
        {
            throw new System.ArgumentNullException("expectedKinds");
        }

        if (expectedKinds.Length == 0)
        {
            throw new System.ArgumentException("At least one expected kind is required.", "expectedKinds");
        }

        TokenCursorResult actual = this.PeekNext(0);
        if (actual.Status == TokenCursorStatus.NeedMoreData
            || actual.Status == TokenCursorStatus.LookaheadLimitExceeded)
        {
            return actual;
        }

        int index = 0;
        while (index < expectedKinds.Length)
        {
            if (actual.Token != null && actual.Token.Kind == expectedKinds[index])
            {
                this.Position = this.Position + 1;
                return actual;
            }

            index = index + 1;
        }

        this.AddExpectedDiagnostic(this.BuildExpectedText(expectedKinds), actual.Token);
        return new TokenCursorResult
        {
            Status = TokenCursorStatus.Mismatch,
            Token = actual.Token
        };
    }

    internal TokenCursorResult ExpectTokenText(AsnTokenKind expectedKind, string expectedText)
    {
        if (expectedText == null)
        {
            throw new System.ArgumentNullException("expectedText");
        }

        TokenCursorResult actual = this.PeekNext(0);
        if (actual.Status == TokenCursorStatus.NeedMoreData
            || actual.Status == TokenCursorStatus.LookaheadLimitExceeded)
        {
            return actual;
        }

        if (actual.Token != null
            && actual.Token.Kind == expectedKind
            && actual.Token.Text == expectedText)
        {
            this.Position = this.Position + 1;
            return actual;
        }

        this.AddExpectedDiagnostic("'" + expectedText + "'", actual.Token);
        return new TokenCursorResult
        {
            Status = TokenCursorStatus.Mismatch,
            Token = actual.Token
        };
    }

    internal int BeginCheckpoint()
    {
        int id = this.NextCheckpointId;
        if (this.NextCheckpointId == int.MaxValue)
        {
            this.NextCheckpointId = 1;
        }
        else
        {
            this.NextCheckpointId = this.NextCheckpointId + 1;
        }

        this.Checkpoints.Add(new TokenCheckpoint
        {
            Id = id,
            Position = this.Position
        });
        return id;
    }

    internal bool CommitCheckpoint(int checkpointId)
    {
        int last = this.Checkpoints.Count - 1;
        if (last < 0 || this.Checkpoints[last].Id != checkpointId)
        {
            return false;
        }

        this.Checkpoints.RemoveAt(last);
        return true;
    }

    internal bool RewindCheckpoint(int checkpointId)
    {
        int last = this.Checkpoints.Count - 1;
        if (last < 0 || this.Checkpoints[last].Id != checkpointId)
        {
            return false;
        }

        this.Position = this.Checkpoints[last].Position;
        this.Checkpoints.RemoveAt(last);
        return true;
    }

    internal TokenCursorResult RecoverTo(RecoverySet recoverySet)
    {
        if (recoverySet == null)
        {
            throw new System.ArgumentNullException("recoverySet");
        }

        int skipped = 0;
        while (true)
        {
            TokenCursorResult current = this.PeekNext(0);
            if (current.Status == TokenCursorStatus.NeedMoreData
                || current.Status == TokenCursorStatus.LookaheadLimitExceeded)
            {
                current.SkippedTokenCount = skipped;
                return current;
            }

            if (current.Token == null)
            {
                return new TokenCursorResult
                {
                    Status = TokenCursorStatus.EndOfInput,
                    SkippedTokenCount = skipped
                };
            }

            if (recoverySet.Contains(current.Token))
            {
                current.SkippedTokenCount = skipped;
                return current;
            }

            this.Position = this.Position + 1;
            skipped = skipped + 1;
        }
    }

    internal AsnToken[] GetTriviaTokens()
    {
        AsnToken[] result = new AsnToken[this.Trivia.Count];
        this.Trivia.CopyTo(result);
        return result;
    }

    internal int GetPosition()
    {
        return this.Position;
    }

    internal TokenCursorResult FillThrough(int target)
    {
        while (this.Tokens.Count <= target)
        {
            if (this.LexerEnded)
            {
                return this.CreateStateResult(TokenCursorStatus.EndOfInput);
            }

            IncrementalLexResult result = this.Lexer.ScanNext();
            if (result.Status == IncrementalLexStatus.NeedMoreData)
            {
                return this.CreateStateResult(TokenCursorStatus.NeedMoreData);
            }

            if (result.Status == IncrementalLexStatus.EndOfInput)
            {
                this.LexerEnded = true;
                return this.CreateStateResult(TokenCursorStatus.EndOfInput);
            }

            AsnToken token = result.Token == null
                ? throw new System.InvalidOperationException("The lexer token is missing.")
                : result.Token;
            if (token.Kind == AsnTokenKind.Whitespace || token.Kind == AsnTokenKind.Comment)
            {
                this.Trivia.Add(token);
            }
            else
            {
                this.Tokens.Add(token);
            }
        }

        return this.CreateStateResult(TokenCursorStatus.Token);
    }

    internal void AddExpectedDiagnostic(string expectedText, AsnToken? actual)
    {
        string actualText = actual == null
            ? "end of input"
            : actual.Kind == AsnTokenKind.EndOfInput
                ? "end of input"
                : "'" + actual.Text + "' (" + actual.Kind.ToString() + ")";
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Syntax,
            1,
            "Expected " + expectedText + " but found " + actualText + ".",
            actual == null ? null : actual.Span,
            null);
    }

    internal string BuildExpectedText(AsnTokenKind[] expectedKinds)
    {
        TextAccumulator text = new TextAccumulator(32);
        int index = 0;
        while (index < expectedKinds.Length)
        {
            if (index > 0)
            {
                text.AppendText(index == expectedKinds.Length - 1 ? " or " : ", ");
            }

            text.AppendText(expectedKinds[index].ToString());
            index = index + 1;
        }

        return text.ToText();
    }

    internal TokenCursorResult CreateStateResult(TokenCursorStatus status)
    {
        return new TokenCursorResult
        {
            Status = status
        };
    }
}
