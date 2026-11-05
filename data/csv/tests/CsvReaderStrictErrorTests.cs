using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderStrictErrorTests
{
    [Test]
    public void Read_StrictModeFailsForQuoteInsideUnquotedField()
    {
        CsvReader reader = CsvReader.Create(new StringReader("a\"b,c"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MalformedQuote"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(0));
        Assert.That(reader.GetDiagnostics().Get(0).Line, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).CharacterOffset, Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(0).RawContext, Is.EqualTo("a\""));
        Assert.That(reader.Read(), Is.False);
    }

    [Test]
    public void Read_StrictModeFailsForCharacterAfterClosingQuote()
    {
        CsvReader reader = CsvReader.Create(new StringReader("\"a\"b,c"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MalformedQuote"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(0));
        Assert.That(reader.GetDiagnostics().Get(0).CharacterOffset, Is.EqualTo(4));
        Assert.That(reader.GetDiagnostics().Get(0).RawContext, Is.EqualTo("\"a\"b"));
    }

    [Test]
    public void Read_StrictModeFailsForUnterminatedQuotedField()
    {
        CsvReader reader = CsvReader.Create(new StringReader("\"a,b"), CsvReaderOptions.CreateDefault());

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("UnterminatedQuote"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(0));
        Assert.That(reader.GetDiagnostics().Get(0).Line, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).CharacterOffset, Is.EqualTo(4));
        Assert.That(reader.GetDiagnostics().Get(0).RawContext, Is.EqualTo("\"a,b"));
    }

    [Test]
    public void Read_StrictModeFailsWhenFieldLengthExceedsLimit()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.MaxFieldLength = 3;
        CsvReader reader = CsvReader.Create(new StringReader("abcd,ok"), options);

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MaxFieldLength"));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(0));
        Assert.That(reader.GetDiagnostics().Get(0).CharacterOffset, Is.EqualTo(4));
        Assert.That(reader.GetDiagnostics().Get(0).RawContext, Is.EqualTo("abcd"));
    }

    [Test]
    public void Read_StrictModeFailsWhenRowLengthExceedsLimit()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.MaxRowLength = 3;
        CsvReader reader = CsvReader.Create(new StringReader("ab,c"), options);

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MaxRowLength"));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).CharacterOffset, Is.EqualTo(4));
        Assert.That(reader.GetDiagnostics().Get(0).RawContext, Is.EqualTo("ab,c"));
    }

    [Test]
    public void Read_StrictModeFailsWhenColumnCountExceedsLimit()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.MaxColumnCount = 2;
        CsvReader reader = CsvReader.Create(new StringReader("a,b,c"), options);

        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.HasParseError, Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MaxColumnCount"));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(0).CharacterOffset, Is.EqualTo(5));
        Assert.That(reader.GetDiagnostics().Get(0).RawContext, Is.EqualTo("a,b,c"));
    }

    [Test]
    public void Read_StrictModeAllowsValuesAtConfiguredLimits()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.MaxFieldLength = 3;
        options.MaxRowLength = 5;
        options.MaxColumnCount = 2;
        CsvReader reader = CsvReader.Create(new StringReader("abc,d"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "abc", "d" }));
        Assert.That(reader.HasParseError, Is.False);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }
}
