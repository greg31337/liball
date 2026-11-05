using System;
using System.Collections.Generic;

namespace email;
internal class EmailThreadGraphBuilder
{
    EmailThreadOptions options;
    List<EmailThreadNode> nodes;
    List<EmailThreadNode?> desiredParents;
    Dictionary<string, EmailThreadNode> identifiers;
    Dictionary<string, EmailThreadNode> subjectAnchors;
    EmailIssueList issues;
    int identifierCount;

    internal EmailThreadGraphBuilder(EmailThreadOptions options)
    {
        this.options = options;
        nodes = new List<EmailThreadNode>();
        desiredParents = new List<EmailThreadNode?>();
        identifiers = new Dictionary<string, EmailThreadNode>(StringComparer.Ordinal);
        subjectAnchors = new Dictionary<string, EmailThreadNode>(StringComparer.Ordinal);
        issues = new EmailIssueList();
        identifierCount = 0;
    }

    internal EmailThreadGraph Build(List<EmailMessage> messages)
    {
        ValidateMessages(messages);
        CreateNodes(messages);
        IndexIdentifiers();
        SelectParents();
        AttachParents();
        List<EmailThreadNode> roots = GetRoots();
        return new EmailThreadGraph(nodes, roots, issues);
    }

    void ValidateMessages(List<EmailMessage> messages)
    {
        if (messages.Count > options.GetMaximumGraphMessageCount())
        {
            throw new EmailLimitException("MaxThreadGraphMessages", options.GetMaximumGraphMessageCount(), messages.Count);
        }

        int index = 0;
        while (index < messages.Count)
        {
            if (messages[index] == null)
            {
                throw new ArgumentException("Message collections cannot contain null entries.", "messages");
            }

            index++;
        }
    }

    void CreateNodes(List<EmailMessage> messages)
    {
        int index = 0;
        while (index < messages.Count)
        {
            EmailThreadIdentity identity = new EmailThreadAnalyzer().ExtractWithMessageAndOptions(messages[index], options);
            EmailThreadNode node = new EmailThreadNode(messages[index], identity, index);
            nodes.Add(node);
            desiredParents.Add(null);
            issues.AddRange(identity.GetIssues());
            index++;
        }
    }

    void IndexIdentifiers()
    {
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            EmailThreadNode node = nodes[nodeIndex];
            EmailThreadIdentity identity = node.GetIdentity();
            int identifierIndex = 0;
            while (identifierIndex < identity.GetMessageIdentifierCount())
            {
                AddIdentifier(node, identity.GetMessageIdentifierAt(identifierIndex));
                identifierIndex++;
            }

            nodeIndex++;
        }
    }

    void AddIdentifier(EmailThreadNode node, EmailMessageId identifier)
    {
        identifierCount++;
        if (identifierCount > options.GetMaximumGraphIdentifierCount())
        {
            throw new EmailLimitException("MaxThreadGraphIdentifiers", options.GetMaximumGraphIdentifierCount(), identifierCount);
        }

        string key = new EmailThreadAnalyzer().CanonicalizeIdentifier(identifier);
        EmailThreadNode? existing;
        if (identifiers.TryGetValue(key, out existing))
        {
            EmailIssue issue = CreateIssue(EmailIssueSeverity.Warning, EmailIssueCode.ThreadDuplicateMessageId, "A Message-ID is shared by multiple input messages.", "Message-ID", "The first input message owns identifier-based parent lookups.");
            node.AddIssue(issue);
            existing.AddIssue(issue);
            issues.Add(issue);
            return;
        }

        identifiers.Add(key, node);
    }

    void SelectParents()
    {
        int index = 0;
        while (index < nodes.Count)
        {
            EmailThreadNode node = nodes[index];
            EmailThreadIdentity identity = node.GetIdentity();
            if (identity.HasParentCandidate())
            {
                string key = new EmailThreadAnalyzer().CanonicalizeIdentifier(identity.GetParentCandidate());
                EmailThreadNode? parent;
                if (identifiers.TryGetValue(key, out parent))
                {
                    desiredParents[index] = parent;
                }
                else
                {
                    string fieldName = identity.GetReferenceCount() > 0 ? "References" : "In-Reply-To";
                    AddNodeIssue(node, EmailIssueSeverity.Warning, EmailIssueCode.ThreadParentMissing, "The referenced parent message is not present in the input collection.", fieldName, "The message remains a graph root.");
                }
            }
            else if (identity.UsesSubjectFallback())
            {
                SelectSubjectParent(node, index, identity.GetThreadKey());
            }

            index++;
        }
    }

    void SelectSubjectParent(EmailThreadNode node, int index, string? key)
    {
        AddNodeIssue(node, EmailIssueSeverity.Information, EmailIssueCode.ThreadSubjectFallbackUsed, "Normalized-subject fallback supplied the conversation identity.", "Subject", "Identifier evidence was unavailable; grouping is heuristic.");
        EmailThreadNode? anchor;
        if (subjectAnchors.TryGetValue((key ?? throw new InvalidOperationException()), out anchor))
        {
            desiredParents[index] = anchor;
        }
        else
        {
            subjectAnchors.Add(key, node);
        }
    }

    void AttachParents()
    {
        int index = 0;
        while (index < nodes.Count)
        {
            EmailThreadNode node = nodes[index];
            EmailThreadNode? parent = desiredParents[index];
            if (parent != null)
            {
                List<EmailThreadNode> cycle = GetCycle(node, parent);
                if (cycle.Count > 0)
                {
                    EmailIssue issue = CreateIssue(EmailIssueSeverity.Warning, EmailIssueCode.ThreadCycleDetected, "A parent edge would create a conversation cycle.", null, "The cycle-closing edge was omitted.");
                    int cycleIndex = 0;
                    while (cycleIndex < cycle.Count)
                    {
                        cycle[cycleIndex].AddIssue(issue);
                        cycleIndex++;
                    }

                    issues.Add(issue);
                }
                else
                {
                    node.AttachTo(parent);
                }
            }

            index++;
        }
    }

    static List<EmailThreadNode> GetCycle(EmailThreadNode node, EmailThreadNode parent)
    {
        List<EmailThreadNode> path = new List<EmailThreadNode>();
        path.Add(node);
        EmailThreadNode? current = parent;
        while (current != null)
        {
            if (object.ReferenceEquals(current, node))
            {
                return path;
            }

            path.Add(current);
            current = current.GetParent();
        }

        path.Clear();
        return path;
    }

    List<EmailThreadNode> GetRoots()
    {
        List<EmailThreadNode> roots = new List<EmailThreadNode>();
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index].IsRoot())
            {
                roots.Add(nodes[index]);
            }

            index++;
        }

        return roots;
    }

    void AddNodeIssue(EmailThreadNode node, EmailIssueSeverity severity, EmailIssueCode code, string description, string fieldName, string recovery)
    {
        EmailIssue issue = CreateIssue(severity, code, description, fieldName, recovery);
        node.AddIssue(issue);
        issues.Add(issue);
    }

    static EmailIssue CreateIssue(EmailIssueSeverity severity, EmailIssueCode code, string description, string? fieldName, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, null, null, fieldName, recovery);
    }
}
