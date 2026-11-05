using System;

namespace html;

/// <summary>Reports HTML serialization output, character count, and explicit completion status.</summary>
public class HtmlSerializationResult : IHtmlSerializationResult
{
    internal HtmlSerializationStatus status;
    internal bool hasString;
    internal string? serialized;
    internal int charactersWritten;

    internal HtmlSerializationResult(HtmlSerializationStatus statusValue,
        bool hasStringValue, string? serializedValue,
        int charactersWrittenValue)
    {
        ValidateStatus(statusValue);
        if (hasStringValue && serializedValue == null)
        {
            throw new ArgumentNullException("serializedValue");
        }

        if (!hasStringValue && serializedValue != null)
        {
            throw new ArgumentException(
                "A writer result cannot contain a serialized string.",
                "serializedValue");
        }

        if (charactersWrittenValue < 0)
        {
            throw new ArgumentOutOfRangeException("charactersWrittenValue",
                charactersWrittenValue,
                "The written character count cannot be negative.");
        }

        if (hasStringValue && serializedValue != null
            && serializedValue.Length != charactersWrittenValue)
        {
            throw new ArgumentException(
                "The serialized string length must match the written character count.",
                "serializedValue");
        }

        this.status = statusValue;
        this.hasString = hasStringValue;
        this.serialized = serializedValue;
        this.charactersWritten = charactersWrittenValue;
    }

    internal static HtmlSerializationResult CreateString(string serialized,
        bool limitExceeded)
    {
        if (serialized == null)
        {
            throw new ArgumentNullException("serialized");
        }

        HtmlSerializationStatus resultStatus = limitExceeded
            ? HtmlSerializationStatus.LimitExceeded
            : HtmlSerializationStatus.Complete;
        return new HtmlSerializationResult(resultStatus, true, serialized,
            serialized.Length);
    }

    internal static HtmlSerializationResult CreateWriter(
        int charactersWritten, bool limitExceeded)
    {
        HtmlSerializationStatus resultStatus = limitExceeded
            ? HtmlSerializationStatus.LimitExceeded
            : HtmlSerializationStatus.Complete;
        return new HtmlSerializationResult(resultStatus, false, null,
            charactersWritten);
    }

    /// <summary>Gets the explicit completion status.</summary>
    /// <returns><see cref = "HtmlSerializationStatus.Complete"/> when all markup was written; otherwise, <see cref = "HtmlSerializationStatus.LimitExceeded"/>.</returns>
    public HtmlSerializationStatus GetStatus()
    {
        return this.status;
    }

    /// <summary>Reports whether all requested markup was written.</summary>
    /// <returns><c>true</c> when serialization completed; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return this.status == HtmlSerializationStatus.Complete;
    }

    /// <summary>Reports whether the configured output-character limit stopped serialization.</summary>
    /// <returns><c>true</c> when the output limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.status == HtmlSerializationStatus.LimitExceeded;
    }

    /// <summary>Reports whether incomplete serialization retained a safe output prefix.</summary>
    /// <returns><c>true</c> when at least one character was written before the output limit stopped serialization; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return !this.IsComplete() && this.charactersWritten > 0;
    }

    /// <summary>Reports whether this result came from a string-producing operation.</summary>
    /// <returns><c>true</c> when <see cref = "GetString"/> returns the retained output; <c>false</c> for a <c>TextWriter</c> operation.</returns>
    public bool HasString()
    {
        return this.hasString;
    }

    /// <summary>Gets the serialized output retained by a string-producing operation.</summary>
    /// <returns>The complete string or safe retained prefix. Returns null when output was sent to a <c>TextWriter</c>.</returns>
    public string? GetString()
    {
        return this.serialized;
    }

    /// <summary>Gets the number of characters written by this operation.</summary>
    /// <returns>The complete output length or the retained safe-prefix length.</returns>
    public int GetCharactersWritten()
    {
        return this.charactersWritten;
    }

    /// <summary>Gets the stable code explaining incomplete serialization.</summary>
    /// <returns><see cref = "HtmlDiagnosticCode.None"/> on completion; otherwise, <see cref = "HtmlDiagnosticCode.SerializationLimitExceeded"/>.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        return this.IsComplete() ? HtmlDiagnosticCode.None
            : HtmlDiagnosticCode.SerializationLimitExceeded;
    }

    internal static void ValidateStatus(HtmlSerializationStatus value)
    {
        if ((int)value < (int)HtmlSerializationStatus.Complete
            || (int)value > (int)HtmlSerializationStatus.LimitExceeded)
        {
            throw new ArgumentOutOfRangeException("statusValue", (int)value,
                "Unknown HTML serialization status.");
        }
    }
}
