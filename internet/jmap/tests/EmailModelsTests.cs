using System.Text.Json;

namespace jmap.Tests;

public class EmailModelsTests
{
    [Test]
    public void EmailPreservesMetadataConvenienceHeadersAndDynamicHeaderValues()
    {
        EmailAddress author = new EmailAddress();
        author.Name = "Alice";
        author.Email = "alice@example.com";

        EmailHeader subjectHeader = new EmailHeader();
        subjectHeader.Name = "Subject";
        subjectHeader.Value = "Project update";

        Email email = new Email();
        email.Id = "email-1";
        email.BlobId = "blob-1";
        email.ThreadId = "thread-1";
        email.MailboxIds = new Dictionary<string, bool> { { "inbox", true } };
        email.Keywords = new Dictionary<string, bool> { { JmapMailKeywords.Seen, true } };
        email.Size = 4096;
        email.ReceivedAt = "2026-09-10T12:00:00Z";
        email.Headers = new List<EmailHeader> { subjectHeader };
        email.HeaderProperties.Add("header:List-Post:asURLs", Json("[\"mailto:list@example.com\"]"));
        email.MessageId = new List<string> { "message@example.com" };
        email.From = new List<EmailAddress> { author };
        email.Subject = "Project update";
        email.SentAt = "2026-09-10T11:59:00Z";
        email.HasAttachment = true;
        email.Preview = "The latest project update";
        email.UnknownProperties.Add("vendorScore", Json("0.95"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(email.MailboxIds["inbox"], Is.True);
            Assert.That(email.Keywords[JmapMailKeywords.Seen], Is.True);
            Assert.That(email.Headers.Single(), Is.SameAs(subjectHeader));
            Assert.That(email.From.Single(), Is.SameAs(author));
            Assert.That(email.HeaderProperties["header:List-Post:asURLs"][0].GetString(), Is.EqualTo("mailto:list@example.com"));
            Assert.That(email.MessageId, Is.EqualTo(new[] { "message@example.com" }));
            Assert.That(email.HasAttachment, Is.True);
            Assert.That(email.UnknownProperties["vendorScore"].GetDouble(), Is.EqualTo(0.95));
        }
    }

    [Test]
    public void AddressGroupsPreserveNamedAndUngroupedMailboxOrdering()
    {
        EmailAddress ungroupedAddress = new EmailAddress();
        ungroupedAddress.Email = "first@example.com";
        EmailAddress groupedAddress = new EmailAddress();
        groupedAddress.Name = "Second";
        groupedAddress.Email = "second@example.com";

        EmailAddressGroup ungrouped = new EmailAddressGroup();
        ungrouped.Addresses.Add(ungroupedAddress);
        EmailAddressGroup friends = new EmailAddressGroup();
        friends.Name = "Friends";
        friends.Addresses.Add(groupedAddress);
        friends.UnknownProperties.Add("vendorGroupId", Json("\"group-7\""));

        List<EmailAddressGroup> groups = new List<EmailAddressGroup> { ungrouped, friends };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(groups[0].Name, Is.Null);
            Assert.That(groups[0].Addresses.Single().Email, Is.EqualTo("first@example.com"));
            Assert.That(groups[1].Name, Is.EqualTo("Friends"));
            Assert.That(groups[1].Addresses.Single(), Is.SameAs(groupedAddress));
            Assert.That(groups[1].UnknownProperties["vendorGroupId"].GetString(), Is.EqualTo("group-7"));
        }
    }

    [Test]
    public void BodyModelsPreserveRecursiveMimeStructureProjectionAndDecodedValues()
    {
        EmailBodyPart text = new EmailBodyPart();
        text.PartId = "1";
        text.BlobId = "blob-text";
        text.Size = 12;
        text.HasSize = true;
        text.Type = "text/plain";
        text.Charset = "utf-8";
        text.Language = new List<string> { "en" };
        text.HeaderProperties.Add("header:Content-Type", Json("\"text/plain; charset=utf-8\""));

        EmailBodyPart attachment = new EmailBodyPart();
        attachment.PartId = "2";
        attachment.BlobId = "blob-file";
        attachment.Size = 300;
        attachment.HasSize = true;
        attachment.Name = "report.pdf";
        attachment.Type = "application/pdf";
        attachment.Disposition = "attachment";
        attachment.Cid = "report-content";
        attachment.Location = "https://example.com/report.pdf";

        EmailBodyPart root = new EmailBodyPart();
        root.Type = "multipart/mixed";
        root.SubParts = new List<EmailBodyPart> { text, attachment };

        EmailBodyValue value = new EmailBodyValue();
        value.Value = "hello\nworld";
        value.IsEncodingProblem = false;
        value.IsTruncated = true;

        Email email = new Email();
        email.BodyStructure = root;
        email.BodyValues = new Dictionary<string, EmailBodyValue> { { "1", value } };
        email.TextBody = new List<EmailBodyPart> { text };
        email.HtmlBody = new List<EmailBodyPart> { text };
        email.Attachments = new List<EmailBodyPart> { attachment };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.PartId, Is.Null);
            Assert.That(root.HasSize, Is.False);
            Assert.That(root.SubParts, Is.EqualTo(new[] { text, attachment }));
            Assert.That(root.SubParts[0].HasSize, Is.True);
            Assert.That(root.SubParts[0].Language, Is.EqualTo(new[] { "en" }));
            Assert.That(text.HeaderProperties["header:Content-Type"].GetString(), Does.StartWith("text/plain"));
            Assert.That(email.BodyValues["1"], Is.SameAs(value));
            Assert.That(email.BodyValues["1"].Value, Is.EqualTo("hello\nworld"));
            Assert.That(email.BodyValues["1"].IsTruncated, Is.True);
            Assert.That(email.TextBody.Single(), Is.SameAs(text));
            Assert.That(email.Attachments.Single(), Is.SameAs(attachment));
        }
    }

    [Test]
    public void EmailFiltersRepresentRecursiveConditionsAndMeaningfulFalseAndZeroValues()
    {
        EmailFilter omitted = new EmailFilter();

        EmailFilter mailboxCondition = new EmailFilter();
        mailboxCondition.InMailbox = "inbox";
        mailboxCondition.InMailboxOtherThan = new List<string> { "trash", "junk" };
        mailboxCondition.Before = "2026-10-01T00:00:00Z";
        mailboxCondition.After = "2026-09-01T00:00:00Z";
        mailboxCondition.MinSize = 0;
        mailboxCondition.HasMinSize = true;
        mailboxCondition.MaxSize = 100000;
        mailboxCondition.HasMaxSize = true;
        mailboxCondition.HasAttachment = false;
        mailboxCondition.HasHasAttachment = true;

        EmailFilter textCondition = new EmailFilter();
        textCondition.AllInThreadHaveKeyword = JmapMailKeywords.Seen;
        textCondition.SomeInThreadHaveKeyword = JmapMailKeywords.Flagged;
        textCondition.NoneInThreadHaveKeyword = JmapMailKeywords.Junk;
        textCondition.HasKeyword = JmapMailKeywords.Seen;
        textCondition.NotKeyword = JmapMailKeywords.Draft;
        textCondition.Text = "project";
        textCondition.From = "alice";
        textCondition.To = "bob";
        textCondition.Cc = "carol";
        textCondition.Bcc = "dave";
        textCondition.Subject = "update";
        textCondition.Body = "milestone";
        textCondition.Header = new List<string> { "List-Id", "example" };

        EmailFilter and = new EmailFilter();
        and.Operator = JmapFilterOperator.And;
        and.HasOperator = true;
        and.Conditions = new List<EmailFilter> { mailboxCondition, textCondition };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(omitted.HasMinSize, Is.False);
            Assert.That(omitted.HasHasAttachment, Is.False);
            Assert.That(mailboxCondition.HasMinSize, Is.True);
            Assert.That(mailboxCondition.MinSize, Is.Zero);
            Assert.That(mailboxCondition.HasHasAttachment, Is.True);
            Assert.That(mailboxCondition.HasAttachment, Is.False);
            Assert.That(and.Operator, Is.EqualTo(JmapFilterOperator.And));
            Assert.That(and.Conditions[0], Is.SameAs(mailboxCondition));
            Assert.That(and.Conditions[1].Header, Is.EqualTo(new[] { "List-Id", "example" }));
            Assert.That(and.Conditions[1].NoneInThreadHaveKeyword, Is.EqualTo(JmapMailKeywords.Junk));
        }
    }

    [Test]
    public void EmailQueryModelsPreserveKeywordSortingThreadCollapseAndServerWindow()
    {
        EmailComparator keywordSort = new EmailComparator();
        keywordSort.Property = JmapProperties.SomeInThreadHaveKeyword;
        keywordSort.Keyword = JmapMailKeywords.Flagged;
        keywordSort.IsAscending = false;
        keywordSort.HasIsAscending = true;

        EmailQueryRequest request = new EmailQueryRequest();
        request.AccountId = "account-1";
        request.Sort = new List<EmailComparator> { keywordSort };
        request.Position = -20;
        request.HasPosition = true;
        request.Limit = 20;
        request.HasLimit = true;
        request.CalculateTotal = true;
        request.HasCalculateTotal = true;
        request.CollapseThreads = true;
        request.HasCollapseThreads = true;

        EmailQueryResponse response = new EmailQueryResponse();
        response.AccountId = "account-1";
        response.QueryState = "query-2";
        response.CanCalculateChanges = true;
        response.Position = 80;
        response.Ids.Add("email-81");
        response.Total = 101;
        response.HasTotal = true;
        response.Limit = 20;
        response.HasLimit = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Sort.Single(), Is.SameAs(keywordSort));
            Assert.That(request.Sort.Single().Keyword, Is.EqualTo(JmapMailKeywords.Flagged));
            Assert.That(request.Position, Is.EqualTo(-20));
            Assert.That(request.CollapseThreads, Is.True);
            Assert.That(request.HasCollapseThreads, Is.True);
            Assert.That(response.Ids, Is.EqualTo(new[] { "email-81" }));
            Assert.That(response.Total, Is.EqualTo(101));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Limit, Is.EqualTo(20));
            Assert.That(response.HasLimit, Is.True);
        }
    }

    [Test]
    public void EmailQueryChangesPreservesNullableMaximumCollapseAndMoves()
    {
        EmailQueryChangesRequest request = new EmailQueryChangesRequest();
        request.AccountId = "account-1";
        request.SinceQueryState = "query-1";
        request.HasMaxChanges = true;
        request.IsMaxChangesNull = true;
        request.UpToId = "email-50";
        request.CollapseThreads = false;
        request.HasCollapseThreads = true;

        JmapAddedItem moved = new JmapAddedItem();
        moved.Id = "email-moved";
        moved.Index = 1;

        EmailQueryChangesResponse response = new EmailQueryChangesResponse();
        response.AccountId = "account-1";
        response.OldQueryState = "query-1";
        response.NewQueryState = "query-2";
        response.Removed.Add("email-moved");
        response.Added.Add(moved);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.HasMaxChanges, Is.True);
            Assert.That(request.IsMaxChangesNull, Is.True);
            Assert.That(request.HasCollapseThreads, Is.True);
            Assert.That(request.CollapseThreads, Is.False);
            Assert.That(response.Removed, Is.EqualTo(new[] { "email-moved" }));
            Assert.That(response.Added.Single(), Is.SameAs(moved));
            Assert.That(response.Added.Single().Index, Is.EqualTo(1));
        }
    }

    [Test]
    public void EmailGetAndChangesRepresentBodyProjectionAndPagedPartitions()
    {
        EmailGetRequest getRequest = new EmailGetRequest();
        getRequest.AccountId = "account-1";
        getRequest.Ids = new List<string> { "email-1" };
        getRequest.Properties = new List<string> { JmapProperties.Id, JmapProperties.BodyValues };
        getRequest.BodyProperties = new List<string> { JmapProperties.PartId, JmapProperties.Type };
        getRequest.FetchHtmlBodyValues = true;
        getRequest.HasFetchHtmlBodyValues = true;
        getRequest.MaxBodyValueBytes = 256;
        getRequest.HasMaxBodyValueBytes = true;

        Email email = new Email();
        email.Id = "email-1";
        EmailGetResponse getResponse = new EmailGetResponse();
        getResponse.AccountId = "account-1";
        getResponse.State = "state-2";
        getResponse.List.Add(email);
        getResponse.NotFound.Add("email-missing");

        EmailChangesRequest changesRequest = new EmailChangesRequest();
        changesRequest.AccountId = "account-1";
        changesRequest.SinceState = "state-1";
        changesRequest.MaxChanges = 100;
        changesRequest.HasMaxChanges = true;

        EmailChangesResponse changesResponse = new EmailChangesResponse();
        changesResponse.AccountId = "account-1";
        changesResponse.OldState = "state-1";
        changesResponse.NewState = "state-2";
        changesResponse.HasMoreChanges = true;
        changesResponse.Created.Add("email-created");
        changesResponse.Updated.Add("email-updated");
        changesResponse.Destroyed.Add("email-destroyed");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(getRequest.FetchHtmlBodyValues, Is.True);
            Assert.That(getRequest.HasFetchHtmlBodyValues, Is.True);
            Assert.That(getRequest.MaxBodyValueBytes, Is.EqualTo(256));
            Assert.That(getResponse.List.Single(), Is.SameAs(email));
            Assert.That(getResponse.NotFound, Is.EqualTo(new[] { "email-missing" }));
            Assert.That(changesRequest.MaxChanges, Is.EqualTo(100));
            Assert.That(changesResponse.HasMoreChanges, Is.True);
            Assert.That(changesResponse.Created, Is.EqualTo(new[] { "email-created" }));
            Assert.That(changesResponse.Updated, Is.EqualTo(new[] { "email-updated" }));
            Assert.That(changesResponse.Destroyed, Is.EqualTo(new[] { "email-destroyed" }));
        }
    }

    [Test]
    public void EmailSetAndCopyPreserveAllowedMutationsAndPartialOutcomes()
    {
        Email draft = new Email();
        draft.MailboxIds = new Dictionary<string, bool> { { "drafts", true } };
        draft.Keywords = new Dictionary<string, bool> { { JmapMailKeywords.Draft, true } };
        draft.Subject = "Draft subject";

        JmapPatchObject patch = new JmapPatchObject();
        patch.Values.Add("keywords/$seen", Json("true"));

        EmailSetRequest setRequest = new EmailSetRequest();
        setRequest.AccountId = "account-1";
        setRequest.IfInState = "state-1";
        setRequest.Create = new Dictionary<string, Email> { { "draft-1", draft } };
        setRequest.Update = new Dictionary<string, JmapPatchObject> { { "email-1", patch } };
        setRequest.Destroy = new List<string> { "email-old" };

        Email created = new Email();
        created.Id = "email-new";
        created.BlobId = "blob-new";
        created.ThreadId = "thread-new";
        created.Size = 100;
        JmapSetError setError = new JmapSetError();
        setError.Type = JmapErrorCodes.TooManyKeywords;

        EmailSetResponse setResponse = new EmailSetResponse();
        setResponse.Created = new Dictionary<string, Email> { { "draft-1", created } };
        setResponse.NotUpdated = new Dictionary<string, JmapSetError> { { "email-1", setError } };

        Email copyData = new Email();
        copyData.Id = "email-source";
        copyData.MailboxIds = new Dictionary<string, bool> { { "archive", true } };
        copyData.ReceivedAt = "2026-09-01T00:00:00Z";
        EmailCopyRequest copyRequest = new EmailCopyRequest();
        copyRequest.FromAccountId = "source";
        copyRequest.AccountId = "target";
        copyRequest.Create.Add("copy-1", copyData);
        copyRequest.OnSuccessDestroyOriginal = true;
        copyRequest.HasOnSuccessDestroyOriginal = true;

        EmailCopyResponse copyResponse = new EmailCopyResponse();
        copyResponse.Created = new Dictionary<string, Email> { { "copy-1", created } };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(setRequest.Create["draft-1"], Is.SameAs(draft));
            Assert.That(setRequest.Update["email-1"], Is.SameAs(patch));
            Assert.That(setResponse.Created["draft-1"], Is.SameAs(created));
            Assert.That(setResponse.NotUpdated["email-1"], Is.SameAs(setError));
            Assert.That(copyRequest.Create["copy-1"], Is.SameAs(copyData));
            Assert.That(copyRequest.OnSuccessDestroyOriginal, Is.True);
            Assert.That(copyRequest.HasOnSuccessDestroyOriginal, Is.True);
            Assert.That(copyResponse.Created["copy-1"], Is.SameAs(created));
        }
    }

    [Test]
    public void EmailImportPreservesUploadedMetadataAndIndividualResults()
    {
        EmailImport import = new EmailImport();
        import.BlobId = "uploaded-blob";
        import.MailboxIds.Add("inbox", true);
        import.Keywords.Add(JmapMailKeywords.Seen, true);
        import.ReceivedAt = "2026-09-10T12:00:00Z";

        EmailImportRequest request = new EmailImportRequest();
        request.AccountId = "account-1";
        request.IfInState = "state-1";
        request.Emails.Add("import-1", import);

        Email created = new Email();
        created.Id = "email-imported";
        created.BlobId = "normalized-blob";
        JmapSetError error = new JmapSetError();
        error.Type = JmapErrorCodes.InvalidEmail;

        EmailImportResponse response = new EmailImportResponse();
        response.AccountId = "account-1";
        response.OldState = "state-1";
        response.NewState = "state-2";
        response.Created = new Dictionary<string, Email> { { "import-1", created } };
        response.NotCreated = new Dictionary<string, JmapSetError> { { "import-2", error } };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Emails["import-1"], Is.SameAs(import));
            Assert.That(import.MailboxIds["inbox"], Is.True);
            Assert.That(import.Keywords[JmapMailKeywords.Seen], Is.True);
            Assert.That(response.Created["import-1"].BlobId, Is.EqualTo("normalized-blob"));
            Assert.That(response.NotCreated["import-2"], Is.SameAs(error));
        }
    }

    [Test]
    public void EmailParsePreservesFetchOptionsAndResultPartitionsWithoutStoreMetadata()
    {
        EmailParseRequest request = new EmailParseRequest();
        request.AccountId = "account-1";
        request.BlobIds.Add("blob-good");
        request.BlobIds.Add("blob-bad");
        request.BlobIds.Add("blob-missing");
        request.Properties = new List<string> { JmapProperties.Subject, JmapProperties.BodyValues };
        request.FetchAllBodyValues = true;
        request.HasFetchAllBodyValues = true;
        request.MaxBodyValueBytes = 1024;
        request.HasMaxBodyValueBytes = true;

        Email parsed = new Email();
        parsed.Subject = "Attached message";
        parsed.Id = null;
        parsed.MailboxIds = null;
        parsed.Keywords = null;
        parsed.ReceivedAt = null;

        EmailParseResponse response = new EmailParseResponse();
        response.AccountId = "account-1";
        response.Parsed = new Dictionary<string, Email> { { "blob-good", parsed } };
        response.NotParsable = new List<string> { "blob-bad" };
        response.NotFound = new List<string> { "blob-missing" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.BlobIds, Is.EqualTo(new[] { "blob-good", "blob-bad", "blob-missing" }));
            Assert.That(request.FetchAllBodyValues, Is.True);
            Assert.That(request.MaxBodyValueBytes, Is.EqualTo(1024));
            Assert.That(response.Parsed["blob-good"], Is.SameAs(parsed));
            Assert.That(response.Parsed["blob-good"].Id, Is.Null);
            Assert.That(response.Parsed["blob-good"].MailboxIds, Is.Null);
            Assert.That(response.NotParsable, Is.EqualTo(new[] { "blob-bad" }));
            Assert.That(response.NotFound, Is.EqualTo(new[] { "blob-missing" }));
        }
    }

    [Test]
    public void RequiredEmailCollectionsAreIndependent()
    {
        EmailAddressGroup firstGroup = new EmailAddressGroup();
        EmailAddressGroup secondGroup = new EmailAddressGroup();
        firstGroup.Addresses.Add(new EmailAddress());

        EmailCopyRequest firstCopy = new EmailCopyRequest();
        EmailCopyRequest secondCopy = new EmailCopyRequest();
        firstCopy.Create.Add("copy", new Email());

        EmailImport firstImport = new EmailImport();
        EmailImport secondImport = new EmailImport();
        firstImport.Keywords.Add(JmapMailKeywords.Seen, true);

        EmailParseRequest firstParse = new EmailParseRequest();
        EmailParseRequest secondParse = new EmailParseRequest();
        firstParse.BlobIds.Add("blob-1");

        EmailQueryChangesResponse firstQueryChanges = new EmailQueryChangesResponse();
        EmailQueryChangesResponse secondQueryChanges = new EmailQueryChangesResponse();
        firstQueryChanges.Removed.Add("email-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondGroup.Addresses, Is.Empty);
            Assert.That(secondCopy.Create, Is.Empty);
            Assert.That(secondImport.Keywords, Is.Empty);
            Assert.That(secondParse.BlobIds, Is.Empty);
            Assert.That(secondQueryChanges.Removed, Is.Empty);
            Assert.That(firstGroup.Addresses, Is.Not.SameAs(secondGroup.Addresses));
            Assert.That(firstCopy.Create, Is.Not.SameAs(secondCopy.Create));
            Assert.That(firstImport.Keywords, Is.Not.SameAs(secondImport.Keywords));
            Assert.That(firstParse.BlobIds, Is.Not.SameAs(secondParse.BlobIds));
            Assert.That(firstQueryChanges.Removed, Is.Not.SameAs(secondQueryChanges.Removed));
        }
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
