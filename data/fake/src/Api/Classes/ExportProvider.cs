using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides deterministic data export operations.
/// </summary>
public class ExportProvider : IExportProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal ExportProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("export");
    }

    /// <summary>Writes one valid JSON string value to a destination span.</summary>
    /// <param name="destination">The destination receiving the formatted value.</param>
    /// <param name="value">The well-formed Unicode string to escape.</param>
    /// <returns>The exact number of characters written.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when text is malformed or <c>destination</c> is too short.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public int WriteJsonString(Span<char> destination, string value)
    {
        return SpanDestinationWriter.Write(
            destination,
            StructuredTextGenerator.JsonString(Context, value));
    }

    /// <summary>Writes one JSON scalar value to a destination span.</summary>
    /// <param name="destination">The destination receiving the formatted value.</param>
    /// <param name="value">A null, Boolean, numeric, textual, enum, or temporal scalar.</param>
    /// <returns>The exact number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>value</c> is not a supported scalar, text is malformed, or <c>destination</c> is too short.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a number is non-finite or a configured limit is insufficient.</exception>
    public int WriteJsonScalar(Span<char> destination, object? value)
    {
        return SpanDestinationWriter.Write(
            destination,
            StructuredTextGenerator.JsonScalar(Context, value));
    }

    /// <summary>Writes one safely escaped RFC 4180 field to a destination span.</summary>
    /// <remarks>Formula-shaped fields receive a leading apostrophe.</remarks>
    /// <param name="destination">The destination receiving the escaped field.</param>
    /// <param name="value">The well-formed Unicode field value.</param>
    /// <returns>The exact number of characters written.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when text is malformed or <c>destination</c> is too short.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public int WriteCsvField(Span<char> destination, string value)
    {
        return SpanDestinationWriter.Write(
            destination,
            StructuredTextGenerator.CsvField(Context, value, true));
    }

    /// <summary>Writes one headerless RFC 4180 row to a destination span.</summary>
    /// <param name="destination">The destination receiving the row.</param>
    /// <param name="value">A public-field object, dictionary, sequence row, or scalar, including null.</param>
    /// <returns>The exact number of characters written.</returns>
    /// <exception cref="System.ArgumentException">Thrown when text is malformed or <c>destination</c> is too short.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when the row has no columns or has an unsupported shape.</exception>
    public int WriteCsvRow(Span<char> destination, object value)
    {
        return SpanDestinationWriter.Write(
            destination,
            StructuredTextGenerator.CsvRow(Context, value));
    }

    /// <summary>Writes one XML element with escaped text content to a destination span.</summary>
    /// <param name="destination">The destination receiving the element.</param>
    /// <param name="name">A valid XML element name.</param>
    /// <param name="value">The well-formed XML text content to escape.</param>
    /// <returns>The exact number of characters written.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when text is malformed or <c>destination</c> is too short.</exception>
    /// <exception cref="System.Xml.XmlException">Thrown when the name or value is not permitted by XML 1.0.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public int WriteXmlElement(Span<char> destination, string name, string value)
    {
        return SpanDestinationWriter.Write(
            destination,
            StructuredTextGenerator.XmlElement(Context, name, value));
    }

    /// <summary>Writes one escaped and folded RFC 6350 text-property line to a destination span.</summary>
    /// <param name="destination">The destination receiving the CRLF-terminated content line.</param>
    /// <param name="name">The ASCII letter, digit, and hyphen property name.</param>
    /// <param name="value">The well-formed Unicode text value.</param>
    /// <returns>The exact number of characters written.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a name or value is malformed or <c>destination</c> is too short.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public int WriteVCardLine(Span<char> destination, string name, string value)
    {
        return SpanDestinationWriter.Write(
            destination,
            StructuredTextGenerator.VCardLine(Context, name, value));
    }

    /// <summary>Writes one ANSI-delimited SQL identifier to a destination span.</summary>
    /// <param name="destination">The destination receiving the quoted identifier.</param>
    /// <param name="value">The nonempty single-line identifier text.</param>
    /// <returns>The exact number of characters written.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when text is malformed or <c>destination</c> is too short.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public int WriteSqlIdentifier(Span<char> destination, string value)
    {
        return SpanDestinationWriter.Write(
            destination,
            SqlTextGenerator.QuoteIdentifier(Context, value));
    }

    /// <summary>Writes one ANSI SQL string literal to a destination span.</summary>
    /// <param name="destination">The destination receiving the quoted literal.</param>
    /// <param name="value">The well-formed Unicode text to quote.</param>
    /// <returns>The exact number of characters written.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when text is malformed or <c>destination</c> is too short.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public int WriteSqlLiteral(Span<char> destination, string value)
    {
        return SpanDestinationWriter.Write(
            destination,
            SqlTextGenerator.StringLiteral(Context, value));
    }

    /// <summary>Writes one inert ANSI SQL insert statement to a destination span.</summary>
    /// <param name="destination">The destination receiving the statement.</param>
    /// <param name="table">The nonempty single-line table identifier.</param>
    /// <param name="value">The public-field object, dictionary, sequence row, or scalar to serialize, including null.</param>
    /// <returns>The exact number of characters written.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>table</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when text is malformed or <c>destination</c> is too short.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when the row has no columns or has an unsupported shape.</exception>
    public int WriteSqlInsert(Span<char> destination, string table, object value)
    {
        return SpanDestinationWriter.Write(
            destination,
            StructuredTextGenerator.SqlInsert(
                Context,
                table,
                new object[] { value },
                SqlDialect.Ansi));
    }

    /// <summary>Formats a string as one valid JSON string value.</summary>
    /// <param name="value">The well-formed Unicode string to escape.</param>
    /// <returns>An RFC 8259 string including surrounding quotation marks.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when value contains malformed UTF-16.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string JsonString(string value)
    {
        return StructuredTextGenerator.JsonString(Context, value);
    }

    /// <summary>Formats a signed integer as one valid JSON number.</summary>
    /// <param name="value">The integer value, including either signed 64-bit boundary.</param>
    /// <returns>A culture-invariant RFC 8259 number.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    public string JsonNumber(long value)
    {
        return StructuredTextGenerator.JsonNumber(Context, value);
    }

    /// <summary>Formats a finite floating-point value as one valid JSON number.</summary>
    /// <param name="value">A finite double-precision value.</param>
    /// <returns>A round-trip, culture-invariant RFC 8259 number.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when value is non-finite or the configured output limit is insufficient.</exception>
    public string JsonNumberFromValue(double value)
    {
        return StructuredTextGenerator.JsonNumber(Context, value);
    }

    /// <summary>Serializes a caller object, profile, schema result, collection, or scalar as compact JSON.</summary>
    /// <remarks>Public instance fields are emitted in ordinal name order. Dictionary and collection enumeration order is preserved. Numbers and temporal values use culture-invariant machine formats.</remarks>
    /// <param name="value">The value to serialize, including null.</param>
    /// <returns>A valid compact RFC 8259 JSON value.</returns>
    /// <exception cref="System.ArgumentException">Thrown when text contains malformed UTF-16.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a number is non-finite or configured count/output limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown for cycles, excessive depth, multidimensional arrays, or non-string dictionary keys.</exception>
    public string Json(object value)
    {
        return StructuredTextGenerator.JsonObject(Context, value);
    }

    /// <summary>Escapes one CSV field and guards spreadsheet-formula prefixes.</summary>
    /// <remarks>Formula-shaped fields receive a leading apostrophe. Commas, quotes, and record separators use RFC 4180 quoting.</remarks>
    /// <param name="value">The well-formed Unicode field value.</param>
    /// <returns>A safely escaped field without a record separator.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when value contains malformed UTF-16.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string CsvField(string value)
    {
        return StructuredTextGenerator.CsvField(Context, value, true);
    }

    /// <summary>Escapes one CSV field with an explicit spreadsheet-formula safety choice.</summary>
    /// <remarks>Disabling the guard returns inert text but may be unsafe if a spreadsheet application later opens it.</remarks>
    /// <param name="value">The well-formed Unicode field value.</param>
    /// <param name="guardSpreadsheetFormula">Whether to prefix formula-shaped values with an apostrophe.</param>
    /// <returns>An RFC 4180-escaped field without a record separator.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when value contains malformed UTF-16.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string CsvFieldFromValueAndGuardSpreadsheetFormula(string value, bool guardSpreadsheetFormula)
    {
        return StructuredTextGenerator.CsvField(Context, value, guardSpreadsheetFormula);
    }

    /// <summary>Serializes one public-field object, schema dictionary, sequence row, or scalar as CSV with a header.</summary>
    /// <param name="value">The row value, including null.</param>
    /// <returns>Compact RFC 4180 CSV using safe defaults.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured count/output limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when the row has no columns or contains an unsupported shape.</exception>
    public string Csv(object value)
    {
        return StructuredTextGenerator.CsvTable(Context, new object[] { value }, new CsvOptions());
    }

    /// <summary>Serializes one public-field object, schema dictionary, sequence row, or scalar with explicit CSV options.</summary>
    /// <param name="value">The row value, including null.</param>
    /// <param name="options">The CSV formatting and safety options.</param>
    /// <returns>Deterministic delimited text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>options</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an option is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when the row has no columns or contains an unsupported shape.</exception>
    public string CsvFromValueAndOptions(object value, CsvOptions options)
    {
        return StructuredTextGenerator.CsvTable(Context, new object[] { value }, options);
    }

    /// <summary>Serializes ordered rows as CSV using safe defaults.</summary>
    /// <remarks>The first non-null row defines columns. Public fields use ordinal name order, dictionary order is preserved, and later rows must match that shape.</remarks>
    /// <param name="rows">The ordered rows to serialize.</param>
    /// <returns>Compact RFC 4180 CSV with a header.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>rows</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when rows have inconsistent or unsupported shapes.</exception>
    public string CsvFromRows(object[] rows)
    {
        return StructuredTextGenerator.CsvTable(Context, rows, new CsvOptions());
    }

    /// <summary>Serializes ordered rows with explicit CSV options.</summary>
    /// <remarks>The first non-null row defines columns. Public fields use ordinal name order, dictionary order is preserved, and later rows must match that shape.</remarks>
    /// <param name="rows">The ordered rows to serialize.</param>
    /// <param name="options">The CSV formatting and safety options.</param>
    /// <returns>Deterministic delimited text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an option is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when rows have inconsistent or unsupported shapes.</exception>
    public string CsvFromRowsAndOptions(object[] rows, CsvOptions options)
    {
        return StructuredTextGenerator.CsvTable(Context, rows, options);
    }

    /// <summary>Formats one XML element with escaped text content.</summary>
    /// <param name="name">A valid XML element name.</param>
    /// <param name="value">The well-formed XML text content to escape.</param>
    /// <returns>A well-formed element containing the decoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument contains malformed UTF-16.</exception>
    /// <exception cref="System.Xml.XmlException">Thrown when name or value is not permitted by XML 1.0.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string XmlElement(string name, string value)
    {
        return StructuredTextGenerator.XmlElement(Context, name, value);
    }

    /// <summary>Serializes a caller object, profile, schema result, collection, or scalar as compact XML.</summary>
    /// <remarks>Public fields are emitted in ordinal name order. Dictionary and collection enumeration order is preserved. Numbers and temporal values use culture-invariant machine formats.</remarks>
    /// <param name="value">The value to serialize, including null.</param>
    /// <returns>A deterministic XML document with an inferred root element.</returns>
    /// <exception cref="System.ArgumentException">Thrown when text contains malformed UTF-16.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a number is non-finite or configured count/output limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown for cycles, excessive depth, multidimensional arrays, or non-string dictionary keys.</exception>
    public string Xml(object? value)
    {
        return StructuredTextGenerator.XmlObject(Context, value, null);
    }

    /// <summary>Serializes a caller object, profile, schema result, collection, or scalar as compact XML with an explicit root.</summary>
    /// <param name="rootName">The valid XML root element name.</param>
    /// <param name="value">The value to serialize, including null.</param>
    /// <returns>A deterministic XML document using the requested root element.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>rootName</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when text contains malformed UTF-16.</exception>
    /// <exception cref="System.Xml.XmlException">Thrown when <c>rootName</c> is not an XML name.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a number is non-finite or configured count/output limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown for cycles, excessive depth, multidimensional arrays, or non-string dictionary keys.</exception>
    public string XmlFromRootNameAndValue(string rootName, object value)
    {
        if (rootName == null)
            throw new ArgumentNullException("rootName");
        return StructuredTextGenerator.XmlObject(Context, value, rootName);
    }

    /// <summary>Exports a person profile as an RFC 6350 vCard.</summary>
    /// <remarks>Text values are escaped, physical lines use CRLF, and lines are folded at 75 UTF-8 octets.</remarks>
    /// <param name="profile">The person profile to export.</param>
    /// <returns>A version 4.0 individual vCard.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>profile</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when required or Unicode text is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is exceeded.</exception>
    public string VCard(PersonProfile profile)
    {
        return StructuredTextGenerator.VCard(Context, profile);
    }

    /// <summary>Exports a company profile as an RFC 6350 vCard.</summary>
    /// <remarks>Text values are escaped, structured address components are localized, physical lines use CRLF, and lines are folded at 75 UTF-8 octets.</remarks>
    /// <param name="profile">The company profile to export.</param>
    /// <returns>A version 4.0 organization vCard.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>profile</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when required or Unicode text is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is exceeded.</exception>
    public string VCardFromProfile(CompanyProfile profile)
    {
        return StructuredTextGenerator.VCard(Context, profile);
    }

    /// <summary>Generates valid JSON text with a scalar, object, or array at the top level.</summary>
    /// <returns>A valid RFC 8259 JSON value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string JsonFromNoArguments()
    {
        return StructuredTextGenerator.Json(Context, RandomValue);
    }

    /// <summary>Generates valid JSON text with the selected top-level shape.</summary>
    /// <param name="kind">The top-level JSON value shape.</param>
    /// <returns>A valid RFC 8259 JSON value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string JsonFromKind(JsonTextKind kind)
    {
        return StructuredTextGenerator.Json(Context, RandomValue, kind);
    }

    /// <summary>Generates explicitly malformed JSON text.</summary>
    /// <remarks>This method is separate from <see cref="JsonFromNoArguments()"/>, which always returns valid text.</remarks>
    /// <returns>JSON-shaped text containing one deliberate syntax defect.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string InvalidJson()
    {
        return StructuredTextGenerator.InvalidJson(Context, RandomValue);
    }

    /// <summary>Returns JSON-shaped text with the selected deliberate syntax defect.</summary>
    /// <param name="kind">The defect to place in the text.</param>
    /// <returns>Explicitly malformed JSON-shaped text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string InvalidJsonFromKind(StructuredTextInvalidKind kind)
    {
        return StructuredTextGenerator.InvalidJson(Context, kind);
    }

    /// <summary>Generates a valid CSV row or document.</summary>
    /// <returns>RFC 4180-shaped comma-separated text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string CsvFromNoArguments()
    {
        return StructuredTextGenerator.Csv(Context, RandomValue);
    }

    /// <summary>Generates valid CSV text with the selected shape.</summary>
    /// <param name="kind">Whether to generate a single row or a document.</param>
    /// <returns>RFC 4180-shaped comma-separated text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string CsvFromKind(CsvTextKind kind)
    {
        return StructuredTextGenerator.Csv(Context, RandomValue, kind);
    }

    /// <summary>Generates explicitly malformed CSV-shaped text.</summary>
    /// <remarks>This method is separate from <see cref="CsvFromNoArguments()"/>, which always returns valid text.</remarks>
    /// <returns>CSV-shaped text containing one deliberate structural defect.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string InvalidCsv()
    {
        return StructuredTextGenerator.InvalidCsv(Context, RandomValue);
    }

    /// <summary>Returns CSV-shaped text with the selected deliberate structural defect.</summary>
    /// <param name="kind">The defect to place in the text.</param>
    /// <returns>Explicitly malformed CSV-shaped text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string InvalidCsvFromKind(StructuredTextInvalidKind kind)
    {
        return StructuredTextGenerator.InvalidCsv(Context, kind);
    }

    /// <summary>Generates a valid XML element or document.</summary>
    /// <returns>Well-formed XML text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string XmlFromNoArguments()
    {
        return StructuredTextGenerator.Xml(Context, RandomValue);
    }

    /// <summary>Generates valid XML text with the selected shape.</summary>
    /// <param name="kind">Whether to generate an element or a complete document.</param>
    /// <returns>Well-formed XML text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string XmlFromKind(XmlTextKind kind)
    {
        return StructuredTextGenerator.Xml(Context, RandomValue, kind);
    }

    /// <summary>Generates explicitly malformed XML-shaped text.</summary>
    /// <remarks>This method is separate from <see cref="XmlFromNoArguments()"/>, which always returns well-formed text.</remarks>
    /// <returns>XML-shaped text containing one deliberate well-formedness defect.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string InvalidXml()
    {
        return StructuredTextGenerator.InvalidXml(Context, RandomValue);
    }

    /// <summary>Returns XML-shaped text with the selected deliberate well-formedness defect.</summary>
    /// <param name="kind">The defect to place in the text.</param>
    /// <returns>Explicitly malformed XML-shaped text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string InvalidXmlFromKind(StructuredTextInvalidKind kind)
    {
        return StructuredTextGenerator.InvalidXml(Context, kind);
    }

    /// <summary>Generates a valid YAML scalar, sequence, or mapping.</summary>
    /// <returns>YAML 1.2-shaped text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Yaml()
    {
        return StructuredTextGenerator.Yaml(Context, RandomValue);
    }

    /// <summary>Generates valid YAML text with the selected top-level shape.</summary>
    /// <param name="kind">The top-level YAML shape.</param>
    /// <returns>YAML 1.2-shaped text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string YamlFromKind(YamlTextKind kind)
    {
        return StructuredTextGenerator.Yaml(Context, RandomValue, kind);
    }

    /// <summary>Generates explicitly malformed YAML-shaped text.</summary>
    /// <remarks>This method is separate from <see cref="Yaml()"/>, which always returns valid fixtures.</remarks>
    /// <returns>YAML-shaped text containing one deliberate syntax defect.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string InvalidYaml()
    {
        return StructuredTextGenerator.InvalidYaml(Context, RandomValue);
    }

    /// <summary>Returns YAML-shaped text with the selected deliberate syntax defect.</summary>
    /// <param name="kind">The defect to place in the text.</param>
    /// <returns>Explicitly malformed YAML-shaped text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string InvalidYamlFromKind(StructuredTextInvalidKind kind)
    {
        return StructuredTextGenerator.InvalidYaml(Context, kind);
    }

    /// <summary>Generates an inert valid ANSI SQL insert statement.</summary>
    /// <remarks>The returned text uses escaped literals and identifiers and is never executed.</remarks>
    /// <returns>A valid SQL insert text fixture.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string SqlInsert()
    {
        return StructuredTextGenerator.SqlInsert(Context, RandomValue);
    }

    /// <summary>Exports one public-field object, schema dictionary, sequence row, or scalar as an inert ANSI SQL insert.</summary>
    /// <param name="table">The nonempty single-line table identifier.</param>
    /// <param name="value">The row value, including null.</param>
    /// <returns>A deterministic <c>INSERT</c> statement using ANSI quoting.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>table</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an identifier or text value is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when the row has no columns or contains an unsupported shape.</exception>
    public string SqlInsertFromTableAndValue(string table, object value)
    {
        return StructuredTextGenerator.SqlInsert(Context, table, new object[] { value }, SqlDialect.Ansi);
    }

    /// <summary>Exports one public-field object, schema dictionary, sequence row, or scalar as inert dialect-shaped SQL.</summary>
    /// <param name="table">The nonempty single-line table identifier.</param>
    /// <param name="value">The row value, including null.</param>
    /// <param name="dialect">The identifier and literal convention to use.</param>
    /// <returns>A deterministic <c>INSERT</c> statement using the requested dialect shape.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>table</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an identifier or text value is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>dialect</c> is unsupported or configured limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when the row has no columns or contains an unsupported shape.</exception>
    public string SqlInsertFromTableAndValueAndDialect(string table, object value, SqlDialect dialect)
    {
        return StructuredTextGenerator.SqlInsert(Context, table, new object[] { value }, dialect);
    }

    /// <summary>Exports ordered rows as one inert multi-row ANSI SQL insert.</summary>
    /// <remarks>The first non-null row defines columns. Public fields use ordinal name order and dictionary order is preserved.</remarks>
    /// <param name="table">The nonempty single-line table identifier.</param>
    /// <param name="rows">The ordered rows to serialize.</param>
    /// <returns>A deterministic multi-row <c>INSERT</c> statement, or empty text for no rows.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>table</c> or <c>rows</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an identifier or text value is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when rows have inconsistent or unsupported shapes.</exception>
    public string SqlInsertFromTableAndRows(string table, object[] rows)
    {
        return StructuredTextGenerator.SqlInsert(Context, table, rows, SqlDialect.Ansi);
    }

    /// <summary>Exports ordered rows as one inert multi-row insert using explicit SQL dialect conventions.</summary>
    /// <remarks>The first non-null row defines columns. Public fields use ordinal name order and dictionary order is preserved.</remarks>
    /// <param name="table">The nonempty single-line table identifier.</param>
    /// <param name="rows">The ordered rows to serialize.</param>
    /// <param name="dialect">The identifier and literal convention to use.</param>
    /// <returns>A deterministic multi-row <c>INSERT</c> statement, or empty text for no rows.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>table</c> or <c>rows</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an identifier or text value is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>dialect</c> is unsupported or configured limits are exceeded.</exception>
    /// <exception cref="FakeException">Thrown when rows have inconsistent or unsupported shapes.</exception>
    public string SqlInsertFromTableAndRowsAndDialect(string table, object[] rows, SqlDialect dialect)
    {
        return StructuredTextGenerator.SqlInsert(Context, table, rows, dialect);
    }

    /// <summary>Generates an inert explicitly malformed SQL insert.</summary>
    /// <remarks>The returned value is text only and is never executed.</remarks>
    /// <returns>SQL-shaped text containing one deliberate syntax defect.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string InvalidSqlInsert()
    {
        return StructuredTextGenerator.InvalidSqlInsert(Context, RandomValue);
    }

    /// <summary>Returns an inert SQL insert with the selected deliberate syntax defect.</summary>
    /// <remarks>The returned value is text only and is never executed.</remarks>
    /// <param name="kind">The defect to place in the text.</param>
    /// <returns>Explicitly malformed SQL-shaped text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when kind is unsupported or a configured limit is insufficient.</exception>
    public string InvalidSqlInsertFromKind(StructuredTextInvalidKind kind)
    {
        return StructuredTextGenerator.InvalidSqlInsert(Context, kind);
    }
}
