using htmlgui;

namespace htmlgui.Tests;

[TestFixture]
public class StableIdTests
{
    [Test]
    public void RootIdsAreStableSafeAndSensitiveToEveryIdentityPart()
    {
        string value = StableId.CreateRoot("app-seed", "Menu Item", "file");

        Assert.That(value, Does.Match("^ui-menu-item-[0-9a-f]{20}$"));
        Assert.That(value, Is.EqualTo(StableId.CreateRoot("app-seed", "Menu Item", "file")));
        Assert.That(value, Is.Not.EqualTo(StableId.CreateRoot("other-seed", "Menu Item", "file")));
        Assert.That(value, Is.Not.EqualTo(StableId.CreateRoot("app-seed", "Button", "file")));
        Assert.That(value, Is.Not.EqualTo(StableId.CreateRoot("app-seed", "Menu Item", "edit")));
    }

    [Test]
    public void ChildIdsIncludeParentIdentity()
    {
        string first = StableId.CreateChild("app-seed", "parent-a", "Text", "label");
        string second = StableId.CreateChild("app-seed", "parent-b", "Text", "label");

        Assert.That(first, Does.Match("^ui-text-[0-9a-f]{20}$"));
        Assert.That(first, Is.Not.EqualTo(second));
    }

    [TestCase("Menu Item", "menu-item")]
    [TestCase("--Menu__Item--", "menu-item")]
    [TestCase("123", "123")]
    [TestCase("\u2603", "node")]
    public void NormalizeKindProducesAsciiIdentifierSegment(string source, string expected)
    {
        Assert.That(StableId.NormalizeKind(source), Is.EqualTo(expected));
    }

    [Test]
    public void IdInputsCannotBeMissingOrWhitespace()
    {
        Assert.Throws<ArgumentNullException>(() => StableId.CreateRoot(null, "node", "key"));
        Assert.Throws<ArgumentException>(() => StableId.CreateRoot("seed", " ", "key"));
        Assert.Throws<ArgumentException>(() => StableId.CreateRoot("seed", "node", ""));
        Assert.Throws<ArgumentException>(() => StableId.CreateChild("seed", "", "node", "key"));
        Assert.Throws<ArgumentNullException>(() => StableId.NormalizeKind(null!));
    }
}
