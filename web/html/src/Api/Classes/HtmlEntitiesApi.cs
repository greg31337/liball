using System;
using System.Globalization;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlEntities operations.</summary>
public class HtmlEntitiesApi : IHtmlEntitiesApi
{
    /// <summary>Executes the Decode operation.</summary>
    public string Decode(string value)
    {
        return HtmlEntities.Decode(value);
    }

    /// <summary>Executes the DecodeInContext operation.</summary>
    public string DecodeInContext(string value, HtmlEntityContext context)
    {
        return HtmlEntities.Decode(value, context);
    }

    /// <summary>Executes the DecodeWithLimits operation.</summary>
    public HtmlEntityResult DecodeWithLimits(string value, HtmlEntityContext context, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlEntities.Decode(value, context, limits, cancellationToken);
    }

    /// <summary>Executes the Encode operation.</summary>
    public string Encode(string value, HtmlEntityEncoding mode)
    {
        return HtmlEntities.Encode(value, mode);
    }

    /// <summary>Executes the EncodeInContext operation.</summary>
    public string EncodeInContext(string value, HtmlEntityContext context, HtmlEntityEncoding mode)
    {
        return HtmlEntities.Encode(value, context, mode);
    }

    /// <summary>Executes the EncodeWithLimits operation.</summary>
    public HtmlEntityResult EncodeWithLimits(string value, HtmlEntityContext context, HtmlEntityEncoding mode, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlEntities.Encode(value, context, mode, limits, cancellationToken);
    }

    /// <summary>Executes the DecodeText operation.</summary>
    public string DecodeText(string input)
    {
        return HtmlEntities.DecodeText(input);
    }

    /// <summary>Executes the DecodeAttribute operation.</summary>
    public string DecodeAttribute(string input)
    {
        return HtmlEntities.DecodeAttribute(input);
    }

    /// <summary>Executes the DecodeTextWithLimits operation.</summary>
    public HtmlEntityResult DecodeTextWithLimits(string input, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlEntities.DecodeText(input, limits, cancellationToken);
    }

    /// <summary>Executes the DecodeAttributeWithLimits operation.</summary>
    public HtmlEntityResult DecodeAttributeWithLimits(string input, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlEntities.DecodeAttribute(input, limits, cancellationToken);
    }

    /// <summary>Executes the EncodeText operation.</summary>
    public string EncodeText(string input, HtmlEntityEncoding mode)
    {
        return HtmlEntities.EncodeText(input, mode);
    }

    /// <summary>Executes the EncodeAttribute operation.</summary>
    public string EncodeAttribute(string input, HtmlEntityEncoding mode)
    {
        return HtmlEntities.EncodeAttribute(input, mode);
    }

    /// <summary>Executes the EncodeTextWithLimits operation.</summary>
    public HtmlEntityResult EncodeTextWithLimits(string input, HtmlEntityEncoding mode, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlEntities.EncodeText(input, mode, limits, cancellationToken);
    }

    /// <summary>Executes the EncodeAttributeWithLimits operation.</summary>
    public HtmlEntityResult EncodeAttributeWithLimits(string input, HtmlEntityEncoding mode, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlEntities.EncodeAttribute(input, mode, limits, cancellationToken);
    }
}
