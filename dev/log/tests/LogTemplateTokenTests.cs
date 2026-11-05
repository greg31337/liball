using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogTemplateTokenTests
{
    [Test]
    public void Literal_CreatesLiteralToken()
    {
        LogTemplateToken token = LogTemplateToken.Literal("hello ", 3);

        Assert.That(token.IsLiteral, Is.True);
        Assert.That(token.IsPlaceholder, Is.False);
        Assert.That(token.Text, Is.EqualTo("hello "));
        Assert.That(token.StartIndex, Is.EqualTo(3));
        Assert.That(token.Length, Is.EqualTo(6));
        Assert.That(token.IsValid, Is.True);
    }

    [Test]
    public void Placeholder_CreatesPlaceholderToken()
    {
        LogTemplateToken token = LogTemplateToken.Placeholder("UserId", 0, 6, 8);

        Assert.That(token.IsLiteral, Is.False);
        Assert.That(token.IsPlaceholder, Is.True);
        Assert.That(token.Name, Is.EqualTo("UserId"));
        Assert.That(token.Position, Is.EqualTo(0));
        Assert.That(token.StartIndex, Is.EqualTo(6));
        Assert.That(token.Length, Is.EqualTo(8));
        Assert.That(token.IsValid, Is.True);
    }

    [Test]
    public void PlaceholderFormat_StoresFormatAndHints()
    {
        LogTemplateToken token = LogTemplateToken.PlaceholderFormat("Order", "0000", true, false, 1, 10, 12);

        Assert.That(token.Name, Is.EqualTo("Order"));
        Assert.That(token.Format, Is.EqualTo("0000"));
        Assert.That(token.HasFormat, Is.True);
        Assert.That(token.IsDestructured, Is.True);
        Assert.That(token.IsStringified, Is.False);
    }

    [Test]
    public void Placeholder_RejectsInvalidFieldName()
    {
        LogTemplateToken token = LogTemplateToken.Placeholder("bad name", 0, 0, 10);

        Assert.That(token.IsValid, Is.False);
        Assert.That(token.ErrorMessage, Is.EqualTo("field name can contain only letters, digits, underscores, and dots"));
    }

    [Test]
    public void Placeholder_RejectsConflictingHints()
    {
        LogTemplateToken token = LogTemplateToken.PlaceholderFormat("User", "", true, true, 0, 0, 6);

        Assert.That(token.IsValid, Is.False);
        Assert.That(token.ErrorMessage, Is.EqualTo("placeholder cannot be both destructured and stringified"));
    }

    [Test]
    public void Copy_CreatesIndependentTokenSnapshot()
    {
        LogTemplateToken original = LogTemplateToken.PlaceholderFormat("User", "json", false, true, 2, 4, 8);

        LogTemplateToken copy = LogTemplateToken.Copy(original);
        original.Name = "Changed";

        Assert.That(copy.Name, Is.EqualTo("User"));
        Assert.That(copy.Format, Is.EqualTo("json"));
        Assert.That(copy.IsStringified, Is.True);
    }
}
