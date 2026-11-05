using System;

namespace sql;

/// <summary>Controls validation behavior.</summary>
public class SqlValidationOptions
{
    /// <summary>The validate table names associated with this value.</summary>
    public bool ValidateTableNames;
    /// <summary>The validate column names associated with this value.</summary>
    public bool ValidateColumnNames;
    /// <summary>The report ambiguous columns associated with this value.</summary>
    public bool ReportAmbiguousColumns;
    /// <summary>Controls validation of function names and arity.</summary>
    public bool ValidateFunctionNames;
    /// <summary>Controls validation of collation names.</summary>
    public bool ValidateCollationNames;
    /// <summary>Controls validation of index references.</summary>
    public bool ValidateIndexNames;
    /// <summary>Controls reporting of duplicate relation and common table expression aliases.</summary>
    public bool ReportDuplicateAliases;

    /// <summary>Initializes a new instance of <see cref="SqlValidationOptions"/> from the supplied values.</summary>
    public SqlValidationOptions()
    {
        ValidateTableNames = true;
        ValidateColumnNames = true;
        ReportAmbiguousColumns = true;
        ValidateFunctionNames = true;
        ValidateCollationNames = true;
        ValidateIndexNames = true;
        ReportDuplicateAliases = true;
    }

    /// <summary>Creates a new value initialized with the library's default settings.</summary>
    /// <returns>The resulting validation options.</returns>
    internal static SqlValidationOptions CreateDefault()
    {
        return new SqlValidationOptions();
    }

    /// <summary>Creates an independent copy of this value.</summary>
    /// <returns>The resulting validation options.</returns>
    internal SqlValidationOptions Copy()
    {
        SqlValidationOptions copy = new SqlValidationOptions();
        copy.ValidateTableNames = ValidateTableNames;
        copy.ValidateColumnNames = ValidateColumnNames;
        copy.ReportAmbiguousColumns = ReportAmbiguousColumns;
        copy.ValidateFunctionNames = ValidateFunctionNames;
        copy.ValidateCollationNames = ValidateCollationNames;
        copy.ValidateIndexNames = ValidateIndexNames;
        copy.ReportDuplicateAliases = ReportDuplicateAliases;
        return copy;
    }

    /// <summary>Checks that validation limits are within their supported ranges.</summary>
    internal void Validate()
    {
    }
}
