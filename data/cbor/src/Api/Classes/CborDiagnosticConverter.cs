using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Converts between generic CBOR values and diagnostic notation.
/// </summary>
public class CborDiagnosticConverter : ICborDiagnosticConverter
{
    /// <summary>Creates a CBOR diagnostic converter.</summary>
    public CborDiagnosticConverter()
    {
    }

    /// <summary>Formats a value as CBOR diagnostic notation.</summary>
    /// <param name="value">The value to format.</param>
    /// <param name="options">The diagnostic notation options.</param>
    /// <returns>The formatted diagnostic notation.</returns>
    public string FormatDiagnostic(CborValue value, CborDiagnosticOptions options)
    {
        CborDiagnosticFormatterCore formatter = new CborDiagnosticFormatterCore(options);
        return formatter.Format(value);
    }

    /// <summary>Parses one value from CBOR diagnostic notation.</summary>
    /// <param name="text">The diagnostic notation text.</param>
    /// <param name="options">The diagnostic notation options.</param>
    /// <returns>The read outcome and parsed value.</returns>
    public CborReadResult ParseDiagnostic(string text, CborDiagnosticOptions options)
    {
        CborDiagnosticParserCore parser = new CborDiagnosticParserCore(text, options);
        return parser.Parse();
    }

    /// <summary>Formats zero or more values as a comma-separated diagnostic CBOR Sequence.</summary>
    /// <param name="values">The values in sequence order.</param>
    /// <param name="options">The diagnostic notation options.</param>
    /// <returns>The formatted diagnostic sequence.</returns>
    public string FormatDiagnosticSequence(
        List<CborValue> values,
        CborDiagnosticOptions options)
    {
        CborDiagnosticFormatterCore formatter = new CborDiagnosticFormatterCore(options);
        return formatter.FormatSequence(values);
    }

    /// <summary>Parses a comma-separated diagnostic CBOR Sequence.</summary>
    /// <param name="text">The diagnostic sequence text.</param>
    /// <param name="options">The diagnostic notation options.</param>
    /// <returns>One result per item, or a final error result.</returns>
    public List<CborReadResult> ParseDiagnosticSequence(
        string text,
        CborDiagnosticOptions options)
    {
        CborDiagnosticParserCore parser = new CborDiagnosticParserCore(text, options);
        return parser.ParseSequence();
    }
}
