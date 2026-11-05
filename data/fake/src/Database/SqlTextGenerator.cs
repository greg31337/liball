using System;
using System.Text;

namespace fake;

internal static class SqlTextGenerator
{
    internal static string ConnectionString(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateConnectionData(context);
        Guard.OutputSize(
            MaximumLength(SqlTextData.ConnectionStrings),
            context.CapturedOptions.MaximumOutputSize,
            "connectionString");
        return SqlTextData.ConnectionStrings[
            random.IntFromMaximumExclusive(SqlTextData.ConnectionStrings.Length)];
    }

    internal static string ConnectionString(
        GenerationContext context,
        DatabaseConnectionKind kind)
    {
        ValidateConnectionKind(kind);
        ValidateConnectionData(context);
        string value = SqlTextData.ConnectionStrings[(int)kind];
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "connectionString");
        return value;
    }

    internal static string QuoteIdentifier(
        GenerationContext context,
        string value)
    {
        int length = QuotedIdentifierLength(context, value, "value");
        Guard.OutputSize(
            length,
            context.CapturedOptions.MaximumOutputSize,
            "sqlIdentifier");
        return Quote(value, '"');
    }

    internal static string StringLiteral(
        GenerationContext context,
        string value)
    {
        int length = StringLiteralLength(context, value, "value");
        Guard.OutputSize(
            length,
            context.CapturedOptions.MaximumOutputSize,
            "sqlStringLiteral");
        return Quote(value, '\'');
    }

    internal static string Insert(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateInsertData(context);
        int maximum = 0;
        int index = 0;
        while (index < SqlTextData.InsertTables.Length)
        {
            int length = InsertLength(
                context,
                SqlTextData.InsertTables[index],
                new string[] { SqlTextData.InsertColumns[index] },
                new string[] { SqlTextData.InsertValues[index] });
            if (length > maximum)
            {
                maximum = length;
            }
            index = index + 1;
        }
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "insert");
        index = random.IntFromMaximumExclusive(SqlTextData.InsertTables.Length);
        return InsertUnchecked(
            SqlTextData.InsertTables[index],
            new string[] { SqlTextData.InsertColumns[index] },
            new string[] { SqlTextData.InsertValues[index] });
    }

    internal static string Insert(
        GenerationContext context,
        string table,
        string column,
        string value)
    {
        Guard.NotNull(column, "column");
        Guard.NotNull(value, "value");
        return Insert(
            context,
            table,
            new string[] { column },
            new string[] { value });
    }

    internal static string Insert(
        GenerationContext context,
        string table,
        string[] columns,
        string[] values)
    {
        int length = InsertLength(context, table, columns, values);
        Guard.OutputSize(
            length,
            context.CapturedOptions.MaximumOutputSize,
            "insert");
        return InsertUnchecked(table, columns, values);
    }

    static string InsertUnchecked(
        string table,
        string[] columns,
        string[] values)
    {
        StringBuilder result = new StringBuilder();
        result.Append("INSERT INTO ");
        result.Append(Quote(table, '"'));
        result.Append(" (");
        int index = 0;
        while (index < columns.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }
            result.Append(Quote(columns[index], '"'));
            index = index + 1;
        }
        result.Append(") VALUES (");
        index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }
            result.Append(Quote(values[index], '\''));
            index = index + 1;
        }
        result.Append(");");
        return result.ToString();
    }

    static int InsertLength(
        GenerationContext context,
        string table,
        string[] columns,
        string[] values)
    {
        Guard.NotNull(table, "table");
        Guard.NotNull(columns, "columns");
        Guard.NotNull(values, "values");
        Guard.Count(
            columns.Length,
            context.CapturedOptions.MaximumCount,
            "columns");
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            "values");
        if (columns.Length == 0)
        {
            throw new ArgumentException(
                "columns must contain at least one value.",
                "columns");
        }
        if (columns.Length != values.Length)
        {
            throw new ArgumentException(
                "columns and values must contain the same number of items.",
                "values");
        }
        long length = "INSERT INTO ".Length +
            (long)QuotedIdentifierLength(context, table, "table") +
            " (".Length +
            ") VALUES (".Length + ");".Length;
        int index = 0;
        while (index < columns.Length)
        {
            if (columns[index] == null)
            {
                throw new ArgumentNullException(
                    "columns",
                    "columns cannot contain null items.");
            }
            if (values[index] == null)
            {
                throw new ArgumentNullException(
                    "values",
                    "values cannot contain null items.");
            }
            length = length + QuotedIdentifierLength(
                context,
                columns[index],
                "columns") + StringLiteralLength(
                    context,
                    values[index],
                    "values");
            if (index > 0)
            {
                length = length + 4L;
            }
            index = index + 1;
        }
        return Guard.OutputSize(
            length,
            context.CapturedOptions.MaximumOutputSize,
            "insert");
    }

    static int QuotedIdentifierLength(
        GenerationContext context,
        string value,
        string parameterName)
    {
        Guard.NotNull(value, parameterName);
        Guard.Count(
            value.Length,
            context.CapturedOptions.MaximumCount,
            parameterName);
        UnicodeText.Validate(value, parameterName);
        if (value.Length == 0)
        {
            throw new ArgumentException(
                "SQL identifiers cannot be empty.",
                parameterName);
        }
        int escapes = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\0' || character == '\r' || character == '\n')
            {
                throw new ArgumentException(
                    "SQL identifiers cannot contain null or newline characters.",
                    parameterName);
            }
            if (character == '"')
            {
                escapes = escapes + 1;
            }
            index = index + 1;
        }
        return value.Length + escapes + 2;
    }

    static int StringLiteralLength(
        GenerationContext context,
        string value,
        string parameterName)
    {
        Guard.NotNull(value, parameterName);
        Guard.Count(
            value.Length,
            context.CapturedOptions.MaximumCount,
            parameterName);
        UnicodeText.Validate(value, parameterName);
        int escapes = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\0')
            {
                throw new ArgumentException(
                    "SQL string literals cannot contain a null character.",
                    parameterName);
            }
            if (value[index] == '\'')
            {
                escapes = escapes + 1;
            }
            index = index + 1;
        }
        return value.Length + escapes + 2;
    }

    static string Quote(string value, char delimiter)
    {
        StringBuilder result = new StringBuilder();
        result.Append(delimiter);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            result.Append(character);
            if (character == delimiter)
            {
                result.Append(character);
            }
            index = index + 1;
        }
        result.Append(delimiter);
        return result.ToString();
    }

    static void ValidateConnectionData(GenerationContext context)
    {
        Guard.Count(
            SqlTextData.ConnectionStrings.Length,
            context.CapturedOptions.MaximumCount,
            "connectionStringData");
        int index = 0;
        while (index < SqlTextData.ConnectionStrings.Length)
        {
            string value = SqlTextData.ConnectionStrings[index];
            if (value.Length == 0 || value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0)
            {
                throw FakeException.Create(
                    "The project-authored connection-string table is malformed.");
            }
            index = index + 1;
        }
    }

    static void ValidateInsertData(GenerationContext context)
    {
        if (SqlTextData.InsertTables.Length != SqlTextData.InsertColumns.Length ||
            SqlTextData.InsertTables.Length != SqlTextData.InsertValues.Length)
        {
            throw FakeException.Create(
                "The project-authored SQL insert table has inconsistent column lengths.");
        }
        Guard.Count(
            SqlTextData.InsertTables.Length,
            context.CapturedOptions.MaximumCount,
            "insertData");
    }

    static void ValidateConnectionKind(DatabaseConnectionKind kind)
    {
        switch (kind)
        {
            case DatabaseConnectionKind.PostgreSql:
            case DatabaseConnectionKind.MySql:
            case DatabaseConnectionKind.SqlServer:
            case DatabaseConnectionKind.Sqlite:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported connection-string category.");
        }
    }

    static int MaximumLength(string[] values)
    {
        int result = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > result)
            {
                result = values[index].Length;
            }
            index = index + 1;
        }
        return result;
    }
}
