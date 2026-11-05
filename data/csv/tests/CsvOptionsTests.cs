using System;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvOptionsTests
{
    [Test]
    public void ReaderOptions_CreateDefault_InitializesStrictRfc4180Reading()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();

        Assert.That(options.Dialect.Delimiter, Is.EqualTo(','));
        Assert.That(options.HeaderOptions, Is.Not.Null);
        Assert.That(options.TrimMode, Is.EqualTo(CsvTrimMode.None));
        Assert.That(options.SkipInitialSpace, Is.False);
        Assert.That(options.SkipBlankLines, Is.False);
        Assert.That(options.AllowComments, Is.False);
        Assert.That(options.HasNullString, Is.False);
        Assert.That(options.BufferSize, Is.EqualTo(4096));
        Assert.That(options.MaxFieldLength, Is.EqualTo(0));
        Assert.That(options.MaxRowLength, Is.EqualTo(0));
        Assert.That(options.MaxColumnCount, Is.EqualTo(0));
        Assert.That(options.ErrorMode, Is.EqualTo(CsvErrorMode.Strict));
        Assert.That(options.LeaveOpen, Is.False);
    }

    [Test]
    public void WriterOptions_CreateDefault_InitializesMinimalRfc4180Writing()
    {
        CsvWriterOptions options = CsvWriterOptions.CreateDefault();

        Assert.That(options.Dialect.Delimiter, Is.EqualTo(','));
        Assert.That(options.QuoteMode, Is.EqualTo(CsvQuoteMode.Minimal));
        Assert.That(options.HasNullString, Is.False);
        Assert.That(options.InjectionMode, Is.EqualTo(CsvInjectionMode.None));
        Assert.That(options.LineTerminator, Is.EqualTo("\r\n"));
        Assert.That(options.NormalizeNewLines, Is.False);
        Assert.That(options.WriteHeader, Is.False);
        Assert.That(options.FlushAfterRow, Is.False);
        Assert.That(options.LeaveOpen, Is.False);
        Assert.That(options.Append, Is.False);
    }

    [Test]
    public void HeaderOptions_CreateDefault_InitializesHeaderPolicies()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();

        Assert.That(options.HasHeader, Is.False);
        Assert.That(options.GenerateHeaders, Is.True);
        Assert.That(options.GeneratedHeaderPrefix, Is.EqualTo("Column"));
        Assert.That(options.NormalizeHeaderNames, Is.False);
        Assert.That(options.CaseSensitive, Is.True);
        Assert.That(options.DuplicateHeaderMode, Is.EqualTo(CsvDuplicateHeaderMode.Error));
        Assert.That(options.MissingHeaderMode, Is.EqualTo(CsvMissingHeaderMode.Generate));
        Assert.That(options.MissingFieldMode, Is.EqualTo(CsvMissingFieldMode.Empty));
        Assert.That(options.ExtraFieldMode, Is.EqualTo(CsvExtraFieldMode.Allow));
    }

    [Test]
    public void ConversionOptions_CreateDefault_InitializesCultureAndNullPolicies()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();

        Assert.That(options.Culture, Is.SameAs(CultureInfo.InvariantCulture));
        Assert.That(options.DateTimeFormats, Is.Empty);
        Assert.That(options.DecimalSeparator, Is.EqualTo("."));
        Assert.That(options.ThousandsSeparator, Is.EqualTo(","));
        Assert.That(options.HasNullString, Is.False);
        Assert.That(options.EmptyStringIsNull, Is.False);
        Assert.That(options.DefaultValue, Is.EqualTo(""));
        Assert.That(options.UseDefaultForNull, Is.False);
        Assert.That(options.UseDefaultForInvalid, Is.False);
    }

    [Test]
    public void ConversionOptions_UseCultureSeparatorsCopiesCultureNumberSeparators()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.Culture = new CultureInfo("de-DE");

        options.UseCultureSeparators();

        Assert.That(options.DecimalSeparator, Is.EqualTo(","));
        Assert.That(options.ThousandsSeparator, Is.EqualTo("."));
    }

    [Test]
    public void ConversionOptions_DetectsNullValuesAndAppliesNullDefault()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.EmptyStringIsNull = true;
        options.HasNullString = true;
        options.NullString = "NULL";
        options.UseDefaultForNull = true;
        options.DefaultValue = "fallback";

        Assert.That(options.IsNullValue(""), Is.True);
        Assert.That(options.IsNullValue("NULL"), Is.True);
        Assert.That(options.IsNullValue("value"), Is.False);
        Assert.That(options.ResolveNullValue("NULL"), Is.EqualTo("fallback"));
        Assert.That(options.ResolveNullValue("value"), Is.EqualTo("value"));
    }

    [Test]
    public void ConversionOptions_ResolvesInvalidDefaultWhenConfigured()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.DefaultValue = "0";

        Assert.That(options.ResolveInvalidValue("bad"), Is.EqualTo("bad"));

        options.UseDefaultForInvalid = true;

        Assert.That(options.ResolveInvalidValue("bad"), Is.EqualTo("0"));
    }

    [Test]
    public void ConversionOptions_NormalizesNumberTextUsingConfiguredSeparators()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.DecimalSeparator = ",";
        options.ThousandsSeparator = ".";

        Assert.That(options.NormalizeNumberString("1.234,56"), Is.EqualTo("1234.56"));
    }

    [Test]
    public void ConversionOptions_TracksDateTimeFormatPresence()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();

        Assert.That(options.HasDateTimeFormats(), Is.False);

        options.DateTimeFormats = new string[] { "yyyy-MM-dd" };

        Assert.That(options.HasDateTimeFormats(), Is.True);
    }

    [Test]
    public void ConversionOptions_CloneCopiesValuesAndDateFormats()
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        options.Culture = new CultureInfo("fr-FR");
        options.DateTimeFormats = new string[] { "dd/MM/yyyy" };
        options.DecimalSeparator = ",";
        options.ThousandsSeparator = " ";
        options.NullString = "NULL";
        options.HasNullString = true;
        options.EmptyStringIsNull = true;
        options.DefaultValue = "fallback";
        options.UseDefaultForNull = true;
        options.UseDefaultForInvalid = true;

        CsvConversionOptions clone = options.Clone();
        options.DateTimeFormats[0] = "changed";

        Assert.That(clone.Culture.Name, Is.EqualTo("fr-FR"));
        Assert.That(clone.DateTimeFormats, Is.EqualTo(new string[] { "dd/MM/yyyy" }));
        Assert.That(clone.DecimalSeparator, Is.EqualTo(","));
        Assert.That(clone.ThousandsSeparator, Is.EqualTo(" "));
        Assert.That(clone.NullString, Is.EqualTo("NULL"));
        Assert.That(clone.HasNullString, Is.True);
        Assert.That(clone.EmptyStringIsNull, Is.True);
        Assert.That(clone.DefaultValue, Is.EqualTo("fallback"));
        Assert.That(clone.UseDefaultForNull, Is.True);
        Assert.That(clone.UseDefaultForInvalid, Is.True);
    }

    [Test]
    public void SnifferOptions_CreateDefault_InitializesSamplingCandidates()
    {
        CsvSnifferOptions options = CsvSnifferOptions.CreateDefault();

        Assert.That(options.MaxSampleCharacters, Is.EqualTo(32768));
        Assert.That(options.MaxSampleRows, Is.EqualTo(20));
        Assert.That(options.CandidateDelimiters, Is.EqualTo(new char[] { ',', '\t', ';', '|' }));
        Assert.That(options.CandidateQuotes, Is.EqualTo(new char[] { '"', '\'' }));
        Assert.That(options.CandidateComments, Is.EqualTo(new char[] { '#' }));
        Assert.That(options.DetectHeader, Is.True);
        Assert.That(options.DetectComments, Is.True);
        Assert.That(options.OverrideDialect, Is.Not.Null);
        Assert.That(options.HasOverrideDialect, Is.False);
    }

    [Test]
    public void InferenceOptions_CreateDefault_InitializesTypeInferenceDefaults()
    {
        CsvInferenceOptions options = CsvInferenceOptions.CreateDefault();

        Assert.That(options.MaxSampleRows, Is.EqualTo(100));
        Assert.That(options.InferTypes, Is.True);
        Assert.That(options.InferNullability, Is.True);
        Assert.That(options.TreatEmptyAsNull, Is.False);
        Assert.That(options.ConversionOptions, Is.Not.Null);
    }

    [Test]
    public void TableOptions_CreateDefault_InitializesBoundedTableDefaults()
    {
        CsvTableOptions options = CsvTableOptions.CreateDefault();

        Assert.That(options.MaxRows, Is.EqualTo(0));
        Assert.That(options.IncludeHeader, Is.True);
        Assert.That(options.CloneRows, Is.True);
        Assert.That(options.PreserveDiagnostics, Is.True);
    }

    [Test]
    public void CreateDefault_ReturnsIndependentNestedOptions()
    {
        CsvReaderOptions firstReader = CsvReaderOptions.CreateDefault();
        CsvReaderOptions secondReader = CsvReaderOptions.CreateDefault();
        firstReader.Dialect.Delimiter = ';';
        firstReader.HeaderOptions.CaseSensitive = false;

        Assert.That(secondReader.Dialect.Delimiter, Is.EqualTo(','));
        Assert.That(secondReader.HeaderOptions.CaseSensitive, Is.True);

        CsvInferenceOptions firstInference = CsvInferenceOptions.CreateDefault();
        CsvInferenceOptions secondInference = CsvInferenceOptions.CreateDefault();
        firstInference.ConversionOptions.DecimalSeparator = ",";

        Assert.That(secondInference.ConversionOptions.DecimalSeparator, Is.EqualTo("."));
    }

    [TestCase(typeof(CsvReaderOptions))]
    [TestCase(typeof(CsvWriterOptions))]
    [TestCase(typeof(CsvHeaderOptions))]
    [TestCase(typeof(CsvConversionOptions))]
    [TestCase(typeof(CsvSnifferOptions))]
    [TestCase(typeof(CsvInferenceOptions))]
    [TestCase(typeof(CsvTableOptions))]
    public void OptionTypes_ExposePublicFieldsAndNoProperties(Type optionType)
    {
        FieldInfo[] fields = optionType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo[] properties = optionType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.That(fields.Length, Is.GreaterThan(0));
        Assert.That(properties, Is.Empty);
    }
}
