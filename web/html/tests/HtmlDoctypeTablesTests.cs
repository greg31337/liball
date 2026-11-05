
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDoctypeTablesTests
{
    [Test]
    public void RecognizesExactAndPrefixQuirksIdentifiersIgnoringAsciiCase()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlDoctypeTables.IsQuirksPublicIdentifier("html", false), Is.True);
            Assert.That(HtmlDoctypeTables.IsQuirksPublicIdentifier(
                "-//ietf//dtd html 2.0//en", false), Is.True);
            Assert.That(HtmlDoctypeTables.IsQuirksPublicIdentifier(
                "-//W3C//DTD HTML 3.2 Finalized//", false), Is.False);
            Assert.That(HtmlDoctypeTables.IsQuirksSystemIdentifier(
                "HTTP://WWW.IBM.COM/DATA/DTD/V11/IBMXHTML1-TRANSITIONAL.DTD"), Is.True);
            Assert.That(HtmlDoctypeTables.IsQuirksSystemIdentifier("about:legacy-compat"), Is.False);
        });
    }

    [Test]
    public void AppliesHtml401SystemIdentifierConditions()
    {
        const string identifier = "-//W3C//DTD HTML 4.01 Transitional//EN";

        Assert.Multiple(() =>
        {
            Assert.That(HtmlDoctypeTables.IsQuirksPublicIdentifier(identifier, true), Is.True);
            Assert.That(HtmlDoctypeTables.IsQuirksPublicIdentifier(identifier, false), Is.False);
            Assert.That(HtmlDoctypeTables.IsLimitedQuirksPublicIdentifier(identifier, true), Is.True);
            Assert.That(HtmlDoctypeTables.IsLimitedQuirksPublicIdentifier(identifier, false), Is.False);
        });
    }

    [Test]
    public void RecognizesXhtmlLimitedQuirksPrefixes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlDoctypeTables.IsLimitedQuirksPublicIdentifier(
                "-//W3C//DTD XHTML 1.0 Frameset//EN", false), Is.True);
            Assert.That(HtmlDoctypeTables.IsLimitedQuirksPublicIdentifier(
                "-//w3c//dtd xhtml 1.0 transitional//en", true), Is.True);
            Assert.That(HtmlDoctypeTables.IsLimitedQuirksPublicIdentifier(
                "-//W3C//DTD XHTML 1.1//EN", true), Is.False);
        });
    }
}
