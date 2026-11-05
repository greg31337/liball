namespace sip;

/// <summary>Provides a thread-safe registry and dispatcher for application SIP extensions.</summary>
public class SipExtensionRegistry : ISipExtensionRegistry
{
    internal object Sync;
    internal ISipExtension[] Extensions = new ISipExtension[0];

    /// <summary>Initializes an empty extension registry.</summary>
    public SipExtensionRegistry()
    {
        Sync = new object();
    }

    /// <summary>Registers an extension instance unless that same instance is already registered.</summary>
    /// <param name="extension">The extension to register.</param>
    /// <returns>True when the extension was added.</returns>
    public bool RegisterExtension(ISipExtension extension)
    {
        if (extension == null) return false;
        lock (Sync)
        {
            int index = 0;
            while (index < Extensions.Length)
            {
                if (object.ReferenceEquals(Extensions[index], extension)) return false;
                index = index + 1;
            }

            ISipExtension[] updated = new ISipExtension[Extensions.Length + 1];
            System.Array.Copy(Extensions, updated, Extensions.Length);
            updated[Extensions.Length] = extension;
            Extensions = updated;
            return true;
        }
    }

    /// <summary>Removes a registered extension instance.</summary>
    /// <param name="extension">The extension to remove.</param>
    /// <returns>True when the extension was removed.</returns>
    public bool RemoveExtension(ISipExtension extension)
    {
        if (extension == null) return false;
        lock (Sync)
        {
            int found = -1;
            int index = 0;
            while (index < Extensions.Length)
            {
                if (object.ReferenceEquals(Extensions[index], extension)) found = index;
                index = index + 1;
            }
            if (found < 0) return false;

            ISipExtension[] updated = new ISipExtension[Extensions.Length - 1];
            if (found > 0) System.Array.Copy(Extensions, 0, updated, 0, found);
            if (found + 1 < Extensions.Length)
                System.Array.Copy(Extensions, found + 1, updated, found, Extensions.Length - found - 1);
            Extensions = updated;
            return true;
        }
    }

    /// <summary>Returns the distinct aggregate capabilities in registration order.</summary>
    /// <returns>A detached capability snapshot.</returns>
    public SipCapabilitySet GetRegisteredCapabilities()
    {
        return SipExtensionRegistryCore.AggregateCapabilities(GetExtensionSnapshot());
    }

    /// <summary>Tests whether a method token is advertised by a registered extension.</summary>
    /// <param name="method">The method token.</param>
    /// <returns>True when the method is advertised.</returns>
    public bool SupportsMethod(string method)
    {
        return SipExtensionRegistryCore.SupportsMethodToken(GetExtensionSnapshot(), method);
    }

    /// <summary>Tests whether a header field name is advertised by a registered extension.</summary>
    /// <param name="headerName">The header field name.</param>
    /// <returns>True when the header field is advertised.</returns>
    public bool SupportsHeader(string headerName)
    {
        return SipExtensionRegistryCore.SupportsHeaderName(GetExtensionSnapshot(), headerName);
    }

    /// <summary>Tests whether a URI scheme is advertised by a registered extension.</summary>
    /// <param name="scheme">The URI scheme token.</param>
    /// <returns>True when the URI scheme is advertised.</returns>
    public bool SupportsUriScheme(string scheme)
    {
        return SipExtensionRegistryCore.SupportsSchemeToken(GetExtensionSnapshot(), scheme);
    }

    /// <summary>Tests whether a media type is advertised by a registered extension.</summary>
    /// <param name="mediaType">The media type, without parameters.</param>
    /// <returns>True when the media type is advertised.</returns>
    public bool SupportsMediaType(string mediaType)
    {
        return SipExtensionRegistryCore.SupportsMediaTypeValue(GetExtensionSnapshot(), mediaType);
    }

    /// <summary>Tests whether an option tag is advertised by a registered extension.</summary>
    /// <param name="optionTag">The option tag.</param>
    /// <returns>True when the option tag is advertised.</returns>
    public bool SupportsOptionTag(string optionTag)
    {
        return SipExtensionRegistryCore.SupportsOptionTagToken(GetExtensionSnapshot(), optionTag);
    }

    /// <summary>Tests whether an event package is advertised by a registered extension.</summary>
    /// <param name="eventPackage">The event package token.</param>
    /// <returns>True when the event package is advertised.</returns>
    public bool SupportsEventPackage(string eventPackage)
    {
        return SipExtensionRegistryCore.SupportsEventPackageToken(GetExtensionSnapshot(), eventPackage);
    }

    /// <summary>Tests whether a transport content coding is advertised by a registered extension.</summary>
    /// <param name="contentCoding">The content-coding name.</param>
    /// <returns>True when the coding is advertised.</returns>
    public bool SupportsContentCoding(string contentCoding)
    {
        return SipExtensionRegistryCore.SupportsContentCodingToken(
            GetExtensionSnapshot(), contentCoding);
    }

    /// <summary>Dispatches a request to matching extensions in registration order.</summary>
    /// <param name="message">The request to process.</param>
    /// <returns>The first handled or failed result, or an unhandled result.</returns>
    public SipExtensionResult ProcessRegisteredRequest(SipMessage message)
    {
        return SipExtensionRegistryCore.ProcessRequest(GetExtensionSnapshot(), message);
    }

    internal ISipExtension[] GetExtensionSnapshot()
    {
        lock (Sync)
        {
            ISipExtension[] snapshot = new ISipExtension[Extensions.Length];
            System.Array.Copy(Extensions, snapshot, Extensions.Length);
            return snapshot;
        }
    }
}
