using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class MustacheTemplateConverterTests
{
    [Test]
    public void ConvertParentBlocksToNative_ConvertsBlocksAndParents()
    {
        string converted = MustacheTemplateConverter.ConvertParentBlocksToNative("A {{< layout }} {{$title}}Title{{/title}} {{/layout}} B");

        Assert.That(converted, Is.EqualTo("A {% extends \"layout\" %} {% block \"title\" %}Title{% endblock %}  B"));
    }

    [Test]
    public void ConvertToNative_ConvertsMustacheVariablesAndSections()
    {
        string converted = MustacheTemplateConverter.ConvertToNative("Hello {{name}} {{#items}}{{.}}{{/items}}");

        Assert.That(converted, Is.EqualTo("Hello {{ name }} {% mustacheSection items %}{{ this }}{% endmustacheSection %}"));
    }

    [Test]
    public void ConvertToNative_ConvertsRawOutputPartialsAndDelimiterChanges()
    {
        string converted = MustacheTemplateConverter.ConvertToNative("{{{html}}} {{& html}} {{> card}} {{>*partialName}} {{=<% %>=}}<%name%>");

        Assert.That(converted, Is.EqualTo("{{ html | safe }} {{ html | safe }} {% partial \"card\" %} {% partial partialName %} {{ name }}"));
    }

    [Test]
    public void ConvertParentBlocksToNative_RejectsNullText()
    {
        Assert.Throws<ArgumentNullException>(() => MustacheTemplateConverter.ConvertParentBlocksToNative(null!));
    }
}
