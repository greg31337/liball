using System.Text.Json;

namespace jmap.Tests;

public class JsonPointerTests
{
    [Test]
    public void ParserDecodesRfcReferenceTokensInOrder()
    {
        JsonPointer pointer = new JsonPointer();

        List<string> tokens = pointer.ParsePointer("/a~1b/m~0n/~01//雪");

        Assert.That(tokens, Is.EqualTo(new[] { "a/b", "m~n", "~1", "", "雪" }));
    }

    [Test]
    public void EmptyPointerReferencesRootWhileSlashContainsAnEmptyToken()
    {
        JsonPointer pointer = new JsonPointer();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pointer.ParsePointer(""), Is.Empty);
            Assert.That(pointer.ParsePointer("/"), Is.EqualTo(new[] { "" }));
        }
    }

    [Test]
    public void EscapingAndFormattingRoundTripArbitraryTokens()
    {
        JsonPointer pointer = new JsonPointer();
        List<string> tokens = new List<string> { "", "a/b", "m~n", "~1", "snow 雪", "\0" };

        string formatted = pointer.FormatPointer(tokens);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(formatted, Is.EqualTo("//a~1b/m~0n/~01/snow 雪/\0"));
            Assert.That(pointer.ParsePointer(formatted), Is.EqualTo(tokens));
            Assert.That(pointer.EscapeToken("/~"), Is.EqualTo("~1~0"));
        }
    }

    [TestCase(null)]
    [TestCase("not/a/pointer")]
    [TestCase("#fragment")]
    [TestCase("/~")]
    [TestCase("/~2")]
    [TestCase("/valid/~a")]
    public void MalformedPointersAreRejected(string? value)
    {
        JsonPointer pointer = new JsonPointer();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pointer.IsPointer(value), Is.False);
            Assert.That(() => pointer.ParsePointer(value), Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void ValidPointersIncludeControlCharactersAndAdjacentEscapes()
    {
        JsonPointer pointer = new JsonPointer();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pointer.IsPointer("/\0/~0~1"), Is.True);
            Assert.That(pointer.ParsePointer("/\0/~0~1"), Is.EqualTo(new[] { "\0", "~/" }));
        }
    }

    [Test]
    public void DirectTokenDecodingRejectsUnescapedSeparatorsAndInvalidUnicode()
    {
        JsonPointer pointer = new JsonPointer();
        string invalidUnicode = new string(new[] { '\ud800' });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => pointer.UnescapeToken("a/b"), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => pointer.UnescapeToken("bad~2escape"), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => pointer.UnescapeToken(invalidUnicode), Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void PatchTraversalAllowsExistingObjectParentsAndNewFinalMembers()
    {
        PatchValidator validator = new PatchValidator();
        JsonElement target = Json("""
            {
              "profile": { "display/name": "Old" },
              "a/b": { "c~d": true },
              "array": [1, 2],
              "numeric": { "0": "object member" }
            }
            """);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsValidPatchPath(target, "profile/display~1name"), Is.True);
            Assert.That(validator.IsValidPatchPath(target, "profile/newProperty"), Is.True);
            Assert.That(validator.IsValidPatchPath(target, "a~1b/c~0d"), Is.True);
            Assert.That(validator.IsValidPatchPath(target, "array"), Is.True);
            Assert.That(validator.IsValidPatchPath(target, "numeric/0"), Is.True);
            Assert.That(validator.IsValidPatchPath(target, "newTopLevelProperty"), Is.True);
        }
    }

    [TestCase("array/0")]
    [TestCase("array/-")]
    [TestCase("array/0/value")]
    [TestCase("scalar/child")]
    [TestCase("missing/child")]
    [TestCase("nullValue/child")]
    [TestCase("profile/bad~2escape")]
    public void PatchTraversalRejectsArraysMissingParentsScalarsAndBadEscapes(string path)
    {
        PatchValidator validator = new PatchValidator();
        JsonElement target = Json("""
            {
              "array": [{ "value": 1 }],
              "scalar": 1,
              "nullValue": null,
              "profile": {}
            }
            """);

        Assert.That(validator.IsValidPatchPath(target, path), Is.False);
    }

    [Test]
    public void PatchValidationReturnsEachInvalidPath()
    {
        PatchValidator validator = new PatchValidator();
        JsonElement target = Json("{\"nested\":{},\"items\":[]}");
        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add("nested/value", Json("1"));
        patch.Values.Add("missing/value", Json("2"));
        patch.Values.Add("items/0", Json("3"));

        Assert.That(
            validator.ValidatePatchPaths(target, patch),
            Is.EqualTo(new[] { "missing/value", "items/0" }));
    }

    [Test]
    public void PatchTraversalRequiresAnObjectRoot()
    {
        PatchValidator validator = new PatchValidator();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsValidPatchPath(Json("[]"), "0"), Is.False);
            Assert.That(validator.IsValidPatchPath(Json("null"), "value"), Is.False);
            Assert.That(validator.ValidatePatchPaths(Json("{}"), null), Is.EqualTo(new[] { "" }));
        }
    }

    [Test]
    public void PatchTraversalRejectsAmbiguousObjectMembers()
    {
        PatchValidator validator = new PatchValidator();
        JsonElement duplicateParent = Json("{\"nested\":{},\"nested\":{}}");
        JsonElement duplicateFinal = Json("{\"value\":1,\"value\":2}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsValidPatchPath(duplicateParent, "nested/value"), Is.False);
            Assert.That(validator.IsValidPatchPath(duplicateFinal, "value"), Is.False);
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
