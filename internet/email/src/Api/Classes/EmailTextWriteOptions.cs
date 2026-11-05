using System;

namespace email;
/// <summary>
/// Controls bounded conversion of byte-oriented message or entity output to text.
/// </summary>
/// <remarks>
/// Text serialization first invokes the regular byte writer, then strictly decodes its output with the configured charset.
/// </remarks>
public class EmailTextWriteOptions : IEmailTextWriteOptionsContract
{
    EmailWriteOptions writeOptions;
    string charsetName;
    EmailTextNewlinePolicy newlinePolicy;
    bool decodeTransferEncodings;
    EmailTextWriteFailureBehavior failureBehavior;
    long maximumBytes;
    int maximumCharacters;
    /// <summary>
    /// Creates text write options using canonical Internet bytes, UTF-8 text, preserved newlines, and result-based failure reporting.
    /// </summary>
    public EmailTextWriteOptions()
    {
        writeOptions = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        charsetName = "utf-8";
        newlinePolicy = EmailTextNewlinePolicy.Preserve;
        decodeTransferEncodings = true;
        failureBehavior = EmailTextWriteFailureBehavior.ReturnResult;
        maximumBytes = EmailParsingLimitsConstants.DefaultMaxMessageBytes;
        maximumCharacters = EmailDecodingLimitsConstants.DefaultMaxTextCharacters;
    }

    /// <summary>Gets the byte-oriented write options.</summary>
    /// <returns>The current write options.</returns>
    public EmailWriteOptions GetWriteOptions()
    {
        return writeOptions.Clone();
    }

    /// <summary>Sets the byte-oriented write options.</summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetWriteOptions(EmailWriteOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        writeOptions = value.Clone();
    }

    /// <summary>Gets the charset name used to decode serialized bytes.</summary>
    /// <returns>The current charset name.</returns>
    public string GetCharsetName()
    {
        return charsetName;
    }

    /// <summary>Sets the charset name used to decode serialized bytes.</summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetCharsetName(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("The charset name cannot be empty.", "value");
        }

        charsetName = value;
    }

    /// <summary>Gets the newline policy.</summary>
    /// <returns>The current newline policy.</returns>
    public EmailTextNewlinePolicy GetNewlinePolicy()
    {
        return newlinePolicy;
    }

    /// <summary>Sets the newline policy.</summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetNewlinePolicy(EmailTextNewlinePolicy value)
    {
        if (value < EmailTextNewlinePolicy.Preserve || value > EmailTextNewlinePolicy.NormalizeToCrlf)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        newlinePolicy = value;
    }

    /// <summary>Determines whether textual leaf transfer encodings are decoded and validated.</summary>
    /// <returns><c>true</c> when transfer encodings are decoded; otherwise, <c>false</c>.</returns>
    public bool GetDecodeTransferEncodings()
    {
        return decodeTransferEncodings;
    }

    /// <summary>Sets whether textual leaf transfer encodings are decoded and validated.</summary>
    /// <param name = "value">Whether to enable transfer decoding.</param>
    public void SetDecodeTransferEncodings(bool value)
    {
        decodeTransferEncodings = value;
    }

    /// <summary>Gets the failure behavior.</summary>
    /// <returns>The current failure behavior.</returns>
    public EmailTextWriteFailureBehavior GetFailureBehavior()
    {
        return failureBehavior;
    }

    /// <summary>Sets the failure behavior.</summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetFailureBehavior(EmailTextWriteFailureBehavior value)
    {
        if (value < EmailTextWriteFailureBehavior.ReturnResult || value > EmailTextWriteFailureBehavior.Throw)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        failureBehavior = value;
    }

    /// <summary>Gets the maximum serialized byte count.</summary>
    /// <returns>The current maximum serialized byte count.</returns>
    public long GetMaximumBytes()
    {
        return maximumBytes;
    }

    /// <summary>Sets the maximum serialized byte count.</summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumBytes(long value)
    {
        maximumBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>Gets the maximum text character count.</summary>
    /// <returns>The current maximum text character count.</returns>
    public int GetMaximumCharacters()
    {
        return maximumCharacters;
    }

    /// <summary>Sets the maximum text character count.</summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumCharacters(int value)
    {
        maximumCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>Creates an independent copy of this instance.</summary>
    /// <returns>A separate copy with the same settings.</returns>
    public EmailTextWriteOptions Clone()
    {
        EmailTextWriteOptions copy = new EmailTextWriteOptions();
        copy.SetWriteOptions(writeOptions);
        copy.SetCharsetName(charsetName);
        copy.SetNewlinePolicy(newlinePolicy);
        copy.SetDecodeTransferEncodings(decodeTransferEncodings);
        copy.SetFailureBehavior(failureBehavior);
        copy.SetMaximumBytes(maximumBytes);
        copy.SetMaximumCharacters(maximumCharacters);
        return copy;
    }
}
