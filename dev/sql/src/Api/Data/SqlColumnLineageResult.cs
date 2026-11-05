using System;

namespace sql;

/// <summary>Contains column-lineage mappings and diagnostics for an analyzed script.</summary>
public class SqlColumnLineageResult
{
    /// <summary>The unchanged script that was analyzed.</summary>
    public SqlScript Script;
    /// <summary>The catalog used to resolve source and target columns.</summary>
    public SqlCatalog Catalog;
    /// <summary>The output and DML lineage entries in analysis order.</summary>
    public SqlColumnLineageEntry[] Entries;
    /// <summary>The diagnostics for unknown and ambiguous origins.</summary>
    public SqlDiagnosticBag Diagnostics;

    /// <summary>Initializes a column-lineage result.</summary>
    /// <param name="script">The analyzed script.</param>
    /// <param name="catalog">The catalog used for resolution.</param>
    /// <param name="entries">The lineage entries.</param>
    /// <param name="diagnostics">The lineage diagnostics.</param>
    /// <exception cref="ArgumentNullException">script, catalog, entries, diagnostics, or an entry is <c>null</c>.</exception>
    public SqlColumnLineageResult(SqlScript script, SqlCatalog catalog, SqlColumnLineageEntry[] entries, SqlDiagnosticBag diagnostics)
    {
        Script = script ?? throw new ArgumentNullException("script");
        Catalog = catalog ?? throw new ArgumentNullException("catalog");
        ValidateEntries(entries);
        Diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
        Entries = entries;
    }

    /// <summary>Returns whether every lineage target has a non-ambiguous known origin.</summary>
    /// <returns><c>true</c> when all origins are known and unambiguous; otherwise, <c>false</c>.</returns>
    internal bool IsComplete()
    {
        int index = 0;
        while (index < Entries.Length)
        {
            if (Entries[index].OriginKind == SqlColumnLineageOriginKind.Unknown || Entries[index].OriginKind == SqlColumnLineageOriginKind.Ambiguous)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void ValidateEntries(SqlColumnLineageEntry[] entries)
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
