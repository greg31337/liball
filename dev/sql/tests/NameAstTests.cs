using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class NameAstTests
{
    [Test]
    public void NamePart_StoresTextSourceSpanAndQuoteStyle()
    {
        SqlTextSpan span = new SqlTextSpan(4, 10);
        SqlNamePart part = SqlNamePart.Create("order id", "\"order id\"", span, SqlNameQuoteStyle.DoubleQuote);

        Assert.That(part.Text, Is.EqualTo("order id"));
        Assert.That(part.SourceText, Is.EqualTo("\"order id\""));
        Assert.That(part.Span, Is.SameAs(span));
        Assert.That(part.QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
        Assert.That(part.IsQuoted(), Is.True);
        Assert.That(part.HasToken(), Is.False);
    }

    [Test]
    public void NamePart_StoresOptionalToken()
    {
        SqlToken token = SqlToken.Create(SqlTokenKind.Identifier, new SqlTextSpan(0, 5), "users");
        SqlNamePart part = SqlNamePart.Create("users", "users", token.Span, SqlNameQuoteStyle.None, token);

        Assert.That(part.HasToken(), Is.True);
        Assert.That(part.Token, Is.SameAs(token));
        Assert.That(part.IsQuoted(), Is.False);
    }

    [Test]
    public void Name_StoresPartsAndSpan()
    {
        SqlNamePart schema = SqlNamePart.Create("main", "main", new SqlTextSpan(0, 4), SqlNameQuoteStyle.None);
        SqlNamePart table = SqlNamePart.Create("select", "[select]", new SqlTextSpan(5, 8), SqlNameQuoteStyle.Bracket);
        SqlTextSpan span = new SqlTextSpan(0, 13);
        SqlName name = new SqlName(new SqlNamePart[] { schema, table }, span);

        Assert.That(name.IsEmpty(), Is.False);
        Assert.That(name.HasQualifier(), Is.True);
        Assert.That(name.GetPartCount(), Is.EqualTo(2));
        Assert.That(name.GetPartAt(0), Is.SameAs(schema));
        Assert.That(name.GetPartAt(1), Is.SameAs(table));
        Assert.That(name.GetLastPart(), Is.SameAs(table));
        Assert.That(name.GetText(), Is.EqualTo("main.select"));
        Assert.That(name.GetSourceText(), Is.EqualTo("main.[select]"));
        Assert.That(name.Span, Is.SameAs(span));
    }

    [Test]
    public void Name_AllowsEmptyPartListForMissingNames()
    {
        SqlName name = new SqlName(new SqlNamePart[0], new SqlTextSpan(3, 0));

        Assert.That(name.IsEmpty(), Is.True);
        Assert.That(name.HasQualifier(), Is.False);
        Assert.That(name.GetPartCount(), Is.EqualTo(0));
        Assert.That(name.GetText(), Is.EqualTo(""));
        Assert.That(name.GetSourceText(), Is.EqualTo(""));
    }

    [Test]
    public void Name_QuoteStyleValuesAreStable()
    {
        Assert.That((int)SqlNameQuoteStyle.None, Is.EqualTo(0));
        Assert.That((int)SqlNameQuoteStyle.DoubleQuote, Is.EqualTo(1));
        Assert.That((int)SqlNameQuoteStyle.Bracket, Is.EqualTo(2));
        Assert.That((int)SqlNameQuoteStyle.Backtick, Is.EqualTo(3));
        Assert.That((int)SqlNameQuoteStyle.SingleQuote, Is.EqualTo(4));
    }

    [Test]
    public void Name_RejectsNullAndMismatchedInputs()
    {
        Assert.Throws<ArgumentNullException>(() => SqlNamePart.Create(null!, "x", new SqlTextSpan(0, 1), SqlNameQuoteStyle.None));
        Assert.Throws<ArgumentNullException>(() => SqlNamePart.Create("x", null!, new SqlTextSpan(0, 1), SqlNameQuoteStyle.None));
        Assert.Throws<ArgumentNullException>(() => SqlNamePart.Create("x", "x", null!, SqlNameQuoteStyle.None));
        Assert.Throws<ArgumentNullException>(() => SqlNamePart.Create("x", "x", new SqlTextSpan(0, 1), SqlNameQuoteStyle.None, null!));
        Assert.Throws<ArgumentException>(() => SqlNamePart.Create("x", "\"x\"", new SqlTextSpan(0, 1), SqlNameQuoteStyle.DoubleQuote));
        Assert.Throws<ArgumentNullException>(() => new SqlName(null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlName(new SqlNamePart[] { null! }, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => new SqlName(new SqlNamePart[0], null!));
    }
}
