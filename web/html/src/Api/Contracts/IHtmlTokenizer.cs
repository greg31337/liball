using System;
using System.IO;

namespace html;
/// <summary>Defines the incremental operations exposed by an HTML tokenizer.</summary>
public interface IHtmlTokenizer
{
    /// <summary>Adds the next input segment and records whether it is the final segment.</summary>
    /// <param name = "input">The input data to process.</param>
    /// <param name = "isFinal">Whether no more input will be supplied after this chunk.</param>
    public void Feed(ReadOnlySpan<char> input, bool isFinal);
    /// <summary>Attaches a text reader that subsequent reads pull from as input is needed.</summary>
    /// <param name = "reader">The decoded character source to pull from.</param>
    /// <param name = "leaveOpen">Whether reset or terminal completion leaves the reader open.</param>
    public void SetReader(TextReader reader, bool leaveOpen);
    /// <summary>Initializes tokenizer state for a context-sensitive HTML fragment.</summary>
    /// <param name = "context">The element or template-content context that controls tokenization.</param>
    /// <param name = "scriptingEnabled">Whether scripting-sensitive fragment states are enabled; no script is executed.</param>
    public void InitializeFragment(HtmlFragmentContext context, bool scriptingEnabled);
    /// <summary>Reads the next available item or reports why one is not yet available.</summary>
    /// <returns>The next token-read status, token when available, and diagnostics.</returns>
    public HtmlTokenReadResult Read();
    /// <summary>Clears accumulated state so the tokenizer can process new input.</summary>
    public void Reset();
    /// <summary>Controls whether CDATA sections are recognized in the current foreign-content context.</summary>
    /// <param name = "value">Whether CDATA syntax is currently allowed.</param>
    public void SetCdataAllowed(bool value);
}
