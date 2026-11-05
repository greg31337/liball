namespace asn1;

internal class Utf8ChunkDecoder
{
    internal byte[] PendingBytes = new byte[4];
    internal int PendingCount;
    internal bool FinalChunkSupplied;

    internal string DecodeChunk(byte[] data, bool isFinal)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (this.FinalChunkSupplied)
        {
            throw new System.InvalidOperationException("No UTF-8 chunk can follow the final chunk.");
        }

        if (data.Length > int.MaxValue - this.PendingCount)
        {
            throw new System.ArgumentOutOfRangeException("data");
        }

        byte[] combined = new byte[this.PendingCount + data.Length];
        System.Array.Copy(this.PendingBytes, 0, combined, 0, this.PendingCount);
        System.Array.Copy(data, 0, combined, this.PendingCount, data.Length);
        this.PendingCount = 0;

        TextAccumulator text = new TextAccumulator(combined.Length);
        int index = 0;
        while (index < combined.Length)
        {
            int sequenceLength = this.GetSequenceLength(combined[index]);
            if (sequenceLength == 0)
            {
                text.AppendCharacter('\uFFFD');
                index = index + 1;
                continue;
            }

            if (sequenceLength > combined.Length - index)
            {
                if (!isFinal)
                {
                    this.StorePendingBytes(combined, index);
                }
                else
                {
                    text.AppendCharacter('\uFFFD');
                }

                index = combined.Length;
                continue;
            }

            if (!this.IsValidSequence(combined, index, sequenceLength))
            {
                text.AppendCharacter('\uFFFD');
                index = index + 1;
                continue;
            }

            int scalarValue = this.DecodeScalar(combined, index, sequenceLength);
            this.AppendScalar(text, scalarValue);
            index = index + sequenceLength;
        }

        this.FinalChunkSupplied = isFinal;
        return text.ToText();
    }

    internal int GetSequenceLength(byte first)
    {
        if (first <= 127)
        {
            return 1;
        }

        if (first >= 194 && first <= 223)
        {
            return 2;
        }

        if (first >= 224 && first <= 239)
        {
            return 3;
        }

        if (first >= 240 && first <= 244)
        {
            return 4;
        }

        return 0;
    }

    internal bool IsValidSequence(byte[] data, int offset, int length)
    {
        int index = 1;
        while (index < length)
        {
            byte value = data[offset + index];
            if (value < 128 || value > 191)
            {
                return false;
            }

            index = index + 1;
        }

        byte first = data[offset];
        byte second = length > 1 ? data[offset + 1] : (byte)0;
        if (first == 224 && second < 160)
        {
            return false;
        }

        if (first == 237 && second > 159)
        {
            return false;
        }

        if (first == 240 && second < 144)
        {
            return false;
        }

        if (first == 244 && second > 143)
        {
            return false;
        }

        return true;
    }

    internal int DecodeScalar(byte[] data, int offset, int length)
    {
        if (length == 1)
        {
            return data[offset];
        }

        if (length == 2)
        {
            return ((data[offset] & 31) << 6)
                | (data[offset + 1] & 63);
        }

        if (length == 3)
        {
            return ((data[offset] & 15) << 12)
                | ((data[offset + 1] & 63) << 6)
                | (data[offset + 2] & 63);
        }

        return ((data[offset] & 7) << 18)
            | ((data[offset + 1] & 63) << 12)
            | ((data[offset + 2] & 63) << 6)
            | (data[offset + 3] & 63);
    }

    internal void AppendScalar(TextAccumulator text, int scalarValue)
    {
        if (scalarValue <= 65535)
        {
            text.AppendCharacter((char)scalarValue);
            return;
        }

        int adjusted = scalarValue - 65536;
        text.AppendCharacter((char)(55296 + adjusted / 1024));
        text.AppendCharacter((char)(56320 + adjusted % 1024));
    }

    internal void StorePendingBytes(byte[] data, int offset)
    {
        this.PendingCount = data.Length - offset;
        int index = 0;
        while (index < this.PendingCount)
        {
            this.PendingBytes[index] = data[offset + index];
            index = index + 1;
        }
    }
}
