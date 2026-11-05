namespace docx;

/// <summary>
/// Provides diagnostic codes reported by document assembly operations.
/// </summary>
public static class DocxAssemblyDiagnosticCodes
{
    /// <summary>A source style differs from a target style with the same identifier.</summary>
    public const string StyleConflict = "assembly.style.conflict";
    /// <summary>A source bookmark name is already present in the target document.</summary>
    public const string BookmarkNameConflict = "assembly.bookmark-name.conflict";
}
