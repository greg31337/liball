using System.Text.Json;

namespace jmap.Tests;

public class StandardMethodModelsTests
{
    [Test]
    public void GetModelsRepresentAllObjectsEmptySelectionAndOrderedResults()
    {
        JmapGetRequest all = new JmapGetRequest();
        all.AccountId = "a1";

        JmapGetRequest none = new JmapGetRequest();
        none.Ids = new List<string>();
        none.Properties = new List<string> { JmapProperties.Id, JmapProperties.Name };
        none.UnknownProperties.Add("vendorArgument", Json("true"));

        JmapGetResponse response = new JmapGetResponse();
        response.AccountId = "a1";
        response.State = "s1";
        response.List.Add(Json("{\"id\":\"1\"}"));
        response.List.Add(Json("{\"id\":\"2\"}"));
        response.NotFound.Add("3");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(all.Ids, Is.Null);
            Assert.That(all.Properties, Is.Null);
            Assert.That(none.Ids, Is.Not.Null.And.Empty);
            Assert.That(none.Properties, Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.Name }));
            Assert.That(none.UnknownProperties["vendorArgument"].GetBoolean(), Is.True);
            Assert.That(response.List.Select(value => value.GetProperty("id").GetString()), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(response.NotFound, Is.EqualTo(new[] { "3" }));
            Assert.That(response.UnknownProperties, Is.Empty);
        }
    }

    [Test]
    public void ChangesModelsDistinguishMissingNullAndPositiveMaximum()
    {
        JmapChangesRequest missing = new JmapChangesRequest();

        JmapChangesRequest explicitNull = new JmapChangesRequest();
        explicitNull.HasMaxChanges = true;
        explicitNull.IsMaxChangesNull = true;

        JmapChangesRequest limited = new JmapChangesRequest();
        limited.AccountId = "a1";
        limited.SinceState = "s1";
        limited.MaxChanges = 50;
        limited.HasMaxChanges = true;

        JmapChangesResponse response = new JmapChangesResponse();
        response.AccountId = "a1";
        response.OldState = "s1";
        response.NewState = "s2";
        response.HasMoreChanges = true;
        response.Created.Add("created");
        response.Updated.Add("updated");
        response.Destroyed.Add("destroyed");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.HasMaxChanges, Is.False);
            Assert.That(missing.IsMaxChangesNull, Is.False);
            Assert.That(explicitNull.HasMaxChanges, Is.True);
            Assert.That(explicitNull.IsMaxChangesNull, Is.True);
            Assert.That(limited.MaxChanges, Is.EqualTo(50));
            Assert.That(limited.HasMaxChanges, Is.True);
            Assert.That(limited.IsMaxChangesNull, Is.False);
            Assert.That(response.Created, Is.EqualTo(new[] { "created" }));
            Assert.That(response.Updated, Is.EqualTo(new[] { "updated" }));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "destroyed" }));
            Assert.That(response.HasMoreChanges, Is.True);
        }
    }

    [Test]
    public void PatchAndSetModelsPreserveNullsAndPerRecordOutcomes()
    {
        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add("keywords/$seen", Json("null"));
        patch.Values.Add("mailboxIds", Json("{\"inbox\":true}"));

        JmapSetRequest request = new JmapSetRequest();
        request.AccountId = "a1";
        request.IfInState = "s1";
        request.Create = new Dictionary<string, JsonElement>();
        request.Create.Add("new-1", Json("{\"name\":\"Drafts\"}"));
        request.Update = new Dictionary<string, JmapPatchObject>();
        request.Update.Add("existing-1", patch);
        request.Destroy = new List<string> { "old-1" };

        JmapSetError setError = new JmapSetError();
        setError.Type = JmapErrorCodes.NotFound;
        setError.Kind = JmapSetErrorKind.NotFound;

        JmapSetResponse response = new JmapSetResponse();
        response.AccountId = "a1";
        response.OldState = "s1";
        response.NewState = "s2";
        response.Created = new Dictionary<string, JsonElement>();
        response.Created.Add("new-1", Json("{\"id\":\"created-1\"}"));
        response.Updated = new Dictionary<string, JsonElement>();
        response.Updated.Add("existing-1", Json("null"));
        response.Destroyed = new List<string> { "old-1" };
        response.NotDestroyed = new Dictionary<string, JmapSetError>();
        response.NotDestroyed.Add("missing-1", setError);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patch.Values["keywords/$seen"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(patch.Values["mailboxIds"].GetProperty("inbox").GetBoolean(), Is.True);
            Assert.That(request.Create["new-1"].GetProperty("name").GetString(), Is.EqualTo("Drafts"));
            Assert.That(request.Update["existing-1"], Is.SameAs(patch));
            Assert.That(request.Destroy, Is.EqualTo(new[] { "old-1" }));
            Assert.That(response.Created["new-1"].GetProperty("id").GetString(), Is.EqualTo("created-1"));
            Assert.That(response.Updated["existing-1"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "old-1" }));
            Assert.That(response.NotDestroyed["missing-1"], Is.SameAs(setError));
            Assert.That(response.NotCreated, Is.Null);
            Assert.That(response.NotUpdated, Is.Null);
        }
    }

    [Test]
    public void CopyModelsTrackOptionalDestructionAndPartialResults()
    {
        JmapCopyRequest omittedDestruction = new JmapCopyRequest();

        JmapCopyRequest request = new JmapCopyRequest();
        request.FromAccountId = "source";
        request.IfFromInState = "source-state";
        request.AccountId = "target";
        request.IfInState = "target-state";
        request.Create.Add("copy-1", Json("{\"id\":\"original-1\"}"));
        request.OnSuccessDestroyOriginal = false;
        request.HasOnSuccessDestroyOriginal = true;
        request.DestroyFromIfInState = "source-state";

        JmapCopyResponse response = new JmapCopyResponse();
        response.FromAccountId = "source";
        response.AccountId = "target";
        response.OldState = "target-state";
        response.NewState = "target-state-2";
        response.Created = new Dictionary<string, JsonElement>();
        response.Created.Add("copy-1", Json("{\"id\":\"copied-1\"}"));
        response.NotCreated = new Dictionary<string, JmapSetError>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(omittedDestruction.HasOnSuccessDestroyOriginal, Is.False);
            Assert.That(request.HasOnSuccessDestroyOriginal, Is.True);
            Assert.That(request.OnSuccessDestroyOriginal, Is.False);
            Assert.That(request.Create["copy-1"].GetProperty("id").GetString(), Is.EqualTo("original-1"));
            Assert.That(response.FromAccountId, Is.EqualTo("source"));
            Assert.That(response.Created["copy-1"].GetProperty("id").GetString(), Is.EqualTo("copied-1"));
            Assert.That(response.NotCreated, Is.Empty);
        }
    }

    [Test]
    public void FilterNodesSupportRecursiveOperatorsAndArbitraryConditions()
    {
        JmapFilterNode mailboxCondition = new JmapFilterNode();
        mailboxCondition.FilterProperties.Add(JmapProperties.InMailbox, Json("\"inbox\""));

        JmapFilterNode keywordCondition = new JmapFilterNode();
        keywordCondition.FilterProperties.Add(JmapProperties.HasKeyword, Json("\"$seen\""));

        JmapFilterNode not = new JmapFilterNode();
        not.HasOperator = true;
        not.Operator = JmapFilterOperator.Not;
        not.Conditions = new List<JmapFilterNode> { keywordCondition };

        JmapFilterNode and = new JmapFilterNode();
        and.HasOperator = true;
        and.Operator = JmapFilterOperator.And;
        and.Conditions = new List<JmapFilterNode> { mailboxCondition, not };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(and.HasOperator, Is.True);
            Assert.That(and.Operator, Is.EqualTo(JmapFilterOperator.And));
            Assert.That(and.Conditions, Has.Count.EqualTo(2));
            Assert.That(and.Conditions[1].Operator, Is.EqualTo(JmapFilterOperator.Not));
            Assert.That(mailboxCondition.HasOperator, Is.False);
            Assert.That(mailboxCondition.Conditions, Is.Null);
            Assert.That(mailboxCondition.FilterProperties[JmapProperties.InMailbox].GetString(), Is.EqualTo("inbox"));
        }
    }

    [Test]
    public void QueryRequestTracksDefaultsSeparatelyFromExplicitValues()
    {
        JmapQueryRequest defaults = new JmapQueryRequest();

        JmapComparator comparator = new JmapComparator();
        comparator.Property = JmapProperties.ReceivedAt;
        comparator.IsAscending = false;
        comparator.HasIsAscending = true;
        comparator.Collation = "i;unicode-casemap";
        comparator.UnknownProperties.Add("keyword", Json("\"$flagged\""));

        JmapQueryRequest request = new JmapQueryRequest();
        request.AccountId = "a1";
        request.Sort = new List<JmapComparator> { comparator };
        request.Position = -10;
        request.HasPosition = true;
        request.Anchor = "id-1";
        request.AnchorOffset = -1;
        request.HasAnchorOffset = true;
        request.HasLimit = true;
        request.IsLimitNull = true;
        request.CalculateTotal = false;
        request.HasCalculateTotal = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(defaults.HasPosition, Is.False);
            Assert.That(defaults.HasAnchorOffset, Is.False);
            Assert.That(defaults.HasLimit, Is.False);
            Assert.That(defaults.IsLimitNull, Is.False);
            Assert.That(defaults.HasCalculateTotal, Is.False);
            Assert.That(request.Position, Is.EqualTo(-10));
            Assert.That(request.AnchorOffset, Is.EqualTo(-1));
            Assert.That(request.HasLimit, Is.True);
            Assert.That(request.IsLimitNull, Is.True);
            Assert.That(request.CalculateTotal, Is.False);
            Assert.That(request.HasCalculateTotal, Is.True);
            Assert.That(comparator.HasIsAscending, Is.True);
            Assert.That(comparator.IsAscending, Is.False);
            Assert.That(comparator.UnknownProperties["keyword"].GetString(), Is.EqualTo("$flagged"));
        }
    }

    [Test]
    public void QueryResponseTracksConditionalTotalAndServerLimit()
    {
        JmapQueryResponse response = new JmapQueryResponse();
        response.AccountId = "a1";
        response.QueryState = "q1";
        response.CanCalculateChanges = true;
        response.Position = 20;
        response.Ids.Add("id-20");
        response.Total = 100;
        response.HasTotal = true;
        response.Limit = 25;
        response.HasLimit = true;
        response.UnknownProperties.Add("vendorResult", Json("7"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.QueryState, Is.EqualTo("q1"));
            Assert.That(response.CanCalculateChanges, Is.True);
            Assert.That(response.Position, Is.EqualTo(20));
            Assert.That(response.Ids, Is.EqualTo(new[] { "id-20" }));
            Assert.That(response.Total, Is.EqualTo(100));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Limit, Is.EqualTo(25));
            Assert.That(response.HasLimit, Is.True);
            Assert.That(response.UnknownProperties["vendorResult"].GetInt32(), Is.EqualTo(7));
        }
    }

    [Test]
    public void QueryChangesModelsPreserveNullableMaximumAndIndexedAdditionOrder()
    {
        JmapQueryChangesRequest request = new JmapQueryChangesRequest();
        request.AccountId = "a1";
        request.SinceQueryState = "q1";
        request.HasMaxChanges = true;
        request.IsMaxChangesNull = true;
        request.UpToId = "id-50";
        request.CalculateTotal = true;
        request.HasCalculateTotal = true;

        JmapAddedItem first = new JmapAddedItem();
        first.Id = "id-2";
        first.Index = 2;
        JmapAddedItem second = new JmapAddedItem();
        second.Id = "id-8";
        second.Index = 8;
        second.UnknownProperties.Add("vendorIndexData", Json("false"));

        JmapQueryChangesResponse response = new JmapQueryChangesResponse();
        response.AccountId = "a1";
        response.OldQueryState = "q1";
        response.NewQueryState = "q2";
        response.Total = 10;
        response.HasTotal = true;
        response.Removed.Add("id-1");
        response.Added.Add(first);
        response.Added.Add(second);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.HasMaxChanges, Is.True);
            Assert.That(request.IsMaxChangesNull, Is.True);
            Assert.That(request.CalculateTotal, Is.True);
            Assert.That(request.HasCalculateTotal, Is.True);
            Assert.That(response.OldQueryState, Is.EqualTo("q1"));
            Assert.That(response.NewQueryState, Is.EqualTo("q2"));
            Assert.That(response.Total, Is.EqualTo(10));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Removed, Is.EqualTo(new[] { "id-1" }));
            Assert.That(response.Added, Is.EqualTo(new[] { first, second }));
            Assert.That(response.Added.Select(item => item.Index), Is.EqualTo(new long[] { 2, 8 }));
            Assert.That(second.UnknownProperties["vendorIndexData"].GetBoolean(), Is.False);
        }
    }

    [Test]
    public void RequiredCollectionsAreIndependentAcrossInstances()
    {
        JmapGetResponse firstGet = new JmapGetResponse();
        JmapGetResponse secondGet = new JmapGetResponse();
        JmapChangesResponse firstChanges = new JmapChangesResponse();
        JmapChangesResponse secondChanges = new JmapChangesResponse();
        JmapQueryChangesResponse firstQueryChanges = new JmapQueryChangesResponse();
        JmapQueryChangesResponse secondQueryChanges = new JmapQueryChangesResponse();

        firstGet.NotFound.Add("missing");
        firstChanges.Created.Add("created");
        firstQueryChanges.Removed.Add("removed");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondGet.NotFound, Is.Empty);
            Assert.That(secondChanges.Created, Is.Empty);
            Assert.That(secondQueryChanges.Removed, Is.Empty);
            Assert.That(secondGet.NotFound, Is.Not.SameAs(firstGet.NotFound));
            Assert.That(secondChanges.Created, Is.Not.SameAs(firstChanges.Created));
            Assert.That(secondQueryChanges.Removed, Is.Not.SameAs(firstQueryChanges.Removed));
        }
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
