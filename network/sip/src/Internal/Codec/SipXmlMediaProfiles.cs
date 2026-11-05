namespace sip;

internal static class SipXmlMediaProfiles
{
    internal static SipXmlMediaProfile? FindProfile(SipMediaType? mediaType)
    {
        if (mediaType == null || !SipAscii.EqualsIgnoreCase(mediaType.Type, "application")) return null;
        SipXmlMediaProfile profile = new SipXmlMediaProfile();
        if (SipAscii.EqualsIgnoreCase(mediaType.Subtype, "pidf+xml"))
        {
            profile.RootName = "presence";
            profile.NamespaceUri = "urn:ietf:params:xml:ns:pidf";
            profile.RequiresEntity = true;
            return profile;
        }
        if (SipAscii.EqualsIgnoreCase(mediaType.Subtype, "pidf-diff+xml"))
        {
            profile.RootName = "pidf-diff";
            profile.AlternateRootName = "pidf-full";
            profile.NamespaceUri = "urn:ietf:params:xml:ns:pidf-diff";
            profile.AllowsVersion = true;
            return profile;
        }
        if (SipAscii.EqualsIgnoreCase(mediaType.Subtype, "watcherinfo+xml"))
        {
            profile.RootName = "watcherinfo";
            profile.NamespaceUri = "urn:ietf:params:xml:ns:watcherinfo";
            profile.RequiresVersionState = true;
            return profile;
        }
        if (SipAscii.EqualsIgnoreCase(mediaType.Subtype, "dialog-info+xml"))
        {
            profile.RootName = "dialog-info";
            profile.NamespaceUri = "urn:ietf:params:xml:ns:dialog-info";
            profile.RequiresEntity = true;
            profile.RequiresVersionState = true;
            return profile;
        }
        if (SipAscii.EqualsIgnoreCase(mediaType.Subtype, "reginfo+xml"))
        {
            profile.RootName = "reginfo";
            profile.NamespaceUri = "urn:ietf:params:xml:ns:reginfo";
            profile.RequiresVersionState = true;
            return profile;
        }
        if (SipAscii.EqualsIgnoreCase(mediaType.Subtype, "im-iscomposing+xml"))
        {
            profile.RootName = "isComposing";
            profile.NamespaceUri = "urn:ietf:params:xml:ns:im-iscomposing";
            profile.RequiresStateChild = true;
            return profile;
        }
        if (SipAscii.EqualsIgnoreCase(mediaType.Subtype, "simple-message-summary"))
        {
            profile.RootName = "message-summary";
            profile.MessageSummary = true;
            return profile;
        }
        if (SipAscii.EqualsIgnoreCase(mediaType.Subtype, "resource-lists+xml"))
        {
            profile.RootName = "resource-lists";
            profile.NamespaceUri = "urn:ietf:params:xml:ns:resource-lists";
            return profile;
        }
        if (SipAscii.EqualsIgnoreCase(mediaType.Subtype, "rlmi+xml"))
        {
            profile.RootName = "list";
            profile.NamespaceUri = "urn:ietf:params:xml:ns:rlmi";
            profile.RequiresListMetadata = true;
            return profile;
        }
        return null;
    }

    internal static string? FindAttributeValue(SipXmlScanResult scan, string name)
    {
        int index = 0;
        while (index < scan.RootAttributeNames.Length)
        {
            if (scan.RootAttributeNames[index] == name) return scan.RootAttributeValues[index];
            index = index + 1;
        }
        return null;
    }

    internal static bool HasDirectChild(SipXmlScanResult scan, string name, string namespaceUri)
    {
        int index = 0;
        while (index < scan.DirectChildNames.Length)
        {
            if (scan.DirectChildNames[index] == name
                && scan.DirectChildNamespaces[index] == namespaceUri) return true;
            index = index + 1;
        }
        return false;
    }
}
