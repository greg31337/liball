using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CurrencyCodes.DataGenerator;

internal sealed class CanonicalCSharpEmitter
{
    private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while"
    };

    internal string Emit(CanonicalCSharpFile file)
    {
        ValidateQualifiedName(file.NamespaceName, "namespace");
        ValidateIdentifier(file.ClassName, "class");
        List<CanonicalCSharpField> fields = new List<CanonicalCSharpField>(file.Fields);
        fields.Sort(CompareFields);
        List<CanonicalCSharpTable> tables = new List<CanonicalCSharpTable>(file.Tables);
        tables.Sort(CompareTables);
        ValidateUniqueMemberNames(fields, tables);

        StringBuilder builder = new StringBuilder();
        AppendLine(builder, "namespace " + file.NamespaceName + ";");
        AppendLine(builder, string.Empty);
        AppendLine(builder, "internal static class " + file.ClassName);
        AppendLine(builder, "{");
        for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            CanonicalCSharpField field = fields[fieldIndex];
            ValidateIdentifier(field.FieldName, "scalar field");
            ValidateTypeName(field.TypeName);
            string declaration = field.IsConstant ? "const " : "static readonly ";
            AppendLine(
                builder,
                "    internal "
                    + declaration
                    + field.TypeName
                    + " "
                    + field.FieldName
                    + " = "
                    + field.Value.Expression
                    + ";");
        }

        if (fields.Count > 0 && tables.Count > 0)
        {
            AppendLine(builder, string.Empty);
        }

        for (int tableIndex = 0; tableIndex < tables.Count; tableIndex++)
        {
            CanonicalCSharpTable table = tables[tableIndex];
            ValidateIdentifier(table.FieldName, "table field");
            ValidateTypeName(table.ElementTypeName);
            List<CanonicalCSharpRow> rows = new List<CanonicalCSharpRow>(table.Rows);
            rows.Sort(CompareRows);
            AppendLine(
                builder,
                "    internal static readonly "
                    + table.ElementTypeName
                    + "[] "
                    + table.FieldName
                    + " = new "
                    + table.ElementTypeName
                    + "[]");
            AppendLine(builder, "    {");
            foreach (CanonicalCSharpRow row in rows)
            {
                AppendLine(builder, "        " + FormatRow(table, row));
            }

            AppendLine(builder, "    };");
            if (tableIndex < tables.Count - 1)
            {
                AppendLine(builder, string.Empty);
            }
        }

        AppendLine(builder, "}");
        return builder.ToString();
    }

    private static int CompareFields(CanonicalCSharpField left, CanonicalCSharpField right)
    {
        return string.CompareOrdinal(left.FieldName, right.FieldName);
    }

    internal int WriteFile(string path, CanonicalCSharpFile file)
    {
        string content = Emit(file);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return Encoding.UTF8.GetByteCount(content);
    }

    private static int CompareTables(CanonicalCSharpTable left, CanonicalCSharpTable right)
    {
        return string.CompareOrdinal(left.FieldName, right.FieldName);
    }

    private static int CompareRows(CanonicalCSharpRow left, CanonicalCSharpRow right)
    {
        int comparison = string.CompareOrdinal(left.SortKey, right.SortKey);
        if (comparison != 0)
        {
            return comparison;
        }

        int sharedValueCount = Math.Min(left.Values.Count, right.Values.Count);
        for (int index = 0; index < sharedValueCount; index++)
        {
            comparison = string.CompareOrdinal(
                left.Values[index].Expression,
                right.Values[index].Expression);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return left.Values.Count.CompareTo(right.Values.Count);
    }

    private static string FormatRow(CanonicalCSharpTable table, CanonicalCSharpRow row)
    {
        if (!table.ConstructRows)
        {
            if (row.Values.Count != 1)
            {
                throw new InvalidDataException(
                    "Direct canonical C# table row for '"
                        + table.FieldName
                        + "' must contain exactly one value.");
            }

            return row.Values[0].Expression + ",";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("new ");
        builder.Append(table.ElementTypeName);
        builder.Append('(');
        for (int index = 0; index < row.Values.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(row.Values[index].Expression);
        }

        builder.Append("),");
        return builder.ToString();
    }

    private static void ValidateUniqueMemberNames(
        IReadOnlyList<CanonicalCSharpField> fields,
        IReadOnlyList<CanonicalCSharpTable> tables)
    {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        foreach (CanonicalCSharpField field in fields)
        {
            if (!names.Add(field.FieldName))
            {
                throw DuplicateMember(field.FieldName);
            }
        }

        foreach (CanonicalCSharpTable table in tables)
        {
            if (!names.Add(table.FieldName))
            {
                throw DuplicateMember(table.FieldName);
            }
        }
    }

    private static InvalidDataException DuplicateMember(string name)
    {
        return new InvalidDataException(
            "Canonical C# output contains duplicate member '" + name + "'.");
    }

    private static void ValidateQualifiedName(string value, string description)
    {
        string[] parts = value.Split('.');
        if (parts.Length == 0)
        {
            throw InvalidIdentifier(value, description);
        }

        foreach (string part in parts)
        {
            ValidateIdentifier(part, description);
        }
    }

    private static void ValidateIdentifier(string value, string description)
    {
        if (value.Length == 0 || !IsIdentifierStart(value[0]) || Keywords.Contains(value))
        {
            throw InvalidIdentifier(value, description);
        }

        for (int index = 1; index < value.Length; index++)
        {
            if (!IsIdentifierPart(value[index]))
            {
                throw InvalidIdentifier(value, description);
            }
        }
    }

    private static void ValidateTypeName(string value)
    {
        if (value == "bool"
            || value == "int"
            || value == "long"
            || value == "decimal"
            || value == "string")
        {
            return;
        }

        ValidateQualifiedName(value, "element type");
    }

    private static bool IsIdentifierStart(char value)
    {
        return value == '_' || (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
    }

    private static bool IsIdentifierPart(char value)
    {
        return IsIdentifierStart(value) || (value >= '0' && value <= '9');
    }

    private static InvalidDataException InvalidIdentifier(string value, string description)
    {
        return new InvalidDataException(
            "Canonical C# " + description + " name '" + value + "' is not a portable identifier.");
    }

    private static void AppendLine(StringBuilder builder, string value)
    {
        builder.Append(value);
        builder.Append('\n');
    }
}
