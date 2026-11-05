using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class HtmlEscaperTests
{
    [Test]
    public void EscapeText_EscapesAmpersandAngleBracketsAndDoubleQuotes()
    {
        string escaped = HtmlEscaper.EscapeText("<tag title=\"x\">Tom & 'Jerry'</tag>");

        Assert.That(escaped, Is.EqualTo("&lt;tag title=&quot;x&quot;&gt;Tom &amp; 'Jerry'&lt;/tag&gt;"));
    }

    [Test]
    public void EscapeAttribute_EscapesDoubleQuotedAttributeCharacters()
    {
        string escaped = HtmlEscaper.EscapeAttribute("Tom & \"Jerry\" <tag>");

        Assert.That(escaped, Is.EqualTo("Tom &amp; &quot;Jerry&quot; &lt;tag&gt;"));
    }

    [Test]
    public void EscapeAttribute_DoesNotEscapeApostropheByDefault()
    {
        string escaped = HtmlEscaper.EscapeAttribute("Greg's docs");

        Assert.That(escaped, Is.EqualTo("Greg's docs"));
    }

    [Test]
    public void EscapeAttributeWithApostrophes_EscapesApostrophe()
    {
        string escaped = HtmlEscaper.EscapeAttributeWithApostrophes("Greg's \"docs\"");

        Assert.That(escaped, Is.EqualTo("Greg&#39;s &quot;docs&quot;"));
    }

    [Test]
    public void Escape_DoesNotTreatExistingEntitiesAsTrustedHtml()
    {
        string escaped = HtmlEscaper.EscapeText("&amp;");

        Assert.That(escaped, Is.EqualTo("&amp;amp;"));
    }

    [Test]
    public void Escape_PreservesEmptyAndPlainText()
    {
        Assert.That(HtmlEscaper.EscapeText(""), Is.EqualTo(""));
        Assert.That(HtmlEscaper.EscapeText("plain text"), Is.EqualTo("plain text"));
    }

    [Test]
    public void Escape_RejectsNullText()
    {
        Assert.That(() => HtmlEscaper.EscapeText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlEscaper.EscapeAttribute(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlEscaper.EscapeAttributeWithApostrophes(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlEscaper.Escape(null!, true, true), Throws.TypeOf<ArgumentNullException>());
    }
}
