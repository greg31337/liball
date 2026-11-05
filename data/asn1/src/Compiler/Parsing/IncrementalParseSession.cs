namespace asn1;

internal class IncrementalParseSession
{
    internal IncrementalLexSession Lexer;
    internal TokenCursor Cursor;
    internal ModuleParser Modules;
    internal System.Collections.Generic.List<ParseProductionFrame> ProductionStack;
    internal bool HasActiveModule;
    internal bool HasCheckpoint;
    internal bool Completed;
    internal int CheckpointId;
    internal int ModuleState;
    internal int MacroDepth;
    internal bool MacroPending;

    internal IncrementalParseSession(
        string sourceName,
        bool treatWarningsAsErrors,
        AsnResourceLimits limits,
        int maximumLookahead)
    {
        this.Lexer = new IncrementalLexSession(
            sourceName == null ? throw new System.ArgumentNullException("sourceName") : sourceName,
            treatWarningsAsErrors,
            limits == null ? throw new System.ArgumentNullException("limits") : limits);
        this.Cursor = new TokenCursor(this.Lexer, maximumLookahead);
        this.Modules = new ModuleParser(this.Cursor);
        this.ProductionStack = new System.Collections.Generic.List<ParseProductionFrame>();
        this.ResetProductionStack();
    }

    internal void SupplyTextChunk(string text, bool isFinal)
    {
        this.Lexer.SupplyTextChunk(text, isFinal);
    }

    internal void SupplyUtf8Chunk(byte[] data, bool isFinal)
    {
        this.Lexer.SupplyUtf8Chunk(data, isFinal);
    }

    internal void UseCompilerOptions(AsnCompilerOptions? options)
    {
        this.Lexer.UseCompilerOptions(options);
    }

    internal ModuleParseResult ParseNext()
    {
        if (this.Completed)
        {
            return this.CreateStateResult(ModuleParseStatus.EndOfInput);
        }

        while (true)
        {
            TokenCursorResult current = this.Cursor.PeekNext(0);
            if (current.Status == TokenCursorStatus.NeedMoreData)
            {
                return this.CreateStateResult(ModuleParseStatus.NeedMoreData);
            }

            if (current.Status == TokenCursorStatus.LookaheadLimitExceeded)
            {
                return this.CreateStateResult(ModuleParseStatus.Invalid);
            }

            if (current.Status == TokenCursorStatus.EndOfInput)
            {
                if (this.HasActiveModule)
                {
                    return this.FinishActiveModule(true);
                }

                this.Cursor.ReadNext();
                this.Completed = true;
                return this.CreateStateResult(ModuleParseStatus.EndOfInput);
            }

            if (current.Token == null)
            {
                return this.CreateStateResult(ModuleParseStatus.NeedMoreData);
            }

            if (!this.HasActiveModule)
            {
                this.BeginModule();
            }

            AsnToken token = current.Token;
            bool moduleEnd = token.Kind == AsnTokenKind.ReservedWord
                && token.Text == "END"
                && this.MacroDepth == 0;
            this.Cursor.ReadNext();
            this.AdvanceProductionStack(token);
            if (moduleEnd)
            {
                return this.FinishActiveModule(false);
            }
        }
    }

    internal ParseProductionFrame[] GetProductionStack()
    {
        ParseProductionFrame[] frames = new ParseProductionFrame[this.ProductionStack.Count];
        int index = 0;
        while (index < this.ProductionStack.Count)
        {
            ParseProductionFrame source = this.ProductionStack[index];
            frames[index] = new ParseProductionFrame
            {
                Kind = source.Kind,
                State = source.State,
                StartTokenPosition = source.StartTokenPosition,
                OpeningToken = source.OpeningToken
            };
            index = index + 1;
        }

        return frames;
    }

    internal AsnDiagnostic[] GetDiagnostics()
    {
        return this.Lexer.GetDiagnostics();
    }

    internal void BeginModule()
    {
        this.CheckpointId = this.Cursor.BeginCheckpoint();
        this.HasCheckpoint = true;
        this.HasActiveModule = true;
        this.ModuleState = 0;
        this.PushProduction(ParseProductionKind.Module, this.Cursor.GetPosition(), null);
        this.PushProduction(ParseProductionKind.ModuleHeader, this.Cursor.GetPosition(), null);
    }

    internal void AdvanceProductionStack(AsnToken token)
    {
        if (this.MacroDepth == 0 && token.Text == "MACRO")
        {
            this.MacroPending = true;
        }
        else if (this.MacroPending && token.Text == "BEGIN")
        {
            this.MacroPending = false;
            this.MacroDepth = 1;
            this.PushProduction(
                ParseProductionKind.RetiredMacro,
                this.Cursor.GetPosition() - 1,
                token);
            return;
        }
        else if (this.MacroDepth > 0 && token.Text == "BEGIN")
        {
            this.MacroDepth = this.MacroDepth + 1;
            return;
        }
        else if (this.MacroDepth > 0 && token.Text == "END")
        {
            this.MacroDepth = this.MacroDepth - 1;
            if (this.MacroDepth == 0)
            {
                this.RemoveProduction(ParseProductionKind.RetiredMacro);
            }

            return;
        }

        if (this.ModuleState == 0 && token.Text == "DEFINITIONS")
        {
            this.ModuleState = 1;
            this.SetModuleFrameState(1);
            return;
        }

        if (this.ModuleState == 1 && token.Text == "BEGIN")
        {
            this.ModuleState = 2;
            this.RemoveProduction(ParseProductionKind.ModuleHeader);
            this.PushProduction(
                ParseProductionKind.ModuleBody,
                this.Cursor.GetPosition() - 1,
                token);
            this.SetModuleFrameState(2);
            return;
        }

        if (token.Kind == AsnTokenKind.LeftParenthesis)
        {
            this.PushProduction(
                ParseProductionKind.ParenthesizedGroup,
                this.Cursor.GetPosition() - 1,
                token);
            return;
        }

        if (token.Kind == AsnTokenKind.LeftBracket)
        {
            this.PushProduction(
                ParseProductionKind.BracketedGroup,
                this.Cursor.GetPosition() - 1,
                token);
            return;
        }

        if (token.Kind == AsnTokenKind.LeftBrace)
        {
            ParseProductionKind kind = this.ModuleState == 0
                ? ParseProductionKind.DefinitiveIdentification
                : ParseProductionKind.BracedGroup;
            this.PushProduction(kind, this.Cursor.GetPosition() - 1, token);
            return;
        }

        if (token.Kind == AsnTokenKind.RightParenthesis)
        {
            this.RemoveProduction(ParseProductionKind.ParenthesizedGroup);
            return;
        }

        if (token.Kind == AsnTokenKind.RightBracket)
        {
            this.RemoveProduction(ParseProductionKind.BracketedGroup);
            return;
        }

        if (token.Kind == AsnTokenKind.RightBrace)
        {
            if (!this.RemoveProduction(ParseProductionKind.BracedGroup))
            {
                this.RemoveProduction(ParseProductionKind.DefinitiveIdentification);
            }
        }
    }

    internal ModuleParseResult FinishActiveModule(bool reachedEndOfInput)
    {
        if (this.HasCheckpoint)
        {
            this.Cursor.RewindCheckpoint(this.CheckpointId);
        }

        this.HasCheckpoint = false;
        ModuleParseResult result = this.Modules.ParseModule();
        if (result.Status == ModuleParseStatus.Invalid)
        {
            TokenCursorResult boundary = this.Cursor.PeekNext(0);
            if (boundary.Token != null
                && boundary.Token.Kind == AsnTokenKind.ReservedWord
                && boundary.Token.Text == "END")
            {
                this.Cursor.ReadNext();
            }
        }

        this.HasActiveModule = false;
        this.ModuleState = 0;
        this.MacroDepth = 0;
        this.MacroPending = false;
        this.ResetProductionStack();
        if (reachedEndOfInput && result.Status == ModuleParseStatus.EndOfInput)
        {
            this.Completed = true;
        }

        return result;
    }

    internal void ResetProductionStack()
    {
        this.ProductionStack.Clear();
        this.PushProduction(ParseProductionKind.CompilationUnit, 0, null);
    }

    internal void PushProduction(
        ParseProductionKind kind,
        int startTokenPosition,
        AsnToken? openingToken)
    {
        this.ProductionStack.Add(new ParseProductionFrame
        {
            Kind = kind,
            StartTokenPosition = startTokenPosition,
            OpeningToken = openingToken
        });
    }

    internal bool RemoveProduction(ParseProductionKind kind)
    {
        int index = this.ProductionStack.Count - 1;
        while (index > 0)
        {
            if (this.ProductionStack[index].Kind == kind)
            {
                this.ProductionStack.RemoveAt(index);
                return true;
            }

            index = index - 1;
        }

        return false;
    }

    internal void SetModuleFrameState(int state)
    {
        int index = 0;
        while (index < this.ProductionStack.Count)
        {
            if (this.ProductionStack[index].Kind == ParseProductionKind.Module)
            {
                this.ProductionStack[index].State = state;
                return;
            }

            index = index + 1;
        }
    }

    internal ModuleParseResult CreateStateResult(ModuleParseStatus status)
    {
        return new ModuleParseResult
        {
            Status = status
        };
    }
}
