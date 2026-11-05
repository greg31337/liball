using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogTemplateTests
{
    [Test]
    public void Constructor_CopiesTokensAndCountsPlaceholders()
    {
        LogTemplateToken[] tokens = new LogTemplateToken[]
        {
            LogTemplateToken.Literal("hello ", 0),
            LogTemplateToken.Placeholder("User", 0, 6, 6)
        };

        LogTemplate template = new LogTemplate("hello {User}", tokens);
        tokens[1].Name = "Changed";

        Assert.That(template.OriginalText, Is.EqualTo("hello {User}"));
        Assert.That(template.Tokens.Length, Is.EqualTo(2));
        Assert.That(template.Tokens[1].Name, Is.EqualTo("User"));
        Assert.That(template.PlaceholderCount, Is.EqualTo(1));
        Assert.That(template.InvalidTokenCount, Is.EqualTo(0));
        Assert.That(template.IsValid, Is.True);
    }

    [Test]
    public void Constructor_CountsInvalidTokens()
    {
        LogTemplateToken[] tokens = new LogTemplateToken[]
        {
            LogTemplateToken.Placeholder("bad name", 0, 0, 10),
            null!
        };

        LogTemplate template = new LogTemplate("hello {bad name}", tokens);

        Assert.That(template.Tokens.Length, Is.EqualTo(1));
        Assert.That(template.PlaceholderCount, Is.EqualTo(1));
        Assert.That(template.InvalidTokenCount, Is.EqualTo(2));
        Assert.That(template.IsValid, Is.False);
        Assert.That(template.ErrorMessage, Is.EqualTo("template contains invalid tokens"));
    }

    [Test]
    public void Empty_CreatesValidEmptyTemplate()
    {
        LogTemplate template = LogTemplate.Empty();

        Assert.That(template.OriginalText, Is.EqualTo(""));
        Assert.That(template.Tokens.Length, Is.EqualTo(0));
        Assert.That(template.PlaceholderCount, Is.EqualTo(0));
        Assert.That(template.IsValid, Is.True);
    }

    [Test]
    public void GetPlaceholders_ReturnsPlaceholderSnapshots()
    {
        LogTemplate template = new LogTemplate(
            "hello {User} from {Region}",
            new LogTemplateToken[]
            {
                LogTemplateToken.Literal("hello ", 0),
                LogTemplateToken.Placeholder("User", 0, 6, 6),
                LogTemplateToken.Literal(" from ", 12),
                LogTemplateToken.Placeholder("Region", 1, 18, 8)
            });

        LogTemplateToken[] placeholders = template.GetPlaceholders();
        template.Tokens[1].Name = "Changed";

        Assert.That(placeholders.Length, Is.EqualTo(2));
        Assert.That(placeholders[0].Name, Is.EqualTo("User"));
        Assert.That(placeholders[1].Name, Is.EqualTo("Region"));
    }
}
