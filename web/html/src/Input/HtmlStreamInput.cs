using System;
using System.IO;

namespace html;

internal class HtmlTextReaderInput
{
    internal const int DefaultBufferCharacters = 4096;
    internal const int MaximumBufferCharacters = 65536;

    internal TextReader reader;
    internal bool leaveOpen;
    internal HtmlBudget budget;
    internal HtmlDiagnosticCollector diagnostics;
    internal HtmlInput input;
    internal char[] buffer;
    internal int maximumInputCharacters;
    internal bool sourceEnded;
    internal bool limitExceeded;
    internal bool closed;

    internal HtmlTextReaderInput(TextReader reader, bool leaveOpen, int bufferCharacters,
        HtmlBudget budget, HtmlDiagnosticCollector diagnostics)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        if (bufferCharacters <= 0 || bufferCharacters > MaximumBufferCharacters)
        {
            throw new ArgumentOutOfRangeException("bufferCharacters", bufferCharacters,
                "The TextReader buffer size must be between 1 and 65536 characters.");
        }

        this.reader = reader;
        this.leaveOpen = leaveOpen;
        this.budget = budget;
        this.diagnostics = diagnostics;
        this.input = HtmlInput.Create(budget, diagnostics);
        this.buffer = new char[bufferCharacters];
        this.maximumInputCharacters = budget.GetLimits().GetMaximumInputCharacters();
        this.sourceEnded = false;
        this.limitExceeded = false;
        this.closed = false;
    }

    internal static HtmlTextReaderInput Create(TextReader reader, bool leaveOpen,
        HtmlBudget budget, HtmlDiagnosticCollector diagnostics)
    {
        return new HtmlTextReaderInput(reader, leaveOpen, DefaultBufferCharacters, budget, diagnostics);
    }

    internal static HtmlTextReaderInput Create(TextReader reader, bool leaveOpen, int bufferCharacters,
        HtmlBudget budget, HtmlDiagnosticCollector diagnostics)
    {
        return new HtmlTextReaderInput(reader, leaveOpen, bufferCharacters, budget, diagnostics);
    }

    internal int Peek()
    {
        return this.PeekAhead(0);
    }

    internal int PeekAhead(int lookahead)
    {
        this.EnsureOpen();
        if (lookahead < 0)
        {
            throw new ArgumentOutOfRangeException("lookahead", lookahead, "Lookahead cannot be negative.");
        }

        this.EnsureAvailable(lookahead);
        return this.input.PeekAhead(lookahead);
    }

    internal int Read()
    {
        this.EnsureOpen();
        this.EnsureAvailable(0);
        return this.input.Read();
    }

    internal void Reconsume()
    {
        this.EnsureOpen();
        this.input.Reconsume();
    }

    internal bool ReadMore()
    {
        this.EnsureOpen();
        if (this.sourceEnded)
        {
            return false;
        }

        this.budget.CheckCancellation();
        int used = this.budget.GetUsed(HtmlLimitKind.InputCharacters);
        int remaining = this.maximumInputCharacters - used;
        if (remaining == 0)
        {
            int next = this.reader.Peek();
            this.budget.CheckCancellation();
            if (next < 0)
            {
                this.EndSource();
            }
            else
            {
                this.budget.TryConsume(HtmlLimitKind.InputCharacters, 1);
                this.EndForLimit();
            }

            return false;
        }

        int requested = this.buffer.Length < remaining ? this.buffer.Length : remaining;
        int read = this.reader.Read(this.buffer, 0, requested);
        this.budget.CheckCancellation();
        if (read < 0 || read > requested)
        {
            throw new InvalidDataException("TextReader returned an invalid character count.");
        }

        if (read == 0)
        {
            this.EndSource();
            return false;
        }

        if (!this.input.TryAppendSpan(new ReadOnlySpan<char>(this.buffer, 0, read)))
        {
            this.EndForLimit();
            return false;
        }

        return true;
    }

    internal bool IsEnd()
    {
        this.EnsureOpen();
        this.EnsureAvailable(0);
        return this.input.IsEnd();
    }

    internal bool IsSourceComplete()
    {
        return this.sourceEnded;
    }

    internal bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    internal bool GetLeaveOpen()
    {
        return this.leaveOpen;
    }

    internal int GetBufferSize()
    {
        return this.buffer.Length;
    }

    internal int GetPosition()
    {
        return this.input.GetPosition();
    }

    internal int GetLength()
    {
        return this.input.GetLength();
    }

    internal int GetOriginalCharacterLength()
    {
        return this.input.GetOriginalCharacterLength();
    }

    internal string GetString()
    {
        return this.input.GetString();
    }

    internal HtmlSourcePosition GetSourcePosition()
    {
        return this.input.GetSourcePosition();
    }

    internal HtmlSourcePosition GetSourcePositionAt(int normalizedCharacterOffset)
    {
        return this.input.GetSourcePositionAt(normalizedCharacterOffset);
    }

    internal HtmlSourceSpan GetSourceSpan(int normalizedStart, int normalizedEnd)
    {
        return this.input.GetSourceSpan(normalizedStart, normalizedEnd);
    }

    internal void Close()
    {
        if (this.closed)
        {
            return;
        }

        this.closed = true;
        if (!this.leaveOpen)
        {
            this.reader.Dispose();
        }
    }

    internal void EnsureAvailable(int lookahead)
    {
        while (lookahead >= this.input.GetAvailableLength() && !this.sourceEnded)
        {
            this.ReadMore();
        }
    }

    internal void EndSource()
    {
        this.input.Complete();
        this.sourceEnded = true;
    }

    internal void EndForLimit()
    {
        if (this.limitExceeded)
        {
            return;
        }

        this.input.Complete();
        this.sourceEnded = true;
        this.limitExceeded = true;
        int offset = this.input.GetLength();
        this.diagnostics.AddDetails(HtmlDiagnosticCode.InputLimitExceeded, HtmlDiagnosticSeverity.Error,
            "The input character limit was exceeded.", this.input.GetSourceSpan(offset, offset));
    }

    internal void EnsureOpen()
    {
        if (this.closed)
        {
            throw new ObjectDisposedException("HtmlTextReaderInput");
        }
    }
}
