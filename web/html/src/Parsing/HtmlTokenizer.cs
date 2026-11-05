using System;

namespace html;

internal interface IHtmlTokenizerControl
{
    public void Feed(ReadOnlySpan<char> input, bool isFinal);
    public HtmlTokenReadResult Read();
    public void SetCdataAllowed(bool value);
    public void InitializeForFragment(HtmlTokenizerState state,
        string lastStartTagName, bool cdataAllowed);
    public void ApplyTreeBuilderState(HtmlTokenizerState state);
    public void EnableByteOffsets(HtmlDecodedInput input);
}

internal class HtmlTokenizerControl : IHtmlTokenizerControl
{
    internal HtmlTokenizer tokenizer;

    internal HtmlTokenizerControl(HtmlTokenizer tokenizerValue)
    {
        if (tokenizerValue == null)
        {
            throw new ArgumentNullException("tokenizerValue");
        }

        this.tokenizer = tokenizerValue;
    }

    public void Feed(ReadOnlySpan<char> input, bool isFinal)
    {
        this.tokenizer.Feed(input, isFinal);
    }

    public HtmlTokenReadResult Read()
    {
        return this.tokenizer.Read();
    }

    public void SetCdataAllowed(bool value)
    {
        this.tokenizer.SetCdataAllowed(value);
    }

    public void InitializeForFragment(HtmlTokenizerState state,
        string lastStartTagName, bool cdataAllowed)
    {
        this.tokenizer.InitializeForFragment(state, lastStartTagName,
            cdataAllowed);
    }

    public void ApplyTreeBuilderState(HtmlTokenizerState state)
    {
        this.tokenizer.ApplyTreeBuilderState(state);
    }

    public void EnableByteOffsets(HtmlDecodedInput input)
    {
        this.tokenizer.EnableByteOffsets(input);
    }
}

internal class HtmlTokenizerContext
{
    internal HtmlTokenizerOptions options;
    internal HtmlBudget budget;
    internal HtmlDiagnosticCollector diagnostics;
    internal HtmlInput input;
    internal HtmlTokenQueue tokens;
    internal int deliveredDiagnosticCount;
    internal int deliveredTokenCount;
    internal int maximumTokenCharacters;
    internal int maximumAttributesPerTag;
    internal int maximumNameCharacters;
    internal int maximumAttributeValueCharacters;
    internal bool acceptingInput;
    internal bool terminalLimitExceeded;
    internal bool tokenEmissionLimitExceeded;
    internal bool limitDiagnosticReported;
    internal bool endOfFileEmitted;
    internal bool complete;
    internal HtmlTokenizerState state;
    internal bool hasLastStartTagName;
    internal string lastStartTagName = "";
    internal int markupStart;
    internal HtmlTagTokenBuilder? currentTag;
    internal HtmlCommentTokenBuilder? currentComment;
    internal HtmlDoctypeTokenBuilder? currentDoctype;
    internal CharacterBuffer scriptTemporaryBuffer;
    internal bool scriptTemporaryBufferOverflow;
    internal bool cdataAllowed;
    internal int cdataPendingStart;

    internal HtmlTokenizerContext(HtmlTokenizerOptions options,
        HtmlBudget? budgetValue, bool inputAlreadyCounted)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        this.options = options.Copy();
        HtmlLimits limits = this.options.GetLimits();
        this.budget = budgetValue == null
            ? new HtmlBudget(limits,
                this.options.GetCancellationToken())
            : budgetValue;
        this.diagnostics = new HtmlDiagnosticCollector(this.budget);
        this.input = HtmlInput.Create(this.budget, this.diagnostics,
            !inputAlreadyCounted);
        this.tokens = new HtmlTokenQueue();
        this.deliveredDiagnosticCount = 0;
        this.deliveredTokenCount = 0;
        this.maximumTokenCharacters = limits.GetMaximumTokenCharacters();
        this.maximumAttributesPerTag = limits.GetMaximumAttributesPerTag();
        this.maximumNameCharacters = limits.GetMaximumNameCharacters();
        this.maximumAttributeValueCharacters = limits.GetMaximumAttributeValueCharacters();
        this.acceptingInput = true;
        this.terminalLimitExceeded = false;
        this.tokenEmissionLimitExceeded = false;
        this.limitDiagnosticReported = false;
        this.endOfFileEmitted = false;
        this.complete = false;
        this.state = HtmlTokenizerState.Data;
        this.hasLastStartTagName = false;
        this.lastStartTagName = "";
        this.markupStart = -1;
        this.currentTag = null;
        this.currentComment = null;
        this.currentDoctype = null;
        int scriptMaximum = this.maximumTokenCharacters < 6
            ? 6 : this.maximumTokenCharacters;
        int scriptCapacity = scriptMaximum < 16 ? scriptMaximum : 16;
        this.scriptTemporaryBuffer = CharacterBuffer.Create(
            scriptCapacity, scriptMaximum);
        this.scriptTemporaryBufferOverflow = false;
        this.cdataAllowed = false;
        this.cdataPendingStart = -1;
    }

    internal void Feed(ReadOnlySpan<char> value, bool isFinal)
    {
        this.budget.CheckCancellation();
        if (!this.acceptingInput)
        {
            throw new InvalidOperationException("The tokenizer cannot accept input after a final chunk or terminal limit.");
        }

        bool appended = this.input.TryAppendSpan(value);
        if (!appended)
        {
            this.StopForLimit(HtmlDiagnosticCode.InputLimitExceeded,
                "The tokenizer input character limit was exceeded.", false);
            this.input.Complete();
            return;
        }

        if (isFinal)
        {
            this.acceptingInput = false;
            this.input.Complete();
        }
    }

    internal void EnableByteOffsets(HtmlDecodedInput value)
    {
        this.input.EnableByteOffsets(value);
    }

    internal HtmlTokenReadResult Read()
    {
        this.budget.CheckCancellation();
        if (this.tokens.GetCount() > 0)
        {
            return this.CreateTokenResult();
        }

        if (this.complete)
        {
            return HtmlTokenReadResult.CreateStatus(HtmlTokenReadStatus.Complete,
                this.TakeDiagnostics(), false, this.diagnostics.WasTruncated(),
                this.diagnostics.GetDroppedCount());
        }

        bool running = true;
        while (this.tokens.GetCount() == 0 && !this.tokenEmissionLimitExceeded && running)
        {
            if (this.input.GetAvailableLength() > 0)
            {
                running = this.RunCurrentState();
            }
            else if (this.input.IsComplete() && (int)this.state >= (int)HtmlTokenizerState.TagOpen)
            {
                running = this.RunCurrentState();
            }
            else
            {
                running = false;
            }
        }

        if (this.tokens.GetCount() > 0)
        {
            return this.CreateTokenResult();
        }

        if (this.terminalLimitExceeded)
        {
            return this.CreateLimitResult();
        }

        if (!this.input.IsEnd())
        {
            return HtmlTokenReadResult.CreateStatus(HtmlTokenReadStatus.NeedMoreInput,
                this.TakeDiagnostics(), false, this.diagnostics.WasTruncated(),
                this.diagnostics.GetDroppedCount());
        }

        if (!this.endOfFileEmitted)
        {
            this.endOfFileEmitted = true;
            HtmlToken token = HtmlToken.CreateEndOfFile(
                this.CreateSourceSpan(this.input.GetPosition(), this.input.GetPosition()));
            this.TryEmitToken(token);
            if (this.tokens.GetCount() > 0)
            {
                return this.CreateTokenResult();
            }

            return this.CreateLimitResult();
        }

        this.complete = true;
        return HtmlTokenReadResult.CreateStatus(HtmlTokenReadStatus.Complete,
            this.TakeDiagnostics(), false, this.diagnostics.WasTruncated(),
            this.diagnostics.GetDroppedCount());
    }

    internal int GetBufferedInputLength()
    {
        return this.input.GetBufferedLength();
    }

    internal int GetInputPosition()
    {
        return this.input.GetPosition();
    }

    internal ReadOnlySpan<char> GetAvailableInputSpan()
    {
        return this.input.GetAvailableSpan();
    }

    internal int PeekInput()
    {
        return this.input.Peek();
    }

    internal int PeekInputAhead(int lookahead)
    {
        return this.input.PeekAhead(lookahead);
    }

    internal int ConsumeInput()
    {
        return this.input.Read();
    }

    internal void ReconsumeInput()
    {
        this.input.Reconsume();
    }

    internal bool IsInputComplete()
    {
        return this.input.IsComplete();
    }

    internal bool IsInputEnd()
    {
        return this.input.IsEnd();
    }

    internal int GetMaximumTokenCharacters()
    {
        return this.maximumTokenCharacters;
    }

    internal bool GetCoalesceCharacterTokens()
    {
        return this.options.GetCoalesceCharacterTokens();
    }

    internal HtmlTokenizerState GetState()
    {
        return this.state;
    }

    internal void SetCdataAllowed(bool value)
    {
        this.cdataAllowed = value;
    }

    internal bool GetCdataAllowed()
    {
        return this.cdataAllowed;
    }

    internal void SetState(HtmlTokenizerState value)
    {
        if ((int)value < 0 || (int)value > 64)
        {
            throw new ArgumentOutOfRangeException("value", (int)value,
                "Unknown tokenizer state.");
        }

        this.state = value;
    }

    internal void SetLastStartTagName(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A last start tag name cannot be empty.", "value");
        }

        char[] folded = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            folded[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        this.lastStartTagName = new string(folded);
        this.hasLastStartTagName = true;
    }

    internal void ClearLastStartTagName()
    {
        this.lastStartTagName = "";
        this.hasLastStartTagName = false;
    }

    internal bool IsAppropriateEndTagName(ReadOnlySpan<char> value)
    {
        if (!this.hasLastStartTagName || value.Length != this.lastStartTagName.Length)
        {
            return false;
        }

        return Ascii.EqualsIgnoreCase(value, this.lastStartTagName.AsSpan());
    }

    internal void ClearScriptTemporaryBuffer()
    {
        this.scriptTemporaryBuffer.Clear();
        this.scriptTemporaryBufferOverflow = false;
    }

    internal bool AppendScriptTemporaryCharacter(char value)
    {
        if (this.scriptTemporaryBuffer.GetLength()
            == this.scriptTemporaryBuffer.GetMaximumCapacity())
        {
            this.scriptTemporaryBufferOverflow = true;
            return true;
        }

        return this.scriptTemporaryBuffer.TryAppend(value);
    }

    internal string GetScriptTemporaryBuffer()
    {
        return this.scriptTemporaryBuffer.GetString();
    }

    internal bool IsScriptTemporaryBuffer(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return !this.scriptTemporaryBufferOverflow
            && Ascii.EqualsIgnoreCase(this.scriptTemporaryBuffer.GetSpan(),
                value.AsSpan());
    }

    internal HtmlSourceSpan CreateSourceSpan(int start, int end)
    {
        if (!this.options.GetTrackSourceLocations())
        {
            return HtmlSourceSpan.CreateUnknown();
        }

        return this.input.GetSourceSpan(start, end);
    }

    internal bool TryEmitToken(HtmlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (this.tokenEmissionLimitExceeded)
        {
            return false;
        }

        if (!this.budget.TryConsume(HtmlLimitKind.Tokens, 1))
        {
            this.StopForLimit(HtmlDiagnosticCode.ResourceLimitExceeded,
                "The tokenizer token limit was exceeded.", true);
            return false;
        }

        if (token.GetKind() == HtmlTokenKind.StartTag)
        {
            this.SetLastStartTagName(token.GetName());
        }

        this.tokens.Enqueue(token);
        return true;
    }

    internal void BeginMarkup(int start)
    {
        if (start < 0 || start > this.input.GetPosition())
        {
            throw new ArgumentOutOfRangeException("start", start,
                "The markup start is outside consumed input.");
        }

        this.markupStart = start;
        this.currentTag = null;
        this.currentComment = null;
        this.currentDoctype = null;
        this.cdataPendingStart = -1;
    }

    internal int GetMarkupStart()
    {
        return this.markupStart;
    }

    internal void BeginTag(HtmlTokenKind kind)
    {
        if (kind != HtmlTokenKind.StartTag && kind != HtmlTokenKind.EndTag)
        {
            throw new ArgumentOutOfRangeException("kind", (int)kind,
                "A tag builder requires a start-tag or end-tag kind.");
        }

        if (this.markupStart < 0)
        {
            throw new InvalidOperationException("A tag requires an active markup start.");
        }

        this.currentTag = new HtmlTagTokenBuilder(kind, this.maximumTokenCharacters,
            this.maximumAttributesPerTag, this.maximumNameCharacters,
            this.maximumAttributeValueCharacters);
        this.currentComment = null;
        this.currentDoctype = null;
    }

    internal HtmlTagTokenBuilder GetCurrentTag()
    {
        if (this.currentTag == null)
        {
            throw new InvalidOperationException("No tag token is being built.");
        }

        return this.currentTag;
    }

    internal void BeginComment()
    {
        if (this.markupStart < 0)
        {
            throw new InvalidOperationException("A comment requires an active markup start.");
        }

        this.currentTag = null;
        this.currentComment = new HtmlCommentTokenBuilder(this.maximumTokenCharacters);
        this.currentDoctype = null;
    }

    internal HtmlCommentTokenBuilder GetCurrentComment()
    {
        if (this.currentComment == null)
        {
            throw new InvalidOperationException("No comment token is being built.");
        }

        return this.currentComment;
    }

    internal void BeginDoctype()
    {
        if (this.markupStart < 0)
        {
            throw new InvalidOperationException("A DOCTYPE requires an active markup start.");
        }

        this.currentTag = null;
        this.currentComment = null;
        this.currentDoctype = new HtmlDoctypeTokenBuilder(
            this.maximumTokenCharacters, this.maximumNameCharacters);
    }

    internal HtmlDoctypeTokenBuilder GetCurrentDoctype()
    {
        if (this.currentDoctype == null)
        {
            throw new InvalidOperationException("No DOCTYPE token is being built.");
        }

        return this.currentDoctype;
    }

    internal bool EmitCurrentDoctype()
    {
        HtmlDoctypeTokenBuilder builder = this.GetCurrentDoctype();
        int end = this.input.GetPosition();
        HtmlToken token = builder.CreateToken(
            this.CreateSourceSpan(this.markupStart, end));
        this.currentDoctype = null;
        this.currentComment = null;
        this.currentTag = null;
        this.markupStart = -1;
        this.state = HtmlTokenizerState.Data;
        bool emitted = this.TryEmitToken(token);
        this.CompactInput();
        return emitted;
    }

    internal bool EmitCurrentComment()
    {
        HtmlCommentTokenBuilder builder = this.GetCurrentComment();
        int end = this.input.GetPosition();
        HtmlToken token = HtmlToken.CreateComment(builder.GetData(),
            this.CreateSourceSpan(this.markupStart, end));
        this.currentComment = null;
        this.currentTag = null;
        this.currentDoctype = null;
        this.markupStart = -1;
        this.state = HtmlTokenizerState.Data;
        bool emitted = this.TryEmitToken(token);
        this.CompactInput();
        return emitted;
    }

    internal void BeginCdataSection()
    {
        if (this.markupStart < 0)
        {
            throw new InvalidOperationException("A CDATA section requires an active markup start.");
        }

        this.currentTag = null;
        this.currentComment = null;
        this.currentDoctype = null;
        this.cdataPendingStart = -1;
    }

    internal void SetCdataPendingStart(int value)
    {
        if (value < 0 || value >= this.input.GetPosition())
        {
            throw new ArgumentOutOfRangeException("value", value,
                "A pending CDATA delimiter must refer to consumed input.");
        }

        this.cdataPendingStart = value;
    }

    internal int GetCdataPendingStart()
    {
        if (this.cdataPendingStart < 0)
        {
            throw new InvalidOperationException("No CDATA delimiter is pending.");
        }

        return this.cdataPendingStart;
    }

    internal bool EmitCdataCharacters(string data, int start, int end)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (data.Length == 0 || data.Length > this.maximumTokenCharacters)
        {
            throw new ArgumentOutOfRangeException("data", data.Length,
                "CDATA character tokens must have a valid bounded length.");
        }

        HtmlToken token = HtmlToken.CreateCharacter(data,
            this.CreateSourceSpan(start, end));
        bool emitted = this.TryEmitToken(token);
        this.CompactInput();
        return emitted;
    }

    internal void EndCdataSection()
    {
        this.currentTag = null;
        this.currentComment = null;
        this.currentDoctype = null;
        this.markupStart = -1;
        this.cdataPendingStart = -1;
        this.state = HtmlTokenizerState.Data;
        this.CompactInput();
    }

    internal bool EmitCurrentTag()
    {
        HtmlTagTokenBuilder builder = this.GetCurrentTag();
        int end = this.input.GetPosition();
        if (builder.GetKind() == HtmlTokenKind.EndTag)
        {
            if (builder.GetAttributeCount() > 0)
            {
                this.AddTokenizerDiagnostic("An end tag contained attributes.",
                    this.markupStart, end);
            }

            if (builder.IsSelfClosing())
            {
                this.AddTokenizerDiagnostic("An end tag had a trailing solidus.",
                    this.markupStart, end);
            }
        }

        HtmlToken token = builder.CreateToken(this,
            this.CreateSourceSpan(this.markupStart, end));
        this.currentTag = null;
        this.currentComment = null;
        this.currentDoctype = null;
        this.markupStart = -1;
        this.state = HtmlTokenizerState.Data;
        bool emitted = this.TryEmitToken(token);
        this.CompactInput();
        return emitted;
    }

    internal void AbandonCurrentTag()
    {
        this.currentTag = null;
        this.currentComment = null;
        this.markupStart = -1;
        this.state = HtmlTokenizerState.Data;
        this.CompactInput();
    }

    internal void AddTokenizerDiagnostic(string message, int start, int end)
    {
        this.AddTokenizerDiagnosticWithCode(HtmlDiagnosticCode.TokenizerUnexpectedCharacter,
            message, start, end);
    }

    internal void AddTokenizerDiagnosticWithCode(HtmlDiagnosticCode code,
        string message, int start, int end)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        this.diagnostics.AddDetails(code, HtmlDiagnosticSeverity.Warning,
            message, this.CreateSourceSpan(start, end));
    }

    internal bool EmitMarkupCharacters(string data)
    {
        return this.EmitMarkupCharactersAndSwitchState(data, HtmlTokenizerState.Data);
    }

    internal bool EmitMarkupCharactersAndSwitchState(string data, HtmlTokenizerState nextState)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (this.markupStart < 0)
        {
            throw new InvalidOperationException("No markup prefix is active.");
        }

        int start = this.markupStart;
        int dataOffset = 0;
        bool emitted = false;
        while (dataOffset < data.Length && !this.tokenEmissionLimitExceeded)
        {
            int length = data.Length - dataOffset;
            if (length > this.maximumTokenCharacters)
            {
                length = this.maximumTokenCharacters;
            }

            if (!this.GetCoalesceCharacterTokens())
            {
                length = 1;
            }

            string part = data.Substring(dataOffset, length);
            HtmlToken token = HtmlToken.CreateCharacter(part,
                this.CreateSourceSpan(start + dataOffset,
                    start + dataOffset + length));
            if (this.TryEmitToken(token))
            {
                emitted = true;
            }

            dataOffset = dataOffset + length;
        }

        this.currentTag = null;
        this.currentComment = null;
        this.currentDoctype = null;
        this.markupStart = -1;
        this.SetState(nextState);
        this.CompactInput();
        return emitted;
    }

    internal void CompactInput()
    {
        this.input.Compact();
    }

    internal void AddCharacterReferenceDiagnostics(HtmlCharacterReferenceResult result,
        int ampersandOffset)
    {
        if (result == null)
        {
            throw new ArgumentNullException("result");
        }

        result.AddDiagnostics(this.diagnostics, this.input, ampersandOffset);
    }

    internal void MarkTokenCharacterLimitExceeded()
    {
        this.StopForLimit(HtmlDiagnosticCode.ResourceLimitExceeded,
            "The tokenizer token character limit was exceeded.", true);
    }

    internal void MarkCurrentTagLimitExceeded()
    {
        HtmlTagTokenBuilder builder = this.GetCurrentTag();
        HtmlLimitKind kind = builder.GetExceededLimit();
        int value = builder.GetExceededLimitValue();
        if (kind == HtmlLimitKind.None || value <= 0)
        {
            throw new InvalidOperationException("The current tag has not exceeded a limit.");
        }

        this.budget.Allows(kind, value);
        this.StopForLimit(HtmlDiagnosticCode.ResourceLimitExceeded,
            "A tokenizer tag resource limit was exceeded.", true);
    }

    internal void MarkCurrentDoctypeLimitExceeded()
    {
        HtmlDoctypeTokenBuilder builder = this.GetCurrentDoctype();
        HtmlLimitKind kind = builder.GetExceededLimit();
        int value = builder.GetExceededLimitValue();
        if (kind == HtmlLimitKind.None || value <= 0)
        {
            throw new InvalidOperationException(
                "The current DOCTYPE has not exceeded a limit.");
        }

        this.budget.Allows(kind, value);
        this.StopForLimit(HtmlDiagnosticCode.ResourceLimitExceeded,
            "A tokenizer DOCTYPE resource limit was exceeded.", true);
    }

    internal bool RunCurrentState()
    {
        if ((int)this.state <= (int)HtmlTokenizerState.PlainText)
        {
            return HtmlTokenizerDataStates.Run(this);
        }

        if ((int)this.state <= (int)HtmlTokenizerState.SelfClosingStartTag)
        {
            return HtmlTokenizerTagStates.Run(this);
        }

        if (this.state == HtmlTokenizerState.Doctype
            || (int)this.state >= (int)HtmlTokenizerState.BeforeDoctypeName
                && (int)this.state <= (int)HtmlTokenizerState.BogusDoctype)
        {
            return HtmlTokenizerDoctypeStates.Run(this);
        }

        if ((int)this.state >= (int)HtmlTokenizerState.ScriptData)
        {
            return HtmlTokenizerScriptStates.Run(this);
        }

        return HtmlTokenizerCommentStates.Run(this);
    }

    internal HtmlTokenReadResult CreateTokenResult()
    {
        HtmlToken? token = this.tokens.Dequeue();
        if (token == null)
        {
            throw new InvalidOperationException(
                "A non-empty token queue must yield a token.");
        }

        if (token.GetKind() != HtmlTokenKind.EndOfFile)
        {
            this.deliveredTokenCount = this.deliveredTokenCount + 1;
        }

        return HtmlTokenReadResult.CreateToken(token, this.TakeDiagnostics(),
            this.terminalLimitExceeded, this.diagnostics.WasTruncated(),
            this.diagnostics.GetDroppedCount());
    }

    internal HtmlTokenReadResult CreateLimitResult()
    {
        return HtmlTokenReadResult.CreateStatus(HtmlTokenReadStatus.LimitExceeded,
            this.TakeDiagnostics(), this.deliveredTokenCount > 0,
            this.diagnostics.WasTruncated(), this.diagnostics.GetDroppedCount());
    }

    internal void StopForLimit(HtmlDiagnosticCode code, string message,
        bool stopTokenEmission)
    {
        this.acceptingInput = false;
        this.terminalLimitExceeded = true;
        if (stopTokenEmission)
        {
            this.tokenEmissionLimitExceeded = true;
        }
        if (this.limitDiagnosticReported)
        {
            return;
        }

        this.limitDiagnosticReported = true;
        int position = this.input.GetPosition();
        this.diagnostics.AddDetails(code, HtmlDiagnosticSeverity.Error, message,
            this.CreateSourceSpan(position, position));
    }

    internal HtmlDiagnostic[] TakeDiagnostics()
    {
        HtmlDiagnostic[] all = this.diagnostics.GetDiagnostics();
        int count = all.Length - this.deliveredDiagnosticCount;
        if (count <= 0)
        {
            return new HtmlDiagnostic[0];
        }

        HtmlDiagnostic[] result = new HtmlDiagnostic[count];
        Array.Copy(all, this.deliveredDiagnosticCount, result, 0, count);
        this.deliveredDiagnosticCount = all.Length;
        return result;
    }
}

internal class HtmlTokenQueue
{
    internal HtmlToken?[] tokens;
    internal int start;
    internal int count;

    internal HtmlTokenQueue()
    {
        this.tokens = new HtmlToken[4];
        this.start = 0;
        this.count = 0;
    }

    internal int GetCount()
    {
        return this.count;
    }

    internal void Enqueue(HtmlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (this.start + this.count == this.tokens.Length)
        {
            this.MakeRoom();
        }

        this.tokens[this.start + this.count] = token;
        this.count = this.count + 1;
    }

    internal HtmlToken? Dequeue()
    {
        if (this.count == 0)
        {
            throw new InvalidOperationException("The token queue is empty.");
        }

        HtmlToken? token = this.tokens[this.start];
        this.tokens[this.start] = null;
        this.start = this.start + 1;
        this.count = this.count - 1;
        if (this.count == 0)
        {
            this.start = 0;
        }

        return token;
    }

    internal void MakeRoom()
    {
        if (this.start > 0)
        {
            Array.Copy(this.tokens, this.start, this.tokens, 0, this.count);
            this.start = 0;
            return;
        }

        if (this.tokens.Length > int.MaxValue / 2)
        {
            throw new InsufficientMemoryException("The token queue cannot represent another token.");
        }

        HtmlToken[] expanded = new HtmlToken[this.tokens.Length * 2];
        Array.Copy(this.tokens, expanded, this.count);
        this.tokens = expanded;
    }
}
