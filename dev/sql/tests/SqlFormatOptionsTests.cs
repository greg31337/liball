using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlFormatOptionsTests
{
    [Test]
    public void SqlFormatOptions_DefaultsAreStable()
    {
        SqlFormatOptions options = new SqlFormatOptions();

        Assert.That(options.KeywordCase, Is.EqualTo(SqlKeywordCase.Preserve));
        Assert.That(options.IndentText, Is.EqualTo("    "));
        Assert.That(options.CommaStyle, Is.EqualTo(SqlCommaStyle.Inline));
        Assert.That(options.PreserveComments, Is.False);
        Assert.That(options.KeywordDialect.Kind, Is.EqualTo(SqlDialectKind.SQLite));
    }

    [Test]
    public void SqlFormatOptions_CopyPreservesValues()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = SqlKeywordCase.Lower;
        options.IndentText = "\t";
        options.CommaStyle = SqlCommaStyle.Leading;
        options.PreserveComments = true;
        options.KeywordDialect = GenericDialect.CreateOptions();

        SqlFormatOptions copy = options.Copy();

        Assert.That(copy.KeywordCase, Is.EqualTo(SqlKeywordCase.Lower));
        Assert.That(copy.IndentText, Is.EqualTo("\t"));
        Assert.That(copy.CommaStyle, Is.EqualTo(SqlCommaStyle.Leading));
        Assert.That(copy.PreserveComments, Is.True);
        Assert.That(copy.KeywordDialect.Kind, Is.EqualTo(SqlDialectKind.Generic));
    }

    [Test]
    public void SqlFormatOptions_RejectsInvalidValues()
    {
        SqlFormatOptions options = new SqlFormatOptions();

        options.IndentText = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());

        options = new SqlFormatOptions();
        options.KeywordDialect = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());

        options = new SqlFormatOptions();
        options.KeywordCase = (SqlKeywordCase)100;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());

        options = new SqlFormatOptions();
        options.CommaStyle = (SqlCommaStyle)100;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Test]
    public void SqlFormatOptions_EnumValuesAreStable()
    {
        Assert.That((int)SqlKeywordCase.Preserve, Is.EqualTo(0));
        Assert.That((int)SqlKeywordCase.Upper, Is.EqualTo(1));
        Assert.That((int)SqlKeywordCase.Lower, Is.EqualTo(2));
        Assert.That((int)SqlCommaStyle.Inline, Is.EqualTo(0));
        Assert.That((int)SqlCommaStyle.Leading, Is.EqualTo(1));
    }
}
