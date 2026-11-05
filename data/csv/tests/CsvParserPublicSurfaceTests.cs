using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvParserPublicSurfaceTests
{
    [Test]
    public void Read_PublicApiCoversRfcFieldsQuotingEscapesAndRecordEndings()
    {
        string input = "a,b,\r\n\"x,y\",\"line 1\r\nline 2\",\"say \"\"hi\"\"\"\r\n,last";
        CsvReader reader = new CsvReader(new StringReader(input), new CsvReaderOptions());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b", "" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "x,y", "line 1\r\nline 2", "say \"hi\"" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "", "last" }));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_AutoNewLineModeAcceptsCrlfLfAndCr()
    {
        CsvReaderOptions options = new CsvReaderOptions();
        options.Dialect.NewLineMode = CsvNewLineMode.Auto;
        CsvReader reader = new CsvReader(new StringReader("one\r\ntwo\nthree\rfour"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("one"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("two"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("three"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("four"));
        Assert.That(reader.Read(), Is.False);
    }

    [TestCase(CsvNewLineMode.Crlf, "one\ntwo\r\nthree", "one\ntwo")]
    [TestCase(CsvNewLineMode.Lf, "one\rtwo\nthree", "one\rtwo")]
    [TestCase(CsvNewLineMode.Cr, "one\ntwo\rthree", "one\ntwo")]
    public void Read_ExplicitNewLineModesOnlySplitOnTheirConfiguredEnding(CsvNewLineMode mode, string input, string firstValue)
    {
        CsvReaderOptions options = new CsvReaderOptions();
        options.Dialect.NewLineMode = mode;
        CsvReader reader = new CsvReader(new StringReader(input), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo(firstValue));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo("three"));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_PublicDialectFieldsControlDelimiterQuoteEscapeAndComments()
    {
        CsvReaderOptions options = new CsvReaderOptions();
        options.Dialect.Delimiter = ';';
        options.Dialect.Quote = '\'';
        options.Dialect.Escape = '\\';
        options.Dialect.UseDoubleQuoteEscaping = false;
        options.Dialect.NewLineMode = CsvNewLineMode.Lf;
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '#';
        options.AllowComments = true;
        CsvReader reader = new CsvReader(new StringReader("# ignored\n'a;b';'it\\'s';plain\n"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a;b", "it's", "plain" }));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(2));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void ReadHeader_PublicApiAppliesNormalizationLookupAndGeneratedNames()
    {
        CsvReaderOptions options = new CsvReaderOptions();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.NormalizeHeaderNames = true;
        options.HeaderOptions.CaseSensitive = false;
        options.HeaderOptions.DuplicateHeaderMode = CsvDuplicateHeaderMode.LastWins;
        options.HeaderOptions.MissingHeaderMode = CsvMissingHeaderMode.Generate;
        options.HeaderOptions.GeneratedHeaderPrefix = "Field";
        options.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Null;
        CsvReader reader = new CsvReader(new StringReader(" Name ,Name,,Tail\r\none,two"), options);

        Assert.That(reader.ShouldReadHeader(), Is.True);
        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "Name", "Name", "Field3", "Tail" }));
        Assert.That(reader.GetHeaderIndex("name"), Is.EqualTo(1));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string?[] { "one", "two", null, null }));
        Assert.That(reader.GetFieldByName("NAME"), Is.EqualTo("two"));
        Assert.That(reader.HasFieldByName("Field3"), Is.True);
    }

    [Test]
    public void ReadHeader_PublicApiReportsDuplicateAndMissingHeaderDiagnostics()
    {
        CsvReaderOptions options = new CsvReaderOptions();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.DuplicateHeaderMode = CsvDuplicateHeaderMode.Error;
        options.HeaderOptions.MissingHeaderMode = CsvMissingHeaderMode.Error;
        CsvReader reader = new CsvReader(new StringReader("Id,Id,\r\n1,2,3"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetDiagnostics().Items, Has.Count.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Items[0].Code, Is.EqualTo("DuplicateHeader"));
        Assert.That(reader.GetDiagnostics().Items[1].Code, Is.EqualTo("MissingHeader"));
        Assert.That(reader.GetDiagnostics().Items[0].Row, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Items[1].Row, Is.EqualTo(1));
    }

    [Test]
    public void Read_PublicApiAppliesEveryMissingFieldPolicy()
    {
        CsvReaderOptions emptyOptions = CreateHeaderOptions();
        emptyOptions.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Empty;
        CsvReader emptyReader = new CsvReader(new StringReader("A,B\nx"), emptyOptions);
        Assert.That(emptyReader.ReadHeader(), Is.True);
        Assert.That(emptyReader.Read(), Is.True);
        Assert.That(emptyReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "x", "" }));

        CsvReaderOptions nullOptions = CreateHeaderOptions();
        nullOptions.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Null;
        CsvReader nullReader = new CsvReader(new StringReader("A,B\nx"), nullOptions);
        Assert.That(nullReader.ReadHeader(), Is.True);
        Assert.That(nullReader.Read(), Is.True);
        Assert.That(nullReader.GetCurrentRow().Fields, Is.EqualTo(new string?[] { "x", null }));

        CsvReaderOptions errorOptions = CreateHeaderOptions();
        errorOptions.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Error;
        CsvReader errorReader = new CsvReader(new StringReader("A,B\nx"), errorOptions);
        Assert.That(errorReader.ReadHeader(), Is.True);
        Assert.That(errorReader.Read(), Is.True);
        Assert.That(errorReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "x" }));
        Assert.That(errorReader.GetDiagnostics().Items[0].Code, Is.EqualTo("MissingField"));
    }

    [Test]
    public void Read_PublicApiAppliesEveryExtraFieldPolicy()
    {
        CsvReaderOptions allowOptions = CreateHeaderOptions();
        allowOptions.HeaderOptions.ExtraFieldMode = CsvExtraFieldMode.Allow;
        CsvReader allowReader = new CsvReader(new StringReader("A,B\nx,y,z"), allowOptions);
        Assert.That(allowReader.ReadHeader(), Is.True);
        Assert.That(allowReader.Read(), Is.True);
        Assert.That(allowReader.GetCurrentRow().Fields, Has.Length.EqualTo(3));

        CsvReaderOptions ignoreOptions = CreateHeaderOptions();
        ignoreOptions.HeaderOptions.ExtraFieldMode = CsvExtraFieldMode.Ignore;
        CsvReader ignoreReader = new CsvReader(new StringReader("A,B\nx,y,z"), ignoreOptions);
        Assert.That(ignoreReader.ReadHeader(), Is.True);
        Assert.That(ignoreReader.Read(), Is.True);
        Assert.That(ignoreReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "x", "y" }));

        CsvReaderOptions errorOptions = CreateHeaderOptions();
        errorOptions.HeaderOptions.ExtraFieldMode = CsvExtraFieldMode.Error;
        CsvReader errorReader = new CsvReader(new StringReader("A,B\nx,y,z"), errorOptions);
        Assert.That(errorReader.ReadHeader(), Is.True);
        Assert.That(errorReader.Read(), Is.True);
        Assert.That(errorReader.GetCurrentRow().Fields, Has.Length.EqualTo(3));
        Assert.That(errorReader.GetDiagnostics().Items[0].Code, Is.EqualTo("ExtraField"));
    }

    [TestCase(CsvTrimMode.None, " a ", " b ")]
    [TestCase(CsvTrimMode.Unquoted, "a", " b ")]
    [TestCase(CsvTrimMode.QuotedAndUnquoted, "a", "b")]
    [TestCase(CsvTrimMode.Left, "a ", "b ")]
    [TestCase(CsvTrimMode.Right, " a", " b")]
    [TestCase(CsvTrimMode.Both, "a", "b")]
    public void Read_PublicApiAppliesEveryWhitespaceTrimMode(CsvTrimMode mode, string first, string second)
    {
        CsvReaderOptions options = new CsvReaderOptions();
        options.TrimMode = mode;
        CsvReader reader = new CsvReader(new StringReader(" a ,\" b \""), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { first, second }));
    }

    [Test]
    public void Read_PublicApiAppliesInitialSpaceBlankCommentAndNullPolicies()
    {
        CsvReaderOptions options = new CsvReaderOptions();
        options.SkipBlankLines = true;
        options.AllowComments = true;
        options.Dialect.HasComment = true;
        options.Dialect.Comment = '#';
        options.SkipInitialSpace = true;
        options.TrimMode = CsvTrimMode.Both;
        options.HasNullString = true;
        options.NullString = "NULL";
        CsvReader reader = new CsvReader(new StringReader("\r\n# comment\r\n  NULL,  \" keep \", text \r\n"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string?[] { null, "keep", "text" }));
        Assert.That(reader.GetCurrentRow().Line, Is.EqualTo(3));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_PublicApiReportsEveryConfiguredParserLimit()
    {
        CsvReaderOptions fieldOptions = new CsvReaderOptions();
        fieldOptions.MaxFieldLength = 2;
        CsvReader fieldReader = new CsvReader(new StringReader("abc"), fieldOptions);
        Assert.That(fieldReader.Read(), Is.False);
        Assert.That(fieldReader.GetDiagnostics().Items[0].Code, Is.EqualTo("MaxFieldLength"));

        CsvReaderOptions rowOptions = new CsvReaderOptions();
        rowOptions.MaxRowLength = 3;
        CsvReader rowReader = new CsvReader(new StringReader("ab,c"), rowOptions);
        Assert.That(rowReader.Read(), Is.False);
        Assert.That(rowReader.GetDiagnostics().Items[0].Code, Is.EqualTo("MaxRowLength"));

        CsvReaderOptions columnOptions = new CsvReaderOptions();
        columnOptions.MaxColumnCount = 2;
        CsvReader columnReader = new CsvReader(new StringReader("a,b,c"), columnOptions);
        Assert.That(columnReader.Read(), Is.False);
        Assert.That(columnReader.GetDiagnostics().Items[0].Code, Is.EqualTo("MaxColumnCount"));
    }

    [Test]
    public void Read_PublicDiagnosticsExposeLogicalAndPhysicalPositions()
    {
        CsvReader reader = new CsvReader(new StringReader("ok,1\r\na\"b,2"), new CsvReaderOptions());

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.Read(), Is.False);
        CsvDiagnostic diagnostic = reader.GetDiagnostics().Items[0];

        Assert.That(diagnostic.Category, Is.EqualTo(CsvDiagnostic.ParserCategory));
        Assert.That(diagnostic.Code, Is.EqualTo("MalformedQuote"));
        Assert.That(diagnostic.Row, Is.EqualTo(2));
        Assert.That(diagnostic.Column, Is.EqualTo(0));
        Assert.That(diagnostic.Line, Is.EqualTo(2));
        Assert.That(diagnostic.CharacterOffset, Is.EqualTo(8));
        Assert.That(diagnostic.ByteOffset, Is.EqualTo(-1));
        Assert.That(diagnostic.RawContext, Is.EqualTo("a\""));
    }

    [Test]
    public void Read_PublicApiExposesStrictAndRelaxedErrorModes()
    {
        string input = "\"bad\"x,1\nok,2";
        CsvReaderOptions strictOptions = new CsvReaderOptions();
        strictOptions.ErrorMode = CsvErrorMode.Strict;
        CsvReader strictReader = new CsvReader(new StringReader(input), strictOptions);
        Assert.That(strictReader.Read(), Is.False);
        Assert.That(strictReader.GetDiagnostics().Items, Has.Count.EqualTo(1));
        Assert.That(strictReader.GetRejects(), Is.Empty);

        CsvReaderOptions relaxedOptions = new CsvReaderOptions();
        relaxedOptions.ErrorMode = CsvErrorMode.Relaxed;
        CsvReader relaxedReader = new CsvReader(new StringReader(input), relaxedOptions);
        Assert.That(relaxedReader.Read(), Is.True);
        Assert.That(relaxedReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "badx", "1" }));
        Assert.That(relaxedReader.GetDiagnostics().Items, Is.Empty);
    }

    [Test]
    public void Read_PublicApiExposesSkipAndCollectErrorModesWithRawRejects()
    {
        string input = "\"bad\"x,1\nok,2";
        CsvReaderOptions skipOptions = new CsvReaderOptions();
        skipOptions.ErrorMode = CsvErrorMode.SkipBadRow;
        CsvReader skipReader = new CsvReader(new StringReader(input), skipOptions);
        Assert.That(skipReader.Read(), Is.True);
        Assert.That(skipReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "2" }));
        Assert.That(skipReader.GetDiagnostics().Items, Has.Count.EqualTo(1));
        Assert.That(skipReader.GetRejects(), Is.Empty);

        CsvReaderOptions collectOptions = new CsvReaderOptions();
        collectOptions.ErrorMode = CsvErrorMode.CollectRejects;
        CsvReader collectReader = new CsvReader(new StringReader(input), collectOptions);
        Assert.That(collectReader.Read(), Is.True);
        Assert.That(collectReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "ok", "2" }));
        Assert.That(collectReader.GetRejects(), Has.Count.EqualTo(1));
        Assert.That(collectReader.GetRejects()[0].RawText, Is.EqualTo("\"bad\"x,1\n"));
        Assert.That(collectReader.GetRejects()[0].Diagnostics.Items[0].Code, Is.EqualTo("MalformedQuote"));
    }

    [Test]
    public void Read_PublicApiHandlesEmptyAndTruncatedInputsByErrorMode()
    {
        CsvReader emptyReader = new CsvReader(new StringReader(""), new CsvReaderOptions());
        Assert.That(emptyReader.Read(), Is.False);
        Assert.That(emptyReader.GetDiagnostics().Items, Is.Empty);

        CsvReader strictReader = new CsvReader(new StringReader("\"truncated"), new CsvReaderOptions());
        Assert.That(strictReader.Read(), Is.False);
        Assert.That(strictReader.GetDiagnostics().Items[0].Code, Is.EqualTo("UnterminatedQuote"));

        CsvReaderOptions relaxedOptions = new CsvReaderOptions();
        relaxedOptions.ErrorMode = CsvErrorMode.Relaxed;
        CsvReader relaxedReader = new CsvReader(new StringReader("\"truncated"), relaxedOptions);
        Assert.That(relaxedReader.Read(), Is.True);
        Assert.That(relaxedReader.GetField(0), Is.EqualTo("truncated"));
        Assert.That(relaxedReader.Read(), Is.False);

        CsvReaderOptions collectOptions = new CsvReaderOptions();
        collectOptions.ErrorMode = CsvErrorMode.CollectRejects;
        CsvReader collectReader = new CsvReader(new StringReader("\"truncated"), collectOptions);
        Assert.That(collectReader.Read(), Is.False);
        Assert.That(collectReader.GetRejects(), Has.Count.EqualTo(1));
        Assert.That(collectReader.GetRejects()[0].RawText, Is.EqualTo("\"truncated"));
    }

    static CsvReaderOptions CreateHeaderOptions()
    {
        CsvReaderOptions options = new CsvReaderOptions();
        options.HeaderOptions.HasHeader = true;
        return options;
    }
}
