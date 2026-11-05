using System;

namespace html;
/// <summary>Configures allowed URL schemes, relative references, and protocol-relative references.</summary>
public class HtmlSanitizerUrlPolicy : IHtmlSanitizerUrlPolicy
{
    internal string[] schemes = Array.Empty<string>();
    internal string[] hosts = Array.Empty<string>();
    internal int[] ports = Array.Empty<int>();
    internal bool allowCredentials;
    internal bool allowRelativeUrls;
    internal bool allowProtocolRelativeUrls;
    internal bool allowFragmentUrls;
    internal string baseUrl = "";
    internal bool rewriteRelativeUrls;
    internal string[] dataMediaTypes = Array.Empty<string>();
    internal int maximumDataBytes;
    /// <summary>Copies and normalizes URL allowlists and resolution settings without enabling <c>data:</c> payloads.</summary>
    /// <param name = "schemesValue">The allowed URL schemes.</param>
    /// <param name = "hostsValue">The allowed host names, or an empty array when hosts are unrestricted.</param>
    /// <param name = "portsValue">The allowed network ports, or an empty array when ports are unrestricted.</param>
    /// <param name = "allowCredentialsValue">Whether accepted URLs may contain user information.</param>
    /// <param name = "allowRelativeUrlsValue">Whether relative URL references are accepted.</param>
    /// <param name = "allowProtocolRelativeUrlsValue">Whether protocol-relative URL references are accepted.</param>
    /// <param name = "allowFragmentUrlsValue">Whether fragment-only URL references are accepted.</param>
    /// <param name = "baseUrlValue">The trusted base URL used to resolve relative references.</param>
    /// <param name = "rewriteRelativeUrlsValue">Whether accepted relative URLs are rewritten to absolute URLs.</param>
    /// <remarks>Scheme and host spellings are normalized and deduplicated, ports are sorted and deduplicated, and every input array is copied. Empty host or port arrays mean unrestricted values after the scheme check.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required array, entry, or base URL is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a scheme, host, port, base URL, or option combination is invalid.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an allowed port is outside the range 1 through 65535.</exception>
    public HtmlSanitizerUrlPolicy(string[] schemesValue, string[] hostsValue, int[] portsValue, bool allowCredentialsValue, bool allowRelativeUrlsValue, bool allowProtocolRelativeUrlsValue, bool allowFragmentUrlsValue, string baseUrlValue, bool rewriteRelativeUrlsValue)
    {
        this.Initialize(schemesValue, hostsValue, portsValue, allowCredentialsValue, allowRelativeUrlsValue, allowProtocolRelativeUrlsValue, allowFragmentUrlsValue, baseUrlValue, rewriteRelativeUrlsValue, new string[0], 0);
    }

    /// <summary>Creates a policy with normalized URL allowlists, relative-reference handling, and bounded <c>data:</c> media settings.</summary>
    /// <param name = "schemesValue">The allowed URL schemes.</param>
    /// <param name = "hostsValue">The allowed host names, or an empty array when hosts are unrestricted.</param>
    /// <param name = "portsValue">The allowed network ports, or an empty array when ports are unrestricted.</param>
    /// <param name = "allowCredentialsValue">Whether accepted URLs may contain user information.</param>
    /// <param name = "allowRelativeUrlsValue">Whether relative URL references are accepted.</param>
    /// <param name = "allowProtocolRelativeUrlsValue">Whether protocol-relative URL references are accepted.</param>
    /// <param name = "allowFragmentUrlsValue">Whether fragment-only URL references are accepted.</param>
    /// <param name = "baseUrlValue">The trusted base URL used to resolve relative references.</param>
    /// <param name = "rewriteRelativeUrlsValue">Whether accepted relative URLs are rewritten to absolute URLs.</param>
    /// <param name = "dataMediaTypesValue">The allowed media types for <c>data</c> URLs.</param>
    /// <param name = "maximumDataBytesValue">The maximum decoded payload size allowed for a <c>data</c> URL.</param>
    /// <returns>A normalized URL policy containing the supplied settings.</returns>
    /// <remarks>Scheme, host, and media-type spellings are normalized and deduplicated; ports are sorted and deduplicated; every input array is copied. Empty host or port arrays mean unrestricted values after the scheme check.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required array, entry, or base URL is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a scheme, host, port, media type, base URL, or option combination is invalid.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an allowed port is outside the range 1 through 65535 or <c>maximumDataBytesValue</c> is negative.</exception>
    internal static HtmlSanitizerUrlPolicy CreateWithDataUrls(string[] schemesValue, string[] hostsValue, int[] portsValue, bool allowCredentialsValue, bool allowRelativeUrlsValue, bool allowProtocolRelativeUrlsValue, bool allowFragmentUrlsValue, string baseUrlValue, bool rewriteRelativeUrlsValue, string[] dataMediaTypesValue, int maximumDataBytesValue)
    {
        HtmlSanitizerUrlPolicy policy = new HtmlSanitizerUrlPolicy(schemesValue, hostsValue, portsValue, allowCredentialsValue, allowRelativeUrlsValue, allowProtocolRelativeUrlsValue, allowFragmentUrlsValue, baseUrlValue, rewriteRelativeUrlsValue);
        policy.InitializeDataUrlSettings(dataMediaTypesValue, maximumDataBytesValue);
        return policy;
    }

    internal void Initialize(string[] schemesValue, string[] hostsValue, int[] portsValue, bool allowCredentialsValue, bool allowRelativeUrlsValue, bool allowProtocolRelativeUrlsValue, bool allowFragmentUrlsValue, string baseUrlValue, bool rewriteRelativeUrlsValue, string[] dataMediaTypesValue, int maximumDataBytesValue)
    {
        this.schemes = CopyAndSortSchemes(schemesValue);
        this.hosts = CopyAndSortHosts(hostsValue);
        this.ports = CopyAndSortPorts(portsValue);
        this.allowCredentials = allowCredentialsValue;
        this.allowRelativeUrls = allowRelativeUrlsValue;
        this.allowProtocolRelativeUrls = allowProtocolRelativeUrlsValue;
        this.allowFragmentUrls = allowFragmentUrlsValue;
        this.baseUrl = NormalizeBaseUrl(baseUrlValue);
        if (rewriteRelativeUrlsValue && this.baseUrl.Length == 0)
        {
            throw new ArgumentException("Relative URL rewriting requires an absolute base URL.", "rewriteRelativeUrlsValue");
        }

        this.rewriteRelativeUrls = rewriteRelativeUrlsValue;
        this.InitializeDataUrlSettings(dataMediaTypesValue, maximumDataBytesValue);
    }

    internal void InitializeDataUrlSettings(string[] dataMediaTypesValue, int maximumDataBytesValue)
    {
        this.dataMediaTypes = CopyAndSortMediaTypes(dataMediaTypesValue);
        if (maximumDataBytesValue < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDataBytesValue", maximumDataBytesValue, "The maximum decoded data URL size cannot be negative.");
        }

        this.maximumDataBytes = maximumDataBytesValue;
    }

    /// <summary>Gets normalized URL schemes accepted by this policy.</summary>
    /// <returns>A snapshot of the allowed schemes; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedSchemes()
    {
        return Copy(this.schemes);
    }

    /// <summary>Gets normalized URL hosts accepted by this policy.</summary>
    /// <returns>A snapshot of the allowed hosts; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedHosts()
    {
        return Copy(this.hosts);
    }

    /// <summary>Gets explicit network ports accepted by this policy.</summary>
    /// <returns>A snapshot of the allowed ports; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public int[] GetAllowedPorts()
    {
        int[] result = new int[this.ports.Length];
        Array.Copy(this.ports, result, this.ports.Length);
        return result;
    }

    /// <summary>Reports whether the URL policy permits embedded user credentials.</summary>
    /// <returns><c>true</c> when URL credentials are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowCredentials()
    {
        return this.allowCredentials;
    }

    /// <summary>Reports whether the policy permits relative URLs.</summary>
    /// <returns><c>true</c> when relative URLs are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowRelativeUrls()
    {
        return this.allowRelativeUrls;
    }

    /// <summary>Reports whether the policy permits protocol-relative URLs.</summary>
    /// <returns><c>true</c> when protocol-relative URLs are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowProtocolRelativeUrls()
    {
        return this.allowProtocolRelativeUrls;
    }

    /// <summary>Reports whether the policy permits fragment-only URLs.</summary>
    /// <returns><c>true</c> when fragment-only URLs are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowFragmentUrls()
    {
        return this.allowFragmentUrls;
    }

    /// <summary>Gets the trusted base used to resolve relative URL references.</summary>
    /// <returns>The configured absolute base URL, or an empty string when no base is configured.</returns>
    public string GetBaseUrl()
    {
        return this.baseUrl;
    }

    /// <summary>Reports whether accepted relative URLs are rewritten as absolute URLs.</summary>
    /// <returns><c>true</c> when accepted relative URLs are rewritten as absolute URLs; otherwise, <c>false</c>.</returns>
    public bool GetRewriteRelativeUrls()
    {
        return this.rewriteRelativeUrls;
    }

    /// <summary>Gets normalized media types accepted in <c>data:</c> URLs.</summary>
    /// <returns>A snapshot of the allowed data media types; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedDataMediaTypes()
    {
        return Copy(this.dataMediaTypes);
    }

    /// <summary>Gets the largest decoded payload accepted from an allowed <c>data:</c> URL.</summary>
    /// <returns>The positive payload ceiling in bytes.</returns>
    public int GetMaximumDataUrlBytes()
    {
        return this.maximumDataBytes;
    }

    /// <summary>Determines whether a media type is allowed for a <c>data</c> URL.</summary>
    /// <param name = "value">The media type to compare after normalization.</param>
    /// <returns><c>true</c> when the media type is allowed; otherwise, <c>false</c>.</returns>
    public bool IsDataMediaTypeAllowed(string value)
    {
        string normalized = NormalizeMediaType(value, "value");
        return Find(this.dataMediaTypes, normalized) >= 0;
    }

    /// <summary>Determines whether the normalized URL scheme is allowed.</summary>
    /// <param name = "value">The scheme name without a trailing colon.</param>
    /// <returns><c>true</c> when the scheme is allowed; otherwise, <c>false</c>.</returns>
    public bool IsSchemeAllowed(string value)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeScheme(value, "value");
        return Find(this.schemes, normalized) >= 0;
    }

    /// <summary>Determines whether the normalized URL host is allowed.</summary>
    /// <param name = "value">The host name to compare with the allowlist.</param>
    /// <returns><c>true</c> when the host is allowed; otherwise, <c>false</c>.</returns>
    public bool IsHostAllowed(string value)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeHost(value, "value");
        return this.hosts.Length == 0 || Find(this.hosts, normalized) >= 0;
    }

    /// <summary>Determines whether the explicit network port is allowed.</summary>
    /// <param name = "value">The numeric port to compare with the allowlist.</param>
    /// <returns><c>true</c> when the port is allowed; otherwise, <c>false</c>.</returns>
    public bool IsPortAllowed(int value)
    {
        ValidatePort(value, "value");
        return Find(this.ports, value) >= 0;
    }

    internal HtmlSanitizerUrlPolicy Copy()
    {
        return HtmlSanitizerUrlPolicy.CreateWithDataUrls(this.schemes, this.hosts, this.ports, this.allowCredentials, this.allowRelativeUrls, this.allowProtocolRelativeUrls, this.allowFragmentUrls, this.baseUrl, this.rewriteRelativeUrls, this.dataMediaTypes, this.maximumDataBytes);
    }

    internal static string NormalizeMediaType(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (!IsValidMediaType(value))
        {
            throw new ArgumentException("A data URL media type is invalid.", parameterName);
        }

        return FoldAscii(value);
    }

    internal static bool IsValidMediaType(string value)
    {
        if (value == null)
        {
            return false;
        }

        int slash = value.IndexOf('/');
        if (slash <= 0 || slash == value.Length - 1 || value.IndexOf('/', slash + 1) >= 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != '/' && !IsMediaTypeTokenCharacter(current))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string NormalizeBaseUrl(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("baseUrlValue");
        }

        if (value.Length == 0)
        {
            return "";
        }

        HtmlUrlResult result = HtmlUrls.Normalize(value);
        if (!result.IsSuccess() || result.WasRelative())
        {
            throw new ArgumentException("The sanitizer base URL must be absolute and valid.", "baseUrlValue");
        }

        return result.GetUrl();
    }

    internal static string[] CopyAndSortSchemes(string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("schemesValue");
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = HtmlSanitizerPolicyNames.NormalizeScheme(values[index], "schemesValue");
            index = index + 1;
        }

        Sort(result);
        ValidateUnique(result, "URL schemes");
        return result;
    }

    internal static string[] CopyAndSortHosts(string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("hostsValue");
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = HtmlSanitizerPolicyNames.NormalizeHost(values[index], "hostsValue");
            index = index + 1;
        }

        Sort(result);
        ValidateUnique(result, "URL hosts");
        return result;
    }

    internal static string[] CopyAndSortMediaTypes(string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("dataMediaTypesValue");
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = NormalizeMediaType(values[index], "dataMediaTypesValue");
            index = index + 1;
        }

        Sort(result);
        ValidateUnique(result, "data URL media types");
        return result;
    }

    internal static int[] CopyAndSortPorts(int[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("portsValue");
        }

        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = ValidatePort(values[index], "portsValue");
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            int current = result[index];
            int position = index;
            while (position > 0 && result[position - 1] > current)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            if (result[index - 1] == result[index])
            {
                throw new ArgumentException("URL ports must be unique.", "portsValue");
            }

            index = index + 1;
        }

        return result;
    }

    internal static int ValidatePort(int value, string parameterName)
    {
        if (value < 1 || value > 65535)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A URL port must be between 1 and 65535.");
        }

        return value;
    }

    internal static void Sort(string[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            string current = values[index];
            int position = index;
            while (position > 0 && string.CompareOrdinal(values[position - 1], current) > 0)
            {
                values[position] = values[position - 1];
                position = position - 1;
            }

            values[position] = current;
            index = index + 1;
        }
    }

    internal static void ValidateUnique(string[] values, string description)
    {
        int index = 1;
        while (index < values.Length)
        {
            if (values[index - 1] == values[index])
            {
                throw new ArgumentException(description + " must be unique.");
            }

            index = index + 1;
        }
    }

    internal static int Find(string[] values, string value)
    {
        int low = 0;
        int high = values.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = string.CompareOrdinal(values[middle], value);
            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    internal static int Find(int[] values, int value)
    {
        int low = 0;
        int high = values.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (values[middle] == value)
            {
                return middle;
            }

            if (values[middle] < value)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    internal static string[] Copy(string[] values)
    {
        string[] result = new string[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    internal static bool IsMediaTypeTokenCharacter(char value)
    {
        return Ascii.IsAlphanumeric(value) || value == '!' || value == '#' || value == '$' || value == '%' || value == '&' || value == '\'' || value == '*' || value == '+' || value == '-' || value == '.' || value == '^' || value == '_' || value == '`' || value == '|' || value == '~';
    }

    internal static string FoldAscii(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string (result);
    }
}
