using System.Text.Json;

namespace jmap.Tests;

public class ResultReferencePathTests
{
    [Test]
    public void ParserUsesJsonPointerEscapingAndRejectsMalformedPaths()
    {
        ResultReferencePath evaluator = new ResultReferencePath();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluator.ParsePath("/a~1b/c~0d/*"), Is.EqualTo(new[] { "a/b", "c~d", "*" }));
            Assert.That(evaluator.ParsePath(""), Is.Empty);
            Assert.That(() => evaluator.ParsePath("missing-leading-slash"), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => evaluator.ParsePath("/bad~2escape"), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => evaluator.ParsePath(null), Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void ObjectAndArrayTokensSelectExactValues()
    {
        ResultReferencePath evaluator = new ResultReferencePath();
        JsonElement arguments = Json("""
            {
              "a/b": { "m~n": ["zero", "one"] },
              "*": "literal star",
              "": "empty member"
            }
            """);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluator.EvaluatePath(arguments, "/a~1b/m~0n/1").GetString(), Is.EqualTo("one"));
            Assert.That(evaluator.EvaluatePath(arguments, "/*").GetString(), Is.EqualTo("literal star"));
            Assert.That(evaluator.EvaluatePath(arguments, "/").GetString(), Is.EqualTo("empty member"));
            Assert.That(evaluator.EvaluatePath(arguments, "").ValueKind, Is.EqualTo(JsonValueKind.Object));
        }
    }

    [Test]
    public void SelectingAnArrayWithoutWildcardPreservesTheArray()
    {
        ResultReferencePath evaluator = new ResultReferencePath();

        JsonElement result = evaluator.EvaluatePath(Json("{\"ids\":[\"a\",\"b\"]}"), "/ids");

        Assert.That(result.EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void ArrayIndexSyntaxAllowsZeroAndLargeDigitStringsButNotLeadingZeros()
    {
        ResultReferencePath evaluator = new ResultReferencePath();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluator.IsArrayIndex("0"), Is.True);
            Assert.That(evaluator.IsArrayIndex("2147483648"), Is.True);
            Assert.That(evaluator.IsArrayIndex("01"), Is.False);
            Assert.That(evaluator.IsArrayIndex("-"), Is.False);
            Assert.That(evaluator.IsArrayIndex(""), Is.False);
        }
    }

    [Test]
    public void WildcardProjectsObjectMembersInArrayOrder()
    {
        ResultReferencePath evaluator = new ResultReferencePath();
        JsonElement arguments = Json("""
            {
              "list": [
                { "threadId": "t2" },
                { "threadId": "t1" },
                { "threadId": "t3" }
              ]
            }
            """);

        JsonElement result = evaluator.EvaluatePath(arguments, "/list/*/threadId");

        Assert.That(result.EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] { "t2", "t1", "t3" }));
    }

    [Test]
    public void WildcardFlattensArrayResults()
    {
        ResultReferencePath evaluator = new ResultReferencePath();
        JsonElement arguments = Json("""
            {
              "list": [
                { "emailIds": ["m1", "m2"] },
                { "emailIds": [] },
                { "emailIds": ["m3"] }
              ]
            }
            """);

        JsonElement result = evaluator.EvaluatePath(arguments, "/list/*/emailIds");

        Assert.That(result.EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] { "m1", "m2", "m3" }));
    }

    [Test]
    public void NestedWildcardsRecursivelyFlattenProjectedArrays()
    {
        ResultReferencePath evaluator = new ResultReferencePath();
        JsonElement arguments = Json("""
            {
              "groups": [
                [{ "id": "a" }, { "id": "b" }],
                [],
                [{ "id": "c" }]
              ]
            }
            """);

        JsonElement result = evaluator.EvaluatePath(arguments, "/groups/*/*/id");

        Assert.That(result.EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void FinalWildcardFlattensArrayItemsAndKeepsScalarItems()
    {
        ResultReferencePath evaluator = new ResultReferencePath();

        JsonElement result = evaluator.EvaluatePath(Json("{\"values\":[[1,2],3,[4]]}"), "/values/*");

        Assert.That(result.EnumerateArray().Select(value => value.GetInt32()), Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void WildcardOverAnEmptyArrayReturnsAnEmptyArray()
    {
        ResultReferencePath evaluator = new ResultReferencePath();

        JsonElement result = evaluator.EvaluatePath(Json("{\"list\":[]}"), "/list/*/id");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(result.GetArrayLength(), Is.Zero);
        }
    }

    [TestCase("/missing")]
    [TestCase("/scalar/child")]
    [TestCase("/nullValue/child")]
    [TestCase("/array/-")]
    [TestCase("/array/01")]
    [TestCase("/array/3")]
    [TestCase("/array/not-an-index")]
    [TestCase("/array/2147483648")]
    public void UnresolvableObjectArrayAndScalarSelectionsFail(string path)
    {
        ResultReferencePath evaluator = new ResultReferencePath();
        JsonElement arguments = Json("{\"scalar\":1,\"nullValue\":null,\"array\":[0,1,2]}");

        Assert.That(
            () => evaluator.EvaluatePath(arguments, path),
            Throws.TypeOf<JmapProtocolException>());
    }

    [Test]
    public void OneFailedWildcardBranchFailsTheWholeEvaluation()
    {
        ResultReferencePath evaluator = new ResultReferencePath();
        JsonElement arguments = Json("{\"list\":[{\"id\":\"a\"},{\"other\":\"b\"}]}");

        Assert.That(
            () => evaluator.EvaluatePath(arguments, "/list/*/id"),
            Throws.TypeOf<JmapProtocolException>());
    }

    [Test]
    public void DuplicateObjectMembersAreAmbiguous()
    {
        ResultReferencePath evaluator = new ResultReferencePath();
        JsonElement arguments = Json("{\"id\":\"first\",\"id\":\"second\"}");

        Assert.That(
            () => evaluator.EvaluatePath(arguments, "/id"),
            Throws.TypeOf<JmapProtocolException>());
    }

    [Test]
    public void EvaluationReturnsDetachedValues()
    {
        ResultReferencePath evaluator = new ResultReferencePath();
        JsonElement selected;
        JsonElement projected;
        using (JsonDocument document = JsonDocument.Parse("{\"object\":{\"id\":\"a\"},\"list\":[{\"id\":\"b\"}]}"))
        {
            selected = evaluator.EvaluatePath(document.RootElement, "/object");
            projected = evaluator.EvaluatePath(document.RootElement, "/list/*/id");
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(selected.GetProperty("id").GetString(), Is.EqualTo("a"));
            Assert.That(projected[0].GetString(), Is.EqualTo("b"));
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
