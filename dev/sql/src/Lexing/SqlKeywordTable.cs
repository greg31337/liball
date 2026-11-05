using System;
using System.Collections.Generic;

namespace sql;

internal class SqlKeywordTable
{
    internal Dictionary<string, string> Keywords;

    internal SqlKeywordTable()
    {
        Keywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    internal static SqlKeywordTable Create()
    {
        return new SqlKeywordTable();
    }

    internal static SqlKeywordTable Create(string[] keywords)
    {
        if (keywords == null)
        {
            throw new ArgumentNullException("keywords");
        }

        SqlKeywordTable table = new SqlKeywordTable();
        table.AddMany(keywords);
        return table;
    }

    internal static SqlKeywordTable CreateBase()
    {
        return Create(GetBaseKeywords());
    }

    internal static string[] GetBaseKeywords()
    {
        return new string[]
        {
            "ADD",
            "ALL",
            "ALTER",
            "AND",
            "AS",
            "ASC",
            "BETWEEN",
            "BY",
            "CASE",
            "CHECK",
            "COLLATE",
            "COLUMN",
            "COMMIT",
            "CONFLICT",
            "CONSTRAINT",
            "CREATE",
            "CROSS",
            "CURRENT_DATE",
            "CURRENT_TIME",
            "CURRENT_TIMESTAMP",
            "DEFAULT",
            "DELETE",
            "DESC",
            "DISTINCT",
            "DROP",
            "ELSE",
            "END",
            "EXCEPT",
            "EXISTS",
            "FALSE",
            "FROM",
            "FULL",
            "GROUP",
            "HAVING",
            "IF",
            "IN",
            "INDEX",
            "INNER",
            "INSERT",
            "INTERSECT",
            "INTO",
            "IS",
            "JOIN",
            "KEY",
            "LEFT",
            "LIKE",
            "LIMIT",
            "NOT",
            "NULL",
            "ON",
            "OR",
            "ORDER",
            "OUTER",
            "PRIMARY",
            "REFERENCES",
            "RIGHT",
            "ROLLBACK",
            "SELECT",
            "SET",
            "TABLE",
            "THEN",
            "TRUE",
            "UNION",
            "UNIQUE",
            "UPDATE",
            "VALUES",
            "WHEN",
            "WHERE"
        };
    }

    internal int GetCount()
    {
        return Keywords.Count;
    }

    internal string[] GetKeywords()
    {
        string[] keywords = new string[Keywords.Count];
        Keywords.Values.CopyTo(keywords, 0);
        Array.Sort(keywords, StringComparer.Ordinal);
        return keywords;
    }

    internal void Add(string keyword)
    {
        ValidateKeyword(keyword);
        Keywords[keyword] = keyword.ToUpperInvariant();
    }

    internal void AddMany(string[] keywords)
    {
        if (keywords == null)
        {
            throw new ArgumentNullException("keywords");
        }

        int index = 0;
        while (index < keywords.Length)
        {
            Add(keywords[index]);
            index = index + 1;
        }
    }

    internal bool ContainsKeyword(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return Keywords.ContainsKey(text);
    }

    internal string? GetCanonicalKeyword(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (Keywords.ContainsKey(text))
        {
            return Keywords[text];
        }

        return null;
    }

    internal static void ValidateKeyword(string keyword)
    {
        if (keyword == null)
        {
            throw new ArgumentNullException("keyword");
        }

        if (keyword.Length == 0)
        {
            throw new ArgumentException("keyword must not be empty");
        }

        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new ArgumentException("keyword must not be whitespace");
        }
    }
}
