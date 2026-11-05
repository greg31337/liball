using System;

namespace sql;

/// <summary>Contains the validation output and any diagnostics produced by the operation.</summary>
public class SqlValidationResult
{
    /// <summary>The script associated with this value.</summary>
    public SqlScript Script;
    /// <summary>The catalog associated with this value.</summary>
    public SqlCatalog Catalog;
    /// <summary>The options used to produce this value.</summary>
    public SqlValidationOptions Options;
    /// <summary>The diagnostics in reporting order.</summary>
    public SqlDiagnosticBag Diagnostics;

    /// <summary>Initializes a new instance of <see cref="SqlValidationResult"/> from the supplied values.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <param name="catalog">The catalog used to resolve SQL names.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="diagnostics">The diagnostic collection produced during processing.</param>
    /// <exception cref="ArgumentNullException">script is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">catalog is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">options is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">diagnostics is <c>null</c>.</exception>
    public SqlValidationResult(SqlScript script, SqlCatalog catalog, SqlValidationOptions options, SqlDiagnosticBag diagnostics)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (catalog == null)
        {
            throw new ArgumentNullException("catalog");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        options.Validate();
        Script = script;
        Catalog = catalog;
        Options = options.Copy();
        Diagnostics = diagnostics;
    }

    /// <summary>Returns whether processing completed without error diagnostics.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsSuccess()
    {
        return !Diagnostics.HasErrors();
    }

    /// <summary>Returns whether diagnostics is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasDiagnostics()
    {
        return !Diagnostics.IsEmpty();
    }

    /// <summary>Returns whether errors is present.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasErrors()
    {
        return Diagnostics.HasErrors();
    }

    /// <summary>Returns the number of diagnostic items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetDiagnosticCount()
    {
        return Diagnostics.GetCount();
    }

    /// <summary>Returns the item at the specified zero-based index.</summary>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>The resulting diagnostic.</returns>
    internal SqlDiagnostic GetDiagnosticAt(int index)
    {
        return Diagnostics.GetAt(index);
    }
}
