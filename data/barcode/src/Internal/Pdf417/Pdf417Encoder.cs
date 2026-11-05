namespace barcode;

internal class Pdf417EncodingResult
{
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal Pdf417CompactionPlan CompactionPlan = new Pdf417CompactionPlan();
    internal Pdf417SymbolPlan SymbolPlan = new Pdf417SymbolPlan();
    internal int[] MacroCodewords = new int[0];
    internal int[] HighLevelCodewords = new int[0];
    internal int[] DataCodewords = new int[0];
    internal int[] ErrorCorrectionCodewords = new int[0];
    internal int[] SymbolCodewords = new int[0];
}

internal static class Pdf417Encoder
{
    internal static Pdf417EncodingResult Encode(
        byte[] data,
        BarcodePayload payload,
        Pdf417Options? pdf417Options,
        BarcodeEncodeOptions? encodeOptions)
    {
        if (data == null)
        {
            throw new BarcodeEncodingException("PDF417 encoder data must not be null.");
        }

        if (payload == null)
        {
            throw new BarcodeEncodingException("PDF417 encoder payload must not be null.");
        }

        BarcodeEncodeOptions common = encodeOptions == null
            ? new BarcodeEncodeOptions()
            : encodeOptions;
        Pdf417Options settings = ResolveOptions(pdf417Options, common);
        int eciAssignmentNumber = settings.EciAssignmentNumber == 0
            ? Pdf417CompactionPlanner.NoEciAssignmentNumber
            : settings.EciAssignmentNumber;
        Pdf417CompactionPlan compactionPlan = Pdf417CompactionPlanner.Plan(
            data,
            settings.CompactionMode,
            eciAssignmentNumber);
        int[] macroCodewords = settings.Macro == null
            ? new int[0]
            : Pdf417MacroCodec.EncodeControlBlock(settings);
        int[] highLevelCodewords = CombineCodewords(
            compactionPlan.Codewords,
            macroCodewords);
        Pdf417SymbolPlan symbolPlan = Pdf417SymbolPlanner.CreateSymbolPlan(
            highLevelCodewords,
            settings,
            common.Sizing);
        int[] symbolCodewords = Pdf417ReedSolomon.EncodeCodewords(
            symbolPlan.DataCodewords,
            symbolPlan.ErrorCorrectionLevel);
        ValidateAssembledCodewords(symbolPlan, symbolCodewords);
        int[] errorCorrectionCodewords = CopyIntegerRange(
            symbolCodewords,
            symbolPlan.DataCodewords.Length,
            symbolPlan.ErrorCorrectionCodewordCount);
        BarcodeSymbol symbol = Pdf417Layout.CreateSymbol(
            symbolPlan,
            symbolCodewords,
            data,
            payload,
            common);
        ApplyPayloadMetadata(symbol, settings, data.Length);

        Pdf417EncodingResult result = new Pdf417EncodingResult();
        result.Symbol = symbol;
        result.CompactionPlan = compactionPlan;
        result.SymbolPlan = symbolPlan;
        result.MacroCodewords = CopyIntegers(macroCodewords);
        result.HighLevelCodewords = CopyIntegers(highLevelCodewords);
        result.DataCodewords = CopyIntegers(symbolPlan.DataCodewords);
        result.ErrorCorrectionCodewords = errorCorrectionCodewords;
        result.SymbolCodewords = CopyIntegers(symbolCodewords);
        return result;
    }

    static Pdf417Options ResolveOptions(
        Pdf417Options? options,
        BarcodeEncodeOptions common)
    {
        Pdf417Options settings = new Pdf417Options();
        settings.ErrorCorrectionLevel = -1;
        if (options != null)
        {
            settings.Columns = options.Columns;
            settings.Rows = options.Rows;
            settings.Variant = options.Variant;
            settings.CompactionMode = options.CompactionMode;
            settings.ErrorCorrectionLevel = options.ErrorCorrectionLevel;
            settings.EciAssignmentNumber = options.EciAssignmentNumber;
            settings.Macro = ResultCopier.CopyStructuredAppendInfo(options.Macro);
            settings.FileName = options.FileName;
            settings.Timestamp = options.Timestamp;
            settings.Sender = options.Sender;
            settings.Addressee = options.Addressee;
            settings.FileSize = options.FileSize;
            settings.FileChecksum = options.FileChecksum;
        }

        if (settings.EciAssignmentNumber == 0)
        {
            settings.EciAssignmentNumber = common.EciAssignmentNumber;
        }

        if (settings.Macro == null)
        {
            settings.Macro = ResultCopier.CopyStructuredAppendInfo(
                common.StructuredAppend);
        }

        if (settings.Macro == null
            && (settings.FileName != null
                || settings.Timestamp != 0
                || settings.Sender != null
                || settings.Addressee != null
                || settings.FileSize != 0
                || settings.FileChecksum != 0))
        {
            throw new BarcodeEncodingException("Macro PDF417 optional fields require segment metadata.");
        }

        if (settings.EciAssignmentNumber < 0)
        {
            throw new BarcodeEncodingException("PDF417 ECI assignment number must be zero or from one through 811799.");
        }

        return settings;
    }

    static void ApplyPayloadMetadata(
        BarcodeSymbol symbol,
        Pdf417Options options,
        int dataLength)
    {
        BarcodePayload? payload = symbol.Payload;
        if (payload == null)
        {
            throw new BarcodeValidationException("PDF417 layout did not create payload metadata.");
        }

        if (options.EciAssignmentNumber > 0)
        {
            EciSegment segment = new EciSegment();
            segment.AssignmentNumber = options.EciAssignmentNumber;
            segment.ByteOffset = 0;
            segment.ByteCount = dataLength;
            payload.EciSegments = new EciSegment[] { segment };
        }

        if (options.Macro != null)
        {
            StructuredAppendInfo? macro = ResultCopier.CopyStructuredAppendInfo(
                options.Macro);
            if (macro == null)
            {
                throw new BarcodeValidationException("Macro PDF417 metadata copying failed.");
            }

            macro.Format = BarcodeFormat.MacroPdf417;
            payload.StructuredAppend = macro;
            symbol.Format = BarcodeFormat.MacroPdf417;
        }
    }

    static void ValidateAssembledCodewords(
        Pdf417SymbolPlan plan,
        int[] symbolCodewords)
    {
        if (symbolCodewords.Length != plan.TotalCodewordCount
            || symbolCodewords.Length
                != plan.DataCodewords.Length + plan.ErrorCorrectionCodewordCount)
        {
            throw new BarcodeValidationException("PDF417 assembled data and error-correction counts do not match the symbol plan.");
        }

        if (!Pdf417ReedSolomon.VerifyCodewords(
            symbolCodewords,
            plan.ErrorCorrectionLevel))
        {
            throw new BarcodeValidationException("PDF417 assembled error-correction codewords do not satisfy the modulo-929 check equations.");
        }
    }

    static int[] CombineCodewords(int[] first, int[] second)
    {
        int[] combined = new int[CheckedMath.Add(first.Length, second.Length)];
        int index = 0;
        while (index < first.Length)
        {
            combined[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;
        while (secondIndex < second.Length)
        {
            combined[first.Length + secondIndex] = second[secondIndex];
            secondIndex = secondIndex + 1;
        }

        return combined;
    }

    static int[] CopyIntegerRange(int[] source, int start, int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[start + index];
            index = index + 1;
        }

        return copy;
    }

    static int[] CopyIntegers(int[] source)
    {
        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }
}
