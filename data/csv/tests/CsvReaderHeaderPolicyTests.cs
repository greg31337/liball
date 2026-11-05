using System.IO;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvReaderHeaderPolicyTests
{
    [Test]
    public void ReadHeader_DuplicateHeaderErrorIsReportedOnReaderDiagnostics()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.DuplicateHeaderMode = CsvDuplicateHeaderMode.Error;
        CsvReader reader = CsvReader.Create(new StringReader("A,A\n1,2"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaders().Diagnostics.Count(), Is.EqualTo(1));
        Assert.That(reader.GetHeaders().Diagnostics.Get(0).Code, Is.EqualTo("DuplicateHeader"));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("DuplicateHeader"));
        Assert.That(reader.GetHeaderIndex("A"), Is.EqualTo(0));
    }

    [Test]
    public void ReadHeader_DuplicateHeaderLookupModesDoNotReportDiagnostics()
    {
        CsvReaderOptions allowOptions = CsvReaderOptions.CreateDefault();
        allowOptions.HeaderOptions.HasHeader = true;
        allowOptions.HeaderOptions.DuplicateHeaderMode = CsvDuplicateHeaderMode.Allow;
        CsvReader allowReader = CsvReader.Create(new StringReader("A,A\n1,2"), allowOptions);

        Assert.That(allowReader.ReadHeader(), Is.True);
        Assert.That(allowReader.GetHeaderIndex("A"), Is.EqualTo(0));
        Assert.That(allowReader.GetDiagnostics().Count(), Is.EqualTo(0));

        CsvReaderOptions firstWinsOptions = CsvReaderOptions.CreateDefault();
        firstWinsOptions.HeaderOptions.HasHeader = true;
        firstWinsOptions.HeaderOptions.DuplicateHeaderMode = CsvDuplicateHeaderMode.FirstWins;
        CsvReader firstWinsReader = CsvReader.Create(new StringReader("A,A\n1,2"), firstWinsOptions);

        Assert.That(firstWinsReader.ReadHeader(), Is.True);
        Assert.That(firstWinsReader.GetHeaderIndex("A"), Is.EqualTo(0));
        Assert.That(firstWinsReader.GetDiagnostics().Count(), Is.EqualTo(0));

        CsvReaderOptions lastWinsOptions = CsvReaderOptions.CreateDefault();
        lastWinsOptions.HeaderOptions.HasHeader = true;
        lastWinsOptions.HeaderOptions.DuplicateHeaderMode = CsvDuplicateHeaderMode.LastWins;
        CsvReader lastWinsReader = CsvReader.Create(new StringReader("A,A\n1,2"), lastWinsOptions);

        Assert.That(lastWinsReader.ReadHeader(), Is.True);
        Assert.That(lastWinsReader.GetHeaderIndex("A"), Is.EqualTo(1));
        Assert.That(lastWinsReader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void ReadHeader_MissingHeaderErrorUsesHeaderRowPosition()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.MissingHeaderMode = CsvMissingHeaderMode.Error;
        CsvReader reader = CsvReader.Create(new StringReader("A,,C\n1,2,3"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "A", "", "C" }));
        Assert.That(reader.GetHeaders().Diagnostics.Count(), Is.EqualTo(1));
        Assert.That(reader.GetHeaders().Diagnostics.Get(0).Code, Is.EqualTo("MissingHeader"));
        Assert.That(reader.GetHeaders().Diagnostics.Get(0).Row, Is.EqualTo(1));
        Assert.That(reader.GetHeaders().Diagnostics.Get(0).Column, Is.EqualTo(1));
    }

    [Test]
    public void ReadHeader_MissingHeaderGenerateAndEmptyModesResolveNames()
    {
        CsvReaderOptions generateOptions = CsvReaderOptions.CreateDefault();
        generateOptions.HeaderOptions.HasHeader = true;
        generateOptions.HeaderOptions.MissingHeaderMode = CsvMissingHeaderMode.Generate;
        generateOptions.HeaderOptions.GeneratedHeaderPrefix = "Field";
        CsvReader generateReader = CsvReader.Create(new StringReader("A,,C\n1,2,3"), generateOptions);

        Assert.That(generateReader.ReadHeader(), Is.True);
        Assert.That(generateReader.GetHeaders().Names, Is.EqualTo(new string[] { "A", "Field2", "C" }));
        Assert.That(generateReader.GetDiagnostics().Count(), Is.EqualTo(0));

        CsvReaderOptions emptyOptions = CsvReaderOptions.CreateDefault();
        emptyOptions.HeaderOptions.HasHeader = true;
        emptyOptions.HeaderOptions.MissingHeaderMode = CsvMissingHeaderMode.Empty;
        CsvReader emptyReader = CsvReader.Create(new StringReader("A,,C\n1,2,3"), emptyOptions);

        Assert.That(emptyReader.ReadHeader(), Is.True);
        Assert.That(emptyReader.GetHeaders().Names, Is.EqualTo(new string[] { "A", "", "C" }));
        Assert.That(emptyReader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_MissingFieldEmptyModePadsEmptyValues()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Empty;
        CsvReader reader = CsvReader.Create(new StringReader("A,B,C\n1,2"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "1", "2", "" }));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_MissingFieldNullModePadsNullValues()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Null;
        CsvReader reader = CsvReader.Create(new StringReader("A,B,C\n1,2"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields.Length, Is.EqualTo(3));
        Assert.That(reader.GetCurrentRow().Fields[0], Is.EqualTo("1"));
        Assert.That(reader.GetCurrentRow().Fields[1], Is.EqualTo("2"));
        Assert.That(reader.GetCurrentRow().Fields[2], Is.Null);
    }

    [Test]
    public void Read_MissingFieldErrorModeReportsDiagnostics()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Error;
        CsvReader reader = CsvReader.Create(new StringReader("A,B,C\n1,2"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "1", "2" }));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MissingField"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().Diagnostics.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Read_MissingFieldErrorModeReportsEveryMissingField()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Error;
        CsvReader reader = CsvReader.Create(new StringReader("A,B,C,D\n1,2"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("MissingField"));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(1).Code, Is.EqualTo("MissingField"));
        Assert.That(reader.GetDiagnostics().Get(1).Column, Is.EqualTo(3));
        Assert.That(reader.GetCurrentRow().Diagnostics.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Read_ExtraFieldAllowModeKeepsAllFields()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.ExtraFieldMode = CsvExtraFieldMode.Allow;
        CsvReader reader = CsvReader.Create(new StringReader("A,B\n1,2,3"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "1", "2", "3" }));
        Assert.That(reader.GetFieldByName("B"), Is.EqualTo("2"));
        Assert.That(reader.GetField(2), Is.EqualTo("3"));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_ExtraFieldIgnoreModeTruncatesFields()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.ExtraFieldMode = CsvExtraFieldMode.Ignore;
        CsvReader reader = CsvReader.Create(new StringReader("A,B\n1,2,3"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "1", "2" }));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }

    [Test]
    public void Read_ExtraFieldErrorModeReportsDiagnostics()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.ExtraFieldMode = CsvExtraFieldMode.Error;
        CsvReader reader = CsvReader.Create(new StringReader("A,B\n1,2,3"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "1", "2", "3" }));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("ExtraField"));
        Assert.That(reader.GetDiagnostics().Get(0).Row, Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().Diagnostics.Count(), Is.EqualTo(1));
    }

    [Test]
    public void Read_ExtraFieldErrorModeReportsEveryExtraField()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.HasHeader = true;
        options.HeaderOptions.ExtraFieldMode = CsvExtraFieldMode.Error;
        CsvReader reader = CsvReader.Create(new StringReader("A\n1,2,3"), options);

        Assert.That(reader.ReadHeader(), Is.True);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(2));
        Assert.That(reader.GetDiagnostics().Get(0).Code, Is.EqualTo("ExtraField"));
        Assert.That(reader.GetDiagnostics().Get(0).Column, Is.EqualTo(1));
        Assert.That(reader.GetDiagnostics().Get(1).Code, Is.EqualTo("ExtraField"));
        Assert.That(reader.GetDiagnostics().Get(1).Column, Is.EqualTo(2));
        Assert.That(reader.GetCurrentRow().Diagnostics.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Read_GeneratedHeadersDoNotApplyFieldCountPoliciesToLaterRows()
    {
        CsvReaderOptions options = CsvReaderOptions.CreateDefault();
        options.HeaderOptions.MissingFieldMode = CsvMissingFieldMode.Error;
        options.HeaderOptions.ExtraFieldMode = CsvExtraFieldMode.Error;
        CsvReader reader = CsvReader.Create(new StringReader("a\nb,c"), options);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetHeaders().Names, Is.EqualTo(new string[] { "Column1" }));
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a" }));

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "b", "c" }));
        Assert.That(reader.GetDiagnostics().Count(), Is.EqualTo(0));
    }
}
