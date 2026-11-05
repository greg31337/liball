using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace jmap;

internal class MailboxValidator
{
    internal ValueValidator Values = new ValueValidator();
    internal MethodContext Access = new MethodContext();

    internal List<string> ValidateCreate(Mailbox? mailbox, long maximumNameBytes)
    {
        List<string> invalid = new List<string>();
        if (mailbox == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (mailbox.Id != null)
        {
            invalid.Add("id");
        }

        ValidateMutableFields(mailbox, maximumNameBytes, invalid);
        if (mailbox.TotalEmails != 0L)
        {
            invalid.Add("totalEmails");
        }

        if (mailbox.UnreadEmails != 0L)
        {
            invalid.Add("unreadEmails");
        }

        if (mailbox.TotalThreads != 0L)
        {
            invalid.Add("totalThreads");
        }

        if (mailbox.UnreadThreads != 0L)
        {
            invalid.Add("unreadThreads");
        }

        if (mailbox.MyRights != null)
        {
            invalid.Add("myRights");
        }

        return invalid;
    }

    internal List<string> ValidateStoredMailbox(Mailbox? mailbox, long maximumNameBytes)
    {
        List<string> invalid = new List<string>();
        if (mailbox == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (!Values.IsId(mailbox.Id))
        {
            invalid.Add("id");
        }

        ValidateMutableFields(mailbox, maximumNameBytes, invalid);
        if (!Values.IsUnsignedInt(mailbox.TotalEmails))
        {
            invalid.Add("totalEmails");
        }

        if (!Values.IsUnsignedInt(mailbox.UnreadEmails) || mailbox.UnreadEmails > mailbox.TotalEmails)
        {
            invalid.Add("unreadEmails");
        }

        if (!Values.IsUnsignedInt(mailbox.TotalThreads))
        {
            invalid.Add("totalThreads");
        }

        if (!Values.IsUnsignedInt(mailbox.UnreadThreads) || mailbox.UnreadThreads > mailbox.TotalThreads)
        {
            invalid.Add("unreadThreads");
        }

        if (mailbox.MyRights == null)
        {
            invalid.Add("myRights");
        }

        return invalid;
    }

    internal List<string> ValidateWritablePatch(JmapPatchObject? patch)
    {
        List<string> invalid = new List<string>();
        if (patch == null)
        {
            invalid.Add("");
            return invalid;
        }

        foreach (string path in patch.Values.Keys)
        {
            if (path != "name" && path != "parentId" && path != "role" && path != "sortOrder" && path != "isSubscribed")
            {
                invalid.Add(path);
            }
        }

        return invalid;
    }

    internal List<string> ValidatePatchValues(JmapPatchObject? patch, long maximumNameBytes)
    {
        List<string> invalid = ValidateWritablePatch(patch);
        if (patch == null)
        {
            return invalid;
        }

        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            if (item.Key == "name")
            {
                if (item.Value.ValueKind != JsonValueKind.String || !IsMailboxName(item.Value.GetString(), maximumNameBytes))
                {
                    AddInvalid(item.Key, invalid);
                }
            }
            else if (item.Key == "parentId")
            {
                if (item.Value.ValueKind != JsonValueKind.Null &&
                    (item.Value.ValueKind != JsonValueKind.String || !IsParentId(item.Value.GetString())))
                {
                    AddInvalid(item.Key, invalid);
                }
            }
            else if (item.Key == "role")
            {
                if (item.Value.ValueKind != JsonValueKind.Null &&
                    (item.Value.ValueKind != JsonValueKind.String || !Values.IsMailboxRole(item.Value.GetString())))
                {
                    AddInvalid(item.Key, invalid);
                }
            }
            else if (item.Key == "sortOrder")
            {
                if (!IsSortOrderElement(item.Value))
                {
                    AddInvalid(item.Key, invalid);
                }
            }
            else if (item.Key == "isSubscribed" && item.Value.ValueKind != JsonValueKind.True && item.Value.ValueKind != JsonValueKind.False)
            {
                AddInvalid(item.Key, invalid);
            }
        }

        return invalid;
    }

    internal List<string> ValidateHierarchy(
        Dictionary<string, Mailbox>? mailboxes,
        JmapMailCapabilities? capabilities)
    {
        List<string> invalid = new List<string>();
        if (mailboxes == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (capabilities == null)
        {
            invalid.Add("capabilities");
            return invalid;
        }

        foreach (KeyValuePair<string, Mailbox> entry in mailboxes)
        {
            string prefix = entry.Key + "/";
            Mailbox? mailbox = entry.Value;
            if (!Values.IsId(entry.Key) || mailbox == null)
            {
                AddInvalid(entry.Key, invalid);
            }
            else
            {
                List<string> fields = ValidateStoredMailbox(mailbox, capabilities.MaxSizeMailboxName);
                foreach (string field in fields)
                {
                    AddInvalid(prefix + field, invalid);
                }

                if (mailbox.Id != entry.Key)
                {
                    AddInvalid(prefix + "id", invalid);
                }

                if (mailbox.ParentId != null && !mailboxes.ContainsKey(mailbox.ParentId))
                {
                    AddInvalid(prefix + "parentId", invalid);
                }

                if (mailbox.Role == JmapMailboxRoles.Inbox && mailbox.ParentId != null)
                {
                    AddInvalid(prefix + "parentId", invalid);
                }
            }
        }

        ValidateUniqueNamesAndRoles(mailboxes, invalid);
        ValidateForestDepthAndCycles(mailboxes, capabilities, invalid);
        return invalid;
    }

    internal List<string> ValidateCreateAgainstHierarchy(
        Mailbox? mailbox,
        Dictionary<string, Mailbox>? mailboxes,
        Dictionary<string, Mailbox>? created,
        JmapMailCapabilities? capabilities)
    {
        long maximumNameBytes = capabilities == null ? 0L : capabilities.MaxSizeMailboxName;
        List<string> invalid = ValidateCreate(mailbox, maximumNameBytes);
        if (mailbox == null)
        {
            return invalid;
        }

        if (mailboxes == null)
        {
            AddInvalid("mailboxes", invalid);
            return invalid;
        }

        if (created == null)
        {
            AddInvalid("created", invalid);
            return invalid;
        }

        if (capabilities == null)
        {
            AddInvalid("capabilities", invalid);
            return invalid;
        }

        if (mailbox.ParentId == null)
        {
            if (!capabilities.MayCreateTopLevelMailbox)
            {
                AddInvalid("parentId", invalid);
            }
        }
        else
        {
            Mailbox? parent = FindParent(mailbox.ParentId, mailboxes, created);
            if (parent == null)
            {
                AddInvalid("parentId", invalid);
            }
            else if (!mailbox.ParentId.StartsWith("#", global::System.StringComparison.Ordinal) &&
                !Access.HasMailboxRight(parent, MailboxRightKind.CreateChild))
            {
                AddInvalid("parentId", invalid);
            }
        }

        if (mailbox.Role == JmapMailboxRoles.Inbox && mailbox.ParentId != null)
        {
            AddInvalid("parentId", invalid);
        }

        if (HasSiblingName(mailbox, null, mailboxes, created))
        {
            AddInvalid("name", invalid);
        }

        if (HasRole(mailbox.Role, null, mailboxes, created))
        {
            AddInvalid("role", invalid);
        }

        if (!IsDepthAllowed(mailbox.ParentId, mailboxes, created, capabilities))
        {
            AddInvalid("parentId", invalid);
        }

        return invalid;
    }

    internal List<string> ValidateUpdateAgainstHierarchy(
        string? mailboxId,
        Mailbox? candidate,
        Dictionary<string, Mailbox>? mailboxes,
        Dictionary<string, Mailbox>? created,
        JmapMailCapabilities? capabilities)
    {
        List<string> invalid = new List<string>();
        if (mailboxId == null || !Values.IsId(mailboxId) || mailboxes == null || !mailboxes.ContainsKey(mailboxId))
        {
            invalid.Add("");
            return invalid;
        }

        if (candidate == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (created == null)
        {
            invalid.Add("created");
            return invalid;
        }

        if (capabilities == null)
        {
            invalid.Add("capabilities");
            return invalid;
        }

        Mailbox? existingMailbox = mailboxes[mailboxId];
        if (existingMailbox == null)
        {
            invalid.Add("");
            return invalid;
        }

        Mailbox original = existingMailbox;
        List<string> stored = ValidateStoredMailbox(candidate, capabilities.MaxSizeMailboxName);
        foreach (string path in stored)
        {
            AddInvalid(path, invalid);
        }

        ValidateImmutableFields(original, candidate, invalid);
        bool nameChanged = original.Name != candidate.Name;
        bool parentChanged = original.ParentId != candidate.ParentId;
        bool roleChanged = original.Role != candidate.Role;
        if ((nameChanged || parentChanged || roleChanged) &&
            !Access.HasMailboxRight(original, MailboxRightKind.Rename))
        {
            if (nameChanged)
            {
                AddInvalid("name", invalid);
            }

            if (parentChanged)
            {
                AddInvalid("parentId", invalid);
            }

            if (roleChanged)
            {
                AddInvalid("role", invalid);
            }
        }

        if (parentChanged)
        {
            ValidateMoveDestination(candidate.ParentId, mailboxes, created, capabilities, invalid);
        }

        if (candidate.Role == JmapMailboxRoles.Inbox && candidate.ParentId != null)
        {
            AddInvalid("parentId", invalid);
        }

        if (HasSiblingName(candidate, mailboxId, mailboxes, created))
        {
            AddInvalid("name", invalid);
        }

        if (HasRole(candidate.Role, mailboxId, mailboxes, created))
        {
            AddInvalid("role", invalid);
        }

        if (CreatesCycle(mailboxId, candidate.ParentId, mailboxes, created) ||
            !IsUpdatedTreeDepthAllowed(mailboxId, candidate, mailboxes, created, capabilities))
        {
            AddInvalid("parentId", invalid);
        }

        return invalid;
    }

    internal void ValidateMutableFields(Mailbox mailbox, long maximumNameBytes, List<string> invalid)
    {
        if (!IsMailboxName(mailbox.Name, maximumNameBytes))
        {
            invalid.Add("name");
        }

        if (!IsParentId(mailbox.ParentId))
        {
            invalid.Add("parentId");
        }

        if (mailbox.Role != null && !Values.IsMailboxRole(mailbox.Role))
        {
            invalid.Add("role");
        }

        if (mailbox.SortOrder < 0L || mailbox.SortOrder >= 2147483648L)
        {
            invalid.Add("sortOrder");
        }
    }

    internal bool IsMailboxName(string? name, long maximumNameBytes)
    {
        if (name == null || name.Length == 0 || !Values.IsUtf8WithinSize(name, maximumNameBytes))
        {
            return false;
        }

        if (!name.IsNormalized(NormalizationForm.FormC))
        {
            return false;
        }

        int index = 0;
        while (index < name.Length)
        {
            int character = name[index];
            if (character <= 31 || (character >= 127 && character <= 159) || character == 8232 || character == 8233)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsParentId(string? parentId)
    {
        return parentId == null || Values.IsId(parentId) || Values.IsCreationIdReference(parentId);
    }

    internal bool IsSortOrderElement(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        string text = value.GetRawText();
        if (text.Length == 0 || text.Length > 10)
        {
            return false;
        }

        if (text == "-0")
        {
            return true;
        }

        long result = 0L;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            result = (result * 10L) + (character - '0');
            if (result >= 2147483648L)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void ValidateUniqueNamesAndRoles(Dictionary<string, Mailbox> mailboxes, List<string> invalid)
    {
        List<string> ids = new List<string>(mailboxes.Keys);
        int leftIndex = 0;
        while (leftIndex < ids.Count)
        {
            string leftId = ids[leftIndex];
            Mailbox? left = mailboxes[leftId];
            if (left != null)
            {
                int rightIndex = leftIndex + 1;
                while (rightIndex < ids.Count)
                {
                    string rightId = ids[rightIndex];
                    Mailbox? right = mailboxes[rightId];
                    if (right != null)
                    {
                        if (left.ParentId == right.ParentId && left.Name == right.Name)
                        {
                            AddInvalid(leftId + "/name", invalid);
                            AddInvalid(rightId + "/name", invalid);
                        }

                        if (left.Role != null && left.Role == right.Role)
                        {
                            AddInvalid(leftId + "/role", invalid);
                            AddInvalid(rightId + "/role", invalid);
                        }
                    }

                    rightIndex = rightIndex + 1;
                }
            }

            leftIndex = leftIndex + 1;
        }
    }

    internal void ValidateForestDepthAndCycles(
        Dictionary<string, Mailbox> mailboxes,
        JmapMailCapabilities capabilities,
        List<string> invalid)
    {
        foreach (string mailboxId in mailboxes.Keys)
        {
            HashSet<string> ancestors = new HashSet<string>(global::System.StringComparer.Ordinal);
            ancestors.Add(mailboxId);
            string currentId = mailboxId;
            long depth = 1L;
            bool finished = false;
            while (!finished && mailboxes.ContainsKey(currentId) && mailboxes[currentId] != null)
            {
                string? parentId = mailboxes[currentId].ParentId;
                if (parentId == null || !mailboxes.ContainsKey(parentId))
                {
                    finished = true;
                }
                else if (!ancestors.Add(parentId))
                {
                    AddInvalid(mailboxId + "/parentId", invalid);
                    finished = true;
                }
                else
                {
                    depth = depth + 1L;
                    currentId = parentId;
                }
            }

            if (capabilities.HasMaxMailboxDepth && !capabilities.IsMaxMailboxDepthNull && depth > capabilities.MaxMailboxDepth)
            {
                AddInvalid(mailboxId + "/parentId", invalid);
            }
        }
    }

    internal Mailbox? FindParent(
        string parentId,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created)
    {
        if (parentId.StartsWith("#", global::System.StringComparison.Ordinal))
        {
            string creationId = parentId.Substring(1);
            return created.ContainsKey(creationId) ? created[creationId] : null;
        }

        return mailboxes.ContainsKey(parentId) ? mailboxes[parentId] : null;
    }

    internal bool HasSiblingName(
        Mailbox mailbox,
        string? ignoredId,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created)
    {
        foreach (KeyValuePair<string, Mailbox> entry in mailboxes)
        {
            Mailbox? existing = entry.Value;
            if (entry.Key != ignoredId && existing != null && existing.ParentId == mailbox.ParentId && existing.Name == mailbox.Name)
            {
                return true;
            }
        }

        foreach (Mailbox existing in created.Values)
        {
            if (existing != null && existing.ParentId == mailbox.ParentId && existing.Name == mailbox.Name)
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasRole(
        string? role,
        string? ignoredId,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created)
    {
        if (role == null)
        {
            return false;
        }

        foreach (KeyValuePair<string, Mailbox> entry in mailboxes)
        {
            Mailbox? existing = entry.Value;
            if (entry.Key != ignoredId && existing != null && existing.Role == role)
            {
                return true;
            }
        }

        foreach (Mailbox existing in created.Values)
        {
            if (existing != null && existing.Role == role)
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsDepthAllowed(
        string? parentId,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created,
        JmapMailCapabilities capabilities)
    {
        long depth = 1L;
        HashSet<string> seen = new HashSet<string>(global::System.StringComparer.Ordinal);
        string? currentId = parentId;
        while (currentId != null)
        {
            if (!seen.Add(currentId))
            {
                return false;
            }

            depth = depth + 1L;
            Mailbox? parent = FindParent(currentId, mailboxes, created);
            if (parent == null)
            {
                return false;
            }

            currentId = parent.ParentId;
        }

        return !capabilities.HasMaxMailboxDepth || capabilities.IsMaxMailboxDepthNull || depth <= capabilities.MaxMailboxDepth;
    }

    internal void ValidateMoveDestination(
        string? parentId,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created,
        JmapMailCapabilities capabilities,
        List<string> invalid)
    {
        if (parentId == null)
        {
            if (!capabilities.MayCreateTopLevelMailbox)
            {
                AddInvalid("parentId", invalid);
            }

            return;
        }

        Mailbox? parent = FindParent(parentId, mailboxes, created);
        if (parent == null)
        {
            AddInvalid("parentId", invalid);
        }
        else if (!parentId.StartsWith("#", global::System.StringComparison.Ordinal) &&
            !Access.HasMailboxRight(parent, MailboxRightKind.CreateChild))
        {
            AddInvalid("parentId", invalid);
        }
    }

    internal bool CreatesCycle(
        string mailboxId,
        string? parentId,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created)
    {
        HashSet<string> seen = new HashSet<string>(global::System.StringComparer.Ordinal);
        seen.Add(mailboxId);
        string? currentId = parentId;
        while (currentId != null)
        {
            if (!seen.Add(currentId))
            {
                return true;
            }

            Mailbox? parent = FindParent(currentId, mailboxes, created);
            if (parent == null)
            {
                return false;
            }

            currentId = parent.ParentId;
        }

        return false;
    }

    internal bool IsUpdatedTreeDepthAllowed(
        string mailboxId,
        Mailbox candidate,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created,
        JmapMailCapabilities capabilities)
    {
        if (!capabilities.HasMaxMailboxDepth || capabilities.IsMaxMailboxDepthNull)
        {
            return true;
        }

        long candidateDepth = CalculateDepth(candidate.ParentId, mailboxes, created);
        if (candidateDepth < 0L || candidateDepth > capabilities.MaxMailboxDepth)
        {
            return false;
        }

        foreach (string descendantId in mailboxes.Keys)
        {
            string currentId = descendantId;
            long edges = 0L;
            HashSet<string> seen = new HashSet<string>(global::System.StringComparer.Ordinal);
            seen.Add(currentId);
            bool reachesCandidate = currentId == mailboxId;
            while (!reachesCandidate && mailboxes.ContainsKey(currentId) && mailboxes[currentId] != null)
            {
                string? nextParentId = mailboxes[currentId].ParentId;
                if (nextParentId == null)
                {
                    break;
                }

                string parentId = nextParentId;
                if (!mailboxes.ContainsKey(parentId) || !seen.Add(parentId))
                {
                    break;
                }

                edges = edges + 1L;
                currentId = parentId;
                reachesCandidate = currentId == mailboxId;
            }

            if (reachesCandidate && candidateDepth + edges > capabilities.MaxMailboxDepth)
            {
                return false;
            }
        }

        return true;
    }

    internal long CalculateDepth(
        string? parentId,
        Dictionary<string, Mailbox> mailboxes,
        Dictionary<string, Mailbox> created)
    {
        long depth = 1L;
        HashSet<string> seen = new HashSet<string>(global::System.StringComparer.Ordinal);
        string? currentId = parentId;
        while (currentId != null)
        {
            if (!seen.Add(currentId))
            {
                return -1L;
            }

            Mailbox? parent = FindParent(currentId, mailboxes, created);
            if (parent == null)
            {
                return -1L;
            }

            depth = depth + 1L;
            currentId = parent.ParentId;
        }

        return depth;
    }

    internal void ValidateImmutableFields(Mailbox original, Mailbox candidate, List<string> invalid)
    {
        if (candidate.Id != original.Id)
        {
            AddInvalid("id", invalid);
        }

        if (candidate.TotalEmails != original.TotalEmails)
        {
            AddInvalid("totalEmails", invalid);
        }

        if (candidate.UnreadEmails != original.UnreadEmails)
        {
            AddInvalid("unreadEmails", invalid);
        }

        if (candidate.TotalThreads != original.TotalThreads)
        {
            AddInvalid("totalThreads", invalid);
        }

        if (candidate.UnreadThreads != original.UnreadThreads)
        {
            AddInvalid("unreadThreads", invalid);
        }

        if (!RightsEqual(candidate.MyRights, original.MyRights))
        {
            AddInvalid("myRights", invalid);
        }
    }

    internal bool RightsEqual(MailboxRights? left, MailboxRights? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        return left.MayReadItems == right.MayReadItems &&
            left.MayAddItems == right.MayAddItems &&
            left.MayRemoveItems == right.MayRemoveItems &&
            left.MaySetSeen == right.MaySetSeen &&
            left.MaySetKeywords == right.MaySetKeywords &&
            left.MayCreateChild == right.MayCreateChild &&
            left.MayRename == right.MayRename &&
            left.MayDelete == right.MayDelete &&
            left.MaySubmit == right.MaySubmit;
    }

    internal void AddInvalid(string path, List<string> invalid)
    {
        if (!invalid.Contains(path))
        {
            invalid.Add(path);
        }
    }
}
