using System.Text.Json;

namespace jmap.Tests;

public class EmailSetValidatorTests
{
    [Test]
    public void CreateAcceptsStandardIanaAndUserKeywords()
    {
        Email email = Draft("m1");
        email.Keywords![JmapMailKeywords.Draft] = true;
        email.Keywords["$mdnsent"] = true;
        email.Keywords["project/blue"] = true;

        EmailSetValidationResult result = new EmailSetValidator().ValidateCreate(email, Context(Mailbox("m1")));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.InvalidProperties, Is.Empty);
            Assert.That(result.IsForbidden, Is.False);
            Assert.That(result.Keywords.Keys, Is.EquivalentTo(new[]
            {
                JmapMailKeywords.Draft,
                "$mdnsent",
                "project/blue"
            }));
        }
    }

    [Test]
    public void CreateRejectsReservedUppercaseAndConflictingJunkKeywords()
    {
        Email email = Draft("m1");
        email.Keywords!["$recent"] = true;
        email.Keywords["$Seen"] = true;
        email.Keywords[JmapMailKeywords.Junk] = true;
        email.Keywords[JmapMailKeywords.NotJunk] = true;

        EmailSetValidationResult result = new EmailSetValidator().ValidateCreate(email, Context(Mailbox("m1")));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.InvalidProperties, Does.Contain("keywords/$recent"));
            Assert.That(result.InvalidProperties, Does.Contain("keywords/$Seen"));
            Assert.That(result.InvalidProperties, Does.Contain("keywords/$junk"));
            Assert.That(result.InvalidProperties, Does.Contain("keywords/$notjunk"));
        }
    }

    [Test]
    public void CreateChecksMailboxExistenceRightsAndAccountLimits()
    {
        Email email = Draft("allowed");
        email.MailboxIds!["denied"] = true;
        email.MailboxIds["missing"] = true;
        email.Keywords![JmapMailKeywords.Draft] = true;
        email.Keywords["project"] = true;
        EmailSetValidationContext context = Context(Mailbox("allowed"), Mailbox("denied", false, true, true, true));
        context.Capabilities!.HasMaxMailboxesPerEmail = true;
        context.Capabilities.MaxMailboxesPerEmail = 1;
        context.HasMaximumKeywords = true;
        context.MaximumKeywords = 1;

        EmailSetValidationResult result = new EmailSetValidator().ValidateCreate(email, context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.InvalidProperties, Does.Contain("mailboxIds/missing"));
            Assert.That(result.IsForbidden, Is.True);
            Assert.That(result.HasTooManyMailboxes, Is.True);
            Assert.That(result.HasTooManyKeywords, Is.True);
        }
    }

    [Test]
    public void CreateRequiresKeywordRightsAcrossEveryMailbox()
    {
        Email seenEmail = Draft("m1");
        seenEmail.MailboxIds!["m2"] = true;
        seenEmail.Keywords![JmapMailKeywords.Seen] = true;
        Email keywordEmail = Draft("m1");
        keywordEmail.MailboxIds!["m2"] = true;
        keywordEmail.Keywords!["project"] = true;
        EmailSetValidationContext context = Context(
            Mailbox("m1"),
            Mailbox("m2", true, true, false, true));
        EmailSetValidator validator = new EmailSetValidator();

        EmailSetValidationResult seen = validator.ValidateCreate(seenEmail, context);
        MailboxRights secondRights = context.Mailboxes["m2"].MyRights!;
        secondRights.MaySetSeen = true;
        secondRights.MaySetKeywords = false;
        EmailSetValidationResult keyword = validator.ValidateCreate(keywordEmail, context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seen.IsForbidden, Is.True);
            Assert.That(keyword.IsForbidden, Is.True);
        }
    }

    [Test]
    public void UpdateSeparatesMalformedPatchesFromImmutableProperties()
    {
        Email original = Draft("m1");
        EmailSetValidationContext context = Context(Mailbox("m1"));
        EmailSetValidator validator = new EmailSetValidator();

        EmailSetValidationResult immutable = validator.ValidateUpdate(original,
            Patch(("subject", "\"changed\"")), context);
        EmailSetValidationResult conflicting = validator.ValidateUpdate(original,
            Patch(("keywords", "{}"), ("keywords/$seen", "true")), context);
        EmailSetValidationResult nestedChild = validator.ValidateUpdate(original,
            Patch(("keywords/project/blue", "true")), context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(immutable.IsInvalidPatch, Is.False);
            Assert.That(immutable.InvalidProperties, Does.Contain("subject"));
            Assert.That(conflicting.IsInvalidPatch, Is.True);
            Assert.That(nestedChild.IsInvalidPatch, Is.False);
            Assert.That(nestedChild.InvalidProperties, Does.Contain("keywords/project/blue"));
        }
    }

    [Test]
    public void UpdateAppliesMailboxChangesEscapedKeywordsAndJunkExclusion()
    {
        Email original = Draft("old");
        original.Keywords![JmapMailKeywords.NotJunk] = true;
        EmailSetValidationContext context = Context(Mailbox("old"), Mailbox("new"));
        JmapPatchObject patch = Patch(
            ("mailboxIds/old", "null"),
            ("mailboxIds/new", "true"),
            ("keywords/$junk", "true"),
            ("keywords/project~1blue", "true"));

        EmailSetValidationResult result = new EmailSetValidator().ValidateUpdate(original, patch, context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.InvalidProperties, Is.Empty);
            Assert.That(result.IsForbidden, Is.False);
            Assert.That(result.MailboxIds.Keys, Is.EqualTo(new[] { "new" }));
            Assert.That(result.Keywords.ContainsKey(JmapMailKeywords.Junk), Is.True);
            Assert.That(result.Keywords.ContainsKey(JmapMailKeywords.NotJunk), Is.False);
            Assert.That(result.Keywords.ContainsKey("project/blue"), Is.True);
        }
    }

    [Test]
    public void UpdateRequiresTheRightForEachMailboxAndChangeKind()
    {
        Email original = Draft("m1");
        original.MailboxIds!["m2"] = true;
        EmailSetValidationContext context = Context(
            Mailbox("m1"),
            Mailbox("m2", true, true, false, true),
            Mailbox("m3", false, true, true, true));
        EmailSetValidator validator = new EmailSetValidator();

        EmailSetValidationResult seen = validator.ValidateUpdate(original,
            Patch(("keywords/$seen", "true")), context);
        MailboxRights secondRights = context.Mailboxes["m2"].MyRights!;
        secondRights.MaySetSeen = true;
        secondRights.MaySetKeywords = false;
        EmailSetValidationResult keyword = validator.ValidateUpdate(original,
            Patch(("keywords/project", "true")), context);
        secondRights.MaySetKeywords = true;
        context.Mailboxes["m1"].MyRights!.MayRemoveItems = false;
        EmailSetValidationResult remove = validator.ValidateUpdate(original,
            Patch(("mailboxIds/m1", "null")), context);
        EmailSetValidationResult add = validator.ValidateUpdate(original,
            Patch(("mailboxIds/m3", "true")), context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seen.IsForbidden, Is.True);
            Assert.That(keyword.IsForbidden, Is.True);
            Assert.That(remove.IsForbidden, Is.True);
            Assert.That(add.IsForbidden, Is.True);
        }
    }

    [Test]
    public void DestroyRequiresRemoveRightsInEveryMailbox()
    {
        Email original = Draft("m1");
        original.MailboxIds!["m2"] = true;
        EmailSetValidationContext context = Context(Mailbox("m1"), Mailbox("m2", true, false, true, true));

        EmailSetValidationResult denied = new EmailSetValidator().ValidateDestroy(original, context);
        context.Mailboxes["m2"].MyRights!.MayRemoveItems = true;
        EmailSetValidationResult allowed = new EmailSetValidator().ValidateDestroy(original, context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(denied.IsForbidden, Is.True);
            Assert.That(allowed.IsForbidden, Is.False);
        }
    }

    [Test]
    public void CreateEnforcesCombinedUnencodedAttachmentSizeWithoutCountingMessageBody()
    {
        Email email = Draft("m1");
        email.TextBody = new List<EmailBodyPart>
        {
            new EmailBodyPart { PartId = "body", Type = "text/plain" }
        };
        email.Attachments = new List<EmailBodyPart>
        {
            new EmailBodyPart { BlobId = "blob", Type = "application/pdf" },
            new EmailBodyPart { PartId = "note", Type = "text/plain", Disposition = "attachment" }
        };
        email.BodyValues = new Dictionary<string, EmailBodyValue>
        {
            ["body"] = new EmailBodyValue { Value = new string('x', 100) },
            ["note"] = new EmailBodyValue { Value = "é" }
        };
        EmailSetValidationContext context = Context(Mailbox("m1"));
        context.Capabilities!.MaxSizeAttachmentsPerEmail = 8;
        context.BlobSizes["blob"] = 7;

        EmailSetValidationResult result = new EmailSetValidator().ValidateCreate(email, context);
        context.Capabilities.MaxSizeAttachmentsPerEmail = 9;
        EmailSetValidationResult atLimit = new EmailSetValidator().ValidateCreate(email, context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.InvalidProperties, Is.Empty);
            Assert.That(result.AttachmentSize, Is.EqualTo(9));
            Assert.That(result.IsAttachmentTooLarge, Is.True);
            Assert.That(atLimit.IsAttachmentTooLarge, Is.False);
        }
    }

    [Test]
    public void UpdateEnforcesMailboxAndKeywordLimitsAfterApplyingThePatch()
    {
        Email original = Draft("m1");
        original.Keywords![JmapMailKeywords.Seen] = true;
        EmailSetValidationContext context = Context(Mailbox("m1"), Mailbox("m2"));
        context.Capabilities!.HasMaxMailboxesPerEmail = true;
        context.Capabilities.MaxMailboxesPerEmail = 1;
        context.HasMaximumKeywords = true;
        context.MaximumKeywords = 1;
        EmailSetValidator validator = new EmailSetValidator();

        EmailSetValidationResult overLimits = validator.ValidateUpdate(
            original,
            Patch(("mailboxIds/m2", "true"), ("keywords/$flagged", "true")),
            context);
        EmailSetValidationResult noMailbox = validator.ValidateUpdate(
            original,
            Patch(("mailboxIds/m1", "null")),
            context);
        EmailSetValidationResult clearKeywords = validator.ValidateUpdate(
            original,
            Patch(("keywords", "null")),
            context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(overLimits.HasTooManyMailboxes, Is.True);
            Assert.That(overLimits.HasTooManyKeywords, Is.True);
            Assert.That(noMailbox.InvalidProperties, Does.Contain("mailboxIds"));
            Assert.That(clearKeywords.InvalidProperties, Is.Empty);
            Assert.That(clearKeywords.Keywords, Is.Empty);
        }
    }

    private static Email Draft(string mailboxId)
    {
        return new Email
        {
            MailboxIds = new Dictionary<string, bool> { [mailboxId] = true },
            Keywords = new Dictionary<string, bool>()
        };
    }

    private static EmailSetValidationContext Context(params Mailbox[] mailboxes)
    {
        EmailSetValidationContext context = new EmailSetValidationContext
        {
            Capabilities = new JmapMailCapabilities { MaxSizeAttachmentsPerEmail = 1_000_000 }
        };
        foreach (Mailbox mailbox in mailboxes)
        {
            context.Mailboxes[mailbox.Id!] = mailbox;
        }

        return context;
    }

    private static Mailbox Mailbox(string id, bool add = true, bool remove = true, bool seen = true,
        bool keywords = true)
    {
        return new Mailbox
        {
            Id = id,
            MyRights = new MailboxRights
            {
                MayAddItems = add,
                MayRemoveItems = remove,
                MaySetSeen = seen,
                MaySetKeywords = keywords
            }
        };
    }

    private static JmapPatchObject Patch(params (string Path, string Json)[] values)
    {
        JmapPatchObject patch = new JmapPatchObject();
        foreach ((string path, string json) in values)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            patch.Values[path] = document.RootElement.Clone();
        }

        return patch;
    }
}
