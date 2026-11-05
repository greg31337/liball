using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlModelAnchorContractTests
{
    [Test]
    public void RenameAnchor_UpdatesAliasesAnchorTableAndRoundTripOutput()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlDocument document = global::yaml.Yaml.Parse("target: &old value\nfirst: *old\nnested:\n  - *old\n");
        global::yaml.YamlNode target = model.FindMappingValue(document.Root, "target")!;
        global::yaml.YamlNode first = model.FindMappingValue(document.Root, "first")!;
        global::yaml.YamlNode nestedAlias = model.FindMappingValue(document.Root, "nested")!.Children[0];

        bool renamed = model.RenameAnchor(document, "old", "new");

        Assert.That(renamed, Is.True);
        Assert.That(target.Anchor, Is.EqualTo("new"));
        Assert.That(first.AliasName, Is.EqualTo("new"));
        Assert.That(nestedAlias.AliasName, Is.EqualTo("new"));
        Assert.That(first.AliasTarget, Is.SameAs(target));
        Assert.That(nestedAlias.AliasTarget, Is.SameAs(target));
        Assert.That(model.FindAnchor(document, "old"), Is.Null);
        Assert.That(model.FindAnchor(document, "new"), Is.SameAs(target));

        string emitted = global::yaml.YamlEmitter.EmitDocument(document);
        global::yaml.YamlDocument reparsed = global::yaml.Yaml.Parse(emitted);
        Assert.That(emitted, Does.Contain("&new"));
        Assert.That(emitted, Does.Not.Contain("&old"));
        Assert.That(model.FindMappingValue(reparsed.Root, "first")!.AliasTarget, Is.SameAs(model.FindMappingValue(reparsed.Root, "target")));
    }

    [Test]
    public void RenameAndRetargetAliases_KeepNamesAndTargetsConsistent()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlDocument document = global::yaml.Yaml.Parse("old: &old O\nnew: &new N\na: *old\nb: *old\nexisting: *new\n");
        global::yaml.YamlNode oldTarget = model.FindMappingValue(document.Root, "old")!;
        global::yaml.YamlNode newTarget = model.FindMappingValue(document.Root, "new")!;
        global::yaml.YamlNode first = model.FindMappingValue(document.Root, "a")!;
        global::yaml.YamlNode second = model.FindMappingValue(document.Root, "b")!;

        Assert.That(model.RenameAliases(document, "old", "new"), Is.EqualTo(2));
        Assert.That(first.AliasName, Is.EqualTo("new"));
        Assert.That(first.AliasTarget, Is.SameAs(newTarget));
        Assert.That(second.AliasTarget, Is.SameAs(newTarget));

        Assert.That(model.RenameAlias(document, first, "old"), Is.True);
        Assert.That(first.AliasName, Is.EqualTo("old"));
        Assert.That(first.AliasTarget, Is.SameAs(oldTarget));
        Assert.That(model.RetargetAlias(document, second, oldTarget), Is.True);
        Assert.That(second.AliasName, Is.EqualTo("old"));
        Assert.That(second.AliasTarget, Is.SameAs(oldTarget));

        Assert.That(model.RetargetAliases(document, oldTarget, newTarget), Is.EqualTo(2));
        Assert.That(first.AliasName, Is.EqualTo("new"));
        Assert.That(second.AliasName, Is.EqualTo("new"));
        Assert.That(first.AliasTarget, Is.SameAs(newTarget));
        Assert.That(second.AliasTarget, Is.SameAs(newTarget));

        global::yaml.YamlNode externalAlias = model.CreateAliasNode("old", null);
        global::yaml.YamlNode unanchored = model.CreateScalarNode("outside", global::yaml.YamlScalarStyle.Plain, null);
        Assert.That(model.RenameAlias(document, externalAlias, "new"), Is.False);
        Assert.That(model.RenameAliases(document, "new", "missing"), Is.EqualTo(0));
        Assert.That(model.RetargetAlias(document, first, unanchored), Is.False);
        Assert.That(model.RetargetAliases(document, newTarget, unanchored), Is.EqualTo(0));
    }

    [Test]
    public void RebuildAndResolveAliases_HandleSharedRecursiveAndUnknownReferences()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        global::yaml.YamlNode root = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlNode target = model.CreateScalarNode("value", global::yaml.YamlScalarStyle.Plain, null);
        global::yaml.YamlNode sharedAlias = model.CreateAliasNode("item", null);
        global::yaml.YamlNode selfAlias = model.CreateAliasNode("root", null);
        global::yaml.YamlNode missingAlias = model.CreateAliasNode("missing", null);
        root.Anchor = "root";
        target.Anchor = "item";
        missingAlias.AliasTarget = target;
        model.AddMappingValue(root, "target", target);
        model.AddMappingValue(root, "first", sharedAlias);
        model.AddMappingValue(root, "second", sharedAlias);
        model.AddMappingValue(root, "self", selfAlias);
        model.AddMappingValue(root, "missing", missingAlias);
        root.Children.Add(root);
        document.Root = root;

        int anchors = model.RebuildAnchorTable(document);

        Assert.That(anchors, Is.EqualTo(2));
        Assert.That(model.FindAnchor(document, "root"), Is.SameAs(root));
        Assert.That(model.FindAnchor(document, "item"), Is.SameAs(target));
        Assert.That(model.ResolveAlias(document, sharedAlias), Is.True);
        Assert.That(sharedAlias.AliasTarget, Is.SameAs(target));

        sharedAlias.AliasTarget = null;
        int resolved = model.ResolveAliases(document);

        Assert.That(resolved, Is.EqualTo(2));
        Assert.That(sharedAlias.AliasTarget, Is.SameAs(target));
        Assert.That(selfAlias.AliasTarget, Is.SameAs(root));
        Assert.That(missingAlias.AliasTarget, Is.Null);
        Assert.That(model.ResolveAlias(document, missingAlias), Is.False);
    }

    [Test]
    public void AnchorOperations_RejectConflictsInvalidNamesAndWrongNodeKinds()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlDocument document = global::yaml.Yaml.Parse("first: &first one\nsecond: &second two\nalias: *first\n");
        global::yaml.YamlNode scalar = model.FindMappingValue(document.Root, "first")!;

        Assert.That(model.RenameAnchor(document, "first", "second"), Is.False);
        Assert.That(model.RenameAnchor(document, "first", "bad name"), Is.False);
        Assert.That(model.RenameAnchor(document, "missing", "new"), Is.False);
        Assert.That(model.RenameAnchor(null, "first", "new"), Is.False);
        Assert.That(model.RenameAlias(document, scalar, "second"), Is.False);
        Assert.That(model.ResolveAlias(document, scalar), Is.False);
        Assert.That(model.FindAnchor(null, "first"), Is.Null);
        Assert.That(model.FindAnchor(document, ""), Is.Null);
        Assert.That(model.RebuildAnchorTable(null), Is.EqualTo(0));
        Assert.That(model.ResolveAliases(null), Is.EqualTo(0));
        Assert.That(model.FindAnchor(document, "first"), Is.SameAs(scalar));
    }
}
