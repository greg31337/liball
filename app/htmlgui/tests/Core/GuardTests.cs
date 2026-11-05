using htmlgui;

namespace htmlgui.Tests;

[TestFixture]
public class GuardTests
{
    [Test]
    public void ArgumentNotNullAcceptsValueAndRejectsNull()
    {
        Assert.DoesNotThrow(() => Guard.ArgumentNotNull(new object(), "value"));
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Guard.ArgumentNotNull(null, "value"))!;
        Assert.That(exception.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void TextGuardsReturnValidValuesAndRejectInvalidValues()
    {
        Assert.That(Guard.ArgumentNotEmpty(" ", "text"), Is.EqualTo(" "));
        Assert.That(Guard.ArgumentNotWhiteSpace(" text ", "text"), Is.EqualTo(" text "));
        Assert.That(Guard.ArgumentMaximumLength("abc", 3, "text"), Is.EqualTo("abc"));

        Assert.That(Assert.Throws<ArgumentNullException>(() => Guard.ArgumentNotEmpty(null, "text"))!.ParamName, Is.EqualTo("text"));
        Assert.That(Assert.Throws<ArgumentException>(() => Guard.ArgumentNotEmpty("", "text"))!.ParamName, Is.EqualTo("text"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => Guard.ArgumentNotWhiteSpace(null, "text"))!.ParamName, Is.EqualTo("text"));
        Assert.That(Assert.Throws<ArgumentException>(() => Guard.ArgumentNotWhiteSpace(" \t", "text"))!.ParamName, Is.EqualTo("text"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => Guard.ArgumentMaximumLength(null, 2, "text"))!.ParamName, Is.EqualTo("text"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Guard.ArgumentMaximumLength("", -1, "text"))!.ParamName, Is.EqualTo("maximumLength"));
        Assert.That(Assert.Throws<ArgumentException>(() => Guard.ArgumentMaximumLength("abc", 2, "text"))!.ParamName, Is.EqualTo("text"));
    }

    [Test]
    public void NumericGuardsCoverBoundariesAndInvalidRanges()
    {
        Assert.That(Guard.ArgumentNonNegative(0, "count"), Is.Zero);
        Assert.That(Guard.ArgumentPositive(1, "count"), Is.EqualTo(1));
        Assert.That(Guard.ArgumentInRange(2, 1, 2, "count"), Is.EqualTo(2));

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Guard.ArgumentNonNegative(-1, "count"))!.ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Guard.ArgumentPositive(0, "count"))!.ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Guard.ArgumentInRange(0, 1, 2, "count"))!.ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => Guard.ArgumentInRange(3, 1, 2, "count"))!.ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentException>(() => Guard.ArgumentInRange(1, 2, 1, "count"))!.ParamName, Is.EqualTo("count"));
    }
}
