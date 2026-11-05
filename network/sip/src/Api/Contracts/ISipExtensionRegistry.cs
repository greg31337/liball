namespace sip;

/// <summary>Registers application extensions, aggregates their capabilities, and dispatches matching requests.</summary>
public interface ISipExtensionRegistry
{
    /// <summary>Registers an extension instance unless that same instance is already registered.</summary>
    /// <param name="extension">The extension to register.</param>
    /// <returns>True when the extension was added.</returns>
    bool RegisterExtension(ISipExtension extension);

    /// <summary>Removes a registered extension instance.</summary>
    /// <param name="extension">The extension to remove.</param>
    /// <returns>True when the extension was removed.</returns>
    bool RemoveExtension(ISipExtension extension);

    /// <summary>Returns the distinct aggregate capabilities in registration order.</summary>
    /// <returns>A detached capability snapshot.</returns>
    SipCapabilitySet GetRegisteredCapabilities();

    /// <summary>Tests whether a method token is advertised by a registered extension.</summary>
    /// <param name="method">The method token.</param>
    /// <returns>True when the method is advertised.</returns>
    bool SupportsMethod(string method);

    /// <summary>Tests whether a header field name is advertised by a registered extension.</summary>
    /// <param name="headerName">The header field name.</param>
    /// <returns>True when the header field is advertised.</returns>
    bool SupportsHeader(string headerName);

    /// <summary>Tests whether a URI scheme is advertised by a registered extension.</summary>
    /// <param name="scheme">The URI scheme token.</param>
    /// <returns>True when the URI scheme is advertised.</returns>
    bool SupportsUriScheme(string scheme);

    /// <summary>Tests whether a media type is advertised by a registered extension.</summary>
    /// <param name="mediaType">The media type, without parameters.</param>
    /// <returns>True when the media type is advertised.</returns>
    bool SupportsMediaType(string mediaType);

    /// <summary>Tests whether an option tag is advertised by a registered extension.</summary>
    /// <param name="optionTag">The option tag.</param>
    /// <returns>True when the option tag is advertised.</returns>
    bool SupportsOptionTag(string optionTag);

    /// <summary>Tests whether an event package is advertised by a registered extension.</summary>
    /// <param name="eventPackage">The event package token.</param>
    /// <returns>True when the event package is advertised.</returns>
    bool SupportsEventPackage(string eventPackage);

    /// <summary>Tests whether a transport content coding is advertised by a registered extension.</summary>
    /// <param name="contentCoding">The content-coding name.</param>
    /// <returns>True when the coding is advertised.</returns>
    bool SupportsContentCoding(string contentCoding);

    /// <summary>Dispatches a request to matching extensions in registration order.</summary>
    /// <param name="message">The request to process.</param>
    /// <returns>The first handled or failed result, or an unhandled result.</returns>
    SipExtensionResult ProcessRegisteredRequest(SipMessage message);
}
