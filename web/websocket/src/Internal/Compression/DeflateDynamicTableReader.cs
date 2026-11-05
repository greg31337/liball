using System;

namespace websocket;

internal class DeflateDynamicTableResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal DeflateHuffmanTable LiteralLengthTable =
        new DeflateHuffmanTable();
    internal DeflateHuffmanTable DistanceTable =
        new DeflateHuffmanTable();
}

internal class DeflateDynamicTableReader
{
    static readonly int[] CodeLengthOrder =
        [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15];

    internal DeflateBitReader Reader;
    int Stage;
    int LiteralLengthCount;
    int DistanceCount;
    int CodeLengthCount;
    int CodeLengthIndex;
    int[] CodeLengthCodeLengths = new int[19];
    int[] CombinedCodeLengths = Array.Empty<int>();
    int CombinedIndex;
    int PreviousCodeLength;
    int PendingRepeatValue;
    DeflateHuffmanDecoder? CodeLengthDecoder;
    bool Failed;
    bool Complete;
    DeflateHuffmanTable LiteralLengthTable = new DeflateHuffmanTable();
    DeflateHuffmanTable DistanceTable = new DeflateHuffmanTable();

    internal DeflateDynamicTableReader(DeflateBitReader reader)
    {
        if (reader == null)
        {
            throw new WebSocketStateException();
        }

        Reader = reader;
    }

    internal DeflateDynamicTableResult ReadDynamicTables(
        ReadOnlySpan<byte> source)
    {
        if (Failed)
        {
            return CreateDynamicResult(
                WebSocketParseStatus.ProtocolFailure,
                0);
        }

        if (Complete)
        {
            return CreateDynamicResult(WebSocketParseStatus.Complete, 0);
        }

        int sourceIndex = 0;
        while (true)
        {
            if (Stage == 0)
            {
                DeflateBitReadResult field = Reader.ReadBits(
                    source.Slice(sourceIndex),
                    5);
                sourceIndex = sourceIndex + field.ConsumedCount;
                if (field.Status != WebSocketParseStatus.Complete)
                {
                    return CreateDynamicResult(field.Status, sourceIndex);
                }

                LiteralLengthCount = field.Value + 257;
                Stage = 1;
            }
            else if (Stage == 1)
            {
                DeflateBitReadResult field = Reader.ReadBits(
                    source.Slice(sourceIndex),
                    5);
                sourceIndex = sourceIndex + field.ConsumedCount;
                if (field.Status != WebSocketParseStatus.Complete)
                {
                    return CreateDynamicResult(field.Status, sourceIndex);
                }

                DistanceCount = field.Value + 1;
                Stage = 2;
            }
            else if (Stage == 2)
            {
                DeflateBitReadResult field = Reader.ReadBits(
                    source.Slice(sourceIndex),
                    4);
                sourceIndex = sourceIndex + field.ConsumedCount;
                if (field.Status != WebSocketParseStatus.Complete)
                {
                    return CreateDynamicResult(field.Status, sourceIndex);
                }

                CodeLengthCount = field.Value + 4;
                CombinedCodeLengths =
                    new int[LiteralLengthCount + DistanceCount];
                Stage = 3;
            }
            else if (Stage == 3)
            {
                while (CodeLengthIndex < CodeLengthCount)
                {
                    DeflateBitReadResult field = Reader.ReadBits(
                        source.Slice(sourceIndex),
                        3);
                    sourceIndex = sourceIndex + field.ConsumedCount;
                    if (field.Status != WebSocketParseStatus.Complete)
                    {
                        return CreateDynamicResult(field.Status, sourceIndex);
                    }

                    int symbol = CodeLengthOrder[CodeLengthIndex];
                    CodeLengthCodeLengths[symbol] = field.Value;
                    CodeLengthIndex = CodeLengthIndex + 1;
                }

                DeflateHuffmanBuildResult codeLengthBuild =
                    DeflateHuffmanBuilder.BuildTable(
                        CodeLengthCodeLengths,
                        7,
                        false,
                        false);
                if (codeLengthBuild.Status != WebSocketParseStatus.Complete)
                {
                    return FailDynamicTables(sourceIndex);
                }

                CodeLengthDecoder =
                    new DeflateHuffmanDecoder(codeLengthBuild.Table);
                Stage = 4;
            }
            else if (Stage == 4)
            {
                if (CombinedIndex == CombinedCodeLengths.Length)
                {
                    return FinishDynamicTables(sourceIndex);
                }

                if (CodeLengthDecoder == null)
                {
                    return FailDynamicTables(sourceIndex);
                }

                DeflateHuffmanReadResult symbolResult =
                    CodeLengthDecoder.ReadSymbol(
                        Reader,
                        source.Slice(sourceIndex));
                sourceIndex = sourceIndex + symbolResult.ConsumedCount;
                if (symbolResult.Status != WebSocketParseStatus.Complete)
                {
                    if (symbolResult.Status ==
                        WebSocketParseStatus.ProtocolFailure)
                    {
                        return FailDynamicTables(sourceIndex);
                    }

                    return CreateDynamicResult(
                        symbolResult.Status,
                        sourceIndex);
                }

                if (symbolResult.Symbol <= 15)
                {
                    CombinedCodeLengths[CombinedIndex] = symbolResult.Symbol;
                    PreviousCodeLength = symbolResult.Symbol;
                    CombinedIndex = CombinedIndex + 1;
                }
                else if (symbolResult.Symbol == 16)
                {
                    if (CombinedIndex == 0)
                    {
                        return FailDynamicTables(sourceIndex);
                    }

                    PendingRepeatValue = PreviousCodeLength;
                    Stage = 5;
                }
                else if (symbolResult.Symbol == 17)
                {
                    PendingRepeatValue = 0;
                    Stage = 6;
                }
                else if (symbolResult.Symbol == 18)
                {
                    PendingRepeatValue = 0;
                    Stage = 7;
                }
                else
                {
                    return FailDynamicTables(sourceIndex);
                }
            }
            else if (Stage == 5)
            {
                DeflateBitReadResult repeat = Reader.ReadBits(
                    source.Slice(sourceIndex),
                    2);
                sourceIndex = sourceIndex + repeat.ConsumedCount;
                if (repeat.Status != WebSocketParseStatus.Complete)
                {
                    return CreateDynamicResult(repeat.Status, sourceIndex);
                }

                if (!AppendCodeLengthRepeat(repeat.Value + 3))
                {
                    return FailDynamicTables(sourceIndex);
                }

                Stage = 4;
            }
            else if (Stage == 6)
            {
                DeflateBitReadResult repeat = Reader.ReadBits(
                    source.Slice(sourceIndex),
                    3);
                sourceIndex = sourceIndex + repeat.ConsumedCount;
                if (repeat.Status != WebSocketParseStatus.Complete)
                {
                    return CreateDynamicResult(repeat.Status, sourceIndex);
                }

                if (!AppendCodeLengthRepeat(repeat.Value + 3))
                {
                    return FailDynamicTables(sourceIndex);
                }

                Stage = 4;
            }
            else if (Stage == 7)
            {
                DeflateBitReadResult repeat = Reader.ReadBits(
                    source.Slice(sourceIndex),
                    7);
                sourceIndex = sourceIndex + repeat.ConsumedCount;
                if (repeat.Status != WebSocketParseStatus.Complete)
                {
                    return CreateDynamicResult(repeat.Status, sourceIndex);
                }

                if (!AppendCodeLengthRepeat(repeat.Value + 11))
                {
                    return FailDynamicTables(sourceIndex);
                }

                Stage = 4;
            }
            else
            {
                return FailDynamicTables(sourceIndex);
            }
        }
    }

    internal void ResetDynamicTableReader()
    {
        Stage = 0;
        LiteralLengthCount = 0;
        DistanceCount = 0;
        CodeLengthCount = 0;
        CodeLengthIndex = 0;
        CodeLengthCodeLengths = new int[19];
        CombinedCodeLengths = Array.Empty<int>();
        CombinedIndex = 0;
        PreviousCodeLength = 0;
        PendingRepeatValue = 0;
        CodeLengthDecoder = null;
        Failed = false;
        Complete = false;
        LiteralLengthTable = new DeflateHuffmanTable();
        DistanceTable = new DeflateHuffmanTable();
    }

    bool AppendCodeLengthRepeat(int count)
    {
        if (count <= 0 || count > CombinedCodeLengths.Length - CombinedIndex)
        {
            return false;
        }

        int repeated = 0;
        while (repeated < count)
        {
            CombinedCodeLengths[CombinedIndex] = PendingRepeatValue;
            CombinedIndex = CombinedIndex + 1;
            repeated = repeated + 1;
        }

        PreviousCodeLength = PendingRepeatValue;
        return true;
    }

    DeflateDynamicTableResult FinishDynamicTables(int consumedCount)
    {
        int[] literalLengths = new int[LiteralLengthCount];
        int literalIndex = 0;
        while (literalIndex < literalLengths.Length)
        {
            literalLengths[literalIndex] = CombinedCodeLengths[literalIndex];
            literalIndex = literalIndex + 1;
        }

        if (literalLengths[256] == 0)
        {
            return FailDynamicTables(consumedCount);
        }

        int[] distanceLengths = new int[DistanceCount];
        int distanceIndex = 0;
        while (distanceIndex < distanceLengths.Length)
        {
            distanceLengths[distanceIndex] =
                CombinedCodeLengths[LiteralLengthCount + distanceIndex];
            distanceIndex = distanceIndex + 1;
        }

        DeflateHuffmanBuildResult literalBuild =
            DeflateHuffmanBuilder.BuildTable(
                literalLengths,
                15,
                false,
                true);
        DeflateHuffmanBuildResult distanceBuild =
            DeflateHuffmanBuilder.BuildTable(
                distanceLengths,
                15,
                true,
                true);
        if (literalBuild.Status != WebSocketParseStatus.Complete ||
            distanceBuild.Status != WebSocketParseStatus.Complete)
        {
            return FailDynamicTables(consumedCount);
        }

        LiteralLengthTable = literalBuild.Table;
        DistanceTable = distanceBuild.Table;
        Complete = true;
        return CreateDynamicResult(
            WebSocketParseStatus.Complete,
            consumedCount);
    }

    DeflateDynamicTableResult FailDynamicTables(int consumedCount)
    {
        Failed = true;
        return CreateDynamicResult(
            WebSocketParseStatus.ProtocolFailure,
            consumedCount);
    }

    DeflateDynamicTableResult CreateDynamicResult(
        WebSocketParseStatus status,
        int consumedCount)
    {
        DeflateDynamicTableResult result = new DeflateDynamicTableResult();
        result.Status = status;
        result.ConsumedCount = consumedCount;
        result.LiteralLengthTable = LiteralLengthTable;
        result.DistanceTable = DistanceTable;
        return result;
    }
}
