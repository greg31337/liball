using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Provides contextual partial parsing and correctness-preserving incremental reparsing for editors.
/// </summary>
/// <remarks>
/// Incremental reparsing validates correctness with a deterministic full parse, then retains unchanged top-level node identities whose complete structure and absolute spans still match.
/// </remarks>
public class MarkdownEditorOperations : IMarkdownEditorOperations
{
    /// <summary>Initializes editor parsing operations.</summary>
    public MarkdownEditorOperations()
    {
    }

    /// <summary>Parses the blocks intersecting an absolute source range using CommonMark defaults.</summary>
    /// <param name="source">The complete Markdown source.</param>
    /// <param name="startOffset">The absolute range start.</param>
    /// <param name="length">The range length.</param>
    /// <returns>A document fragment with absolute source spans.</returns>
    public MarkdownDocument ParsePartialSource(string source, int startOffset, int length)
    {
        return ParsePartialSourceWithPipeline(source, startOffset, length, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Parses the blocks intersecting an absolute source range using a supplied pipeline.</summary>
    /// <param name="source">The complete Markdown source.</param>
    /// <param name="startOffset">The absolute range start.</param>
    /// <param name="length">The range length.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>A document fragment with absolute source spans.</returns>
    public MarkdownDocument ParsePartialSourceWithPipeline(string source, int startOffset, int length, MarkdownPipeline pipeline)
    {
        ValidatePartialArguments(source, startOffset, length, pipeline);
        string normalized = MarkdownInputNormalizer.Normalize(source);
        MarkdownDocument complete = MarkdownParser.Parse(normalized, pipeline);
        MarkdownSourceSpan rangeSpan = CreateRangeSpan(normalized, startOffset, length);
        MarkdownDocument partial = MarkdownDocument.Create(rangeSpan);
        partial.Source = normalized;

        MarkdownNode[] children = complete.Root.GetChildren();
        int index = 0;
        while (index < children.Length)
        {
            if (IntersectsRange(children[index].Span, startOffset, length, normalized.Length))
            {
                partial.Root.AppendChild(children[index].CloneNode());
            }

            index = index + 1;
        }

        MarkdownDiagnostic[] diagnostics = complete.Diagnostics.GetDiagnostics();
        index = 0;
        while (index < diagnostics.Length)
        {
            if (IntersectsRange(diagnostics[index].Span, startOffset, length, normalized.Length))
            {
                partial.Diagnostics.Add(diagnostics[index]);
            }

            index = index + 1;
        }

        if (complete.Root.Trivia != null)
        {
            partial.Root.Trivia = new MarkdownTrivia(
                rangeSpan,
                normalized.Substring(startOffset, length));
        }

        return partial;
    }

    /// <summary>Applies a source change and reparses a document using CommonMark defaults.</summary>
    /// <param name="document">The document to update in place.</param>
    /// <param name="change">The contiguous source replacement.</param>
    /// <returns>The incremental parse result.</returns>
    public MarkdownIncrementalParseResult ReparseDocument(MarkdownDocument document, MarkdownTextChange change)
    {
        return ReparseDocumentWithPipeline(document, change, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Applies a source change and reparses a document using a supplied pipeline.</summary>
    /// <param name="document">The document to update in place.</param>
    /// <param name="change">The contiguous source replacement.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The incremental parse result.</returns>
    public MarkdownIncrementalParseResult ReparseDocumentWithPipeline(
        MarkdownDocument document,
        MarkdownTextChange change,
        MarkdownPipeline pipeline)
    {
        ValidateReparseArguments(document, change, pipeline);
        string insertedText = MarkdownInputNormalizer.Normalize(change.InsertedText);
        string newSource = document.Source.Substring(0, change.StartOffset)
            + insertedText
            + document.Source.Substring(change.StartOffset + change.RemovedLength);
        MarkdownDocument parsed = MarkdownParser.Parse(newSource, pipeline);
        MarkdownNode[] oldChildren = document.Root.GetChildren();
        MarkdownNode[] parsedChildren = parsed.Root.GetChildren();
        List<MarkdownNode> selectedChildren = new List<MarkdownNode>();
        int reusedCount = 0;
        int index = 0;
        while (index < parsedChildren.Length)
        {
            MarkdownNode selected = parsedChildren[index];
            if (index < oldChildren.Length
                && IsOutsideOldChange(oldChildren[index].Span, change)
                && NodesMatchExactly(oldChildren[index], parsedChildren[index]))
            {
                selected = oldChildren[index];
                reusedCount = reusedCount + 1;
            }

            selectedChildren.Add(selected);
            index = index + 1;
        }

        document.Root.ClearChildren();
        parsed.Root.ClearChildren();
        CopyRootState(parsed.Root, document.Root);
        index = 0;
        while (index < selectedChildren.Count)
        {
            document.Root.AppendChild(selectedChildren[index]);
            index = index + 1;
        }

        document.Source = newSource;
        document.Diagnostics = parsed.Diagnostics;
        document.SourceAttributions = parsed.SourceAttributions;
        MarkdownSourceSpan changedSpan = CreateRangeSpan(newSource, change.StartOffset, insertedText.Length);
        return new MarkdownIncrementalParseResult(document, changedSpan, reusedCount, true);
    }

    internal static void ValidatePartialArguments(string source, int startOffset, int length, MarkdownPipeline pipeline)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (pipeline == null) throw new ArgumentNullException("pipeline");
        if (startOffset < 0 || startOffset > source.Length) throw new ArgumentOutOfRangeException("startOffset");
        if (length < 0 || length > source.Length - startOffset) throw new ArgumentOutOfRangeException("length");
    }

    internal static void ValidateReparseArguments(MarkdownDocument document, MarkdownTextChange change, MarkdownPipeline pipeline)
    {
        if (document == null) throw new ArgumentNullException("document");
        if (change == null) throw new ArgumentNullException("change");
        if (pipeline == null) throw new ArgumentNullException("pipeline");
        if (change.StartOffset < 0 || change.StartOffset > document.Source.Length) throw new ArgumentOutOfRangeException("StartOffset");
        if (change.RemovedLength < 0 || change.RemovedLength > document.Source.Length - change.StartOffset) throw new ArgumentOutOfRangeException("RemovedLength");
        if (change.InsertedText == null) throw new ArgumentNullException("InsertedText");
    }

    internal static MarkdownSourceSpan CreateRangeSpan(string source, int startOffset, int length)
    {
        MarkdownSourcePosition start = GetPosition(source, startOffset);
        MarkdownSourcePosition end = GetPosition(source, startOffset + length);
        return MarkdownSourceSpan.Create(start, end);
    }

    internal static MarkdownSourcePosition GetPosition(string source, int offset)
    {
        int line = 1;
        int column = 1;
        int index = 0;
        while (index < offset)
        {
            char character = source[index];
            if (character == '\r')
            {
                if (index + 1 < source.Length && source[index + 1] == '\n' && index + 1 < offset)
                {
                    index = index + 1;
                }

                line = line + 1;
                column = 1;
            }
            else if (character == '\n')
            {
                line = line + 1;
                column = 1;
            }
            else
            {
                column = column + 1;
            }

            index = index + 1;
        }

        return MarkdownSourcePosition.Create(offset, line, column);
    }

    internal static bool IntersectsRange(MarkdownSourceSpan span, int startOffset, int length, int sourceLength)
    {
        if (length == 0)
        {
            return startOffset >= span.Start.Offset
                && (startOffset < span.End.Offset || startOffset == sourceLength && startOffset == span.End.Offset);
        }

        int endOffset = startOffset + length;
        return span.Start.Offset < endOffset && span.End.Offset > startOffset;
    }

    internal static bool IsOutsideOldChange(MarkdownSourceSpan span, MarkdownTextChange change)
    {
        int changeEnd = change.StartOffset + change.RemovedLength;
        return span.End.Offset <= change.StartOffset || span.Start.Offset >= changeEnd;
    }

    internal static bool NodesMatchExactly(MarkdownNode first, MarkdownNode second)
    {
        if (first.Kind != second.Kind || first.Literal != second.Literal || !SpansMatch(first.Span, second.Span)) return false;
        if (first.Attributes.Count != second.Attributes.Count || first.Children.Count != second.Children.Count) return false;
        int index = 0;
        while (index < first.Attributes.Count)
        {
            if (first.Attributes[index].Name != second.Attributes[index].Name
                || first.Attributes[index].Value != second.Attributes[index].Value) return false;
            index = index + 1;
        }

        index = 0;
        while (index < first.Children.Count)
        {
            if (!NodesMatchExactly(first.Children[index], second.Children[index])) return false;
            index = index + 1;
        }

        return true;
    }

    internal static bool SpansMatch(MarkdownSourceSpan first, MarkdownSourceSpan second)
    {
        return first.Start.Offset == second.Start.Offset
            && first.Start.Line == second.Start.Line
            && first.Start.Column == second.Start.Column
            && first.End.Offset == second.End.Offset
            && first.End.Line == second.End.Line
            && first.End.Column == second.End.Column;
    }

    internal static void CopyRootState(MarkdownNode source, MarkdownNode destination)
    {
        destination.SetKind(source.Kind);
        destination.SetSourceSpan(source.Span);
        destination.SetLiteral(source.Literal);
        destination.ClearAttributes();
        int index = 0;
        while (index < source.Attributes.Count)
        {
            destination.SetAttributeCopy(source.Attributes[index]);
            index = index + 1;
        }

        destination.Data = source.Data.CopyData();
        destination.Trivia = source.Trivia == null ? null : source.Trivia.Copy();
        destination.Diagnostics.Clear();
        MarkdownDiagnostic[] diagnostics = source.Diagnostics.GetDiagnostics();
        index = 0;
        while (index < diagnostics.Length)
        {
            destination.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }
    }
}
