using System.Text.Json;

namespace jmap.Tests;

public class MailboxValidatorHierarchyTests
{
    [Test]
    public void NamesAndPatchValuesEnforceNetUnicodeTypesAndSortRange()
    {
        MailboxValidator validator = new MailboxValidator();
        Mailbox normalized = new Mailbox { Name = "Café", SortOrder = 2147483647L };
        Mailbox decomposed = new Mailbox { Name = "Cafe\u0301" };
        Mailbox control = new Mailbox { Name = "Bad\nName" };
        JmapPatchObject validPatch = Patch(
            ("name", "\"Archive\""),
            ("parentId", "null"),
            ("role", "\"archive\""),
            ("sortOrder", "2147483647"),
            ("isSubscribed", "true"));
        JmapPatchObject negativeZero = Patch(("sortOrder", "-0"));
        JmapPatchObject invalidPatch = Patch(
            ("name", "null"),
            ("parentId", "17"),
            ("role", "\"Archive\""),
            ("sortOrder", "2147483648"),
            ("isSubscribed", "\"true\""),
            ("totalEmails", "0"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateCreate(normalized, 5), Is.Empty);
            Assert.That(validator.ValidateCreate(normalized, 4), Does.Contain("name"));
            Assert.That(validator.ValidateCreate(decomposed, 100), Does.Contain("name"));
            Assert.That(validator.ValidateCreate(control, 100), Does.Contain("name"));
            Assert.That(validator.ValidatePatchValues(validPatch, 100), Is.Empty);
            Assert.That(validator.ValidatePatchValues(negativeZero, 100), Is.Empty);
            Assert.That(
                validator.ValidatePatchValues(invalidPatch, 100),
                Is.EqualTo(new[] { "totalEmails", "name", "parentId", "role", "sortOrder", "isSubscribed" }));
        }
    }

    [Test]
    public void StoredMailboxRequiresValidCountsAndRights()
    {
        MailboxValidator validator = new MailboxValidator();
        Mailbox valid = Stored("mailbox-1", "Inbox", null, JmapMailboxRoles.Inbox, true, true);
        valid.TotalEmails = 5;
        valid.UnreadEmails = 4;
        valid.TotalThreads = 3;
        valid.UnreadThreads = 2;
        Mailbox invalid = Stored("mailbox-2", "Archive", null, null, true, true);
        invalid.TotalEmails = -1;
        invalid.UnreadEmails = 2;
        invalid.TotalThreads = 1;
        invalid.UnreadThreads = 2;
        invalid.MyRights = null;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateStoredMailbox(valid, 100), Is.Empty);
            Assert.That(
                validator.ValidateStoredMailbox(invalid, 100),
                Is.EqualTo(new[] { "totalEmails", "unreadEmails", "unreadThreads", "myRights" }));
        }
    }

    [Test]
    public void WholeHierarchyDetectsDuplicateNamesRolesMissingParentsInboxNestingCyclesAndDepth()
    {
        MailboxValidator validator = new MailboxValidator();
        JmapMailCapabilities capabilities = Capabilities(true, 2);
        Dictionary<string, Mailbox> mailboxes = new Dictionary<string, Mailbox>();
        mailboxes["root-1"] = Stored("root-1", "Root", null, null, true, true);
        mailboxes["root-2"] = Stored("root-2", "Root", null, null, true, true);
        mailboxes["child-1"] = Stored("child-1", "Child", "root-1", JmapMailboxRoles.Inbox, true, true);
        mailboxes["child-2"] = Stored("child-2", "Child", "root-1", JmapMailboxRoles.Inbox, true, true);
        mailboxes["deep-1"] = Stored("deep-1", "Deep", "child-1", null, true, true);
        mailboxes["missing-1"] = Stored("missing-1", "Missing", "absent-1", null, true, true);
        mailboxes["cycle-1"] = Stored("cycle-1", "Cycle 1", "cycle-2", null, true, true);
        mailboxes["cycle-2"] = Stored("cycle-2", "Cycle 2", "cycle-1", null, true, true);

        List<string> invalid = validator.ValidateHierarchy(mailboxes, capabilities);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalid, Does.Contain("root-1/name"));
            Assert.That(invalid, Does.Contain("root-2/name"));
            Assert.That(invalid, Does.Contain("child-1/name"));
            Assert.That(invalid, Does.Contain("child-2/name"));
            Assert.That(invalid, Does.Contain("child-1/role"));
            Assert.That(invalid, Does.Contain("child-2/role"));
            Assert.That(invalid, Does.Contain("child-1/parentId"));
            Assert.That(invalid, Does.Contain("missing-1/parentId"));
            Assert.That(invalid, Does.Contain("deep-1/parentId"));
            Assert.That(invalid, Does.Contain("cycle-1/parentId"));
            Assert.That(invalid, Does.Contain("cycle-2/parentId"));
        }
    }

    [Test]
    public void ValidForestPassesHierarchyValidation()
    {
        MailboxValidator validator = new MailboxValidator();
        Dictionary<string, Mailbox> mailboxes = new Dictionary<string, Mailbox>();
        mailboxes["inbox-1"] = Stored("inbox-1", "Inbox", null, JmapMailboxRoles.Inbox, true, false);
        mailboxes["archive-1"] = Stored("archive-1", "Archive", null, JmapMailboxRoles.Archive, true, true);
        mailboxes["year-2026"] = Stored("year-2026", "2026", "archive-1", null, true, true);

        Assert.That(validator.ValidateHierarchy(mailboxes, Capabilities(true, 3)), Is.Empty);
    }

    [Test]
    public void CreateHonorsTopLevelAndParentRightsUniquenessInboxAndDepth()
    {
        MailboxValidator validator = new MailboxValidator();
        Dictionary<string, Mailbox> existing = new Dictionary<string, Mailbox>();
        existing["root-1"] = Stored("root-1", "Root", null, null, false, true);
        existing["archive-1"] = Stored("archive-1", "Archive", "root-1", JmapMailboxRoles.Archive, true, true);
        Dictionary<string, Mailbox> created = new Dictionary<string, Mailbox>();
        created["middle"] = new Mailbox { Name = "Middle", ParentId = "root-1" };
        JmapMailCapabilities denied = Capabilities(false, 2);

        List<string> topLevel = validator.ValidateCreateAgainstHierarchy(
            new Mailbox { Name = "Top" }, existing, created, denied);
        List<string> deniedChild = validator.ValidateCreateAgainstHierarchy(
            new Mailbox { Name = "Child", ParentId = "root-1" }, existing, created, denied);
        existing["root-1"].MyRights!.MayCreateChild = true;
        List<string> duplicate = validator.ValidateCreateAgainstHierarchy(
            new Mailbox { Name = "Archive", ParentId = "root-1", Role = JmapMailboxRoles.Archive },
            existing,
            created,
            Capabilities(true, 3));
        List<string> nestedInbox = validator.ValidateCreateAgainstHierarchy(
            new Mailbox { Name = "New Inbox", ParentId = "root-1", Role = JmapMailboxRoles.Inbox },
            existing,
            created,
            Capabilities(true, 3));
        List<string> tooDeep = validator.ValidateCreateAgainstHierarchy(
            new Mailbox { Name = "Leaf", ParentId = "#middle" },
            existing,
            created,
            Capabilities(true, 2));
        List<string> allowed = validator.ValidateCreateAgainstHierarchy(
            new Mailbox { Name = "Leaf", ParentId = "#middle" },
            existing,
            created,
            Capabilities(true, 3));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(topLevel, Does.Contain("parentId"));
            Assert.That(deniedChild, Does.Contain("parentId"));
            Assert.That(duplicate, Does.Contain("name"));
            Assert.That(duplicate, Does.Contain("role"));
            Assert.That(nestedInbox, Does.Contain("parentId"));
            Assert.That(tooDeep, Does.Contain("parentId"));
            Assert.That(allowed, Is.Empty);
        }
    }

    [Test]
    public void UpdateEnforcesRenameDestinationCycleDescendantDepthAndImmutableFields()
    {
        MailboxValidator validator = new MailboxValidator();
        Dictionary<string, Mailbox> existing = new Dictionary<string, Mailbox>();
        existing["root-a"] = Stored("root-a", "A", null, null, true, true);
        existing["root-b"] = Stored("root-b", "B", null, null, false, false);
        existing["child-1"] = Stored("child-1", "Child", "root-a", null, true, true);
        existing["leaf-1"] = Stored("leaf-1", "Leaf", "child-1", null, true, true);
        Dictionary<string, Mailbox> created = new Dictionary<string, Mailbox>();
        JmapMailCapabilities capabilities = Capabilities(true, 2);

        Mailbox subscriptionOnly = Copy(existing["root-b"]);
        subscriptionOnly.IsSubscribed = !subscriptionOnly.IsSubscribed;
        List<string> subscriptionResult = validator.ValidateUpdateAgainstHierarchy(
            "root-b", subscriptionOnly, existing, created, capabilities);

        Mailbox deniedRename = Copy(existing["root-b"]);
        deniedRename.Name = "Renamed";
        List<string> renameResult = validator.ValidateUpdateAgainstHierarchy(
            "root-b", deniedRename, existing, created, capabilities);

        Mailbox move = Copy(existing["child-1"]);
        move.ParentId = "root-b";
        move.TotalEmails = 1;
        List<string> moveResult = validator.ValidateUpdateAgainstHierarchy(
            "child-1", move, existing, created, capabilities);

        existing["root-b"].MyRights!.MayCreateChild = true;
        Mailbox depthMove = Copy(existing["child-1"]);
        depthMove.ParentId = "root-b";
        List<string> depthResult = validator.ValidateUpdateAgainstHierarchy(
            "child-1", depthMove, existing, created, capabilities);

        Mailbox cycle = Copy(existing["root-a"]);
        cycle.ParentId = "leaf-1";
        List<string> cycleResult = validator.ValidateUpdateAgainstHierarchy(
            "root-a", cycle, existing, created, Capabilities(true, 10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(subscriptionResult, Is.Empty);
            Assert.That(renameResult, Is.EqualTo(new[] { "name" }));
            Assert.That(moveResult, Does.Contain("parentId"));
            Assert.That(moveResult, Does.Contain("totalEmails"));
            Assert.That(depthResult, Does.Contain("parentId"));
            Assert.That(cycleResult, Does.Contain("parentId"));
        }
    }

    private static JmapPatchObject Patch(params (string Path, string Json)[] values)
    {
        JmapPatchObject patch = new JmapPatchObject();
        foreach ((string path, string json) in values)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            patch.Values.Add(path, document.RootElement.Clone());
        }

        return patch;
    }

    private static JmapMailCapabilities Capabilities(bool topLevel, long depth)
    {
        return new JmapMailCapabilities
        {
            MaxSizeMailboxName = 100,
            HasMaxMailboxDepth = true,
            MaxMailboxDepth = depth,
            MayCreateTopLevelMailbox = topLevel
        };
    }

    private static Mailbox Stored(
        string id,
        string name,
        string? parentId,
        string? role,
        bool mayCreateChild,
        bool mayRename)
    {
        return new Mailbox
        {
            Id = id,
            Name = name,
            ParentId = parentId,
            Role = role,
            MyRights = new MailboxRights
            {
                MayCreateChild = mayCreateChild,
                MayRename = mayRename
            }
        };
    }

    private static Mailbox Copy(Mailbox source)
    {
        return new Mailbox
        {
            Id = source.Id,
            Name = source.Name,
            ParentId = source.ParentId,
            Role = source.Role,
            SortOrder = source.SortOrder,
            TotalEmails = source.TotalEmails,
            UnreadEmails = source.UnreadEmails,
            TotalThreads = source.TotalThreads,
            UnreadThreads = source.UnreadThreads,
            MyRights = source.MyRights == null ? null : new MailboxRights
            {
                MayReadItems = source.MyRights.MayReadItems,
                MayAddItems = source.MyRights.MayAddItems,
                MayRemoveItems = source.MyRights.MayRemoveItems,
                MaySetSeen = source.MyRights.MaySetSeen,
                MaySetKeywords = source.MyRights.MaySetKeywords,
                MayCreateChild = source.MyRights.MayCreateChild,
                MayRename = source.MyRights.MayRename,
                MayDelete = source.MyRights.MayDelete,
                MaySubmit = source.MyRights.MaySubmit
            },
            IsSubscribed = source.IsSubscribed
        };
    }
}
