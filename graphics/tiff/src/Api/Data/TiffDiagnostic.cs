namespace tiff;

/// <summary>Describes one warning or error, with optional file, image, and tag location information.</summary>
public class TiffDiagnostic
{
    /// <summary>Defines the no offset value used by this API.</summary>
    public const long NoOffset = -1;
    /// <summary>Defines the no image index value used by this API.</summary>
    public const int NoImageIndex = -1;
    /// <summary>Defines the no tag id value used by this API.</summary>
    public const int NoTagId = -1;

    /// <summary>Contains the severity.</summary>
    public TiffDiagnosticSeverity Severity;
    /// <summary>Contains the code.</summary>
    public TiffErrorCode Code;
    /// <summary>Contains the message.</summary>
    public string Message;
    /// <summary>Contains the byte offset measured from the start of the TIFF file.</summary>
    public long Offset;
    /// <summary>Identifies the zero-based image directory in the file.</summary>
    public int ImageIndex;
    /// <summary>Identifies the TIFF tag.</summary>
    public int TagId;

    /// <summary>Creates a diagnostic without file, image, or tag location information.</summary>
    /// <param name="severity">The severity.</param>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    public TiffDiagnostic(TiffDiagnosticSeverity severity, TiffErrorCode code, string message)
    {
        Severity = severity;
        Code = code;
        if (message == null)
        {
            Message = "";
        }
        else
        {
            Message = message;
        }

        Offset = NoOffset;
        ImageIndex = NoImageIndex;
        TagId = NoTagId;
    }

    /// <summary>Creates a diagnostic with file, image, and tag location information.</summary>
    /// <param name="severity">The severity.</param>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>A diagnostic containing the supplied context.</returns>
    internal static TiffDiagnostic Create(TiffDiagnosticSeverity severity, TiffErrorCode code, string message, long offset, int imageIndex, int tagId)
    {
        TiffDiagnostic diagnostic = new TiffDiagnostic(severity, code, message);
        diagnostic.Offset = offset;
        diagnostic.ImageIndex = imageIndex;
        diagnostic.TagId = tagId;
        return diagnostic;
    }

    /// <summary>Determines whether is error under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsError()
    {
        return Severity == TiffDiagnosticSeverity.Error;
    }

    /// <summary>Determines whether is warning under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsWarning()
    {
        return Severity == TiffDiagnosticSeverity.Warning;
    }

    /// <summary>Determines whether has offset under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasOffset()
    {
        return Offset >= 0;
    }

    /// <summary>Determines whether has image index under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasImageIndex()
    {
        return ImageIndex >= 0;
    }

    /// <summary>Determines whether has tag id under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasTagId()
    {
        return TagId >= 0;
    }

    /// <summary>Formats the diagnostic as a readable message including any available file, image, and tag context.</summary>
    /// <returns>The resulting string.</returns>
    internal string Format()
    {
        string text = Severity.ToString() + " " + Code.ToString();
        if (HasOffset())
        {
            text = text + " at offset " + Offset.ToString();
        }

        if (HasImageIndex())
        {
            text = text + " image " + ImageIndex.ToString();
        }

        if (HasTagId())
        {
            text = text + " tag " + TagId.ToString();
        }

        if (Message.Length > 0)
        {
            text = text + ": " + Message;
        }

        return text;
    }
}
