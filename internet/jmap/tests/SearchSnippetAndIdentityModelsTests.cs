using System.Text.Json;

namespace jmap.Tests;

public class SearchSnippetAndIdentityModelsTests
{
    [Test]
    public void SearchSnippetsPreserveEscapedHighlightsNullMatchesAndServerOrdering()
    {
        EmailFilter filter = new EmailFilter();
        filter.Text = "foo";

        SearchSnippetGetRequest request = new SearchSnippetGetRequest();
        request.AccountId = "account-1";
        request.Filter = filter;
        request.EmailIds.Add("email-1");
        request.EmailIds.Add("email-2");

        SearchSnippet noMatch = new SearchSnippet();
        noMatch.EmailId = "email-1";

        SearchSnippet match = new SearchSnippet();
        match.EmailId = "email-2";
        match.Subject = "The <mark>Foo</mark> report";
        match.Preview = "...returns &lt;1 <mark>foo</mark> result...";
        match.UnknownProperties.Add("vendorRank", Json("1"));

        SearchSnippetGetResponse response = new SearchSnippetGetResponse();
        response.AccountId = "account-1";
        response.List.Add(match);
        response.List.Add(noMatch);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Filter, Is.SameAs(filter));
            Assert.That(request.EmailIds, Is.EqualTo(new[] { "email-1", "email-2" }));
            Assert.That(response.List, Is.EqualTo(new[] { match, noMatch }));
            Assert.That(response.List[0].Subject, Does.Contain("<mark>Foo</mark>"));
            Assert.That(response.List[0].Preview, Does.Contain("&lt;1"));
            Assert.That(response.List[1].Subject, Is.Null);
            Assert.That(response.List[1].Preview, Is.Null);
            Assert.That(response.NotFound, Is.Null);
            Assert.That(match.UnknownProperties["vendorRank"].GetInt32(), Is.EqualTo(1));
        }
    }

    [Test]
    public void SearchSnippetResponseRepresentsMissingEmailsSeparately()
    {
        SearchSnippetGetResponse response = new SearchSnippetGetResponse();
        response.NotFound = new List<string> { "missing-1", "missing-2" };

        Assert.That(response.NotFound, Is.EqualTo(new[] { "missing-1", "missing-2" }));
    }

    [Test]
    public void IdentityPreservesDefaultsAddressesSignaturesAndDeletionPermission()
    {
        EmailAddress reply = new EmailAddress();
        reply.Name = "Support";
        reply.Email = "support@example.com";
        EmailAddress archive = new EmailAddress();
        archive.Email = "archive@example.com";

        Identity identity = new Identity();
        identity.Id = "identity-1";
        identity.Name = "Alice";
        identity.Email = "alice@example.com";
        identity.ReplyTo = new List<EmailAddress> { reply };
        identity.Bcc = new List<EmailAddress> { archive };
        identity.TextSignature = "-- \nAlice";
        identity.HtmlSignature = "<div>Alice</div>";
        identity.MayDelete = false;
        identity.UnknownProperties.Add("vendorDefault", Json("true"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(identity.ReplyTo.Single(), Is.SameAs(reply));
            Assert.That(identity.Bcc.Single(), Is.SameAs(archive));
            Assert.That(identity.TextSignature, Does.StartWith("--"));
            Assert.That(identity.HtmlSignature, Is.EqualTo("<div>Alice</div>"));
            Assert.That(identity.MayDelete, Is.False);
            Assert.That(identity.UnknownProperties["vendorDefault"].GetBoolean(), Is.True);
            Assert.That(new Identity().ReplyTo, Is.Null);
            Assert.That(new Identity().Bcc, Is.Null);
        }
    }

    [Test]
    public void IdentityGetAndChangesPreserveOrderingProjectionAndPagedPartitions()
    {
        IdentityGetRequest getRequest = new IdentityGetRequest();
        getRequest.AccountId = "account-1";
        getRequest.Ids = null;
        getRequest.Properties = new List<string> { JmapProperties.Id, JmapProperties.Email };

        Identity first = new Identity();
        first.Id = "identity-1";
        Identity second = new Identity();
        second.Id = "identity-2";

        IdentityGetResponse getResponse = new IdentityGetResponse();
        getResponse.AccountId = "account-1";
        getResponse.State = "state-2";
        getResponse.List.Add(first);
        getResponse.List.Add(second);
        getResponse.NotFound.Add("missing");

        IdentityChangesRequest changesRequest = new IdentityChangesRequest();
        changesRequest.AccountId = "account-1";
        changesRequest.SinceState = "state-1";
        changesRequest.HasMaxChanges = true;
        changesRequest.IsMaxChangesNull = true;

        IdentityChangesResponse changesResponse = new IdentityChangesResponse();
        changesResponse.OldState = "state-1";
        changesResponse.NewState = "state-2";
        changesResponse.HasMoreChanges = true;
        changesResponse.Created.Add("created");
        changesResponse.Updated.Add("updated");
        changesResponse.Destroyed.Add("destroyed");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(getRequest.Ids, Is.Null);
            Assert.That(getResponse.List, Is.EqualTo(new[] { first, second }));
            Assert.That(getResponse.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(changesRequest.HasMaxChanges, Is.True);
            Assert.That(changesRequest.IsMaxChangesNull, Is.True);
            Assert.That(changesResponse.HasMoreChanges, Is.True);
            Assert.That(changesResponse.Created, Is.EqualTo(new[] { "created" }));
            Assert.That(changesResponse.Updated, Is.EqualTo(new[] { "updated" }));
            Assert.That(changesResponse.Destroyed, Is.EqualTo(new[] { "destroyed" }));
        }
    }

    [Test]
    public void IdentitySetPreservesCreatePatchDestroyAndForbiddenFromOutcome()
    {
        Identity identity = new Identity();
        identity.Email = "*@example.com";

        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add(JmapProperties.TextSignature, Json("\"Updated signature\""));

        IdentitySetRequest request = new IdentitySetRequest();
        request.AccountId = "account-1";
        request.IfInState = "state-1";
        request.Create = new Dictionary<string, Identity> { { "create-1", identity } };
        request.Update = new Dictionary<string, JmapPatchObject> { { "identity-1", patch } };
        request.Destroy = new List<string> { "identity-old" };

        JmapSetError error = new JmapSetError();
        error.Type = JmapErrorCodes.ForbiddenFrom;
        error.Kind = JmapSetErrorKind.ForbiddenFrom;

        IdentitySetResponse response = new IdentitySetResponse();
        response.AccountId = "account-1";
        response.OldState = "state-1";
        response.NewState = "state-2";
        response.NotCreated = new Dictionary<string, JmapSetError> { { "create-1", error } };
        response.Updated = new Dictionary<string, JsonElement> { { "identity-1", Json("null") } };
        response.Destroyed = new List<string> { "identity-old" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Create["create-1"], Is.SameAs(identity));
            Assert.That(request.Update["identity-1"], Is.SameAs(patch));
            Assert.That(response.NotCreated["create-1"], Is.SameAs(error));
            Assert.That(response.Updated["identity-1"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "identity-old" }));
            Assert.That(response.Created, Is.Null);
            Assert.That(response.NotUpdated, Is.Null);
            Assert.That(response.NotDestroyed, Is.Null);
        }
    }

    [Test]
    public void RequiredSnippetAndIdentityCollectionsAreIndependent()
    {
        SearchSnippetGetRequest firstSnippet = new SearchSnippetGetRequest();
        SearchSnippetGetRequest secondSnippet = new SearchSnippetGetRequest();
        firstSnippet.EmailIds.Add("email-1");

        IdentityGetResponse firstGet = new IdentityGetResponse();
        IdentityGetResponse secondGet = new IdentityGetResponse();
        firstGet.NotFound.Add("missing");

        IdentityChangesResponse firstChanges = new IdentityChangesResponse();
        IdentityChangesResponse secondChanges = new IdentityChangesResponse();
        firstChanges.Updated.Add("identity-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondSnippet.EmailIds, Is.Empty);
            Assert.That(secondGet.NotFound, Is.Empty);
            Assert.That(secondChanges.Updated, Is.Empty);
            Assert.That(firstSnippet.EmailIds, Is.Not.SameAs(secondSnippet.EmailIds));
            Assert.That(firstGet.NotFound, Is.Not.SameAs(secondGet.NotFound));
            Assert.That(firstChanges.Updated, Is.Not.SameAs(secondChanges.Updated));
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
