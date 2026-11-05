using System;
using System.Collections.Generic;

namespace iso20022;

internal class RepositorySnapshotIndex
{
    internal List<RepositoryIndexEntry> Entries = new List<RepositoryIndexEntry>();
    internal Dictionary<string, List<RepositoryIndexEntry>> EntriesByIdentifier =
        new Dictionary<string, List<RepositoryIndexEntry>>(StringComparer.Ordinal);
    internal Dictionary<string, List<RepositoryIndexEntry>> EntriesByName =
        new Dictionary<string, List<RepositoryIndexEntry>>(StringComparer.Ordinal);
    internal Dictionary<RepositoryEntityKind, List<RepositoryIndexEntry>> EntriesByKind =
        new Dictionary<RepositoryEntityKind, List<RepositoryIndexEntry>>();
    internal Dictionary<RegistrationStatus, List<RepositoryIndexEntry>> EntriesByStatus =
        new Dictionary<RegistrationStatus, List<RepositoryIndexEntry>>();
    internal Dictionary<string, List<RepositoryIndexEntry>> EntriesByBusinessArea =
        new Dictionary<string, List<RepositoryIndexEntry>>(StringComparer.Ordinal);
    internal Dictionary<string, List<RepositoryIndexEntry>> EntriesByMessageSet =
        new Dictionary<string, List<RepositoryIndexEntry>>(StringComparer.Ordinal);
    internal Dictionary<string, List<RepositoryIndexEntry>> EntriesByMessageIdentifier =
        new Dictionary<string, List<RepositoryIndexEntry>>(StringComparer.Ordinal);
    internal Dictionary<string, List<RepositoryIndexEntry>> EntriesByMessageFunctionality =
        new Dictionary<string, List<RepositoryIndexEntry>>(StringComparer.Ordinal);
    internal Dictionary<string, List<RepositoryIndexEntry>> EntriesByVariant =
        new Dictionary<string, List<RepositoryIndexEntry>>(StringComparer.Ordinal);
    internal Dictionary<string, List<RepositoryIndexEntry>> EntriesByNamespace =
        new Dictionary<string, List<RepositoryIndexEntry>>(StringComparer.Ordinal);
    internal Dictionary<string, List<RepositoryIndexEntry>> EntriesByVersion =
        new Dictionary<string, List<RepositoryIndexEntry>>(StringComparer.Ordinal);

    internal List<RepositoryIndexEntry> FindIdentifier(string identifier)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException("identifier");
        }

        if (EntriesByIdentifier.ContainsKey(identifier))
        {
            return EntriesByIdentifier[identifier];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (EntriesByName.ContainsKey(name))
        {
            return EntriesByName[name];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindKind(RepositoryEntityKind kind)
    {
        if (EntriesByKind.ContainsKey(kind))
        {
            return EntriesByKind[kind];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindStatus(RegistrationStatus status)
    {
        if (EntriesByStatus.ContainsKey(status))
        {
            return EntriesByStatus[status];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindBusinessArea(string businessAreaCode)
    {
        if (businessAreaCode == null)
        {
            throw new ArgumentNullException("businessAreaCode");
        }

        if (EntriesByBusinessArea.ContainsKey(businessAreaCode))
        {
            return EntriesByBusinessArea[businessAreaCode];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindMessageSet(string messageSetIdentifier)
    {
        if (messageSetIdentifier == null)
        {
            throw new ArgumentNullException("messageSetIdentifier");
        }

        if (EntriesByMessageSet.ContainsKey(messageSetIdentifier))
        {
            return EntriesByMessageSet[messageSetIdentifier];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindMessageIdentifier(string messageIdentifier)
    {
        if (messageIdentifier == null)
        {
            throw new ArgumentNullException("messageIdentifier");
        }

        if (EntriesByMessageIdentifier.ContainsKey(messageIdentifier))
        {
            return EntriesByMessageIdentifier[messageIdentifier];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindNamespace(string namespaceUri)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (EntriesByNamespace.ContainsKey(namespaceUri))
        {
            return EntriesByNamespace[namespaceUri];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindMessageFunctionality(string messageFunctionality)
    {
        if (messageFunctionality == null)
        {
            throw new ArgumentNullException("messageFunctionality");
        }

        if (EntriesByMessageFunctionality.ContainsKey(messageFunctionality))
        {
            return EntriesByMessageFunctionality[messageFunctionality];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindVariant(string variant)
    {
        if (variant == null)
        {
            throw new ArgumentNullException("variant");
        }

        if (EntriesByVariant.ContainsKey(variant))
        {
            return EntriesByVariant[variant];
        }

        return new List<RepositoryIndexEntry>();
    }

    internal List<RepositoryIndexEntry> FindVersion(string version)
    {
        if (version == null)
        {
            throw new ArgumentNullException("version");
        }

        if (EntriesByVersion.ContainsKey(version))
        {
            return EntriesByVersion[version];
        }

        return new List<RepositoryIndexEntry>();
    }
}
