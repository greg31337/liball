using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlModelPublicContractTests
{
    [Test]
    public void ModelServices_ConstructEveryPublicModelShape()
    {
        global::yaml.IYamlModel shared = global::yaml.YamlApi.Model;
        global::yaml.IYamlModel concrete = new global::yaml.YamlModel();
        global::yaml.YamlSourceSpan span = new global::yaml.YamlSourceSpan();
        span.StartIndex = 2;
        span.EndIndex = 4;

        global::yaml.YamlResult result = shared.CreateResult();
        global::yaml.YamlTagDirective directive = concrete.CreateTagDirective("!e!", "tag:example/", span);
        global::yaml.YamlNode empty = shared.CreateEmptyNode(span);
        global::yaml.YamlNode scalar = concrete.CreateScalarNode("value", global::yaml.YamlScalarStyle.SingleQuoted, span);
        global::yaml.YamlNode sequence = shared.CreateSequenceNode(global::yaml.YamlCollectionStyle.Flow, span);
        global::yaml.YamlNode mapping = concrete.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, span);
        global::yaml.YamlNode alias = shared.CreateAliasNode("target", span);
        global::yaml.YamlPair pair = concrete.CreatePair(scalar, alias, span);

        Assert.That(shared, Is.TypeOf<global::yaml.YamlModel>());
        Assert.That(concrete, Is.TypeOf<global::yaml.YamlModel>());
        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(directive.Handle, Is.EqualTo("!e!"));
        Assert.That(directive.Prefix, Is.EqualTo("tag:example/"));
        Assert.That(directive.Span, Is.SameAs(span));
        Assert.That(empty.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
        Assert.That(scalar.Value, Is.EqualTo("value"));
        Assert.That(scalar.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.SingleQuoted));
        Assert.That(sequence.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(mapping.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Mapping));
        Assert.That(alias.AliasName, Is.EqualTo("target"));
        Assert.That(pair.Key, Is.SameAs(scalar));
        Assert.That(pair.Value, Is.SameAs(alias));
    }

    [Test]
    public void JsonConversion_RoundTripsValuesAndHandlesGraphCyclesSafely()
    {
        global::yaml.IYamlModel model = new global::yaml.YamlModel();
        Dictionary<string, object?> input = new Dictionary<string, object?>();
        input["name"] = "Ada";
        input["items"] = new object?[] { 1, true, null };

        global::yaml.YamlNode node = model.FromJsonValue(input);
        Dictionary<string, object?> converted = (Dictionary<string, object?>)model.ToJsonValue(node)!;
        List<object?> items = (List<object?>)converted["items"]!;

        Assert.That(converted["name"], Is.EqualTo("Ada"));
        Assert.That(items, Is.EqualTo(new object?[] { 1L, true, null }));
        Assert.That(model.ToJsonValue(null), Is.Null);

        List<object> cycle = new List<object>();
        cycle.Add(cycle);
        Assert.That(() => model.FromJsonValue(cycle), Throws.TypeOf<global::yaml.YamlException>());

        global::yaml.YamlNode nodeCycle = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);
        nodeCycle.Children.Add(nodeCycle);
        List<object?> cycleValue = (List<object?>)model.ToJsonValue(nodeCycle)!;
        Assert.That(cycleValue, Is.EqualTo(new object?[] { null }));
    }

    [Test]
    public void PathOperations_ParseFindReplaceAndRemoveStringAndParsedPaths()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlNode root = PeopleGraph(model);

        global::yaml.YamlPath valid = model.ParsePath("$.users[1].name");
        global::yaml.YamlPath invalidRoot = model.ParsePath("$users");
        global::yaml.YamlPath invalidSeparator = model.ParsePath("users[0]name");
        global::yaml.YamlPath invalidDotIndex = model.ParsePath("users.[0]");

        Assert.That(valid.Valid, Is.True);
        Assert.That(valid.Segments.Count, Is.EqualTo(3));
        Assert.That(invalidRoot.Valid, Is.False);
        Assert.That(invalidSeparator.Valid, Is.False);
        Assert.That(invalidDotIndex.Valid, Is.False);
        Assert.That(model.FindPath(root, "users[0].name")!.Value, Is.EqualTo("Ada"));
        Assert.That(model.FindParsedPath(root, valid)!.Value, Is.EqualTo("Grace"));

        Assert.That(model.ReplacePath(root, "users[0].name", Scalar(model, "Augusta")), Is.True);
        Assert.That(model.ReplaceParsedPath(root, valid, Scalar(model, "Hopper")), Is.True);
        Assert.That(model.FindPath(root, "users[0].name")!.Value, Is.EqualTo("Augusta"));
        Assert.That(model.FindParsedPath(root, valid)!.Value, Is.EqualTo("Hopper"));
        Assert.That(model.RemovePath(root, "users[0].name"), Is.True);
        Assert.That(model.RemoveParsedPath(root, valid), Is.True);
        Assert.That(model.FindPath(root, "users[0].name"), Is.Null);
        Assert.That(model.FindParsedPath(root, valid), Is.Null);
    }

    [Test]
    public void SequenceOperations_CoverEveryEditAndRejectInvalidRequests()
    {
        global::yaml.IYamlModel model = new global::yaml.YamlModel();
        global::yaml.YamlNode sequence = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlNode one = Scalar(model, "one");
        global::yaml.YamlNode two = Scalar(model, "two");
        global::yaml.YamlNode middle = Scalar(model, "middle");

        Assert.That(model.AddSequenceItem(sequence, one), Is.True);
        Assert.That(model.AddSequenceItem(sequence, two), Is.True);
        Assert.That(model.InsertSequenceItem(sequence, 1, middle), Is.True);
        Assert.That(model.SequenceItemCount(sequence), Is.EqualTo(3));
        Assert.That(model.GetSequenceItem(sequence, 1), Is.SameAs(middle));
        Assert.That(model.SetSequenceItem(sequence, 1, Scalar(model, "changed")), Is.True);
        Assert.That(model.RemoveSequenceItem(sequence, 0), Is.True);
        Assert.That(model.GetSequenceItem(sequence, 0)!.Value, Is.EqualTo("changed"));
        Assert.That(model.AddSequenceItem(null, one), Is.False);
        Assert.That(model.InsertSequenceItem(sequence, -1, one), Is.False);
        Assert.That(model.SetSequenceItem(sequence, 9, one), Is.False);
        Assert.That(model.RemoveSequenceItem(sequence, 9), Is.False);
        Assert.That(model.GetSequenceItem(sequence, -1), Is.Null);
        Assert.That(model.SequenceItemCount(one), Is.EqualTo(0));
    }

    [Test]
    public void MappingOperations_CoverOrderedAndScalarKeyedEdits()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlNode mapping = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlPair first = Pair(model, "first", "1");
        global::yaml.YamlPair third = Pair(model, "third", "3");
        global::yaml.YamlPair second = Pair(model, "second", "2");

        Assert.That(model.AddMappingPair(mapping, first), Is.True);
        Assert.That(model.AddMappingPair(mapping, third), Is.True);
        Assert.That(model.InsertMappingPair(mapping, 1, second), Is.True);
        Assert.That(model.MappingPairCount(mapping), Is.EqualTo(3));
        Assert.That(model.GetMappingPair(mapping, 1), Is.SameAs(second));
        Assert.That(model.FindMappingPair(mapping, "third"), Is.SameAs(third));
        Assert.That(model.FindMappingValue(mapping, "first")!.Value, Is.EqualTo("1"));

        global::yaml.YamlPair replacement = Pair(model, "replacement", "changed");
        Assert.That(model.SetMappingPair(mapping, 1, replacement), Is.True);
        Assert.That(model.RemoveMappingPair(mapping, 0), Is.True);
        Assert.That(model.AddMappingValue(mapping, "added", Scalar(model, "value")), Is.True);
        Assert.That(model.SetMappingValue(mapping, "added", Scalar(model, "updated")), Is.True);
        Assert.That(model.FindMappingValue(mapping, "added")!.Value, Is.EqualTo("updated"));
        Assert.That(model.RemoveMappingValue(mapping, "added"), Is.True);
        Assert.That(model.MappingPairCount(mapping), Is.EqualTo(2));

        Assert.That(model.AddMappingPair(null, first), Is.False);
        Assert.That(model.InsertMappingPair(mapping, -1, first), Is.False);
        Assert.That(model.SetMappingPair(mapping, 9, first), Is.False);
        Assert.That(model.RemoveMappingPair(mapping, 9), Is.False);
        Assert.That(model.AddMappingValue(null, "key", first.Value), Is.False);
        Assert.That(model.SetMappingValue(mapping, "missing", first.Value), Is.False);
        Assert.That(model.RemoveMappingValue(mapping, "missing"), Is.False);
        Assert.That(model.GetMappingPair(mapping, -1), Is.Null);
        Assert.That(model.FindMappingPair(null, "key"), Is.Null);
        Assert.That(model.FindMappingValue(mapping, "missing"), Is.Null);
        Assert.That(model.MappingPairCount(first.Value), Is.EqualTo(0));
    }

    [Test]
    public void StyleTagSchemaAndNamingOperations_ApplyRequestedValues()
    {
        global::yaml.IYamlModel model = new global::yaml.YamlModel();
        global::yaml.YamlNode scalar = Scalar(model, "true");
        global::yaml.YamlNode sequence = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);

        Assert.That(model.SetScalarStyle(scalar, global::yaml.YamlScalarStyle.DoubleQuoted), Is.True);
        Assert.That(model.SetCollectionStyle(sequence, global::yaml.YamlCollectionStyle.Flow), Is.True);
        Assert.That(model.SetTag(scalar, "!!str"), Is.True);
        Assert.That(scalar.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.DoubleQuoted));
        Assert.That(sequence.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(scalar.Tag, Is.EqualTo("!!str"));
        Assert.That(model.SetTag(scalar, null), Is.True);
        Assert.That(scalar.Tag, Is.Empty);
        Assert.That(model.SetScalarStyle(sequence, global::yaml.YamlScalarStyle.Plain), Is.False);
        Assert.That(model.SetCollectionStyle(scalar, global::yaml.YamlCollectionStyle.Block), Is.False);
        Assert.That(model.SetTag(null, "!tag"), Is.False);

        Assert.That(model.ResolveScalarTag("37", global::yaml.YamlScalarStyle.Plain, null), Is.EqualTo(global::yaml.YamlTags.Int));
        Assert.That(model.NormalizeStandardTag("!!str"), Is.EqualTo(global::yaml.YamlTags.String));
        Assert.That(model.IsStandardTag(global::yaml.YamlTags.Bool), Is.True);
        Assert.That(model.IsStandardTag("!custom"), Is.False);
        Assert.That(model.ApplyFieldNaming("HTTPServerValue", global::yaml.YamlNamingConvention.CamelCase), Is.EqualTo("httpServerValue"));
        Assert.That(model.ApplyFieldNaming("HTTPServerValue", global::yaml.YamlNamingConvention.PascalCase), Is.EqualTo("HttpServerValue"));
        Assert.That(model.ApplyFieldNaming("HTTPServerValue", global::yaml.YamlNamingConvention.SnakeCase), Is.EqualTo("http_server_value"));
        Assert.That(model.ApplyFieldNaming("HTTPServerValue", global::yaml.YamlNamingConvention.KebabCase), Is.EqualTo("http-server-value"));
    }

    [Test]
    public void TriviaOperations_AddAndRemoveEveryOwnerAndPlacement()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        global::yaml.YamlNode node = Scalar(model, "value");
        global::yaml.YamlPair pair = Pair(model, "key", "value");

        Assert.That(model.AddDocumentComment(document, "document", global::yaml.YamlTriviaPlacement.Before), Is.True);
        Assert.That(model.AddDocumentBlankLine(document, global::yaml.YamlTriviaPlacement.Following), Is.True);
        Assert.That(model.AddNodeComment(node, "node", global::yaml.YamlTriviaPlacement.Inline), Is.True);
        Assert.That(model.AddNodeBlankLine(node, global::yaml.YamlTriviaPlacement.Before), Is.True);
        Assert.That(model.AddPairComment(pair, "pair", global::yaml.YamlTriviaPlacement.Following), Is.True);
        Assert.That(model.AddPairBlankLine(pair, global::yaml.YamlTriviaPlacement.Before), Is.True);
        Assert.That(document.Trivia.Count, Is.EqualTo(2));
        Assert.That(node.Trivia.Count, Is.EqualTo(2));
        Assert.That(pair.Trivia.Count, Is.EqualTo(2));
        Assert.That(model.RemoveDocumentTrivia(document, 0), Is.True);
        Assert.That(model.RemoveNodeTrivia(node, 0), Is.True);
        Assert.That(model.RemovePairTrivia(pair, 0), Is.True);
        Assert.That(document.Trivia.Count, Is.EqualTo(1));
        Assert.That(node.Trivia.Count, Is.EqualTo(1));
        Assert.That(pair.Trivia.Count, Is.EqualTo(1));
        Assert.That(model.AddDocumentComment(document, "inline", global::yaml.YamlTriviaPlacement.Inline), Is.False);
        Assert.That(model.AddNodeBlankLine(node, global::yaml.YamlTriviaPlacement.Inline), Is.False);
        Assert.That(model.AddPairBlankLine(pair, global::yaml.YamlTriviaPlacement.Inline), Is.False);
        Assert.That(model.RemoveDocumentTrivia(null, 0), Is.False);
        Assert.That(model.RemoveNodeTrivia(node, 9), Is.False);
        Assert.That(model.RemovePairTrivia(pair, -1), Is.False);
    }

    [Test]
    public void CloneNode_PreservesSharedRecursiveGraphsWithoutSharingMutableNodes()
    {
        global::yaml.IYamlModel model = new global::yaml.YamlModel();
        global::yaml.YamlNode root = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlNode shared = Scalar(model, "shared");
        root.Children.Add(shared);
        root.Children.Add(shared);
        root.Children.Add(root);

        global::yaml.YamlNode clone = model.CloneNode(root);
        global::yaml.YamlNode empty = model.CloneNode(null);

        Assert.That(clone, Is.Not.SameAs(root));
        Assert.That(clone.Children[0], Is.Not.SameAs(shared));
        Assert.That(clone.Children[0], Is.SameAs(clone.Children[1]));
        Assert.That(clone.Children[2], Is.SameAs(clone));
        Assert.That(empty.Kind, Is.EqualTo(global::yaml.YamlNodeKind.Empty));
    }

    [Test]
    public void AnchorAndAliasOperations_UpdateNamesTargetsAndRecursiveGraphs()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        global::yaml.YamlNode root = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlNode oldTarget = Scalar(model, "old");
        global::yaml.YamlNode newTarget = Scalar(model, "new");
        oldTarget.Anchor = "old";
        newTarget.Anchor = "new";
        global::yaml.YamlNode first = model.CreateAliasNode("old", null);
        global::yaml.YamlNode second = model.CreateAliasNode("old", null);
        model.AddMappingValue(root, "old", oldTarget);
        model.AddMappingValue(root, "new", newTarget);
        model.AddMappingValue(root, "first", first);
        model.AddMappingValue(root, "second", second);
        root.Children.Add(root);
        document.Root = root;

        Assert.That(model.RebuildAnchorTable(document), Is.EqualTo(2));
        Assert.That(model.FindAnchor(document, "old"), Is.SameAs(oldTarget));
        Assert.That(model.ResolveAlias(document, first), Is.True);
        Assert.That(model.ResolveAliases(document), Is.EqualTo(2));
        Assert.That(first.AliasTarget, Is.SameAs(oldTarget));
        Assert.That(second.AliasTarget, Is.SameAs(oldTarget));
        Assert.That(model.RenameAlias(document, first, "new"), Is.True);
        Assert.That(model.RenameAliases(document, "old", "new"), Is.EqualTo(1));
        Assert.That(first.AliasTarget, Is.SameAs(newTarget));
        Assert.That(second.AliasTarget, Is.SameAs(newTarget));
        Assert.That(model.RetargetAlias(document, first, oldTarget), Is.True);
        Assert.That(model.RetargetAliases(document, oldTarget, newTarget), Is.EqualTo(1));
        Assert.That(first.AliasTarget, Is.SameAs(newTarget));
        Assert.That(model.RenameAnchor(document, "new", "renamed"), Is.True);
        Assert.That(model.FindAnchor(document, "renamed"), Is.SameAs(newTarget));
        Assert.That(first.AliasName, Is.EqualTo("renamed"));
        Assert.That(second.AliasName, Is.EqualTo("renamed"));
        Assert.That(model.RenameAnchor(document, "old", "bad name"), Is.False);
        Assert.That(model.FindAnchor(null, "old"), Is.Null);
        Assert.That(model.RebuildAnchorTable(null), Is.EqualTo(0));
        Assert.That(model.ResolveAlias(document, oldTarget), Is.False);
        Assert.That(model.ResolveAliases(null), Is.EqualTo(0));
    }

    [Test]
    public void ApplyMergeKeys_MaterializesMissingValuesAndHandlesInvalidRequests()
    {
        global::yaml.IYamlModel model = new global::yaml.YamlModel();
        global::yaml.YamlNode defaults = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Flow, null);
        defaults.Anchor = "defaults";
        model.AddMappingValue(defaults, "enabled", Scalar(model, "true"));
        model.AddMappingValue(defaults, "name", Scalar(model, "base"));
        global::yaml.YamlNode target = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        model.AddMappingPair(target, model.CreatePair(
            Scalar(model, "<<"),
            AliasTo(model, "defaults", defaults),
            null));
        model.AddMappingValue(target, "name", Scalar(model, "explicit"));

        int changed = model.ApplyMergeKeys(target);

        Assert.That(changed, Is.EqualTo(2));
        Assert.That(model.FindMappingValue(target, "enabled")!.Value, Is.EqualTo("true"));
        Assert.That(model.FindMappingValue(target, "name")!.Value, Is.EqualTo("explicit"));
        Assert.That(model.FindMappingPair(target, "<<"), Is.Null);
        Assert.That(model.ApplyMergeKeys(null), Is.EqualTo(0));
    }

    public static global::yaml.YamlNode PeopleGraph(global::yaml.IYamlModel model)
    {
        global::yaml.YamlNode root = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlNode users = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlNode first = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlNode second = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        model.AddMappingValue(first, "name", Scalar(model, "Ada"));
        model.AddMappingValue(second, "name", Scalar(model, "Grace"));
        model.AddSequenceItem(users, first);
        model.AddSequenceItem(users, second);
        model.AddMappingValue(root, "users", users);
        return root;
    }

    public static global::yaml.YamlNode Scalar(global::yaml.IYamlModel model, string value)
    {
        return model.CreateScalarNode(value, global::yaml.YamlScalarStyle.Plain, null);
    }

    public static global::yaml.YamlPair Pair(global::yaml.IYamlModel model, string key, string value)
    {
        return model.CreatePair(Scalar(model, key), Scalar(model, value), null);
    }

    public static global::yaml.YamlNode AliasTo(global::yaml.IYamlModel model, string name, global::yaml.YamlNode target)
    {
        global::yaml.YamlNode alias = model.CreateAliasNode(name, null);
        alias.AliasTarget = target;
        return alias;
    }
}
