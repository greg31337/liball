using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a media type.
/// </summary>
public class EmailMediaType : IEmailMediaTypeContract
{
    string type;
    string subtype;
    EmailParameterList parameters;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    bool defaulted;
    /// <summary>
    /// Creates a new media type.
    /// </summary>
    /// <param name = "type">The type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "defaulted">Whether to enable the defaulted behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMediaType(string? type, string? subtype, EmailParameterList parameters, byte[] rawBytes, EmailSourcePosition? sourcePosition, bool defaulted)
    {
        if (type == null || type.Length == 0)
        {
            throw new ArgumentException("A media type is required.", "type");
        }

        if (subtype == null || subtype.Length == 0)
        {
            throw new ArgumentException("A media subtype is required.", "subtype");
        }

        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.type = type;
        this.subtype = subtype;
        this.parameters = parameters;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
        this.defaulted = defaulted;
    }

    internal static EmailMediaType CreateDefaultImplementationWith()
    {
        return CreateDefaultImplementationWithRawBytesAndPosition(new byte[0], new EmailSourcePosition(0, 1, 1));
    }

    internal static EmailMediaType CreateDefaultImplementationWithRawBytesAndPosition(byte[] rawBytes, EmailSourcePosition position)
    {
        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (position == null)
        {
            throw new ArgumentNullException("position");
        }

        EmailParameter charset = new EmailParameter("charset", Encoding.ASCII.GetBytes("us-ascii"), Encoding.ASCII.GetBytes("us-ascii"), false, Encoding.ASCII.GetBytes("charset=us-ascii"), position);
        List<EmailParameter> physical = new List<EmailParameter>();
        physical.Add(charset);
        List<EmailRfc2231Parameter> rfc = new List<EmailRfc2231Parameter>();
        rfc.Add(new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuotedAndSourcePosition("charset", Encoding.ASCII.GetBytes("us-ascii"), false, position));
        EmailRfc2231DecodeResult decoded = new EmailRfc2231().DecodeWithParametersAndMode(rfc, EmailRfc2231DecodeMode.Strict);
        return new EmailMediaType("text", "plain", new EmailParameterList(physical, decoded.GetParameters()), rawBytes, position, true);
    }

    /// <summary>
    /// Gets the top level type.
    /// </summary>
    /// <returns>The current top level type.</returns>
    public string GetTopLevelType()
    {
        return type;
    }

    /// <summary>
    /// Gets the subtype.
    /// </summary>
    /// <returns>The current subtype.</returns>
    public string GetSubtype()
    {
        return subtype;
    }

    /// <summary>
    /// Gets the normalized top level type.
    /// </summary>
    /// <returns>The current normalized top level type.</returns>
    public string GetNormalizedTopLevelType()
    {
        return type.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the normalized subtype.
    /// </summary>
    /// <returns>The current normalized subtype.</returns>
    public string GetNormalizedSubtype()
    {
        return subtype.ToLowerInvariant();
    }

    /// <summary>
    /// Determines whether the current value or state is type.
    /// </summary>
    /// <param name = "expectedType">The expected type used by the operation.</param>
    /// <param name = "expectedSubtype">The expected subtype used by the operation.</param>
    /// <returns><c>true</c> when the value or state is type; otherwise, <c>false</c>.</returns>
    public bool IsType(string? expectedType, string? expectedSubtype)
    {
        return string.Equals(type, expectedType, StringComparison.OrdinalIgnoreCase) && string.Equals(subtype, expectedSubtype, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the current value or state is extension type.
    /// </summary>
    /// <returns><c>true</c> when the value or state is extension type; otherwise, <c>false</c>.</returns>
    public bool IsExtensionType()
    {
        return !string.Equals(type, "text", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "image", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "audio", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "video", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "application", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "message", StringComparison.OrdinalIgnoreCase) && !string.Equals(type, "multipart", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the current value or state is private type.
    /// </summary>
    /// <returns><c>true</c> when the value or state is private type; otherwise, <c>false</c>.</returns>
    public bool IsPrivateType()
    {
        return type.Length > 2 && (type[0] == 'x' || type[0] == 'X') && type[1] == '-';
    }

    /// <summary>
    /// Determines whether the current value or state is private subtype.
    /// </summary>
    /// <returns><c>true</c> when the value or state is private subtype; otherwise, <c>false</c>.</returns>
    public bool IsPrivateSubtype()
    {
        return subtype.Length > 2 && (subtype[0] == 'x' || subtype[0] == 'X') && subtype[1] == '-';
    }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <returns>The current parameters.</returns>
    public EmailParameterList GetParameters()
    {
        return parameters;
    }

    /// <summary>
    /// Determines whether the operation or source value was defaulted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasDefaulted()
    {
        return defaulted;
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
