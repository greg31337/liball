using System.Text.Json;
using JmapThread = jmap.Thread;

namespace jmap.Tests;

public class ThreadModelsTests
{
    [Test]
    public void ThreadPreservesStableOldestFirstEmailOrderAndExtensions()
    {
        JmapThread thread = new JmapThread();
        thread.Id = "thread-1";
        thread.EmailIds.Add("email-oldest");
        thread.EmailIds.Add("email-middle");
        thread.EmailIds.Add("email-newest");
        thread.UnknownProperties.Add("vendorSubject", Json("\"Project update\""));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(thread.Id, Is.EqualTo("thread-1"));
            Assert.That(thread.EmailIds, Is.EqualTo(new[]
            {
                "email-oldest",
                "email-middle",
                "email-newest"
            }));
            Assert.That(thread.UnknownProperties["vendorSubject"].GetString(), Is.EqualTo("Project update"));
        }
    }

    [Test]
    public void ThreadGetRepresentsAllThreadsProjectionOrderingAndNotFoundIds()
    {
        ThreadGetRequest all = new ThreadGetRequest();
        all.AccountId = "account-1";

        ThreadGetRequest selected = new ThreadGetRequest();
        selected.AccountId = "account-1";
        selected.Ids = new List<string> { "thread-2", "thread-1" };
        selected.Properties = new List<string> { JmapProperties.Id, JmapProperties.EmailIds };
        selected.UnknownProperties.Add("vendorArgument", Json("true"));

        JmapThread second = new JmapThread();
        second.Id = "thread-2";
        JmapThread first = new JmapThread();
        first.Id = "thread-1";

        ThreadGetResponse response = new ThreadGetResponse();
        response.AccountId = "account-1";
        response.State = "thread-state";
        response.List.Add(second);
        response.List.Add(first);
        response.NotFound.Add("thread-missing");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(all.Ids, Is.Null);
            Assert.That(all.Properties, Is.Null);
            Assert.That(selected.Ids, Is.EqualTo(new[] { "thread-2", "thread-1" }));
            Assert.That(selected.Properties, Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.EmailIds }));
            Assert.That(selected.UnknownProperties["vendorArgument"].GetBoolean(), Is.True);
            Assert.That(response.List, Is.EqualTo(new[] { second, first }));
            Assert.That(response.NotFound, Is.EqualTo(new[] { "thread-missing" }));
        }
    }

    [Test]
    public void ThreadChangesDistinguishesMissingNullAndLimitedMaximums()
    {
        ThreadChangesRequest missing = new ThreadChangesRequest();

        ThreadChangesRequest explicitNull = new ThreadChangesRequest();
        explicitNull.HasMaxChanges = true;
        explicitNull.IsMaxChangesNull = true;

        ThreadChangesRequest limited = new ThreadChangesRequest();
        limited.AccountId = "account-1";
        limited.SinceState = "state-1";
        limited.MaxChanges = 25;
        limited.HasMaxChanges = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.HasMaxChanges, Is.False);
            Assert.That(missing.IsMaxChangesNull, Is.False);
            Assert.That(explicitNull.HasMaxChanges, Is.True);
            Assert.That(explicitNull.IsMaxChangesNull, Is.True);
            Assert.That(limited.MaxChanges, Is.EqualTo(25));
            Assert.That(limited.HasMaxChanges, Is.True);
            Assert.That(limited.IsMaxChangesNull, Is.False);
        }
    }

    [Test]
    public void ThreadChangesPreservesPagedCreatedUpdatedAndDestroyedPartitions()
    {
        ThreadChangesResponse response = new ThreadChangesResponse();
        response.AccountId = "account-1";
        response.OldState = "state-1";
        response.NewState = "state-2";
        response.HasMoreChanges = true;
        response.Created.Add("thread-created");
        response.Updated.Add("thread-membership-changed");
        response.Destroyed.Add("thread-merged-away");
        response.UnknownProperties.Add("vendorCheckpoint", Json("17"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.AccountId, Is.EqualTo("account-1"));
            Assert.That(response.OldState, Is.EqualTo("state-1"));
            Assert.That(response.NewState, Is.EqualTo("state-2"));
            Assert.That(response.HasMoreChanges, Is.True);
            Assert.That(response.Created, Is.EqualTo(new[] { "thread-created" }));
            Assert.That(response.Updated, Is.EqualTo(new[] { "thread-membership-changed" }));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "thread-merged-away" }));
            Assert.That(response.UnknownProperties["vendorCheckpoint"].GetInt32(), Is.EqualTo(17));
        }
    }

    [Test]
    public void RequiredThreadCollectionsAreIndependent()
    {
        JmapThread firstThread = new JmapThread();
        JmapThread secondThread = new JmapThread();
        firstThread.EmailIds.Add("email-1");

        ThreadGetResponse firstGet = new ThreadGetResponse();
        ThreadGetResponse secondGet = new ThreadGetResponse();
        firstGet.NotFound.Add("missing");

        ThreadChangesResponse firstChanges = new ThreadChangesResponse();
        ThreadChangesResponse secondChanges = new ThreadChangesResponse();
        firstChanges.Updated.Add("thread-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondThread.EmailIds, Is.Empty);
            Assert.That(secondGet.NotFound, Is.Empty);
            Assert.That(secondChanges.Updated, Is.Empty);
            Assert.That(firstThread.EmailIds, Is.Not.SameAs(secondThread.EmailIds));
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
