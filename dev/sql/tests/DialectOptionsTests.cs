using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DialectOptionsTests
{
    [Test]
    public void DialectOptions_DefaultsToGenericBaseBehavior()
    {
        SqlDialectOptions options = new SqlDialectOptions();

        Assert.That(options.Kind, Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(options.Keywords, Is.Not.Null);
        Assert.That(options.CaseInsensitiveKeywords, Is.True);
        Assert.That(options.AllowBareIdentifiers, Is.True);
        Assert.That(options.AllowDoubleQuotedIdentifiers, Is.True);
        Assert.That(options.AllowBracketQuotedIdentifiers, Is.False);
        Assert.That(options.AllowBacktickQuotedIdentifiers, Is.False);
        Assert.That(options.AllowSingleQuotedStrings, Is.True);
        Assert.That(options.AllowDoubleQuotedStrings, Is.False);
        Assert.That(options.AllowBooleanLiterals, Is.True);
        Assert.That(options.AllowCommonTableExpressions, Is.True);
        Assert.That(options.AllowWindowFunctions, Is.True);
        Assert.That(options.AllowReturningClause, Is.False);
        Assert.That(options.AllowUpsertClause, Is.False);
    }

    [Test]
    public void DialectOptions_KeywordLookupUsesBaseTableAndCaseFlag()
    {
        SqlDialectOptions options = new SqlDialectOptions();

        Assert.That(options.IsKeyword("select"), Is.True);
        Assert.That(options.GetCanonicalKeyword("select"), Is.EqualTo("SELECT"));

        options.CaseInsensitiveKeywords = false;

        Assert.That(options.IsKeyword("select"), Is.False);
        Assert.That(options.GetCanonicalKeyword("select"), Is.Null);
        Assert.That(options.IsKeyword("SELECT"), Is.True);
        Assert.That(options.GetCanonicalKeyword("SELECT"), Is.EqualTo("SELECT"));
    }

    [Test]
    public void DialectOptions_IdentifierQuoteLookupReflectsFlags()
    {
        SqlDialectOptions options = new SqlDialectOptions();

        Assert.That(options.IsIdentifierQuoteAllowed('"'), Is.True);
        Assert.That(options.IsIdentifierQuoteAllowed('['), Is.False);
        Assert.That(options.IsIdentifierQuoteAllowed(']'), Is.False);
        Assert.That(options.IsIdentifierQuoteAllowed('`'), Is.False);

        options.AllowBracketQuotedIdentifiers = true;
        options.AllowBacktickQuotedIdentifiers = true;

        Assert.That(options.IsIdentifierQuoteAllowed('['), Is.True);
        Assert.That(options.IsIdentifierQuoteAllowed(']'), Is.True);
        Assert.That(options.IsIdentifierQuoteAllowed('`'), Is.True);
    }

    [Test]
    public void DialectOptions_ParameterPrefixLookupReflectsFlags()
    {
        SqlDialectOptions options = new SqlDialectOptions();

        Assert.That(options.IsParameterPrefixAllowed('?'), Is.True);
        Assert.That(options.IsParameterPrefixAllowed(':'), Is.True);
        Assert.That(options.IsParameterPrefixAllowed('@'), Is.True);
        Assert.That(options.IsParameterPrefixAllowed('$'), Is.True);

        options.AllowAtParameters = false;
        options.AllowDollarParameters = false;

        Assert.That(options.IsParameterPrefixAllowed('@'), Is.False);
        Assert.That(options.IsParameterPrefixAllowed('$'), Is.False);
        Assert.That(options.IsParameterPrefixAllowed('#'), Is.False);
    }

    [Test]
    public void DialectOptions_RejectsNullKeywordLookup()
    {
        SqlDialectOptions options = new SqlDialectOptions();

        Assert.Throws<ArgumentNullException>(() => options.IsKeyword(null!));
        Assert.Throws<ArgumentNullException>(() => options.GetCanonicalKeyword(null!));
    }

    [Test]
    public void DialectOptions_PublicKeywordMethodsSupportCustomDialects()
    {
        SqlDialectOptions options = new SqlDialectOptions();

        options.AddKeyword("qualify");
        options.AddKeywords(new string[] { "sample", "variant" });

        Assert.That(options.GetCanonicalKeyword("QUALIFY"), Is.EqualTo("QUALIFY"));
        Assert.That(options.IsKeyword("sample"), Is.True);
        Assert.That(options.IsKeyword("variant"), Is.True);
        Assert.Throws<ArgumentNullException>(() => options.AddKeyword(null!));
        Assert.Throws<ArgumentNullException>(() => options.AddKeywords(null!));
        Assert.Throws<ArgumentException>(() => options.AddKeyword(" "));
    }
}
