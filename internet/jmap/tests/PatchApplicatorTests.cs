using System.Text.Json;

namespace jmap.Tests;

public class PatchApplicatorTests
{
    [Test]
    public void ConflictDetectionUsesDecodedTokenBoundaries()
    {
        PatchValidator validator = new PatchValidator();
        JmapPatchObject patch = Patch(
            ("alerts", "{}"),
            ("alerts/offset", "5"),
            ("alert", "true"),
            ("a~1b", "{}"),
            ("a/b", "false"),
            ("escaped~1parent", "{}"),
            ("escaped~1parent/child", "1"));

        List<string> conflicts = validator.FindConflictingPaths(patch);

        Assert.That(
            conflicts,
            Is.EqualTo(new[] { "alerts", "alerts/offset", "escaped~1parent", "escaped~1parent/child" }));
    }

    [Test]
    public void SimilarStringPrefixesAndEscapedSlashTokensDoNotConflict()
    {
        PatchValidator validator = new PatchValidator();
        JmapPatchObject patch = Patch(
            ("foo", "1"),
            ("foobar/value", "2"),
            ("a~1b", "3"),
            ("a/b", "4"));

        Assert.That(validator.FindConflictingPaths(patch), Is.Empty);
    }

    [Test]
    public void ApplicationReplacesAddsAndTargetsEscapedObjectMembers()
    {
        PatchApplicator applicator = new PatchApplicator();
        JsonElement original = Json("""
            {
              "title": "Old",
              "settings": { "a/b": 1, "keep": true },
              "items": [1, 2]
            }
            """);
        JmapPatchObject patch = Patch(
            ("title", "\"New\""),
            ("settings/a~1b", "2"),
            ("settings/added", "{\"value\":3}"),
            ("items", "[3,4]"),
            ("newTopLevel", "true"));

        JsonElement result = applicator.ApplyPatch(original, patch, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.GetProperty("title").GetString(), Is.EqualTo("New"));
            Assert.That(result.GetProperty("settings").GetProperty("a/b").GetInt32(), Is.EqualTo(2));
            Assert.That(result.GetProperty("settings").GetProperty("keep").GetBoolean(), Is.True);
            Assert.That(result.GetProperty("settings").GetProperty("added").GetProperty("value").GetInt32(), Is.EqualTo(3));
            Assert.That(result.GetProperty("items").EnumerateArray().Select(value => value.GetInt32()), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(result.GetProperty("newTopLevel").GetBoolean(), Is.True);
            Assert.That(original.GetProperty("title").GetString(), Is.EqualTo("Old"));
            Assert.That(original.GetProperty("settings").GetProperty("a/b").GetInt32(), Is.EqualTo(1));
        }
    }

    [Test]
    public void NullRestoresConfiguredDefaultsOrRemovesTheMember()
    {
        PatchApplicator applicator = new PatchApplicator();
        JsonElement original = Json("""
            {
              "enabled": false,
              "label": "custom",
              "nested": { "value": 1, "keep": true }
            }
            """);
        JmapPatchObject patch = Patch(
            ("enabled", "null"),
            ("label", "null"),
            ("nested/value", "null"),
            ("nested/missing", "null"));
        Dictionary<string, JsonElement> defaults = new Dictionary<string, JsonElement>
        {
            ["enabled"] = Json("true")
        };

        JsonElement result = applicator.ApplyPatch(original, patch, defaults);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(result.TryGetProperty("label", out _), Is.False);
            Assert.That(result.GetProperty("nested").TryGetProperty("value", out _), Is.False);
            Assert.That(result.GetProperty("nested").TryGetProperty("missing", out _), Is.False);
            Assert.That(result.GetProperty("nested").GetProperty("keep").GetBoolean(), Is.True);
        }
    }

    [Test]
    public void NullCanRestoreObjectArrayAndExplicitNullDefaults()
    {
        PatchApplicator applicator = new PatchApplicator();
        JsonElement original = Json("{\"object\":{},\"array\":[],\"nullable\":\"set\"}");
        JmapPatchObject patch = Patch(
            ("object", "null"),
            ("array", "null"),
            ("nullable", "null"));
        Dictionary<string, JsonElement> defaults = new Dictionary<string, JsonElement>
        {
            ["object"] = Json("{\"defaulted\":true}"),
            ["array"] = Json("[1,2]"),
            ["nullable"] = Json("null")
        };

        JsonElement result = applicator.ApplyPatch(original, patch, defaults);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.GetProperty("object").GetProperty("defaulted").GetBoolean(), Is.True);
            Assert.That(result.GetProperty("array").GetArrayLength(), Is.EqualTo(2));
            Assert.That(result.GetProperty("nullable").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
    }

    [Test]
    public void OverlappingPathsAreRejectedBeforeApplication()
    {
        PatchApplicator applicator = new PatchApplicator();
        JsonElement original = Json("{\"settings\":{\"value\":1}}");
        JmapPatchObject patch = Patch(
            ("settings", "{}"),
            ("settings/value", "2"));

        JmapValidationException exception = Assert.Throws<JmapValidationException>(() =>
            applicator.ApplyPatch(original, patch, null))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.InvalidProperties, Is.EqualTo(new[] { "settings", "settings/value" }));
            Assert.That(original.GetProperty("settings").GetProperty("value").GetInt32(), Is.EqualTo(1));
        }
    }

    [Test]
    public void ArrayInteriorAndMissingParentPatchesAreRejectedAtomically()
    {
        PatchApplicator applicator = new PatchApplicator();
        JsonElement original = Json("{\"title\":\"old\",\"items\":[1,2]}");
        JmapPatchObject patch = Patch(
            ("title", "\"new\""),
            ("items/0", "9"),
            ("missing/value", "1"));

        JmapValidationException exception = Assert.Throws<JmapValidationException>(() =>
            applicator.ApplyPatch(original, patch, null))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.InvalidProperties, Is.EqualTo(new[] { "items/0", "missing/value" }));
            Assert.That(original.GetProperty("title").GetString(), Is.EqualTo("old"));
            Assert.That(original.GetProperty("items")[0].GetInt32(), Is.EqualTo(1));
        }
    }

    [Test]
    public void UndefinedPatchAndDefaultValuesAreRejected()
    {
        PatchApplicator applicator = new PatchApplicator();
        JsonElement original = Json("{\"value\":1}");
        JmapPatchObject undefinedPatch = new JmapPatchObject();
        undefinedPatch.Values.Add("value", default(JsonElement));
        JmapPatchObject resetPatch = Patch(("value", "null"));
        Dictionary<string, JsonElement> defaults = new Dictionary<string, JsonElement>
        {
            ["value"] = default(JsonElement)
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => applicator.ApplyPatch(original, undefinedPatch, null),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(
                () => applicator.ApplyPatch(original, resetPatch, defaults),
                Throws.TypeOf<JmapValidationException>());
        }
    }

    private static JmapPatchObject Patch(params (string Path, string Json)[] entries)
    {
        JmapPatchObject patch = new JmapPatchObject();
        foreach ((string path, string json) in entries)
        {
            patch.Values.Add(path, Json(json));
        }

        return patch;
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
