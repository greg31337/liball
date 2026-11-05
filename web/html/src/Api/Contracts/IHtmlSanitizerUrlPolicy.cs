using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerUrlPolicy"/>.</summary>
public interface IHtmlSanitizerUrlPolicy
{
    /// <summary>Executes the GetAllowedSchemes operation.</summary>
    public string[] GetAllowedSchemes();
    /// <summary>Executes the GetAllowedHosts operation.</summary>
    public string[] GetAllowedHosts();
    /// <summary>Executes the GetAllowedPorts operation.</summary>
    public int[] GetAllowedPorts();
    /// <summary>Executes the GetAllowCredentials operation.</summary>
    public bool GetAllowCredentials();
    /// <summary>Executes the GetAllowRelativeUrls operation.</summary>
    public bool GetAllowRelativeUrls();
    /// <summary>Executes the GetAllowProtocolRelativeUrls operation.</summary>
    public bool GetAllowProtocolRelativeUrls();
    /// <summary>Executes the GetAllowFragmentUrls operation.</summary>
    public bool GetAllowFragmentUrls();
    /// <summary>Executes the GetBaseUrl operation.</summary>
    public string GetBaseUrl();
    /// <summary>Executes the GetRewriteRelativeUrls operation.</summary>
    public bool GetRewriteRelativeUrls();
    /// <summary>Executes the GetAllowedDataMediaTypes operation.</summary>
    public string[] GetAllowedDataMediaTypes();
    /// <summary>Executes the GetMaximumDataUrlBytes operation.</summary>
    public int GetMaximumDataUrlBytes();
    /// <summary>Executes the IsDataMediaTypeAllowed operation.</summary>
    public bool IsDataMediaTypeAllowed(string value);
    /// <summary>Executes the IsSchemeAllowed operation.</summary>
    public bool IsSchemeAllowed(string value);
    /// <summary>Executes the IsHostAllowed operation.</summary>
    public bool IsHostAllowed(string value);
    /// <summary>Executes the IsPortAllowed operation.</summary>
    public bool IsPortAllowed(int value);
}
