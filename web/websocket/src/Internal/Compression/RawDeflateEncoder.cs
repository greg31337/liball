using System;

namespace websocket;

internal class RawDeflateEncodeResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int ConsumedCount;
    internal int ProducedCount;
    internal long TotalInputCount;
    internal bool StreamComplete;
    internal bool SyncFlushComplete;
    internal int LastBlockType = -1;
    internal string ErrorIdentifier = "";
}

internal class DeflateEncoderDynamicTables
{
    internal DeflateHuffmanTable LiteralLengthTable =
        new DeflateHuffmanTable();
    internal DeflateHuffmanTable DistanceTable =
        new DeflateHuffmanTable();
    internal DeflateHuffmanTable CodeLengthTable =
        new DeflateHuffmanTable();
    internal int LiteralLengthLastIndex;
    internal int DistanceLastIndex;
    internal int CodeLengthCount;
    internal int HeaderBitCount;
}

internal class RawDeflateEncoder
{
    const int BlockCapacity = 32768;
    const int HashSize = 4096;
    const int MaximumMatchCandidates = 128;

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

    static readonly int[] CodeLengthOrder =
        [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15];

    DeflateBitWriter Writer = new DeflateBitWriter();
    DeflateMatchHistory History;
    byte[] Block = new byte[BlockCapacity];
    int BlockCount;
    byte[] Generated = new byte[(BlockCapacity * 2) + 4096];
    int GeneratedOffset;
    int GeneratedCount;
    byte[] SearchBytes;
    int[] HashHeads = new int[HashSize];
    int[] HashPrevious;
    int[] TokenKinds = new int[BlockCapacity];
    int[] TokenValues = new int[BlockCapacity];
    int[] TokenDistances = new int[BlockCapacity];
    int[] TokenLengthSymbols = new int[BlockCapacity];
    int[] TokenDistanceSymbols = new int[BlockCapacity];
    int TokenCount;
    int[] LiteralLengthFrequencies = new int[286];
    int[] DistanceFrequencies = new int[30];
    long TotalInputCount;
    bool FlushInProgress;
    bool FlushIsFinal;
    bool StreamComplete;
    bool Failed;
    string FailureIdentifier = "";
    int LastBlockType = -1;

    internal RawDeflateEncoder(int windowBits)
    {
        History = new DeflateMatchHistory(windowBits);
        SearchBytes = new byte[History.GetMatchWindowSize() + BlockCapacity];
        HashPrevious = new int[SearchBytes.Length];
    }

    internal RawDeflateEncodeResult EncodeRawDeflate(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        if (Failed)
        {
            return CreateEncodeResult(
                WebSocketParseStatus.ProtocolFailure,
                0,
                0,
                false);
        }

        if (StreamComplete || FlushInProgress)
        {
            return FailEncode(
                "deflate-encoder-state-invalid",
                0,
                0);
        }

        int sourceIndex = 0;
        int destinationIndex = DrainGenerated(destination);
        if (GeneratedCount != 0)
        {
            return CreateEncodeResult(
                WebSocketParseStatus.NeedOutputCapacity,
                0,
                destinationIndex,
                false);
        }

        while (sourceIndex < source.Length)
        {
            int copyCount = BlockCapacity - BlockCount;
            int sourceRemaining = source.Length - sourceIndex;
            if (copyCount > sourceRemaining)
            {
                copyCount = sourceRemaining;
            }

            source.Slice(sourceIndex, copyCount).CopyTo(
                Block.AsSpan(BlockCount));
            BlockCount = BlockCount + copyCount;
            sourceIndex = sourceIndex + copyCount;
            TotalInputCount = TotalInputCount + copyCount;
            if (BlockCount == BlockCapacity)
            {
                PrepareGeneratedOutput();
                EncodeDataBlock(false);
                int produced = DrainGenerated(
                    destination.Slice(destinationIndex));
                destinationIndex = destinationIndex + produced;
                if (GeneratedCount != 0)
                {
                    return CreateEncodeResult(
                        WebSocketParseStatus.NeedOutputCapacity,
                        sourceIndex,
                        destinationIndex,
                        false);
                }
            }
        }

        return CreateEncodeResult(
            WebSocketParseStatus.Complete,
            sourceIndex,
            destinationIndex,
            false);
    }

    internal RawDeflateEncodeResult FinishRawDeflate(
        Span<byte> destination,
        bool finalStream)
    {
        if (Failed)
        {
            return CreateEncodeResult(
                WebSocketParseStatus.ProtocolFailure,
                0,
                0,
                false);
        }

        if (StreamComplete)
        {
            return CreateEncodeResult(
                WebSocketParseStatus.Complete,
                0,
                0,
                false);
        }

        int producedCount = 0;
        if (!FlushInProgress && GeneratedCount != 0)
        {
            producedCount = DrainGenerated(destination);
            if (GeneratedCount != 0)
            {
                return CreateEncodeResult(
                    WebSocketParseStatus.NeedOutputCapacity,
                    0,
                    producedCount,
                    false);
            }
        }

        if (!FlushInProgress)
        {
            PrepareGeneratedOutput();
            FlushInProgress = true;
            FlushIsFinal = finalStream;
            if (finalStream)
            {
                EncodeDataBlock(true);
                FinishGeneratedByte();
            }
            else
            {
                if (BlockCount != 0)
                {
                    EncodeDataBlock(false);
                }

                EncodeEmptySyncFlushBlock();
            }
        }
        else if (FlushIsFinal != finalStream)
        {
            return FailEncode(
                "deflate-encoder-state-invalid",
                0,
                0);
        }

        int flushedCount = DrainGenerated(
            destination.Slice(producedCount));
        producedCount = producedCount + flushedCount;
        if (GeneratedCount != 0)
        {
            return CreateEncodeResult(
                WebSocketParseStatus.NeedOutputCapacity,
                0,
                producedCount,
                false);
        }

        FlushInProgress = false;
        if (FlushIsFinal)
        {
            StreamComplete = true;
            return CreateEncodeResult(
                WebSocketParseStatus.Complete,
                0,
                producedCount,
                false);
        }

        return CreateEncodeResult(
            WebSocketParseStatus.Complete,
            0,
            producedCount,
            true);
    }

    internal void ResetRawDeflateEncoder(bool retainHistory)
    {
        Writer.ResetBitWriter();
        History.ResetMatchHistory(retainHistory);
        BlockCount = 0;
        GeneratedOffset = 0;
        GeneratedCount = 0;
        TokenCount = 0;
        TotalInputCount = 0;
        FlushInProgress = false;
        FlushIsFinal = false;
        StreamComplete = false;
        Failed = false;
        FailureIdentifier = "";
        LastBlockType = -1;
    }

    internal int GetRawDeflateEncoderHistoryCount()
    {
        return History.GetMatchHistoryCount();
    }

    void EncodeDataBlock(bool finalBlock)
    {
        AnalyzeBlock();
        DeflateHuffmanTable fixedLiteral =
            DeflateHuffmanTables.CreateFixedLiteralLengthTable();
        DeflateHuffmanTable fixedDistance =
            DeflateHuffmanTables.CreateFixedDistanceTable();
        DeflateEncoderDynamicTables dynamicTables =
            BuildDynamicTables();
        int storedBits = MeasureStoredBlockBits();
        int fixedBits = MeasureCompressedBlockBits(
            fixedLiteral,
            fixedDistance,
            3);
        int dynamicBits = MeasureCompressedBlockBits(
            dynamicTables.LiteralLengthTable,
            dynamicTables.DistanceTable,
            3 + dynamicTables.HeaderBitCount);
        int blockType = 0;
        int smallest = storedBits;
        if (fixedBits < smallest)
        {
            blockType = 1;
            smallest = fixedBits;
        }

        if (dynamicBits < smallest)
        {
            blockType = 2;
        }

        LastBlockType = blockType;
        if (blockType == 0)
        {
            EncodeStoredBlock(finalBlock);
        }
        else if (blockType == 1)
        {
            EncodeFixedBlock(
                finalBlock,
                fixedLiteral,
                fixedDistance);
        }
        else
        {
            EncodeDynamicBlock(finalBlock, dynamicTables);
        }

        History.AppendMatchBytes(Block.AsSpan(0, BlockCount));
        BlockCount = 0;
    }

    void AnalyzeBlock()
    {
        LiteralLengthFrequencies.AsSpan().Clear();
        DistanceFrequencies.AsSpan().Clear();
        TokenCount = 0;
        int historyCount = History.CopyMatchHistory(SearchBytes);
        Block.AsSpan(0, BlockCount).CopyTo(
            SearchBytes.AsSpan(historyCount));
        int searchLength = historyCount + BlockCount;
        int hashIndex = 0;
        while (hashIndex < HashHeads.Length)
        {
            HashHeads[hashIndex] = -1;
            hashIndex = hashIndex + 1;
        }

        int previousIndex = 0;
        while (previousIndex < searchLength)
        {
            HashPrevious[previousIndex] = -1;
            previousIndex = previousIndex + 1;
        }

        int seedIndex = 0;
        while (seedIndex < historyCount)
        {
            InsertHashPosition(seedIndex, searchLength);
            seedIndex = seedIndex + 1;
        }

        int blockIndex = 0;
        while (blockIndex < BlockCount)
        {
            int absoluteIndex = historyCount + blockIndex;
            int remaining = BlockCount - blockIndex;
            int bestLength = 0;
            int bestDistance = 0;
            if (remaining >= 3)
            {
                int hash = ComputeHash(absoluteIndex);
                int candidate = HashHeads[hash];
                int candidateCount = 0;
                while (candidate >= 0 &&
                    candidateCount < MaximumMatchCandidates)
                {
                    int distance = absoluteIndex - candidate;
                    if (distance > History.GetMatchWindowSize())
                    {
                        break;
                    }

                    int length = MeasureMatchLength(
                        candidate,
                        absoluteIndex,
                        remaining);
                    if (length > bestLength)
                    {
                        bestLength = length;
                        bestDistance = distance;
                        if (bestLength == 258)
                        {
                            break;
                        }
                    }

                    candidate = HashPrevious[candidate];
                    candidateCount = candidateCount + 1;
                }
            }

            int advance = 1;
            if (bestLength >= 3)
            {
                int lengthSymbol = FindLengthSymbol(bestLength);
                int distanceSymbol = FindDistanceSymbol(bestDistance);
                TokenKinds[TokenCount] = 1;
                TokenValues[TokenCount] = bestLength;
                TokenDistances[TokenCount] = bestDistance;
                TokenLengthSymbols[TokenCount] = lengthSymbol;
                TokenDistanceSymbols[TokenCount] = distanceSymbol;
                LiteralLengthFrequencies[lengthSymbol] =
                    LiteralLengthFrequencies[lengthSymbol] + 1;
                DistanceFrequencies[distanceSymbol] =
                    DistanceFrequencies[distanceSymbol] + 1;
                advance = bestLength;
            }
            else
            {
                int literal = Block[blockIndex];
                TokenKinds[TokenCount] = 0;
                TokenValues[TokenCount] = literal;
                TokenDistances[TokenCount] = 0;
                TokenLengthSymbols[TokenCount] = 0;
                TokenDistanceSymbols[TokenCount] = 0;
                LiteralLengthFrequencies[literal] =
                    LiteralLengthFrequencies[literal] + 1;
            }

            TokenCount = TokenCount + 1;
            int inserted = 0;
            while (inserted < advance)
            {
                InsertHashPosition(
                    absoluteIndex + inserted,
                    searchLength);
                inserted = inserted + 1;
            }

            blockIndex = blockIndex + advance;
        }

        LiteralLengthFrequencies[256] =
            LiteralLengthFrequencies[256] + 1;
    }

    void InsertHashPosition(int position, int searchLength)
    {
        if (position + 2 >= searchLength)
        {
            return;
        }

        int hash = ComputeHash(position);
        HashPrevious[position] = HashHeads[hash];
        HashHeads[hash] = position;
    }

    int ComputeHash(int position)
    {
        int hash = SearchBytes[position] * 251;
        hash = hash + (SearchBytes[position + 1] * 31);
        hash = hash + SearchBytes[position + 2];
        return hash & (HashSize - 1);
    }

    int MeasureMatchLength(
        int candidate,
        int current,
        int remaining)
    {
        int maximum = remaining;
        if (maximum > 258)
        {
            maximum = 258;
        }

        int length = 0;
        while (length < maximum &&
            SearchBytes[candidate + length] ==
                SearchBytes[current + length])
        {
            length = length + 1;
        }

        return length;
    }

    int FindLengthSymbol(int length)
    {
        int index = 0;
        while (index < LengthBases.Length)
        {
            int maximum = LengthBases[index];
            int extraBits = LengthExtraBits[index];
            if (extraBits != 0)
            {
                maximum = maximum + ((1 << extraBits) - 1);
            }

            if (length <= maximum)
            {
                return index + 257;
            }

            index = index + 1;
        }

        return 285;
    }

    int FindDistanceSymbol(int distance)
    {
        int index = 0;
        while (index < DistanceBases.Length)
        {
            int maximum = DistanceBases[index];
            int extraBits = DistanceExtraBits[index];
            if (extraBits != 0)
            {
                maximum = maximum + ((1 << extraBits) - 1);
            }

            if (distance <= maximum)
            {
                return index;
            }

            index = index + 1;
        }

        return 29;
    }

    DeflateEncoderDynamicTables BuildDynamicTables()
    {
        DeflateEncoderDynamicTables tables =
            new DeflateEncoderDynamicTables();
        int[] literalLengths = BuildBalancedCodeLengths(
            LiteralLengthFrequencies);
        int[] distanceLengths = BuildBalancedCodeLengths(
            DistanceFrequencies);
        tables.LiteralLengthLastIndex = FindLastNonzero(
            literalLengths,
            256);
        tables.DistanceLastIndex = FindLastNonzero(
            distanceLengths,
            0);
        int[] codeLengthFrequencies = new int[19];
        int literalIndex = 0;
        while (literalIndex <= tables.LiteralLengthLastIndex)
        {
            int length = literalLengths[literalIndex];
            codeLengthFrequencies[length] =
                codeLengthFrequencies[length] + 1;
            literalIndex = literalIndex + 1;
        }

        int distanceIndex = 0;
        while (distanceIndex <= tables.DistanceLastIndex)
        {
            int length = distanceLengths[distanceIndex];
            codeLengthFrequencies[length] =
                codeLengthFrequencies[length] + 1;
            distanceIndex = distanceIndex + 1;
        }

        EnsureTwoCodeLengthSymbols(codeLengthFrequencies);
        int[] codeLengthLengths = BuildBalancedCodeLengths(
            codeLengthFrequencies);
        int orderIndex = CodeLengthOrder.Length - 1;
        while (orderIndex >= 4 &&
            codeLengthLengths[CodeLengthOrder[orderIndex]] == 0)
        {
            orderIndex = orderIndex - 1;
        }

        tables.CodeLengthCount = orderIndex + 1;
        tables.LiteralLengthTable = DeflateHuffmanBuilder.BuildTable(
            literalLengths,
            15,
            false,
            true).Table;
        DeflateHuffmanBuildResult distanceBuild =
            DeflateHuffmanBuilder.BuildTable(
                distanceLengths,
                15,
                true,
                true);
        tables.DistanceTable = distanceBuild.Table;
        tables.CodeLengthTable = DeflateHuffmanBuilder.BuildTable(
            codeLengthLengths,
            7,
            false,
            false).Table;
        int headerBits = 14 + (tables.CodeLengthCount * 3);
        literalIndex = 0;
        while (literalIndex <= tables.LiteralLengthLastIndex)
        {
            int length = literalLengths[literalIndex];
            headerBits = headerBits +
                tables.CodeLengthTable.CodeLengths[length];
            literalIndex = literalIndex + 1;
        }

        distanceIndex = 0;
        while (distanceIndex <= tables.DistanceLastIndex)
        {
            int length = distanceLengths[distanceIndex];
            headerBits = headerBits +
                tables.CodeLengthTable.CodeLengths[length];
            distanceIndex = distanceIndex + 1;
        }

        tables.HeaderBitCount = headerBits;
        return tables;
    }

    int[] BuildBalancedCodeLengths(int[] frequencies)
    {
        int usedCount = 0;
        int index = 0;
        while (index < frequencies.Length)
        {
            if (frequencies[index] != 0)
            {
                usedCount = usedCount + 1;
            }

            index = index + 1;
        }

        int[] lengths = new int[frequencies.Length];
        if (usedCount == 0)
        {
            return lengths;
        }

        int[] symbols = new int[usedCount];
        int symbolIndex = 0;
        index = 0;
        while (index < frequencies.Length)
        {
            if (frequencies[index] != 0)
            {
                symbols[symbolIndex] = index;
                symbolIndex = symbolIndex + 1;
            }

            index = index + 1;
        }

        SortSymbolsByFrequency(symbols, frequencies);
        if (usedCount == 1)
        {
            lengths[symbols[0]] = 1;
            return lengths;
        }

        int power = 1;
        int shortLength = 0;
        while ((power << 1) <= usedCount)
        {
            power = power << 1;
            shortLength = shortLength + 1;
        }

        int shortCount = (power << 1) - usedCount;
        index = 0;
        while (index < usedCount)
        {
            if (index < shortCount)
            {
                lengths[symbols[index]] = shortLength;
            }
            else
            {
                lengths[symbols[index]] = shortLength + 1;
            }

            index = index + 1;
        }

        return lengths;
    }

    void SortSymbolsByFrequency(int[] symbols, int[] frequencies)
    {
        int destination = 0;
        while (destination < symbols.Length)
        {
            int best = destination;
            int candidate = destination + 1;
            while (candidate < symbols.Length)
            {
                int candidateSymbol = symbols[candidate];
                int bestSymbol = symbols[best];
                if (frequencies[candidateSymbol] > frequencies[bestSymbol] ||
                    (frequencies[candidateSymbol] == frequencies[bestSymbol] &&
                        candidateSymbol < bestSymbol))
                {
                    best = candidate;
                }

                candidate = candidate + 1;
            }

            int temporary = symbols[destination];
            symbols[destination] = symbols[best];
            symbols[best] = temporary;
            destination = destination + 1;
        }
    }

    int FindLastNonzero(int[] lengths, int minimum)
    {
        int index = lengths.Length - 1;
        while (index > minimum && lengths[index] == 0)
        {
            index = index - 1;
        }

        return index;
    }

    void EnsureTwoCodeLengthSymbols(int[] frequencies)
    {
        int usedCount = 0;
        int usedSymbol = 0;
        int index = 0;
        while (index < frequencies.Length)
        {
            if (frequencies[index] != 0)
            {
                usedCount = usedCount + 1;
                usedSymbol = index;
            }

            index = index + 1;
        }

        if (usedCount == 1)
        {
            int addedSymbol = 0;
            if (usedSymbol == 0)
            {
                addedSymbol = 16;
            }

            frequencies[addedSymbol] = 1;
        }
    }

    int MeasureStoredBlockBits()
    {
        int pendingBits = Writer.GetPendingBitCount();
        int paddingBits = 8 - ((pendingBits + 3) % 8);
        if (paddingBits == 8)
        {
            paddingBits = 0;
        }

        return 3 + paddingBits + 32 + (BlockCount * 8);
    }

    int MeasureCompressedBlockBits(
        DeflateHuffmanTable literalTable,
        DeflateHuffmanTable distanceTable,
        int initialBits)
    {
        int bitCount = initialBits;
        int tokenIndex = 0;
        while (tokenIndex < TokenCount)
        {
            if (TokenKinds[tokenIndex] == 0)
            {
                int literal = TokenValues[tokenIndex];
                bitCount = bitCount + literalTable.CodeLengths[literal];
            }
            else
            {
                int lengthSymbol = TokenLengthSymbols[tokenIndex];
                int distanceSymbol = TokenDistanceSymbols[tokenIndex];
                bitCount = bitCount +
                    literalTable.CodeLengths[lengthSymbol] +
                    LengthExtraBits[lengthSymbol - 257] +
                    distanceTable.CodeLengths[distanceSymbol] +
                    DistanceExtraBits[distanceSymbol];
            }

            tokenIndex = tokenIndex + 1;
        }

        bitCount = bitCount + literalTable.CodeLengths[256];
        return bitCount;
    }

    void EncodeStoredBlock(bool finalBlock)
    {
        int header = 0;
        if (finalBlock)
        {
            header = 1;
        }

        WriteValueBits(header, 3);
        FinishGeneratedByte();
        DeflateStoredBlockWriteResult stored =
            DeflateStoredBlockHeaderWriter.WriteStoredBlockHeader(
                BlockCount,
                Generated.AsSpan(GeneratedCount));
        GeneratedCount = GeneratedCount + stored.ProducedCount;
        Block.AsSpan(0, BlockCount).CopyTo(
            Generated.AsSpan(GeneratedCount));
        GeneratedCount = GeneratedCount + BlockCount;
    }

    void EncodeFixedBlock(
        bool finalBlock,
        DeflateHuffmanTable literalTable,
        DeflateHuffmanTable distanceTable)
    {
        int header = 2;
        if (finalBlock)
        {
            header = 3;
        }

        WriteValueBits(header, 3);
        EncodeTokens(literalTable, distanceTable);
    }

    void EncodeDynamicBlock(
        bool finalBlock,
        DeflateEncoderDynamicTables tables)
    {
        int header = 4;
        if (finalBlock)
        {
            header = 5;
        }

        WriteValueBits(header, 3);
        WriteValueBits(tables.LiteralLengthLastIndex - 256, 5);
        WriteValueBits(tables.DistanceLastIndex, 5);
        WriteValueBits(tables.CodeLengthCount - 4, 4);
        int orderIndex = 0;
        while (orderIndex < tables.CodeLengthCount)
        {
            int symbol = CodeLengthOrder[orderIndex];
            WriteValueBits(
                tables.CodeLengthTable.CodeLengths[symbol],
                3);
            orderIndex = orderIndex + 1;
        }

        int literalIndex = 0;
        while (literalIndex <= tables.LiteralLengthLastIndex)
        {
            int length =
                tables.LiteralLengthTable.CodeLengths[literalIndex];
            WriteHuffmanBits(
                tables.CodeLengthTable.Codes[length],
                tables.CodeLengthTable.CodeLengths[length]);
            literalIndex = literalIndex + 1;
        }

        int distanceIndex = 0;
        while (distanceIndex <= tables.DistanceLastIndex)
        {
            int length = tables.DistanceTable.CodeLengths[distanceIndex];
            WriteHuffmanBits(
                tables.CodeLengthTable.Codes[length],
                tables.CodeLengthTable.CodeLengths[length]);
            distanceIndex = distanceIndex + 1;
        }

        EncodeTokens(
            tables.LiteralLengthTable,
            tables.DistanceTable);
    }

    void EncodeTokens(
        DeflateHuffmanTable literalTable,
        DeflateHuffmanTable distanceTable)
    {
        int tokenIndex = 0;
        while (tokenIndex < TokenCount)
        {
            if (TokenKinds[tokenIndex] == 0)
            {
                int literal = TokenValues[tokenIndex];
                WriteHuffmanBits(
                    literalTable.Codes[literal],
                    literalTable.CodeLengths[literal]);
            }
            else
            {
                int lengthSymbol = TokenLengthSymbols[tokenIndex];
                int lengthIndex = lengthSymbol - 257;
                WriteHuffmanBits(
                    literalTable.Codes[lengthSymbol],
                    literalTable.CodeLengths[lengthSymbol]);
                WriteValueBits(
                    TokenValues[tokenIndex] - LengthBases[lengthIndex],
                    LengthExtraBits[lengthIndex]);
                int distanceSymbol = TokenDistanceSymbols[tokenIndex];
                WriteHuffmanBits(
                    distanceTable.Codes[distanceSymbol],
                    distanceTable.CodeLengths[distanceSymbol]);
                WriteValueBits(
                    TokenDistances[tokenIndex] -
                        DistanceBases[distanceSymbol],
                    DistanceExtraBits[distanceSymbol]);
            }

            tokenIndex = tokenIndex + 1;
        }

        WriteHuffmanBits(
            literalTable.Codes[256],
            literalTable.CodeLengths[256]);
    }

    void EncodeEmptySyncFlushBlock()
    {
        WriteValueBits(0, 3);
        FinishGeneratedByte();
        DeflateStoredBlockWriteResult stored =
            DeflateStoredBlockHeaderWriter.WriteStoredBlockHeader(
                0,
                Generated.AsSpan(GeneratedCount));
        GeneratedCount = GeneratedCount + stored.ProducedCount;
    }

    void WriteValueBits(int value, int bitCount)
    {
        DeflateBitWriteResult write = Writer.WriteBits(
            value,
            bitCount,
            Generated.AsSpan(GeneratedCount));
        GeneratedCount = GeneratedCount + write.ProducedCount;
    }

    void WriteHuffmanBits(int code, int bitCount)
    {
        DeflateBitWriteResult write = Writer.WriteHuffmanCode(
            code,
            bitCount,
            Generated.AsSpan(GeneratedCount));
        GeneratedCount = GeneratedCount + write.ProducedCount;
    }

    void FinishGeneratedByte()
    {
        DeflateBitWriteResult finish = Writer.FinishWriterByte(
            Generated.AsSpan(GeneratedCount));
        GeneratedCount = GeneratedCount + finish.ProducedCount;
    }

    void PrepareGeneratedOutput()
    {
        GeneratedOffset = 0;
        GeneratedCount = 0;
    }

    int DrainGenerated(Span<byte> destination)
    {
        int copyCount = GeneratedCount;
        if (copyCount > destination.Length)
        {
            copyCount = destination.Length;
        }

        Generated.AsSpan(GeneratedOffset, copyCount).CopyTo(destination);
        GeneratedOffset = GeneratedOffset + copyCount;
        GeneratedCount = GeneratedCount - copyCount;
        if (GeneratedCount == 0)
        {
            GeneratedOffset = 0;
        }

        return copyCount;
    }

    RawDeflateEncodeResult FailEncode(
        string identifier,
        int consumedCount,
        int producedCount)
    {
        Failed = true;
        FailureIdentifier = identifier;
        return CreateEncodeResult(
            WebSocketParseStatus.ProtocolFailure,
            consumedCount,
            producedCount,
            false);
    }

    RawDeflateEncodeResult CreateEncodeResult(
        WebSocketParseStatus status,
        int consumedCount,
        int producedCount,
        bool syncFlushComplete)
    {
        RawDeflateEncodeResult result = new RawDeflateEncodeResult();
        result.Status = status;
        result.ConsumedCount = consumedCount;
        result.ProducedCount = producedCount;
        result.TotalInputCount = TotalInputCount;
        result.StreamComplete = StreamComplete;
        result.SyncFlushComplete = syncFlushComplete;
        result.LastBlockType = LastBlockType;
        result.ErrorIdentifier = FailureIdentifier;
        return result;
    }
}
