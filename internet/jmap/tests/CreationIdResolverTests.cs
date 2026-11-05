using System.Text.Json;

namespace jmap.Tests;

public class CreationIdResolverTests
{
    [Test]
    public void CarryInIsCopiedAccumulatedAndReturnedWithLatestMappings()
    {
        Dictionary<string, string> carryIn = new Dictionary<string, string>
        {
            { "draft", "M1" }
        };
        CreationIdResolver resolver = new CreationIdResolver(carryIn);
        carryIn["draft"] = "changed-outside";

        bool replaced = resolver.AccumulateCreatedId("draft", "M2");
        bool added = resolver.AccumulateCreatedId("mailbox", "B1");
        Dictionary<string, string>? carryOut = resolver.GetCarryOutCreatedIds();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(replaced, Is.True);
            Assert.That(added, Is.True);
            Assert.That(carryOut, Is.EqualTo(new Dictionary<string, string>
            {
                { "draft", "M2" },
                { "mailbox", "B1" }
            }));
        }

        carryOut!["draft"] = "changed-copy";
        Assert.That(resolver.SnapshotCreatedIds()["draft"], Is.EqualTo("M2"));
    }

    [Test]
    public void OmittedCarryInSuppressesCarryOutButStillTracksRequestMappings()
    {
        CreationIdResolver resolver = new CreationIdResolver(null);
        CreationIdResolver emptyCarryIn = new CreationIdResolver(new Dictionary<string, string>());

        bool added = resolver.AccumulateCreatedId("draft", "M1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(added, Is.True);
            Assert.That(resolver.SnapshotCreatedIds()["draft"], Is.EqualTo("M1"));
            Assert.That(resolver.GetCarryOutCreatedIds(), Is.Null);
            Assert.That(emptyCarryIn.GetCarryOutCreatedIds(), Is.Not.Null.And.Empty);
        }
    }

    [Test]
    public void AccumulatorRejectsInvalidCreationAndRecordIds()
    {
        CreationIdResolver resolver = new CreationIdResolver(null);

        bool invalidCreationId = resolver.AccumulateCreatedId("#draft", "M1");
        bool invalidRecordId = resolver.AccumulateCreatedId("draft", "not/an/id");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalidCreationId, Is.False);
            Assert.That(invalidRecordId, Is.False);
            Assert.That(resolver.SnapshotCreatedIds(), Is.Empty);
        }
    }

    [Test]
    public void CreateDataSubstitutesReferencesInValuesArraysAndIdMapKeys()
    {
        CreationIdResolver resolver = ResolverWithIds(
            new Dictionary<string, string>
            {
                { "parent", "P1" },
                { "mailbox", "B1" }
            });
        JsonElement data = Json("""
            {
              "parentId": "#parent",
              "mailboxIds": { "#mailbox": true },
              "related": ["#parent", null, 7],
              "name": "ordinary"
            }
            """);

        CreationIdResolution resolution = resolver.ResolveCreateData(data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution.IsSuccess, Is.True);
            Assert.That(resolution.Error, Is.Null);
            Assert.That(resolution.Data.GetProperty("parentId").GetString(), Is.EqualTo("P1"));
            Assert.That(resolution.Data.GetProperty("mailboxIds").GetProperty("B1").GetBoolean(), Is.True);
            Assert.That(resolution.Data.GetProperty("related")[0].GetString(), Is.EqualTo("P1"));
            Assert.That(resolution.Data.GetProperty("related")[1].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(resolution.Data.GetProperty("name").GetString(), Is.EqualTo("ordinary"));
            Assert.That(data.GetProperty("parentId").GetString(), Is.EqualTo("#parent"));
        }
    }

    [Test]
    public void UpdateDataSubstitutesPatchValuesAndCreationIdsInPatchTokens()
    {
        CreationIdResolver resolver = ResolverWithIds(
            new Dictionary<string, string>
            {
                { "parent", "P1" },
                { "mailbox", "B1" }
            });
        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add("parentId", Json("\"#parent\""));
        patch.Values.Add("mailboxIds/#mailbox", Json("true"));
        patch.Values.Add("links", Json("[\"#parent\"]"));

        CreationIdResolution resolution = resolver.ResolveUpdateData(patch);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution.IsSuccess, Is.True);
            Assert.That(resolution.Error, Is.Null);
            Assert.That(resolution.Patch!.Values["parentId"].GetString(), Is.EqualTo("P1"));
            Assert.That(resolution.Patch.Values["mailboxIds/B1"].GetBoolean(), Is.True);
            Assert.That(resolution.Patch.Values["links"][0].GetString(), Is.EqualTo("P1"));
            Assert.That(patch.Values.ContainsKey("mailboxIds/#mailbox"), Is.True);
        }
    }

    [Test]
    public void UnresolvedCreateReferencesReturnAtomicInvalidPropertiesSetError()
    {
        CreationIdResolver resolver = new CreationIdResolver(null);
        JsonElement data = Json("""
            {
              "parentId": "#missing",
              "mailboxIds": { "#other": true },
              "children": [{ "id": "#third" }],
              "link/key": "#fourth"
            }
            """);

        CreationIdResolution resolution = resolver.ResolveCreateData(data);

        AssertInvalidProperties(
            resolution,
            new[] { "parentId", "mailboxIds/#other", "children/0/id", "link~1key" });
        Assert.That(resolution.Data.ValueKind, Is.EqualTo(JsonValueKind.Undefined));
        Assert.That(data.GetProperty("parentId").GetString(), Is.EqualTo("#missing"));
    }

    [Test]
    public void UnresolvedUpdateReferencesIdentifyPatchAndNestedValuePaths()
    {
        CreationIdResolver resolver = new CreationIdResolver(null);
        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add("mailboxIds/#missing", Json("true"));
        patch.Values.Add("relationship", Json("{\"items\":[\"#other\"]}"));

        CreationIdResolution resolution = resolver.ResolveUpdateData(patch);

        AssertInvalidProperties(
            resolution,
            new[] { "mailboxIds/#missing", "relationship/items/0" });
        Assert.That(resolution.Patch, Is.Null);
    }

    [Test]
    public void SubstitutionCollisionReturnsInvalidPropertiesWithoutWritingDuplicateJson()
    {
        CreationIdResolver resolver = ResolverWithIds(
            new Dictionary<string, string> { { "mailbox", "B1" } });
        JsonElement data = Json("{\"mailboxIds\":{\"B1\":true,\"#mailbox\":false}}");

        CreationIdResolution resolution = resolver.ResolveCreateData(data);

        AssertInvalidProperties(resolution, new[] { "mailboxIds/#mailbox" });
    }

    private static CreationIdResolver ResolverWithIds(Dictionary<string, string> values)
    {
        return new CreationIdResolver(values);
    }

    private static void AssertInvalidProperties(CreationIdResolution resolution, string[] paths)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution.IsSuccess, Is.False);
            Assert.That(resolution.Error, Is.Not.Null);
            Assert.That(resolution.Error!.Type, Is.EqualTo(JmapErrorCodes.InvalidProperties));
            Assert.That(resolution.Error.Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(resolution.Error.Properties, Is.EqualTo(paths));
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
