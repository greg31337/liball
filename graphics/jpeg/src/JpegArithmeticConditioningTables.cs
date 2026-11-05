using System.Collections.Generic;

namespace jpeg;

internal class JpegArithmeticConditioningTableParser
{
    internal List<JpegArithmeticConditioningTable> Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if ((payload.Length & 1) != 0)
        {
            throw JpegInvalidDataException.Create("DAC payload must contain two bytes for each conditioning table.");
        }

        List<JpegArithmeticConditioningTable> tables = new List<JpegArithmeticConditioningTable>();
        int offset = 0;
        while (offset < payload.Length)
        {
            int tableInfo = payload[offset];
            int conditioningValue = payload[offset + 1];
            JpegArithmeticConditioningTable table = CreateTable(tableInfo, conditioningValue);
            tables.Add(table);
            offset = offset + 2;
        }

        return tables;
    }

    internal JpegArithmeticConditioningTable CreateTable(int tableInfo, int conditioningValue)
    {
        int tableClass = tableInfo >> 4;
        int identifier = tableInfo & 15;
        if (tableClass != 0 && tableClass != 1)
        {
            throw JpegInvalidDataException.Create("DAC table class must be DC/lossless or AC.");
        }

        if (identifier > 3)
        {
            throw JpegInvalidDataException.Create("DAC table identifier must be between 0 and 3.");
        }

        JpegArithmeticConditioningTable table = new JpegArithmeticConditioningTable();
        table.TableClass = tableClass;
        table.Identifier = identifier;
        table.ConditioningValue = conditioningValue;

        if (tableClass == 0)
        {
            ParseDcConditioningValue(table, conditioningValue);
        }
        else
        {
            ParseAcConditioningValue(table, conditioningValue);
        }

        return table;
    }

    internal void ParseDcConditioningValue(JpegArithmeticConditioningTable table, int conditioningValue)
    {
        int lower = conditioningValue & 15;
        int upper = conditioningValue >> 4;
        if (lower > upper)
        {
            throw JpegInvalidDataException.Create("DAC DC conditioning lower bound cannot exceed upper bound.");
        }

        table.Lower = lower;
        table.Upper = upper;
        table.Kx = 0;
    }

    internal void ParseAcConditioningValue(JpegArithmeticConditioningTable table, int conditioningValue)
    {
        if (conditioningValue < 1 || conditioningValue > 63)
        {
            throw JpegInvalidDataException.Create("DAC AC conditioning value must be between 1 and 63.");
        }

        table.Lower = 0;
        table.Upper = 0;
        table.Kx = conditioningValue;
    }
}

internal class JpegArithmeticConditioningTableWriter
{
    internal byte[] WritePayload(JpegArithmeticConditioningTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        List<JpegArithmeticConditioningTable> tables = new List<JpegArithmeticConditioningTable>();
        tables.Add(table);
        return WritePayloads(tables);
    }

    internal byte[] WritePayloads(List<JpegArithmeticConditioningTable> tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        JpegByteWriter writer = new JpegByteWriter();
        foreach (JpegArithmeticConditioningTable table in tables)
        {
            WriteTable(writer, table);
        }

        return writer.ToArray();
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, List<JpegArithmeticConditioningTable> tables)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayloads(tables);
        segmentWriter.WriteMarkerSegment(JpegMarker.DefineArithmeticConditioning, payload);
    }

    internal void WriteTable(JpegByteWriter writer, JpegArithmeticConditioningTable table)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        ValidateTable(table);
        writer.WriteByte((table.TableClass << 4) + table.Identifier);
        writer.WriteByte(GetConditioningValue(table));
    }

    internal void ValidateTable(JpegArithmeticConditioningTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (table.TableClass != 0 && table.TableClass != 1)
        {
            throw JpegWriteValidationException.Create("DAC table class must be DC/lossless or AC.");
        }

        if (table.Identifier < 0 || table.Identifier > 3)
        {
            throw JpegWriteValidationException.Create("DAC table identifier must be between 0 and 3.");
        }

        if (table.ConditioningValue < 0 || table.ConditioningValue > 255)
        {
            throw JpegWriteValidationException.Create("DAC conditioning value must fit in one byte.");
        }

        if (table.TableClass == 0)
        {
            ValidateDcTable(table);
        }
        else
        {
            ValidateAcTable(table);
        }
    }

    internal void ValidateDcTable(JpegArithmeticConditioningTable table)
    {
        int lower = table.Lower;
        int upper = table.Upper;
        if (table.ConditioningValue != 0 && lower == 0 && upper == 0)
        {
            lower = table.ConditioningValue & 15;
            upper = table.ConditioningValue >> 4;
        }

        if (lower < 0 || lower > 15 || upper < 0 || upper > 15)
        {
            throw JpegWriteValidationException.Create("DAC DC conditioning bounds must be between 0 and 15.");
        }

        if (lower > upper)
        {
            throw JpegWriteValidationException.Create("DAC DC conditioning lower bound cannot exceed upper bound.");
        }

        int value = (upper << 4) + lower;
        if (table.ConditioningValue != 0 && table.ConditioningValue != value)
        {
            throw JpegWriteValidationException.Create("DAC DC conditioning value must match lower and upper bounds.");
        }
    }

    internal void ValidateAcTable(JpegArithmeticConditioningTable table)
    {
        int kx = table.Kx;
        if (kx == 0 && table.ConditioningValue != 0)
        {
            kx = table.ConditioningValue;
        }

        if (kx < 1 || kx > 63)
        {
            throw JpegWriteValidationException.Create("DAC AC conditioning value must be between 1 and 63.");
        }

        if (table.ConditioningValue != 0 && table.ConditioningValue != kx)
        {
            throw JpegWriteValidationException.Create("DAC AC conditioning value must match Kx.");
        }
    }

    internal int GetConditioningValue(JpegArithmeticConditioningTable table)
    {
        if (table.TableClass == 0)
        {
            if (table.ConditioningValue != 0 && table.Lower == 0 && table.Upper == 0)
            {
                return table.ConditioningValue;
            }

            return (table.Upper << 4) + table.Lower;
        }

        if (table.Kx == 0 && table.ConditioningValue != 0)
        {
            return table.ConditioningValue;
        }

        return table.Kx;
    }
}
