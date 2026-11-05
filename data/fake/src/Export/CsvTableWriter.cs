using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace fake;

internal class CsvOutputBuffer
{
    StringBuilder Builder;
    int MaximumOutputSize;

    internal CsvOutputBuffer(int maximumOutputSize)
    {
        Builder = new StringBuilder();
        MaximumOutputSize = maximumOutputSize;
    }

    internal void Append(char value)
    {
        Guard.OutputSize(
            (long)Builder.Length + 1L,
            MaximumOutputSize,
            "csv");
        Builder.Append(value);
    }

    internal void AppendFromValue(string value)
    {
        Guard.OutputSize(
            (long)Builder.Length + value.Length,
            MaximumOutputSize,
            "csv");
        Builder.Append(value);
    }

    internal string Result()
    {
        return Builder.ToString();
    }
}

internal static class CsvFieldWriter
{
    internal static string Escape(
        GenerationContext context,
        string value,
        char delimiter,
        bool guardSpreadsheetFormula)
    {
        Guard.NotNull(value, "value");
        UnicodeText.Validate(value, "value");
        CsvOutputBuffer output = new CsvOutputBuffer(
            context.CapturedOptions.MaximumOutputSize);
        Append(
            output,
            value,
            delimiter,
            guardSpreadsheetFormula);
        return output.Result();
    }

    internal static void Append(
        CsvOutputBuffer output,
        string value,
        char delimiter,
        bool guardSpreadsheetFormula)
    {
        bool prefix = guardSpreadsheetFormula && IsFormula(value);
        bool quote = RequiresQuotes(value, delimiter);
        if (quote)
            output.Append('"');
        if (prefix)
            output.Append('\'');
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            output.Append(character);
            if (character == '"')
                output.Append(character);
            index = index + 1;
        }
        if (quote)
            output.Append('"');
    }

    static bool IsFormula(string value)
    {
        int index = 0;
        while (index < value.Length && value[index] == ' ')
            index = index + 1;
        if (index == value.Length)
            return false;
        char character = value[index];
        return character == '=' || character == '+' || character == '-' ||
            character == '@' || character == '\t' || character == '\r' ||
            character == '\n';
    }

    static bool RequiresQuotes(string value, char delimiter)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == delimiter || character == '"' ||
                character == '\r' || character == '\n' ||
                character == '\u0085' || character == '\u2028' ||
                character == '\u2029')
                return true;
            index = index + 1;
        }
        return false;
    }
}

internal enum CsvRowKind
{
    Scalar = 0,
    Fields = 1,
    Dictionary = 2,
    Sequence = 3
}

internal class CsvColumn
{
    internal string Name = string.Empty;
    internal FieldInfo? Field;
    internal int Index;
}

internal class CsvShape
{
    internal CsvRowKind Kind;
    internal Type? RowType;
    internal CsvColumn[] Columns = new CsvColumn[0];
}

internal class CsvCell
{
    internal string? Text;
    internal bool GuardFormula;
}

internal class CsvTableWriter
{
    GenerationContext Context;
    CsvOptions Options;
    CsvOutputBuffer Output;

    internal CsvTableWriter(
        GenerationContext context,
        CsvOptions options)
    {
        Context = context;
        Options = options;
        ValidateOptions();
        Output = new CsvOutputBuffer(
            context.CapturedOptions.MaximumOutputSize);
    }

    internal string Write(object[] rows)
    {
        Guard.NotNull(rows, "rows");
        Guard.Count(
            rows.Length,
            Context.CapturedOptions.MaximumCount,
            "rows");
        if (rows.Length == 0)
            return "";
        CsvShape shape = Shape(rows);
        if (Options.IncludeHeader)
            WriteHeader(shape);
        int index = 0;
        while (index < rows.Length)
        {
            if (Options.IncludeHeader || index > 0)
                Output.AppendFromValue(Options.NewLine);
            WriteRow(rows[index], shape, index);
            index = index + 1;
        }
        return Output.Result();
    }

    void ValidateOptions()
    {
        if (Options == null)
            throw new ArgumentNullException("options");
        if (Options.Delimiter == '"' || Options.Delimiter == '\r' ||
            Options.Delimiter == '\n' ||
            (Char.IsControl(Options.Delimiter) && Options.Delimiter != '\t'))
            throw new ArgumentOutOfRangeException(
                "Delimiter",
                Options.Delimiter,
                "Delimiter must be one non-control character other than a quote or record separator.");
        if (Options.NewLine == null)
            throw new ArgumentNullException("NewLine");
        if (Options.NewLine != "\r\n" && Options.NewLine != "\n" &&
            Options.NewLine != "\r")
            throw new ArgumentException(
                "NewLine must be CRLF, LF, or CR.",
                "NewLine");
        if (Options.NullValue == null)
            throw new ArgumentNullException("NullValue");
        UnicodeText.Validate(Options.NullValue, "NullValue");
    }

    CsvShape Shape(object[] rows)
    {
        object? sample = null;
        int index = 0;
        while (index < rows.Length && sample == null)
        {
            sample = rows[index];
            index = index + 1;
        }
        if (sample == null)
            return ScalarShape(null);
        Type type = sample.GetType();
        if (CsvValueFormatter.IsScalar(type))
            return ScalarShape(type);
        if (type.IsArray)
            return SequenceShape((Array)sample, type);
        if (type.GetInterface("System.Collections.IDictionary") != null)
            return DictionaryShape((IDictionary)sample, type);
        if (type.GetInterface("System.Collections.IList") != null)
            return SequenceShapeFromValuesAndType((IList)sample, type);
        return FieldShape(type);
    }

    CsvShape ScalarShape(Type? type)
    {
        CsvShape shape = new CsvShape();
        shape.Kind = CsvRowKind.Scalar;
        shape.RowType = type;
        shape.Columns = new CsvColumn[] { Column("Value", null, 0) };
        return shape;
    }

    CsvShape SequenceShape(Array values, Type type)
    {
        if (values.Rank != 1)
            throw FakeException.Create(
                "CSV export supports only one-dimensional row arrays.");
        return SequenceShapeFromCountAndType(values.Length, type);
    }

    CsvShape SequenceShapeFromValuesAndType(IList values, Type type)
    {
        return SequenceShapeFromCountAndType(values.Count, type);
    }

    CsvShape SequenceShapeFromCountAndType(int count, Type type)
    {
        Guard.Count(
            count,
            Context.CapturedOptions.MaximumCount,
            "columns");
        if (count == 0)
            throw FakeException.Create(
                "CSV rows must contain at least one column.");
        CsvShape shape = new CsvShape();
        shape.Kind = CsvRowKind.Sequence;
        shape.RowType = type;
        shape.Columns = new CsvColumn[count];
        int index = 0;
        while (index < count)
        {
            shape.Columns[index] = Column(
                "Column" + (index + 1).ToString(
                    CultureInfo.InvariantCulture),
                null,
                index);
            index = index + 1;
        }
        return shape;
    }

    CsvShape DictionaryShape(IDictionary values, Type type)
    {
        Guard.Count(
            values.Count,
            Context.CapturedOptions.MaximumCount,
            "columns");
        if (values.Count == 0)
            throw FakeException.Create(
                "CSV dictionary rows must contain at least one column.");
        CsvShape shape = new CsvShape();
        shape.Kind = CsvRowKind.Dictionary;
        shape.RowType = type;
        shape.Columns = new CsvColumn[values.Count];
        IDictionaryEnumerator enumerator = values.GetEnumerator();
        int index = 0;
        while (enumerator.MoveNext())
        {
            object key = enumerator.Key;
            if (key == null || Type.GetTypeCode(key.GetType()) != TypeCode.String)
                throw FakeException.Create(
                    "CSV dictionary column names must be strings.");
            shape.Columns[index] = Column(
                (string)key,
                null,
                index);
            index = index + 1;
        }
        return shape;
    }

    CsvShape FieldShape(Type type)
    {
        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance | BindingFlags.Public);
        Guard.Count(
            fields.Length,
            Context.CapturedOptions.MaximumCount,
            "columns");
        if (fields.Length == 0)
            throw FakeException.Create(
                "CSV object rows must expose at least one public field.");
        SortFields(fields);
        CsvShape shape = new CsvShape();
        shape.Kind = CsvRowKind.Fields;
        shape.RowType = type;
        shape.Columns = new CsvColumn[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            shape.Columns[index] = Column(
                fields[index].Name,
                fields[index],
                index);
            index = index + 1;
        }
        return shape;
    }

    void WriteHeader(CsvShape shape)
    {
        int index = 0;
        while (index < shape.Columns.Length)
        {
            if (index > 0)
                Output.Append(Options.Delimiter);
            CsvFieldWriter.Append(
                Output,
                shape.Columns[index].Name,
                Options.Delimiter,
                Options.GuardSpreadsheetFormula);
            index = index + 1;
        }
    }

    void WriteRow(object row, CsvShape shape, int rowIndex)
    {
        if (shape.Kind == CsvRowKind.Scalar)
        {
            WriteCell(CsvValueFormatter.Format(Context, row));
            return;
        }
        if (row == null)
        {
            WriteNullRow(shape.Columns.Length);
            return;
        }
        if (shape.Kind == CsvRowKind.Fields)
        {
            RequireRowType(row, shape, rowIndex);
            WriteFieldRow(row, shape);
            return;
        }
        if (shape.Kind == CsvRowKind.Dictionary)
        {
            RequireRowType(row, shape, rowIndex);
            WriteDictionaryRow((IDictionary)row, shape, rowIndex);
            return;
        }
        RequireRowType(row, shape, rowIndex);
        if (row.GetType().IsArray)
            WriteArrayRow((Array)row, shape, rowIndex);
        else
            WriteListRow((IList)row, shape, rowIndex);
    }

    void WriteFieldRow(object row, CsvShape shape)
    {
        int index = 0;
        while (index < shape.Columns.Length)
        {
            if (index > 0)
                Output.Append(Options.Delimiter);
            FieldInfo? field = shape.Columns[index].Field;
            if (field == null)
                throw FakeException.Create("A CSV field column has no reflected field.");
            WriteCell(CsvValueFormatter.Format(Context, field.GetValue(row)));
            index = index + 1;
        }
    }

    void WriteDictionaryRow(
        IDictionary row,
        CsvShape shape,
        int rowIndex)
    {
        Guard.Count(
            row.Count,
            Context.CapturedOptions.MaximumCount,
            "columns");
        ValidateDictionaryColumns(row, shape, rowIndex);
        int index = 0;
        while (index < shape.Columns.Length)
        {
            if (index > 0)
                Output.Append(Options.Delimiter);
            string name = shape.Columns[index].Name;
            object? value = row.Contains(name) ? row[name] : null;
            WriteCell(CsvValueFormatter.Format(Context, value));
            index = index + 1;
        }
    }

    void WriteArrayRow(
        Array row,
        CsvShape shape,
        int rowIndex)
    {
        if (row.Rank != 1 || row.Length != shape.Columns.Length)
            InvalidRowWidth(rowIndex, shape.Columns.Length);
        int lowerBound = row.GetLowerBound(0);
        int index = 0;
        while (index < row.Length)
        {
            if (index > 0)
                Output.Append(Options.Delimiter);
            WriteCell(CsvValueFormatter.Format(
                Context,
                row.GetValue(lowerBound + index)));
            index = index + 1;
        }
    }

    void WriteListRow(
        IList row,
        CsvShape shape,
        int rowIndex)
    {
        if (row.Count != shape.Columns.Length)
            InvalidRowWidth(rowIndex, shape.Columns.Length);
        int index = 0;
        while (index < row.Count)
        {
            if (index > 0)
                Output.Append(Options.Delimiter);
            WriteCell(CsvValueFormatter.Format(Context, row[index]));
            index = index + 1;
        }
    }

    void WriteNullRow(int count)
    {
        int index = 0;
        while (index < count)
        {
            if (index > 0)
                Output.Append(Options.Delimiter);
            WriteCell(CsvValueFormatter.Null(Options.NullValue));
            index = index + 1;
        }
    }

    void WriteCell(CsvCell cell)
    {
        string? text = cell.Text;
        if (text == null)
            text = Options.NullValue;
        UnicodeText.Validate(text, "value");
        CsvFieldWriter.Append(
            Output,
            text,
            Options.Delimiter,
            Options.GuardSpreadsheetFormula && cell.GuardFormula);
    }

    void RequireRowType(object row, CsvShape shape, int rowIndex)
    {
        if (row.GetType() != shape.RowType)
            throw FakeException.Create(
                "CSV row " + rowIndex +
                " does not match the first non-null row type.");
    }

    void ValidateDictionaryColumns(
        IDictionary row,
        CsvShape shape,
        int rowIndex)
    {
        IDictionaryEnumerator enumerator = row.GetEnumerator();
        while (enumerator.MoveNext())
        {
            object key = enumerator.Key;
            if (key == null || Type.GetTypeCode(key.GetType()) != TypeCode.String)
                throw FakeException.Create(
                    "CSV dictionary column names must be strings.");
            if (!HasColumn(shape, (string)key))
                throw FakeException.Create(
                    "CSV row " + rowIndex +
                    " contains unexpected column '" + key + "'.");
        }
    }

    static bool HasColumn(CsvShape shape, string name)
    {
        int index = 0;
        while (index < shape.Columns.Length)
        {
            if (shape.Columns[index].Name == name)
                return true;
            index = index + 1;
        }
        return false;
    }

    static CsvColumn Column(
        string name,
        FieldInfo? field,
        int index)
    {
        CsvColumn result = new CsvColumn();
        result.Name = name;
        result.Field = field;
        result.Index = index;
        return result;
    }

    static void InvalidRowWidth(int rowIndex, int expected)
    {
        throw FakeException.Create(
            "CSV row " + rowIndex + " must contain exactly " +
            expected + " columns.");
    }

    static void SortFields(FieldInfo[] fields)
    {
        int index = 1;
        while (index < fields.Length)
        {
            FieldInfo selected = fields[index];
            int position = index;
            while (position > 0 && String.CompareOrdinal(
                fields[position - 1].Name,
                selected.Name) > 0)
            {
                fields[position] = fields[position - 1];
                position = position - 1;
            }
            fields[position] = selected;
            index = index + 1;
        }
    }
}

internal static class CsvValueFormatter
{
    internal static bool IsScalar(Type type)
    {
        if (type.IsEnum)
            return true;
        TypeCode code = Type.GetTypeCode(type);
        if (code != TypeCode.Object && code != TypeCode.Empty &&
            code != TypeCode.DBNull)
            return true;
        string? name = type.FullName;
        return name == "System.DateTimeOffset" ||
            name == "System.DateOnly" || name == "System.TimeOnly" ||
            name == "System.TimeSpan" || name == "System.Guid" ||
            name == "System.Uri";
    }

    internal static CsvCell Null(string? value)
    {
        CsvCell result = new CsvCell();
        result.Text = value;
        result.GuardFormula = true;
        return result;
    }

    internal static CsvCell Format(
        GenerationContext context,
        object? value)
    {
        if (value == null)
            return Null(null);
        CsvCell result = new CsvCell();
        Type type = value.GetType();
        if (type.IsEnum)
        {
            result.Text = value.ToString() ?? string.Empty;
            result.GuardFormula = false;
            return result;
        }
        TypeCode code = Type.GetTypeCode(type);
        switch (code)
        {
            case TypeCode.String:
                result.Text = (string)value;
                result.GuardFormula = true;
                return result;
            case TypeCode.Char:
                result.Text = ((char)value).ToString();
                result.GuardFormula = true;
                return result;
            case TypeCode.Boolean:
                result.Text = (bool)value ? "true" : "false";
                result.GuardFormula = false;
                return result;
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Decimal:
                result.Text = Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture) ?? string.Empty;
                result.GuardFormula = false;
                return result;
            case TypeCode.Single:
                result.Text = SingleValue((float)value);
                result.GuardFormula = false;
                return result;
            case TypeCode.Double:
                result.Text = DoubleValue((double)value);
                result.GuardFormula = false;
                return result;
            case TypeCode.DateTime:
                result.Text = ((DateTime)value).ToString(
                    "O",
                    CultureInfo.InvariantCulture);
                result.GuardFormula = false;
                return result;
        }
        string? name = type.FullName;
        if (name == "System.DateTimeOffset")
            result.Text = ((DateTimeOffset)value).ToString(
                "O",
                CultureInfo.InvariantCulture);
        else if (name == "System.DateOnly")
            result.Text = ((DateOnly)value).ToString(
                "O",
                CultureInfo.InvariantCulture);
        else if (name == "System.TimeOnly")
            result.Text = ((TimeOnly)value).ToString(
                "O",
                CultureInfo.InvariantCulture);
        else if (name == "System.TimeSpan")
            result.Text = ((TimeSpan)value).ToString(
                "c",
                CultureInfo.InvariantCulture);
        else if (name == "System.Guid")
            result.Text = ((Guid)value).ToString("D");
        else if (name == "System.Uri")
            result.Text = ((Uri)value).OriginalString;
        else
            result.Text = new JsonObjectWriter(context).Write(value);
        result.GuardFormula = false;
        return result;
    }

    static string SingleValue(float value)
    {
        if (Single.IsNaN(value) || Single.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                "value",
                value,
                "CSV numbers must be finite.");
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    static string DoubleValue(double value)
    {
        if (Double.IsNaN(value) || Double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                "value",
                value,
                "CSV numbers must be finite.");
        return value.ToString("R", CultureInfo.InvariantCulture);
    }
}
