using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlWriterTests
{
    [Test]
    public void SqlWriter_WritesTextAndSpacesCurrentLine()
    {
        SqlWriter writer = SqlWriter.CreateDefault();

        writer.Write("select");
        writer.Space();
        writer.Space();
        writer.Write("*");

        Assert.That(writer.HasCurrentLine(), Is.True);
        Assert.That(writer.GetCurrentLineText(), Is.EqualTo("select *"));
        Assert.That(writer.GetCurrentLineLength(), Is.EqualTo(8));
        Assert.That(writer.ToSql(), Is.EqualTo("select *"));
    }

    [Test]
    public void SqlWriter_BuffersLinesAndAppliesIndentation()
    {
        SqlWriter writer = SqlWriter.CreateDefault();

        writer.WriteTextLine("select");
        writer.Indent();
        writer.WriteTextLine("id,");
        writer.WriteTextLine("name");
        writer.Dedent();
        writer.Write("from");
        writer.Space();
        writer.Write("items");

        Assert.That(writer.ToSql(), Is.EqualTo("select\n    id,\n    name\nfrom items"));
    }

    [Test]
    public void SqlWriter_UsesCustomIndentationAndSetIndentLevel()
    {
        SqlWriter writer = SqlWriter.Create("  ");

        writer.SetIndentLevel(2);
        writer.Write("where");

        Assert.That(writer.GetIndentLevel(), Is.EqualTo(2));
        Assert.That(writer.GetCurrentLineText(), Is.EqualTo("    where"));
    }

    [Test]
    public void SqlWriter_UsesFormatOptionsForKeywordCaseAndIndentation()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = SqlKeywordCase.Lower;
        options.IndentText = "\t";
        SqlWriter writer = SqlWriter.Create(options);

        writer.Indent();
        writer.Write("SELECT 'FROM' [WHERE] \"JOIN\" `TABLE` FROM items");

        Assert.That(writer.GetCurrentLineText(), Is.EqualTo("\tselect 'FROM' [WHERE] \"JOIN\" `TABLE` from items"));
    }

    [Test]
    public void SqlWriter_WriteRawBypassesKeywordCase()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = SqlKeywordCase.Upper;
        SqlWriter writer = SqlWriter.Create(options);

        writer.WriteRaw("-- select from where");

        Assert.That(writer.ToSql(), Is.EqualTo("-- select from where"));
    }

    [Test]
    public void SqlWriter_PreservesBlankLines()
    {
        SqlWriter writer = SqlWriter.CreateDefault();

        writer.WriteTextLine("select 1");
        writer.BlankLine();
        writer.Write("select 2");

        Assert.That(writer.ToSql(), Is.EqualTo("select 1\n\nselect 2"));
    }

    [Test]
    public void SqlWriter_ClearResetsBufferedLinesAndIndentation()
    {
        SqlWriter writer = SqlWriter.CreateDefault();

        writer.Indent();
        writer.WriteTextLine("select 1");
        writer.Write("select 2");
        writer.Clear();

        Assert.That(writer.ToSql(), Is.EqualTo(""));
        Assert.That(writer.HasCurrentLine(), Is.False);
        Assert.That(writer.GetIndentLevel(), Is.EqualTo(0));
    }

    [Test]
    public void SqlWriter_RejectsInvalidInputs()
    {
        SqlWriter writer = SqlWriter.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => SqlWriter.Create((string)null!));
        Assert.Throws<ArgumentNullException>(() => SqlWriter.Create((SqlFormatOptions)null!));
        Assert.Throws<ArgumentNullException>(() => writer.Write(null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteRaw(null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteTextLine(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.SetIndentLevel(-1));
        Assert.Throws<InvalidOperationException>(() => writer.Dedent());
    }
}
