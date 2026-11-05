namespace gif;

internal class GifLzwCodeWriteResult
{
    internal bool Success;
    internal int Code;
    internal int CodeSize;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifLzwCodeWriteResult()
    {
        Success = false;
        Code = 0;
        CodeSize = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLzwBitWriter
{
    internal byte[] Buffer;
    internal int ByteOffset;
    internal int BitOffset;
    internal long TotalBitsWritten;

    internal GifLzwBitWriter(int capacity)
    {
        if (capacity <= 0)
        {
            capacity = 1;
        }

        Buffer = new byte[capacity];
        ByteOffset = 0;
        BitOffset = 0;
        TotalBitsWritten = 0;
    }

    internal GifLzwCodeWriteResult WriteCode(int code, int codeSize)
    {
        GifLzwCodeWriteResult result = ValidateCode(code, codeSize);
        if (!result.Success)
        {
            return result;
        }

        int bitIndex = 0;
        while (bitIndex < codeSize)
        {
            int bit = (code >> bitIndex) & 1;
            WriteBit(bit);
            bitIndex = bitIndex + 1;
        }

        result.BytesWritten = ByteCount();
        return result;
    }

    internal GifLzwCodeWriteResult ValidateCode(int code, int codeSize)
    {
        GifLzwCodeWriteResult result = new GifLzwCodeWriteResult();
        result.Code = code;
        result.CodeSize = codeSize;
        result.BytesWritten = ByteCount();
        if (codeSize <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code size must be positive.", new GifDiagnosticPosition(ByteOffset, -1, "LzwBitWriter"));
            return result;
        }

        if (codeSize > GifConstants.MaximumLzwCodeSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code size exceeds the GIF maximum.", new GifDiagnosticPosition(ByteOffset, -1, "LzwBitWriter"));
            return result;
        }

        if (code < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code must be non-negative.", new GifDiagnosticPosition(ByteOffset, -1, "LzwBitWriter"));
            return result;
        }

        if (code >= CodeLimit(codeSize))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code does not fit in the requested code size.", new GifDiagnosticPosition(ByteOffset, -1, "LzwBitWriter"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal int CodeLimit(int codeSize)
    {
        if (codeSize <= 0)
        {
            return 0;
        }

        if (codeSize >= GifConstants.MaximumLzwCodeSize)
        {
            return GifConstants.MaximumLzwTableSize;
        }

        return 1 << codeSize;
    }

    internal void WriteBit(int bit)
    {
        EnsureCapacity(ByteOffset + 1);
        if (bit != 0)
        {
            Buffer[ByteOffset] = (byte)(Buffer[ByteOffset] | (byte)(1 << BitOffset));
        }

        BitOffset = BitOffset + 1;
        TotalBitsWritten = TotalBitsWritten + 1;
        if (BitOffset == 8)
        {
            BitOffset = 0;
            ByteOffset = ByteOffset + 1;
        }
    }

    internal int ByteCount()
    {
        if (BitOffset > 0)
        {
            return ByteOffset + 1;
        }

        return ByteOffset;
    }

    internal byte[] ToArray()
    {
        int count = ByteCount();
        byte[] data = new byte[count];
        int index = 0;
        while (index < count)
        {
            data[index] = Buffer[index];
            index = index + 1;
        }

        return data;
    }

    internal void Clear()
    {
        int index = 0;
        while (index < Buffer.Length)
        {
            Buffer[index] = 0;
            index = index + 1;
        }

        ByteOffset = 0;
        BitOffset = 0;
        TotalBitsWritten = 0;
    }

    internal void EnsureCapacity(int capacity)
    {
        if (Buffer.Length >= capacity)
        {
            return;
        }

        int newCapacity = capacity;
        if (Buffer.Length <= 1073741823)
        {
            newCapacity = Buffer.Length * 2;
            if (newCapacity < capacity)
            {
                newCapacity = capacity;
            }
        }

        if (newCapacity <= 0)
        {
            newCapacity = 1;
        }

        byte[] expanded = new byte[newCapacity];
        int index = 0;
        while (index < Buffer.Length)
        {
            expanded[index] = Buffer[index];
            index = index + 1;
        }

        Buffer = expanded;
    }
}

internal class GifLzwEncoderDictionary
{
    internal int MinimumCodeSize;
    internal int RootCodeCount;
    internal int ClearCode;
    internal int EndCode;
    internal int NextCode;
    internal int[] PrefixCodes;
    internal byte[] SuffixBytes;

    internal GifLzwEncoderDictionary()
    {
        MinimumCodeSize = 0;
        RootCodeCount = 0;
        ClearCode = -1;
        EndCode = -1;
        NextCode = 0;
        PrefixCodes = new int[GifConstants.MaximumLzwTableSize];
        SuffixBytes = new byte[GifConstants.MaximumLzwTableSize];
        ClearStorage();
    }

    internal GifLzwDictionaryResult Initialize(int minimumCodeSize)
    {
        GifLzwDictionaryResult result = ValidateMinimumCodeSize(minimumCodeSize);
        if (!result.Success)
        {
            return result;
        }

        ClearStorage();
        MinimumCodeSize = minimumCodeSize;
        RootCodeCount = 1 << minimumCodeSize;
        ClearCode = RootCodeCount;
        EndCode = ClearCode + 1;
        NextCode = EndCode + 1;

        int code = 0;
        while (code < RootCodeCount)
        {
            PrefixCodes[code] = -1;
            SuffixBytes[code] = (byte)code;
            code = code + 1;
        }

        PrefixCodes[ClearCode] = -1;
        PrefixCodes[EndCode] = -1;
        result.Code = NextCode;
        return result;
    }

    internal GifLzwDictionaryResult Add(int prefixCode, byte suffixByte)
    {
        GifLzwDictionaryResult result = new GifLzwDictionaryResult();
        int existingCode = Find(prefixCode, suffixByte);
        if (existingCode >= 0)
        {
            result.Success = true;
            result.Code = existingCode;
            return result;
        }

        if (!ContainsDataCode(prefixCode))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW encoder dictionary prefix code is invalid.", new GifDiagnosticPosition(-1, -1, "LzwEncoderDictionary"));
            return result;
        }

        if (NextCode >= GifConstants.MaximumLzwTableSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "LZW encoder dictionary is full.", new GifDiagnosticPosition(-1, -1, "LzwEncoderDictionary"));
            return result;
        }

        int code = NextCode;
        PrefixCodes[code] = prefixCode;
        SuffixBytes[code] = suffixByte;
        NextCode = NextCode + 1;
        result.Success = true;
        result.Code = code;
        return result;
    }

    internal int FindRoot(byte value)
    {
        int code = value;
        if (code < RootCodeCount)
        {
            return code;
        }

        return -1;
    }

    internal int Find(int prefixCode, byte suffixByte)
    {
        int code = EndCode + 1;
        while (code < NextCode)
        {
            if (PrefixCodes[code] == prefixCode)
            {
                if (SuffixBytes[code] == suffixByte)
                {
                    return code;
                }
            }

            code = code + 1;
        }

        return -1;
    }

    internal bool Contains(int prefixCode, byte suffixByte)
    {
        return Find(prefixCode, suffixByte) >= 0;
    }

    internal bool ContainsCode(int code)
    {
        if (code < 0)
        {
            return false;
        }

        if (code >= GifConstants.MaximumLzwTableSize)
        {
            return false;
        }

        return code < NextCode;
    }

    internal bool ContainsDataCode(int code)
    {
        if (!ContainsCode(code))
        {
            return false;
        }

        if (code == ClearCode)
        {
            return false;
        }

        if (code == EndCode)
        {
            return false;
        }

        return code < RootCodeCount || PrefixCodes[code] >= 0;
    }

    internal bool IsFull()
    {
        return NextCode >= GifConstants.MaximumLzwTableSize;
    }

    internal GifLzwDictionaryResult ValidateMinimumCodeSize(int minimumCodeSize)
    {
        GifLzwDictionaryResult result = new GifLzwDictionaryResult();
        if (minimumCodeSize < GifConstants.MinimumLzwMinimumCodeSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwMinimumCodeSize, "LZW minimum code size is too small.", new GifDiagnosticPosition(-1, -1, "LzwEncoderDictionary"));
            return result;
        }

        if (minimumCodeSize > GifConstants.MaximumLzwMinimumCodeSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwMinimumCodeSize, "LZW minimum code size is too large.", new GifDiagnosticPosition(-1, -1, "LzwEncoderDictionary"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal void ClearStorage()
    {
        int index = 0;
        while (index < GifConstants.MaximumLzwTableSize)
        {
            PrefixCodes[index] = -1;
            SuffixBytes[index] = 0;
            index = index + 1;
        }
    }
}

internal class GifLzwEncoderDictionaryWriteResult
{
    internal bool Success;
    internal int Code;
    internal bool AddedEntry;
    internal bool WroteClearCode;
    internal bool WroteEarlyClearCode;
    internal int CurrentCodeSize;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifLzwEncoderDictionaryWriteResult()
    {
        Success = false;
        Code = -1;
        AddedEntry = false;
        WroteClearCode = false;
        WroteEarlyClearCode = false;
        CurrentCodeSize = 0;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLzwEncoder
{
    internal GifLzwEncoderDictionary Dictionary;
    internal GifLzwBitWriter Writer;
    internal int MinimumCodeSize;
    internal int CurrentCodeSize;
    internal bool HasWrittenClearCode;
    internal bool HasWrittenEndCode;
    internal GifLzwClearPolicy ClearPolicy;

    internal GifLzwEncoder()
    {
        Dictionary = new GifLzwEncoderDictionary();
        Writer = new GifLzwBitWriter(16);
        MinimumCodeSize = 0;
        CurrentCodeSize = 0;
        HasWrittenClearCode = false;
        HasWrittenEndCode = false;
        ClearPolicy = GifLzwClearPolicy.ClearOnFullDictionary;
    }

    internal GifLzwCodeWriteResult Initialize(int minimumCodeSize)
    {
        GifLzwCodeWriteResult result = new GifLzwCodeWriteResult();
        GifLzwDictionaryResult dictionaryResult = Dictionary.Initialize(minimumCodeSize);
        if (!dictionaryResult.Success)
        {
            result.Error = dictionaryResult.Error;
            return result;
        }

        Writer.Clear();
        MinimumCodeSize = minimumCodeSize;
        CurrentCodeSize = minimumCodeSize + 1;
        HasWrittenClearCode = false;
        HasWrittenEndCode = false;
        result.Success = true;
        result.CodeSize = CurrentCodeSize;
        return result;
    }

    internal GifLzwCodeWriteResult WriteClearCode()
    {
        GifLzwCodeWriteResult result = ValidateInitialized("LZW encoder must be initialized before writing a clear code.");
        if (!result.Success)
        {
            return result;
        }

        if (HasWrittenEndCode)
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW clear code cannot be written after the end code.", new GifDiagnosticPosition(-1, -1, "LzwEncoder"));
            return result;
        }

        result = Writer.WriteCode(Dictionary.ClearCode, CurrentCodeSize);
        if (!result.Success)
        {
            return result;
        }

        Dictionary.Initialize(MinimumCodeSize);
        CurrentCodeSize = MinimumCodeSize + 1;
        HasWrittenClearCode = true;
        HasWrittenEndCode = false;
        return result;
    }

    internal GifLzwCodeWriteResult WriteEndCode()
    {
        GifLzwCodeWriteResult result = ValidateInitialized("LZW encoder must be initialized before writing an end code.");
        if (!result.Success)
        {
            return result;
        }

        if (!HasWrittenClearCode)
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW end code cannot be written before a clear code.", new GifDiagnosticPosition(-1, -1, "LzwEncoder"));
            return result;
        }

        if (HasWrittenEndCode)
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW end code has already been written.", new GifDiagnosticPosition(-1, -1, "LzwEncoder"));
            return result;
        }

        result = Writer.WriteCode(Dictionary.EndCode, CurrentCodeSize);
        if (!result.Success)
        {
            return result;
        }

        HasWrittenEndCode = true;
        return result;
    }

    internal GifLzwCodeWriteResult ValidateInitialized(string message)
    {
        GifLzwCodeWriteResult result = new GifLzwCodeWriteResult();
        if (MinimumCodeSize == 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, message, new GifDiagnosticPosition(-1, -1, "LzwEncoder"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal byte[] ToArray()
    {
        return Writer.ToArray();
    }

    internal void Configure(GifEncodeOptions options)
    {
        if (options == null)
        {
            ClearPolicy = GifLzwClearPolicy.ClearOnFullDictionary;
            return;
        }

        ClearPolicy = options.LzwClearPolicy;
    }

    internal GifLzwEncoderDictionaryWriteResult AddDictionaryEntry(int prefixCode, byte suffixByte)
    {
        GifLzwEncoderDictionaryWriteResult result = ValidateCanAddDictionaryEntry();
        if (!result.Success)
        {
            return result;
        }

        int existingCode = Dictionary.Find(prefixCode, suffixByte);
        if (existingCode >= 0)
        {
            result.Success = true;
            result.Code = existingCode;
            result.CurrentCodeSize = CurrentCodeSize;
            result.BytesWritten = Writer.ByteCount();
            return result;
        }

        if (ShouldEarlyClearBeforeAddingEntry())
        {
            return ResetDictionaryAfterEarlyClear();
        }

        if (Dictionary.IsFull())
        {
            return ResetDictionaryAfterFullTable();
        }

        GifLzwDictionaryResult add = Dictionary.Add(prefixCode, suffixByte);
        if (!add.Success)
        {
            result.Success = false;
            result.Error = add.Error;
            return result;
        }

        GrowCodeSizeIfNeeded();
        result.Success = true;
        result.Code = add.Code;
        result.AddedEntry = true;
        result.CurrentCodeSize = CurrentCodeSize;
        result.BytesWritten = Writer.ByteCount();
        return result;
    }

    internal bool ShouldEarlyClearBeforeAddingEntry()
    {
        if (ClearPolicy != GifLzwClearPolicy.EarlyClear)
        {
            return false;
        }

        int limit = CurrentCodeSizeLimit();
        if (limit <= 0)
        {
            return false;
        }

        return Dictionary.NextCode >= limit - 1;
    }

    internal GifLzwEncoderDictionaryWriteResult ResetDictionaryAfterEarlyClear()
    {
        GifLzwEncoderDictionaryWriteResult result = ResetDictionaryAfterFullTable();
        if (result.Success)
        {
            result.WroteEarlyClearCode = true;
        }

        return result;
    }

    internal GifLzwEncoderDictionaryWriteResult ValidateCanAddDictionaryEntry()
    {
        GifLzwEncoderDictionaryWriteResult result = new GifLzwEncoderDictionaryWriteResult();
        if (MinimumCodeSize == 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW encoder must be initialized before adding dictionary entries.", new GifDiagnosticPosition(-1, -1, "LzwEncoder"));
            return result;
        }

        if (!HasWrittenClearCode)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW encoder dictionary entries require an initial clear code.", new GifDiagnosticPosition(-1, -1, "LzwEncoder"));
            return result;
        }

        if (HasWrittenEndCode)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW encoder dictionary entries cannot be added after the end code.", new GifDiagnosticPosition(-1, -1, "LzwEncoder"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal GifLzwEncoderDictionaryWriteResult ResetDictionaryAfterFullTable()
    {
        GifLzwEncoderDictionaryWriteResult result = new GifLzwEncoderDictionaryWriteResult();
        GifLzwCodeWriteResult clear = WriteClearCode();
        if (!clear.Success)
        {
            result.Error = clear.Error;
            return result;
        }

        result.Success = true;
        result.WroteClearCode = true;
        result.CurrentCodeSize = CurrentCodeSize;
        result.BytesWritten = clear.BytesWritten;
        return result;
    }

    internal void GrowCodeSizeIfNeeded()
    {
        while (CanGrowCodeSize())
        {
            if (Dictionary.NextCode < CurrentCodeSizeLimit())
            {
                return;
            }

            CurrentCodeSize = CurrentCodeSize + 1;
        }
    }

    internal bool CanGrowCodeSize()
    {
        if (CurrentCodeSize <= 0)
        {
            return false;
        }

        return CurrentCodeSize < GifConstants.MaximumLzwCodeSize;
    }

    internal int CurrentCodeSizeLimit()
    {
        if (CurrentCodeSize <= 0)
        {
            return 0;
        }

        if (CurrentCodeSize >= GifConstants.MaximumLzwCodeSize)
        {
            return GifConstants.MaximumLzwTableSize;
        }

        return 1 << CurrentCodeSize;
    }
}
