using System;

namespace sql;

/// <summary>Controls format behavior.</summary>
public class SqlFormatOptions
{
    /// <summary>The keyword case associated with this value.</summary>
    public SqlKeywordCase KeywordCase;
    /// <summary>The casing applied to unquoted identifiers.</summary>
    public SqlIdentifierCase IdentifierCase;
    /// <summary>The indent text associated with this value.</summary>
    public string IndentText;
    /// <summary>The comma style associated with this value.</summary>
    public SqlCommaStyle CommaStyle;
    /// <summary>The policy for legal trailing commas.</summary>
    public SqlTrailingCommaPolicy TrailingCommaPolicy;
    /// <summary>The preferred output layout mode.</summary>
    public SqlFormatMode Mode;
    /// <summary>The maximum preferred line width, or zero for no width limit.</summary>
    public int TargetLineWidth;
    /// <summary>Controls whether comments is retained in the result.</summary>
    public bool PreserveComments;
    /// <summary>The keyword dialect associated with this value.</summary>
    public SqlDialectOptions KeywordDialect;
    /// <summary>The dialect used to parse raw SQL supplied to the formatter.</summary>
    public SqlDialectOptions ReadDialect;
    /// <summary>The dialect targeted by formatted output.</summary>
    public SqlDialectOptions WriteDialect;

    /// <summary>Initializes a new instance of <see cref="SqlFormatOptions"/> from the supplied values.</summary>
    public SqlFormatOptions()
    {
        KeywordCase = SqlKeywordCase.Preserve;
        IdentifierCase = SqlIdentifierCase.Preserve;
        IndentText = "    ";
        CommaStyle = SqlCommaStyle.Inline;
        TrailingCommaPolicy = SqlTrailingCommaPolicy.Never;
        Mode = SqlFormatMode.Pretty;
        TargetLineWidth = 0;
        PreserveComments = false;
        KeywordDialect = SqliteDialect.CreateOptions();
        ReadDialect = SqliteDialect.CreateOptions();
        WriteDialect = SqliteDialect.CreateOptions();
    }

    /// <summary>Creates a new value initialized with the library's default settings.</summary>
    /// <returns>The resulting format options.</returns>
    internal static SqlFormatOptions CreateDefault()
    {
        return new SqlFormatOptions();
    }

    /// <summary>Creates an independent copy of this value.</summary>
    /// <returns>The resulting format options.</returns>
    internal SqlFormatOptions Copy()
    {
        SqlFormatOptions copy = new SqlFormatOptions();
        copy.KeywordCase = KeywordCase;
        copy.IdentifierCase = IdentifierCase;
        copy.IndentText = IndentText;
        copy.CommaStyle = CommaStyle;
        copy.TrailingCommaPolicy = TrailingCommaPolicy;
        copy.Mode = Mode;
        copy.TargetLineWidth = TargetLineWidth;
        copy.PreserveComments = PreserveComments;
        copy.KeywordDialect = KeywordDialect;
        copy.ReadDialect = ReadDialect;
        copy.WriteDialect = WriteDialect;
        return copy;
    }

    /// <summary>Checks that indentation and line-width settings are within their supported ranges.</summary>
    /// <exception cref="ArgumentNullException">IndentText is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">KeywordDialect is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException">ReadDialect or WriteDialect is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">KeywordCase is outside the supported range or otherwise invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">IdentifierCase, trailing-comma policy, mode, or target line width is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">CommaStyle is outside the supported range or otherwise invalid.</exception>
    internal void Validate()
    {
        if (IndentText == null)
        {
            throw new ArgumentNullException("IndentText");
        }

        if (KeywordDialect == null)
        {
            throw new ArgumentNullException("KeywordDialect");
        }

        if (ReadDialect == null)
        {
            throw new ArgumentNullException("ReadDialect");
        }

        if (WriteDialect == null)
        {
            throw new ArgumentNullException("WriteDialect");
        }

        if (KeywordCase != SqlKeywordCase.Preserve && KeywordCase != SqlKeywordCase.Upper && KeywordCase != SqlKeywordCase.Lower)
        {
            throw new ArgumentOutOfRangeException("KeywordCase");
        }

        if (IdentifierCase != SqlIdentifierCase.Preserve && IdentifierCase != SqlIdentifierCase.Upper && IdentifierCase != SqlIdentifierCase.Lower)
        {
            throw new ArgumentOutOfRangeException("IdentifierCase");
        }

        if (CommaStyle != SqlCommaStyle.Inline && CommaStyle != SqlCommaStyle.Leading)
        {
            throw new ArgumentOutOfRangeException("CommaStyle");
        }

        if (TrailingCommaPolicy != SqlTrailingCommaPolicy.Never && TrailingCommaPolicy != SqlTrailingCommaPolicy.WhenSupported)
        {
            throw new ArgumentOutOfRangeException("TrailingCommaPolicy");
        }

        if (Mode != SqlFormatMode.Pretty && Mode != SqlFormatMode.Compact && Mode != SqlFormatMode.Minimal && Mode != SqlFormatMode.RoundTrip)
        {
            throw new ArgumentOutOfRangeException("Mode");
        }

        if (TargetLineWidth < 0)
        {
            throw new ArgumentOutOfRangeException("TargetLineWidth");
        }

        KeywordDialect.Validate();
        ReadDialect.Validate();
        WriteDialect.Validate();
    }
}
