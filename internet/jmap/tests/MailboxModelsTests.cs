using System.Text.Json;

namespace jmap.Tests;

public class MailboxModelsTests
{
    [Test]
    public void MailboxPreservesHierarchyRoleCountsRightsAndExtensions()
    {
        MailboxRights rights = new MailboxRights();
        rights.MayReadItems = true;
        rights.MayAddItems = true;
        rights.MayRemoveItems = false;
        rights.MaySetSeen = true;
        rights.MaySetKeywords = true;
        rights.MayCreateChild = true;
        rights.MayRename = false;
        rights.MayDelete = false;
        rights.MaySubmit = true;
        rights.UnknownProperties.Add("vendorRight", Json("true"));

        Mailbox mailbox = new Mailbox();
        mailbox.Id = "mailbox-1";
        mailbox.Name = "Inbox";
        mailbox.ParentId = null;
        mailbox.Role = JmapMailboxRoles.Inbox;
        mailbox.SortOrder = 10;
        mailbox.TotalEmails = 25;
        mailbox.UnreadEmails = 4;
        mailbox.TotalThreads = 20;
        mailbox.UnreadThreads = 3;
        mailbox.MyRights = rights;
        mailbox.IsSubscribed = true;
        mailbox.UnknownProperties.Add("vendorColor", Json("\"blue\""));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mailbox.ParentId, Is.Null);
            Assert.That(mailbox.Role, Is.EqualTo(JmapMailboxRoles.Inbox));
            Assert.That(mailbox.TotalEmails, Is.EqualTo(25));
            Assert.That(mailbox.UnreadThreads, Is.EqualTo(3));
            Assert.That(mailbox.MyRights, Is.SameAs(rights));
            Assert.That(mailbox.MyRights.MayReadItems, Is.True);
            Assert.That(mailbox.MyRights.MayRemoveItems, Is.False);
            Assert.That(mailbox.MyRights.UnknownProperties["vendorRight"].GetBoolean(), Is.True);
            Assert.That(mailbox.UnknownProperties["vendorColor"].GetString(), Is.EqualTo("blue"));
        }
    }

    [Test]
    public void MailboxFilterDistinguishesOmittedNullAndFalseConditionsRecursively()
    {
        MailboxFilter omitted = new MailboxFilter();

        MailboxFilter topLevelWithoutRole = new MailboxFilter();
        topLevelWithoutRole.ParentId = null;
        topLevelWithoutRole.HasParentId = true;
        topLevelWithoutRole.Role = null;
        topLevelWithoutRole.HasRole = true;
        topLevelWithoutRole.HasAnyRole = false;
        topLevelWithoutRole.HasHasAnyRole = true;
        topLevelWithoutRole.IsSubscribed = false;
        topLevelWithoutRole.HasIsSubscribed = true;

        MailboxFilter named = new MailboxFilter();
        named.Name = "project";
        named.UnknownProperties.Add("vendorPinned", Json("true"));

        MailboxFilter either = new MailboxFilter();
        either.Operator = JmapFilterOperator.Or;
        either.HasOperator = true;
        either.Conditions = new List<MailboxFilter> { topLevelWithoutRole, named };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(omitted.HasParentId, Is.False);
            Assert.That(omitted.HasRole, Is.False);
            Assert.That(omitted.HasHasAnyRole, Is.False);
            Assert.That(omitted.HasIsSubscribed, Is.False);
            Assert.That(topLevelWithoutRole.HasParentId, Is.True);
            Assert.That(topLevelWithoutRole.ParentId, Is.Null);
            Assert.That(topLevelWithoutRole.HasRole, Is.True);
            Assert.That(topLevelWithoutRole.Role, Is.Null);
            Assert.That(topLevelWithoutRole.HasHasAnyRole, Is.True);
            Assert.That(topLevelWithoutRole.HasAnyRole, Is.False);
            Assert.That(topLevelWithoutRole.HasIsSubscribed, Is.True);
            Assert.That(topLevelWithoutRole.IsSubscribed, Is.False);
            Assert.That(either.Operator, Is.EqualTo(JmapFilterOperator.Or));
            Assert.That(either.Conditions[1].Name, Is.EqualTo("project"));
            Assert.That(either.Conditions[1].UnknownProperties["vendorPinned"].GetBoolean(), Is.True);
        }
    }

    [Test]
    public void MailboxGetAndChangesKeepOrderingAndCountOnlyOptimization()
    {
        MailboxGetRequest getRequest = new MailboxGetRequest();
        getRequest.AccountId = "account-1";
        getRequest.Ids = null;
        getRequest.Properties = new List<string> { JmapProperties.Id, JmapProperties.Name };

        Mailbox first = new Mailbox();
        first.Id = "mailbox-1";
        Mailbox second = new Mailbox();
        second.Id = "mailbox-2";

        MailboxGetResponse getResponse = new MailboxGetResponse();
        getResponse.AccountId = "account-1";
        getResponse.State = "state-2";
        getResponse.List.Add(first);
        getResponse.List.Add(second);
        getResponse.NotFound.Add("missing");

        MailboxChangesRequest changesRequest = new MailboxChangesRequest();
        changesRequest.AccountId = "account-1";
        changesRequest.SinceState = "state-1";
        changesRequest.HasMaxChanges = true;
        changesRequest.IsMaxChangesNull = true;

        MailboxChangesResponse changesResponse = new MailboxChangesResponse();
        changesResponse.AccountId = "account-1";
        changesResponse.OldState = "state-1";
        changesResponse.NewState = "state-2";
        changesResponse.Updated.Add("mailbox-1");
        changesResponse.UpdatedProperties = new List<string>
        {
            JmapProperties.TotalEmails,
            JmapProperties.UnreadEmails
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(getRequest.Ids, Is.Null);
            Assert.That(getResponse.List, Is.EqualTo(new[] { first, second }));
            Assert.That(getResponse.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(changesRequest.HasMaxChanges, Is.True);
            Assert.That(changesRequest.IsMaxChangesNull, Is.True);
            Assert.That(changesResponse.Updated, Is.EqualTo(new[] { "mailbox-1" }));
            Assert.That(changesResponse.UpdatedProperties, Is.EqualTo(new[]
            {
                JmapProperties.TotalEmails,
                JmapProperties.UnreadEmails
            }));
            Assert.That(new MailboxChangesResponse().UpdatedProperties, Is.Null);
        }
    }

    [Test]
    public void MailboxQueryRepresentsTreeOptionsDefaultsAndServerAppliedWindow()
    {
        MailboxQueryRequest defaults = new MailboxQueryRequest();

        MailboxFilter filter = new MailboxFilter();
        filter.Role = JmapMailboxRoles.Inbox;
        filter.HasRole = true;

        JmapComparator comparator = new JmapComparator();
        comparator.Property = JmapProperties.SortOrder;
        comparator.IsAscending = true;
        comparator.HasIsAscending = true;

        MailboxQueryRequest request = new MailboxQueryRequest();
        request.AccountId = "account-1";
        request.Filter = filter;
        request.Sort = new List<JmapComparator> { comparator };
        request.Position = -10;
        request.HasPosition = true;
        request.Limit = 10;
        request.HasLimit = true;
        request.CalculateTotal = true;
        request.HasCalculateTotal = true;
        request.SortAsTree = true;
        request.HasSortAsTree = true;
        request.FilterAsTree = true;
        request.HasFilterAsTree = true;

        MailboxQueryResponse response = new MailboxQueryResponse();
        response.AccountId = "account-1";
        response.QueryState = "query-state";
        response.CanCalculateChanges = true;
        response.Position = 7;
        response.Ids.Add("mailbox-8");
        response.Total = 17;
        response.HasTotal = true;
        response.Limit = 10;
        response.HasLimit = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(defaults.HasSortAsTree, Is.False);
            Assert.That(defaults.SortAsTree, Is.False);
            Assert.That(defaults.HasFilterAsTree, Is.False);
            Assert.That(defaults.FilterAsTree, Is.False);
            Assert.That(request.Position, Is.EqualTo(-10));
            Assert.That(request.Sort.Single(), Is.SameAs(comparator));
            Assert.That(request.SortAsTree, Is.True);
            Assert.That(request.FilterAsTree, Is.True);
            Assert.That(response.Position, Is.EqualTo(7));
            Assert.That(response.Ids, Is.EqualTo(new[] { "mailbox-8" }));
            Assert.That(response.Total, Is.EqualTo(17));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Limit, Is.EqualTo(10));
            Assert.That(response.HasLimit, Is.True);
        }
    }

    [Test]
    public void MailboxQueryChangesRepresentsNullableMaximumAndOrderedMoves()
    {
        MailboxQueryChangesRequest request = new MailboxQueryChangesRequest();
        request.AccountId = "account-1";
        request.SinceQueryState = "query-1";
        request.MaxChanges = 0;
        request.HasMaxChanges = true;
        request.IsMaxChangesNull = true;
        request.UpToId = "mailbox-20";
        request.CalculateTotal = true;
        request.HasCalculateTotal = true;

        JmapAddedItem moved = new JmapAddedItem();
        moved.Id = "mailbox-2";
        moved.Index = 0;
        JmapAddedItem inserted = new JmapAddedItem();
        inserted.Id = "mailbox-new";
        inserted.Index = 2;

        MailboxQueryChangesResponse response = new MailboxQueryChangesResponse();
        response.AccountId = "account-1";
        response.OldQueryState = "query-1";
        response.NewQueryState = "query-2";
        response.Total = 21;
        response.HasTotal = true;
        response.Removed.Add("mailbox-2");
        response.Added.Add(moved);
        response.Added.Add(inserted);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.HasMaxChanges, Is.True);
            Assert.That(request.IsMaxChangesNull, Is.True);
            Assert.That(request.UpToId, Is.EqualTo("mailbox-20"));
            Assert.That(response.Removed, Is.EqualTo(new[] { "mailbox-2" }));
            Assert.That(response.Added, Is.EqualTo(new[] { moved, inserted }));
            Assert.That(response.Added.Select(item => item.Index), Is.EqualTo(new long[] { 0, 2 }));
            Assert.That(response.Total, Is.EqualTo(21));
            Assert.That(response.HasTotal, Is.True);
        }
    }

    [Test]
    public void MailboxSetPreservesCreatePatchDestroyAndPerRecordErrors()
    {
        Mailbox createdMailbox = new Mailbox();
        createdMailbox.Name = "Projects";
        createdMailbox.ParentId = "mailbox-parent";

        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add(JmapProperties.Name, Json("\"Renamed\""));

        MailboxSetRequest request = new MailboxSetRequest();
        request.AccountId = "account-1";
        request.IfInState = "state-1";
        request.Create = new Dictionary<string, Mailbox>();
        request.Create.Add("create-1", createdMailbox);
        request.Update = new Dictionary<string, JmapPatchObject>();
        request.Update.Add("mailbox-1", patch);
        request.Destroy = new List<string> { "mailbox-old" };
        request.OnDestroyRemoveEmails = false;
        request.HasOnDestroyRemoveEmails = true;

        JmapSetError error = new JmapSetError();
        error.Type = JmapErrorCodes.MailboxHasEmail;

        MailboxSetResponse response = new MailboxSetResponse();
        response.AccountId = "account-1";
        response.OldState = "state-1";
        response.NewState = "state-2";
        response.Created = new Dictionary<string, Mailbox>();
        Mailbox createdResult = new Mailbox();
        createdResult.Id = "mailbox-new";
        response.Created.Add("create-1", createdResult);
        response.Updated = new Dictionary<string, JsonElement>();
        response.Updated.Add("mailbox-1", Json("null"));
        response.NotDestroyed = new Dictionary<string, JmapSetError>();
        response.NotDestroyed.Add("mailbox-old", error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Create["create-1"], Is.SameAs(createdMailbox));
            Assert.That(request.Update["mailbox-1"], Is.SameAs(patch));
            Assert.That(request.HasOnDestroyRemoveEmails, Is.True);
            Assert.That(request.OnDestroyRemoveEmails, Is.False);
            Assert.That(response.Created["create-1"].Id, Is.EqualTo("mailbox-new"));
            Assert.That(response.Updated["mailbox-1"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(response.NotDestroyed["mailbox-old"], Is.SameAs(error));
            Assert.That(response.Destroyed, Is.Null);
            Assert.That(response.NotCreated, Is.Null);
            Assert.That(response.NotUpdated, Is.Null);
        }
    }

    [Test]
    public void RequiredMailboxResultCollectionsAreIndependent()
    {
        MailboxGetResponse firstGet = new MailboxGetResponse();
        MailboxGetResponse secondGet = new MailboxGetResponse();
        firstGet.NotFound.Add("missing");

        MailboxChangesResponse firstChanges = new MailboxChangesResponse();
        MailboxChangesResponse secondChanges = new MailboxChangesResponse();
        firstChanges.Created.Add("created");

        MailboxQueryResponse firstQuery = new MailboxQueryResponse();
        MailboxQueryResponse secondQuery = new MailboxQueryResponse();
        firstQuery.Ids.Add("mailbox-1");

        MailboxQueryChangesResponse firstQueryChanges = new MailboxQueryChangesResponse();
        MailboxQueryChangesResponse secondQueryChanges = new MailboxQueryChangesResponse();
        firstQueryChanges.Removed.Add("mailbox-2");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondGet.NotFound, Is.Empty);
            Assert.That(secondChanges.Created, Is.Empty);
            Assert.That(secondQuery.Ids, Is.Empty);
            Assert.That(secondQueryChanges.Removed, Is.Empty);
            Assert.That(firstGet.NotFound, Is.Not.SameAs(secondGet.NotFound));
            Assert.That(firstChanges.Created, Is.Not.SameAs(secondChanges.Created));
            Assert.That(firstQuery.Ids, Is.Not.SameAs(secondQuery.Ids));
            Assert.That(firstQueryChanges.Removed, Is.Not.SameAs(secondQueryChanges.Removed));
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
