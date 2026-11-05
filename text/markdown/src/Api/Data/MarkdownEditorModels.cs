using System;

namespace markdown;

/// <summary>
/// Describes one contiguous replacement in normalized Markdown source.
/// </summary>
public class MarkdownTextChange
{
    /// <summary>The zero-based start offset in the previous source.</summary>
    public int StartOffset;
    /// <summary>The number of characters removed from the previous source.</summary>
    public int RemovedLength;
    /// <summary>The replacement text.</summary>
    public string InsertedText;

    /// <summary>
    /// Initializes a source-text replacement.
    /// </summary>
    /// <param name="startOffset">The zero-based start offset.</param>
    /// <param name="removedLength">The number of removed characters.</param>
    /// <param name="insertedText">The replacement text.</param>
    public MarkdownTextChange(int startOffset, int removedLength, string insertedText)
    {
        if (startOffset < 0)
        {
            throw new ArgumentOutOfRangeException("startOffset");
        }

        if (removedLength < 0)
        {
            throw new ArgumentOutOfRangeException("removedLength");
        }

        if (insertedText == null)
        {
            throw new ArgumentNullException("insertedText");
        }

        StartOffset = startOffset;
        RemovedLength = removedLength;
        InsertedText = insertedText;
    }
}

/// <summary>
/// Reports the result of an incremental document reparse.
/// </summary>
public class MarkdownIncrementalParseResult
{
    /// <summary>The updated document, which is the caller-supplied document instance.</summary>
    public MarkdownDocument Document;
    /// <summary>The changed range in the updated source.</summary>
    public MarkdownSourceSpan ChangedSpan;
    /// <summary>The number of unchanged top-level node identities retained.</summary>
    public int ReusedTopLevelNodeCount;
    /// <summary>Whether a full correctness parse was used as the deterministic fallback.</summary>
    public bool UsedFullParse;

    internal MarkdownIncrementalParseResult(
        MarkdownDocument document,
        MarkdownSourceSpan changedSpan,
        int reusedTopLevelNodeCount,
        bool usedFullParse)
    {
        Document = document;
        ChangedSpan = changedSpan;
        ReusedTopLevelNodeCount = reusedTopLevelNodeCount;
        UsedFullParse = usedFullParse;
    }
}
