using System.Collections.Generic;

namespace jpeg;

internal class JpegHuffmanTableParser
{
    internal List<JpegHuffmanTable> Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        List<JpegHuffmanTable> tables = new List<JpegHuffmanTable>();
        int offset = 0;
        while (offset < payload.Length)
        {
            if (payload.Length - offset < 17)
            {
                throw JpegInvalidDataException.Create("DHT payload ended before a complete table header.");
            }

            int tableInfo = payload[offset];
            offset = offset + 1;
            JpegHuffmanTable table = CreateTable(tableInfo);

            int index = 0;
            int symbolCount = 0;
            while (index < 16)
            {
                int count = payload[offset + index];
                table.CodeCounts[index] = count;
                symbolCount = symbolCount + count;
                index = index + 1;
            }

            offset = offset + 16;
            if (symbolCount > payload.Length - offset)
            {
                throw JpegInvalidDataException.Create("DHT payload ended before all Huffman symbols.");
            }

            table.SymbolCount = symbolCount;
            table.Symbols = new int[symbolCount];
            table.Codes = new int[symbolCount];
            table.CodeLengths = new int[symbolCount];

            index = 0;
            while (index < symbolCount)
            {
                table.Symbols[index] = payload[offset + index];
                index = index + 1;
            }

            offset = offset + symbolCount;
            BuildDecodeTable(table);
            tables.Add(table);
        }

        return tables;
    }

    internal JpegHuffmanTable CreateTable(int tableInfo)
    {
        int tableClass = tableInfo >> 4;
        int identifier = tableInfo & 15;
        if (tableClass != 0 && tableClass != 1)
        {
            throw JpegInvalidDataException.Create("DHT table class must be DC or AC.");
        }

        if (identifier > 3)
        {
            throw JpegInvalidDataException.Create("DHT table identifier must be between 0 and 3.");
        }

        JpegHuffmanTable table = new JpegHuffmanTable();
        table.TableClass = tableClass;
        table.Identifier = identifier;
        return table;
    }

    internal void BuildDecodeTable(JpegHuffmanTable table)
    {
        InitializeDecodeRanges(table);

        int code = 0;
        int symbolIndex = 0;
        int length = 1;
        while (length <= 16)
        {
            int count = table.CodeCounts[length - 1];
            if (count > 0)
            {
                if (code + count > (1 << length))
                {
                    throw JpegInvalidDataException.Create("DHT code counts describe an over-subscribed Huffman tree.");
                }

                table.MinCode[length] = code;
                table.ValueOffset[length] = symbolIndex - code;
            }

            int index = 0;
            while (index < count)
            {
                table.Codes[symbolIndex] = code;
                table.CodeLengths[symbolIndex] = length;
                code = code + 1;
                symbolIndex = symbolIndex + 1;
                index = index + 1;
            }

            if (count > 0)
            {
                table.MaxCode[length] = code - 1;
            }

            code = code << 1;
            length = length + 1;
        }
    }

    internal void PrepareCallerTable(JpegHuffmanTable table)
    {
        int symbolCount = 0;
        int index = 0;
        while (index < 16)
        {
            symbolCount = symbolCount + table.CodeCounts[index];
            index = index + 1;
        }

        table.SymbolCount = symbolCount;
        table.Codes = new int[symbolCount];
        table.CodeLengths = new int[symbolCount];
        table.MinCode = new int[17];
        table.MaxCode = new int[17];
        table.ValueOffset = new int[17];
        BuildDecodeTable(table);
    }

    internal void InitializeDecodeRanges(JpegHuffmanTable table)
    {
        int index = 0;
        while (index < 17)
        {
            table.MinCode[index] = -1;
            table.MaxCode[index] = -1;
            table.ValueOffset[index] = 0;
            index = index + 1;
        }
    }

    internal int DecodeSymbol(JpegHuffmanTable table, int code, int length)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (length < 1 || length > 16)
        {
            return -1;
        }

        if (table.MinCode[length] < 0)
        {
            return -1;
        }

        if (code < table.MinCode[length] || code > table.MaxCode[length])
        {
            return -1;
        }

        int index = code + table.ValueOffset[length];
        if (index < 0 || index >= table.SymbolCount)
        {
            return -1;
        }

        return table.Symbols[index];
    }
}

internal class JpegHuffmanTableWriter
{
    internal byte[] WritePayload(JpegHuffmanTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        List<JpegHuffmanTable> tables = new List<JpegHuffmanTable>();
        tables.Add(table);
        return WritePayloads(tables);
    }

    internal byte[] WritePayloads(List<JpegHuffmanTable> tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        JpegByteWriter writer = new JpegByteWriter();
        foreach (JpegHuffmanTable table in tables)
        {
            WriteTable(writer, table);
        }

        return writer.ToArray();
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, List<JpegHuffmanTable> tables)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayloads(tables);
        segmentWriter.WriteMarkerSegment(JpegMarker.DefineHuffmanTable, payload);
    }

    internal void WriteTable(JpegByteWriter writer, JpegHuffmanTable table)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        ValidateTable(table);
        int symbolCount = CountSymbols(table);
        int tableInfo = (table.TableClass << 4) + table.Identifier;
        writer.WriteByte(tableInfo);

        int index = 0;
        while (index < 16)
        {
            writer.WriteByte(table.CodeCounts[index]);
            index = index + 1;
        }

        index = 0;
        while (index < symbolCount)
        {
            ValidateSymbolValue(table.Symbols[index]);
            writer.WriteByte(table.Symbols[index]);
            index = index + 1;
        }
    }

    internal void ValidateTable(JpegHuffmanTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.TableClass != 0 && table.TableClass != 1)
        {
            throw JpegWriteValidationException.Create("DHT table class must be DC or AC.");
        }

        if (table.Identifier < 0 || table.Identifier > 3)
        {
            throw JpegWriteValidationException.Create("DHT table identifier must be between 0 and 3.");
        }

        if (table.CodeCounts == null || table.CodeCounts.Length < 16)
        {
            throw JpegWriteValidationException.Create("DHT code-count array must contain 16 entries.");
        }

        if (table.Symbols == null)
        {
            throw JpegWriteValidationException.Create("DHT symbol array cannot be null.");
        }

        int symbolCount = CountSymbols(table);
        if (table.SymbolCount != 0 && table.SymbolCount != symbolCount)
        {
            throw JpegWriteValidationException.Create("DHT symbol count must match the sum of code counts.");
        }

        if (table.Symbols.Length < symbolCount)
        {
            throw JpegWriteValidationException.Create("DHT symbol array must contain every counted symbol.");
        }

        ValidateCodeTree(table);
    }

    internal int CountSymbols(JpegHuffmanTable table)
    {
        int symbolCount = 0;
        int index = 0;
        while (index < 16)
        {
            int count = table.CodeCounts[index];
            if (count < 0 || count > 255)
            {
                throw JpegWriteValidationException.Create("DHT code counts must fit in one byte.");
            }

            symbolCount = symbolCount + count;
            if (symbolCount > 256)
            {
                throw JpegWriteValidationException.Create("DHT cannot contain more than 256 symbols.");
            }

            index = index + 1;
        }

        return symbolCount;
    }

    internal void ValidateCodeTree(JpegHuffmanTable table)
    {
        int code = 0;
        int length = 1;
        while (length <= 16)
        {
            int count = table.CodeCounts[length - 1];
            if (code + count > (1 << length))
            {
                throw JpegWriteValidationException.Create("DHT code counts describe an over-subscribed Huffman tree.");
            }

            code = (code + count) << 1;
            length = length + 1;
        }
    }

    internal void ValidateSymbolValue(int value)
    {
        if (value < 0 || value > 255)
        {
            throw JpegWriteValidationException.Create("DHT symbols must fit in one byte.");
        }
    }
}
