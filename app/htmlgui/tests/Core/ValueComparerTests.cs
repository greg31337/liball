using htmlgui;

namespace htmlgui.Tests;

[TestFixture]
public class ValueComparerTests
{
    [Test]
    public void TextComparisonIsOrdinalAndNullAware()
    {
        Assert.That(ValueComparer.TextEquals("file", "file"), Is.True);
        Assert.That(ValueComparer.TextEquals("file", "FILE"), Is.False);
        Assert.That(ValueComparer.TextEquals(null, null), Is.True);
        Assert.That(ValueComparer.CompareText(null, "a"), Is.LessThan(0));
        Assert.That(ValueComparer.CompareText("Z", "a"), Is.LessThan(0));
        Assert.That(ValueComparer.Ordinal.Equals("i", "I"), Is.False);
    }

    [Test]
    public void IgnoreCaseComparisonUsesOrdinalRules()
    {
        Assert.That(ValueComparer.TextEqualsIgnoreCase("file", "FILE"), Is.True);
        Assert.That(ValueComparer.TextEqualsIgnoreCase("i", "\u0130"), Is.False);
        Assert.That(ValueComparer.CompareTextIgnoreCase("a", "B"), Is.LessThan(0));
        Assert.That(ValueComparer.OrdinalIgnoreCase.Equals("key", "KEY"), Is.True);
    }

    [Test]
    public void SortTextReturnsOrdinallySortedCopy()
    {
        string[] source = new string[] { "a", "Z", "z", "A" };

        string[] result = ValueComparer.SortText(source);

        Assert.That(result, Is.EqualTo(new string[] { "A", "Z", "a", "z" }));
        Assert.That(source, Is.EqualTo(new string[] { "a", "Z", "z", "A" }));
        Assert.Throws<ArgumentNullException>(() => ValueComparer.SortText(null!));
    }
}
