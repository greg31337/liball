using System;

namespace fake;

internal static class DatabaseGenerator
{
    internal static string DatabaseName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateIdentifierData(
            context,
            DatabaseData.DatabaseNames,
            "databaseNameData");
        return SelectValue(
            context,
            random,
            DatabaseData.DatabaseNames,
            "databaseName");
    }

    internal static string SchemaName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateIdentifierData(
            context,
            DatabaseData.SchemaNames,
            "schemaNameData");
        return SelectValue(
            context,
            random,
            DatabaseData.SchemaNames,
            "schemaName");
    }

    internal static string TableName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateIdentifierData(
            context,
            DatabaseData.TableNames,
            "tableNameData");
        return SelectValue(
            context,
            random,
            DatabaseData.TableNames,
            "tableName");
    }

    internal static string ColumnName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateIdentifierData(
            context,
            DatabaseData.ColumnNames,
            "columnNameData");
        return SelectValue(
            context,
            random,
            DatabaseData.ColumnNames,
            "columnName");
    }

    internal static string IndexName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTableAndColumnData(context);
        int maximum = 3 + MaximumLength(DatabaseData.TableNames) +
            1 + MaximumLength(DatabaseData.ColumnNames);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "indexName");
        return "ix_" + DatabaseData.TableNames[
            random.IntFromMaximumExclusive(DatabaseData.TableNames.Length)] + "_" +
            DatabaseData.ColumnNames[
                random.IntFromMaximumExclusive(DatabaseData.ColumnNames.Length)];
    }

    internal static string ConstraintName(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTableAndColumnData(context);
        Guard.OutputSize(
            MaximumConstraintLength(),
            context.CapturedOptions.MaximumOutputSize,
            "constraintName");
        DatabaseConstraintKind kind = (DatabaseConstraintKind)random.IntFromMaximumExclusive(4);
        return ConstraintNameUnchecked(random, kind);
    }

    internal static string ConstraintName(
        GenerationContext context,
        FakeRandom random,
        DatabaseConstraintKind kind)
    {
        ValidateConstraintKind(kind);
        ValidateTableAndColumnData(context);
        int maximum = ConstraintMaximumLength(kind);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "constraintName");
        return ConstraintNameUnchecked(random, kind);
    }

    internal static string Type(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateScalarData(
            context,
            DatabaseData.SqlTypes,
            "sqlTypeData");
        return SelectValue(
            context,
            random,
            DatabaseData.SqlTypes,
            "sqlType");
    }

    internal static string Collation(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateScalarData(
            context,
            DatabaseData.Collations,
            "collationData");
        return SelectValue(
            context,
            random,
            DatabaseData.Collations,
            "collation");
    }

    internal static string Engine(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateScalarData(
            context,
            DatabaseData.Engines,
            "engineData");
        return SelectValue(
            context,
            random,
            DatabaseData.Engines,
            "engine");
    }

    internal static string SqlIdentifier(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateIdentifierData(
            context,
            DatabaseData.DatabaseNames,
            "databaseNameData");
        ValidateIdentifierData(
            context,
            DatabaseData.SchemaNames,
            "schemaNameData");
        ValidateIdentifierData(
            context,
            DatabaseData.TableNames,
            "tableNameData");
        ValidateIdentifierData(
            context,
            DatabaseData.ColumnNames,
            "columnNameData");
        int maximum = MaximumIdentifierLength();
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            "sqlIdentifier");
        int category = random.IntFromMaximumExclusive(4);
        if (category == 0)
        {
            return DatabaseData.DatabaseNames[
                random.IntFromMaximumExclusive(DatabaseData.DatabaseNames.Length)];
        }
        if (category == 1)
        {
            return DatabaseData.SchemaNames[
                random.IntFromMaximumExclusive(DatabaseData.SchemaNames.Length)];
        }
        if (category == 2)
        {
            return DatabaseData.TableNames[
                random.IntFromMaximumExclusive(DatabaseData.TableNames.Length)];
        }
        return DatabaseData.ColumnNames[
            random.IntFromMaximumExclusive(DatabaseData.ColumnNames.Length)];
    }

    internal static bool IsSqlIdentifier(
        GenerationContext context,
        string value)
    {
        Guard.NotNull(value, "value");
        Guard.Count(
            value.Length,
            context.CapturedOptions.MaximumCount,
            "value");
        return ValidIdentifier(value);
    }

    static string ConstraintNameUnchecked(
        FakeRandom random,
        DatabaseConstraintKind kind)
    {
        string table = DatabaseData.TableNames[
            random.IntFromMaximumExclusive(DatabaseData.TableNames.Length)];
        if (kind == DatabaseConstraintKind.PrimaryKey)
        {
            return "pk_" + table;
        }
        string column = DatabaseData.ColumnNames[
            random.IntFromMaximumExclusive(DatabaseData.ColumnNames.Length)];
        if (kind == DatabaseConstraintKind.ForeignKey)
        {
            return "fk_" + table + "_" + column;
        }
        if (kind == DatabaseConstraintKind.Unique)
        {
            return "uq_" + table + "_" + column;
        }
        return "ck_" + table + "_" + column;
    }

    static void ValidateTableAndColumnData(GenerationContext context)
    {
        ValidateIdentifierData(
            context,
            DatabaseData.TableNames,
            "tableNameData");
        ValidateIdentifierData(
            context,
            DatabaseData.ColumnNames,
            "columnNameData");
    }

    static void ValidateIdentifierData(
        GenerationContext context,
        string[] values,
        string operation)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            operation);
        int index = 0;
        while (index < values.Length)
        {
            if (!ValidIdentifier(values[index]))
            {
                throw FakeException.Create(
                    "The project-authored " + operation + " table contains an invalid or reserved SQL identifier.");
            }
            index = index + 1;
        }
    }

    static void ValidateScalarData(
        GenerationContext context,
        string[] values,
        string operation)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            operation);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length == 0 ||
                values[index].IndexOf('\r') >= 0 ||
                values[index].IndexOf('\n') >= 0)
            {
                throw FakeException.Create(
                    "The project-authored " + operation + " table is malformed.");
            }
            index = index + 1;
        }
    }

    static string SelectValue(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string operation)
    {
        int maximum = MaximumLength(values);
        Guard.OutputSize(
            maximum,
            context.CapturedOptions.MaximumOutputSize,
            operation);
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static bool ValidIdentifier(string value)
    {
        if (value.Length == 0 || !AsciiLetter(value[0]) && value[0] != '_')
        {
            return false;
        }
        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!AsciiLetter(character) && !AsciiDigit(character) &&
                character != '_')
            {
                return false;
            }
            index = index + 1;
        }
        index = 0;
        while (index < DatabaseData.ReservedWords.Length)
        {
            if (string.Equals(
                value,
                DatabaseData.ReservedWords[index],
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool AsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' ||
            value >= 'a' && value <= 'z';
    }

    static bool AsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static void ValidateConstraintKind(DatabaseConstraintKind kind)
    {
        switch (kind)
        {
            case DatabaseConstraintKind.PrimaryKey:
            case DatabaseConstraintKind.ForeignKey:
            case DatabaseConstraintKind.Unique:
            case DatabaseConstraintKind.Check:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported database constraint category.");
        }
    }

    static int ConstraintMaximumLength(DatabaseConstraintKind kind)
    {
        if (kind == DatabaseConstraintKind.PrimaryKey)
        {
            return 3 + MaximumLength(DatabaseData.TableNames);
        }
        return MaximumConstraintLength();
    }

    static int MaximumConstraintLength()
    {
        return 3 + MaximumLength(DatabaseData.TableNames) + 1 +
            MaximumLength(DatabaseData.ColumnNames);
    }

    static int MaximumIdentifierLength()
    {
        int result = MaximumLength(DatabaseData.DatabaseNames);
        int length = MaximumLength(DatabaseData.SchemaNames);
        if (length > result)
        {
            result = length;
        }
        length = MaximumLength(DatabaseData.TableNames);
        if (length > result)
        {
            result = length;
        }
        length = MaximumLength(DatabaseData.ColumnNames);
        if (length > result)
        {
            result = length;
        }
        return result;
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
