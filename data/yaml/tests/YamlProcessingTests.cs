using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlProcessingTests
{
    [Test]
    public void Processing_PathNavigationAndEditing_UpdateNestedMappingsAndSequences()
    {
        global::yaml.YamlNode root = BuildUsersGraph();

        bool renamed = root.UpdatePath("users[0].name", global::yaml.YamlNodeFactory.Scalar("Augusta"));
        bool promoted = root.ReplacePath("users[1].roles[0]", global::yaml.YamlNodeFactory.Scalar("lead"));
        bool removedRole = root.RemovePath("users[0].roles[1]");
        bool removedRoot = root.RemovePath("$");
        bool missing = root.UpdatePath("users[2].name", global::yaml.YamlNodeFactory.Scalar("missing"));

        Assert.That(renamed, Is.True);
        Assert.That(promoted, Is.True);
        Assert.That(removedRole, Is.True);
        Assert.That(removedRoot, Is.False);
        Assert.That(missing, Is.False);
        Assert.That(root.FindPath("users[0].name")!.Value, Is.EqualTo("Augusta"));
        Assert.That(root.FindPath("users[0].roles[0]")!.Value, Is.EqualTo("admin"));
        Assert.That(root.FindPath("users[0].roles[1]"), Is.Null);
        Assert.That(root.FindPath("users[1].roles[0]")!.Value, Is.EqualTo("lead"));
    }

    [Test]
    public void Processing_CloneAndAliasRename_IsolateClonedGraphFromOriginal()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode settings = global::yaml.YamlNodeFactory.Mapping();
        settings.Anchor = "settings";
        settings.AddMappingValue("mode", global::yaml.YamlNodeFactory.Scalar("base"));
        global::yaml.YamlNode alias = global::yaml.YamlNodeFactory.AliasTo("settings", settings);
        root.AddMappingValue("settings", settings);
        root.AddMappingValue("copy", alias);

        global::yaml.YamlNode clone = root.Clone();
        global::yaml.YamlNode clonedSettings = clone.FindPath("settings")!;
        global::yaml.YamlNode clonedAlias = clone.FindPath("copy")!;
        int renamed = clone.RenameAnchorAndAliases("settings", "clone-settings");
        bool updatedClone = clonedSettings.UpdateMappingValue("mode", global::yaml.YamlNodeFactory.Scalar("clone"));

        Assert.That(renamed, Is.EqualTo(2));
        Assert.That(updatedClone, Is.True);
        Assert.That(settings.Anchor, Is.EqualTo("settings"));
        Assert.That(alias.AliasName, Is.EqualTo("settings"));
        Assert.That(settings.FindMappingValue("mode")!.Value, Is.EqualTo("base"));
        Assert.That(clonedSettings.Anchor, Is.EqualTo("clone-settings"));
        Assert.That(clonedAlias.AliasName, Is.EqualTo("clone-settings"));
        Assert.That(clonedAlias.AliasTarget, Is.SameAs(clonedSettings));
        Assert.That(clonedSettings.FindMappingValue("mode")!.Value, Is.EqualTo("clone"));
    }

    [Test]
    public void Processing_AliasRetargeting_UpdatesNestedAliasesWithoutChangingUnmatchedNames()
    {
        global::yaml.YamlNode root = global::yaml.YamlNodeFactory.Mapping();
        global::yaml.YamlNode oldTarget = global::yaml.YamlNodeFactory.WithAnchor(global::yaml.YamlNodeFactory.Scalar("old"), "old");
        global::yaml.YamlNode newTarget = global::yaml.YamlNodeFactory.WithAnchor(global::yaml.YamlNodeFactory.Scalar("new"), "new");
        global::yaml.YamlNode first = global::yaml.YamlNodeFactory.AliasTo("old", oldTarget);
        global::yaml.YamlNode second = global::yaml.YamlNodeFactory.AliasTo("old", oldTarget);
        global::yaml.YamlNode unrelated = global::yaml.YamlNodeFactory.AliasTo("other", oldTarget);
        global::yaml.YamlNode sequence = global::yaml.YamlNodeFactory.Sequence();
        sequence.AddSequenceItem(second);
        sequence.AddSequenceItem(unrelated);
        root.AddMappingValue("old", oldTarget);
        root.AddMappingValue("new", newTarget);
        root.AddMappingValue("first", first);
        root.AddMappingValue("nested", sequence);

        int changed = root.RetargetAliases("old", newTarget);

        Assert.That(changed, Is.EqualTo(2));
        Assert.That(first.AliasName, Is.EqualTo("new"));
        Assert.That(second.AliasName, Is.EqualTo("new"));
        Assert.That(unrelated.AliasName, Is.EqualTo("other"));
        Assert.That(first.AliasTarget, Is.SameAs(newTarget));
        Assert.That(second.AliasTarget, Is.SameAs(newTarget));
        Assert.That(unrelated.AliasTarget, Is.SameAs(oldTarget));
    }

    [Test]
    public void Processing_MergeKeysMaterializeBeforeJsonConversion()
    {
        Dictionary<string, object> defaultValues = new Dictionary<string, object>();
        defaultValues["enabled"] = true;
        defaultValues["retries"] = 3;
        defaultValues["region"] = "us";
        global::yaml.YamlNode defaults = global::yaml.YamlNodeFactory.FromJsonValue(defaultValues);
        defaults.Anchor = "defaults";

        Dictionary<string, object> itemValues = new Dictionary<string, object>();
        itemValues["enabled"] = false;
        global::yaml.YamlNode item = global::yaml.YamlNodeFactory.FromJsonValue(itemValues);
        item.InsertMappingPair(0, global::yaml.YamlNodeFactory.Pair(
            global::yaml.YamlNodeFactory.Scalar("<<"),
            global::yaml.YamlNodeFactory.AliasTo("defaults", defaults)));

        int changed = item.ApplyMergeKeys();
        Dictionary<string, object> json = (Dictionary<string, object>)item.ToJsonValue()!;

        Assert.That(changed, Is.EqualTo(3));
        Assert.That(item.FindMappingPair("<<"), Is.Null);
        Assert.That(item.FindMappingValue("enabled")!.Value, Is.EqualTo("false"));
        Assert.That(item.FindMappingValue("retries"), Is.Not.SameAs(defaults.FindMappingValue("retries")));
        Assert.That(json["enabled"], Is.EqualTo(false));
        Assert.That(json["retries"], Is.EqualTo(3L));
        Assert.That(json["region"], Is.EqualTo("us"));
    }

    static global::yaml.YamlNode BuildUsersGraph()
    {
        Dictionary<string, object> first = new Dictionary<string, object>();
        List<object> firstRoles = new List<object>();
        firstRoles.Add("admin");
        firstRoles.Add("writer");
        first["name"] = "Ada";
        first["roles"] = firstRoles;

        Dictionary<string, object> second = new Dictionary<string, object>();
        List<object> secondRoles = new List<object>();
        secondRoles.Add("viewer");
        second["name"] = "Grace";
        second["roles"] = secondRoles;

        List<object> users = new List<object>();
        users.Add(first);
        users.Add(second);

        Dictionary<string, object> root = new Dictionary<string, object>();
        root["users"] = users;
        return global::yaml.YamlNodeFactory.FromJsonValue(root);
    }
}
