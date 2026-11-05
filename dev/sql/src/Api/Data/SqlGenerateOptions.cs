using System;

namespace sql;

/// <summary>Controls generate behavior.</summary>
public class SqlGenerateOptions
{
    /// <summary>The format options associated with this value.</summary>
    public SqlFormatOptions FormatOptions = SqlFormatOptions.CreateDefault();
    /// <summary>The dialect targeted by generated SQL, independently of any dialect used to parse the tree.</summary>
    public SqlDialectOptions WriteDialect = GenericDialect.CreateOptions();
    /// <summary>The include semicolons associated with this value.</summary>
    public bool IncludeSemicolons = true;
    /// <summary>Controls whether a parsed script is emitted from its preserved source tokens.</summary>
    public bool PreserveSourceText = false;

    /// <summary>Initializes a new instance of <see cref="SqlGenerateOptions"/> from the supplied values.</summary>
    public SqlGenerateOptions()
    {
    }

    /// <summary>Creates a new value initialized with the library's default settings.</summary>
    /// <returns>The resulting generate options.</returns>
    internal static SqlGenerateOptions CreateDefault()
    {
        return new SqlGenerateOptions();
    }

    /// <summary>Creates an independent copy of this value.</summary>
    /// <returns>The resulting generate options.</returns>
    internal SqlGenerateOptions Copy()
    {
        SqlGenerateOptions copy = new SqlGenerateOptions();
        copy.FormatOptions = FormatOptions.Copy();
        copy.WriteDialect = WriteDialect;
        copy.IncludeSemicolons = IncludeSemicolons;
        copy.PreserveSourceText = PreserveSourceText;
        return copy;
    }

    /// <summary>Checks that the nested formatting and write-dialect options are present and valid.</summary>
    /// <exception cref="ArgumentNullException">FormatOptions is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">WriteDialect is <c>null</c>.</exception>
    internal void Validate()
    {
        if (FormatOptions == null)
        {
            throw new ArgumentNullException("FormatOptions");
        }

        FormatOptions.Validate();
        if (WriteDialect == null)
        {
            throw new ArgumentNullException("WriteDialect");
        }

        WriteDialect.Validate();
    }
}
