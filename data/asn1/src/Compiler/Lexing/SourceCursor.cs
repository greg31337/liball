namespace asn1;

internal class SourceCursor
{
    internal string SourceName;
    internal string Chunk = "";
    internal int ChunkIndex;
    internal bool FinalChunkSupplied;
    internal bool HasSuppliedChunk;
    internal bool StartProcessed;
    internal bool HasPendingCarriageReturn;
    internal bool HasPendingHighSurrogate;
    internal char PendingHighSurrogate;
    internal AsnSourcePosition PendingStart = new AsnSourcePosition();
    internal int Offset;
    internal int Line = 1;
    internal int Column = 1;

    internal SourceCursor(string sourceName)
    {
        this.SourceName = sourceName == null ? throw new System.ArgumentNullException("sourceName") : sourceName;
    }

    internal void SupplyChunk(string chunk, bool isFinal)
    {
        if (chunk == null)
        {
            throw new System.ArgumentNullException("chunk");
        }

        if (this.FinalChunkSupplied)
        {
            throw new System.InvalidOperationException("No chunk can follow the final chunk.");
        }

        if (this.HasSuppliedChunk && this.ChunkIndex < this.Chunk.Length)
        {
            throw new System.InvalidOperationException("The current chunk has not been consumed.");
        }

        this.Chunk = chunk;
        this.ChunkIndex = 0;
        this.FinalChunkSupplied = isFinal;
        this.HasSuppliedChunk = true;
    }

    internal SourceCursorReadResult ReadNext()
    {
        while (true)
        {
            if (this.HasPendingCarriageReturn)
            {
                if (this.ChunkIndex < this.Chunk.Length && this.Chunk[this.ChunkIndex] == '\n')
                {
                    this.ChunkIndex = this.ChunkIndex + 1;
                    this.HasPendingCarriageReturn = false;
                    return this.CreateLineBreakResult(this.PendingStart, 2, "\r\n");
                }

                if (this.ChunkIndex < this.Chunk.Length || this.FinalChunkSupplied)
                {
                    this.HasPendingCarriageReturn = false;
                    return this.CreateLineBreakResult(this.PendingStart, 1, "\r");
                }

                return this.CreateStatusResult(SourceCursorStatus.NeedMoreData);
            }

            if (this.HasPendingHighSurrogate)
            {
                if (this.ChunkIndex < this.Chunk.Length && char.IsLowSurrogate(this.Chunk[this.ChunkIndex]))
                {
                    char low = this.Chunk[this.ChunkIndex];
                    this.ChunkIndex = this.ChunkIndex + 1;
                    int scalar = 65536 + ((this.PendingHighSurrogate - 55296) * 1024) + (low - 56320);
                    this.HasPendingHighSurrogate = false;
                    return this.CreateScalarResult(
                        this.PendingStart,
                        scalar,
                        2,
                        SourceCursorStatus.Value,
                        new string([this.PendingHighSurrogate, low]));
                }

                if (this.ChunkIndex < this.Chunk.Length || this.FinalChunkSupplied)
                {
                    this.HasPendingHighSurrogate = false;
                    return this.CreateScalarResult(
                        this.PendingStart,
                        65533,
                        1,
                        SourceCursorStatus.Invalid,
                        new string(this.PendingHighSurrogate, 1));
                }

                return this.CreateStatusResult(SourceCursorStatus.NeedMoreData);
            }

            if (this.ChunkIndex >= this.Chunk.Length)
            {
                if (this.FinalChunkSupplied)
                {
                    return this.CreateStatusResult(SourceCursorStatus.EndOfInput);
                }

                return this.CreateStatusResult(SourceCursorStatus.NeedMoreData);
            }

            AsnSourcePosition start = this.GetPosition();
            char value = this.Chunk[this.ChunkIndex];

            if (!this.StartProcessed)
            {
                this.StartProcessed = true;
                if (value == '\uFEFF')
                {
                    this.ChunkIndex = this.ChunkIndex + 1;
                    this.AdvanceBom();
                    continue;
                }
            }

            if (value == '\r')
            {
                this.ChunkIndex = this.ChunkIndex + 1;
                if (this.ChunkIndex < this.Chunk.Length)
                {
                    if (this.Chunk[this.ChunkIndex] == '\n')
                    {
                        this.ChunkIndex = this.ChunkIndex + 1;
                        return this.CreateLineBreakResult(start, 2, "\r\n");
                    }

                    return this.CreateLineBreakResult(start, 1, "\r");
                }

                if (this.FinalChunkSupplied)
                {
                    return this.CreateLineBreakResult(start, 1, "\r");
                }

                this.HasPendingCarriageReturn = true;
                this.PendingStart = start;
                return this.CreateStatusResult(SourceCursorStatus.NeedMoreData);
            }

            if (value == '\n')
            {
                this.ChunkIndex = this.ChunkIndex + 1;
                return this.CreateLineBreakResult(start, 1, "\n");
            }

            if (value == '\v')
            {
                this.ChunkIndex = this.ChunkIndex + 1;
                return this.CreateLineBreakResult(start, 1, "\v");
            }

            if (value == '\f')
            {
                this.ChunkIndex = this.ChunkIndex + 1;
                return this.CreateLineBreakResult(start, 1, "\f");
            }

            if (char.IsHighSurrogate(value))
            {
                this.ChunkIndex = this.ChunkIndex + 1;
                if (this.ChunkIndex < this.Chunk.Length)
                {
                    char low = this.Chunk[this.ChunkIndex];
                    if (char.IsLowSurrogate(low))
                    {
                        this.ChunkIndex = this.ChunkIndex + 1;
                        int scalar = 65536 + ((value - 55296) * 1024) + (low - 56320);
                        return this.CreateScalarResult(
                            start,
                            scalar,
                            2,
                            SourceCursorStatus.Value,
                            new string([value, low]));
                    }

                    return this.CreateScalarResult(start, 65533, 1, SourceCursorStatus.Invalid, new string(value, 1));
                }

                if (this.FinalChunkSupplied)
                {
                    return this.CreateScalarResult(start, 65533, 1, SourceCursorStatus.Invalid, new string(value, 1));
                }

                this.HasPendingHighSurrogate = true;
                this.PendingHighSurrogate = value;
                this.PendingStart = start;
                return this.CreateStatusResult(SourceCursorStatus.NeedMoreData);
            }

            this.ChunkIndex = this.ChunkIndex + 1;
            if (char.IsLowSurrogate(value))
            {
                return this.CreateScalarResult(start, 65533, 1, SourceCursorStatus.Invalid, new string(value, 1));
            }

            return this.CreateScalarResult(start, value, 1, SourceCursorStatus.Value, new string(value, 1));
        }
    }

    internal AsnSourcePosition GetPosition()
    {
        return new AsnSourcePosition
        {
            SourceName = this.SourceName,
            Offset = this.Offset,
            Line = this.Line,
            Column = this.Column
        };
    }

    internal int GetConsumedCharacterCount()
    {
        return this.Offset;
    }

    internal SourceCursorReadResult CreateScalarResult(
        AsnSourcePosition start,
        int scalarValue,
        int utf16Length,
        SourceCursorStatus status,
        string text)
    {
        this.AdvanceScalar(utf16Length);
        return new SourceCursorReadResult
        {
            Status = status,
            ScalarValue = scalarValue,
            Utf16Length = utf16Length,
            IsLineBreak = false,
            Text = text,
            Start = start,
            End = this.GetPosition()
        };
    }

    internal SourceCursorReadResult CreateLineBreakResult(AsnSourcePosition start, int utf16Length, string text)
    {
        this.AdvanceLineBreak(utf16Length);
        return new SourceCursorReadResult
        {
            Status = SourceCursorStatus.Value,
            ScalarValue = 10,
            Utf16Length = utf16Length,
            IsLineBreak = true,
            Text = text,
            Start = start,
            End = this.GetPosition()
        };
    }

    internal SourceCursorReadResult CreateStatusResult(SourceCursorStatus status)
    {
        AsnSourcePosition position = this.GetPosition();
        return new SourceCursorReadResult
        {
            Status = status,
            Start = position,
            End = this.GetPosition()
        };
    }

    internal void AdvanceScalar(int utf16Length)
    {
        if (utf16Length < 0 || utf16Length > int.MaxValue - this.Offset)
        {
            throw new System.ArgumentOutOfRangeException("utf16Length");
        }

        this.Offset = this.Offset + utf16Length;
        this.Column = this.Column + 1;
    }

    internal void AdvanceLineBreak(int utf16Length)
    {
        if (utf16Length < 0 || utf16Length > int.MaxValue - this.Offset)
        {
            throw new System.ArgumentOutOfRangeException("utf16Length");
        }

        this.Offset = this.Offset + utf16Length;
        this.Line = this.Line + 1;
        this.Column = 1;
    }

    internal void AdvanceBom()
    {
        if (this.Offset == int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("Offset");
        }

        this.Offset = this.Offset + 1;
    }
}
