using System.Collections.Generic;

namespace jpeg;

internal class JpegQuantizationTableParser
{
    internal List<JpegQuantizationTable> Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        List<JpegQuantizationTable> tables = new List<JpegQuantizationTable>();
        int offset = 0;
        while (offset < payload.Length)
        {
            int tableInfo = payload[offset];
            offset = offset + 1;
            JpegQuantizationTable table = ParseTable(payload, offset, tableInfo);
            tables.Add(table);
            if (table.Is16Bit)
            {
                offset = offset + 128;
            }
            else
            {
                offset = offset + 64;
            }
        }

        return tables;
    }

    internal JpegQuantizationTable ParseTable(byte[] payload, int valueOffset, int tableInfo)
    {
        int precision = tableInfo >> 4;
        int identifier = tableInfo & 15;
        if (identifier > 3)
        {
            throw JpegInvalidDataException.Create("DQT table identifier must be between 0 and 3.");
        }

        if (precision != 0 && precision != 1)
        {
            throw JpegInvalidDataException.Create("DQT precision must be 8-bit or 16-bit.");
        }

        int tableBytes = 64;
        if (precision == 1)
        {
            tableBytes = 128;
        }

        if (tableBytes > payload.Length - valueOffset)
        {
            throw JpegInvalidDataException.Create("DQT payload ended before a complete quantization table.");
        }

        JpegQuantizationTable table = new JpegQuantizationTable();
        table.Identifier = identifier;
        table.Is16Bit = precision == 1;
        if (table.Is16Bit)
        {
            table.Precision = 16;
        }
        else
        {
            table.Precision = 8;
        }

        int index = 0;
        int offset = valueOffset;
        while (index < 64)
        {
            int value;
            if (table.Is16Bit)
            {
                value = JpegByteReader.ReadUInt16(payload, offset);
                offset = offset + 2;
            }
            else
            {
                value = payload[offset];
                offset = offset + 1;
            }

            table.ZigZagValues[index] = value;
            table.Values[JpegConstants.ZigZagOrder[index]] = value;
            index = index + 1;
        }

        return table;
    }
}

internal class JpegQuantizationTableWriter
{
    internal byte[] WritePayload(JpegQuantizationTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        List<JpegQuantizationTable> tables = new List<JpegQuantizationTable>();
        tables.Add(table);
        return WritePayloads(tables);
    }

    internal byte[] WritePayloads(List<JpegQuantizationTable> tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        JpegByteWriter writer = new JpegByteWriter();
        foreach (JpegQuantizationTable table in tables)
        {
            WriteTable(writer, table);
        }

        return writer.ToArray();
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, List<JpegQuantizationTable> tables)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayloads(tables);
        segmentWriter.WriteMarkerSegment(JpegMarker.DefineQuantizationTable, payload);
    }

    internal void WriteTable(JpegByteWriter writer, JpegQuantizationTable table)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        ValidateTable(table);
        bool use16Bit = Uses16BitPrecision(table);
        bool useZigZagValues = HasZigZagValues(table);
        int tableInfo = table.Identifier;
        if (use16Bit)
        {
            tableInfo = tableInfo + 16;
        }

        writer.WriteByte(tableInfo);
        int index = 0;
        while (index < 64)
        {
            int value = GetTableValue(table, index, useZigZagValues);
            ValidateTableValue(value, use16Bit);
            if (use16Bit)
            {
                writer.WriteUInt16(value);
            }
            else
            {
                writer.WriteByte(value);
            }

            index = index + 1;
        }
    }

    internal void ValidateTable(JpegQuantizationTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.Identifier < 0 || table.Identifier > 3)
        {
            throw JpegWriteValidationException.Create("DQT table identifier must be between 0 and 3.");
        }

        if (table.Values == null || table.Values.Length < 64)
        {
            throw JpegWriteValidationException.Create("DQT natural-order value array must contain 64 entries.");
        }

        if (table.ZigZagValues == null || table.ZigZagValues.Length < 64)
        {
            throw JpegWriteValidationException.Create("DQT zig-zag value array must contain 64 entries.");
        }

        if (table.Precision != 0 && table.Precision != 8 && table.Precision != 16)
        {
            throw JpegWriteValidationException.Create("DQT precision must be 8-bit or 16-bit.");
        }
    }

    internal bool Uses16BitPrecision(JpegQuantizationTable table)
    {
        if (table.Precision == 16)
        {
            return true;
        }

        if (table.Precision == 8)
        {
            return false;
        }

        return table.Is16Bit;
    }

    internal bool HasZigZagValues(JpegQuantizationTable table)
    {
        int index = 0;
        while (index < 64)
        {
            if (table.ZigZagValues[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int GetTableValue(JpegQuantizationTable table, int zigZagIndex, bool useZigZagValues)
    {
        if (useZigZagValues)
        {
            return table.ZigZagValues[zigZagIndex];
        }

        return table.Values[JpegConstants.ZigZagOrder[zigZagIndex]];
    }

    internal void ValidateTableValue(int value, bool use16Bit)
    {
        if (value <= 0)
        {
            throw JpegWriteValidationException.Create("DQT values must be positive.");
        }

        if (use16Bit)
        {
            if (value > 65535)
            {
                throw JpegWriteValidationException.Create("16-bit DQT values cannot exceed 65535.");
            }

            return;
        }

        if (value > 255)
        {
            throw JpegWriteValidationException.Create("8-bit DQT values cannot exceed 255.");
        }
    }
}
