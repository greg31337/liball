namespace qrcode;

internal class DecodedSymbolBuilder
{
    internal static QrDecodeResult Create(MatrixCodewordResult words, QrDecodeOptions options)
    {
        DecodeStatus status = new DecodeStatus();
        QrDecodeResult? result = TryCreate(words, options, status);
        if (result == null) { throw status.Error ?? new QrDecodeException("The payload is invalid.", status.FailureReason, -1, 0); }
        return result;
    }

    internal static QrDecodeResult? TryCreate(MatrixCodewordResult words, QrDecodeOptions options, DecodeStatus status)
    {
        BlockCorrectionResult? correction = words.Correction;
        if (!words.Success || correction == null)
        {
            status.Fail(words.FailureReason, "Verified codewords are required to build a decoded symbol.");
            return null;
        }

        PayloadParseResult? parsed = PayloadParser.TryParse(words.DataCodewords, words.DataBitCount, words.SymbolKind, words.Version, options, status);
        if (parsed == null) { return null; }
        QrDecodeResult? result = DecodedPayloadBuilder.TryCreate(parsed, options, status);
        if (result == null) { return null; }
        QrSymbolKind kind = words.SymbolKind;
        int version = words.Version;
        int width = kind == QrSymbolKind.Micro ? MicroVersionTable.GetDimension(version)
            : kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetWidth(version)
            : 17 + version * 4;
        int height = kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetHeight(version) : width;
        int eccCount = correction.Codewords.Length - correction.DataCodewords.Length;
        if (kind == QrSymbolKind.LegacyModel1) { eccCount -= words.RemainderBitCount / 8; }
        result.CorrectedErrorCount = correction.CorrectedErrorCount;
        result.CorrectedErasureCount = correction.CorrectedErasureCount;
        result.ErrorCorrectionBlocks = correction.BlockResults;
        result.Symbol = new QrSymbol
        {
            SymbolKind = kind, Version = kind == QrSymbolKind.rMQR ? 0 : version, Width = width, Height = height,
            ErrorCorrectionLevel = words.ErrorCorrectionLevel, MaskPattern = words.MaskPattern,
            Codewords = correction.Codewords, Segments = result.Segments,
            DataCodewordCount = correction.DataCodewords.Length, ErrorCorrectionCodewordCount = eccCount,
            TotalCodewordCount = correction.Codewords.Length, RemainderBitCount = words.RemainderBitCount,
            DataBitCapacity = words.DataBitCount, UsedDataBitCount = parsed.PayloadBitCount,
            UnusedDataBitCount = words.DataBitCount - parsed.PayloadBitCount, UsesHanziCompatibility = result.UsesHanziCompatibility
        };
        return result;
    }
}
