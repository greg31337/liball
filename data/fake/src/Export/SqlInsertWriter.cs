using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace fake;

internal class SqlOutputBuffer
{
    StringBuilder Builder;
    int MaximumOutputSize;

    internal SqlOutputBuffer(int maximumOutputSize)
    {
        Builder = new StringBuilder();
        MaximumOutputSize = maximumOutputSize;
    }

    internal void Append(char value)
    {
        Guard.OutputSize(
            (long)Builder.Length + 1L,
            MaximumOutputSize,
            "sqlInsert");
        Builder.Append(value);
    }

    internal void AppendFromValue(string value)
    {
        Guard.OutputSize(
            (long)Builder.Length + value.Length,
            MaximumOutputSize,
            "sqlInsert");
        Builder.Append(value);
    }

    internal string Result()
    {
        return Builder.ToString();
    }
}

internal enum SqlRowKind
{
    Scalar = 0,
    Fields = 1,
    Dictionary = 2,
    Sequence = 3
}

internal class SqlColumn
{
    internal string Name = string.Empty;
    internal FieldInfo? Field;
    internal int Index;
}

internal class SqlShape
{
    internal SqlRowKind Kind;
    internal Type? RowType;
    internal SqlColumn[] Columns = new SqlColumn[0];
}

internal class SqlInsertWriter
{
    GenerationContext Context;
    SqlDialect Dialect;
    SqlOutputBuffer Output;

    internal SqlInsertWriter(
        GenerationContext context,
        SqlDialect dialect)
    {
        Context = context;
        Dialect = dialect;
        ValidateDialect();
        Output = new SqlOutputBuffer(
            context.CapturedOptions.MaximumOutputSize);
    }

    internal string Write(string table, object[] rows)
    {
        ValidateIdentifier(table, "table");
        Guard.NotNull(rows, "rows");
        Guard.Count(
            rows.Length,
            Context.CapturedOptions.MaximumCount,
            "rows");
        if (rows.Length == 0)
            return "";
        SqlShape shape = Shape(rows);
        Output.AppendFromValue("INSERT INTO ");
        WriteIdentifier(table);
        Output.AppendFromValue(" (");
        WriteColumns(shape);
        Output.AppendFromValue(") VALUES ");
        int index = 0;
        while (index < rows.Length)
        {
            if (index > 0)
                Output.AppendFromValue(", ");
            WriteRow(rows[index], shape, index);
            index = index + 1;
        }
        Output.Append(';');
        return Output.Result();
    }

    SqlShape Shape(object[] rows)
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
        if (SqlScalar(type))
            return ScalarShape(type);
        if (type.IsArray)
            return SequenceShape((Array)sample, type);
        if (type.GetInterface("System.Collections.IDictionary") != null)
            return DictionaryShape((IDictionary)sample, type);
        if (type.GetInterface("System.Collections.IList") != null)
            return SequenceShapeFromValuesAndType((IList)sample, type);
        return FieldShape(type);
    }

    SqlShape ScalarShape(Type? type)
    {
        SqlShape shape = new SqlShape();
        shape.Kind = SqlRowKind.Scalar;
        shape.RowType = type;
        shape.Columns = new SqlColumn[] { Column("Value", null, 0) };
        return shape;
    }

    SqlShape SequenceShape(Array values, Type type)
    {
        if (values.Rank != 1)
            throw FakeException.Create(
                "SQL export supports only one-dimensional row arrays.");
        return SequenceShapeFromCountAndType(values.Length, type);
    }

    SqlShape SequenceShapeFromValuesAndType(IList values, Type type)
    {
        return SequenceShapeFromCountAndType(values.Count, type);
    }

    SqlShape SequenceShapeFromCountAndType(int count, Type type)
    {
        Guard.Count(
            count,
            Context.CapturedOptions.MaximumCount,
            "columns");
        if (count == 0)
            throw FakeException.Create(
                "SQL rows must contain at least one column.");
        SqlShape shape = new SqlShape();
        shape.Kind = SqlRowKind.Sequence;
        shape.RowType = type;
        shape.Columns = new SqlColumn[count];
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

    SqlShape DictionaryShape(IDictionary values, Type type)
    {
        Guard.Count(
            values.Count,
            Context.CapturedOptions.MaximumCount,
            "columns");
        if (values.Count == 0)
            throw FakeException.Create(
                "SQL dictionary rows must contain at least one column.");
        SqlShape shape = new SqlShape();
        shape.Kind = SqlRowKind.Dictionary;
        shape.RowType = type;
        shape.Columns = new SqlColumn[values.Count];
        IDictionaryEnumerator enumerator = values.GetEnumerator();
        int index = 0;
        while (enumerator.MoveNext())
        {
            object key = enumerator.Key;
            if (key == null || Type.GetTypeCode(key.GetType()) != TypeCode.String)
                throw FakeException.Create(
                    "SQL dictionary column names must be strings.");
            string name = (string)key;
            ValidateIdentifier(name, "columns");
            shape.Columns[index] = Column(name, null, index);
            index = index + 1;
        }
        return shape;
    }

    SqlShape FieldShape(Type type)
    {
        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance | BindingFlags.Public);
        Guard.Count(
            fields.Length,
            Context.CapturedOptions.MaximumCount,
            "columns");
        if (fields.Length == 0)
            throw FakeException.Create(
                "SQL object rows must expose at least one public field.");
        SortFields(fields);
        SqlShape shape = new SqlShape();
        shape.Kind = SqlRowKind.Fields;
        shape.RowType = type;
        shape.Columns = new SqlColumn[fields.Length];
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

    void WriteColumns(SqlShape shape)
    {
        int index = 0;
        while (index < shape.Columns.Length)
        {
            if (index > 0)
                Output.AppendFromValue(", ");
            WriteIdentifier(shape.Columns[index].Name);
            index = index + 1;
        }
    }

    void WriteRow(object row, SqlShape shape, int rowIndex)
    {
        Output.Append('(');
        if (shape.Kind == SqlRowKind.Scalar)
        {
            WriteValue(row);
        }
        else if (row == null)
        {
            WriteNullRow(shape.Columns.Length);
        }
        else if (shape.Kind == SqlRowKind.Fields)
        {
            RequireRowType(row, shape, rowIndex);
            WriteFieldRow(row, shape);
        }
        else if (shape.Kind == SqlRowKind.Dictionary)
        {
            RequireRowType(row, shape, rowIndex);
            WriteDictionaryRow((IDictionary)row, shape, rowIndex);
        }
        else
        {
            RequireRowType(row, shape, rowIndex);
            if (row.GetType().IsArray)
                WriteArrayRow((Array)row, shape, rowIndex);
            else
                WriteListRow((IList)row, shape, rowIndex);
        }
        Output.Append(')');
    }

    void WriteFieldRow(object row, SqlShape shape)
    {
        int index = 0;
        while (index < shape.Columns.Length)
        {
            if (index > 0)
                Output.AppendFromValue(", ");
            FieldInfo? field = shape.Columns[index].Field;
            if (field == null)
                throw FakeException.Create("A SQL field column has no reflected field.");
            WriteValue(field.GetValue(row));
            index = index + 1;
        }
    }

    void WriteDictionaryRow(
        IDictionary row,
        SqlShape shape,
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
                Output.AppendFromValue(", ");
            string name = shape.Columns[index].Name;
            WriteValue(row.Contains(name) ? row[name] : null);
            index = index + 1;
        }
    }

    void WriteArrayRow(
        Array row,
        SqlShape shape,
        int rowIndex)
    {
        if (row.Rank != 1 || row.Length != shape.Columns.Length)
            InvalidRowWidth(rowIndex, shape.Columns.Length);
        int lowerBound = row.GetLowerBound(0);
        int index = 0;
        while (index < row.Length)
        {
            if (index > 0)
                Output.AppendFromValue(", ");
            WriteValue(row.GetValue(lowerBound + index));
            index = index + 1;
        }
    }

    void WriteListRow(
        IList row,
        SqlShape shape,
        int rowIndex)
    {
        if (row.Count != shape.Columns.Length)
            InvalidRowWidth(rowIndex, shape.Columns.Length);
        int index = 0;
        while (index < row.Count)
        {
            if (index > 0)
                Output.AppendFromValue(", ");
            WriteValue(row[index]);
            index = index + 1;
        }
    }

    void WriteNullRow(int count)
    {
        int index = 0;
        while (index < count)
        {
            if (index > 0)
                Output.AppendFromValue(", ");
            Output.AppendFromValue("NULL");
            index = index + 1;
        }
    }

    void WriteValue(object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            Output.AppendFromValue("NULL");
            return;
        }
        Type type = value.GetType();
        if (type.IsEnum)
        {
            WriteText(value.ToString() ?? string.Empty);
            return;
        }
        TypeCode code = Type.GetTypeCode(type);
        switch (code)
        {
            case TypeCode.Boolean:
                WriteBoolean((bool)value);
                return;
            case TypeCode.Char:
                WriteText(((char)value).ToString());
                return;
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Decimal:
                Output.AppendFromValue(Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture) ?? string.Empty);
                return;
            case TypeCode.Single:
                WriteSingle((float)value);
                return;
            case TypeCode.Double:
                WriteDouble((double)value);
                return;
            case TypeCode.DateTime:
                WriteText(((DateTime)value).ToString(
                    "O",
                    CultureInfo.InvariantCulture));
                return;
            case TypeCode.String:
                WriteText((string)value);
                return;
        }
        string? name = type.FullName;
        if (name == "System.DateTimeOffset")
            WriteText(((DateTimeOffset)value).ToString(
                "O",
                CultureInfo.InvariantCulture));
        else if (name == "System.DateOnly")
            WriteText(((DateOnly)value).ToString(
                "O",
                CultureInfo.InvariantCulture));
        else if (name == "System.TimeOnly")
            WriteText(((TimeOnly)value).ToString(
                "O",
                CultureInfo.InvariantCulture));
        else if (name == "System.TimeSpan")
            WriteText(((TimeSpan)value).ToString(
                "c",
                CultureInfo.InvariantCulture));
        else if (name == "System.Guid")
            WriteText(((Guid)value).ToString("D"));
        else if (name == "System.Uri")
            WriteText(((Uri)value).OriginalString);
        else if (name == "System.Byte[]")
            WriteBinary((byte[])value);
        else
            WriteText(new JsonObjectWriter(Context).Write(value));
    }

    void WriteBoolean(bool value)
    {
        if (Dialect == SqlDialect.Ansi ||
            Dialect == SqlDialect.PostgreSql)
            Output.AppendFromValue(value ? "TRUE" : "FALSE");
        else
            Output.AppendFromValue(value ? "1" : "0");
    }

    void WriteSingle(float value)
    {
        if (Single.IsNaN(value) || Single.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                "value",
                value,
                "SQL numbers must be finite.");
        Output.AppendFromValue(value.ToString("R", CultureInfo.InvariantCulture));
    }

    void WriteDouble(double value)
    {
        if (Double.IsNaN(value) || Double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                "value",
                value,
                "SQL numbers must be finite.");
        Output.AppendFromValue(value.ToString("R", CultureInfo.InvariantCulture));
    }

    void WriteText(string value)
    {
        Guard.NotNull(value, "value");
        Guard.Count(
            value.Length,
            Context.CapturedOptions.MaximumCount,
            "value");
        UnicodeText.Validate(value, "value");
        if (value.IndexOf('\0') >= 0)
            throw new ArgumentException(
                "SQL string literals cannot contain a null character.",
                "value");
        if (Dialect == SqlDialect.SqlServer)
            Output.Append('N');
        Output.Append('\'');
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (Dialect == SqlDialect.MySql && character == '\\')
                Output.AppendFromValue("\\\\");
            else
            {
                Output.Append(character);
                if (character == '\'')
                    Output.Append(character);
            }
            index = index + 1;
        }
        Output.Append('\'');
    }

    void WriteBinary(byte[] value)
    {
        Guard.Count(
            value.Length,
            Context.CapturedOptions.MaximumCount,
            "value");
        if (Dialect == SqlDialect.PostgreSql)
            Output.AppendFromValue("decode('");
        else if (Dialect == SqlDialect.SqlServer)
            Output.AppendFromValue("0x");
        else
            Output.AppendFromValue("X'");
        int index = 0;
        while (index < value.Length)
        {
            Output.AppendFromValue(value[index].ToString("X2"));
            index = index + 1;
        }
        if (Dialect == SqlDialect.PostgreSql)
            Output.AppendFromValue("', 'hex')");
        else if (Dialect != SqlDialect.SqlServer)
            Output.Append('\'');
    }

    void WriteIdentifier(string value)
    {
        char opening = '"';
        char closing = '"';
        if (Dialect == SqlDialect.MySql)
        {
            opening = '`';
            closing = '`';
        }
        else if (Dialect == SqlDialect.SqlServer)
        {
            opening = '[';
            closing = ']';
        }
        Output.Append(opening);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            Output.Append(character);
            if (character == closing)
                Output.Append(character);
            index = index + 1;
        }
        Output.Append(closing);
    }

    void ValidateIdentifier(string value, string parameterName)
    {
        Guard.NotNull(value, parameterName);
        Guard.Count(
            value.Length,
            Context.CapturedOptions.MaximumCount,
            parameterName);
        UnicodeText.Validate(value, parameterName);
        if (value.Length == 0)
            throw new ArgumentException(
                "SQL identifiers cannot be empty.",
                parameterName);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\0' || character == '\r' || character == '\n')
                throw new ArgumentException(
                    "SQL identifiers cannot contain null or newline characters.",
                    parameterName);
            index = index + 1;
        }
    }

    void RequireRowType(object row, SqlShape shape, int rowIndex)
    {
        if (row.GetType() != shape.RowType)
            throw FakeException.Create(
                "SQL row " + rowIndex +
                " does not match the first non-null row type.");
    }

    void ValidateDictionaryColumns(
        IDictionary row,
        SqlShape shape,
        int rowIndex)
    {
        IDictionaryEnumerator enumerator = row.GetEnumerator();
        while (enumerator.MoveNext())
        {
            object key = enumerator.Key;
            if (key == null || Type.GetTypeCode(key.GetType()) != TypeCode.String)
                throw FakeException.Create(
                    "SQL dictionary column names must be strings.");
            if (!HasColumn(shape, (string)key))
                throw FakeException.Create(
                    "SQL row " + rowIndex +
                    " contains unexpected column '" + key + "'.");
        }
    }

    static bool HasColumn(SqlShape shape, string name)
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

    static bool SqlScalar(Type type)
    {
        if (type.FullName == "System.Byte[]" ||
            type.FullName == "System.DBNull")
            return true;
        return CsvValueFormatter.IsScalar(type);
    }

    static SqlColumn Column(
        string name,
        FieldInfo? field,
        int index)
    {
        SqlColumn result = new SqlColumn();
        result.Name = name;
        result.Field = field;
        result.Index = index;
        return result;
    }

    static void InvalidRowWidth(int rowIndex, int expected)
    {
        throw FakeException.Create(
            "SQL row " + rowIndex + " must contain exactly " +
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

    void ValidateDialect()
    {
        switch (Dialect)
        {
            case SqlDialect.Ansi:
            case SqlDialect.PostgreSql:
            case SqlDialect.MySql:
            case SqlDialect.SqlServer:
            case SqlDialect.Sqlite:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "dialect",
                    Dialect,
                    "dialect is not supported.");
        }
    }
}
