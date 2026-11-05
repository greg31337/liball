namespace gif;

internal class GifLzwCodeReadResult
{
    internal bool Success;
    internal int Code;
    internal int CodeSize;
    internal bool EndOfData;
    internal GifErrorDiagnostic Error;

    internal GifLzwCodeReadResult()
    {
        Success = false;
        Code = 0;
        CodeSize = 0;
        EndOfData = false;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLzwBitReader
{
    internal byte[] Data;
    internal int ByteOffset;
    internal int BitOffset;
    internal long TotalBitsRead;

    internal GifLzwBitReader(byte[] data)
    {
        if (data == null)
        {
            Data = new byte[0];
        }
        else
        {
            Data = data;
        }

        ByteOffset = 0;
        BitOffset = 0;
        TotalBitsRead = 0;
    }

    internal GifLzwCodeReadResult ReadCode(int codeSize)
    {
        GifLzwCodeReadResult result = ValidateCodeSize(codeSize);
        if (!result.Success)
        {
            return result;
        }

        if (!CanReadBits(codeSize))
        {
            result.Success = false;
            result.EndOfData = true;
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code stream ended before a complete code was available.", new GifDiagnosticPosition(ByteOffset, -1, "LzwBitReader"));
            return result;
        }

        int code = 0;
        int bitIndex = 0;
        while (bitIndex < codeSize)
        {
            int bit = ReadBit();
            code = code | (bit << bitIndex);
            bitIndex = bitIndex + 1;
        }

        result.Code = code;
        result.CodeSize = codeSize;
        result.Success = true;
        return result;
    }

    internal GifLzwCodeReadResult ValidateCodeSize(int codeSize)
    {
        GifLzwCodeReadResult result = new GifLzwCodeReadResult();
        result.CodeSize = codeSize;
        if (codeSize <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code size must be positive.", new GifDiagnosticPosition(ByteOffset, -1, "LzwBitReader"));
            return result;
        }

        if (codeSize > GifConstants.MaximumLzwCodeSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code size exceeds the GIF maximum.", new GifDiagnosticPosition(ByteOffset, -1, "LzwBitReader"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal bool CanReadBits(int bitCount)
    {
        if (bitCount < 0)
        {
            return false;
        }

        return bitCount <= RemainingBits();
    }

    internal int RemainingBits()
    {
        long totalBits = (long)Data.Length * 8;
        long consumedBits = ((long)ByteOffset * 8) + BitOffset;
        long remaining = totalBits - consumedBits;
        if (remaining < 0)
        {
            return 0;
        }

        if (remaining > 2147483647)
        {
            return 2147483647;
        }

        return (int)remaining;
    }

    internal int ReadBit()
    {
        if (!CanReadBits(1))
        {
            return -1;
        }

        int value = Data[ByteOffset];
        int bit = (value >> BitOffset) & 1;
        BitOffset = BitOffset + 1;
        TotalBitsRead = TotalBitsRead + 1;
        if (BitOffset == 8)
        {
            BitOffset = 0;
            ByteOffset = ByteOffset + 1;
        }

        return bit;
    }
}

internal class GifLzwDictionaryResult
{
    internal bool Success;
    internal int Code;
    internal GifErrorDiagnostic Error;

    internal GifLzwDictionaryResult()
    {
        Success = false;
        Code = -1;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLzwDictionary
{
    internal int MinimumCodeSize;
    internal int RootCodeCount;
    internal int ClearCode;
    internal int EndCode;
    internal int NextCode;
    internal int[] PrefixCodes;
    internal byte[] SuffixBytes;
    internal byte[] FirstBytes;
    internal int[] Lengths;

    internal GifLzwDictionary()
    {
        MinimumCodeSize = 0;
        RootCodeCount = 0;
        ClearCode = -1;
        EndCode = -1;
        NextCode = 0;
        PrefixCodes = new int[GifConstants.MaximumLzwTableSize];
        SuffixBytes = new byte[GifConstants.MaximumLzwTableSize];
        FirstBytes = new byte[GifConstants.MaximumLzwTableSize];
        Lengths = new int[GifConstants.MaximumLzwTableSize];
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
            FirstBytes[code] = (byte)code;
            Lengths[code] = 1;
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
        if (!ContainsDataCode(prefixCode))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW dictionary prefix code is invalid.", new GifDiagnosticPosition(-1, -1, "LzwDictionary"));
            return result;
        }

        if (NextCode >= GifConstants.MaximumLzwTableSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "LZW dictionary is full.", new GifDiagnosticPosition(-1, -1, "LzwDictionary"));
            return result;
        }

        int code = NextCode;
        PrefixCodes[code] = prefixCode;
        SuffixBytes[code] = suffixByte;
        FirstBytes[code] = FirstBytes[prefixCode];
        Lengths[code] = Lengths[prefixCode] + 1;
        NextCode = NextCode + 1;
        result.Success = true;
        result.Code = code;
        return result;
    }

    internal GifLzwDictionaryResult ValidateMinimumCodeSize(int minimumCodeSize)
    {
        GifLzwDictionaryResult result = new GifLzwDictionaryResult();
        if (minimumCodeSize < GifConstants.MinimumLzwMinimumCodeSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwMinimumCodeSize, "LZW minimum code size is too small.", new GifDiagnosticPosition(-1, -1, "LzwDictionary"));
            return result;
        }

        if (minimumCodeSize > GifConstants.MaximumLzwMinimumCodeSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwMinimumCodeSize, "LZW minimum code size is too large.", new GifDiagnosticPosition(-1, -1, "LzwDictionary"));
            return result;
        }

        result.Success = true;
        return result;
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

        return Lengths[code] > 0;
    }

    internal bool IsFull()
    {
        return NextCode >= GifConstants.MaximumLzwTableSize;
    }

    internal bool IsClearCode(int code)
    {
        return code == ClearCode;
    }

    internal bool IsEndCode(int code)
    {
        return code == EndCode;
    }

    internal bool IsControlCode(int code)
    {
        if (IsClearCode(code))
        {
            return true;
        }

        return IsEndCode(code);
    }

    internal int GetPrefixCode(int code)
    {
        if (!ContainsCode(code))
        {
            return -1;
        }

        return PrefixCodes[code];
    }

    internal byte GetSuffixByte(int code)
    {
        if (!ContainsDataCode(code))
        {
            return 0;
        }

        return SuffixBytes[code];
    }

    internal byte GetFirstByte(int code)
    {
        if (!ContainsDataCode(code))
        {
            return 0;
        }

        return FirstBytes[code];
    }

    internal int GetLength(int code)
    {
        if (!ContainsDataCode(code))
        {
            return 0;
        }

        return Lengths[code];
    }

    internal void ClearStorage()
    {
        int index = 0;
        while (index < GifConstants.MaximumLzwTableSize)
        {
            PrefixCodes[index] = -1;
            SuffixBytes[index] = 0;
            FirstBytes[index] = 0;
            Lengths[index] = 0;
            index = index + 1;
        }
    }
}

internal class GifLzwDecodeCodeResult
{
    internal bool Success;
    internal int Code;
    internal bool IsClearCode;
    internal bool IsEndCode;
    internal bool IsDataCode;
    internal bool IsFirstDataCode;
    internal bool IsSpecialCurrentCode;
    internal bool IsIgnoredCode;
    internal int PrefixCode;
    internal byte SuffixByte;
    internal byte FirstByte;
    internal int Length;
    internal GifWarningDiagnostic Warning;
    internal GifErrorDiagnostic Error;

    internal GifLzwDecodeCodeResult()
    {
        Success = false;
        Code = -1;
        IsClearCode = false;
        IsEndCode = false;
        IsDataCode = false;
        IsFirstDataCode = false;
        IsSpecialCurrentCode = false;
        IsIgnoredCode = false;
        PrefixCode = -1;
        SuffixByte = 0;
        FirstByte = 0;
        Length = 0;
        Warning = new GifWarningDiagnostic(GifWarningCode.None, string.Empty, null);
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLzwOutputCountResult
{
    internal bool Success;
    internal long OutputCount;
    internal GifErrorDiagnostic Error;

    internal GifLzwOutputCountResult()
    {
        Success = false;
        OutputCount = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLzwDecoder
{
    internal GifLzwDictionary Dictionary;
    internal int MinimumCodeSize;
    internal int CurrentCodeSize;
    internal int PreviousCode;
    internal bool HasPreviousCode;
    internal bool HasSeenClearCode;
    internal bool HasSeenEndCode;
    internal long ExpectedOutputCount;
    internal long MaxOutputCount;
    internal long OutputCount;
    internal GifStrictness Strictness;
    internal bool TolerateMalformedStreams;
    internal GifWarningDiagnostic[] Warnings;

    internal GifLzwDecoder()
    {
        Dictionary = new GifLzwDictionary();
        MinimumCodeSize = 0;
        CurrentCodeSize = 0;
        PreviousCode = -1;
        HasPreviousCode = false;
        HasSeenClearCode = false;
        HasSeenEndCode = false;
        ExpectedOutputCount = -1;
        MaxOutputCount = GifConstants.DefaultMaxDecodedPixels;
        OutputCount = 0;
        Strictness = GifStrictness.Normal;
        TolerateMalformedStreams = false;
        Warnings = new GifWarningDiagnostic[0];
    }

    internal GifLzwDecodeCodeResult Initialize(int minimumCodeSize)
    {
        GifLzwDecodeCodeResult result = new GifLzwDecodeCodeResult();
        GifLzwDictionaryResult dictionaryResult = Dictionary.Initialize(minimumCodeSize);
        if (!dictionaryResult.Success)
        {
            result.Error = dictionaryResult.Error;
            return result;
        }

        MinimumCodeSize = minimumCodeSize;
        CurrentCodeSize = minimumCodeSize + 1;
        PreviousCode = -1;
        HasPreviousCode = false;
        HasSeenClearCode = false;
        HasSeenEndCode = false;
        OutputCount = 0;
        result.Success = true;
        return result;
    }

    internal GifLzwDecodeCodeResult HandleCode(int code)
    {
        GifLzwDecodeCodeResult result = new GifLzwDecodeCodeResult();
        result.Code = code;
        if (HasSeenEndCode)
        {
            if (IsTolerantMode())
            {
                result.Success = true;
                result.IsIgnoredCode = true;
                result.Warning = AddToleratedWarning("LZW code after end code was ignored.");
                return result;
            }

            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code appears after the end code.", new GifDiagnosticPosition(-1, -1, "LzwDecoder"));
            return result;
        }

        if (Dictionary.IsClearCode(code))
        {
            return HandleClearCode(code);
        }

        if (Dictionary.IsEndCode(code))
        {
            return HandleEndCode(code);
        }

        if (!HasSeenClearCode)
        {
            if (IsTolerantMode())
            {
                if (Dictionary.ContainsDataCode(code))
                {
                    result.Warning = AddToleratedWarning("LZW data code appeared before an initial clear code.");
                    HasSeenClearCode = true;
                }
                else
                {
                    result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code stream must begin with a clear code.", new GifDiagnosticPosition(-1, -1, "LzwDecoder"));
                    return result;
                }
            }
            else
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code stream must begin with a clear code.", new GifDiagnosticPosition(-1, -1, "LzwDecoder"));
                return result;
            }
        }

        if (IsSpecialCurrentCode(code))
        {
            return HandleSpecialCurrentCode(code);
        }

        if (!Dictionary.ContainsDataCode(code))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW data code is not present in the dictionary.", new GifDiagnosticPosition(-1, -1, "LzwDecoder"));
            return result;
        }

        result.Success = true;
        result.IsDataCode = true;
        result.IsFirstDataCode = !HasPreviousCode;
        result.PrefixCode = Dictionary.GetPrefixCode(code);
        result.SuffixByte = Dictionary.GetSuffixByte(code);
        result.FirstByte = Dictionary.GetFirstByte(code);
        result.Length = Dictionary.GetLength(code);
        PreviousCode = code;
        HasPreviousCode = true;
        return result;
    }

    internal GifLzwDecodeCodeResult HandleClearCode(int code)
    {
        GifLzwDecodeCodeResult result = new GifLzwDecodeCodeResult();
        result.Code = code;
        GifLzwDictionaryResult dictionaryResult = Dictionary.Initialize(MinimumCodeSize);
        if (!dictionaryResult.Success)
        {
            result.Error = dictionaryResult.Error;
            return result;
        }

        CurrentCodeSize = MinimumCodeSize + 1;
        PreviousCode = -1;
        HasPreviousCode = false;
        HasSeenClearCode = true;
        HasSeenEndCode = false;
        result.Success = true;
        result.IsClearCode = true;
        return result;
    }

    internal GifLzwDecodeCodeResult HandleEndCode(int code)
    {
        GifLzwDecodeCodeResult result = new GifLzwDecodeCodeResult();
        result.Code = code;
        if (!HasSeenClearCode)
        {
            if (IsTolerantMode())
            {
                result.Warning = AddToleratedWarning("LZW end code appeared before an initial clear code.");
                HasSeenClearCode = true;
            }
            else
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW end code appeared before a clear code.", new GifDiagnosticPosition(-1, -1, "LzwDecoder"));
                return result;
            }
        }

        HasSeenEndCode = true;
        PreviousCode = -1;
        HasPreviousCode = false;
        result.Success = true;
        result.IsEndCode = true;
        return result;
    }

    internal GifLzwDecodeCodeResult ValidateFinalCodeStream()
    {
        GifLzwDecodeCodeResult result = new GifLzwDecodeCodeResult();
        if (HasSeenEndCode)
        {
            result.Success = true;
            result.IsEndCode = true;
            return result;
        }

        if (IsTolerantMode())
        {
            result.Success = true;
            result.Warning = AddToleratedWarning("LZW code stream ended without an end code.");
            return result;
        }

        result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW code stream ended without an end code.", new GifDiagnosticPosition(-1, -1, "LzwDecoder"));
        return result;
    }

    internal GifLzwDecodeCodeResult HandleSpecialCurrentCode(int code)
    {
        GifLzwDecodeCodeResult result = new GifLzwDecodeCodeResult();
        result.Code = code;
        if (!IsSpecialCurrentCode(code))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW special current code is invalid.", new GifDiagnosticPosition(-1, -1, "LzwDecoder"));
            return result;
        }

        byte firstByte = Dictionary.GetFirstByte(PreviousCode);
        result.Success = true;
        result.IsDataCode = true;
        result.IsSpecialCurrentCode = true;
        result.PrefixCode = PreviousCode;
        result.SuffixByte = firstByte;
        result.FirstByte = firstByte;
        result.Length = Dictionary.GetLength(PreviousCode) + 1;
        PreviousCode = code;
        HasPreviousCode = true;
        return result;
    }

    internal bool IsSpecialCurrentCode(int code)
    {
        if (!HasPreviousCode)
        {
            return false;
        }

        if (!Dictionary.ContainsDataCode(PreviousCode))
        {
            return false;
        }

        return code == Dictionary.NextCode;
    }

    internal void Configure(GifDecodeOptions options)
    {
        if (options == null)
        {
            Strictness = GifStrictness.Normal;
            TolerateMalformedStreams = false;
            return;
        }

        Strictness = options.Strictness;
        TolerateMalformedStreams = options.TolerateMalformedStreams;
    }

    internal bool IsTolerantMode()
    {
        if (TolerateMalformedStreams)
        {
            return true;
        }

        return Strictness == GifStrictness.Tolerant;
    }

    internal GifWarningDiagnostic AddToleratedWarning(string message)
    {
        GifWarningDiagnostic warning = new GifWarningDiagnostic(GifWarningCode.ToleratedMalformedBlock, message, new GifDiagnosticPosition(-1, -1, "LzwDecoder"));
        Warnings = AppendWarning(Warnings, warning);
        return warning;
    }

    internal GifWarningDiagnostic[] AppendWarning(GifWarningDiagnostic[] items, GifWarningDiagnostic item)
    {
        if (items == null)
        {
            items = new GifWarningDiagnostic[0];
        }

        GifWarningDiagnostic[] result = new GifWarningDiagnostic[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }

    internal GifLzwDictionaryResult AddDictionaryEntry(int prefixCode, byte suffixByte)
    {
        GifLzwDictionaryResult result = Dictionary.Add(prefixCode, suffixByte);
        if (!result.Success)
        {
            return result;
        }

        GrowCodeSizeIfNeeded();
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

    internal GifLzwOutputCountResult ConfigureOutputValidation(long expectedOutputCount, long maxOutputCount)
    {
        GifLzwOutputCountResult result = new GifLzwOutputCountResult();
        if (expectedOutputCount < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "LZW expected output count is invalid.", new GifDiagnosticPosition(-1, -1, "LzwOutput"));
            return result;
        }

        if (maxOutputCount < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelLimitExceeded, "LZW maximum output count is invalid.", new GifDiagnosticPosition(-1, -1, "LzwOutput"));
            return result;
        }

        ExpectedOutputCount = expectedOutputCount;
        MaxOutputCount = maxOutputCount;
        OutputCount = 0;
        result.Success = true;
        result.OutputCount = OutputCount;
        return result;
    }

    internal GifLzwOutputCountResult RecordOutputCount(long count)
    {
        GifLzwOutputCountResult result = new GifLzwOutputCountResult();
        if (count < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "LZW output count increment is invalid.", new GifDiagnosticPosition(-1, -1, "LzwOutput"));
            return result;
        }

        if (OutputCount > long.MaxValue - count)
        {
            result.OutputCount = OutputCount;
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelLimitExceeded, "LZW decoded output exceeds the configured limit.", new GifDiagnosticPosition(-1, -1, "LzwOutput"));
            return result;
        }

        long nextCount = OutputCount + count;
        if (nextCount > MaxOutputCount)
        {
            result.OutputCount = OutputCount;
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelLimitExceeded, "LZW decoded output exceeds the configured limit.", new GifDiagnosticPosition(-1, -1, "LzwOutput"));
            return result;
        }

        if (ExpectedOutputCount >= 0)
        {
            if (nextCount > ExpectedOutputCount)
            {
                result.OutputCount = OutputCount;
                result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "LZW decoded output exceeds the expected pixel count.", new GifDiagnosticPosition(-1, -1, "LzwOutput"));
                return result;
            }
        }

        OutputCount = nextCount;
        result.Success = true;
        result.OutputCount = OutputCount;
        return result;
    }

    internal GifLzwOutputCountResult ValidateFinalOutputCount()
    {
        GifLzwOutputCountResult result = new GifLzwOutputCountResult();
        result.OutputCount = OutputCount;
        if (ExpectedOutputCount >= 0)
        {
            if (OutputCount != ExpectedOutputCount)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "LZW decoded output count does not match the expected pixel count.", new GifDiagnosticPosition(-1, -1, "LzwOutput"));
                return result;
            }
        }

        result.Success = true;
        return result;
    }
}
