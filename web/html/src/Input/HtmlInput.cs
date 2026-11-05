using System;

namespace html;

internal class HtmlInput
{
    internal HtmlBudget budget;
    internal HtmlDiagnosticCollector diagnostics;
    internal char[] characters;
    internal int bufferStart;
    internal int length;
    internal int position;
    internal int originalCharacterOffset;
    internal HtmlLineMap lineMap;
    internal HtmlDecodedInput? decodedInput;
    internal bool countInputCharacters;
    internal bool completed;
    internal bool hasPendingCharacter;
    internal char pendingCharacter;
    internal int pendingCharacterOffset;

    internal HtmlInput(HtmlBudget budget, HtmlDiagnosticCollector diagnostics,
        bool countInputCharactersValue)
    {
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        this.budget = budget;
        this.diagnostics = diagnostics;
        this.characters = new char[16];
        this.bufferStart = 0;
        this.length = 0;
        this.position = 0;
        this.originalCharacterOffset = 0;
        this.lineMap = HtmlLineMap.Create();
        this.decodedInput = null;
        this.countInputCharacters = countInputCharactersValue;
        this.completed = false;
        this.hasPendingCharacter = false;
        this.pendingCharacter = '\0';
        this.pendingCharacterOffset = 0;
    }

    internal static HtmlInput Create(HtmlBudget budget, HtmlDiagnosticCollector diagnostics)
    {
        return new HtmlInput(budget, diagnostics, true);
    }

    internal static HtmlInput Create(HtmlBudget budget,
        HtmlDiagnosticCollector diagnostics, bool countInputCharacters)
    {
        return new HtmlInput(budget, diagnostics,
            countInputCharacters);
    }

    internal bool TryAppend(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        return this.TryAppendSpan(input.AsSpan());
    }

    internal void EnableByteOffsets(HtmlDecodedInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        if (this.length != 0 || this.originalCharacterOffset != 0
            || this.hasPendingCharacter || this.completed)
        {
            throw new InvalidOperationException(
                "Byte offsets must be enabled before input is appended.");
        }

        this.decodedInput = input;
        this.lineMap = HtmlLineMap.CreateWithByteOffsets(
            input.GetByteOffset(0));
    }

    internal bool TryAppendSpan(ReadOnlySpan<char> input)
    {
        if (this.completed)
        {
            throw new InvalidOperationException("Cannot append input after completion.");
        }

        if (this.countInputCharacters
            && !this.budget.TryConsume(HtmlLimitKind.InputCharacters,
                input.Length))
        {
            return false;
        }

        this.budget.CheckCancellation();

        int index = 0;
        if (this.hasPendingCharacter && input.Length > 0)
        {
            index = this.ResolvePendingCharacter(input);
        }

        while (index < input.Length)
        {
            this.budget.CheckCancellation();
            char value = input[index];
            int characterOffset = this.originalCharacterOffset;
            this.originalCharacterOffset = this.originalCharacterOffset + 1;
            index = index + 1;

            if (value == '\r')
            {
                if (index == input.Length)
                {
                    this.SetPendingCharacter(value, characterOffset);
                }
                else if (input[index] == '\n')
                {
                    this.originalCharacterOffset = this.originalCharacterOffset + 1;
                    index = index + 1;
                    this.AppendNormalized('\n', characterOffset, this.originalCharacterOffset);
                }
                else
                {
                    this.AppendNormalized('\n', characterOffset, this.originalCharacterOffset);
                }
            }
            else if (UnicodeTools.IsHighSurrogate(value))
            {
                if (index == input.Length)
                {
                    this.SetPendingCharacter(value, characterOffset);
                }
                else if (UnicodeTools.IsLowSurrogate(input[index]))
                {
                    this.AppendNormalized(value, characterOffset, this.originalCharacterOffset);
                    int lowOffset = this.originalCharacterOffset;
                    this.originalCharacterOffset = this.originalCharacterOffset + 1;
                    this.AppendNormalized(input[index], lowOffset, this.originalCharacterOffset);
                    index = index + 1;
                }
                else
                {
                    this.AppendReplacement(HtmlDiagnosticCode.InputInvalidScalar,
                        "An unpaired UTF-16 high surrogate was replaced.", characterOffset, this.originalCharacterOffset);
                }
            }
            else if (UnicodeTools.IsLowSurrogate(value))
            {
                this.AppendReplacement(HtmlDiagnosticCode.InputInvalidScalar,
                    "An unpaired UTF-16 low surrogate was replaced.", characterOffset, this.originalCharacterOffset);
            }
            else if (value == '\0')
            {
                this.AppendReplacement(HtmlDiagnosticCode.InputNullCharacter,
                    "A null character was replaced.", characterOffset, this.originalCharacterOffset);
            }
            else
            {
                this.AppendNormalized(value, characterOffset, this.originalCharacterOffset);
            }
        }

        return true;
    }

    internal void Complete()
    {
        this.budget.CheckCancellation();
        if (this.completed)
        {
            return;
        }

        if (this.hasPendingCharacter)
        {
            if (this.pendingCharacter == '\r')
            {
                this.AppendNormalized('\n', this.pendingCharacterOffset, this.originalCharacterOffset);
            }
            else
            {
                this.AppendReplacement(HtmlDiagnosticCode.InputInvalidScalar,
                    "An unpaired UTF-16 high surrogate was replaced.", this.pendingCharacterOffset, this.originalCharacterOffset);
            }

            this.ClearPendingCharacter();
        }

        this.completed = true;
    }

    internal int Peek()
    {
        return this.PeekAhead(0);
    }

    internal int PeekAhead(int lookahead)
    {
        this.budget.CheckCancellation();
        if (lookahead < 0)
        {
            throw new ArgumentOutOfRangeException("lookahead", lookahead, "Lookahead cannot be negative.");
        }

        if (lookahead >= this.length - this.position)
        {
            return -1;
        }

        return this.characters[this.position + lookahead - this.bufferStart];
    }

    internal int Read()
    {
        this.budget.CheckCancellation();
        if (this.position == this.length)
        {
            return -1;
        }

        char value = this.characters[this.position - this.bufferStart];
        this.position = this.position + 1;
        return value;
    }

    internal void Reconsume()
    {
        this.budget.CheckCancellation();
        if (this.position == this.bufferStart)
        {
            throw new InvalidOperationException("No consumed character is available to reconsume.");
        }

        this.position = this.position - 1;
    }

    internal int GetLength()
    {
        return this.length;
    }

    internal int GetPosition()
    {
        return this.position;
    }

    internal int GetAvailableLength()
    {
        return this.length - this.position;
    }

    internal ReadOnlySpan<char> GetAvailableSpan()
    {
        return new ReadOnlySpan<char>(this.characters,
            this.position - this.bufferStart, this.length - this.position);
    }

    internal int GetBufferStart()
    {
        return this.bufferStart;
    }

    internal int GetBufferedLength()
    {
        return this.length - this.bufferStart;
    }

    internal int GetOriginalCharacterLength()
    {
        return this.originalCharacterOffset;
    }

    internal bool IsComplete()
    {
        return this.completed;
    }

    internal bool IsEnd()
    {
        return this.completed && this.position == this.length;
    }

    internal string GetString()
    {
        return new string(this.characters, 0, this.length - this.bufferStart);
    }

    internal void Compact()
    {
        this.budget.CheckCancellation();
        if (this.position == this.bufferStart)
        {
            return;
        }

        int discarded = this.position - this.bufferStart;
        int remaining = this.length - this.position;
        if (remaining > 0)
        {
            Array.Copy(this.characters, discarded, this.characters, 0, remaining);
        }

        this.bufferStart = this.position;
        if (this.characters.Length > 65536 && remaining < this.characters.Length / 4)
        {
            int capacity = remaining < 16 ? 16 : remaining * 2;
            char[] compacted = new char[capacity];
            Array.Copy(this.characters, compacted, remaining);
            this.characters = compacted;
        }
    }

    internal HtmlSourcePosition GetSourcePosition()
    {
        return this.lineMap.GetPosition(this.position);
    }

    internal HtmlSourcePosition GetSourcePositionAt(int normalizedCharacterOffset)
    {
        return this.lineMap.GetPosition(normalizedCharacterOffset);
    }

    internal HtmlSourceSpan GetSourceSpan(int normalizedStart, int normalizedEnd)
    {
        return this.lineMap.GetSpan(normalizedStart, normalizedEnd);
    }

    internal int ResolvePendingCharacter(ReadOnlySpan<char> input)
    {
        int index = 0;
        char pending = this.pendingCharacter;
        int pendingOffset = this.pendingCharacterOffset;
        this.ClearPendingCharacter();
        if (pending == '\r')
        {
            if (input.Length > 0 && input[0] == '\n')
            {
                this.originalCharacterOffset = this.originalCharacterOffset + 1;
                index = 1;
            }

            this.AppendNormalized('\n', pendingOffset, this.originalCharacterOffset);
            return index;
        }

        if (input.Length > 0 && UnicodeTools.IsLowSurrogate(input[0]))
        {
            this.AppendNormalized(pending, pendingOffset, pendingOffset + 1);
            int lowOffset = this.originalCharacterOffset;
            this.originalCharacterOffset = this.originalCharacterOffset + 1;
            this.AppendNormalized(input[0], lowOffset, this.originalCharacterOffset);
            return 1;
        }

        this.AppendReplacement(HtmlDiagnosticCode.InputInvalidScalar,
            "An unpaired UTF-16 high surrogate was replaced.", pendingOffset, this.originalCharacterOffset);
        return index;
    }

    internal void SetPendingCharacter(char value, int characterOffset)
    {
        this.hasPendingCharacter = true;
        this.pendingCharacter = value;
        this.pendingCharacterOffset = characterOffset;
    }

    internal void ClearPendingCharacter()
    {
        this.hasPendingCharacter = false;
        this.pendingCharacter = '\0';
        this.pendingCharacterOffset = 0;
    }

    internal void AppendReplacement(HtmlDiagnosticCode code, string message, int characterOffset, int nextCharacterOffset)
    {
        int normalizedOffset = this.length;
        this.AppendNormalized((char)UnicodeTools.ReplacementCharacter, characterOffset, nextCharacterOffset);
        this.diagnostics.AddDetails(code, HtmlDiagnosticSeverity.Warning, message,
            this.lineMap.GetSpan(normalizedOffset, normalizedOffset + 1));
    }

    internal void AppendNormalized(char value, int characterOffset, int nextCharacterOffset)
    {
        int bufferedLength = this.length - this.bufferStart;
        this.EnsureCapacity(bufferedLength + 1);
        this.characters[bufferedLength] = value;
        if (this.decodedInput == null)
        {
            this.lineMap.AppendWithCharacterOffsets(value, characterOffset,
                nextCharacterOffset);
        }
        else
        {
            this.lineMap.AppendWithByteOffsets(value, characterOffset,
                nextCharacterOffset,
                this.decodedInput.GetByteOffset(characterOffset),
                this.decodedInput.GetByteOffset(nextCharacterOffset));
        }
        this.length = this.length + 1;
    }

    internal void EnsureCapacity(int required)
    {
        if (required <= this.characters.Length)
        {
            return;
        }

        int capacity = this.characters.Length;
        while (capacity < required)
        {
            if (capacity > int.MaxValue / 2)
            {
                capacity = int.MaxValue;
            }
            else
            {
                capacity = capacity * 2;
            }

            if (capacity == int.MaxValue)
            {
                break;
            }
        }

        if (capacity < required)
        {
            throw new InsufficientMemoryException("The normalized input cannot represent the requested capacity.");
        }

        char[] expanded = new char[capacity];
        Array.Copy(this.characters, expanded, this.length - this.bufferStart);
        this.characters = expanded;
    }
}
