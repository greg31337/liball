namespace docx;

/// <summary>
/// Describes a non-fatal conflict discovered during document assembly.
/// </summary>
public class DocxAssemblyConflict
{
    /// <summary>The stable conflict code.</summary>
    public string Code;
    /// <summary>The human-readable conflict message.</summary>
    public string Message;
    /// <summary>The conflicting source identifier.</summary>
    public string SourceIdentifier;

    /// <summary>Creates an assembly conflict descriptor.</summary>
    /// <param name="code">The stable conflict code.</param>
    /// <param name="message">The conflict message.</param>
    /// <param name="sourceIdentifier">The source identifier.</param>
    public DocxAssemblyConflict(string code, string message, string sourceIdentifier)
    {
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (sourceIdentifier == null)
        {
            throw new ArgumentNullException("sourceIdentifier");
        }

        this.Code = code;
        this.Message = message;
        this.SourceIdentifier = sourceIdentifier;
    }
}
