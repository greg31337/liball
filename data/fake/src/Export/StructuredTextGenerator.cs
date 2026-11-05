using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace fake;

internal static class StructuredTextGenerator
{
    internal static string JsonString(
        GenerationContext context,
        string value)
    {
        ValidateInput(context, value, "value");
        string result = JsonSerializer.Serialize(value);
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "jsonString");
        return result;
    }

    internal static string JsonNumber(
        GenerationContext context,
        long value)
    {
        string result = value.ToString(CultureInfo.InvariantCulture);
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "jsonNumber");
        return result;
    }

    internal static string JsonNumber(
        GenerationContext context,
        double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(
                "value",
                value,
                "value must be a finite JSON number.");
        }
        string result = value.ToString("R", CultureInfo.InvariantCulture);
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "jsonNumber");
        return result;
    }

    internal static string JsonScalar(
        GenerationContext context,
        object? value)
    {
        if (value != null && !CsvValueFormatter.IsScalar(value.GetType()))
        {
            throw new ArgumentException(
                "value must be a JSON scalar.",
                "value");
        }
        return new JsonObjectWriter(context).Write(value);
    }

    internal static string JsonObject(
        GenerationContext context,
        object value)
    {
        return new JsonObjectWriter(context).Write(value);
    }

    internal static string CsvField(
        GenerationContext context,
        string value,
        bool guardSpreadsheetFormula)
    {
        ValidateInput(context, value, "value");
        return CsvFieldWriter.Escape(
            context,
            value,
            ',',
            guardSpreadsheetFormula);
    }

    internal static string CsvTable(
        GenerationContext context,
        object[] rows,
        CsvOptions options)
    {
        return new CsvTableWriter(context, options).Write(rows);
    }

    internal static string CsvRow(
        GenerationContext context,
        object value)
    {
        CsvOptions options = new CsvOptions();
        options.IncludeHeader = false;
        return new CsvTableWriter(context, options).Write(
            new object[] { value });
    }

    internal static string XmlElement(
        GenerationContext context,
        string name,
        string value)
    {
        ValidateInput(context, name, "name");
        ValidateInput(context, value, "value");
        XmlConvert.VerifyNCName(name);
        XmlConvert.VerifyXmlChars(value);
        int length = Guard.OutputSize(
            5L + (long)name.Length * 2L + XmlEscapedLength(value),
            context.CapturedOptions.MaximumOutputSize,
            "xmlElement");
        StringBuilder result = new StringBuilder(length);
        result.Append('<');
        result.Append(name);
        result.Append('>');
        AppendXmlEscaped(result, value);
        result.Append("</");
        result.Append(name);
        result.Append('>');
        return result.ToString();
    }

    internal static string XmlObject(
        GenerationContext context,
        object? value,
        string? rootName)
    {
        return new XmlObjectWriter(context).Write(value, rootName);
    }

    internal static string VCard(
        GenerationContext context,
        PersonProfile profile)
    {
        return new VCardWriter(context).Person(profile);
    }

    internal static string VCard(
        GenerationContext context,
        CompanyProfile profile)
    {
        return new VCardWriter(context).Company(profile);
    }

    internal static string VCardLine(
        GenerationContext context,
        string name,
        string value)
    {
        return new VCardWriter(context).TextLine(name, value);
    }

    internal static string Json(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateJsonData(context);
        Guard.OutputSize(
            JsonMaximumLength(),
            context.CapturedOptions.MaximumOutputSize,
            "json");
        JsonTextKind kind = (JsonTextKind)random.IntFromMaximumExclusive(3);
        return JsonUnchecked(random, kind);
    }

    internal static string Json(
        GenerationContext context,
        FakeRandom random,
        JsonTextKind kind)
    {
        ValidateJsonKind(kind);
        string[] values = JsonValues(kind);
        ValidateValues(context, values, "jsonData");
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            "json");
        return ChooseValue(random, values);
    }

    internal static string InvalidJson(
        GenerationContext context,
        FakeRandom random)
    {
        return InvalidValue(
            context,
            random,
            StructuredTextData.InvalidJson,
            "invalidJsonData",
            "invalidJson");
    }

    internal static string InvalidJson(
        GenerationContext context,
        StructuredTextInvalidKind kind)
    {
        return InvalidValue(
            context,
            StructuredTextData.InvalidJson,
            kind,
            "invalidJsonData",
            "invalidJson");
    }

    internal static string Csv(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateCsvData(context);
        Guard.OutputSize(
            CsvMaximumLength(),
            context.CapturedOptions.MaximumOutputSize,
            "csv");
        CsvTextKind kind = (CsvTextKind)random.IntFromMaximumExclusive(2);
        return CsvUnchecked(random, kind);
    }

    internal static string Csv(
        GenerationContext context,
        FakeRandom random,
        CsvTextKind kind)
    {
        ValidateCsvKind(kind);
        string[] values = CsvValues(kind);
        ValidateValues(context, values, "csvData");
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            "csv");
        return ChooseValue(random, values);
    }

    internal static string InvalidCsv(
        GenerationContext context,
        FakeRandom random)
    {
        return InvalidValue(
            context,
            random,
            StructuredTextData.InvalidCsv,
            "invalidCsvData",
            "invalidCsv");
    }

    internal static string InvalidCsv(
        GenerationContext context,
        StructuredTextInvalidKind kind)
    {
        return InvalidValue(
            context,
            StructuredTextData.InvalidCsv,
            kind,
            "invalidCsvData",
            "invalidCsv");
    }

    internal static string Xml(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateXmlData(context);
        Guard.OutputSize(
            XmlMaximumLength(),
            context.CapturedOptions.MaximumOutputSize,
            "xml");
        XmlTextKind kind = (XmlTextKind)random.IntFromMaximumExclusive(2);
        return XmlUnchecked(random, kind);
    }

    internal static string Xml(
        GenerationContext context,
        FakeRandom random,
        XmlTextKind kind)
    {
        ValidateXmlKind(kind);
        string[] values = XmlValues(kind);
        ValidateValues(context, values, "xmlData");
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            "xml");
        return ChooseValue(random, values);
    }

    internal static string InvalidXml(
        GenerationContext context,
        FakeRandom random)
    {
        return InvalidValue(
            context,
            random,
            StructuredTextData.InvalidXml,
            "invalidXmlData",
            "invalidXml");
    }

    internal static string InvalidXml(
        GenerationContext context,
        StructuredTextInvalidKind kind)
    {
        return InvalidValue(
            context,
            StructuredTextData.InvalidXml,
            kind,
            "invalidXmlData",
            "invalidXml");
    }

    internal static string Yaml(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateYamlData(context);
        Guard.OutputSize(
            YamlMaximumLength(),
            context.CapturedOptions.MaximumOutputSize,
            "yaml");
        YamlTextKind kind = (YamlTextKind)random.IntFromMaximumExclusive(3);
        return YamlUnchecked(random, kind);
    }

    internal static string Yaml(
        GenerationContext context,
        FakeRandom random,
        YamlTextKind kind)
    {
        ValidateYamlKind(kind);
        string[] values = YamlValues(kind);
        ValidateValues(context, values, "yamlData");
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            "yaml");
        return ChooseValue(random, values);
    }

    internal static string InvalidYaml(
        GenerationContext context,
        FakeRandom random)
    {
        return InvalidValue(
            context,
            random,
            StructuredTextData.InvalidYaml,
            "invalidYamlData",
            "invalidYaml");
    }

    internal static string InvalidYaml(
        GenerationContext context,
        StructuredTextInvalidKind kind)
    {
        return InvalidValue(
            context,
            StructuredTextData.InvalidYaml,
            kind,
            "invalidYamlData",
            "invalidYaml");
    }

    internal static string SqlInsert(
        GenerationContext context,
        FakeRandom random)
    {
        return SqlTextGenerator.Insert(context, random);
    }

    internal static string SqlInsert(
        GenerationContext context,
        string table,
        object[] rows,
        SqlDialect dialect)
    {
        return new SqlInsertWriter(context, dialect).Write(table, rows);
    }

    internal static string InvalidSqlInsert(
        GenerationContext context,
        FakeRandom random)
    {
        return InvalidValue(
            context,
            random,
            StructuredTextData.InvalidSqlInserts,
            "invalidSqlInsertData",
            "invalidSqlInsert");
    }

    internal static string InvalidSqlInsert(
        GenerationContext context,
        StructuredTextInvalidKind kind)
    {
        return InvalidValue(
            context,
            StructuredTextData.InvalidSqlInserts,
            kind,
            "invalidSqlInsertData",
            "invalidSqlInsert");
    }

    static string JsonUnchecked(
        FakeRandom random,
        JsonTextKind kind)
    {
        return ChooseValue(random, JsonValues(kind));
    }

    static string CsvUnchecked(
        FakeRandom random,
        CsvTextKind kind)
    {
        return ChooseValue(random, CsvValues(kind));
    }

    static string XmlUnchecked(
        FakeRandom random,
        XmlTextKind kind)
    {
        return ChooseValue(random, XmlValues(kind));
    }

    static string YamlUnchecked(
        FakeRandom random,
        YamlTextKind kind)
    {
        return ChooseValue(random, YamlValues(kind));
    }

    static string[] JsonValues(JsonTextKind kind)
    {
        switch (kind)
        {
            case JsonTextKind.Scalar:
                return StructuredTextData.JsonScalars;
            case JsonTextKind.Object:
                return StructuredTextData.JsonObjects;
            default:
                return StructuredTextData.JsonArrays;
        }
    }

    static string[] CsvValues(CsvTextKind kind)
    {
        if (kind == CsvTextKind.Row)
        {
            return StructuredTextData.CsvRows;
        }
        return StructuredTextData.CsvDocuments;
    }

    static string[] XmlValues(XmlTextKind kind)
    {
        if (kind == XmlTextKind.Element)
        {
            return StructuredTextData.XmlElements;
        }
        return StructuredTextData.XmlDocuments;
    }

    static string[] YamlValues(YamlTextKind kind)
    {
        switch (kind)
        {
            case YamlTextKind.Scalar:
                return StructuredTextData.YamlScalars;
            case YamlTextKind.Sequence:
                return StructuredTextData.YamlSequences;
            default:
                return StructuredTextData.YamlMappings;
        }
    }

    static string InvalidValue(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string dataName,
        string outputName)
    {
        ValidateValues(context, values, dataName);
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return ChooseValue(random, values);
    }

    static string InvalidValue(
        GenerationContext context,
        string[] values,
        StructuredTextInvalidKind kind,
        string dataName,
        string outputName)
    {
        ValidateInvalidKind(kind);
        ValidateValues(context, values, dataName);
        string value = values[(int)kind];
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return value;
    }

    static string ChooseValue(FakeRandom random, string[] values)
    {
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static void ValidateInput(
        GenerationContext context,
        string value,
        string parameterName)
    {
        Guard.NotNull(value, parameterName);
        Guard.Count(
            value.Length,
            context.CapturedOptions.MaximumCount,
            parameterName);
        UnicodeText.Validate(value, parameterName);
    }

    static long XmlEscapedLength(string value)
    {
        long length = 0L;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '&')
            {
                length = length + 5L;
            }
            else if (character == '<' || character == '>')
            {
                length = length + 4L;
            }
            else if (character == '"' || character == '\'')
            {
                length = length + 6L;
            }
            else
            {
                length = length + 1L;
            }
            index = index + 1;
        }
        return length;
    }

    static void AppendXmlEscaped(StringBuilder result, string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '&')
            {
                result.Append("&amp;");
            }
            else if (character == '<')
            {
                result.Append("&lt;");
            }
            else if (character == '>')
            {
                result.Append("&gt;");
            }
            else if (character == '"')
            {
                result.Append("&quot;");
            }
            else if (character == '\'')
            {
                result.Append("&apos;");
            }
            else
            {
                result.Append(character);
            }
            index = index + 1;
        }
    }

    static void ValidateJsonData(GenerationContext context)
    {
        ValidateValues(context, StructuredTextData.JsonScalars, "jsonScalarData");
        ValidateValues(context, StructuredTextData.JsonObjects, "jsonObjectData");
        ValidateValues(context, StructuredTextData.JsonArrays, "jsonArrayData");
    }

    static void ValidateCsvData(GenerationContext context)
    {
        ValidateValues(context, StructuredTextData.CsvRows, "csvRowData");
        ValidateValues(context, StructuredTextData.CsvDocuments, "csvDocumentData");
    }

    static void ValidateXmlData(GenerationContext context)
    {
        ValidateValues(context, StructuredTextData.XmlElements, "xmlElementData");
        ValidateValues(context, StructuredTextData.XmlDocuments, "xmlDocumentData");
    }

    static void ValidateYamlData(GenerationContext context)
    {
        ValidateValues(context, StructuredTextData.YamlScalars, "yamlScalarData");
        ValidateValues(context, StructuredTextData.YamlSequences, "yamlSequenceData");
        ValidateValues(context, StructuredTextData.YamlMappings, "yamlMappingData");
    }

    static void ValidateValues(
        GenerationContext context,
        string[] values,
        string dataName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            dataName);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || values[index].Length == 0)
            {
                throw FakeException.Create(
                    "A project-authored structured-text table is malformed.");
            }
            index = index + 1;
        }
    }

    static void ValidateJsonKind(JsonTextKind kind)
    {
        switch (kind)
        {
            case JsonTextKind.Scalar:
            case JsonTextKind.Object:
            case JsonTextKind.Array:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported JSON text shape.");
        }
    }

    static void ValidateCsvKind(CsvTextKind kind)
    {
        switch (kind)
        {
            case CsvTextKind.Row:
            case CsvTextKind.Document:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported CSV text shape.");
        }
    }

    static void ValidateXmlKind(XmlTextKind kind)
    {
        switch (kind)
        {
            case XmlTextKind.Element:
            case XmlTextKind.Document:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported XML text shape.");
        }
    }

    static void ValidateYamlKind(YamlTextKind kind)
    {
        switch (kind)
        {
            case YamlTextKind.Scalar:
            case YamlTextKind.Sequence:
            case YamlTextKind.Mapping:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported YAML text shape.");
        }
    }

    static void ValidateInvalidKind(StructuredTextInvalidKind kind)
    {
        switch (kind)
        {
            case StructuredTextInvalidKind.UnterminatedQuotedValue:
            case StructuredTextInvalidKind.MissingSeparator:
            case StructuredTextInvalidKind.UnexpectedToken:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported structured-text defect.");
        }
    }

    static int JsonMaximumLength()
    {
        return Maximum(
            MaximumLength(StructuredTextData.JsonScalars),
            MaximumLength(StructuredTextData.JsonObjects),
            MaximumLength(StructuredTextData.JsonArrays));
    }

    static int CsvMaximumLength()
    {
        return Maximum(
            MaximumLength(StructuredTextData.CsvRows),
            MaximumLength(StructuredTextData.CsvDocuments));
    }

    static int XmlMaximumLength()
    {
        return Maximum(
            MaximumLength(StructuredTextData.XmlElements),
            MaximumLength(StructuredTextData.XmlDocuments));
    }

    static int YamlMaximumLength()
    {
        return Maximum(
            MaximumLength(StructuredTextData.YamlScalars),
            MaximumLength(StructuredTextData.YamlSequences),
            MaximumLength(StructuredTextData.YamlMappings));
    }

    static int Maximum(int first, int second)
    {
        if (first > second)
        {
            return first;
        }
        return second;
    }

    static int Maximum(int first, int second, int third)
    {
        return Maximum(Maximum(first, second), third);
    }

    static int MaximumLength(string[] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > maximum)
            {
                maximum = values[index].Length;
            }
            index = index + 1;
        }
        return maximum;
    }
}
