using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlModelEditingContractTests
{
    [Test]
    public void SequenceOperations_TraverseInsertReplaceAndRemoveItems()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlNode sequence = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlNode one = Scalar(model, "one");
        global::yaml.YamlNode two = Scalar(model, "two");
        global::yaml.YamlNode middle = Scalar(model, "middle");
        global::yaml.YamlNode replacement = Scalar(model, "replacement");

        Assert.That(model.AddSequenceItem(sequence, one), Is.True);
        Assert.That(model.AddSequenceItem(sequence, two), Is.True);
        Assert.That(model.InsertSequenceItem(sequence, 1, middle), Is.True);
        Assert.That(model.SequenceItemCount(sequence), Is.EqualTo(3));
        Assert.That(model.GetSequenceItem(sequence, 1), Is.SameAs(middle));
        Assert.That(model.SetSequenceItem(sequence, 1, replacement), Is.True);
        Assert.That(model.RemoveSequenceItem(sequence, 0), Is.True);
        Assert.That(model.GetSequenceItem(sequence, 0), Is.SameAs(replacement));
        Assert.That(model.GetSequenceItem(sequence, 8), Is.Null);
        Assert.That(model.AddSequenceItem(one, two), Is.False);
        Assert.That(model.InsertSequenceItem(sequence, -1, two), Is.False);
        Assert.That(model.SetSequenceItem(null, 0, two), Is.False);
        Assert.That(model.RemoveSequenceItem(sequence, 9), Is.False);
        Assert.That(model.SequenceItemCount(one), Is.EqualTo(0));
    }

    [Test]
    public void MappingOperations_PreserveOrderedPairsAndSupportKeyedValues()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlNode mapping = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlPair second = Pair(model, "second", "2");
        global::yaml.YamlPair first = Pair(model, "first", "1");
        global::yaml.YamlPair replacement = Pair(model, "replacement", "r");

        Assert.That(model.AddMappingValue(mapping, "third", Scalar(model, "3")), Is.True);
        Assert.That(model.AddMappingPair(mapping, second), Is.True);
        Assert.That(model.InsertMappingPair(mapping, 0, first), Is.True);
        Assert.That(model.MappingPairCount(mapping), Is.EqualTo(3));
        Assert.That(model.GetMappingPair(mapping, 0), Is.SameAs(first));
        Assert.That(model.FindMappingPair(mapping, "second"), Is.SameAs(second));
        Assert.That(model.FindMappingValue(mapping, "third")!.Value, Is.EqualTo("3"));

        global::yaml.YamlNode updated = Scalar(model, "updated");
        Assert.That(model.SetMappingValue(mapping, "third", updated), Is.True);
        Assert.That(model.FindMappingValue(mapping, "third"), Is.SameAs(updated));
        Assert.That(model.SetMappingPair(mapping, 0, replacement), Is.True);
        Assert.That(model.GetMappingPair(mapping, 0), Is.SameAs(replacement));
        Assert.That(model.RemoveMappingPair(mapping, 0), Is.True);
        Assert.That(model.RemoveMappingValue(mapping, "second"), Is.True);
        Assert.That(model.MappingPairCount(mapping), Is.EqualTo(1));
        Assert.That(model.GetMappingPair(mapping, 0)!.Key!.Value, Is.EqualTo("third"));

        Assert.That(model.GetMappingPair(mapping, -1), Is.Null);
        Assert.That(model.FindMappingPair(mapping, "missing"), Is.Null);
        Assert.That(model.AddMappingPair(Scalar(model, "not mapping"), first), Is.False);
        Assert.That(model.InsertMappingPair(mapping, 8, first), Is.False);
        Assert.That(model.SetMappingPair(mapping, 2, first), Is.False);
        Assert.That(model.RemoveMappingPair(null, 0), Is.False);
        Assert.That(model.SetMappingValue(mapping, "missing", updated), Is.False);
        Assert.That(model.RemoveMappingValue(mapping, "missing"), Is.False);
    }

    [Test]
    public void ParsedPathOperations_FindReplaceAndRemoveWithoutReparsing()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlNode root = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlNode items = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);
        model.AddSequenceItem(items, Scalar(model, "zero"));
        model.AddSequenceItem(items, Scalar(model, "one"));
        model.AddMappingValue(root, "items", items);
        global::yaml.YamlPath path = model.ParsePath("items[1]");

        Assert.That(model.FindParsedPath(root, path)!.Value, Is.EqualTo("one"));
        Assert.That(model.ReplaceParsedPath(root, path, Scalar(model, "changed")), Is.True);
        Assert.That(model.FindParsedPath(root, path)!.Value, Is.EqualTo("changed"));
        Assert.That(model.RemoveParsedPath(root, path), Is.True);
        Assert.That(model.FindParsedPath(root, path), Is.Null);

        global::yaml.YamlPath invalid = model.ParsePath("items[]");
        Assert.That(model.FindParsedPath(root, invalid), Is.Null);
        Assert.That(model.ReplaceParsedPath(null, path, Scalar(model, "x")), Is.False);
        Assert.That(model.RemoveParsedPath(root, invalid), Is.False);
    }

    [Test]
    public void PresentationOperations_SetScalarCollectionAndTagMetadata()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlNode scalar = Scalar(model, "true");
        global::yaml.YamlNode sequence = model.CreateSequenceNode(global::yaml.YamlCollectionStyle.Block, null);
        model.AddSequenceItem(sequence, scalar);

        Assert.That(model.SetScalarStyle(scalar, global::yaml.YamlScalarStyle.DoubleQuoted), Is.True);
        Assert.That(model.SetScalarStyle(sequence, global::yaml.YamlScalarStyle.Plain), Is.False);
        Assert.That(model.SetCollectionStyle(sequence, global::yaml.YamlCollectionStyle.Flow), Is.True);
        Assert.That(model.SetCollectionStyle(scalar, global::yaml.YamlCollectionStyle.Flow), Is.False);
        Assert.That(model.SetTag(scalar, "!!str"), Is.True);
        Assert.That(model.SetTag(null, "!!str"), Is.False);

        Assert.That(scalar.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.DoubleQuoted));
        Assert.That(sequence.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(scalar.Tag, Is.EqualTo("!!str"));
        Assert.That(global::yaml.YamlEmitter.EmitNode(sequence), Is.EqualTo("[\"true\"]"));
        Assert.That(model.SetTag(scalar, null), Is.True);
        Assert.That(scalar.Tag, Is.Empty);
    }

    [Test]
    public void TriviaOperations_AddEmitAndRemovePlacedCommentsAndBlankLines()
    {
        global::yaml.IYamlModel model = global::yaml.YamlApi.Model;
        global::yaml.YamlDocument document = new global::yaml.YamlDocument();
        global::yaml.YamlNode mapping = model.CreateMappingNode(global::yaml.YamlCollectionStyle.Block, null);
        global::yaml.YamlPair pair = Pair(model, "name", "Ada");
        model.AddMappingPair(mapping, pair);
        document.Root = mapping;

        Assert.That(model.AddDocumentComment(document, "document", global::yaml.YamlTriviaPlacement.Before), Is.True);
        Assert.That(model.AddDocumentBlankLine(document, global::yaml.YamlTriviaPlacement.Before), Is.True);
        Assert.That(model.AddPairComment(pair, "pair", global::yaml.YamlTriviaPlacement.Before), Is.True);
        Assert.That(model.AddPairComment(pair, "inline", global::yaml.YamlTriviaPlacement.Inline), Is.True);
        Assert.That(model.AddPairComment(pair, "following", global::yaml.YamlTriviaPlacement.Following), Is.True);
        Assert.That(model.AddNodeComment(pair.Value, "value", global::yaml.YamlTriviaPlacement.Before), Is.True);
        Assert.That(model.AddNodeBlankLine(pair.Value, global::yaml.YamlTriviaPlacement.Following), Is.True);
        Assert.That(model.AddPairBlankLine(pair, global::yaml.YamlTriviaPlacement.Before), Is.True);
        Assert.That(model.AddDocumentComment(document, "bad", global::yaml.YamlTriviaPlacement.Inline), Is.False);
        Assert.That(model.AddNodeBlankLine(pair.Value, global::yaml.YamlTriviaPlacement.Inline), Is.False);

        string emitted = global::yaml.YamlEmitter.EmitDocument(document);

        Assert.That(emitted, Is.EqualTo("# document\n\n# pair\n\nname: Ada # inline\n# following\n"));
        Assert.That(model.RemoveDocumentTrivia(document, 0), Is.True);
        Assert.That(model.RemoveNodeTrivia(pair.Value, 0), Is.True);
        Assert.That(model.RemovePairTrivia(pair, 0), Is.True);
        Assert.That(document.LeadingComments, Is.Empty);
        Assert.That(pair.Value!.LeadingComments, Is.Empty);
        Assert.That(pair.LeadingComments, Is.Empty);
        Assert.That(model.RemoveDocumentTrivia(document, 9), Is.False);
        Assert.That(model.RemoveNodeTrivia(null, 0), Is.False);
        Assert.That(model.RemovePairTrivia(pair, -1), Is.False);
    }

    public static global::yaml.YamlNode Scalar(global::yaml.IYamlModel model, string value)
    {
        return model.CreateScalarNode(value, global::yaml.YamlScalarStyle.Plain, null);
    }

    public static global::yaml.YamlPair Pair(global::yaml.IYamlModel model, string key, string value)
    {
        return model.CreatePair(Scalar(model, key), Scalar(model, value), null);
    }
}
