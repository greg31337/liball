using System;
using System.Globalization;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlEntities operations.</summary>
public interface IHtmlEntitiesApi
{
    /// <summary>Executes the Decode operation.</summary>
    public string Decode(string value);
    /// <summary>Executes the DecodeInContext operation.</summary>
    public string DecodeInContext(string value, HtmlEntityContext context);
    /// <summary>Executes the DecodeWithLimits operation.</summary>
    public HtmlEntityResult DecodeWithLimits(string value, HtmlEntityContext context, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the Encode operation.</summary>
    public string Encode(string value, HtmlEntityEncoding mode);
    /// <summary>Executes the EncodeInContext operation.</summary>
    public string EncodeInContext(string value, HtmlEntityContext context, HtmlEntityEncoding mode);
    /// <summary>Executes the EncodeWithLimits operation.</summary>
    public HtmlEntityResult EncodeWithLimits(string value, HtmlEntityContext context, HtmlEntityEncoding mode, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the DecodeText operation.</summary>
    public string DecodeText(string input);
    /// <summary>Executes the DecodeAttribute operation.</summary>
    public string DecodeAttribute(string input);
    /// <summary>Executes the DecodeTextWithLimits operation.</summary>
    public HtmlEntityResult DecodeTextWithLimits(string input, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the DecodeAttributeWithLimits operation.</summary>
    public HtmlEntityResult DecodeAttributeWithLimits(string input, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the EncodeText operation.</summary>
    public string EncodeText(string input, HtmlEntityEncoding mode);
    /// <summary>Executes the EncodeAttribute operation.</summary>
    public string EncodeAttribute(string input, HtmlEntityEncoding mode);
    /// <summary>Executes the EncodeTextWithLimits operation.</summary>
    public HtmlEntityResult EncodeTextWithLimits(string input, HtmlEntityEncoding mode, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the EncodeAttributeWithLimits operation.</summary>
    public HtmlEntityResult EncodeAttributeWithLimits(string input, HtmlEntityEncoding mode, HtmlLimits limits, CancellationToken cancellationToken);
}
