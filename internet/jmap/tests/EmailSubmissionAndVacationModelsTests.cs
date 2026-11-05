using System.Text.Json;

namespace jmap.Tests;

public class EmailSubmissionAndVacationModelsTests
{
    [Test]
    public void SubmissionEnvelopePreservesRecipientsAndStringOrNullSmtpParameters()
    {
        EmailSubmissionAddress mailFrom = new EmailSubmissionAddress();
        mailFrom.Email = "sender@example.com";
        mailFrom.Parameters = new Dictionary<string, JsonElement>();
        mailFrom.Parameters.Add("RET", Json("\"FULL\""));
        mailFrom.Parameters.Add("SMTPUTF8", Json("null"));

        EmailSubmissionAddress recipient = new EmailSubmissionAddress();
        recipient.Email = "recipient@example.com";
        recipient.Parameters = new Dictionary<string, JsonElement>();
        recipient.Parameters.Add("NOTIFY", Json("\"SUCCESS,FAILURE\""));

        EmailSubmissionEnvelope envelope = new EmailSubmissionEnvelope();
        envelope.MailFrom = mailFrom;
        envelope.RcptTo.Add(recipient);
        envelope.UnknownProperties.Add("vendorRoute", Json("\"primary\""));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(envelope.MailFrom, Is.SameAs(mailFrom));
            Assert.That(envelope.RcptTo.Single(), Is.SameAs(recipient));
            Assert.That(mailFrom.Parameters["RET"].GetString(), Is.EqualTo("FULL"));
            Assert.That(mailFrom.Parameters["SMTPUTF8"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(recipient.Parameters["NOTIFY"].GetString(), Is.EqualTo("SUCCESS,FAILURE"));
            Assert.That(envelope.UnknownProperties["vendorRoute"].GetString(), Is.EqualTo("primary"));
            Assert.That(new EmailSubmissionAddress().Parameters, Is.Null);
        }
    }

    [Test]
    public void EmailSubmissionPreservesCancellationDeliveryAndNotificationHistory()
    {
        EmailDeliveryStatus delivered = new EmailDeliveryStatus();
        delivered.SmtpReply = "250 2.0.0 Message accepted";
        delivered.Delivered = "yes";
        delivered.Displayed = "unknown";
        delivered.UnknownProperties.Add("vendorAttempts", Json("1"));

        EmailSubmission submission = new EmailSubmission();
        submission.Id = "submission-1";
        submission.IdentityId = "identity-1";
        submission.EmailId = "email-1";
        submission.ThreadId = "thread-1";
        submission.SendAt = "2026-09-10T12:00:00Z";
        submission.UndoStatus = JmapSubmissionUndoStatus.Final;
        submission.HasUndoStatus = true;
        submission.DeliveryStatus = new Dictionary<string, EmailDeliveryStatus>();
        submission.DeliveryStatus.Add("recipient@example.com", delivered);
        submission.DsnBlobIds = new List<string> { "dsn-old", "dsn-new" };
        submission.MdnBlobIds = new List<string> { "mdn-1" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(submission.UndoStatus, Is.EqualTo(JmapSubmissionUndoStatus.Final));
            Assert.That(submission.HasUndoStatus, Is.True);
            Assert.That(submission.DeliveryStatus["recipient@example.com"], Is.SameAs(delivered));
            Assert.That(delivered.Delivered, Is.EqualTo("yes"));
            Assert.That(delivered.Displayed, Is.EqualTo("unknown"));
            Assert.That(delivered.UnknownProperties["vendorAttempts"].GetInt32(), Is.EqualTo(1));
            Assert.That(submission.DsnBlobIds, Is.EqualTo(new[] { "dsn-old", "dsn-new" }));
            Assert.That(submission.MdnBlobIds, Is.EqualTo(new[] { "mdn-1" }));
            Assert.That(new EmailSubmission().DeliveryStatus, Is.Null);
        }
    }

    [Test]
    public void SubmissionFilterAndQueryPreserveRecursiveCriteriaAndStableSorting()
    {
        EmailSubmissionFilter identity = new EmailSubmissionFilter();
        identity.IdentityIds = new List<string> { "identity-1", "identity-2" };
        identity.EmailIds = new List<string> { "email-1" };
        identity.ThreadIds = new List<string> { "thread-1" };
        identity.UndoStatus = JmapSubmissionUndoStatus.Pending;
        identity.HasUndoStatus = true;
        identity.Before = "2026-10-01T00:00:00Z";
        identity.After = "2026-09-01T00:00:00Z";

        EmailSubmissionFilter not = new EmailSubmissionFilter();
        not.Operator = JmapFilterOperator.Not;
        not.HasOperator = true;
        not.Conditions = new List<EmailSubmissionFilter> { identity };

        EmailSubmissionComparator comparator = new EmailSubmissionComparator();
        comparator.Property = JmapProperties.SentAt;
        comparator.IsAscending = false;
        comparator.HasIsAscending = true;

        EmailSubmissionQueryRequest request = new EmailSubmissionQueryRequest();
        request.AccountId = "account-1";
        request.Filter = not;
        request.Sort = new List<EmailSubmissionComparator> { comparator };
        request.Position = -10;
        request.HasPosition = true;
        request.Limit = 10;
        request.HasLimit = true;
        request.CalculateTotal = true;
        request.HasCalculateTotal = true;

        EmailSubmissionQueryResponse response = new EmailSubmissionQueryResponse();
        response.AccountId = "account-1";
        response.QueryState = "query-1";
        response.CanCalculateChanges = true;
        response.Position = 15;
        response.Ids.Add("submission-16");
        response.Total = 26;
        response.HasTotal = true;
        response.Limit = 10;
        response.HasLimit = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(not.Operator, Is.EqualTo(JmapFilterOperator.Not));
            Assert.That(not.Conditions.Single(), Is.SameAs(identity));
            Assert.That(identity.IdentityIds, Is.EqualTo(new[] { "identity-1", "identity-2" }));
            Assert.That(identity.UndoStatus, Is.EqualTo(JmapSubmissionUndoStatus.Pending));
            Assert.That(identity.HasUndoStatus, Is.True);
            Assert.That(request.Sort.Single(), Is.SameAs(comparator));
            Assert.That(request.Position, Is.EqualTo(-10));
            Assert.That(response.Ids, Is.EqualTo(new[] { "submission-16" }));
            Assert.That(response.Total, Is.EqualTo(26));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Limit, Is.EqualTo(10));
        }
    }

    [Test]
    public void SubmissionGetAndChangesPreserveProjectionAndPagedPartitions()
    {
        EmailSubmissionGetRequest getRequest = new EmailSubmissionGetRequest();
        getRequest.AccountId = "account-1";
        getRequest.Ids = new List<string> { "submission-1" };
        getRequest.Properties = new List<string> { JmapProperties.Id, JmapProperties.UndoStatus };

        EmailSubmission submission = new EmailSubmission();
        submission.Id = "submission-1";
        EmailSubmissionGetResponse getResponse = new EmailSubmissionGetResponse();
        getResponse.State = "state-2";
        getResponse.List.Add(submission);
        getResponse.NotFound.Add("missing");

        EmailSubmissionChangesRequest changesRequest = new EmailSubmissionChangesRequest();
        changesRequest.AccountId = "account-1";
        changesRequest.SinceState = "state-1";
        changesRequest.HasMaxChanges = true;
        changesRequest.IsMaxChangesNull = true;

        EmailSubmissionChangesResponse changesResponse = new EmailSubmissionChangesResponse();
        changesResponse.OldState = "state-1";
        changesResponse.NewState = "state-2";
        changesResponse.HasMoreChanges = true;
        changesResponse.Created.Add("created");
        changesResponse.Updated.Add("updated");
        changesResponse.Destroyed.Add("destroyed");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(getResponse.List.Single(), Is.SameAs(submission));
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
    public void SubmissionQueryChangesPreservesNullableMaximumAndOrderedMoves()
    {
        EmailSubmissionQueryChangesRequest request = new EmailSubmissionQueryChangesRequest();
        request.AccountId = "account-1";
        request.SinceQueryState = "query-1";
        request.HasMaxChanges = true;
        request.IsMaxChangesNull = true;
        request.UpToId = "submission-50";

        JmapAddedItem moved = new JmapAddedItem();
        moved.Id = "submission-moved";
        moved.Index = 2;

        EmailSubmissionQueryChangesResponse response = new EmailSubmissionQueryChangesResponse();
        response.OldQueryState = "query-1";
        response.NewQueryState = "query-2";
        response.Total = 50;
        response.HasTotal = true;
        response.Removed.Add("submission-moved");
        response.Added.Add(moved);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.HasMaxChanges, Is.True);
            Assert.That(request.IsMaxChangesNull, Is.True);
            Assert.That(request.UpToId, Is.EqualTo("submission-50"));
            Assert.That(response.Removed, Is.EqualTo(new[] { "submission-moved" }));
            Assert.That(response.Added.Single(), Is.SameAs(moved));
            Assert.That(response.Total, Is.EqualTo(50));
            Assert.That(response.HasTotal, Is.True);
        }
    }

    [Test]
    public void SubmissionSetPreservesEnvelopeCancellationAndImplicitEmailActions()
    {
        EmailSubmission submission = new EmailSubmission();
        submission.IdentityId = "identity-1";
        submission.EmailId = "email-1";

        JmapPatchObject cancel = new JmapPatchObject();
        cancel.Values.Add(JmapProperties.UndoStatus, Json("\"canceled\""));
        JmapPatchObject updateEmail = new JmapPatchObject();
        updateEmail.Values.Add("keywords/$draft", Json("null"));

        EmailSubmissionSetRequest request = new EmailSubmissionSetRequest();
        request.AccountId = "account-1";
        request.IfInState = "state-1";
        request.Create = new Dictionary<string, EmailSubmission> { { "create-1", submission } };
        request.Update = new Dictionary<string, JmapPatchObject> { { "submission-1", cancel } };
        request.OnSuccessUpdateEmail = new Dictionary<string, JmapPatchObject> { { "#create-1", updateEmail } };
        request.OnSuccessDestroyEmail = new List<string> { "submission-old" };

        EmailSubmission created = new EmailSubmission();
        created.Id = "submission-new";
        JmapSetError error = new JmapSetError();
        error.Type = JmapErrorCodes.CannotUnsend;
        error.Kind = JmapSetErrorKind.CannotUnsend;

        EmailSubmissionSetResponse response = new EmailSubmissionSetResponse();
        response.AccountId = "account-1";
        response.OldState = "state-1";
        response.NewState = "state-2";
        response.Created = new Dictionary<string, EmailSubmission> { { "create-1", created } };
        response.NotUpdated = new Dictionary<string, JmapSetError> { { "submission-1", error } };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Create["create-1"], Is.SameAs(submission));
            Assert.That(request.Update["submission-1"], Is.SameAs(cancel));
            Assert.That(request.OnSuccessUpdateEmail["#create-1"], Is.SameAs(updateEmail));
            Assert.That(request.OnSuccessDestroyEmail, Is.EqualTo(new[] { "submission-old" }));
            Assert.That(response.Created["create-1"], Is.SameAs(created));
            Assert.That(response.NotUpdated["submission-1"], Is.SameAs(error));
            Assert.That(response.Updated, Is.Null);
            Assert.That(response.Destroyed, Is.Null);
        }
    }

    [Test]
    public void VacationResponseRepresentsOpenEndedWindowAndSingletonGet()
    {
        VacationResponse vacation = new VacationResponse();
        vacation.Id = "singleton";
        vacation.IsEnabled = true;
        vacation.FromDate = null;
        vacation.ToDate = null;
        vacation.Subject = null;
        vacation.TextBody = "I am away.";
        vacation.HtmlBody = null;
        vacation.UnknownProperties.Add("vendorLocale", Json("\"en-US\""));

        VacationResponseGetRequest request = new VacationResponseGetRequest();
        request.AccountId = "account-1";

        VacationResponseGetResponse response = new VacationResponseGetResponse();
        response.AccountId = "account-1";
        response.State = "vacation-state";
        response.List.Add(vacation);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Ids, Is.Null);
            Assert.That(vacation.Id, Is.EqualTo("singleton"));
            Assert.That(vacation.IsEnabled, Is.True);
            Assert.That(vacation.FromDate, Is.Null);
            Assert.That(vacation.ToDate, Is.Null);
            Assert.That(vacation.Subject, Is.Null);
            Assert.That(vacation.TextBody, Is.EqualTo("I am away."));
            Assert.That(vacation.UnknownProperties["vendorLocale"].GetString(), Is.EqualTo("en-US"));
            Assert.That(response.List.Single(), Is.SameAs(vacation));
            Assert.That(response.NotFound, Is.Empty);
        }
    }

    [Test]
    public void VacationSetPreservesNullResetAndSingletonErrors()
    {
        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add(JmapProperties.IsEnabled, Json("false"));
        patch.Values.Add(JmapProperties.ToDate, Json("null"));

        VacationResponseSetRequest request = new VacationResponseSetRequest();
        request.AccountId = "account-1";
        request.IfInState = "state-1";
        request.Update = new Dictionary<string, JmapPatchObject> { { "singleton", patch } };

        JmapSetError singletonError = new JmapSetError();
        singletonError.Type = JmapErrorCodes.Singleton;
        singletonError.Kind = JmapSetErrorKind.Singleton;

        VacationResponseSetResponse response = new VacationResponseSetResponse();
        response.OldState = "state-1";
        response.NewState = "state-2";
        response.Updated = new Dictionary<string, JsonElement> { { "singleton", Json("null") } };
        response.NotCreated = new Dictionary<string, JmapSetError> { { "create-1", singletonError } };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Update["singleton"], Is.SameAs(patch));
            Assert.That(patch.Values[JmapProperties.IsEnabled].GetBoolean(), Is.False);
            Assert.That(patch.Values[JmapProperties.ToDate].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(response.Updated["singleton"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(response.NotCreated["create-1"], Is.SameAs(singletonError));
        }
    }

    [Test]
    public void RequiredSubmissionAndVacationCollectionsAreIndependent()
    {
        EmailSubmissionEnvelope firstEnvelope = new EmailSubmissionEnvelope();
        EmailSubmissionEnvelope secondEnvelope = new EmailSubmissionEnvelope();
        firstEnvelope.RcptTo.Add(new EmailSubmissionAddress());

        EmailSubmissionGetResponse firstGet = new EmailSubmissionGetResponse();
        EmailSubmissionGetResponse secondGet = new EmailSubmissionGetResponse();
        firstGet.NotFound.Add("missing");

        EmailSubmissionQueryChangesResponse firstChanges = new EmailSubmissionQueryChangesResponse();
        EmailSubmissionQueryChangesResponse secondChanges = new EmailSubmissionQueryChangesResponse();
        firstChanges.Removed.Add("submission-1");

        VacationResponseGetResponse firstVacation = new VacationResponseGetResponse();
        VacationResponseGetResponse secondVacation = new VacationResponseGetResponse();
        firstVacation.List.Add(new VacationResponse());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondEnvelope.RcptTo, Is.Empty);
            Assert.That(secondGet.NotFound, Is.Empty);
            Assert.That(secondChanges.Removed, Is.Empty);
            Assert.That(secondVacation.List, Is.Empty);
            Assert.That(firstEnvelope.RcptTo, Is.Not.SameAs(secondEnvelope.RcptTo));
            Assert.That(firstGet.NotFound, Is.Not.SameAs(secondGet.NotFound));
            Assert.That(firstChanges.Removed, Is.Not.SameAs(secondChanges.Removed));
            Assert.That(firstVacation.List, Is.Not.SameAs(secondVacation.List));
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
