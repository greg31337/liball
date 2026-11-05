using ttf;

namespace ttf.Tests;

public class FontExceptionsTests
{
    [Test]
    public void DefaultFactoriesUseDeterministicMessages()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                FontFormatException.Create().Message,
                Is.EqualTo("The font data is malformed or structurally invalid."));
            Assert.That(
                FontLimitException.Create().Message,
                Is.EqualTo("Font processing exceeded a configured resource limit."));
            Assert.That(
                FontLoadException.Create().Message,
                Is.EqualTo("The font source could not be loaded."));
            Assert.That(
                FontEmbeddingException.Create().Message,
                Is.EqualTo("The font cannot be embedded under the selected policy."));
        });
    }

    [Test]
    public void MessageFactoriesPreserveMessages()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FontFormatException.Create("format").Message, Is.EqualTo("format"));
            Assert.That(FontLimitException.Create("limit").Message, Is.EqualTo("limit"));
            Assert.That(FontLoadException.Create("load").Message, Is.EqualTo("load"));
            Assert.That(FontEmbeddingException.Create("embedding").Message, Is.EqualTo("embedding"));
        });
    }

    [Test]
    public void InnerExceptionFactoriesPreserveCauseAndMessage()
    {
        Exception cause = new InvalidOperationException("cause");
        FontFormatException format = FontFormatException.Create("format", cause);
        FontLimitException limit = FontLimitException.Create("limit", cause);
        FontLoadException load = FontLoadException.Create("load", cause);
        FontEmbeddingException embedding = FontEmbeddingException.Create("embedding", cause);

        Assert.Multiple(() =>
        {
            Assert.That(format.Message, Is.EqualTo("format"));
            Assert.That(format.InnerException, Is.SameAs(cause));
            Assert.That(limit.Message, Is.EqualTo("limit"));
            Assert.That(limit.InnerException, Is.SameAs(cause));
            Assert.That(load.Message, Is.EqualTo("load"));
            Assert.That(load.InnerException, Is.SameAs(cause));
            Assert.That(embedding.Message, Is.EqualTo("embedding"));
            Assert.That(embedding.InnerException, Is.SameAs(cause));
        });
    }

    [Test]
    public void ExceptionTypesCanBeThrownAndCaughtIndependently()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => throw FontFormatException.Create());
            Assert.Throws<FontLimitException>(() => throw FontLimitException.Create());
            Assert.Throws<FontLoadException>(() => throw FontLoadException.Create());
            Assert.Throws<FontEmbeddingException>(() => throw FontEmbeddingException.Create());
        });
    }
}
