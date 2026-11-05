namespace svg;

/// <summary>Stores an ordinal set of library or host capability identifiers.</summary>
public class SvgCapabilitySet
{
    /// <summary>The mutable identifiers; ordinary methods preserve uniqueness.</summary>
    public System.Collections.Generic.List<string> Identifiers;

    /// <summary>Creates an empty capability set.</summary>
    public SvgCapabilitySet()
    {
        Identifiers = new System.Collections.Generic.List<string>();
    }

    /// <summary>Creates a capability set from an identifier array.</summary>
    /// <param name="identifiers">The identifiers to add in input order.</param>
    /// <returns>A new capability set.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>identifiers</c> is <c>null</c>.</exception>
    internal static SvgCapabilitySet Create(string[] identifiers)
    {
        if (identifiers == null)
        {
            throw new System.ArgumentNullException("identifiers");
        }

        SvgCapabilitySet result = new SvgCapabilitySet();
        int index = 0;
        while (index < identifiers.Length)
        {
            result.Add(identifiers[index]);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Adds an identifier if it is not already present.</summary>
    /// <param name="identifier">The nonempty identifier.</param>
    /// <returns><c>true</c> when the identifier was added.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <c>identifier</c> is empty.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>identifier</c> is <c>null</c>.</exception>
    internal bool Add(string identifier)
    {
        if (identifier == null)
        {
            throw new System.ArgumentNullException("identifier");
        }
        if (identifier.Length == 0)
        {
            throw new System.ArgumentException("Capability identifiers must not be empty.", "identifier");
        }
        if (Contains(identifier))
        {
            return false;
        }
        Identifiers.Add(identifier);
        return true;
    }

    /// <summary>Tests an identifier using ordinal case-sensitive equality.</summary>
    /// <param name="identifier">The identifier to find.</param>
    /// <returns><c>true</c> when the identifier is present.</returns>
    internal bool Contains(string? identifier)
    {
        if (identifier == null)
        {
            return false;
        }

        int index = 0;
        while (index < Identifiers.Count)
        {
            if (string.Equals(Identifiers[index], identifier, System.StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    /// <summary>Removes every ordinal match for an identifier.</summary>
    /// <param name="identifier">The identifier to remove.</param>
    /// <returns><c>true</c> when at least one entry was removed.</returns>
    internal bool Remove(string? identifier)
    {
        if (identifier == null)
        {
            return false;
        }

        bool removed = false;
        int index = Identifiers.Count - 1;
        while (index >= 0)
        {
            if (string.Equals(Identifiers[index], identifier, System.StringComparison.Ordinal))
            {
                Identifiers.RemoveAt(index);
                removed = true;
            }
            index = index - 1;
        }
        return removed;
    }

    /// <summary>Returns an ordinally sorted snapshot without duplicate identifiers.</summary>
    /// <returns>A new deterministic identifier list.</returns>
    internal System.Collections.Generic.List<string> Snapshot()
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < Identifiers.Count)
        {
            string identifier = Identifiers[index];
            if (identifier != null && identifier.Length > 0)
            {
                int insertion = 0;
                while (insertion < result.Count && string.Compare(result[insertion], identifier, System.StringComparison.Ordinal) < 0)
                {
                    insertion = insertion + 1;
                }
                if (insertion == result.Count || !string.Equals(result[insertion], identifier, System.StringComparison.Ordinal))
                {
                    result.Insert(insertion, identifier);
                }
            }
            index = index + 1;
        }
        return result;
    }

    /// <summary>Creates an independent copy with the same identifiers.</summary>
    /// <returns>A new capability set.</returns>
    internal SvgCapabilitySet Clone()
    {
        SvgCapabilitySet result = new SvgCapabilitySet();
        System.Collections.Generic.List<string> snapshot = Snapshot();
        int index = 0;
        while (index < snapshot.Count)
        {
            result.Identifiers.Add(snapshot[index]);
            index = index + 1;
        }
        return result;
    }
}

/// <summary>Describes one feature occurrence and its handling status.</summary>
public class SvgFeatureEntry
{
    /// <summary>The stable feature identifier.</summary>
    public string Identifier;

    /// <summary>The stable profile identifier.</summary>
    public string ProfileIdentifier;

    /// <summary>The feature handling status.</summary>
    public SvgFeatureStatus Status;

    /// <summary>A deterministic human-readable status detail.</summary>
    public string Detail;

    /// <summary>The source span that used the feature, or <c>null</c> for library entries.</summary>
    public SvgSourceSpan? Source;

    /// <summary>The report-assigned insertion sequence, or <c>-1</c> before addition.</summary>
    public long Sequence;

    /// <summary>Creates a feature entry.</summary>
    /// <param name="identifier">The nonempty feature identifier.</param>
    /// <param name="profileIdentifier">The nonempty profile identifier.</param>
    /// <param name="status">The handling status.</param>
    /// <param name="detail">The non-null status detail.</param>
    /// <param name="source">The source span, or <c>null</c>.</param>
    /// <exception cref="System.ArgumentException">Thrown for an empty identifier.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown for a required <c>null</c> string.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for an invalid status.</exception>
    public SvgFeatureEntry(string identifier, string profileIdentifier, SvgFeatureStatus status, string detail, SvgSourceSpan? source)
    {
        if (identifier == null)
        {
            throw new System.ArgumentNullException("identifier");
        }
        if (identifier.Length == 0)
        {
            throw new System.ArgumentException("Feature identifiers must not be empty.", "identifier");
        }
        if (profileIdentifier == null)
        {
            throw new System.ArgumentNullException("profileIdentifier");
        }
        if (profileIdentifier.Length == 0)
        {
            throw new System.ArgumentException("Profile identifiers must not be empty.", "profileIdentifier");
        }
        if (detail == null)
        {
            throw new System.ArgumentNullException("detail");
        }
        if (status < SvgFeatureStatus.Implemented || status > SvgFeatureStatus.Unsupported)
        {
            throw new System.ArgumentOutOfRangeException("status");
        }

        Identifier = identifier;
        ProfileIdentifier = profileIdentifier;
        Status = status;
        Detail = detail;
        Source = source;
        Sequence = -1;
    }
}

/// <summary>Collects feature statuses and related diagnostics.</summary>
public class SvgFeatureReport
{
    /// <summary>The feature entries in insertion order.</summary>
    public System.Collections.Generic.List<SvgFeatureEntry> Entries;

    /// <summary>The related diagnostics in insertion order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal long NextSequence;

    /// <summary>Creates an empty feature report.</summary>
    public SvgFeatureReport()
    {
        Entries = new System.Collections.Generic.List<SvgFeatureEntry>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        NextSequence = 0;
    }

    /// <summary>Adds a feature entry and assigns its sequence.</summary>
    /// <param name="entry">The entry to add.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>entry</c> is <c>null</c>.</exception>
    internal void Add(SvgFeatureEntry entry)
    {
        if (entry == null)
        {
            throw new System.ArgumentNullException("entry");
        }
        entry.Sequence = NextSequence;
        NextSequence = NextSequence + 1;
        Entries.Add(entry);
    }

    /// <summary>Adds a related diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>diagnostic</c> is <c>null</c>.</exception>
    internal void AddDiagnostic(SvgDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new System.ArgumentNullException("diagnostic");
        }
        Diagnostics.Add(diagnostic);
    }

    /// <summary>Returns an independently allocated, deterministically ordered entry list.</summary>
    /// <returns>The sorted feature entries.</returns>
    internal System.Collections.Generic.List<SvgFeatureEntry> Snapshot()
    {
        System.Collections.Generic.List<SvgFeatureEntry> result = new System.Collections.Generic.List<SvgFeatureEntry>();
        int index = 0;
        while (index < Entries.Count)
        {
            SvgFeatureEntry entry = Entries[index];
            if (entry != null)
            {
                int insertion = result.Count;
                while (insertion > 0 && SvgFeatureOrdering.Compare(entry, result[insertion - 1]) < 0)
                {
                    insertion = insertion - 1;
                }
                result.Insert(insertion, entry);
            }
            index = index + 1;
        }
        return result;
    }

    /// <summary>Finds the first deterministic entry with an identifier.</summary>
    /// <param name="identifier">The feature identifier.</param>
    /// <returns>The matching entry, or <c>null</c>.</returns>
    internal SvgFeatureEntry? Find(string? identifier)
    {
        if (identifier == null)
        {
            return null;
        }
        System.Collections.Generic.List<SvgFeatureEntry> snapshot = Snapshot();
        int index = 0;
        while (index < snapshot.Count)
        {
            if (string.Equals(snapshot[index].Identifier, identifier, System.StringComparison.Ordinal))
            {
                return snapshot[index];
            }
            index = index + 1;
        }
        return null;
    }

    /// <summary>Returns entries with one handling status.</summary>
    /// <param name="status">The status to select.</param>
    /// <returns>A new deterministic filtered list.</returns>
    internal System.Collections.Generic.List<SvgFeatureEntry> FilterByStatus(SvgFeatureStatus status)
    {
        System.Collections.Generic.List<SvgFeatureEntry> snapshot = Snapshot();
        System.Collections.Generic.List<SvgFeatureEntry> result = new System.Collections.Generic.List<SvgFeatureEntry>();
        int index = 0;
        while (index < snapshot.Count)
        {
            if (snapshot[index].Status == status)
            {
                result.Add(snapshot[index]);
            }
            index = index + 1;
        }
        return result;
    }

    /// <summary>Returns entries assigned to one profile.</summary>
    /// <param name="profileIdentifier">The profile identifier to select.</param>
    /// <returns>A new deterministic filtered list.</returns>
    internal System.Collections.Generic.List<SvgFeatureEntry> FilterByProfile(string? profileIdentifier)
    {
        System.Collections.Generic.List<SvgFeatureEntry> snapshot = Snapshot();
        System.Collections.Generic.List<SvgFeatureEntry> result = new System.Collections.Generic.List<SvgFeatureEntry>();
        if (profileIdentifier == null)
        {
            return result;
        }
        int index = 0;
        while (index < snapshot.Count)
        {
            if (string.Equals(snapshot[index].ProfileIdentifier, profileIdentifier, System.StringComparison.Ordinal))
            {
                result.Add(snapshot[index]);
            }
            index = index + 1;
        }
        return result;
    }
}
