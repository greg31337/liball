using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal interface ICatalogEntrySource
{
    XmlCatalogEntry[]? LoadXmlCatalogEntries(string catalogUri);
}

internal static class CatalogIdentifiers
{
    internal static bool IsXmlPublicIdentifierUrn(string value)
    {
        Guard.NotNull(value, "value");
        return value.StartsWith("urn:publicid:", StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeXmlPublicIdentifier(string value)
    {
        Guard.NotNull(value, "value");
        StringBuilder normalized = new StringBuilder(value.Length);
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == ' ' || current == '\t' || current == '\r' || current == '\n')
            {
                if (normalized.Length > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    normalized.Append(' ');
                    pendingSpace = false;
                }

                normalized.Append(current);
            }

            index = index + 1;
        }

        return normalized.ToString();
    }

    internal static string NormalizeXmlPublicIdentifierInput(string value)
    {
        Guard.NotNull(value, "value");
        string identifier = IsXmlPublicIdentifierUrn(value) ? UnwrapXmlPublicIdentifierUrn(value) : value;
        return NormalizeXmlPublicIdentifier(identifier);
    }

    internal static string NormalizeXmlCatalogIdentifier(string value)
    {
        Guard.NotNull(value, "value");
        StringBuilder normalized = new StringBuilder(value.Length);
        const string hexadecimal = "0123456789ABCDEF";
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool disallowedAscii = current <= ' ' || current == '\u007f' || current == '"' || current == '<' || current == '>' || current == '\\' || current == '^' || current == '`' || current == '{' || current == '|' || current == '}';
            if (current <= 127 && !disallowedAscii)
            {
                normalized.Append(current);
                index = index + 1;
                continue;
            }

            int characterLength = 1;
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    throw new FormatException("An XML Catalog identifier contains an unmatched surrogate.");
                }

                characterLength = 2;
            }
            else if (char.IsLowSurrogate(current))
            {
                throw new FormatException("An XML Catalog identifier contains an unmatched surrogate.");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value.Substring(index, characterLength));
            int byteIndex = 0;
            while (byteIndex < bytes.Length)
            {
                int octet = bytes[byteIndex];
                normalized.Append('%');
                normalized.Append(hexadecimal[octet / 16]);
                normalized.Append(hexadecimal[octet % 16]);
                byteIndex = byteIndex + 1;
            }

            index = index + characterLength;
        }

        return normalized.ToString();
    }

    internal static string UnwrapXmlPublicIdentifierUrn(string value)
    {
        Guard.NotNull(value, "value");
        if (!IsXmlPublicIdentifierUrn(value))
        {
            throw new FormatException("The value is not a public identifier URN.");
        }

        string name = value.Substring(13);
        StringBuilder identifier = new StringBuilder(name.Length);
        int index = 0;
        while (index < name.Length)
        {
            char current = name[index];
            if (current == '+')
            {
                identifier.Append(' ');
                index = index + 1;
            }
            else if (current == ':')
            {
                identifier.Append("//");
                index = index + 1;
            }
            else if (current == ';')
            {
                identifier.Append("::");
                index = index + 1;
            }
            else if (current == '%')
            {
                if (index + 2 >= name.Length)
                {
                    throw new FormatException("A public identifier URN contains an incomplete percent escape.");
                }

                int high = UriTools.HexValue(name[index + 1]);
                int low = UriTools.HexValue(name[index + 2]);
                if (high < 0 || low < 0)
                {
                    throw new FormatException("A public identifier URN contains an invalid percent escape.");
                }

                int decoded = high * 16 + low;
                char transcribed = DecodeXmlPublicIdentifierUrnEscape(decoded);
                if (transcribed == '\0')
                {
                    throw new FormatException("A public identifier URN contains an unsupported percent escape.");
                }

                identifier.Append(transcribed);
                index = index + 3;
            }
            else
            {
                identifier.Append(current);
                index = index + 1;
            }
        }

        return identifier.ToString();
    }

    internal static char DecodeXmlPublicIdentifierUrnEscape(int octet)
    {
        if (octet == 0x2b)
        {
            return '+';
        }

        if (octet == 0x3a)
        {
            return ':';
        }

        if (octet == 0x2f)
        {
            return '/';
        }

        if (octet == 0x3b)
        {
            return ';';
        }

        if (octet == 0x27)
        {
            return '\'';
        }

        if (octet == 0x3f)
        {
            return '?';
        }

        if (octet == 0x23)
        {
            return '#';
        }

        if (octet == 0x25)
        {
            return '%';
        }

        return '\0';
    }
}

internal class CatalogIndex
{
    internal List<XmlCatalogEntry> OrderedEntries;
    internal List<XmlCatalogEntry> ActiveGroups;

    internal CatalogIndex(XmlCatalogEntry[] entries)
    {
        Guard.NotNull(entries, "entries");
        OrderedEntries = new List<XmlCatalogEntry>();
        ActiveGroups = new List<XmlCatalogEntry>();
        AppendXmlCatalogEntries(entries);
    }

    internal void AppendXmlCatalogEntries(XmlCatalogEntry[] entries)
    {
        int index = 0;
        while (index < entries.Length)
        {
            XmlCatalogEntry entry = entries[index];
            if (entry == null)
            {
                throw new ArgumentException("A catalog entry collection cannot contain null.", "entries");
            }

            if (entry.Kind == XmlCatalogEntryKind.Group)
            {
                if (ContainsActiveXmlCatalogGroup(entry))
                {
                    throw new FormatException("An XML Catalog group contains itself.");
                }

                ActiveGroups.Add(entry);
                if (entry.Entries != null)
                {
                    AppendXmlCatalogEntries(entry.Entries);
                }

                ActiveGroups.RemoveAt(ActiveGroups.Count - 1);
            }
            else
            {
                OrderedEntries.Add(entry);
            }

            index = index + 1;
        }
    }

    internal bool ContainsActiveXmlCatalogGroup(XmlCatalogEntry candidate)
    {
        int index = 0;
        while (index < ActiveGroups.Count)
        {
            if (ActiveGroups[index] == candidate)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

internal class CatalogCycleScope : IDisposable
{
    internal CatalogMatcher Matcher;
    internal string CatalogKey;
    internal bool Active;

    internal CatalogCycleScope(CatalogMatcher matcher, string catalogKey)
    {
        Matcher = matcher;
        CatalogKey = catalogKey;
        Active = true;
    }

    public void Dispose()
    {
        if (Active)
        {
            Matcher.CompleteXmlCatalog(CatalogKey);
            Active = false;
        }
    }
}

internal class CatalogMatcher
{
    internal CatalogIndex RootIndex;
    internal ICatalogEntrySource? EntrySource;
    internal bool CacheEnabled;
    internal Dictionary<string, XmlCatalogEntry[]> CatalogCache;
    internal Dictionary<string, bool> MissingCatalogCache;
    internal Dictionary<string, string> MatchCache;
    internal Dictionary<string, bool> MissCache;
    internal Dictionary<string, bool> ActiveCatalogs;

    internal CatalogMatcher(XmlCatalogEntry[] entries, ICatalogEntrySource? entrySource, bool cacheEnabled)
    {
        RootIndex = new CatalogIndex(entries);
        EntrySource = entrySource;
        CacheEnabled = cacheEnabled;
        CatalogCache = new Dictionary<string, XmlCatalogEntry[]>(StringComparer.Ordinal);
        MissingCatalogCache = new Dictionary<string, bool>(StringComparer.Ordinal);
        MatchCache = new Dictionary<string, string>(StringComparer.Ordinal);
        MissCache = new Dictionary<string, bool>(StringComparer.Ordinal);
        ActiveCatalogs = new Dictionary<string, bool>(StringComparer.Ordinal);
    }

    internal string? ResolveExternalIdentifier(string? publicIdentifier, string? systemIdentifier)
    {
        string? normalizedPublic = publicIdentifier == null ? null : CatalogIdentifiers.NormalizeXmlPublicIdentifierInput(publicIdentifier);
        string? normalizedSystem = systemIdentifier == null ? null : CatalogIdentifiers.NormalizeXmlCatalogIdentifier(systemIdentifier);
        if (systemIdentifier != null && CatalogIdentifiers.IsXmlPublicIdentifierUrn(systemIdentifier))
        {
            string systemPublic = CatalogIdentifiers.NormalizeXmlPublicIdentifierInput(systemIdentifier);
            if (normalizedPublic != null && normalizedPublic != systemPublic)
            {
                throw new FormatException("The public identifier and public identifier URN do not identify the same value.");
            }

            normalizedPublic = systemPublic;
            normalizedSystem = null;
        }

        string cacheKey = "external|" + CacheIdentifier(normalizedPublic) + "|" + CacheIdentifier(normalizedSystem);
        string? cached = ReadCachedXmlCatalogMatch(cacheKey);
        if (cached != null || MissCache.ContainsKey(cacheKey))
        {
            return cached;
        }

        string? resolved = MatchExternalCatalog(RootIndex, normalizedPublic, normalizedSystem);
        CacheXmlCatalogMatch(cacheKey, resolved);
        return resolved;
    }

    internal string? ResolveUriIdentifier(string uri)
    {
        Guard.NotNull(uri, "uri");
        if (CatalogIdentifiers.IsXmlPublicIdentifierUrn(uri))
        {
            return ResolveExternalIdentifier(uri, null);
        }

        string normalized = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(uri);
        string cacheKey = "uri|" + CacheIdentifier(normalized);
        string? cached = ReadCachedXmlCatalogMatch(cacheKey);
        if (cached != null || MissCache.ContainsKey(cacheKey))
        {
            return cached;
        }

        string? resolved = MatchUriCatalog(RootIndex, normalized);
        CacheXmlCatalogMatch(cacheKey, resolved);
        return resolved;
    }

    internal string? MatchExternalCatalog(CatalogIndex catalog, string? publicIdentifier, string? systemIdentifier)
    {
        string? resolved;
        if (systemIdentifier != null)
        {
            resolved = FindExactSystemMatch(catalog, systemIdentifier);
            if (resolved != null)
            {
                return resolved;
            }

            resolved = FindRewriteSystemMatch(catalog, systemIdentifier);
            if (resolved != null)
            {
                return resolved;
            }

            resolved = FindSystemSuffixMatch(catalog, systemIdentifier);
            if (resolved != null)
            {
                return resolved;
            }

            List<XmlCatalogEntry> systemDelegates = CollectSystemDelegates(catalog, systemIdentifier);
            if (systemDelegates.Count > 0)
            {
                return ResolveExternalDelegates(systemDelegates, null, systemIdentifier);
            }
        }

        if (publicIdentifier != null)
        {
            resolved = FindExactPublicMatch(catalog, publicIdentifier, systemIdentifier != null);
            if (resolved != null)
            {
                return resolved;
            }

            List<XmlCatalogEntry> publicDelegates = CollectPublicDelegates(catalog, publicIdentifier, systemIdentifier != null);
            if (publicDelegates.Count > 0)
            {
                return ResolveExternalDelegates(publicDelegates, publicIdentifier, null);
            }
        }

        return ResolveExternalNextCatalogs(catalog, publicIdentifier, systemIdentifier);
    }

    internal string? MatchUriCatalog(CatalogIndex catalog, string uri)
    {
        string? resolved = FindExactUriMatch(catalog, uri);
        if (resolved != null)
        {
            return resolved;
        }

        resolved = FindRewriteUriMatch(catalog, uri);
        if (resolved != null)
        {
            return resolved;
        }

        resolved = FindUriSuffixMatch(catalog, uri);
        if (resolved != null)
        {
            return resolved;
        }

        List<XmlCatalogEntry> delegates = CollectUriDelegates(catalog, uri);
        if (delegates.Count > 0)
        {
            return ResolveUriDelegates(delegates, uri);
        }

        return ResolveUriNextCatalogs(catalog, uri);
    }

    internal string? FindExactSystemMatch(CatalogIndex catalog, string systemIdentifier)
    {
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.System && entry.SystemIdentifier != null && CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.SystemIdentifier) == systemIdentifier)
            {
                return RequireXmlCatalogTarget(entry);
            }

            index = index + 1;
        }

        return null;
    }

    internal string? FindRewriteSystemMatch(CatalogIndex catalog, string systemIdentifier)
    {
        XmlCatalogEntry? selected = null;
        int selectedLength = -1;
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.RewriteSystem && entry.SystemIdentifier != null)
            {
                string prefix = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.SystemIdentifier);
                if (systemIdentifier.StartsWith(prefix, StringComparison.Ordinal) && prefix.Length > selectedLength)
                {
                    selected = entry;
                    selectedLength = prefix.Length;
                }
            }

            index = index + 1;
        }

        if (selected == null)
        {
            return null;
        }

        string replacement = RequireXmlCatalogRewritePrefix(selected);
        return replacement + systemIdentifier.Substring(selectedLength);
    }

    internal string? FindSystemSuffixMatch(CatalogIndex catalog, string systemIdentifier)
    {
        XmlCatalogEntry? selected = null;
        int selectedLength = -1;
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.SystemSuffix && entry.Suffix != null)
            {
                string suffix = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.Suffix);
                if (systemIdentifier.EndsWith(suffix, StringComparison.Ordinal) && suffix.Length > selectedLength)
                {
                    selected = entry;
                    selectedLength = suffix.Length;
                }
            }

            index = index + 1;
        }

        return selected == null ? null : RequireXmlCatalogTarget(selected);
    }

    internal string? FindExactPublicMatch(CatalogIndex catalog, string publicIdentifier, bool systemIdentifierPresent)
    {
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.Public && (!systemIdentifierPresent || entry.PreferPublic) && entry.PublicIdentifier != null && CatalogIdentifiers.NormalizeXmlPublicIdentifier(entry.PublicIdentifier) == publicIdentifier)
            {
                return RequireXmlCatalogTarget(entry);
            }

            index = index + 1;
        }

        return null;
    }

    internal string? FindExactUriMatch(CatalogIndex catalog, string uri)
    {
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.Uri && entry.Uri != null && CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.Uri) == uri)
            {
                return RequireXmlCatalogTarget(entry);
            }

            index = index + 1;
        }

        return null;
    }

    internal string? FindRewriteUriMatch(CatalogIndex catalog, string uri)
    {
        XmlCatalogEntry? selected = null;
        int selectedLength = -1;
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.RewriteUri && entry.Uri != null)
            {
                string prefix = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.Uri);
                if (uri.StartsWith(prefix, StringComparison.Ordinal) && prefix.Length > selectedLength)
                {
                    selected = entry;
                    selectedLength = prefix.Length;
                }
            }

            index = index + 1;
        }

        if (selected == null)
        {
            return null;
        }

        string replacement = RequireXmlCatalogRewritePrefix(selected);
        return replacement + uri.Substring(selectedLength);
    }

    internal string? FindUriSuffixMatch(CatalogIndex catalog, string uri)
    {
        XmlCatalogEntry? selected = null;
        int selectedLength = -1;
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.UriSuffix && entry.Suffix != null)
            {
                string suffix = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.Suffix);
                if (uri.EndsWith(suffix, StringComparison.Ordinal) && suffix.Length > selectedLength)
                {
                    selected = entry;
                    selectedLength = suffix.Length;
                }
            }

            index = index + 1;
        }

        return selected == null ? null : RequireXmlCatalogTarget(selected);
    }

    internal List<XmlCatalogEntry> CollectSystemDelegates(CatalogIndex catalog, string systemIdentifier)
    {
        List<XmlCatalogEntry> delegates = new List<XmlCatalogEntry>();
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.DelegateSystem && entry.SystemIdentifier != null)
            {
                string prefix = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.SystemIdentifier);
                if (systemIdentifier.StartsWith(prefix, StringComparison.Ordinal))
                {
                    InsertXmlCatalogDelegate(delegates, entry, prefix.Length);
                }
            }

            index = index + 1;
        }

        return delegates;
    }

    internal List<XmlCatalogEntry> CollectPublicDelegates(CatalogIndex catalog, string publicIdentifier, bool systemIdentifierPresent)
    {
        List<XmlCatalogEntry> delegates = new List<XmlCatalogEntry>();
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.DelegatePublic && (!systemIdentifierPresent || entry.PreferPublic) && entry.PublicIdentifier != null)
            {
                string prefix = CatalogIdentifiers.NormalizeXmlPublicIdentifier(entry.PublicIdentifier);
                if (publicIdentifier.StartsWith(prefix, StringComparison.Ordinal))
                {
                    InsertXmlCatalogDelegate(delegates, entry, prefix.Length);
                }
            }

            index = index + 1;
        }

        return delegates;
    }

    internal List<XmlCatalogEntry> CollectUriDelegates(CatalogIndex catalog, string uri)
    {
        List<XmlCatalogEntry> delegates = new List<XmlCatalogEntry>();
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.DelegateUri && entry.Uri != null)
            {
                string prefix = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.Uri);
                if (uri.StartsWith(prefix, StringComparison.Ordinal))
                {
                    InsertXmlCatalogDelegate(delegates, entry, prefix.Length);
                }
            }

            index = index + 1;
        }

        return delegates;
    }

    internal void InsertXmlCatalogDelegate(List<XmlCatalogEntry> delegates, XmlCatalogEntry candidate, int candidateLength)
    {
        int position = 0;
        while (position < delegates.Count && XmlCatalogDelegatePrefixLength(delegates[position]) >= candidateLength)
        {
            position = position + 1;
        }

        delegates.Insert(position, candidate);
    }

    internal int XmlCatalogDelegatePrefixLength(XmlCatalogEntry entry)
    {
        if (entry.Kind == XmlCatalogEntryKind.DelegatePublic)
        {
            return entry.PublicIdentifier == null ? 0 : CatalogIdentifiers.NormalizeXmlPublicIdentifier(entry.PublicIdentifier).Length;
        }

        if (entry.Kind == XmlCatalogEntryKind.DelegateSystem)
        {
            return entry.SystemIdentifier == null ? 0 : CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.SystemIdentifier).Length;
        }

        return entry.Uri == null ? 0 : CatalogIdentifiers.NormalizeXmlCatalogIdentifier(entry.Uri).Length;
    }

    internal string? ResolveExternalDelegates(List<XmlCatalogEntry> delegates, string? publicIdentifier, string? systemIdentifier)
    {
        int index = 0;
        while (index < delegates.Count)
        {
            string catalogUri = RequireXmlCatalogUri(delegates[index]);
            string? resolved = MatchExternalReferencedCatalog(catalogUri, publicIdentifier, systemIdentifier);
            if (resolved != null)
            {
                return resolved;
            }

            index = index + 1;
        }

        return null;
    }

    internal string? ResolveUriDelegates(List<XmlCatalogEntry> delegates, string uri)
    {
        int index = 0;
        while (index < delegates.Count)
        {
            string catalogUri = RequireXmlCatalogUri(delegates[index]);
            string? resolved = MatchUriReferencedCatalog(catalogUri, uri);
            if (resolved != null)
            {
                return resolved;
            }

            index = index + 1;
        }

        return null;
    }

    internal string? ResolveExternalNextCatalogs(CatalogIndex catalog, string? publicIdentifier, string? systemIdentifier)
    {
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.NextCatalog)
            {
                string? resolved = MatchExternalReferencedCatalog(RequireXmlCatalogUri(entry), publicIdentifier, systemIdentifier);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal string? ResolveUriNextCatalogs(CatalogIndex catalog, string uri)
    {
        int index = 0;
        while (index < catalog.OrderedEntries.Count)
        {
            XmlCatalogEntry entry = catalog.OrderedEntries[index];
            if (entry.Kind == XmlCatalogEntryKind.NextCatalog)
            {
                string? resolved = MatchUriReferencedCatalog(RequireXmlCatalogUri(entry), uri);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal string? MatchExternalReferencedCatalog(string catalogUri, string? publicIdentifier, string? systemIdentifier)
    {
        string key = CanonicalXmlCatalogUri(catalogUri);
        using (CatalogCycleScope scope = BeginXmlCatalog(key))
        {
            XmlCatalogEntry[]? entries = LoadReferencedXmlCatalog(key);
            if (entries == null)
            {
                return null;
            }

            return MatchExternalCatalog(new CatalogIndex(entries), publicIdentifier, systemIdentifier);
        }
    }

    internal string? MatchUriReferencedCatalog(string catalogUri, string uri)
    {
        string key = CanonicalXmlCatalogUri(catalogUri);
        using (CatalogCycleScope scope = BeginXmlCatalog(key))
        {
            XmlCatalogEntry[]? entries = LoadReferencedXmlCatalog(key);
            if (entries == null)
            {
                return null;
            }

            return MatchUriCatalog(new CatalogIndex(entries), uri);
        }
    }

    internal CatalogCycleScope BeginXmlCatalog(string catalogKey)
    {
        if (ActiveCatalogs.ContainsKey(catalogKey))
        {
            throw CreateXmlCatalogCycle(catalogKey);
        }

        ActiveCatalogs.Add(catalogKey, true);
        return new CatalogCycleScope(this, catalogKey);
    }

    internal void CompleteXmlCatalog(string catalogKey)
    {
        ActiveCatalogs.Remove(catalogKey);
    }

    internal XmlCatalogEntry[]? LoadReferencedXmlCatalog(string catalogKey)
    {
        if (CacheEnabled && CatalogCache.ContainsKey(catalogKey))
        {
            return CatalogCache[catalogKey];
        }

        if (CacheEnabled && MissingCatalogCache.ContainsKey(catalogKey))
        {
            return null;
        }

        XmlCatalogEntry[]? entries = EntrySource == null ? null : EntrySource.LoadXmlCatalogEntries(catalogKey);
        if (CacheEnabled)
        {
            if (entries == null)
            {
                MissingCatalogCache.Add(catalogKey, true);
            }
            else
            {
                CatalogCache.Add(catalogKey, entries);
            }
        }

        return entries;
    }

    internal string RequireXmlCatalogTarget(XmlCatalogEntry entry)
    {
        if (entry.TargetUri == null || entry.TargetUri.Length == 0)
        {
            throw new FormatException("An exact or suffix XML Catalog entry requires a target URI.");
        }

        return ResolveXmlCatalogEntryReference(entry.TargetUri, entry.BaseUri);
    }

    internal string RequireXmlCatalogRewritePrefix(XmlCatalogEntry entry)
    {
        if (entry.RewritePrefix == null || entry.RewritePrefix.Length == 0)
        {
            throw new FormatException("A rewrite XML Catalog entry requires a replacement prefix.");
        }

        return ResolveXmlCatalogEntryReference(entry.RewritePrefix, entry.BaseUri);
    }

    internal string RequireXmlCatalogUri(XmlCatalogEntry entry)
    {
        if (entry.CatalogUri == null || entry.CatalogUri.Length == 0)
        {
            throw new FormatException("A delegate or nextCatalog XML Catalog entry requires a catalog URI.");
        }

        return ResolveXmlCatalogEntryReference(entry.CatalogUri, entry.BaseUri);
    }

    internal string ResolveXmlCatalogEntryReference(string reference, string? baseUri)
    {
        string normalizedReference = CatalogIdentifiers.NormalizeXmlCatalogIdentifier(reference);
        Uri candidate = UriTools.CreateReference(normalizedReference);
        if (candidate.IsAbsoluteUri)
        {
            return UriTools.CanonicalAbsoluteUri(candidate);
        }

        if (baseUri == null)
        {
            return normalizedReference;
        }

        return UriTools.CanonicalAbsoluteUri(UriTools.ResolveReference(UriTools.CreateAbsolute(baseUri), normalizedReference));
    }

    internal string CanonicalXmlCatalogUri(string catalogUri)
    {
        Uri value = UriTools.CreateAbsolute(catalogUri);
        if (UriTools.HasFragment(value.OriginalString))
        {
            throw new FormatException("An XML Catalog URI must not contain a fragment identifier.");
        }

        return UriTools.ResourceKey(value);
    }

    internal string? ReadCachedXmlCatalogMatch(string cacheKey)
    {
        if (CacheEnabled && MatchCache.ContainsKey(cacheKey))
        {
            return MatchCache[cacheKey];
        }

        return null;
    }

    internal void CacheXmlCatalogMatch(string cacheKey, string? resolvedUri)
    {
        if (!CacheEnabled)
        {
            return;
        }

        if (resolvedUri == null)
        {
            MissCache.Add(cacheKey, true);
        }
        else
        {
            MatchCache.Add(cacheKey, resolvedUri);
        }
    }

    internal string CacheIdentifier(string? value)
    {
        return value == null ? "null" : value.Length + ":" + value;
    }

    internal XmlResourceException CreateXmlCatalogCycle(string catalogUri)
    {
        SourceLocation location = new SourceLocation();
        location.SourceUri = catalogUri;
        location.ByteOffset = -1;
        location.Line = 1;
        location.Column = 1;
        SourceRange range = new SourceRange();
        range.Start = location;
        range.End = location;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.CatalogCycle;
        diagnostic.Category = XmlErrorCategory.Resource;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = "XML Catalog cycle detected for '" + catalogUri + "'.";
        diagnostic.Range = range;
        return new XmlResourceException(diagnostic.Message, diagnostic, location);
    }
}
