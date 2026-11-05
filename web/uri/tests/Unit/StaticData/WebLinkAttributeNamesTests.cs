namespace uri.Tests;

public class WebLinkAttributeNamesTests
{
    [Test]
    public void StandardAttributeNamesMatchRfc8288WireNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WebLinkAttributeNames.Anchor, Is.EqualTo("anchor"));
            Assert.That(WebLinkAttributeNames.HrefLang, Is.EqualTo("hreflang"));
            Assert.That(WebLinkAttributeNames.Media, Is.EqualTo("media"));
            Assert.That(WebLinkAttributeNames.Rel, Is.EqualTo("rel"));
            Assert.That(WebLinkAttributeNames.Rev, Is.EqualTo("rev"));
            Assert.That(WebLinkAttributeNames.Title, Is.EqualTo("title"));
            Assert.That(WebLinkAttributeNames.TitleExtended, Is.EqualTo("title*"));
            Assert.That(WebLinkAttributeNames.Type, Is.EqualTo("type"));
        });
    }
}
