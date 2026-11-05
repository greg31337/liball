using System;

namespace sql;

/// <summary>Contains target-dialect compatibility issues and diagnostics for a script.</summary>
public class SqlCompatibilityResult
{
    /// <summary>The unchanged script that was analyzed.</summary>
    public SqlScript Script;
    /// <summary>The immutable target-dialect policy used by the analysis.</summary>
    public ISqlDialectPolicy TargetDialect;
    /// <summary>The compatibility issues in source order.</summary>
    public SqlCompatibilityIssue[] Issues;
    /// <summary>The diagnostics in source order.</summary>
    public SqlDiagnosticBag Diagnostics;

    /// <summary>Initializes a target-dialect compatibility result.</summary>
    /// <param name="script">The script that was analyzed.</param>
    /// <param name="targetDialect">The target-dialect policy.</param>
    /// <param name="issues">The compatibility issues.</param>
    /// <param name="diagnostics">The diagnostic collection.</param>
    /// <exception cref="ArgumentNullException">script, targetDialect, issues, diagnostics, or an issue is <c>null</c>.</exception>
    public SqlCompatibilityResult(SqlScript script, ISqlDialectPolicy targetDialect, SqlCompatibilityIssue[] issues, SqlDiagnosticBag diagnostics)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (targetDialect == null)
        {
            throw new ArgumentNullException("targetDialect");
        }

        ValidateIssues(issues);
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        Script = script;
        TargetDialect = targetDialect;
        Issues = issues;
        Diagnostics = diagnostics;
    }

    /// <summary>Returns whether the script uses only syntax supported by the target dialect.</summary>
    /// <returns><c>true</c> when no compatibility issues were found; otherwise, <c>false</c>.</returns>
    internal bool IsCompatible()
    {
        return Issues.Length == 0;
    }

    /// <summary>Returns the number of compatibility issues.</summary>
    /// <returns>The requested count.</returns>
    internal int GetIssueCount()
    {
        return Issues.Length;
    }

    /// <summary>Returns the compatibility issue at a zero-based index.</summary>
    /// <param name="index">The zero-based index of the issue.</param>
    /// <returns>The requested compatibility issue.</returns>
    internal SqlCompatibilityIssue GetIssueAt(int index)
    {
        return Issues[index];
    }

    internal static void ValidateIssues(SqlCompatibilityIssue[] issues)
    {
        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        int index = 0;
        while (index < issues.Length)
        {
            if (issues[index] == null)
            {
                throw new ArgumentNullException("issues");
            }

            index = index + 1;
        }
    }
}
