using System;

namespace sql;

/// <summary>Contains path-based node comparisons between two SQL syntax trees.</summary>
public class SqlAstDiffResult
{
    /// <summary>The unchanged original script.</summary>
    public SqlScript OriginalScript;
    /// <summary>The unchanged modified script.</summary>
    public SqlScript ModifiedScript;
    /// <summary>The node comparisons in stable preorder.</summary>
    public SqlAstDiffEntry[] Entries;

    /// <summary>Initializes an AST diff result.</summary>
    /// <param name="originalScript">The original script.</param>
    /// <param name="modifiedScript">The modified script.</param>
    /// <param name="entries">The node comparisons.</param>
    /// <exception cref="ArgumentNullException">A script, entries, or an entry is <c>null</c>.</exception>
    public SqlAstDiffResult(SqlScript originalScript, SqlScript modifiedScript, SqlAstDiffEntry[] entries)
    {
        OriginalScript = originalScript ?? throw new ArgumentNullException("originalScript");
        ModifiedScript = modifiedScript ?? throw new ArgumentNullException("modifiedScript");
        ValidateEntries(entries);
        Entries = entries;
    }

    internal static void ValidateEntries(SqlAstDiffEntry[] entries)
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
