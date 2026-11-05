using System;

namespace sql;

/// <summary>Contains SQLite type-affinity entries for a script and its resolved catalog data.</summary>
public class SqliteTypeAffinityResult
{
    /// <summary>The unchanged script that was analyzed.</summary>
    public SqlScript Script;
    /// <summary>The catalog used to resolve columns and functions.</summary>
    public SqlCatalog Catalog;
    /// <summary>The affinity entries in analysis order.</summary>
    public SqliteTypeAffinityEntry[] Entries;

    /// <summary>Initializes a SQLite type-affinity result.</summary>
    /// <param name="script">The analyzed script.</param>
    /// <param name="catalog">The catalog used for resolution.</param>
    /// <param name="entries">The affinity entries.</param>
    /// <exception cref="ArgumentNullException">script, catalog, entries, or an entry is <c>null</c>.</exception>
    public SqliteTypeAffinityResult(SqlScript script, SqlCatalog catalog, SqliteTypeAffinityEntry[] entries)
    {
        Script = script ?? throw new ArgumentNullException("script");
        Catalog = catalog ?? throw new ArgumentNullException("catalog");
        ValidateEntries(entries);
        Entries = entries;
    }

    /// <summary>Returns whether at least one analyzed target has unknown affinity.</summary>
    /// <returns><c>true</c> when an unknown affinity is present; otherwise, <c>false</c>.</returns>
    internal bool HasUnknownAffinity()
    {
        int index = 0;
        while (index < Entries.Length)
        {
            if (Entries[index].Affinity == SqliteTypeAffinityKind.Unknown)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateEntries(SqliteTypeAffinityEntry[] entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index] == null)
            {
                throw new ArgumentNullException("entries");
            }

            index = index + 1;
        }
    }
}
