using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class KeywordTableTests
{
    [Test]
    public void KeywordTable_BaseTableContainsCommonSqlKeywords()
    {
        SqlKeywordTable table = SqlKeywordTable.CreateBase();

        Assert.That(table.ContainsKeyword("SELECT"), Is.True);
        Assert.That(table.ContainsKeyword("from"), Is.True);
        Assert.That(table.ContainsKeyword("Where"), Is.True);
        Assert.That(table.ContainsKeyword("JOIN"), Is.True);
        Assert.That(table.ContainsKeyword("null"), Is.True);
    }

    [Test]
    public void KeywordTable_LookupIsCaseInsensitiveAndReturnsCanonicalKeyword()
    {
        SqlKeywordTable table = SqlKeywordTable.CreateBase();

        Assert.That(table.GetCanonicalKeyword("select"), Is.EqualTo("SELECT"));
        Assert.That(table.GetCanonicalKeyword("SeLeCt"), Is.EqualTo("SELECT"));
        Assert.That(table.GetCanonicalKeyword("CURRENT_TIMESTAMP"), Is.EqualTo("CURRENT_TIMESTAMP"));
    }

    [Test]
    public void KeywordTable_ReturnsFalseAndNullForNonKeywords()
    {
        SqlKeywordTable table = SqlKeywordTable.CreateBase();

        Assert.That(table.ContainsKeyword("select1"), Is.False);
        Assert.That(table.ContainsKeyword("excluded"), Is.False);
        Assert.That(table.GetCanonicalKeyword("select1"), Is.Null);
    }

    [Test]
    public void KeywordTable_AddAndAddManyNormalizeKeywords()
    {
        SqlKeywordTable table = SqlKeywordTable.Create();

        table.Add("custom");
        table.Add("CUSTOM");
        table.AddMany(new string[] { "merge", "returning" });

        Assert.That(table.GetCount(), Is.EqualTo(3));
        Assert.That(table.ContainsKeyword("CuStOm"), Is.True);
        Assert.That(table.GetCanonicalKeyword("custom"), Is.EqualTo("CUSTOM"));
        Assert.That(table.GetCanonicalKeyword("MERGE"), Is.EqualTo("MERGE"));
        Assert.That(table.GetCanonicalKeyword("Returning"), Is.EqualTo("RETURNING"));
    }

    [Test]
    public void KeywordTable_RejectsNullAndBlankInputs()
    {
        SqlKeywordTable table = SqlKeywordTable.Create();

        Assert.Throws<ArgumentNullException>(() => SqlKeywordTable.Create(null!));
        Assert.Throws<ArgumentNullException>(() => table.Add(null!));
        Assert.Throws<ArgumentNullException>(() => table.AddMany(null!));
        Assert.Throws<ArgumentNullException>(() => table.ContainsKeyword(null!));
        Assert.Throws<ArgumentNullException>(() => table.GetCanonicalKeyword(null!));
        Assert.Throws<ArgumentException>(() => table.Add(""));
        Assert.Throws<ArgumentException>(() => table.Add("   "));
    }
}
