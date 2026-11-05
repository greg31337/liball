using System.Text.Json;

namespace jmap.Tests;

public class ModelValidatorTests
{
    [Test]
    public void MailboxCreateSeparatesWritableValuesFromServerSetFields()
    {
        Mailbox valid = new Mailbox
        {
            Name = "Inbox",
            ParentId = "parent_1",
            Role = JmapMailboxRoles.Inbox,
            SortOrder = 10,
            IsSubscribed = true
        };
        Mailbox invalid = new Mailbox
        {
            Id = "server-id",
            Name = "",
            ParentId = "bad/id",
            Role = "Inbox",
            SortOrder = 2147483648L,
            TotalEmails = 1,
            UnreadEmails = 2,
            TotalThreads = 3,
            UnreadThreads = 4,
            MyRights = new MailboxRights()
        };
        MailboxValidator validator = new MailboxValidator();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateCreate(valid, 255), Is.Empty);
            Assert.That(
                validator.ValidateCreate(invalid, 255),
                Is.EqualTo(new[] { "id", "name", "parentId", "role", "sortOrder", "totalEmails", "unreadEmails", "totalThreads", "unreadThreads", "myRights" }));
        }
    }

    [Test]
    public void MailboxPatchReportsEveryNonWritablePath()
    {
        JmapPatchObject patch = Patch(
            ("name", "\"Archive\""),
            ("parentId", "null"),
            ("totalEmails", "12"),
            ("myRights/mayDelete", "true"),
            ("id", "\"replacement\""));
        MailboxValidator validator = new MailboxValidator();

        Assert.That(validator.ValidateWritablePatch(patch), Is.EqualTo(new[] { "totalEmails", "myRights/mayDelete", "id" }));
    }

    [Test]
    public void EmailCreateValidatesIdsKeywordsServerFieldsAndNestedBodyPaths()
    {
        Email valid = new Email
        {
            MailboxIds = new Dictionary<string, bool> { ["mailbox-1"] = true },
            Keywords = new Dictionary<string, bool> { [JmapMailKeywords.Draft] = true },
            ReceivedAt = "2024-03-01T12:30:00Z"
        };
        EmailBodyPart bodyPart = new EmailBodyPart
        {
            PartId = "part1",
            BlobId = "blob1",
            Type = "not a media type",
            Charset = "utf-8",
            Size = -1,
            HasSize = true,
            Headers = new List<EmailHeader>()
        };
        Email invalid = new Email
        {
            Id = "server-id",
            BlobId = "server-blob",
            ThreadId = "server-thread",
            Size = 20,
            MailboxIds = new Dictionary<string, bool> { ["bad/id"] = false },
            Keywords = new Dictionary<string, bool> { ["$Seen"] = true },
            ReceivedAt = "2024-03-01T12:30:00+00:00",
            Headers = new List<EmailHeader>(),
            BodyStructure = bodyPart,
            TextBody = new List<EmailBodyPart> { new EmailBodyPart { Type = "text/html" } },
            HasAttachment = true,
            Preview = "server preview"
        };
        invalid.HeaderProperties["header:Content-Type"] = Json("\"text/plain\"");
        EmailValidator validator = new EmailValidator();

        List<string> paths = validator.ValidateCreate(invalid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateCreate(valid), Is.Empty);
            Assert.That(paths, Does.Contain("id"));
            Assert.That(paths, Does.Contain("blobId"));
            Assert.That(paths, Does.Contain("threadId"));
            Assert.That(paths, Does.Contain("size"));
            Assert.That(paths, Does.Contain("mailboxIds/bad/id"));
            Assert.That(paths, Does.Contain("keywords/$Seen"));
            Assert.That(paths, Does.Contain("receivedAt"));
            Assert.That(paths, Does.Contain("header:Content-Type"));
            Assert.That(paths, Does.Contain("bodyStructure/headers"));
            Assert.That(paths, Does.Contain("bodyStructure/partId"));
            Assert.That(paths, Does.Contain("bodyStructure/blobId"));
            Assert.That(paths, Does.Contain("bodyStructure/charset"));
            Assert.That(paths, Does.Contain("bodyStructure/size"));
            Assert.That(paths, Does.Contain("bodyStructure/type"));
            Assert.That(paths, Does.Contain("textBody"));
            Assert.That(paths, Does.Contain("hasAttachment"));
            Assert.That(paths, Does.Contain("preview"));
        }
    }

    [Test]
    public void EmailPatchAndCopyAllowOnlyTheirMutablePropertySets()
    {
        EmailValidator validator = new EmailValidator();
        JmapPatchObject patch = Patch(
            ("mailboxIds", "{\"m1\":true}"),
            ("keywords/$seen", "true"),
            ("receivedAt", "\"2024-01-01T00:00:00Z\""),
            ("subject", "\"changed\""),
            ("id", "\"changed\""));
        Email copy = new Email
        {
            Id = "source-email",
            MailboxIds = new Dictionary<string, bool> { ["m1"] = true },
            Keywords = new Dictionary<string, bool>(),
            ReceivedAt = "2024-01-01T00:00:00Z",
            Subject = "not copy-writable",
            BodyValues = new Dictionary<string, EmailBodyValue>()
        };
        List<string> copyPaths = validator.ValidateCopy(copy);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateWritablePatch(patch), Is.EqualTo(new[] { "receivedAt", "subject", "id" }));
            Assert.That(copyPaths, Is.EqualTo(new[] { "subject", "bodyValues" }));
        }
    }

    [Test]
    public void EmailImportRequiresBlobMailboxKeywordAndUtcValues()
    {
        EmailValidator validator = new EmailValidator();
        EmailImport valid = new EmailImport
        {
            BlobId = "blob_1",
            ReceivedAt = "2024-01-01T00:00:00Z"
        };
        valid.MailboxIds["mailbox_1"] = true;
        valid.Keywords[JmapMailKeywords.Seen] = true;
        EmailImport invalid = new EmailImport
        {
            BlobId = "bad/blob",
            ReceivedAt = "not-a-date"
        };
        invalid.MailboxIds["mailbox_1"] = false;
        invalid.Keywords["$Seen"] = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateImport(valid), Is.Empty);
            Assert.That(
                validator.ValidateImport(invalid),
                Is.EqualTo(new[] { "blobId", "mailboxIds/mailbox_1", "keywords/$Seen", "receivedAt" }));
        }
    }

    [Test]
    public void IdentityValidationTreatsEmailAsCreateOnlyAndServerFieldsAsReadOnly()
    {
        SubmissionValidator validator = new SubmissionValidator();
        Identity valid = new Identity { Email = "sender@example.test" };
        Identity invalid = new Identity { Id = "identity-1", Email = null, MayDelete = true };
        JmapPatchObject patch = Patch(
            ("name", "\"Sender\""),
            ("textSignature", "\"Thanks\""),
            ("email", "\"other@example.test\""),
            ("mayDelete", "false"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateIdentityCreate(valid), Is.Empty);
            Assert.That(validator.ValidateIdentityCreate(invalid), Is.EqualTo(new[] { "id", "email", "mayDelete" }));
            Assert.That(validator.ValidateIdentityWritablePatch(patch), Is.EqualTo(new[] { "email", "mayDelete" }));
        }
    }

    [Test]
    public void SubmissionValidationRejectsServerSetDataAndOnlyAllowsCancellationUpdates()
    {
        SubmissionValidator validator = new SubmissionValidator();
        EmailSubmission valid = new EmailSubmission { IdentityId = "identity-1", EmailId = "#draft-1" };
        EmailSubmission invalid = new EmailSubmission
        {
            Id = "submission-1",
            IdentityId = "bad/id",
            EmailId = null,
            ThreadId = "thread-1",
            SendAt = "2024-01-01T00:00:00Z",
            HasUndoStatus = true,
            DeliveryStatus = new Dictionary<string, EmailDeliveryStatus>(),
            DsnBlobIds = new List<string>(),
            MdnBlobIds = new List<string>()
        };
        JmapPatchObject validPatch = Patch(("undoStatus", "\"canceled\""));
        JmapPatchObject invalidPatch = Patch(("undoStatus", "\"final\""), ("emailId", "\"other\""));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateEmailSubmissionCreate(valid), Is.Empty);
            Assert.That(
                validator.ValidateEmailSubmissionCreate(invalid),
                Is.EqualTo(new[] { "id", "identityId", "emailId", "threadId", "sendAt", "undoStatus", "deliveryStatus", "dsnBlobIds", "mdnBlobIds" }));
            Assert.That(validator.ValidateEmailSubmissionWritablePatch(validPatch), Is.Empty);
            Assert.That(validator.ValidateEmailSubmissionWritablePatch(invalidPatch), Is.EqualTo(new[] { "undoStatus", "emailId" }));
        }
    }

    [Test]
    public void VacationResponseValidationReportsImmutableAndDateRangePaths()
    {
        SubmissionValidator validator = new SubmissionValidator();
        VacationResponse valid = new VacationResponse
        {
            FromDate = "2024-01-01T00:00:00Z",
            ToDate = "2024-02-01T00:00:00Z",
            TextBody = "Away"
        };
        VacationResponse invalid = new VacationResponse
        {
            Id = "singleton",
            FromDate = "2024-03-01T00:00:00Z",
            ToDate = "2024-02-01T00:00:00Z"
        };
        JmapPatchObject patch = Patch(("isEnabled", "true"), ("id", "\"other\""));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateVacationResponse(valid), Is.Empty);
            Assert.That(validator.ValidateVacationResponse(invalid), Is.EqualTo(new[] { "id", "fromDate", "toDate" }));
            Assert.That(validator.ValidateVacationResponseWritablePatch(patch), Is.EqualTo(new[] { "id" }));
        }
    }

    [Test]
    public void PushValidationEnforcesCreateOnlySecretsAndMutableUpdateFields()
    {
        PushValidator validator = new PushValidator();
        PushSubscription valid = new PushSubscription
        {
            DeviceClientId = "device-app-hash",
            Url = "https://push.example.test/events",
            Keys = new PushSubscriptionKeys { P256dh = P256PublicKey(), Auth = Base64Url(new byte[16]) },
            Expires = "2024-01-01T00:00:00Z",
            Types = new List<string> { JmapDataTypes.Email }
        };
        PushSubscription invalid = new PushSubscription
        {
            Id = "push-1",
            DeviceClientId = "",
            Url = "http://push.example.test/events",
            Keys = new PushSubscriptionKeys { P256dh = "bad=padding", Auth = null },
            VerificationCode = "server-code",
            Expires = "not-a-date",
            Types = new List<string> { "" }
        };
        JmapPatchObject patch = Patch(("expires", "null"), ("types", "null"), ("url", "\"https://other.test\""), ("keys/auth", "\"x\""));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateCreate(valid), Is.Empty);
            Assert.That(
                validator.ValidateCreate(invalid),
                Is.EqualTo(new[] { "id", "deviceClientId", "url", "keys/p256dh", "keys/auth", "verificationCode", "expires", "types/0" }));
            Assert.That(validator.ValidateWritablePatch(patch), Is.EqualTo(new[] { "url", "keys/auth" }));
        }
    }

    [Test]
    public void SessionValidationReturnsPathsThroughCapabilityAndAccountMaps()
    {
        SessionValidator validator = new SessionValidator();
        JmapSession valid = ValidSession();
        JmapSession invalid = ValidSession();
        invalid.Capabilities[JmapCapabilities.Core].Core!.MaxSizeRequest = -1;
        invalid.Accounts["account-1"].Name = null;
        invalid.PrimaryAccounts[JmapCapabilities.Mail] = "missing-account";
        invalid.ApiUrl = null;

        List<string> paths = validator.ValidateSession(invalid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateSession(valid), Is.Empty);
            Assert.That(paths, Does.Contain("capabilities/urn:ietf:params:jmap:core/maxSizeRequest"));
            Assert.That(paths, Does.Contain("accounts/account-1/name"));
            Assert.That(paths, Does.Contain("primaryAccounts/urn:ietf:params:jmap:mail"));
            Assert.That(paths, Does.Contain("apiUrl"));
        }
    }

    private static JmapSession ValidSession()
    {
        JmapSession session = new JmapSession
        {
            Username = "user@example.test",
            ApiUrl = "https://jmap.example.test/api",
            DownloadUrl = "https://jmap.example.test/download/{accountId}/{blobId}",
            UploadUrl = "https://jmap.example.test/upload/{accountId}",
            EventSourceUrl = "https://jmap.example.test/events/{types}",
            State = "state-1"
        };
        session.Capabilities[JmapCapabilities.Core] = new JmapCapability { Core = new JmapCoreCapabilities() };
        session.Capabilities[JmapCapabilities.Mail] = new JmapCapability { Mail = new JmapMailCapabilities() };
        session.Accounts["account-1"] = new JmapAccount { Name = "Personal" };
        session.PrimaryAccounts[JmapCapabilities.Mail] = "account-1";
        return session;
    }

    private static JmapPatchObject Patch(params (string Path, string Json)[] values)
    {
        JmapPatchObject patch = new JmapPatchObject();
        foreach ((string path, string json) in values)
        {
            patch.Values[path] = Json(json);
        }

        return patch;
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string P256PublicKey()
    {
        return Base64Url(Convert.FromHexString("046B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C2964FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5"));
    }

    private static string Base64Url(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
