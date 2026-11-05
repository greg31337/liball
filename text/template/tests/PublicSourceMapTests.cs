using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PublicSourceMapTests
{
    [Test]
    public void BufferedAndStreamingRender_ProduceExactEquivalentMappingsAcrossComposition()
    {
        TemplateRegistry registry = CreateCompositionRegistry();
        TemplateRenderer bufferedRenderer = TemplateRenderer.Create(registry);
        TemplateSourceMapCollector bufferedCollector = new TemplateSourceMapCollector();
        bufferedRenderer.SetSourceMapCollector(bufferedCollector);
        string templateText = "A{{ name }}B{% include \"part\" %}{% partial \"badge\" %}";

        string buffered = bufferedRenderer.RenderToString(templateText, CreateModel());
        List<TemplateSourceMapEntry> bufferedMappings = bufferedRenderer.GetLastSourceMap();

        TemplateRenderer streamingRenderer = TemplateRenderer.Create(registry);
        TemplateSourceMapCollector streamingCollector = new TemplateSourceMapCollector();
        streamingRenderer.SetSourceMapCollector(streamingCollector);
        StringWriter writer = new StringWriter();
        streamingRenderer.RenderToWriter(templateText, CreateModel(), writer);
        List<TemplateSourceMapEntry> streamingMappings = streamingCollector.GetMappings();

        Assert.That(buffered, Is.EqualTo("AAdaBIAdaP"));
        Assert.That(writer.ToString(), Is.EqualTo(buffered));
        AssertExactMappings(bufferedMappings);
        AssertEquivalentMappings(streamingMappings, bufferedMappings);
        AssertEquivalentMappings(bufferedCollector.GetMappings(), bufferedMappings);
        Assert.That(bufferedCollector.GetMappingAt(2)!.SourceSpan.GetText(), Is.EqualTo("name"));
        Assert.That(bufferedCollector.GetMappingAt(buffered.Length), Is.Null);
    }

    [Test]
    public void SourceMap_TracksMacroBodiesAndInheritedTemplateOverrides()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("layout", "L[{% block body %}base{% endblock %}]R");
        TemplateRenderer renderer = TemplateRenderer.Create(registry);
        string templateText = "{% extends \"layout\" %}{% block body %}{{ greet(\"X\") }}{% endblock %}{% macro greet value %}M{{ value }}{% endmacro %}";

        string output = renderer.RenderToString(templateText, CreateModel());
        List<TemplateSourceMapEntry> mappings = renderer.GetLastSourceMap();

        Assert.That(output, Is.EqualTo("L[MX]R"));
        Assert.That(mappings.Count, Is.EqualTo(4));
        AssertMapping(mappings[0], 0, 2, "layout", "L[");
        AssertMapping(mappings[1], 2, 1, "(anonymous)", "M");
        AssertMapping(mappings[2], 3, 1, "(anonymous)", "value");
        AssertMapping(mappings[3], 4, 2, "layout", "]R");
    }

    [Test]
    public async Task AsyncRender_PreservesExactMappingsForLoadedTemplates()
    {
        TemplateRenderer renderer = TemplateRenderer.Create(CreateCompositionRegistry());

        string output = await renderer.RenderToStringAsync("A{{ name }}B{% include \"part\" %}{% partial \"badge\" %}", CreateModel());
        List<TemplateSourceMapEntry> mappings = renderer.GetLastSourceMap();

        Assert.That(output, Is.EqualTo("AAdaBIAdaP"));
        AssertExactMappings(mappings);
    }

    [Test]
    public void WhitespaceTransforms_KeepExactChunkOriginsAndOutputRanges()
    {
        TemplateOptions options = new TemplateOptions();
        options.WhitespaceMode = TemplateWhitespaceMode.NormalizeCarriageReturnLineFeeds;
        TemplateRenderer renderer = TemplateRenderer.Create(options);

        string output = renderer.RenderToString("A\n{{ name }}\nB", CreateModel());
        List<TemplateSourceMapEntry> mappings = renderer.GetLastSourceMap();

        Assert.That(output, Is.EqualTo("A\r\nAda\r\nB"));
        Assert.That(mappings.Count, Is.EqualTo(3));
        AssertMapping(mappings[0], 0, 3, "(anonymous)", "A\n");
        AssertMapping(mappings[1], 3, 3, "(anonymous)", "name");
        AssertMapping(mappings[2], 6, 3, "(anonymous)", "\nB");

        StringWriter writer = new StringWriter();
        renderer.RenderToWriter("A   {{- name -}}   B", CreateModel(), writer);
        mappings = renderer.GetLastSourceMap();

        Assert.That(writer.ToString(), Is.EqualTo("AAdaB"));
        Assert.That(mappings.Count, Is.EqualTo(3));
        AssertMapping(mappings[0], 0, 1, "(anonymous)", "A   ");
        AssertMapping(mappings[1], 1, 3, "(anonymous)", "name");
        AssertMapping(mappings[2], 4, 1, "(anonymous)", "   B");
    }

    [Test]
    public void Collector_ReturnsDefensiveListsLooksUpOffsetsAndClears()
    {
        TemplateSource source = new TemplateSource("sample", null, null, null, "abc");
        TemplateSourceMapEntry first = new TemplateSourceMapEntry(0, 2, new TemplateSourceSpan(source, 0, 2));
        ITemplateSourceMapCollector collector = new TemplateSourceMapCollector();

        collector.Report(first);
        List<TemplateSourceMapEntry> copy = collector.GetMappings();
        copy.Clear();

        Assert.That(collector.GetMappings(), Is.EqualTo(new TemplateSourceMapEntry[] { first }));
        Assert.That(collector.GetMappingAt(0), Is.SameAs(first));
        Assert.That(collector.GetMappingAt(2), Is.Null);
        collector.Clear();
        Assert.That(collector.GetMappings(), Is.Empty);
        Assert.Throws<ArgumentNullException>(() => collector.Report(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => collector.GetMappingAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemplateSourceMapEntry(-1, 0, first.SourceSpan));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemplateSourceMapEntry(0, -1, first.SourceSpan));
        Assert.Throws<ArgumentNullException>(() => new TemplateSourceMapEntry(0, 0, null!));
    }

    static TemplateRegistry CreateCompositionRegistry()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("part", "I{{ name }}");
        registry.RegisterPartial("badge", "P");
        return registry;
    }

    static TemplateValue CreateModel()
    {
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        values["name"] = TemplateValue.FromString("Ada");
        return TemplateValue.FromMap(values);
    }

    static void AssertExactMappings(List<TemplateSourceMapEntry> mappings)
    {
        Assert.That(mappings.Count, Is.EqualTo(6));
        AssertMapping(mappings[0], 0, 1, "(anonymous)", "A");
        AssertMapping(mappings[1], 1, 3, "(anonymous)", "name");
        AssertMapping(mappings[2], 4, 1, "(anonymous)", "B");
        AssertMapping(mappings[3], 5, 1, "part", "I");
        AssertMapping(mappings[4], 6, 3, "part", "name");
        AssertMapping(mappings[5], 9, 1, "badge", "P");
    }

    static void AssertMapping(TemplateSourceMapEntry entry, int start, int length, string templateName, string sourceText)
    {
        Assert.That(entry.OutputStart, Is.EqualTo(start));
        Assert.That(entry.OutputLength, Is.EqualTo(length));
        Assert.That(entry.OutputEnd, Is.EqualTo(start + length));
        Assert.That(entry.TemplateName, Is.EqualTo(templateName));
        Assert.That(entry.SourceSpan.GetText(), Is.EqualTo(sourceText));
    }

    static void AssertEquivalentMappings(List<TemplateSourceMapEntry> actual, List<TemplateSourceMapEntry> expected)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count));
        int index = 0;
        while (index < expected.Count)
        {
            AssertMapping(actual[index], expected[index].OutputStart, expected[index].OutputLength, expected[index].TemplateName, expected[index].SourceSpan.GetText());
            index = index + 1;
        }
    }
}
