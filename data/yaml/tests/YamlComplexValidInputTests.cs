using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlComplexValidInputTests
{
    [Test]
    public void ParseAll_ComplexValidStream_ComposesNestedDocumentsAnchorsAliasesAndStyles()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "# deployment",
                "---",
                "defaults: &defaults !settings",
                "  retries: 3",
                "  labels: [core, api]",
                "services:",
                "  - name: api",
                "    enabled: true",
                "    config:",
                "      <<: *defaults",
                "      ports: [80, 443]",
                "      notes: |",
                "        keep warm",
                "        rotate slowly",
                "...",
                "---",
                "summary: {services: 1, ok: true}",
                ""
            });

        global::yaml.YamlStream stream = global::yaml.Yaml.ParseAll(text);
        global::yaml.YamlDocument first = stream.Documents[0];
        global::yaml.YamlNode root = first.Root!;
        global::yaml.YamlNode defaults = root.FindMappingValue("defaults")!;
        global::yaml.YamlNode services = root.FindMappingValue("services")!;
        global::yaml.YamlNode service = services.Children[0];
        global::yaml.YamlNode config = service.FindMappingValue("config")!;
        global::yaml.YamlNode merge = config.FindMappingValue("<<")!;
        global::yaml.YamlNode ports = config.FindMappingValue("ports")!;
        global::yaml.YamlNode notes = config.FindMappingValue("notes")!;
        global::yaml.YamlNode summary = stream.Documents[1].Root!.FindMappingValue("summary")!;

        Assert.That(stream.DocumentCount(), Is.EqualTo(2));
        Assert.That(first.LeadingComments, Is.EqualTo(new List<string> { "# deployment" }));
        Assert.That(defaults.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(defaults.Anchor, Is.EqualTo("defaults"));
        Assert.That(defaults.Tag, Is.EqualTo("!settings"));
        Assert.That(first.Anchors.Find("defaults"), Is.SameAs(defaults));
        Assert.That(defaults.FindMappingValue("retries")!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.IntTag));
        Assert.That(defaults.FindMappingValue("labels")!.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(services.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Sequence));
        Assert.That(service.FindMappingValue("enabled")!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
        Assert.That(merge.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Alias));
        Assert.That(merge.AliasName, Is.EqualTo("defaults"));
        Assert.That(merge.AliasTarget, Is.SameAs(defaults));
        Assert.That(ports.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(ports.Children.Select(child => child.Value), Is.EqualTo(new string[] { "80", "443" }));
        Assert.That(notes.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.Literal));
        Assert.That(notes.Value, Is.EqualTo("keep warm\nrotate slowly\n"));
        Assert.That(summary.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(summary.FindMappingValue("ok")!.Tag, Is.EqualTo(global::yaml.YamlScalarResolver.BoolTag));
    }

    [Test]
    public void Load_ComplexValidDocument_ReturnsNestedJsonCompatibleValues()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "name: release",
                "windows:",
                "  - {name: canary, percent: 10, regions: [us-west, eu]}",
                "  - {name: stable, percent: 90, regions: [us-east, ap]}",
                "checks:",
                "  health: true",
                "  threshold: 0.975",
                "  nullable: null",
                ""
            });

        object? value = global::yaml.Yaml.Load(text);
        Dictionary<string, object> root = (Dictionary<string, object>)value!;
        List<object> windows = (List<object>)root["windows"]!;
        Dictionary<string, object> canary = (Dictionary<string, object>)windows[0]!;
        Dictionary<string, object> stable = (Dictionary<string, object>)windows[1]!;
        List<object> canaryRegions = (List<object>)canary["regions"]!;
        Dictionary<string, object> checks = (Dictionary<string, object>)root["checks"]!;

        Assert.That(root["name"], Is.EqualTo("release"));
        Assert.That(canary["name"], Is.EqualTo("canary"));
        Assert.That(canary["percent"], Is.EqualTo(10L));
        Assert.That(canaryRegions, Is.EqualTo(new List<object> { "us-west", "eu" }));
        Assert.That(stable["percent"], Is.EqualTo(90L));
        Assert.That(checks["health"], Is.EqualTo(true));
        Assert.That(checks["threshold"], Is.EqualTo(0.975d));
        Assert.That(checks["nullable"], Is.Null);
    }
}
