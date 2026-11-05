using System;

namespace email;
/// <summary>
/// Represents a message ID.
/// </summary>
public class EmailMessageId : IEmailMessageIdContract
{
    string idLeft;
    string idRight;
    bool rightDomainLiteral;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    bool obsoleteSyntax;
    bool recovered;
    /// <summary>
    /// Creates a new message ID.
    /// </summary>
    /// <param name = "idLeft">The id left used by the operation.</param>
    /// <param name = "idRight">The id right used by the operation.</param>
    /// <param name = "rightDomainLiteral">Whether to enable the right domain literal behavior.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "obsoleteSyntax">Whether to enable the obsolete syntax behavior.</param>
    /// <param name = "recovered">Whether to enable the recovered behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMessageId(string? idLeft, string? idRight, bool rightDomainLiteral, byte[] rawBytes, EmailSourcePosition sourcePosition, bool obsoleteSyntax, bool recovered)
    {
        if (idLeft == null || idLeft.Length == 0)
        {
            throw new ArgumentException("An identifier left side is required.", "idLeft");
        }

        if (idRight == null)
        {
            throw new ArgumentNullException("idRight");
        }

        if (!rightDomainLiteral && idRight.Length == 0)
        {
            throw new ArgumentException("An identifier right side is required.", "idRight");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.idLeft = idLeft;
        this.idRight = idRight;
        this.rightDomainLiteral = rightDomainLiteral;
        this.rawBytes = new byte[rawBytes.Length];
        rawBytes.CopyTo(this.rawBytes, 0);
        this.sourcePosition = sourcePosition;
        this.obsoleteSyntax = obsoleteSyntax;
        this.recovered = recovered;
    }

    /// <summary>
    /// Gets the ID left.
    /// </summary>
    /// <returns>The current ID left.</returns>
    public string GetIdLeft()
    {
        return idLeft;
    }

    /// <summary>
    /// Gets the ID right.
    /// </summary>
    /// <returns>The current ID right.</returns>
    public string GetIdRight()
    {
        return idRight;
    }

    /// <summary>
    /// Determines whether the current value or state is right domain literal.
    /// </summary>
    /// <returns><c>true</c> when the value or state is right domain literal; otherwise, <c>false</c>.</returns>
    public bool IsRightDomainLiteral()
    {
        return rightDomainLiteral;
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
        byte[] copy = new byte[rawBytes.Length];
        rawBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    public bool UsesObsoleteSyntax()
    {
        return obsoleteSyntax;
    }

    /// <summary>
    /// Determines whether the operation or source value was recovered.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasRecovered()
    {
        return recovered;
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The message ID format result produced by the operation.</returns>
    public EmailMessageIdFormatResult Format()
    {
        return new EmailMessageIdFormatter().FormatWithIdentifier(this);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    public EmailMessageIdFormatResult FormatWithOptions(EmailMessageIdFormatOptions options)
    {
        return new EmailMessageIdFormatter().FormatWithIdentifierAndOptions(this, options);
    }
}
