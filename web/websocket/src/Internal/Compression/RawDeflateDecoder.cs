using System;

namespace websocket;

internal class RawDeflateDecodeResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal int ProducedCount;
    internal int TotalOutputCount;
    internal bool StreamComplete;
    internal bool SyncFlushComplete;
    internal string ErrorIdentifier = "";
}

internal class RawDeflateDecoder
{
    const int ReadBlockHeaderStage = 0;
    const int ReadStoredHeaderStage = 1;
    const int CopyStoredBytesStage = 2;
    const int ReadDynamicTablesStage = 3;
    const int ReadLiteralLengthStage = 4;
    const int ReadLengthExtraStage = 5;
    const int ReadDistanceSymbolStage = 6;
    const int ReadDistanceExtraStage = 7;
    const int WriteLiteralStage = 8;
    const int WriteMatchStage = 9;

    static readonly int[] LengthBases =
    [
        3, 4, 5, 6, 7, 8, 9, 10,
        11, 13, 15, 17, 19, 23, 27, 31,
        35, 43, 51, 59, 67, 83, 99, 115,
        131, 163, 195, 227, 258
    ];

    static readonly int[] LengthExtraBits =
    [
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 1, 1, 1, 2, 2, 2, 2,
        3, 3, 3, 3, 4, 4, 4, 4,
        5, 5, 5, 5, 0
    ];

    static readonly int[] DistanceBases =
    [
        1, 2, 3, 4, 5, 7, 9, 13,
        17, 25, 33, 49, 65, 97, 129, 193,
        257, 385, 513, 769, 1025, 1537, 2049, 3073,
        4097, 6145, 8193, 12289, 16385, 24577
    ];

    static readonly int[] DistanceExtraBits =
    [
        0, 0, 0, 0, 1, 1, 2, 2,
        3, 3, 4, 4, 5, 5, 6, 6,
        7, 7, 8, 8, 9, 9, 10, 10,
        11, 11, 12, 12, 13, 13
    ];

    DeflateBitReader Reader = new DeflateBitReader();
    DeflateBlockHeaderReader BlockHeaderReader;
    DeflateStoredBlockHeaderReader StoredHeaderReader;
    DeflateSlidingWindow Window;
    int MaximumOutputLength;
    int Stage;
    bool FinalBlock;
    int StoredRemaining;
    DeflateDynamicTableReader? DynamicTableReader;
    DeflateHuffmanDecoder? LiteralLengthDecoder;
    DeflateHuffmanDecoder? DistanceDecoder;
    int PendingLiteral;
    int MatchLengthBase;
    int MatchLengthExtraBits;
    int MatchLength;
    int MatchDistanceBase;
    int MatchDistanceExtraBits;
    int TotalOutputCount;
    bool StreamComplete;
    bool AtSyncFlushBoundary;
    bool Failed;
    WebSocketParseStatus FailureStatus = WebSocketParseStatus.ProtocolFailure;
    string FailureIdentifier = "";
    byte[] LiteralBuffer = new byte[1];

    internal RawDeflateDecoder(int windowBits, int maximumOutputLength)
    {
        if (maximumOutputLength < 0)
        {
            throw new WebSocketStateException();
        }

        MaximumOutputLength = maximumOutputLength;
        Window = new DeflateSlidingWindow(windowBits);
        BlockHeaderReader = new DeflateBlockHeaderReader(Reader);
        StoredHeaderReader = new DeflateStoredBlockHeaderReader(Reader);
    }

    internal RawDeflateDecodeResult DecodeRawDeflate(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        if (Failed)
        {
            return CreateDecodeResult(
                FailureStatus,
                0,
                0,
                false);
        }

        if (StreamComplete)
        {
            return CreateDecodeResult(
                WebSocketParseStatus.Complete,
                0,
                0,
                false);
        }

        if (AtSyncFlushBoundary)
        {
            if (source.Length == 0)
            {
                return CreateDecodeResult(
                    WebSocketParseStatus.Complete,
                    0,
                    0,
                    true);
            }

            AtSyncFlushBoundary = false;
        }

        int sourceIndex = 0;
        int destinationIndex = 0;
        while (true)
        {
            if (Stage == ReadBlockHeaderStage)
            {
                DeflateBlockHeaderResult header =
                    BlockHeaderReader.ReadBlockHeader(
                        source.Slice(sourceIndex));
                sourceIndex = sourceIndex + header.ConsumedCount;
                if (header.Status == WebSocketParseStatus.NeedInput)
                {
                    return CreateDecodeResult(
                        header.Status,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                if (header.Status != WebSocketParseStatus.Complete)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-block-type-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                FinalBlock = header.FinalBlock;
                if (header.BlockType == 0)
                {
                    StoredHeaderReader.ResetStoredBlockHeader();
                    Stage = ReadStoredHeaderStage;
                }
                else if (header.BlockType == 1)
                {
                    SetCompressedTables(
                        DeflateHuffmanTables.CreateFixedLiteralLengthTable(),
                        DeflateHuffmanTables.CreateFixedDistanceTable());
                    Stage = ReadLiteralLengthStage;
                }
                else if (header.BlockType == 2)
                {
                    DynamicTableReader =
                        new DeflateDynamicTableReader(Reader);
                    Stage = ReadDynamicTablesStage;
                }
                else
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-block-type-invalid",
                        sourceIndex,
                        destinationIndex);
                }
            }
            else if (Stage == ReadStoredHeaderStage)
            {
                DeflateStoredBlockHeaderResult stored =
                    StoredHeaderReader.ReadStoredBlockHeader(
                        source.Slice(sourceIndex));
                sourceIndex = sourceIndex + stored.ConsumedCount;
                if (stored.Status == WebSocketParseStatus.NeedInput)
                {
                    return CreateDecodeResult(
                        stored.Status,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                if (stored.Status != WebSocketParseStatus.Complete)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-stored-length-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                StoredRemaining = stored.Length;
                if (StoredRemaining >
                    MaximumOutputLength - TotalOutputCount)
                {
                    return FailDecode(
                        WebSocketParseStatus.LimitRejected,
                        "deflate-output-limit-exceeded",
                        sourceIndex,
                        destinationIndex);
                }

                Stage = CopyStoredBytesStage;
                if (StoredRemaining == 0)
                {
                    RawDeflateDecodeResult blockEnd = FinishBlock(
                        sourceIndex,
                        destinationIndex,
                        true);
                    if (blockEnd.Status == WebSocketParseStatus.Complete)
                    {
                        return blockEnd;
                    }
                }
            }
            else if (Stage == CopyStoredBytesStage)
            {
                if (destinationIndex == destination.Length)
                {
                    return CreateDecodeResult(
                        WebSocketParseStatus.NeedOutputCapacity,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                if (sourceIndex == source.Length)
                {
                    return CreateDecodeResult(
                        WebSocketParseStatus.NeedInput,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                int availableCount = source.Length - sourceIndex;
                if (availableCount > StoredRemaining)
                {
                    availableCount = StoredRemaining;
                }

                DeflateWindowWriteResult write = Window.WriteBytes(
                    source.Slice(sourceIndex, availableCount),
                    destination.Slice(destinationIndex));
                sourceIndex = sourceIndex + write.ConsumedCount;
                destinationIndex = destinationIndex + write.ProducedCount;
                StoredRemaining = StoredRemaining - write.ConsumedCount;
                TotalOutputCount = TotalOutputCount + write.ProducedCount;
                if (StoredRemaining == 0)
                {
                    RawDeflateDecodeResult blockEnd = FinishBlock(
                        sourceIndex,
                        destinationIndex,
                        false);
                    if (blockEnd.Status == WebSocketParseStatus.Complete)
                    {
                        return blockEnd;
                    }
                }
                else if (write.Status ==
                    WebSocketParseStatus.NeedOutputCapacity)
                {
                    return CreateDecodeResult(
                        write.Status,
                        sourceIndex,
                        destinationIndex,
                        false);
                }
            }
            else if (Stage == ReadDynamicTablesStage)
            {
                if (DynamicTableReader == null)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-state-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                DeflateDynamicTableResult tables =
                    DynamicTableReader.ReadDynamicTables(
                        source.Slice(sourceIndex));
                sourceIndex = sourceIndex + tables.ConsumedCount;
                if (tables.Status == WebSocketParseStatus.NeedInput)
                {
                    return CreateDecodeResult(
                        tables.Status,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                if (tables.Status != WebSocketParseStatus.Complete)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-huffman-tree-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                SetCompressedTables(
                    tables.LiteralLengthTable,
                    tables.DistanceTable);
                Stage = ReadLiteralLengthStage;
            }
            else if (Stage == ReadLiteralLengthStage)
            {
                if (LiteralLengthDecoder == null)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-state-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                DeflateHuffmanReadResult symbol =
                    LiteralLengthDecoder.ReadSymbol(
                        Reader,
                        source.Slice(sourceIndex));
                sourceIndex = sourceIndex + symbol.ConsumedCount;
                if (symbol.Status == WebSocketParseStatus.NeedInput)
                {
                    return CreateDecodeResult(
                        symbol.Status,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                if (symbol.Status != WebSocketParseStatus.Complete)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-huffman-code-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                if (symbol.Symbol < 256)
                {
                    if (TotalOutputCount == MaximumOutputLength)
                    {
                        return FailDecode(
                            WebSocketParseStatus.LimitRejected,
                            "deflate-output-limit-exceeded",
                            sourceIndex,
                            destinationIndex);
                    }

                    PendingLiteral = symbol.Symbol;
                    Stage = WriteLiteralStage;
                }
                else if (symbol.Symbol == 256)
                {
                    RawDeflateDecodeResult blockEnd = FinishBlock(
                        sourceIndex,
                        destinationIndex,
                        false);
                    if (blockEnd.Status == WebSocketParseStatus.Complete)
                    {
                        return blockEnd;
                    }
                }
                else if (symbol.Symbol <= 285)
                {
                    int lengthIndex = symbol.Symbol - 257;
                    MatchLengthBase = LengthBases[lengthIndex];
                    MatchLengthExtraBits = LengthExtraBits[lengthIndex];
                    Stage = ReadLengthExtraStage;
                }
                else
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-literal-length-symbol-invalid",
                        sourceIndex,
                        destinationIndex);
                }
            }
            else if (Stage == ReadLengthExtraStage)
            {
                DeflateBitReadResult extra = Reader.ReadBits(
                    source.Slice(sourceIndex),
                    MatchLengthExtraBits);
                sourceIndex = sourceIndex + extra.ConsumedCount;
                if (extra.Status != WebSocketParseStatus.Complete)
                {
                    return CreateDecodeResult(
                        extra.Status,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                MatchLength = MatchLengthBase + extra.Value;
                if (MatchLength > MaximumOutputLength - TotalOutputCount)
                {
                    return FailDecode(
                        WebSocketParseStatus.LimitRejected,
                        "deflate-output-limit-exceeded",
                        sourceIndex,
                        destinationIndex);
                }

                Stage = ReadDistanceSymbolStage;
            }
            else if (Stage == ReadDistanceSymbolStage)
            {
                if (DistanceDecoder == null)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-distance-tree-missing",
                        sourceIndex,
                        destinationIndex);
                }

                DeflateHuffmanReadResult symbol = DistanceDecoder.ReadSymbol(
                    Reader,
                    source.Slice(sourceIndex));
                sourceIndex = sourceIndex + symbol.ConsumedCount;
                if (symbol.Status == WebSocketParseStatus.NeedInput)
                {
                    return CreateDecodeResult(
                        symbol.Status,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                if (symbol.Status != WebSocketParseStatus.Complete)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-huffman-code-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                if (symbol.Symbol < 0 || symbol.Symbol > 29)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-distance-symbol-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                MatchDistanceBase = DistanceBases[symbol.Symbol];
                MatchDistanceExtraBits = DistanceExtraBits[symbol.Symbol];
                Stage = ReadDistanceExtraStage;
            }
            else if (Stage == ReadDistanceExtraStage)
            {
                DeflateBitReadResult extra = Reader.ReadBits(
                    source.Slice(sourceIndex),
                    MatchDistanceExtraBits);
                sourceIndex = sourceIndex + extra.ConsumedCount;
                if (extra.Status != WebSocketParseStatus.Complete)
                {
                    return CreateDecodeResult(
                        extra.Status,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                int distance = MatchDistanceBase + extra.Value;
                WebSocketParseStatus begin = Window.BeginCopy(
                    distance,
                    MatchLength);
                if (begin != WebSocketParseStatus.Complete)
                {
                    return FailDecode(
                        WebSocketParseStatus.ProtocolFailure,
                        "deflate-distance-invalid",
                        sourceIndex,
                        destinationIndex);
                }

                Stage = WriteMatchStage;
            }
            else if (Stage == WriteLiteralStage)
            {
                if (destinationIndex == destination.Length)
                {
                    return CreateDecodeResult(
                        WebSocketParseStatus.NeedOutputCapacity,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                LiteralBuffer[0] = (byte)PendingLiteral;
                DeflateWindowWriteResult write = Window.WriteBytes(
                    LiteralBuffer,
                    destination.Slice(destinationIndex));
                destinationIndex = destinationIndex + write.ProducedCount;
                TotalOutputCount = TotalOutputCount + write.ProducedCount;
                Stage = ReadLiteralLengthStage;
            }
            else if (Stage == WriteMatchStage)
            {
                if (destinationIndex == destination.Length)
                {
                    return CreateDecodeResult(
                        WebSocketParseStatus.NeedOutputCapacity,
                        sourceIndex,
                        destinationIndex,
                        false);
                }

                DeflateWindowWriteResult write = Window.ContinueCopy(
                    destination.Slice(destinationIndex));
                destinationIndex = destinationIndex + write.ProducedCount;
                TotalOutputCount = TotalOutputCount + write.ProducedCount;
                if (write.Status == WebSocketParseStatus.Complete)
                {
                    Stage = ReadLiteralLengthStage;
                }
                else
                {
                    return CreateDecodeResult(
                        write.Status,
                        sourceIndex,
                        destinationIndex,
                        false);
                }
            }
            else
            {
                return FailDecode(
                    WebSocketParseStatus.ProtocolFailure,
                    "deflate-state-invalid",
                    sourceIndex,
                    destinationIndex);
            }
        }
    }

    internal RawDeflateDecodeResult FinishRawDeflateInput()
    {
        if (Failed)
        {
            return CreateDecodeResult(
                FailureStatus,
                0,
                0,
                false);
        }

        if (StreamComplete)
        {
            return CreateDecodeResult(
                WebSocketParseStatus.Complete,
                0,
                0,
                false);
        }

        if (AtSyncFlushBoundary)
        {
            return CreateDecodeResult(
                WebSocketParseStatus.Complete,
                0,
                0,
                true);
        }

        if (Stage == WriteLiteralStage || Stage == WriteMatchStage)
        {
            return CreateDecodeResult(
                WebSocketParseStatus.NeedOutputCapacity,
                0,
                0,
                false);
        }

        return FailDecode(
            WebSocketParseStatus.ProtocolFailure,
            "deflate-input-truncated",
            0,
            0);
    }

    internal void ResetRawDeflateDecoder(bool retainHistory)
    {
        Reader.ResetBitReader();
        StoredHeaderReader.ResetStoredBlockHeader();
        Window.ResetSlidingWindow(retainHistory);
        Stage = ReadBlockHeaderStage;
        FinalBlock = false;
        StoredRemaining = 0;
        DynamicTableReader = null;
        LiteralLengthDecoder = null;
        DistanceDecoder = null;
        PendingLiteral = 0;
        MatchLengthBase = 0;
        MatchLengthExtraBits = 0;
        MatchLength = 0;
        MatchDistanceBase = 0;
        MatchDistanceExtraBits = 0;
        TotalOutputCount = 0;
        StreamComplete = false;
        AtSyncFlushBoundary = false;
        Failed = false;
        FailureStatus = WebSocketParseStatus.ProtocolFailure;
        FailureIdentifier = "";
    }

    internal int GetRawDeflateHistoryCount()
    {
        return Window.GetHistoryCount();
    }

    void SetCompressedTables(
        DeflateHuffmanTable literalLengthTable,
        DeflateHuffmanTable distanceTable)
    {
        LiteralLengthDecoder =
            new DeflateHuffmanDecoder(literalLengthTable);
        DistanceDecoder = null;
        if (distanceTable.MaximumCodeLength > 0)
        {
            DistanceDecoder = new DeflateHuffmanDecoder(distanceTable);
        }
    }

    RawDeflateDecodeResult FinishBlock(
        int consumedCount,
        int producedCount,
        bool emptyStoredBlock)
    {
        DynamicTableReader = null;
        LiteralLengthDecoder = null;
        DistanceDecoder = null;
        Stage = ReadBlockHeaderStage;
        if (FinalBlock)
        {
            StreamComplete = true;
            return CreateDecodeResult(
                WebSocketParseStatus.Complete,
                consumedCount,
                producedCount,
                false);
        }

        if (emptyStoredBlock)
        {
            AtSyncFlushBoundary = true;
            return CreateDecodeResult(
                WebSocketParseStatus.Complete,
                consumedCount,
                producedCount,
                true);
        }

        RawDeflateDecodeResult result = new RawDeflateDecodeResult();
        result.Status = WebSocketParseStatus.NeedInput;
        return result;
    }

    RawDeflateDecodeResult FailDecode(
        WebSocketParseStatus status,
        string identifier,
        int consumedCount,
        int producedCount)
    {
        Failed = true;
        FailureStatus = status;
        FailureIdentifier = identifier;
        return CreateDecodeResult(
            status,
            consumedCount,
            producedCount,
            false);
    }

    RawDeflateDecodeResult CreateDecodeResult(
        WebSocketParseStatus status,
        int consumedCount,
        int producedCount,
        bool syncFlushComplete)
    {
        RawDeflateDecodeResult result = new RawDeflateDecodeResult();
        result.Status = status;
        result.ConsumedCount = consumedCount;
        result.ProducedCount = producedCount;
        result.TotalOutputCount = TotalOutputCount;
        result.StreamComplete = StreamComplete;
        result.SyncFlushComplete = syncFlushComplete;
        result.ErrorIdentifier = FailureIdentifier;
        return result;
    }
}
