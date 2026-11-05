namespace fake;
/// <summary>Defines the public operations exposed by <see cref="InternetProvider"/>.</summary>
public interface IInternetProvider
{
    /// <summary>
    /// Generates a lowercase DNS host label between five and twelve characters long.
    /// </summary>
    /// <returns>An ASCII letter, digit, and hyphen label without a leading or trailing hyphen.</returns>
    string DomainWord();
    /// <summary>
    /// Generates a lowercase DNS host label of an exact length.
    /// </summary>
    /// <param name="length">The length from 1 through 63 ASCII characters.</param>
    /// <returns>An ASCII letter, digit, and hyphen label without a leading or trailing hyphen.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length is outside DNS label or configured output limits.</exception>
    string DomainWordFromLength(int length);
    /// <summary>
    /// Selects a lowercase ASCII top-level domain from the bundled IANA root-zone snapshot.
    /// </summary>
    /// <remarks>Registry presence does not imply that a child domain is available, unowned, reachable, or safe to contact.</remarks>
    /// <returns>A top-level label without a leading dot.</returns>
    string DomainSuffix();
    /// <summary>
    /// Returns every lowercase ASCII top-level domain in the bundled IANA root-zone snapshot.
    /// </summary>
    /// <remarks>Punycode labels retain their canonical <c>xn--</c> form. Registry presence makes no allocation or safety claim.</remarks>
    /// <returns>A new array in ordinal order without leading dots.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured collection or output limit is too small.</exception>
    string[] DomainSuffixes();
    /// <summary>
    /// Returns the serial identifier of the bundled IANA root-zone snapshot.
    /// </summary>
    /// <returns>The source serial text recorded with the bundled data.</returns>
    string DomainSuffixVersion();
    /// <summary>
    /// Reports whether a label occurs in the bundled IANA root-zone snapshot.
    /// </summary>
    /// <remarks>Registry presence does not imply that a child domain is available, unowned, reachable, or safe to contact.</remarks>
    /// <param name="value">The case-insensitive top-level label without a leading dot.</param>
    /// <returns><c>true</c> when the label occurs in the bundled snapshot; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is empty or whitespace.</exception>
    bool IsDomainSuffix(string value);
    /// <summary>
    /// Selects one of the RFC 2606 documentation domains from resolved locale data.
    /// </summary>
    /// <returns>A reserved example domain suitable for fixtures and documentation.</returns>
    /// <exception cref="FakeException">Thrown when documentation-domain locale data is unavailable or malformed.</exception>
    string ExampleDomain();
    /// <summary>
    /// Generates a DNS domain name under the configured safety policy.
    /// </summary>
    /// <remarks>Reserved mode places the generated label beneath an RFC 2606 example domain. Other modes make no availability, ownership, allocation, reachability, or contact-safety claim.</remarks>
    /// <returns>A lowercase ASCII domain name no longer than 253 characters.</returns>
    /// <exception cref="FakeException">Thrown when reserved documentation-domain data is unavailable or malformed.</exception>
    string DomainName();
    /// <summary>
    /// Generates a multi-label DNS host name under the configured safety policy.
    /// </summary>
    /// <remarks>Reserved mode keeps the generated host beneath an RFC 2606 example domain. Other modes make no availability, ownership, allocation, reachability, or contact-safety claim.</remarks>
    /// <returns>A lowercase ASCII host name no longer than 253 characters.</returns>
    /// <exception cref="FakeException">Thrown when reserved documentation-domain data is unavailable or malformed.</exception>
    string HostName();
    /// <summary>
    /// Generates a Unicode internationalized domain name under the configured safety policy.
    /// </summary>
    /// <remarks>The result round-trips through the platform IDNA mapping and avoids mixing Latin, Greek, and Cyrillic within a label. Reserved mode keeps it beneath an RFC 2606 example domain.</remarks>
    /// <returns>A canonical Unicode domain name containing a non-ASCII label.</returns>
    /// <exception cref="FakeException">Thrown when reserved documentation-domain data is unavailable or malformed.</exception>
    string IdnDomainName();
    /// <summary>
    /// Generates the ASCII Punycode form of an internationalized domain name.
    /// </summary>
    /// <remarks>Reserved mode keeps the generated name beneath an RFC 2606 example domain.</remarks>
    /// <returns>A lowercase ASCII domain name containing an <c>xn--</c> label.</returns>
    /// <exception cref="FakeException">Thrown when reserved documentation-domain data is unavailable or malformed.</exception>
    string PunycodeDomainName();
    /// <summary>
    /// Converts a Unicode or ASCII domain name to canonical lowercase ASCII IDNA form.
    /// </summary>
    /// <remarks>Each label must round-trip canonically and may not mix Latin, Greek, and Cyrillic scripts.</remarks>
    /// <param name="domainName">The domain name, optionally ending in a root dot.</param>
    /// <returns>The canonical ASCII domain with Punycode labels and the same root-dot form.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the domain is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a domain or IDN label is malformed, noncanonical, or mixes restricted scripts.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a label, domain, or configured output limit is exceeded.</exception>
    string ToAsciiDomain(string domainName);
    /// <summary>
    /// Converts an ASCII or Punycode domain name to canonical NFC Unicode form.
    /// </summary>
    /// <remarks>Each label must round-trip canonically and may not mix Latin, Greek, and Cyrillic scripts.</remarks>
    /// <param name="domainName">The domain name, optionally ending in a root dot.</param>
    /// <returns>The canonical Unicode domain with the same root-dot form.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the domain is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a domain or IDN label is malformed, does not round-trip, or mixes restricted scripts.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a label, domain, or configured output limit is exceeded.</exception>
    string ToUnicodeDomain(string domainName);
    /// <summary>
    /// Percent-encodes component data as UTF-8 according to RFC 3986.
    /// </summary>
    /// <remarks>Only ASCII letters, digits, hyphen, period, underscore, and tilde remain unescaped. Reserved characters are encoded because this method treats them as data, not delimiters.</remarks>
    /// <param name="value">The component data to encode exactly once.</param>
    /// <returns>ASCII text using uppercase hexadecimal percent encodings.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value contains malformed UTF-16.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    string EncodeUriComponent(string value);
    /// <summary>
    /// Generates password-free URI user information without its trailing <c>@</c> delimiter.
    /// </summary>
    /// <remarks>The value is synthetic component data and never represents credentials or authorization.</remarks>
    /// <returns>An RFC 3986 percent-encoded user-information component.</returns>
    string UserInfo();
    /// <summary>
    /// Generates one RFC 3986 percent-encoded path segment.
    /// </summary>
    /// <returns>Segment data without a slash delimiter.</returns>
    string PathSegment();
    /// <summary>
    /// Generates an absolute path containing one through four segments.
    /// </summary>
    /// <returns>A percent-encoded path beginning with a slash.</returns>
    string Path();
    /// <summary>
    /// Generates an absolute path with an exact number of segments.
    /// </summary>
    /// <param name="segmentCount">The number of segments; zero produces the empty path component.</param>
    /// <returns>An empty string or a percent-encoded path beginning with a slash.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    string PathFromSegmentCount(int segmentCount);
    /// <summary>
    /// Generates a query component containing one through four key-value pairs.
    /// </summary>
    /// <returns>Percent-encoded pairs separated by <c>&amp;</c>, without a leading <c>?</c>.</returns>
    string Query();
    /// <summary>
    /// Generates a query component with an exact number of key-value pairs.
    /// </summary>
    /// <param name="parameterCount">The number of pairs; zero produces an empty component.</param>
    /// <returns>Percent-encoded pairs separated by <c>&amp;</c>, without a leading <c>?</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    string QueryFromParameterCount(int parameterCount);
    /// <summary>
    /// Generates a percent-encoded fragment component without its leading <c>#</c> delimiter.
    /// </summary>
    /// <returns>Inert synthetic fragment data.</returns>
    string Fragment();
    /// <summary>Generates an eight-character URL-path short code.</summary>
    /// <returns>An ASCII letter-and-digit code suitable for one path segment.</returns>
    string UrlShortCode();
    /// <summary>Generates a URL-path short code with an explicit length.</summary>
    /// <param name="length">The positive number of ASCII characters.</param>
    /// <returns>An ASCII letter-and-digit code suitable for one path segment.</returns>
    string UrlShortCodeFromLength(int length);
    /// <summary>
    /// Generates a reserved-safe HTTPS URL with path, query, and fragment components.
    /// </summary>
    /// <remarks>User information is omitted by default. Reserved safety mode keeps the host beneath an RFC 2606 example domain and no generated URL is claimed reachable.</remarks>
    /// <returns>An absolute URL string.</returns>
    string Url();
    /// <summary>
    /// Generates a reserved-safe HTTPS URL with explicit optional components.
    /// </summary>
    /// <param name="includeUserInfo">Whether to include password-free user information.</param>
    /// <param name="includePath">Whether to include a nonempty absolute path.</param>
    /// <param name="includeQuery">Whether to include a nonempty query.</param>
    /// <param name="includeFragment">Whether to include a nonempty fragment.</param>
    /// <returns>An absolute HTTPS URL string.</returns>
    string UrlFromIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment(bool includeUserInfo, bool includePath, bool includeQuery, bool includeFragment);
    /// <summary>
    /// Generates an authority-based URL with an explicit RFC 3986 scheme and optional components.
    /// </summary>
    /// <remarks>Reserved safety mode keeps the host beneath an RFC 2606 example domain. User information never contains a password.</remarks>
    /// <param name="scheme">The scheme beginning with an ASCII letter.</param>
    /// <param name="includeUserInfo">Whether to include password-free user information.</param>
    /// <param name="includePath">Whether to include a nonempty absolute path.</param>
    /// <param name="includeQuery">Whether to include a nonempty query.</param>
    /// <param name="includeFragment">Whether to include a nonempty fragment.</param>
    /// <returns>An absolute URL string using a canonical lowercase scheme.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the scheme is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the scheme is empty or malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds a configured limit.</exception>
    string UrlFromSchemeAndIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment(string scheme, bool includeUserInfo, bool includePath, bool includeQuery, bool includeFragment);
    /// <summary>
    /// Generates a reserved-safe absolute <see cref="System.Uri"/> with path, query, and fragment components.
    /// </summary>
    /// <returns>A new HTTPS URI without user information.</returns>
    System.Uri Uri();
    /// <summary>
    /// Generates a reserved-safe absolute <see cref="System.Uri"/> with explicit optional components.
    /// </summary>
    /// <param name="includeUserInfo">Whether to include password-free user information.</param>
    /// <param name="includePath">Whether to include a nonempty absolute path.</param>
    /// <param name="includeQuery">Whether to include a nonempty query.</param>
    /// <param name="includeFragment">Whether to include a nonempty fragment.</param>
    /// <returns>A new absolute HTTPS URI.</returns>
    System.Uri UriFromIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment(bool includeUserInfo, bool includePath, bool includeQuery, bool includeFragment);
    /// <summary>
    /// Generates an absolute <see cref="System.Uri"/> with an explicit RFC 3986 scheme and optional components.
    /// </summary>
    /// <param name="scheme">The scheme beginning with an ASCII letter.</param>
    /// <param name="includeUserInfo">Whether to include password-free user information.</param>
    /// <param name="includePath">Whether to include a nonempty absolute path.</param>
    /// <param name="includeQuery">Whether to include a nonempty query.</param>
    /// <param name="includeFragment">Whether to include a nonempty fragment.</param>
    /// <returns>A new absolute URI.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the scheme is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the scheme is empty or malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds a configured limit.</exception>
    System.Uri UriFromSchemeAndIncludeUserInfoAndIncludePathAndIncludeQueryAndIncludeFragment(string scheme, bool includeUserInfo, bool includePath, bool includeQuery, bool includeFragment);
    /// <summary>
    /// Returns a deterministic deliberately invalid URL fixture.
    /// </summary>
    /// <returns>A URL with one randomly selected documented syntax defect.</returns>
    string InvalidUrl();
    /// <summary>
    /// Returns a deliberately invalid URL fixture for a selected syntax defect.
    /// </summary>
    /// <remarks>The fixtures use reserved example hosts where a recognizable host is needed and are returned as inert strings only.</remarks>
    /// <param name="kind">The requested invalidity category.</param>
    /// <returns>A deliberately malformed URL string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unsupported or the configured output limit is too small.</exception>
    string InvalidUrlFromKind(UriInvalidKind kind);
    /// <summary>
    /// Generates a synthetic ASCII display name for mailbox formatting.
    /// </summary>
    /// <returns>A plain display name without surrounding quotes.</returns>
    string DisplayName();
    /// <summary>
    /// Generates a synthetic UTF-8 display name containing a non-ASCII character.
    /// </summary>
    /// <returns>A plain international display name without surrounding quotes.</returns>
    string InternationalDisplayName();
    /// <summary>
    /// Generates a typical lowercase RFC 5322 dot-atom local part.
    /// </summary>
    /// <returns>ASCII local-part text without a domain.</returns>
    string EmailLocalPart();
    /// <summary>
    /// Generates a selected valid ASCII email local-part case.
    /// </summary>
    /// <param name="kind">The requested dot-atom, quoted-string, or boundary case.</param>
    /// <returns>ASCII local-part text without a domain.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the case is unsupported or the output exceeds the configured limit.</exception>
    string EmailLocalPartFromKind(EmailLocalPartCase kind);
    /// <summary>
    /// Generates an ASCII email address under the configured safety policy.
    /// </summary>
    /// <remarks>Reserved mode uses RFC 2606 example domains. Other modes make no allocation, ownership, deliverability, or contact-safety claim.</remarks>
    /// <returns>An RFC 5322 dot-atom address.</returns>
    string Email();
    /// <summary>
    /// Generates an ASCII email address with a selected valid local-part case.
    /// </summary>
    /// <remarks>Reserved mode uses RFC 2606 example domains. Other modes make no allocation, ownership, deliverability, or contact-safety claim.</remarks>
    /// <param name="kind">The requested local-part case.</param>
    /// <returns>An RFC 5322 address with an ASCII DNS domain.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the case is unsupported or the output exceeds the configured limit.</exception>
    string EmailFromKind(EmailLocalPartCase kind);
    /// <summary>
    /// Generates an ASCII email address using an RFC 2606 example domain regardless of safety mode.
    /// </summary>
    /// <remarks>The result is reserved fixture data and is not claimed to identify a mailbox or accept delivery.</remarks>
    /// <returns>An RFC 5322 dot-atom address at an example domain.</returns>
    string ExampleEmail();
    /// <summary>
    /// Generates an ASCII example-domain address with a selected valid local-part case.
    /// </summary>
    /// <param name="kind">The requested local-part case.</param>
    /// <returns>An RFC 5322 address at an RFC 2606 example domain.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the case is unsupported or the output exceeds the configured limit.</exception>
    string ExampleEmailFromKind(EmailLocalPartCase kind);
    /// <summary>
    /// Generates an SMTPUTF8 email address with a non-ASCII local part and IDN domain.
    /// </summary>
    /// <remarks>Reserved mode keeps the domain beneath an RFC 2606 example domain. Transmission requires explicit SMTPUTF8 support and the result is never claimed deliverable.</remarks>
    /// <returns>A UTF-8 mailbox address without a display name.</returns>
    string InternationalEmail();
    /// <summary>
    /// Generates an SMTPUTF8 email address with a non-ASCII local part and IDN domain.
    /// </summary>
    /// <returns>The same form produced by <see cref="InternationalEmail"/>.</returns>
    string SmtpUtf8Email();
    /// <summary>
    /// Generates an RFC 5322 name-address mailbox with a quoted ASCII display name.
    /// </summary>
    /// <remarks>The email address follows the configured safety policy and is not claimed deliverable.</remarks>
    /// <returns>A display name followed by an address in angle brackets.</returns>
    string Mailbox();
    /// <summary>
    /// Generates an RFC 5322 name-address mailbox using a caller-supplied ASCII display name.
    /// </summary>
    /// <param name="displayName">The nonempty display name to quote and escape.</param>
    /// <returns>The quoted display name followed by a generated address in angle brackets.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the display name is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the display name is empty, non-ASCII, malformed Unicode, or contains control characters.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds the configured limit.</exception>
    string MailboxFromDisplayName(string displayName);
    /// <summary>
    /// Generates an SMTPUTF8 name-address mailbox with a quoted international display name.
    /// </summary>
    /// <remarks>Transmission requires explicit SMTPUTF8 support and the result is never claimed deliverable.</remarks>
    /// <returns>A UTF-8 display name followed by an international address in angle brackets.</returns>
    string InternationalMailbox();
    /// <summary>
    /// Generates an SMTPUTF8 name-address mailbox using a caller-supplied Unicode display name.
    /// </summary>
    /// <param name="displayName">The nonempty display name to quote and escape.</param>
    /// <returns>The quoted display name followed by a generated SMTPUTF8 address in angle brackets.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the display name is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the display name is empty, malformed Unicode, or contains control characters.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds the configured limit.</exception>
    string InternationalMailboxFromDisplayName(string displayName);
    /// <summary>
    /// Generates an SMTPUTF8 name-address mailbox with a quoted international display name.
    /// </summary>
    /// <returns>The same form produced by <see cref="InternationalMailbox()"/>.</returns>
    string SmtpUtf8Mailbox();
    /// <summary>Generates the Gravatar-compatible hash of a reserved example email.</summary>
    /// <returns>A 32-character lowercase hexadecimal MD5 hash.</returns>
    string GravatarHash();
    /// <summary>Returns the Gravatar-compatible normalized-email hash.</summary>
    /// <param name="email">The nonempty email text to normalize and hash.</param>
    /// <returns>A 32-character lowercase hexadecimal MD5 hash.</returns>
    string GravatarHashFromEmail(string email);
    /// <summary>Generates a strongly typed documentation IP address from either address family.</summary>
    /// <returns>An RFC documentation IPv4 or IPv6 address.</returns>
    System.Net.IPAddress IpAddress();
    /// <summary>Generates a strongly typed IP address from either family and a selected category.</summary>
    /// <param name="kind">The requested address category.</param>
    /// <returns>An IPv4 or IPv6 address from the selected category.</returns>
    System.Net.IPAddress IpAddressFromKind(NetworkAddressKind kind);
    /// <summary>Generates a documentation endpoint from either IP address family and a dynamic port.</summary>
    /// <returns>An RFC documentation address with a dynamic port.</returns>
    System.Net.IPEndPoint Endpoint();
    /// <summary>Generates an endpoint from either IP address family, a category, and a dynamic port.</summary>
    /// <param name="kind">The requested address category.</param>
    /// <returns>An address from either family with a dynamic port.</returns>
    System.Net.IPEndPoint EndpointFromKind(NetworkAddressKind kind);
    /// <summary>Generates a documentation endpoint from either IP address family and an explicit port.</summary>
    /// <param name="port">The port from zero through 65535.</param>
    /// <returns>An RFC documentation address with the requested port.</returns>
    System.Net.IPEndPoint EndpointFromPort(int port);
    /// <summary>Generates an endpoint from either IP address family and explicit category and port.</summary>
    /// <param name="kind">The requested address category.</param>
    /// <param name="port">The port from zero through 65535.</param>
    /// <returns>An address from either family with the requested port.</returns>
    System.Net.IPEndPoint EndpointFromKindAndPort(NetworkAddressKind kind, int port);
    /// <summary>
    /// Generates an IPv4 address from an RFC 5737 documentation block.
    /// </summary>
    /// <returns>A dotted-decimal documentation address suitable for fixtures.</returns>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string Ipv4();
    /// <summary>
    /// Generates a dotted-decimal IPv4 address from a selected category.
    /// </summary>
    /// <remarks>Public output is only public-unicast-shaped and makes no allocation, ownership, routing, or contact-safety claim.</remarks>
    /// <param name="kind">The requested address category.</param>
    /// <returns>A dotted-decimal IPv4 address.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unsupported or the configured output limit is too small.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string Ipv4FromKind(NetworkAddressKind kind);
    /// <summary>
    /// Generates a strongly typed IPv4 address from an RFC 5737 documentation block.
    /// </summary>
    /// <returns>An IPv4 <see cref="System.Net.IPAddress"/> suitable for fixtures.</returns>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPAddress Ipv4Address();
    /// <summary>
    /// Generates a strongly typed IPv4 address from a selected category.
    /// </summary>
    /// <remarks>Public output is only public-unicast-shaped and makes no allocation, ownership, routing, or contact-safety claim.</remarks>
    /// <param name="kind">The requested address category.</param>
    /// <returns>An IPv4 <see cref="System.Net.IPAddress"/>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unsupported or the configured output limit is too small.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPAddress Ipv4AddressFromKind(NetworkAddressKind kind);
    /// <summary>
    /// Generates a contiguous IPv4 mask with a prefix length from eight through 31.
    /// </summary>
    /// <returns>A dotted-decimal IPv4 network mask.</returns>
    string Ipv4Mask();
    /// <summary>
    /// Returns the contiguous IPv4 mask for an explicit prefix length.
    /// </summary>
    /// <param name="prefixLength">The prefix length from zero through 32.</param>
    /// <returns>A dotted-decimal IPv4 network mask.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the prefix or configured output limit is invalid.</exception>
    string Ipv4MaskFromPrefixLength(int prefixLength);
    /// <summary>
    /// Generates an RFC 5737 documentation network in CIDR notation.
    /// </summary>
    /// <returns>An aligned documentation network with a <c>/24</c> prefix.</returns>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string Cidr();
    /// <summary>
    /// Generates a <c>/24</c> IPv4 network from a selected category in CIDR notation.
    /// </summary>
    /// <param name="kind">The requested address category.</param>
    /// <returns>An aligned IPv4 network with a <c>/24</c> prefix.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string CidrFromKind(NetworkAddressKind kind);
    /// <summary>
    /// Generates an IPv4 network from a selected category and prefix in CIDR notation.
    /// </summary>
    /// <param name="kind">The requested address category.</param>
    /// <param name="prefixLength">A prefix that fits inside the selected category block.</param>
    /// <returns>An aligned IPv4 network and prefix.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category, prefix, or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string CidrFromKindAndPrefixLength(NetworkAddressKind kind, int prefixLength);
    /// <summary>
    /// Generates an RFC 5737 documentation network in IPv4 CIDR notation.
    /// </summary>
    /// <returns>The same form produced by <see cref="Cidr()"/>.</returns>
    string Ipv4Cidr();
    /// <summary>
    /// Generates a <c>/24</c> IPv4 network from a selected category in CIDR notation.
    /// </summary>
    /// <param name="kind">The requested address category.</param>
    /// <returns>The same form produced by <see cref="CidrFromKind(NetworkAddressKind)"/>.</returns>
    string Ipv4CidrFromKind(NetworkAddressKind kind);
    /// <summary>
    /// Generates an IPv4 network from a selected category and prefix in CIDR notation.
    /// </summary>
    /// <param name="kind">The requested address category.</param>
    /// <param name="prefixLength">A prefix that fits inside the selected category block.</param>
    /// <returns>The same form produced by <see cref="CidrFromKindAndPrefixLength(NetworkAddressKind, int)"/>.</returns>
    string Ipv4CidrFromKindAndPrefixLength(NetworkAddressKind kind, int prefixLength);
    /// <summary>
    /// Generates an inclusive RFC 5737 documentation network range.
    /// </summary>
    /// <returns>A documentation <c>/24</c> range.</returns>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    Ipv4Range Ipv4Range();
    /// <summary>
    /// Generates an inclusive <c>/24</c> IPv4 network range from a selected category.
    /// </summary>
    /// <param name="kind">The requested address category.</param>
    /// <returns>An aligned <c>/24</c> range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    Ipv4Range Ipv4RangeFromKind(NetworkAddressKind kind);
    /// <summary>
    /// Generates an inclusive IPv4 network range from a selected category and prefix.
    /// </summary>
    /// <param name="kind">The requested address category.</param>
    /// <param name="prefixLength">A prefix that fits inside the selected category block.</param>
    /// <returns>Aligned network, final address, netmask, CIDR, and address-count metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category, prefix, or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    Ipv4Range Ipv4RangeFromKindAndPrefixLength(NetworkAddressKind kind, int prefixLength);
    /// <summary>
    /// Generates an IPv4 endpoint using an RFC 5737 documentation address and a dynamic port.
    /// </summary>
    /// <returns>A documentation address with a port from 49152 through 65535.</returns>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPEndPoint Ipv4Endpoint();
    /// <summary>
    /// Generates an IPv4 endpoint from a selected category and a dynamic port.
    /// </summary>
    /// <remarks>Public output is only public-unicast-shaped and makes no allocation, ownership, routing, or contact-safety claim.</remarks>
    /// <param name="kind">The requested address category.</param>
    /// <returns>An address with a port from 49152 through 65535.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPEndPoint Ipv4EndpointFromKind(NetworkAddressKind kind);
    /// <summary>
    /// Generates an IPv4 endpoint using an RFC 5737 documentation address and an explicit port.
    /// </summary>
    /// <param name="port">The port from zero through 65535.</param>
    /// <returns>A documentation address with the requested port.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the port or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPEndPoint Ipv4EndpointFromPort(int port);
    /// <summary>
    /// Generates an IPv4 endpoint from a selected category and explicit port.
    /// </summary>
    /// <remarks>Public output is only public-unicast-shaped and makes no allocation, ownership, routing, or contact-safety claim.</remarks>
    /// <param name="kind">The requested address category.</param>
    /// <param name="port">The port from zero through 65535.</param>
    /// <returns>An address from the selected category with the requested port.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category, port, or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPEndPoint Ipv4EndpointFromKindAndPort(NetworkAddressKind kind, int port);
    /// <summary>
    /// Generates a canonical IPv6 address from the RFC 3849 documentation prefix.
    /// </summary>
    /// <returns>A lowercase, RFC 5952-compressed documentation address.</returns>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string Ipv6();
    /// <summary>
    /// Generates a canonical IPv6 address from a selected category.
    /// </summary>
    /// <remarks>Public output is only public-unicast-shaped and makes no allocation, ownership, routing, or contact-safety claim.</remarks>
    /// <param name="kind">The requested address category.</param>
    /// <returns>A lowercase IPv6 address using RFC 5952 compression.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string Ipv6FromKind(NetworkAddressKind kind);
    /// <summary>
    /// Generates a strongly typed IPv6 address from the RFC 3849 documentation prefix.
    /// </summary>
    /// <returns>An IPv6 <see cref="System.Net.IPAddress"/> suitable for fixtures.</returns>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPAddress Ipv6Address();
    /// <summary>
    /// Generates a strongly typed IPv6 address from a selected category.
    /// </summary>
    /// <remarks>Public output is only public-unicast-shaped and makes no allocation, ownership, routing, or contact-safety claim.</remarks>
    /// <param name="kind">The requested address category.</param>
    /// <returns>An IPv6 <see cref="System.Net.IPAddress"/>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPAddress Ipv6AddressFromKind(NetworkAddressKind kind);
    /// <summary>Generates a contiguous IPv6 network mask.</summary>
    /// <returns>A canonically formatted IPv6 network mask.</returns>
    string Ipv6Mask();
    /// <summary>Returns the contiguous IPv6 mask for an explicit prefix length.</summary>
    /// <param name="prefixLength">The prefix length from zero through 128.</param>
    /// <returns>A canonically formatted IPv6 network mask.</returns>
    string Ipv6MaskFromPrefixLength(int prefixLength);
    /// <summary>
    /// Converts IPv6 text to canonical RFC 5952 presentation form.
    /// </summary>
    /// <param name="address">An IPv6 address without endpoint brackets or a scope identifier.</param>
    /// <returns>Lowercase IPv6 text with leading zeroes removed and the first longest zero run compressed.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the address is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the address is empty, is not IPv6, or contains endpoint or scope syntax.</exception>
    /// <exception cref="System.FormatException">Thrown when the address text is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the output exceeds the configured limit.</exception>
    string CanonicalIpv6(string address);
    /// <summary>
    /// Generates an RFC 3849 documentation network in IPv6 CIDR notation.
    /// </summary>
    /// <returns>An aligned documentation network with a <c>/64</c> prefix.</returns>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string Ipv6Cidr();
    /// <summary>
    /// Generates an IPv6 network from a selected category using its customary prefix length.
    /// </summary>
    /// <param name="kind">The requested address category.</param>
    /// <returns>A <c>/64</c> network, or the loopback address with a <c>/128</c> prefix.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string Ipv6CidrFromKind(NetworkAddressKind kind);
    /// <summary>
    /// Generates an IPv6 network from a selected category and prefix in CIDR notation.
    /// </summary>
    /// <param name="kind">The requested address category.</param>
    /// <param name="prefixLength">A prefix that fits inside the selected category block.</param>
    /// <returns>An aligned, canonically formatted IPv6 network and prefix.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category, prefix, or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    string Ipv6CidrFromKindAndPrefixLength(NetworkAddressKind kind, int prefixLength);
    /// <summary>Generates an inclusive RFC 3849 documentation IPv6 range.</summary>
    /// <returns>A documentation <c>/64</c> range.</returns>
    Ipv6Range Ipv6Range();
    /// <summary>Generates an inclusive IPv6 range using a category's customary prefix.</summary>
    /// <param name="kind">The requested address category.</param>
    /// <returns>A category-appropriate inclusive range.</returns>
    Ipv6Range Ipv6RangeFromKind(NetworkAddressKind kind);
    /// <summary>Generates an inclusive IPv6 range from an explicit category and prefix.</summary>
    /// <param name="kind">The requested address category.</param>
    /// <param name="prefixLength">A prefix that fits inside the selected category block.</param>
    /// <returns>Aligned network, final address, netmask, CIDR, and address-count metadata.</returns>
    Ipv6Range Ipv6RangeFromKindAndPrefixLength(NetworkAddressKind kind, int prefixLength);
    /// <summary>
    /// Generates an IPv6 endpoint using an RFC 3849 documentation address and a dynamic port.
    /// </summary>
    /// <returns>A documentation address with a port from 49152 through 65535.</returns>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPEndPoint Ipv6Endpoint();
    /// <summary>
    /// Generates an IPv6 endpoint from a selected category and a dynamic port.
    /// </summary>
    /// <remarks>Public output is only public-unicast-shaped and makes no allocation, ownership, routing, or contact-safety claim.</remarks>
    /// <param name="kind">The requested address category.</param>
    /// <returns>An address with a port from 49152 through 65535.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPEndPoint Ipv6EndpointFromKind(NetworkAddressKind kind);
    /// <summary>
    /// Generates an IPv6 endpoint using an RFC 3849 documentation address and an explicit port.
    /// </summary>
    /// <param name="port">The port from zero through 65535.</param>
    /// <returns>A documentation address with the requested port.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the port or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPEndPoint Ipv6EndpointFromPort(int port);
    /// <summary>
    /// Generates an IPv6 endpoint from a selected category and explicit port.
    /// </summary>
    /// <remarks>Public output is only public-unicast-shaped and makes no allocation, ownership, routing, or contact-safety claim.</remarks>
    /// <param name="kind">The requested address category.</param>
    /// <param name="port">The port from zero through 65535.</param>
    /// <returns>An address from the selected category with the requested port.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category, port, or configured output limit is invalid.</exception>
    /// <exception cref="FakeException">Thrown when documentation-prefix locale data is unavailable or malformed.</exception>
    System.Net.IPEndPoint Ipv6EndpointFromKindAndPort(NetworkAddressKind kind, int port);
    /// <summary>
    /// Generates a locally administered unicast 48-bit MAC address.
    /// </summary>
    /// <returns>A lowercase, colon-separated MAC address.</returns>
    string Mac();
    /// <summary>
    /// Generates a 48-bit MAC address with explicit individual/group and universal/local bits.
    /// </summary>
    /// <remarks>A universally administered shape makes no vendor, registry-assignment, ownership, or uniqueness claim.</remarks>
    /// <param name="multicast">Whether to set the individual/group bit for a multicast group address.</param>
    /// <param name="locallyAdministered">Whether to set the universal/local bit for local administration.</param>
    /// <returns>A lowercase, colon-separated MAC address.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    string MacFromMulticastAndLocallyAdministered(bool multicast, bool locallyAdministered);
    /// <summary>
    /// Generates a locally administered unicast physical address.
    /// </summary>
    /// <returns>A six-byte platform physical-address object.</returns>
    System.Net.NetworkInformation.PhysicalAddress MacAddress();
    /// <summary>
    /// Generates a physical address with explicit individual/group and universal/local bits.
    /// </summary>
    /// <remarks>A universally administered shape makes no vendor, registry-assignment, ownership, or uniqueness claim.</remarks>
    /// <param name="multicast">Whether to set the individual/group bit for a multicast group address.</param>
    /// <param name="locallyAdministered">Whether to set the universal/local bit for local administration.</param>
    /// <returns>A six-byte platform physical-address object.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    System.Net.NetworkInformation.PhysicalAddress MacAddressFromMulticastAndLocallyAdministered(bool multicast, bool locallyAdministered);
    /// <summary>
    /// Generates a synthetic network-interface record with a locally administered unicast address.
    /// </summary>
    /// <returns>A wired or wireless interface record with correlated MAC metadata.</returns>
    NetworkInterfaceRecord NetworkInterface();
    /// <summary>
    /// Generates a synthetic network-interface record with explicit MAC address flags.
    /// </summary>
    /// <remarks>A universally administered shape makes no vendor, registry-assignment, ownership, or uniqueness claim.</remarks>
    /// <param name="multicast">Whether to set the individual/group bit for a multicast group address.</param>
    /// <param name="locallyAdministered">Whether to set the universal/local bit for local administration.</param>
    /// <returns>A wired or wireless interface record with correlated text, physical address, and flags.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is too small.</exception>
    NetworkInterfaceRecord NetworkInterfaceFromMulticastAndLocallyAdministered(bool multicast, bool locallyAdministered);
    /// <summary>
    /// Generates a port from the IANA dynamic or private range.
    /// </summary>
    /// <returns>A port from 49152 through 65535.</returns>
    int Port();
    /// <summary>
    /// Generates a port from a selected IANA port-number range.
    /// </summary>
    /// <param name="kind">The system, registered, or dynamic range.</param>
    /// <returns>A port inside the selected inclusive range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the range is unsupported.</exception>
    int PortFromKind(PortKind kind);
    /// <summary>
    /// Generates a port from the system or well-known range.
    /// </summary>
    /// <returns>A port from 0 through 1023.</returns>
    int SystemPort();
    /// <summary>
    /// Generates a port from the user or registered range.
    /// </summary>
    /// <returns>A port from 1024 through 49151.</returns>
    int RegisteredPort();
    /// <summary>
    /// Generates a port from the dynamic or private range.
    /// </summary>
    /// <returns>A port from 49152 through 65535.</returns>
    int DynamicPort();
    /// <summary>
    /// Selects a transport protocol represented by assigned records in the bundled IANA registry.
    /// </summary>
    /// <returns>One of <c>dccp</c>, <c>sctp</c>, <c>tcp</c>, or <c>udp</c>.</returns>
    string Protocol();
    /// <summary>
    /// Selects a transport protocol represented by assigned records in the bundled IANA registry.
    /// </summary>
    /// <returns>The same form produced by <see cref="Protocol()"/>.</returns>
    string TransportProtocol();
    /// <summary>
    /// Returns the supported transport protocols in ordinal order.
    /// </summary>
    /// <returns>A new array containing <c>dccp</c>, <c>sctp</c>, <c>tcp</c>, and <c>udp</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured collection or output limits are too small.</exception>
    string[] Protocols();
    /// <summary>
    /// Selects an assigned service name from the bundled IANA registry.
    /// </summary>
    /// <remarks>Registration does not imply that a service is running, reachable, safe, authenticated, or owned by any party.</remarks>
    /// <returns>A lowercase registered service name having a single assigned port.</returns>
    string Service();
    /// <summary>
    /// Selects an assigned service name for a transport from the bundled IANA registry.
    /// </summary>
    /// <remarks>Registration does not imply that a service is running, reachable, safe, authenticated, or owned by any party.</remarks>
    /// <param name="transport">One of <c>dccp</c>, <c>sctp</c>, <c>tcp</c>, or <c>udp</c>, case-insensitively.</param>
    /// <returns>A lowercase registered service name having a single assigned port.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the transport is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the transport is empty or unsupported.</exception>
    string ServiceFromTransport(string transport);
    /// <summary>
    /// Selects a correlated assigned service record from the bundled IANA registry.
    /// </summary>
    /// <remarks>Registration does not imply that a service is running, reachable, safe, authenticated, or owned by any party.</remarks>
    /// <returns>A service name, port, transport, range category, and registry version.</returns>
    NetworkServiceRecord ServiceRecord();
    /// <summary>
    /// Selects a correlated assigned service record for a transport from the bundled IANA registry.
    /// </summary>
    /// <param name="transport">One of <c>dccp</c>, <c>sctp</c>, <c>tcp</c>, or <c>udp</c>, case-insensitively.</param>
    /// <returns>A service name, port, transport, range category, and registry version.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the transport is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the transport is empty or unsupported.</exception>
    NetworkServiceRecord ServiceRecordFromTransport(string transport);
    /// <summary>
    /// Looks up an assigned service and transport in the bundled IANA registry.
    /// </summary>
    /// <param name="service">The case-insensitive registered service name.</param>
    /// <param name="transport">One of <c>dccp</c>, <c>sctp</c>, <c>tcp</c>, or <c>udp</c>, case-insensitively.</param>
    /// <returns>The first matching single-port assignment in source order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an argument is empty, unsupported, or unassigned.</exception>
    NetworkServiceRecord ServiceRecordFromServiceAndTransport(string service, string transport);
    /// <summary>
    /// Returns the source-version text of the bundled IANA service registry.
    /// </summary>
    /// <returns>The snapshot version recorded with the embedded data.</returns>
    string ServiceVersion();
    /// <summary>
    /// Selects a standard HTTP request method.
    /// </summary>
    /// <returns>An uppercase core HTTP method token.</returns>
    string HttpMethod();
    /// <summary>
    /// Returns the supported core HTTP request methods.
    /// </summary>
    /// <returns>A new array of uppercase HTTP method tokens.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured collection or output limits are too small.</exception>
    string[] HttpMethods();
    /// <summary>
    /// Selects a standard HTTP status code.
    /// </summary>
    /// <returns>A registered status code from 100 through 511.</returns>
    int HttpStatus();
    /// <summary>
    /// Selects a correlated standard HTTP status record.
    /// </summary>
    /// <returns>A code, reason phrase, status class, error flag, and combined text.</returns>
    HttpStatusRecord HttpStatusRecord();
    /// <summary>
    /// Returns the correlated record for a supported HTTP status code.
    /// </summary>
    /// <param name="statusCode">A code in the supported standard status set.</param>
    /// <returns>The code, reason phrase, status class, error flag, and combined text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the code is unsupported or output exceeds the configured limit.</exception>
    HttpStatusRecord HttpStatusRecordFromStatusCode(int statusCode);
    /// <summary>
    /// Returns the conventional reason phrase for a supported HTTP status code.
    /// </summary>
    /// <param name="statusCode">A code in the supported standard status set.</param>
    /// <returns>The conventional English reason phrase.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the code is unsupported or output exceeds the configured limit.</exception>
    string HttpReasonPhrase(int statusCode);
    /// <summary>
    /// Selects a standard HTTP field name.
    /// </summary>
    /// <returns>A conventionally capitalized HTTP header name.</returns>
    string HttpHeader();
    /// <summary>
    /// Returns the supported standard HTTP field names.
    /// </summary>
    /// <returns>A new array of conventionally capitalized header names.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured collection or output limits are too small.</exception>
    string[] HttpHeaders();
    /// <summary>Generates a supported HTTP field with a correlated synthetic value.</summary>
    /// <returns>A separated field name, value, and combined text record.</returns>
    HttpHeaderRecord HttpHeaderRecord();
    /// <summary>Returns a supported HTTP field with its synthetic value fixture.</summary>
    /// <param name="name">A case-insensitive name returned by <c>HttpHeaders</c>.</param>
    /// <returns>A canonical field name and correlated synthetic value.</returns>
    HttpHeaderRecord HttpHeaderRecordFromName(string name);
    /// <summary>Creates an inert HTTP field record from an explicit safe name and value.</summary>
    /// <param name="name">A nonempty ASCII HTTP field-name token.</param>
    /// <param name="value">Visible ASCII field content without line breaks.</param>
    /// <returns>A separated field name, value, and combined text record.</returns>
    HttpHeaderRecord HttpHeaderRecordFromNameAndValue(string name, string value);
    /// <summary>
    /// Selects a syntactically valid media type from the bundled IANA registry.
    /// </summary>
    /// <remarks>Registration does not imply that content is safe, trusted, correctly encoded, or suitable for execution.</remarks>
    /// <returns>A registered type and subtype separated by a slash.</returns>
    string MimeType();
    /// <summary>
    /// Selects a registered media type beneath a top-level type.
    /// </summary>
    /// <remarks>Registration does not imply that content is safe, trusted, correctly encoded, or suitable for execution.</remarks>
    /// <param name="topLevelType">The case-insensitive top-level type without a slash.</param>
    /// <returns>A registered type and subtype separated by a slash.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the top-level type is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the top-level type is empty, malformed, or has no registrations.</exception>
    string MimeTypeFromTopLevelType(string topLevelType);
    /// <summary>
    /// Returns every syntactically valid media type in the bundled IANA snapshot.
    /// </summary>
    /// <returns>A new array preserving registry order and spelling.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured collection or output limits are too small.</exception>
    string[] MimeTypes();
    /// <summary>
    /// Reports whether a media type occurs in the bundled IANA snapshot.
    /// </summary>
    /// <param name="value">The case-insensitive type and subtype without parameters.</param>
    /// <returns><c>true</c> when the media type occurs in the snapshot; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the value is empty or whitespace.</exception>
    bool IsMimeType(string value);
    /// <summary>
    /// Returns the source-version text of the bundled IANA media-type registry.
    /// </summary>
    /// <returns>The snapshot version recorded with the embedded data.</returns>
    string MimeTypeVersion();
    /// <summary>
    /// Generates a lowercase synthetic username.
    /// </summary>
    /// <returns>An adjective, noun, and four digits separated by periods.</returns>
    string Username();
    /// <summary>
    /// Generates a social handle with a leading <c>@</c> marker.
    /// </summary>
    /// <remarks>The result is synthetic and makes no availability, ownership, or account-existence claim.</remarks>
    /// <returns>A lowercase handle no longer than 16 characters including its marker.</returns>
    string SocialHandle();
    /// <summary>
    /// Generates a synthetic hashtag with a leading <c>#</c> marker.
    /// </summary>
    /// <returns>A lowercase ASCII hashtag.</returns>
    string Hashtag();
    /// <summary>
    /// Selects a Unicode emoji fixture, including selected multi-code-point sequences.
    /// </summary>
    /// <returns>One emoji grapheme fixture.</returns>
    string Emoji();
    /// <summary>
    /// Returns every supported emoji fixture.
    /// </summary>
    /// <returns>A new array containing single-code-point and emoji-sequence fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured collection or output limits are too small.</exception>
    string[] Emojis();
    /// <summary>
    /// Generates a 16-character password through the shared primitive secret generator.
    /// </summary>
    /// <remarks>Use cryptographic random mode for a password used outside deterministic fixture data.</remarks>
    /// <returns>A password containing lowercase, uppercase, digit, and symbol characters.</returns>
    string Password();
    /// <summary>
    /// Generates an exact-length password through the shared primitive secret generator.
    /// </summary>
    /// <remarks>Use cryptographic random mode for a password used outside deterministic fixture data.</remarks>
    /// <param name="length">The password length, which must be at least four.</param>
    /// <returns>A password containing lowercase, uppercase, digit, and symbol characters.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length is too short or exceeds the configured output limit.</exception>
    string PasswordFromLength(int length);
    /// <summary>
    /// Generates an exact-length password with explicit required character classes.
    /// </summary>
    /// <param name="length">The password length.</param>
    /// <param name="lowercase">Whether lowercase ASCII letters are enabled and required.</param>
    /// <param name="uppercase">Whether uppercase ASCII letters are enabled and required.</param>
    /// <param name="digits">Whether decimal digits are enabled and required.</param>
    /// <param name="symbols">Whether printable ASCII password symbols are enabled and required.</param>
    /// <returns>The generated password.</returns>
    /// <exception cref="System.ArgumentException">Thrown when no character class is enabled.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the length cannot include every enabled class or exceeds the configured output limit.</exception>
    string PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(int length, bool lowercase, bool uppercase, bool digits, bool symbols);
    /// <summary>
    /// Generates a four-word hyphen-separated passphrase through the shared primitive secret generator.
    /// </summary>
    /// <remarks>Use cryptographic random mode for a passphrase used outside deterministic fixture data.</remarks>
    /// <returns>Four authored five-letter words separated by hyphens.</returns>
    string Passphrase();
    /// <summary>
    /// Generates a hyphen-separated passphrase through the shared primitive secret generator.
    /// </summary>
    /// <param name="wordCount">The number of authored five-letter words.</param>
    /// <returns>The generated passphrase.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    string PassphraseFromWordCount(int wordCount);
    /// <summary>
    /// Generates a passphrase with an explicit separator through the shared primitive secret generator.
    /// </summary>
    /// <param name="wordCount">The number of authored five-letter words.</param>
    /// <param name="separator">The well-formed UTF-16 separator placed between words.</param>
    /// <returns>The generated passphrase.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the separator is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the separator contains unpaired surrogates.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count or output exceeds a configured limit.</exception>
    string PassphraseFromWordCountAndSeparator(int wordCount, string separator);
    /// <summary>
    /// Selects a browser name from the fixed user-agent fixture catalog.
    /// </summary>
    /// <returns>A browser family name.</returns>
    string Browser();
    /// <summary>
    /// Selects an operating-system token from the fixed user-agent fixture catalog.
    /// </summary>
    /// <returns>An operating-system token used by a supported user-agent fixture.</returns>
    string OperatingSystem();
    /// <summary>
    /// Selects a device token from the fixed user-agent fixture catalog.
    /// </summary>
    /// <returns>A desktop, mobile, or hardware token used by a supported fixture.</returns>
    string Device();
    /// <summary>
    /// Selects a synthetic application name.
    /// </summary>
    /// <returns>A project-fictional application display name.</returns>
    string AppName();
    /// <summary>
    /// Generates a semantic-version-shaped application version.
    /// </summary>
    /// <returns>A three-component numeric version.</returns>
    string AppVersion();
    /// <summary>
    /// Generates a synthetic application name and version.
    /// </summary>
    /// <returns>An application display name followed by a slash and numeric version.</returns>
    string App();
    /// <summary>
    /// Selects a fixed synthetic browser user-agent fixture.
    /// </summary>
    /// <remarks>Versions are stable fixtures and do not report current releases or make compatibility claims.</remarks>
    /// <returns>A browser user-agent string.</returns>
    string UserAgent();
    /// <summary>
    /// Selects an explicitly synthetic crawler user-agent fixture.
    /// </summary>
    /// <returns>A crawler string whose contact URL uses the reserved <c>example.com</c> domain.</returns>
    string BotUserAgent();
    /// <summary>
    /// Selects correlated browser user-agent metadata.
    /// </summary>
    /// <returns>Browser, version, operating-system, device, and user-agent fields.</returns>
    UserAgentRecord UserAgentRecord();
    /// <summary>
    /// Selects correlated browser or synthetic crawler user-agent metadata.
    /// </summary>
    /// <param name="bot">Whether to select an explicitly synthetic crawler fixture.</param>
    /// <returns>Correlated name, version, operating-system, device, and user-agent fields.</returns>
    UserAgentRecord UserAgentRecordFromBot(bool bot);
    /// <summary>
    /// Returns the JOSE algorithm label used by generated JWT-shaped fixtures.
    /// </summary>
    /// <remarks>The result is always <c>none</c>; generated fixtures contain no cryptographic signature.</remarks>
    /// <returns>The unsecured JWT algorithm label.</returns>
    string JwtAlgorithm();
    /// <summary>
    /// Generates an explicitly synthetic, unsecured JWT-shaped value with a one-hour lifetime.
    /// </summary>
    /// <remarks>
    /// The value uses <c>alg: none</c> and an empty signature segment. It is not authenticated
    /// and must never be used as a credential or authorization artifact.
    /// </remarks>
    /// <returns>A three-segment JWT-shaped fixture whose issue time uses the captured reference clock.</returns>
    string Jwt();
    /// <summary>
    /// Generates an explicitly synthetic, unsecured JWT-shaped value with a requested lifetime.
    /// </summary>
    /// <param name="lifetime">The lifetime, truncated to whole seconds, with a minimum of one second.</param>
    /// <remarks>
    /// The value uses <c>alg: none</c> and an empty signature segment. It is not authenticated
    /// and must never be used as a credential or authorization artifact.
    /// </remarks>
    /// <returns>A three-segment JWT-shaped fixture with correlated issue and expiry claims.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the lifetime is less than one second.</exception>
    string JwtFromLifetime(System.TimeSpan lifetime);
    /// <summary>
    /// Generates correlated metadata for an explicitly synthetic, unsecured JWT-shaped fixture with a one-hour lifetime.
    /// </summary>
    /// <remarks>The returned token has no signature and is not authenticated.</remarks>
    /// <returns>The token, decoded JSON, claims, and explicit synthetic security metadata.</returns>
    JwtRecord JwtRecord();
    /// <summary>
    /// Generates correlated metadata for an explicitly synthetic, unsecured JWT-shaped fixture.
    /// </summary>
    /// <param name="lifetime">The lifetime, truncated to whole seconds, with a minimum of one second.</param>
    /// <remarks>The returned token has no signature and is not authenticated.</remarks>
    /// <returns>The token, decoded JSON, claims, and explicit synthetic security metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the lifetime is less than one second.</exception>
    JwtRecord JwtRecordFromLifetime(System.TimeSpan lifetime);
    /// <summary>
    /// Generates a self-contained SVG placeholder data URI with default dimensions.
    /// </summary>
    /// <remarks>The value contains only local SVG primitives and does not reference a network image service or external source.</remarks>
    /// <returns>A Base64-encoded <c>image/svg+xml</c> data URI measuring 640 by 480 pixels.</returns>
    string DataUri();
    /// <summary>
    /// Generates a self-contained SVG placeholder data URI with requested dimensions.
    /// </summary>
    /// <param name="width">The width from 1 through 4096 pixels.</param>
    /// <param name="height">The height from 1 through 4096 pixels.</param>
    /// <remarks>The value contains only local SVG primitives and does not reference a network image service or external source.</remarks>
    /// <returns>A Base64-encoded <c>image/svg+xml</c> data URI.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a dimension or the generated output exceeds its configured limit.</exception>
    string DataUriFromWidthAndHeight(int width, int height);
    /// <summary>
    /// Generates a self-contained SVG image placeholder data URI with default dimensions.
    /// </summary>
    /// <returns>A Base64-encoded <c>image/svg+xml</c> data URI measuring 640 by 480 pixels.</returns>
    string ImageDataUri();
    /// <summary>
    /// Generates a self-contained SVG image placeholder data URI with requested dimensions.
    /// </summary>
    /// <param name="width">The width from 1 through 4096 pixels.</param>
    /// <param name="height">The height from 1 through 4096 pixels.</param>
    /// <returns>A Base64-encoded <c>image/svg+xml</c> data URI.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a dimension or the generated output exceeds its configured limit.</exception>
    string ImageDataUriFromWidthAndHeight(int width, int height);
    /// <summary>
    /// Generates a self-contained SVG placeholder data URI with default dimensions.
    /// </summary>
    /// <returns>A Base64-encoded <c>image/svg+xml</c> data URI measuring 640 by 480 pixels.</returns>
    string SvgDataUri();
    /// <summary>
    /// Generates a self-contained SVG placeholder data URI with requested dimensions.
    /// </summary>
    /// <param name="width">The width from 1 through 4096 pixels.</param>
    /// <param name="height">The height from 1 through 4096 pixels.</param>
    /// <returns>A Base64-encoded <c>image/svg+xml</c> data URI.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a dimension or the generated output exceeds its configured limit.</exception>
    string SvgDataUriFromWidthAndHeight(int width, int height);
    /// <summary>
    /// Generates a self-contained anonymous avatar data URI with a default square size.
    /// </summary>
    /// <remarks>The avatar contains only local SVG primitives and does not identify a person or reference an external source.</remarks>
    /// <returns>A Base64-encoded <c>image/svg+xml</c> data URI measuring 128 by 128 pixels.</returns>
    string AvatarDataUri();
    /// <summary>
    /// Generates a self-contained anonymous avatar data URI with a requested square size.
    /// </summary>
    /// <param name="size">The width and height from 1 through 4096 pixels.</param>
    /// <remarks>The avatar contains only local SVG primitives and does not identify a person or reference an external source.</remarks>
    /// <returns>A Base64-encoded <c>image/svg+xml</c> data URI.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the size or generated output exceeds its configured limit.</exception>
    string AvatarDataUriFromSize(int size);
}
